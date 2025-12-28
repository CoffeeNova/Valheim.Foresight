using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Valheim.Foresight.Autogen;
using Valheim.Foresight.Components;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Core;
using Valheim.Foresight.Models;
using Valheim.Foresight.Patches;
using Valheim.Foresight.Services.Castbar;
using Valheim.Foresight.Services.Castbar.Interfaces;
using Valheim.Foresight.Services.Combat;
using Valheim.Foresight.Services.Combat.Interfaces;
using Valheim.Foresight.Services.Combat.Wrappers;
using Valheim.Foresight.Services.Damage;
using Valheim.Foresight.Services.Hud;
using Valheim.Foresight.Services.Hud.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight;

/// <summary>
/// Main plugin class for ValheimForesight threat assessment mod
/// </summary>
[BepInPlugin(
    PluginInfoGenerated.PluginGuid,
    PluginInfoGenerated.PluginName,
    PluginInfoGenerated.PluginVersion
)]
public sealed class ValheimForesightPlugin : BaseUnityPlugin
{
    internal static ILogger Log = null!;
    public static bool InstanceDebugHudEnabled => _instance?._config.DebugEnabled.Value ?? false;
    public static IForesightConfiguration? ForesightConfig => _instance?._config;
    public static IAttackOverridesConfig? AttackOverridesConfig => _instance?._attackConfig;
    public static IActiveAttackTracker? ActiveAttackTracker { get; private set; }
    public static IUnityCastbarRenderer? CastbarRenderer { get; private set; }
    public static IAttackTimingService? AttackTimingService { get; private set; }

    private static IThreatIconSpriteProvider? _spriteProvider;
    private static AttackTimingEditorManager? _editorManager;

    internal static IThreatResponseHintService ThreatResponseHintService =>
        _instance?._threatResponseHintService
        ?? throw new InvalidOperationException("ThreatResponseHintService not initialized.");

    internal static IThreatHudIconRenderer? HudIconRenderer => _instance?._hudIconRenderer;

    private static ValheimForesightPlugin? _instance;
    private static Harmony? _harmony;

    private readonly Dictionary<Character?, ThreatAssessment?> _threatCache = new();

    private ForesightConfiguration _config = null!;
    private AttackOverridesConfig _attackConfig = null!;
    private IThreatCalculationService _threatService = null!;
    private IDifficultyMultiplierCalculator _difficultyCalculator = null!;
    private IThreatResponseHintService _threatResponseHintService = null!;
    private IThreatHudIconRenderer? _hudIconRenderer;

    private float _playerLogTimer;
    private float _enemyUpdateTimer;
    private bool _isPluginActive;

    /// <summary>
    /// Tries to get the cached threat assessment for a character
    /// </summary>
    public static bool TryGetThreatAssessment(
        Character? character,
        out ThreatAssessment? assessment
    )
    {
        assessment = null;
        if (_instance is null || character is null)
            return false;

        return _instance._threatCache.TryGetValue(character, out assessment);
    }

    private void Awake()
    {
        _instance = this;
        _isPluginActive = true;
        InitializeServices();
        ApplyHarmonyPatches();
        LogDifficultySettings();

        var asm = typeof(ValheimForesightPlugin).Assembly;
        foreach (var n in asm.GetManifestResourceNames())
            Log.LogDebug($"Embedded: {n}");

        Log.LogInfo($"{PluginInfoGenerated.PluginName} {PluginInfoGenerated.PluginVersion} loaded");
    }

    private void OnDestroy()
    {
        Log.LogInfo(
            $"{PluginInfoGenerated.PluginName} {PluginInfoGenerated.PluginVersion} unloading..."
        );

        _config.SettingsChanged -= OnConfigurationChanged;

        DisablePlugin();

        if (_instance == this)
        {
            _instance = null;
        }

        Log.LogInfo(
            $"{PluginInfoGenerated.PluginName} {PluginInfoGenerated.PluginVersion} unloaded"
        );
        Log = null!;
    }

    private void InitializeServices()
    {
        _config = new ForesightConfiguration(Config);
        _config.SettingsChanged += OnConfigurationChanged;
        _attackConfig = new AttackOverridesConfig(Config);

        Log = new ForesightLogger(Logger)
        {
            IsLogsEnabled = _config.IsLogsEnabled.Value,
            IsDebugLogsEnabled = _config.DebugEnabled.Value,
        };

        var blockEstimator = new BlockDamageEstimator(Log);
        var parryEstimator = new ParryDamageEstimator(Log);

        var attackInspector = new Lazy<ICreatureAttackInspector?>(() =>
        {
            if (ZNetScene.instance == null)
            {
                Log.LogError("ZNetScene not found");
                return null;
            }
            var wrapper = new ZNetSceneWrapper(ZNetScene.instance);
            return new CreatureAttackInspector(wrapper, Log);
        });

        var playerWrapper = new PlayerWrapper();
        var zoneSystemWrapper = new ZoneSystemWrapper();
        var mathfWrapper = new MathfWrapper();
        var vector3Wrapper = new Vector3Wrapper();
        _difficultyCalculator = new DifficultyMultiplierCalculator(
            Log,
            playerWrapper,
            zoneSystemWrapper,
            mathfWrapper
        );

        _threatService = new ThreatCalculationService(
            Log,
            blockEstimator,
            parryEstimator,
            attackInspector,
            _difficultyCalculator,
            vector3Wrapper,
            mathfWrapper
        );

        _threatResponseHintService = new ThreatResponseHintService();
        var asm = typeof(ValheimForesightPlugin).Assembly;
        var embedded = new AssemblyEmbeddedResourceStreamProvider(asm);
        _spriteProvider = new EmbeddedPngSpriteProvider(embedded, Log);
        _hudIconRenderer = new UnityThreatHudIconRenderer(_spriteProvider);

        ActiveAttackTracker = new ActiveAttackTracker();
        var parryWindowService = new ParryWindowService(_config);
        CastbarRenderer = new UnityCastbarRenderer(Log, _config, parryWindowService);
        AttackTimingService = new AttackTimingService(Log, _config, _attackConfig);

        InitializeEditorUI();
    }

    private void InitializeEditorUI()
    {
        if (AttackTimingService == null)
        {
            Log.LogError("AttackTimingService not initialized");
            return;
        }

        var keyString = _config.TimingEditorToggleKey.Value;
        if (!Enum.TryParse<KeyCode>(keyString, true, out var toggleKey))
        {
            Log.LogWarning($"Invalid key '{keyString}', using default F7");
            toggleKey = KeyCode.F7;
        }

        var go = new GameObject("AttackTimingEditorManager");
        DontDestroyOnLoad(go);
        _editorManager = go.AddComponent<AttackTimingEditorManager>();
        _editorManager.Initialize(
            Log,
            AttackTimingService,
            (IAttackTimingDataProvider)AttackTimingService,
            _config,
            toggleKey
        );

        Log.LogInfo(
            $"Attack Timing Editor UI initialized ({toggleKey} to toggle, Global Learning: {_config.AttackTimingLearningEnabled.Value})"
        );
    }

    private void ApplyHarmonyPatches()
    {
        var harmony = new Harmony(PluginInfoGenerated.PluginGuid);
        harmony.PatchAll();
        _harmony = harmony;

        var target = AccessTools.Method(typeof(EnemyHud), "LateUpdate");
        var postfix = AccessTools.Method(typeof(EnemyHudPatch), "LateUpdatePostfix");
        if (target == null)
        {
            Log.LogError("Failed to find EnemyHud.LateUpdate");
        }
        else if (postfix == null)
        {
            Log.LogError("Failed to find EnemyHudPatch.LateUpdatePostfix");
        }
        else
        {
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            Log.LogInfo("Patched EnemyHud.LateUpdate");
        }

        var targetCustomFixedUpdate = AccessTools.Method(typeof(Character), "CustomFixedUpdate");
        var postfixCustomFixedUpdate = AccessTools.Method(
            typeof(CharacterFixedUpdatePatch),
            "Postfix"
        );
        if (targetCustomFixedUpdate == null)
        {
            Log.LogError("Failed to find Character.CustomFixedUpdate");
        }
        else if (postfixCustomFixedUpdate == null)
        {
            Log.LogError("Failed to find CharacterFixedUpdatePatch.Postfix");
        }
        else
        {
            harmony.Patch(
                targetCustomFixedUpdate,
                postfix: new HarmonyMethod(postfixCustomFixedUpdate)
            );
            Log.LogInfo("Patched Character.CustomFixedUpdate");
        }
    }

    private void Update()
    {
        if (_config.PluginEnabled.Value != _isPluginActive)
        {
            _isPluginActive = _config.PluginEnabled.Value;
            if (_isPluginActive)
            {
                EnablePlugin();
            }
            else
            {
                DisablePlugin();
            }
        }

        if (!_isPluginActive)
            return;

        _playerLogTimer += Time.deltaTime;
        _enemyUpdateTimer += Time.deltaTime;
        _enemyUpdateTimer += Time.deltaTime;

        if (_playerLogTimer >= 1f)
        {
            _playerLogTimer = 0f;
            LogPlayerHealth();
        }

        if (_enemyUpdateTimer >= 1f)
        {
            _enemyUpdateTimer = 0f;
            UpdateNearbyEnemiesThreat();
        }

        CleanupThreatCache();
        AttackTimingService?.Update();
    }

    private void LogPlayerHealth()
    {
        var player = Player.m_localPlayer;
        if (player == null)
        {
            Log.LogDebug("Player not spawned yet");
            return;
        }

        Log.LogDebug($"Player HP: {player.GetHealth()}");
    }

    private void UpdateNearbyEnemiesThreat()
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return;

        var allCharacters = Character.GetAllCharacters();
        var playerPos = player.transform.position;
        const float updateRadiusSq = 100f * 100f;

        foreach (var character in allCharacters)
        {
            if (!IsValidEnemy(character))
                continue;

            var distSq = (character.transform.position - playerPos).sqrMagnitude;
            if (distSq > updateRadiusSq)
                continue;

            var assessment = _threatService.CalculateThreat(character, player, false);
            if (assessment != null)
            {
                _threatCache[character] = assessment;
            }
        }
    }

    private bool IsValidEnemy(Character character)
    {
        return character != null
            && !character.IsPlayer()
            && !string.IsNullOrEmpty(character.m_name);
    }

    private void CleanupThreatCache()
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return;

        const float maxDistanceSq = 100f * 100f;
        var toRemove = new List<Character?>();
        var playerPos = player.transform.position;

        foreach (var kvp in _threatCache)
        {
            var character = kvp.Key;
            if (ShouldRemoveFromCache(character, playerPos, maxDistanceSq))
            {
                toRemove.Add(character);
            }
        }

        foreach (var character in toRemove)
        {
            if (character != null)
                _threatCache.Remove(character);
        }
    }

    private bool ShouldRemoveFromCache(Character? character, Vector3 playerPos, float maxDistanceSq)
    {
        if (character == null || character.IsDead())
            return true;

        var distSq = (character.transform.position - playerPos).sqrMagnitude;
        return distSq > maxDistanceSq;
    }

    private void LogDifficultySettings()
    {
        if (ZoneSystem.instance == null)
        {
            Log.LogDebug($"[{nameof(LogDifficultySettings)}]ZoneSystem not ready yet");
            return;
        }

        var enemyDmg = _difficultyCalculator.GetIncomingDamageFactor();
        var enemyHp = _difficultyCalculator.GetEnemyHealthFactor();

        Log.LogInfo($"Current difficulty: EnemyDamage={enemyDmg:F2}x, EnemyHP={enemyHp:F2}x");
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        Log.IsLogsEnabled = _config.IsLogsEnabled.Value;
        Log.IsDebugLogsEnabled = _config.DebugEnabled.Value;
    }

    private void DisablePlugin()
    {
        Log.LogInfo($"{PluginInfoGenerated.PluginName} disabling...");

        _threatCache.Clear();

        CastbarRenderer?.Dispose();
        CastbarRenderer = null;

        _hudIconRenderer?.Dispose();
        _hudIconRenderer = null;

        _spriteProvider?.Dispose();
        _spriteProvider = null;

        AttackTimingService?.Dispose();
        AttackTimingService = null;

        if (_editorManager != null)
        {
            Destroy(_editorManager.gameObject);
            _editorManager = null;
        }

        _harmony?.UnpatchSelf();
        _harmony = null;

        Log.LogInfo($"{PluginInfoGenerated.PluginName} disabled");
    }

    private void EnablePlugin()
    {
        Log.LogInfo($"{PluginInfoGenerated.PluginName} enabling...");

        InitializeServices();
        ApplyHarmonyPatches();

        Log.LogInfo($"{PluginInfoGenerated.PluginName} enabled");
    }
}

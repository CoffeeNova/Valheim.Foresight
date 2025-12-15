using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Valheim.Foresight.Autogen;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Core;
using Valheim.Foresight.Models;
using Valheim.Foresight.Patches;
using Valheim.Foresight.Services.Combat;
using Valheim.Foresight.Services.Combat.Interfaces;
using Valheim.Foresight.Services.Combat.Wrappers;
using Valheim.Foresight.Services.Damage;
using Valheim.Foresight.Services.Hud;
using Valheim.Foresight.Services.Hud.Interfaces;
using Valheim.Foresight.Services.Hud.Wrappers;
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
    public static bool InstanceDebugHudEnabled => _instance?._config.DebugHudEnabled.Value ?? false;

    internal static IThreatResponseHintService ThreatResponseHintService =>
        _instance?._threatResponseHintService
        ?? throw new InvalidOperationException("ThreatResponseHintService not initialized.");

    internal static IThreatHudIconRenderer? HudIconRenderer => _instance?._hudIconRenderer;

    private static ValheimForesightPlugin? _instance;

    private readonly Dictionary<Character?, ThreatAssessment?> _threatCache = new();

    private ForesightConfiguration _config = null!;
    private IThreatCalculationService _threatService = null!;
    private IDifficultyMultiplierCalculator _difficultyCalculator = null!;
    private IThreatResponseHintService _threatResponseHintService = null!;
    private IThreatHudIconRenderer? _hudIconRenderer;

    private float _playerLogTimer;
    private float _enemyUpdateTimer;

    public static bool TryGetThreatAssessment(
        Character? character,
        out ThreatAssessment? assessment
    )
    {
        assessment = null;
        if (_instance == null || character == null)
            return false;

        return _instance._threatCache.TryGetValue(character, out assessment);
    }

    private void Awake()
    {
        _instance = this;
        InitializeServices();
        ApplyHarmonyPatches();
        LogDifficultySettings();

        var asm = typeof(ValheimForesightPlugin).Assembly;
        foreach (var n in asm.GetManifestResourceNames())
            Log.LogDebug($"Embedded: {n}");

        Log.LogInfo($"{PluginInfoGenerated.PluginName} {PluginInfoGenerated.PluginVersion} loaded");
    }

    private void InitializeServices()
    {
        _config = new ForesightConfiguration(Config);
        _config.SettingsChanged += OnConfigurationChanged;

        Log = new ForesightLogger(Logger)
        {
            IsLogsEnabled = _config.IsLogsEnabled.Value,
            IsDebugLogsEnabled = _config.DebugHudEnabled.Value,
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
        //var resourcesWrapper = new ResourcesWrapper();
        // var spriteProvider = new UnityResourcesThreatIconSpriteProvider (resourcesWrapper, Log);
        var asm = typeof(ValheimForesightPlugin).Assembly;
        var embedded = new AssemblyEmbeddedResourceStreamProvider(asm);
        IThreatIconSpriteProvider spriteProvider = new EmbeddedPngSpriteProvider(embedded, Log);
        _hudIconRenderer = new UnityThreatHudIconRenderer(spriteProvider);
    }

    private void ApplyHarmonyPatches()
    {
        var harmony = new Harmony(PluginInfoGenerated.PluginGuid);
        harmony.PatchAll();

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
    }

    private void Update()
    {
        _playerLogTimer += Time.deltaTime;
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

    private bool TryFindNearestEnemy(Player player, out Character nearestEnemy)
    {
        nearestEnemy = null!;
        var allCharacters = Character.GetAllCharacters();
        var nearestDistSq = float.MaxValue;
        var playerPos = player.transform.position;

        foreach (var character in allCharacters)
        {
            if (!IsValidEnemy(character))
                continue;

            var distSq = (character.transform.position - playerPos).sqrMagnitude;
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestEnemy = character;
            }
        }

        return nearestEnemy != null;
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

        var allKeys = _difficultyCalculator.GetAllGlobalKeys();
        Log.LogInfo($"Global Keys count: {allKeys.Count}");

        foreach (var key in allKeys)
        {
            if (ZoneSystem.instance.GetGlobalKey(key, out string value))
            {
                Log.LogInfo($"  {key} = {value}");
            }
            else
            {
                Log.LogInfo($"  {key} (no value)");
            }
        }

        var enemyDmg = _difficultyCalculator.GetIncomingDamageFactor();
        var enemyHp = _difficultyCalculator.GetEnemyHealthFactor();

        Log.LogInfo($"Current difficulty: EnemyDamage={enemyDmg:F2}x, EnemyHP={enemyHp:F2}x");
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        Log.IsLogsEnabled = _config.IsLogsEnabled.Value;
        Log.IsDebugLogsEnabled = _config.DebugHudEnabled.Value;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Manages manual attack duration overrides through configuration
/// </summary>
public sealed class AttackOverridesConfig : IAttackOverridesConfig
{
    private const string SectionName = "Attack Overrides";
    private const string OverrideDefaultValue = "gd_king::stomp=3.0";
    private const string IgnoreDefaultValue = "gd_king::spawn,gd_king::scream";
    private const string MappingDefaultValue =
        "TentaRoot::attack->punch,Dragon::attack_breath->cold breath";

    private const string NoParryDefaultValue =
        "Bonemass::aoe,Bonemass::spawn,Dragon::Taunt,Dragon::attack_breath";

    private Dictionary<AttackKey, AttackOverrideData>? _cachedOverrides;
    private Dictionary<AttackKey, string>? _cachedMappings;
    private HashSet<AttackKey>? _cachedIgnored;
    private HashSet<AttackKey>? _cachedNoParry;

    private static readonly char[] ConfigSeparator = ['\n', '\r', ',', ';'];

    public ConfigEntry<string> DurationOverrideList { get; }
    public ConfigEntry<string> IgnoreList { get; }
    public ConfigEntry<string> AttackMappingList { get; }
    public ConfigEntry<string> NoParryIndicatorList { get; }

    public AttackOverridesConfig(ConfigFile configFile)
    {
        DurationOverrideList = configFile.Bind(
            SectionName,
            "OverrideList",
            OverrideDefaultValue,
            new ConfigDescription(
                "Manual attack duration overrides (in seconds). Hit timing is always calculated automatically.\n"
                    + "Format (one per line, comma or semicolon separated): CreatureName::AttackAnimation=duration\n"
                    + "Examples:\n"
                    + "  Dragon::attack_breath=3.5\n"
                    + "  Troll::attack_log=2.8\n"
                    + "  Eikthyr::stomp=1.8\n"
                    + "Lines starting with # are ignored as comments.",
                null,
                new ConfigurationManagerAttributes { Order = 98 }
            )
        );

        IgnoreList = configFile.Bind(
            SectionName,
            "IgnoreList",
            IgnoreDefaultValue,
            new ConfigDescription(
                "Attacks to ignore (no castbar will be shown). Useful for DOT, AOE, or buggy attacks.\n"
                    + "Format (one per line, comma or semicolon separated): CreatureName::AttackAnimation\n"
                    + "Examples:\n"
                    + "  Blob::attack_aoe\n"
                    + "  Wraith::attack_drain\n"
                    + "  Dragon::attack_groundfire\n"
                    + "Lines starting with # are ignored as comments.",
                null,
                new ConfigurationManagerAttributes { Order = 99 }
            )
        );

        AttackMappingList = configFile.Bind(
            SectionName,
            "AttackMappingList",
            MappingDefaultValue,
            new ConfigDescription(
                "Manual mapping of attack names to animation names for attacks that don't auto-detect correctly.\n"
                    + "Format (one per line, comma or semicolon separated): CreatureName::AttackName->AnimationName\n"
                    + "Use this when attack.m_attackAnimation doesn't match the actual animator state name.\n"
                    + "Examples:\n"
                    + "  gd_king::attack_stomp->stomp_aoe\n"
                    + "  Dragon::breath->dragon_breath_attack\n"
                    + "  Bonemass::vomit->boss_attack3\n"
                    + "Lines starting with # are ignored as comments.",
                null,
                new ConfigurationManagerAttributes { Order = 100 }
            )
        );

        NoParryIndicatorList = configFile.Bind(
            SectionName,
            "NoParryIndicatorList",
            NoParryDefaultValue,
            new ConfigDescription(
                "Attacks where parry indicator should not be shown (castbar still visible, but no parry line).\n"
                    + "Useful for unparriable AOE attacks, or attacks that can only be dodged.\n"
                    + "Format (one per line, comma or semicolon separated): CreatureName::AttackName\n"
                    + "Examples:\n"
                    + "  Dragon::attack_breath\n"
                    + "  Bonemass::aoe\n"
                    + "  Fuling_Shaman::fireball\n"
                    + "Lines starting with # are ignored as comments.",
                null,
                new ConfigurationManagerAttributes { Order = 101 }
            )
        );

        DurationOverrideList.SettingChanged += (_, _) => InvalidateCache();
        IgnoreList.SettingChanged += (_, _) => InvalidateCache();
        AttackMappingList.SettingChanged += (_, _) => InvalidateCache();
        NoParryIndicatorList.SettingChanged += (_, _) => InvalidateCache();
    }

    /// <inheritdoc/>
    public float? GetOverriddenDuration(string creaturePrefab, string attackAnimation)
    {
        EnsureCacheBuilt();

        var key = new AttackKey(creaturePrefab, attackAnimation);
        return _cachedOverrides!.TryGetValue(key, out var data) ? data.Duration : null;
    }

    /// <inheritdoc/>
    public bool ShouldIgnoreAttack(string creaturePrefab, string attackAnimation)
    {
        EnsureCacheBuilt();

        var key = new AttackKey(creaturePrefab, attackAnimation);

        return _cachedIgnored!.Contains(key);
    }

    /// <inheritdoc/>
    public string? GetMappedAnimationName(string creaturePrefab, string attackAnimation)
    {
        EnsureCacheBuilt();

        var key = new AttackKey(creaturePrefab, attackAnimation);
        return _cachedMappings!.GetValueOrDefault(key);
    }

    /// <inheritdoc/>
    public bool ShouldHideParryIndicator(string creaturePrefab, string attackAnimation)
    {
        EnsureCacheBuilt();

        var key = new AttackKey(creaturePrefab, attackAnimation);
        return _cachedNoParry!.Contains(key);
    }

    /// <inheritdoc/>
    public bool HasOverride(string creaturePrefab, string attackAnimation)
    {
        return GetOverriddenDuration(creaturePrefab, attackAnimation).HasValue;
    }

    /// <inheritdoc/>
    public bool HasMapping(string creaturePrefab, string attackAnimation)
    {
        return GetMappedAnimationName(creaturePrefab, attackAnimation) != null;
    }

    private void EnsureCacheBuilt()
    {
        if (
            _cachedOverrides != null
            && _cachedIgnored != null
            && _cachedMappings != null
            && _cachedNoParry != null
        )
            return;

        var newOverrides = new Dictionary<AttackKey, AttackOverrideData>();
        var newIgnored = new HashSet<AttackKey>();
        var newMappings = new Dictionary<AttackKey, string>();
        var newNoParry = new HashSet<AttackKey>();

        var overrideLines = ParseLines(DurationOverrideList);
        foreach (var line in overrideLines)
        {
            if (TryParseOverrideLine(line, out var key, out var data))
            {
                newOverrides[key] = data;
            }
        }

        var ignoreLines = ParseLines(IgnoreList);
        foreach (var line in ignoreLines)
        {
            if (TryParseIgnoreLine(line, out var key))
            {
                newIgnored.Add(key);
            }
        }

        var mappingLines = ParseLines(AttackMappingList);
        foreach (var line in mappingLines)
        {
            if (TryParseMappingLine(line, out var key, out var animationName))
            {
                newMappings[key] = animationName;
            }
        }

        var noParryLines = ParseLines(NoParryIndicatorList);
        foreach (var line in noParryLines)
        {
            if (TryParseNoParryLine(line, out var key))
            {
                newNoParry.Add(key);
            }
        }

        _cachedOverrides = newOverrides;
        _cachedIgnored = newIgnored;
        _cachedMappings = newMappings;
        _cachedNoParry = newNoParry;
    }

    private IEnumerable<string> ParseLines(ConfigEntry<string> config)
    {
        return config
            .Value.Split(ConfigSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"));
    }

    private void InvalidateCache()
    {
        _cachedOverrides = null;
        _cachedIgnored = null;
    }

    private bool TryParseOverrideLine(string line, out AttackKey key, out AttackOverrideData data)
    {
        key = default;
        data = default;

        // Format: "CreatureName::AttackAnimation=duration"
        var parts = line.Split('=');
        if (parts.Length != 2)
            return false;

        var keyParts = parts[0].Split(["::"], StringSplitOptions.None);
        if (keyParts.Length != 2)
            return false;

        if (!float.TryParse(parts[1].Trim(), out var duration) || duration <= 0)
            return false;

        key = new AttackKey(keyParts[0].Trim(), keyParts[1].Trim());
        data = new AttackOverrideData(duration);

        return true;
    }

    private bool TryParseIgnoreLine(string line, out AttackKey key)
    {
        key = default;

        // Format: "CreatureName::AttackAnimation"
        var parts = line.Split(["::"], StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        key = new AttackKey(parts[0].Trim(), parts[1].Trim());
        return true;
    }

    private bool TryParseMappingLine(string line, out AttackKey key, out string animationName)
    {
        key = default;
        animationName = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        // Format: "CreatureName::AttackName->AnimationName"
        var parts = line.Split(["->"], StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var keyParts = parts[0].Split(["::"], StringSplitOptions.None);
        if (keyParts.Length != 2)
            return false;

        var creatureName = keyParts[0].Trim();
        var attackName = keyParts[1].Trim();
        var mappedAnimName = parts[1].Trim();

        if (
            string.IsNullOrWhiteSpace(creatureName)
            || string.IsNullOrWhiteSpace(attackName)
            || string.IsNullOrWhiteSpace(mappedAnimName)
        )
            return false;

        key = new AttackKey(creatureName, attackName);
        animationName = mappedAnimName;

        return true;
    }

    private bool TryParseNoParryLine(string line, out AttackKey key)
    {
        key = default;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        // Format: "CreatureName::AttackName"
        var parts = line.Split(["::"], StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var creatureName = parts[0].Trim();
        var attackName = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(creatureName) || string.IsNullOrWhiteSpace(attackName))
            return false;

        key = new AttackKey(creatureName, attackName);

        return true;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Valheim.Foresight.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Configuration entry for a single attack timing with UI controls
/// </summary>
public sealed class AttackTimingConfigEntry
{
    public ConfigEntry<float> Timing { get; set; } = null!;
    public ConfigEntry<bool> LearningEnabled { get; set; } = null!;
}

/// <summary>
/// Manages attack timing configurations with UI (ConfigEntry) and YAML file sync
/// </summary>
public sealed class AttackTimingsConfig : IAttackTimingsConfig
{
    private const string DataFileName = "attack_timings.yml";
    private const string TimingsDbDirectoryName = "foresight.Database";
    private const float AutoSaveIntervalSeconds = 30f;
    private const string SectionName = "Attack Timings Database";
    private const string GlobalSectionName = "Attack Timings Global";

    private readonly string _dataFilePath;
    private readonly ConfigFile _configFile;
    private readonly Dictionary<AttackKey, AttackTimingStats> _cachedTimings = new();
    private readonly Dictionary<AttackKey, AttackTimingConfigEntry> _configEntries = new();
    private readonly Action<AttackKey>? _onResetCallback;
    private ConfigEntry<bool>? _globalDisableAll;
    private bool _isDirty;
    private float _timeSinceLastSave;
    private bool _isUpdatingFromCode; // Prevent infinite loops

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public AttackTimingsConfig(ConfigFile configFile, Action<AttackKey>? onResetCallback = null)
    {
        _configFile = configFile;
        _onResetCallback = onResetCallback;

        var configDir = Path.Combine(Paths.ConfigPath, TimingsDbDirectoryName);
        Directory.CreateDirectory(configDir);
        _dataFilePath = Path.Combine(configDir, DataFileName);

        // Create global Enable/Disable All control
        _globalDisableAll = configFile.Bind(
            GlobalSectionName,
            "Enable Learning For All Attacks",
            true,
            new ConfigDescription(
                "When disabled, stops learning for ALL attacks. Changes are applied immediately.",
                null,
                new ConfigurationManagerAttributes { Order = 100 }
            )
        );

        _globalDisableAll.SettingChanged += OnGlobalDisableChanged;

        // Don't load from disk here - AttackTimingService will provide data via LoadExistingTimings
        // This ensures AttackTimingService is the single source of truth for loading/saving
    }

    /// <inheritdoc/>
    public void RegisterAttackTiming(
        AttackKey key,
        float meanHitOffset,
        int sampleCount,
        bool learningEnabled
    )
    {
        if (_cachedTimings.ContainsKey(key))
        {
            // Update existing entry
            _cachedTimings[key] = new AttackTimingStats
            {
                MeanHitOffsetSeconds = meanHitOffset,
                Variance = _cachedTimings[key].Variance,
                SampleCount = sampleCount,
                LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LearningEnabled = learningEnabled,
            };

            // Update UI if exists
            if (_configEntries.TryGetValue(key, out var entry))
            {
                _isUpdatingFromCode = true;
                entry.Timing.Value = meanHitOffset;
                entry.LearningEnabled.Value = learningEnabled;
                _isUpdatingFromCode = false;
            }

            _isDirty = true;
            return;
        }

        // Add new entry
        _cachedTimings[key] = new AttackTimingStats
        {
            MeanHitOffsetSeconds = meanHitOffset,
            Variance = 0f,
            SampleCount = sampleCount,
            LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            LearningEnabled = learningEnabled,
        };

        // Create UI config entries
        CreateConfigEntry(key, meanHitOffset, sampleCount, learningEnabled);

        _isDirty = true;
    }

    /// <inheritdoc/>
    public void UpdateAttackTiming(
        AttackKey key,
        float meanHitOffset,
        int sampleCount,
        bool learningEnabled
    )
    {
        if (!_cachedTimings.ContainsKey(key))
        {
            RegisterAttackTiming(key, meanHitOffset, sampleCount, learningEnabled);
            return;
        }

        // Only update if the value changed significantly (avoid constant updates)
        var stats = _cachedTimings[key];
        bool changed =
            Math.Abs(stats.MeanHitOffsetSeconds - meanHitOffset) > 0.001f
            || stats.LearningEnabled != learningEnabled;

        if (changed)
        {
            _cachedTimings[key] = new AttackTimingStats
            {
                MeanHitOffsetSeconds = meanHitOffset,
                Variance = stats.Variance,
                SampleCount = sampleCount,
                LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LearningEnabled = learningEnabled,
            };

            // Update UI if exists
            if (_configEntries.TryGetValue(key, out var entry))
            {
                _isUpdatingFromCode = true;
                entry.Timing.Value = meanHitOffset;
                entry.LearningEnabled.Value = learningEnabled;
                _isUpdatingFromCode = false;
            }

            _isDirty = true;
        }
    }

    /// <inheritdoc/>
    public float? GetConfiguredTiming(AttackKey key)
    {
        return _cachedTimings.TryGetValue(key, out var stats) ? stats.MeanHitOffsetSeconds : null;
    }

    /// <inheritdoc/>
    public bool HasConfiguredTiming(AttackKey key)
    {
        return _cachedTimings.ContainsKey(key);
    }

    /// <inheritdoc/>
    public void RemoveAttackTiming(AttackKey key)
    {
        _cachedTimings.Remove(key);
    }

    /// <inheritdoc/>
    public IEnumerable<AttackKey> GetAllConfiguredAttacks()
    {
        return _cachedTimings.Keys.ToList();
    }

    /// <inheritdoc/>
    public void LoadExistingTimings(Dictionary<AttackKey, AttackTimingStats> timings)
    {
        foreach (var kvp in timings)
        {
            RegisterAttackTiming(
                kvp.Key,
                kvp.Value.MeanHitOffsetSeconds,
                kvp.Value.SampleCount,
                kvp.Value.LearningEnabled
            );
        }
    }

    /// <inheritdoc/>
    public void ResetToPrelearned(AttackKey key, float prelearnedValue)
    {
        if (_cachedTimings.ContainsKey(key))
        {
            // Keep current learning state when resetting timing value
            // (LearningEnabled flag is copied from prelearned during AttackTimingService reset)
            var currentLearningEnabled = _cachedTimings[key].LearningEnabled;

            _cachedTimings[key] = new AttackTimingStats
            {
                MeanHitOffsetSeconds = prelearnedValue,
                Variance = _cachedTimings[key].Variance,
                SampleCount = _cachedTimings[key].SampleCount,
                LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LearningEnabled = currentLearningEnabled,
            };

            // Update UI if exists
            if (_configEntries.TryGetValue(key, out var entry))
            {
                _isUpdatingFromCode = true;
                entry.Timing.Value = prelearnedValue;
                _isUpdatingFromCode = false;
            }

            _isDirty = true;
            SaveToDisk();
        }
    }

    /// <summary>
    /// Saves the cached timings to YAML file if dirty
    /// </summary>
    public void SaveToDisk()
    {
        if (!_isDirty)
            return;

        try
        {
            var db = new AttackTimingYamlDatabase
            {
                LastSavedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            foreach (var kvp in _cachedTimings)
            {
                db.Timings[kvp.Key.ToString()] = kvp.Value;
            }

            var yaml = YamlSerializer.Serialize(db);
            File.WriteAllText(_dataFilePath, yaml);

            _isDirty = false;
        }
        catch (Exception)
        {
            // Silently fail - no logger available in config class
        }
    }

    /// <summary>
    /// Updates the auto-save timer and saves if needed
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isDirty)
            return;

        _timeSinceLastSave += deltaTime;

        if (_timeSinceLastSave >= AutoSaveIntervalSeconds)
        {
            SaveToDisk();
            _timeSinceLastSave = 0f;
        }
    }

    /// <summary>
    /// Creates ConfigEntry UI elements for timing and learning enabled flag
    /// </summary>
    private void CreateConfigEntry(
        AttackKey key,
        float meanHitOffset,
        int sampleCount,
        bool learningEnabled
    )
    {
        if (_configEntries.ContainsKey(key))
            return;

        var configKey = $"{key.CreaturePrefab}::{key.AttackAnimation}";

        // Create timing entry
        var timingEntry = _configFile.Bind(
            SectionName,
            $"{configKey}_Timing",
            meanHitOffset,
            new ConfigDescription(
                $"Hit timing offset (seconds) for {key.CreaturePrefab}'s {key.AttackAnimation} attack.\n"
                    + $"Learned from {sampleCount} sample(s).",
                new AcceptableValueRange<float>(0f, 10f),
                new ConfigurationManagerAttributes { Order = 0 }
            )
        );

        // Create learning enabled entry
        var learningEnabledEntry = _configFile.Bind(
            SectionName,
            $"{configKey}_LearningEnabled",
            learningEnabled,
            new ConfigDescription(
                $"Enable learning for this attack. When disabled, timing won't be updated from new samples.",
                null,
                new ConfigurationManagerAttributes { Order = 0 }
            )
        );

        _configEntries[key] = new AttackTimingConfigEntry
        {
            Timing = timingEntry,
            LearningEnabled = learningEnabledEntry,
        };

        // When user changes timing in UI
        timingEntry.SettingChanged += (sender, _) =>
        {
            if (_isUpdatingFromCode)
                return;

            if (sender is ConfigEntry<float> changedEntry && _cachedTimings.ContainsKey(key))
            {
                var stats = _cachedTimings[key];
                stats.MeanHitOffsetSeconds = changedEntry.Value;
                stats.LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _isDirty = true;
            }
        };

        // When user changes learning enabled flag in UI
        learningEnabledEntry.SettingChanged += (sender, _) =>
        {
            if (_isUpdatingFromCode)
                return;

            if (sender is ConfigEntry<bool> changedEntry && _cachedTimings.ContainsKey(key))
            {
                var stats = _cachedTimings[key];
                stats.LearningEnabled = changedEntry.Value;
                stats.LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _isDirty = true;
            }
        };
    }

    /// <summary>
    /// Handler for global Enable/Disable All toggle
    /// </summary>
    private void OnGlobalDisableChanged(object? sender, EventArgs e)
    {
        if (_globalDisableAll == null || _isUpdatingFromCode)
            return;

        var enableAll = _globalDisableAll.Value;

        _isUpdatingFromCode = true;

        // Update all cached timings
        foreach (var kvp in _cachedTimings)
        {
            kvp.Value.LearningEnabled = enableAll;
            kvp.Value.LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Update UI
            if (_configEntries.TryGetValue(kvp.Key, out var entry))
            {
                entry.LearningEnabled.Value = enableAll;
            }
        }

        _isUpdatingFromCode = false;
        _isDirty = true;
    }
}

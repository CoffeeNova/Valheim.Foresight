using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Core;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Castbar.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Castbar;

/// <summary>
/// Service for tracking and predicting enemy attack timings
/// </summary>
public sealed class AttackTimingService : IAttackTimingService, IAttackTimingDataProvider
{
    private const string DataFileName = "attack_timings.yml";
    private const string PrelearnedDataFileName = "attack_timings_prelearned.yml";
    private const float AutoSaveIntervalSeconds = 30f;
    private const int MinSamplesForPrediction = 2;
    private const string UnknownKeyName = "unknown";
    private const string TimingsDbDirectoryName = "foresight.Database";
    private const float ParryIndicatorStartPosition = 0.7f;

    private readonly string _dataFilePath;
    private readonly string _prelearnedDataFilePath;
    private readonly ILogger _logger;
    private readonly Dictionary<AttackKey, AttackTimingStats> _timings = new();
    private readonly Dictionary<AttackKey, AttackTimingStats> _prelearnedTimings = new();
    private readonly IForesightConfiguration _config;
    private readonly IAttackOverridesConfig _overridesConfig;

    private float _timeSinceLastSave;
    private bool _isDirty;

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public AttackTimingService(
        ILogger logger,
        IForesightConfiguration config,
        IAttackOverridesConfig overridesConfig
    )
    {
        _logger = logger;
        _config = config;
        _overridesConfig = overridesConfig;

        var configDir = Path.Combine(Paths.ConfigPath, TimingsDbDirectoryName);
        Directory.CreateDirectory(configDir);
        _dataFilePath = Path.Combine(configDir, DataFileName);
        _prelearnedDataFilePath = Path.Combine(configDir, PrelearnedDataFileName);

        ExtractEmbeddedPrelearnedFile();
        LoadPrelearnedFromDisk();
        LoadFromDisk();
    }

    /// <summary>
    /// Resets a timing to its prelearned value
    /// </summary>
    public void ResetToPrelearned(AttackKey key)
    {
        _logger.LogInfo($"[{nameof(ResetToPrelearned)}] Resetting {key} to prelearned value");

        if (_prelearnedTimings.TryGetValue(key, out var prelearnedStats))
        {
            var newStats = new AttackTimingStats
            {
                MeanHitOffsetSeconds = prelearnedStats.MeanHitOffsetSeconds,
                Variance = prelearnedStats.Variance,
                SampleCount = prelearnedStats.SampleCount,
                LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LearningEnabled = prelearnedStats.LearningEnabled,
            };

            _timings[key] = newStats;
            _isDirty = true;

            _logger.LogInfo(
                $"[{nameof(ResetToPrelearned)}] Reset {key} to prelearned: mean={newStats.MeanHitOffsetSeconds:F3}s, variance={newStats.Variance:F4}, samples={newStats.SampleCount}, learning={newStats.LearningEnabled}"
            );

            SaveToDisk();
            _logger.LogInfo($"[{nameof(ResetToPrelearned)}] Changes saved to disk immediately");
        }
        else
        {
            _logger.LogWarning(
                $"[{nameof(ResetToPrelearned)}] No prelearned value found for {key}, cannot reset. Available prelearned count: {_prelearnedTimings.Count}"
            );
        }
    }

    /// <inheritdoc/>
    public void RecordHit(Character? attacker, Attack? attack, float hitTime, float attackStartTime)
    {
        if (attacker is null || attack is null)
            return;

        if (!_config.AttackTimingLearningEnabled.Value)
        {
            _logger.LogDebug($"[{nameof(RecordHit)}] Global learning disabled, skipping recording");
            return;
        }

        var key = CreateKey(attacker, attack);
        var hitOffset = hitTime - attackStartTime;

        if (hitOffset < 0f || hitOffset > 10f) // sanity check
        {
            _logger.LogWarning(
                $"[{nameof(RecordHit)}] Suspicious hit offset {hitOffset:F3}s for {key}, ignoring"
            );
            return;
        }

        if (!_timings.TryGetValue(key, out var stats))
        {
            stats = new AttackTimingStats(hitOffset);
            _timings[key] = stats;
            _logger.LogDebug($"[{nameof(RecordHit)}] New timing: {key} -> {hitOffset:F3}s");
        }
        else
        {
            if (!stats.LearningEnabled)
            {
                _logger.LogDebug(
                    $"[{nameof(RecordHit)}] Learning disabled for {key}, skipping sample"
                );
                return;
            }

            stats.AddSample(hitOffset);
            _logger.LogDebug(
                $"[{nameof(RecordHit)}] Updated {key}: mean={stats.MeanHitOffsetSeconds:F3}s, samples={stats.SampleCount}"
            );
        }

        _isDirty = true;
    }

    /// <inheritdoc/>
    public float? GetPredictedHitTime(Character? attacker, Attack? attack)
    {
        if (attacker is null || attack is null)
            return null;

        var key = CreateKey(attacker, attack);

        if (
            _timings.TryGetValue(key, out var stats)
            && stats.SampleCount >= MinSamplesForPrediction
        )
        {
            return stats.MeanHitOffsetSeconds;
        }

        if (_prelearnedTimings.TryGetValue(key, out var prelearnedStats))
        {
            return prelearnedStats.MeanHitOffsetSeconds;
        }

        return ParryIndicatorStartPosition;
    }

    /// <inheritdoc/>
    public float? GetOverriddenDuration(Character? attacker, Attack? attack)
    {
        if (attacker is null || attack is null)
            return null;

        var prefab = attacker.GetPrefabName();
        var anim = attack.m_attackAnimation ?? UnknownKeyName;

        return _overridesConfig.GetOverriddenDuration(prefab, anim);
    }

    /// <inheritdoc/>
    public bool ShouldIgnoreAttack(Character? attacker, Attack? attack)
    {
        if (attacker is null || attack is null)
            return false;

        var prefab = attacker.GetPrefabName();
        var anim = attack.m_attackAnimation ?? UnknownKeyName;

        return _overridesConfig.ShouldIgnoreAttack(prefab, anim);
    }

    /// <inheritdoc/>
    public bool ShouldHideParryIndicator(Character? attacker, Attack? attack)
    {
        if (attacker is null || attack is null)
            return false;

        var prefab = attacker.GetPrefabName();
        var anim = attack.m_attackAnimation ?? UnknownKeyName;

        return _overridesConfig.ShouldHideParryIndicator(prefab, anim);
    }

    /// <inheritdoc/>
    public void Update()
    {
        if (!_isDirty)
            return;

        _timeSinceLastSave += Time.deltaTime;

        if (_timeSinceLastSave >= AutoSaveIntervalSeconds)
        {
            SaveToDisk();
            _timeSinceLastSave = 0f;
        }
    }

    public void Dispose()
    {
        SaveToDisk();
    }

    private AttackKey CreateKey(Character? attacker, Attack? attack)
    {
        var prefabName = attacker.GetPrefabName();
        var animName = attack?.m_attackAnimation ?? UnknownKeyName;
        return new AttackKey(prefabName, animName);
    }

    /// <summary>
    /// Extracts embedded prelearned file to config directory if it doesn't exist
    /// </summary>
    private void ExtractEmbeddedPrelearnedFile()
    {
        try
        {
            var assembly = typeof(AttackTimingService).Assembly;
            var resourceName = "Valheim.Foresight.Assets.attack_timings_prelearned.yml";

            // Debug: List all embedded resources
            var allResources = assembly.GetManifestResourceNames();
            _logger.LogInfo(
                $"[{nameof(ExtractEmbeddedPrelearnedFile)}] Found {allResources.Length} embedded resources:"
            );
            foreach (var res in allResources)
            {
                _logger.LogInfo($"  - {res}");
            }

            // Skip if file already exists and is not empty
            if (File.Exists(_prelearnedDataFilePath))
            {
                var fileInfo = new FileInfo(_prelearnedDataFilePath);
                if (fileInfo.Length > 0)
                {
                    _logger.LogInfo(
                        $"[{nameof(ExtractEmbeddedPrelearnedFile)}] Prelearned file already exists ({fileInfo.Length} bytes), skipping extraction"
                    );
                    return;
                }
                else
                {
                    _logger.LogInfo(
                        $"[{nameof(ExtractEmbeddedPrelearnedFile)}] Prelearned file exists but is empty, re-extracting"
                    );
                }
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning(
                    $"[{nameof(ExtractEmbeddedPrelearnedFile)}] Embedded resource '{resourceName}' not found"
                );
                return;
            }

            // Extract to file
            using var fileStream = File.Create(_prelearnedDataFilePath);
            stream.CopyTo(fileStream);

            _logger.LogInfo(
                $"[{nameof(ExtractEmbeddedPrelearnedFile)}] Extracted embedded prelearned file to {_prelearnedDataFilePath}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[{nameof(ExtractEmbeddedPrelearnedFile)}] Failed to extract embedded resource: {ex.Message}"
            );
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogInfo(
                    $"[{nameof(LoadFromDisk)}] No existing data file at {_dataFilePath}"
                );

                // First run: copy prelearned to main database
                CopyPrelearnedToMain();
                return;
            }

            var yaml = File.ReadAllText(_dataFilePath);
            var db = YamlDeserializer.Deserialize<AttackTimingYamlDatabase?>(yaml);
            if (db?.Timings == null)
                return;

            foreach (var kvp in db.Timings)
            {
                var parts = kvp.Key.Split(["::"], StringSplitOptions.None);
                if (parts.Length != 2)
                    continue;

                var key = new AttackKey(parts[0], parts[1]);
                _timings[key] = kvp.Value;
            }

            _logger.LogInfo(
                $"[{nameof(LoadFromDisk)}] Loaded {_timings.Count} attack timings from disk"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{nameof(LoadFromDisk)}]  Failed to load data: {ex.Message}");
        }
    }

    private void LoadPrelearnedFromDisk()
    {
        try
        {
            if (!File.Exists(_prelearnedDataFilePath))
            {
                _logger.LogInfo(
                    $"[{nameof(LoadPrelearnedFromDisk)}] No prelearned data file at {_prelearnedDataFilePath}"
                );
                return;
            }

            var yaml = File.ReadAllText(_prelearnedDataFilePath);
            var db = YamlDeserializer.Deserialize<AttackTimingYamlDatabase?>(yaml);
            if (db?.Timings == null)
                return;

            foreach (var kvp in db.Timings)
            {
                var parts = kvp.Key.Split(["::"], StringSplitOptions.None);
                if (parts.Length != 2)
                    continue;

                var key = new AttackKey(parts[0], parts[1]);
                _prelearnedTimings[key] = kvp.Value;
            }

            _logger.LogInfo(
                $"[{nameof(LoadPrelearnedFromDisk)}] Loaded {_prelearnedTimings.Count} prelearned timings"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[{nameof(LoadPrelearnedFromDisk)}] Failed to load prelearned data: {ex.Message}"
            );
        }
    }

    private void CopyPrelearnedToMain()
    {
        if (_prelearnedTimings.Count == 0)
        {
            _logger.LogInfo($"[{nameof(CopyPrelearnedToMain)}] No prelearned data to copy");
            return;
        }

        try
        {
            foreach (var kvp in _prelearnedTimings)
            {
                _timings[kvp.Key] = kvp.Value;
            }

            _isDirty = true;
            SaveToDisk();

            _logger.LogInfo(
                $"[{nameof(CopyPrelearnedToMain)}] Copied {_prelearnedTimings.Count} prelearned timings to main database"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[{nameof(CopyPrelearnedToMain)}] Failed to copy prelearned data: {ex.Message}"
            );
        }
    }

    private void SaveToDisk()
    {
        if (!_isDirty)
            return;

        try
        {
            var db = new AttackTimingYamlDatabase
            {
                LastSavedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            foreach (var kvp in _timings)
            {
                db.Timings[kvp.Key.ToString()] = kvp.Value;
            }

            var yaml = YamlSerializer.Serialize(db);
            File.WriteAllText(_dataFilePath, yaml);

            _isDirty = false;
            _logger.LogDebug(
                $"[{nameof(SaveToDisk)}] Saved {_timings.Count} timings to {_dataFilePath}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{nameof(SaveToDisk)}] Failed to save data: {ex.Message}");
        }
    }

    public Dictionary<AttackKey, AttackTimingStats> GetAllTimings()
    {
        var result = new Dictionary<AttackKey, AttackTimingStats>();

        foreach (var kvp in _prelearnedTimings)
        {
            result[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in _timings)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    public Dictionary<AttackKey, AttackTimingStats> GetLearnedTimings()
    {
        return new Dictionary<AttackKey, AttackTimingStats>(_timings);
    }

    public Dictionary<AttackKey, AttackTimingStats> GetPrelearnedTimings()
    {
        return new Dictionary<AttackKey, AttackTimingStats>(_prelearnedTimings);
    }

    public void UpdateTiming(AttackKey key, AttackTimingStats stats)
    {
        _timings[key] = stats;
        _isDirty = true;
        _logger.LogInfo(
            $"[{nameof(UpdateTiming)}] Updated timing for {key}: mean={stats.MeanHitOffsetSeconds:F3}s"
        );
    }

    public void DeleteTiming(AttackKey key)
    {
        if (_timings.Remove(key))
        {
            _isDirty = true;
            _logger.LogInfo($"[{nameof(DeleteTiming)}] Deleted timing for {key}");
        }
    }

    public void ForceSave()
    {
        if (_isDirty)
        {
            SaveToDisk();
            _logger.LogInfo($"[{nameof(ForceSave)}] Force saved timings to disk");
        }
    }
}

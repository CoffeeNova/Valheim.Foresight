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
public sealed class AttackTimingService : IAttackTimingService
{
    private const string DataFileName = "attack_timings.yml";
    private const float AutoSaveIntervalSeconds = 30f;
    private const int MinSamplesForPrediction = 2;
    private const string UnknownKeyName = "unknown";
    private const string TimingsDbDirectoryName = "foresight.Database";

    private readonly string _dataFilePath;
    private readonly ILogger _logger;
    private readonly Dictionary<AttackKey, AttackTimingStats> _timings = new();
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

        LoadFromDisk();
    }

    /// <inheritdoc/>
    public void RecordHit(Character? attacker, Attack? attack, float hitTime, float attackStartTime)
    {
        if (attacker is null || attack is null)
            return;

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

        return _config.ParryIndicatorStartPosition.Value;
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

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogInfo(
                    $"[{nameof(LoadFromDisk)}] No existing data file at {_dataFilePath}"
                );
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
}

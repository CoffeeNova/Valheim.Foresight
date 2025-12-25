using System;
using System.Collections.Generic;

namespace Valheim.Foresight.Models;

/// <summary>
/// Key for identifying a unique attack
/// </summary>
[Serializable]
public struct AttackKey : IEquatable<AttackKey>
{
    public string CreaturePrefab { get; set; }

    public string AttackAnimation { get; set; }

    public AttackKey(string? creaturePrefab, string? attackAnimation)
    {
        CreaturePrefab = creaturePrefab ?? string.Empty;
        AttackAnimation = attackAnimation ?? string.Empty;
    }

    public bool Equals(AttackKey other) =>
        string.Equals(CreaturePrefab, other.CreaturePrefab, StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            AttackAnimation,
            other.AttackAnimation,
            StringComparison.OrdinalIgnoreCase
        );

    public override bool Equals(object? obj) => obj is AttackKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(CreaturePrefab, AttackAnimation);

    public override string ToString() => $"{CreaturePrefab}::{AttackAnimation}";
}

/// <summary>
/// Statistics for hit timing of a specific attack
/// </summary>
[Serializable]
public class AttackTimingStats
{
    public float MeanHitOffsetSeconds { get; set; }
    public float Variance { get; set; }
    public int SampleCount { get; set; }
    public long LastUpdatedUtc { get; set; }

    /// <summary>
    /// If true, learning is enabled for this attack - timing will be updated from new samples
    /// </summary>
    public bool LearningEnabled { get; set; }

    public AttackTimingStats() { }

    /// <summary>
    /// Creates new timing statistics with initial hit offset
    /// </summary>
    public AttackTimingStats(float hitOffset)
    {
        MeanHitOffsetSeconds = hitOffset;
        Variance = 0f;
        SampleCount = 1;
        LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        LearningEnabled = true;
    }

    /// <summary>
    /// Updates statistics with a new sample (rolling mean + variance)
    /// </summary>
    public void AddSample(float newHitOffset)
    {
        var n = SampleCount;
        var oldMean = MeanHitOffsetSeconds;
        var newMean = oldMean + (newHitOffset - oldMean) / (n + 1);
        var newVariance =
            (n * Variance + (newHitOffset - oldMean) * (newHitOffset - newMean)) / (n + 1);

        MeanHitOffsetSeconds = newMean;
        Variance = newVariance;
        SampleCount = n + 1;
        LastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Gets the standard deviation
    /// </summary>
    public float GetStdDev() => UnityEngine.Mathf.Sqrt(Variance);
}

/// <summary>
/// Container for YAML serialization
/// </summary>
[Serializable]
public sealed class AttackTimingYamlDatabase
{
    public Dictionary<string, AttackTimingStats> Timings { get; set; } = new();
    public long LastSavedUtc { get; set; }
}

using System.Collections.Generic;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Configuration interface for managing attack timings through UI
/// </summary>
public interface IAttackTimingsConfig
{
    /// <summary>
    /// Registers a new attack timing entry in the configuration UI
    /// </summary>
    void RegisterAttackTiming(
        AttackKey key,
        float meanHitOffset,
        int sampleCount,
        bool learningEnabled
    );

    /// <summary>
    /// Updates an existing attack timing entry
    /// </summary>
    void UpdateAttackTiming(
        AttackKey key,
        float meanHitOffset,
        int sampleCount,
        bool learningEnabled
    );

    /// <summary>
    /// Gets the configured timing value for an attack
    /// </summary>
    float? GetConfiguredTiming(AttackKey key);

    /// <summary>
    /// Checks if an attack has a configured timing
    /// </summary>
    bool HasConfiguredTiming(AttackKey key);

    /// <summary>
    /// Removes an attack timing from configuration
    /// </summary>
    void RemoveAttackTiming(AttackKey key);

    /// <summary>
    /// Gets all configured attack keys
    /// </summary>
    IEnumerable<AttackKey> GetAllConfiguredAttacks();

    /// <summary>
    /// Loads existing timings into the configuration UI
    /// </summary>
    void LoadExistingTimings(Dictionary<AttackKey, AttackTimingStats> timings);

    /// <summary>
    /// Resets an attack timing to its prelearned value
    /// </summary>
    void ResetToPrelearned(AttackKey key, float prelearnedValue);
}

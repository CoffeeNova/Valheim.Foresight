using System.Collections.Generic;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Castbar.Interfaces;

/// <summary>
/// Provides access to attack timing data for UI and editing purposes
/// </summary>
public interface IAttackTimingDataProvider
{
    /// <summary>
    /// Gets all attack timings (learned and prelearned combined)
    /// </summary>
    Dictionary<AttackKey, AttackTimingStats> GetAllTimings();

    /// <summary>
    /// Gets all learned timings only
    /// </summary>
    Dictionary<AttackKey, AttackTimingStats> GetLearnedTimings();

    /// <summary>
    /// Gets all prelearned timings only
    /// </summary>
    Dictionary<AttackKey, AttackTimingStats> GetPrelearnedTimings();

    /// <summary>
    /// Updates a specific timing
    /// </summary>
    void UpdateTiming(AttackKey key, AttackTimingStats stats);

    /// <summary>
    /// Deletes a specific timing (reverts to prelearned if available)
    /// </summary>
    void DeleteTiming(AttackKey key);

    /// <summary>
    /// Forces a save to disk
    /// </summary>
    void ForceSave();
}

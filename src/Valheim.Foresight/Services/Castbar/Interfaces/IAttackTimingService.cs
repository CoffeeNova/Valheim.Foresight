using System;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Castbar.Interfaces;

/// <summary>
/// Service for tracking and predicting enemy attack timings
/// </summary>
public interface IAttackTimingService : IDisposable
{
    /// <summary>
    /// Records an actual hit and updates statistics
    /// </summary>
    void RecordHit(Character? attacker, Attack? attack, float hitTime, float attackStartTime);

    /// <summary>
    /// Returns predicted hit time (in seconds from attack start) or null if no data available
    /// </summary>
    float? GetPredictedHitTime(Character? attacker, Attack? attack);

    /// <summary>
    /// Gets the overridden duration or null if no override exists
    /// </summary>
    float? GetOverriddenDuration(Character? attacker, Attack? attack);

    /// <summary>
    /// Checks if the attack should be ignored
    /// </summary>
    bool ShouldIgnoreAttack(Character? attacker, Attack? attack);

    /// <summary>
    /// Checks if the parry indicator should be hidden for this attack
    /// </summary>
    bool ShouldHideParryIndicator(Character? attacker, Attack? attack);

    /// <summary>
    /// Resets a timing to its prelearned value
    /// </summary>
    void ResetToPrelearned(AttackKey key);

    /// <summary>
    /// Updates every frame for auto-saving
    /// </summary>
    void Update();
}

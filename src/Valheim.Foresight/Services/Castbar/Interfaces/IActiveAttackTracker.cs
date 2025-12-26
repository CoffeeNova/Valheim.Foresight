using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Castbar.Interfaces;

/// <summary>
/// Service for tracking active enemy attacks
/// </summary>
public interface IActiveAttackTracker
{
    /// <summary>
    /// Registers a new active attack
    /// </summary>
    void RegisterAttack(
        Character attacker,
        Attack attack,
        float duration,
        float startTime,
        float? predictedHitTime,
        string? animationName,
        bool hideParryIndicator
    );

    /// <summary>
    /// Gets the active attack for the given attacker
    /// </summary>
    ActiveAttackInfo? GetActiveAttack(Character attacker);

    /// <summary>
    /// Cleans up expired attacks
    /// </summary>
    void CleanupExpired();
}

using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Castbar.Interfaces;

/// <summary>
/// Service for calculating parry window information based on attack timing
/// </summary>
public interface IParryWindowService
{
    /// <summary>
    /// Checks if we are currently in the parry window for the given attack
    /// </summary>
    bool IsInParryWindow(ActiveAttackInfo attackInfo);

    /// <summary>
    /// Gets the parry window information for rendering
    /// Returns tuple of (startPosition, width) on the castbar (0 to 1 scale)
    /// </summary>
    (float startPosition, float width)? GetParryWindowInfo(
        ActiveAttackInfo attackInfo,
        float castbarDuration
    );
}

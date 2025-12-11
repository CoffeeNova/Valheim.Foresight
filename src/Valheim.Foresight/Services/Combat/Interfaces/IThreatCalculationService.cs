using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Service for calculating threat assessments for enemies
/// </summary>
public interface IThreatCalculationService
{
    /// <summary>
    /// Calculates threat assessment for an enemy against the player
    /// </summary>
    ThreatAssessment? CalculateThreat(Character enemy, Player player, bool detailedMode);

    /// <summary>
    /// Determines threat level based on damage ratios
    /// </summary>
    ThreatLevel DetermineThreatLevel(float blockRatio, float parryRatio);
}

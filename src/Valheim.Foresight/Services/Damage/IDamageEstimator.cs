using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Damage;

/// <summary>
/// Service for estimating effective damage after defenses
/// </summary>
public interface IDamageEstimator
{
    /// <summary>
    /// Estimates effective damage to player after defenses are applied
    /// </summary>
    float EstimateEffectiveDamage(Player player, float rawDamage);

    /// <summary>
    /// Estimates effective damage using player defense stats
    /// </summary>
    float EstimateEffectiveDamage(PlayerDefenseStats defenseStats, float rawDamage);
}

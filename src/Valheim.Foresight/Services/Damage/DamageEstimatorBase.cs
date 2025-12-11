using UnityEngine;
using Valheim.Foresight.Models;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Damage;

/// <summary>
/// Base class for damage estimation with armor and resistance calculations
/// </summary>
public abstract class DamageEstimatorBase : IDamageEstimator
{
    private const float MinimumDamage = 1f;

    protected readonly ILogger Logger;

    protected DamageEstimatorBase(ILogger logger)
    {
        Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    public float EstimateEffectiveDamage(Player player, float rawDamage)
    {
        if (player == null || rawDamage <= 0f)
            return 0f;

        var defenseStats = PlayerDefenseStats.FromPlayer(player);
        return EstimateEffectiveDamage(defenseStats, rawDamage);
    }

    public float EstimateEffectiveDamage(PlayerDefenseStats defenseStats, float rawDamage)
    {
        if (rawDamage <= 0f)
            return 0f;

        var afterBlock = ApplyActiveDefense(defenseStats, rawDamage);
        var afterArmor = ApplyArmor(defenseStats.Armor, afterBlock);
        var afterResists = ApplyResistances(afterArmor, elementalDamage: 0f);

        return Mathf.Max(MinimumDamage, afterResists);
    }

    protected abstract float ApplyActiveDefense(
        PlayerDefenseStats defenseStats,
        float physicalDamage
    );

    private float ApplyArmor(float armor, float physicalDamage)
    {
        if (physicalDamage <= 0f)
            return 0f;

        float result;
        if (armor <= 0f)
        {
            result = physicalDamage;
        }
        else if (armor < physicalDamage / 2f)
        {
            result = physicalDamage - armor;
        }
        else
        {
            result = physicalDamage * physicalDamage / (4f * armor);
        }

        result = Mathf.Max(MinimumDamage, result);

        Logger.LogDebug(
            $"[{GetType().Name}] Armor: in={physicalDamage:F1}, armor={armor:F1}, out={result:F1}"
        );

        return result;
    }

    private float ApplyResistances(float physicalAfterArmor, float elementalDamage)
    {
        // todo: Future enhancement: add resistance calculations
        float total = physicalAfterArmor + elementalDamage;

        Logger.LogDebug(
            $"[{GetType().Name}] Resists: phys={physicalAfterArmor:F1}, elem={elementalDamage:F1}, total={total:F1}"
        );

        return total;
    }
}

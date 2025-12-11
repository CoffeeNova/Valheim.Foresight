using UnityEngine;
using Valheim.Foresight.Models;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Damage;

/// <summary>
/// Estimates damage when player performs a timed parry
/// </summary>
public sealed class ParryDamageEstimator : DamageEstimatorBase
{
    private const float BlockingSkillPercentPerLevel = 0.005f;

    public ParryDamageEstimator(ILogger logger)
        : base(logger) { }

    protected override float ApplyActiveDefense(
        PlayerDefenseStats defenseStats,
        float physicalDamage
    )
    {
        if (physicalDamage <= 0f)
            return 0f;

        if (defenseStats.Shield == null)
            return physicalDamage;

        var blockPower = CalculateEffectiveParryPower(defenseStats);
        var blocked = Mathf.Min(physicalDamage, blockPower);
        var remaining = physicalDamage - blocked;

        Logger.LogDebug(
            $"[{nameof(ParryDamageEstimator)}] Parry: raw={physicalDamage:F1}, "
                + $"baseBlock={defenseStats.Shield.m_shared.m_blockPower:F1}, "
                + $"parryBonus={defenseStats.Shield.m_shared.m_timedBlockBonus:F1}, "
                + $"skill={defenseStats.BlockingSkillLevel:F0}, "
                + $"effBlock={blockPower:F1}, blocked={blocked:F1}, remain={remaining:F1}"
        );

        return remaining;
    }

    private float CalculateEffectiveParryPower(PlayerDefenseStats defenseStats)
    {
        if (defenseStats.Shield == null)
            return 0f;

        var shared = defenseStats.Shield.m_shared;
        var baseBlockPower = shared.m_blockPower;
        var parryBonus = shared.m_timedBlockBonus;
        var skillMultiplier = 1f + (BlockingSkillPercentPerLevel * defenseStats.BlockingSkillLevel);

        return (baseBlockPower + parryBonus) * skillMultiplier;
    }
}

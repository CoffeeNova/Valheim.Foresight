namespace Valheim.Foresight.Models;

/// <summary>
/// Immutable threat assessment result for an enemy
/// </summary>
public sealed class ThreatAssessment
{
    public ThreatLevel Level { get; }
    public DamageInfo DamageInfo { get; }
    public float DamageToHealthRatio { get; }
    public float MaxMeleeDamage { get; }
    public float MaxRangedDamage { get; }
    public bool UsedRangedAttack { get; }

    public ThreatAssessment(
        ThreatLevel level,
        DamageInfo damageInfo,
        float damageToHealthRatio,
        float maxMeleeDamage,
        float maxRangedDamage,
        bool usedRangedAttack
    )
    {
        Level = level;
        DamageInfo = damageInfo;
        DamageToHealthRatio = damageToHealthRatio;
        MaxMeleeDamage = maxMeleeDamage;
        MaxRangedDamage = maxRangedDamage;
        UsedRangedAttack = usedRangedAttack;
    }
}

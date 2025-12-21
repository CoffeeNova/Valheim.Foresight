namespace Valheim.Foresight.Models;

/// <summary>
/// Immutable threat assessment result for an enemy
/// </summary>
public sealed class ThreatAssessment
{
    public ThreatLevel Level { get; }
    public DamageInfo DamageInfo { get; }
    public float DamageToHealthRatio { get; }

    public ThreatAssessment(ThreatLevel level, DamageInfo damageInfo, float damageToHealthRatio)
    {
        Level = level;
        DamageInfo = damageInfo;
        DamageToHealthRatio = damageToHealthRatio;
    }
}

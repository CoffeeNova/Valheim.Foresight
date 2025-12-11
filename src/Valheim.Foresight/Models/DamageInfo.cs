namespace Valheim.Foresight.Models;

/// <summary>
/// Value object representing damage information for an attack
/// </summary>
public readonly struct DamageInfo
{
    public float RawDamage { get; }
    public float EffectiveDamageWithBlock { get; }
    public float EffectiveDamageWithParry { get; }

    public DamageInfo(float rawDamage, float effectiveWithBlock, float effectiveWithParry)
    {
        RawDamage = rawDamage;
        EffectiveDamageWithBlock = effectiveWithBlock;
        EffectiveDamageWithParry = effectiveWithParry;
    }
}

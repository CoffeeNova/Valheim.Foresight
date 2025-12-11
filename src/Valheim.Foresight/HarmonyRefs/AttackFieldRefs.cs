using HarmonyLib;
using UnityEngine;

namespace Valheim.Foresight.HarmonyRefs;

public static class AttackFieldRefs
{
    public static readonly AccessTools.FieldRef<Attack, ItemDrop.ItemData>? WeaponRef;
    public static readonly AccessTools.FieldRef<Attack, Attack.AttackType>? AttackTypeRef;
    public static readonly AccessTools.FieldRef<Attack, GameObject>? ProjectilePrefabRef;
    public static readonly AccessTools.FieldRef<Attack, float>? DamageMultiplierRef;

    static AttackFieldRefs()
    {
        try
        {
            WeaponRef = AccessTools.FieldRefAccess<Attack, ItemDrop.ItemData>(
                nameof(Attack.m_weapon)
            );
        }
        catch (System.Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Attack.{nameof(Attack.m_weapon)}: {ex}"
            );
            WeaponRef = null;
        }

        try
        {
            AttackTypeRef = AccessTools.FieldRefAccess<Attack, Attack.AttackType>(
                nameof(Attack.m_attackType)
            );
        }
        catch (System.Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Attack.{nameof(Attack.m_attackType)}: {ex}"
            );
            AttackTypeRef = null;
        }

        try
        {
            ProjectilePrefabRef = AccessTools.FieldRefAccess<Attack, GameObject>(
                nameof(Attack.m_attackProjectile)
            );
        }
        catch (System.Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Attack.{nameof(Attack.m_attackProjectile)}: {ex}"
            );
            ProjectilePrefabRef = null;
        }

        try
        {
            DamageMultiplierRef = AccessTools.FieldRefAccess<Attack, float>(
                nameof(Attack.m_damageMultiplier)
            );
        }
        catch (System.Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Attack.{nameof(Attack.m_damageMultiplier)}: {ex}"
            );
            DamageMultiplierRef = null;
        }
    }
}

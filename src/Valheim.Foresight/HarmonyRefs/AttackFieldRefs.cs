using HarmonyLib;
using UnityEngine;

namespace Valheim.Foresight.HarmonyRefs;

public static class AttackFieldRefs
{
    private const string WeaponFieldName = "m_weapon";
    private const string AttackTypeFieldName = "m_attackType";
    private const string ProjectilePrefabFieldName = "m_attackProjectile";
    private const string DamageMultiplierFieldName = "m_damageMultiplier";

    public static readonly AccessTools.FieldRef<Attack, ItemDrop.ItemData?>? WeaponRef;
    public static readonly AccessTools.FieldRef<Attack, Attack.AttackType>? AttackTypeRef;
    public static readonly AccessTools.FieldRef<Attack, GameObject?>? ProjectilePrefabRef;
    public static readonly AccessTools.FieldRef<Attack, float>? DamageMultiplierRef;

    static AttackFieldRefs()
    {
        var weaponField = AccessTools.Field(typeof(Attack), WeaponFieldName);
        if (weaponField != null)
        {
            WeaponRef = AccessTools.FieldRefAccess<Attack, ItemDrop.ItemData?>(weaponField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Attack.{WeaponFieldName} not found via Harmony reflection"
            );
            WeaponRef = null;
        }

        var attackTypeField = AccessTools.Field(typeof(Attack), AttackTypeFieldName);
        if (attackTypeField != null)
        {
            AttackTypeRef = AccessTools.FieldRefAccess<Attack, Attack.AttackType>(attackTypeField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Attack.{AttackTypeFieldName} not found via Harmony reflection"
            );
            AttackTypeRef = null;
        }

        var projectilePrefabField = AccessTools.Field(typeof(Attack), ProjectilePrefabFieldName);
        if (projectilePrefabField != null)
        {
            ProjectilePrefabRef = AccessTools.FieldRefAccess<Attack, GameObject?>(
                projectilePrefabField
            );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Attack.{ProjectilePrefabFieldName} not found via Harmony reflection"
            );
            ProjectilePrefabRef = null;
        }

        var damageMultiplierField = AccessTools.Field(typeof(Attack), DamageMultiplierFieldName);
        if (damageMultiplierField != null)
        {
            DamageMultiplierRef = AccessTools.FieldRefAccess<Attack, float>(damageMultiplierField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Attack.{DamageMultiplierFieldName} not found via Harmony reflection"
            );
            DamageMultiplierRef = null;
        }
    }
}

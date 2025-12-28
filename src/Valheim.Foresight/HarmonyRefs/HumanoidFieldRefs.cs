using System;
using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

/// <summary>
/// Provides reflection access to private Humanoid fields using Harmony
/// </summary>
public static class HumanoidFieldRefs
{
    private const string CurrentAttackFieldName = "m_currentAttack";
    private const string RightItemFieldName = "m_rightItem";
    private const string LeftItemFieldName = "m_leftItem";
    private const string RandomSetsFieldName = "m_randomSets";
    private const string UnarmedWeaponFieldName = "m_unarmedWeapon";
    private const string DefaultItemsFieldName = "m_defaultItems";

    public static readonly AccessTools.FieldRef<Humanoid, Attack?>? CurrentAttackRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData?>? RightItemRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData?>? LeftItemRef;
    public static readonly AccessTools.FieldRef<Humanoid, Humanoid.ItemSet[]?>? RandomSetsRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop?>? UnarmedWeaponRef;
    public static readonly AccessTools.FieldRef<Humanoid, UnityEngine.GameObject[]?>? DefaultItemsRef;

    static HumanoidFieldRefs()
    {
        var currentAttackField = AccessTools.Field(typeof(Humanoid), CurrentAttackFieldName);
        if (currentAttackField != null)
        {
            CurrentAttackRef = AccessTools.FieldRefAccess<Humanoid, Attack?>(currentAttackField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.{CurrentAttackFieldName} not found via Harmony reflection"
            );
            CurrentAttackRef = null;
        }

        var rightItemField = AccessTools.Field(typeof(Humanoid), RightItemFieldName);
        if (rightItemField != null)
        {
            RightItemRef = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData?>(rightItemField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.{RightItemFieldName} not found via Harmony reflection"
            );
            RightItemRef = null;
        }

        var leftItemField = AccessTools.Field(typeof(Humanoid), LeftItemFieldName);
        if (leftItemField != null)
        {
            LeftItemRef = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData?>(leftItemField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.{LeftItemFieldName} not found via Harmony reflection"
            );
            LeftItemRef = null;
        }

        var randomSetsField = AccessTools.Field(typeof(Humanoid), RandomSetsFieldName);
        if (randomSetsField != null)
        {
            RandomSetsRef = AccessTools.FieldRefAccess<Humanoid, Humanoid.ItemSet[]?>(
                randomSetsField
            );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.{RandomSetsFieldName} not found via Harmony reflection"
            );
            RandomSetsRef = null;
        }

        var unarmedWeaponField = AccessTools.Field(typeof(Humanoid), UnarmedWeaponFieldName);
        if (unarmedWeaponField != null)
        {
            UnarmedWeaponRef = AccessTools.FieldRefAccess<Humanoid, ItemDrop?>(unarmedWeaponField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.{UnarmedWeaponFieldName} not found via Harmony reflection"
            );
            UnarmedWeaponRef = null;
        }

        var defaultItemsField = AccessTools.Field(typeof(Humanoid), DefaultItemsFieldName);
        if (defaultItemsField != null)
        {
            DefaultItemsRef = AccessTools.FieldRefAccess<Humanoid, UnityEngine.GameObject[]?>(
                defaultItemsField
            );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.{DefaultItemsFieldName} not found via Harmony reflection"
            );
            DefaultItemsRef = null;
        }
    }
}

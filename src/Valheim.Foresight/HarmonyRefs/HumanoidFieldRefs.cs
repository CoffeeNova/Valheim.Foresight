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

    public static readonly AccessTools.FieldRef<Humanoid, Attack?>? CurrentAttackRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData?>? RightItemRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData?>? LeftItemRef;

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
    }
}

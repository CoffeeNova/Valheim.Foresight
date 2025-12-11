using System;
using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

public static class HumanoidFieldRefs
{
    public static readonly AccessTools.FieldRef<Humanoid, Attack>? CurrentAttackRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData>? RightItemRef;
    public static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData>? LeftItemRef;

    static HumanoidFieldRefs()
    {
        try
        {
            CurrentAttackRef = AccessTools.FieldRefAccess<Humanoid, Attack>(
                nameof(Humanoid.m_currentAttack)
            );
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Humanoid.{nameof(Humanoid.m_currentAttack)}: {ex}"
            );
            CurrentAttackRef = null;
        }

        try
        {
            RightItemRef = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(
                nameof(Humanoid.m_rightItem)
            );
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Humanoid.{nameof(Humanoid.m_rightItem)}: {ex}"
            );
            RightItemRef = null;
        }

        try
        {
            LeftItemRef = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(
                nameof(Humanoid.m_leftItem)
            );
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError(
                $"Failed to create FieldRef for Humanoid.{nameof(Humanoid.m_leftItem)}: {ex}"
            );
            RightItemRef = null;
        }
    }
}

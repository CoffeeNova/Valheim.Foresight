using System;
using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

/// <summary>
/// Provides reflection access to Humanoid methods using Harmony
/// </summary>
public static class HumanoidMethodRefs
{
    private const string GetRightItemMethodName = "GetRightItem";
    private const string GetLeftItemMethodName = "GetLeftItem";

    public static readonly Func<Humanoid, ItemDrop.ItemData?>? GetRightItem;
    public static readonly Func<Humanoid, ItemDrop.ItemData?>? GetLeftItem;

    static HumanoidMethodRefs()
    {
        var rightItemMethod = AccessTools.Method(typeof(Humanoid), GetRightItemMethodName);
        if (rightItemMethod != null)
        {
            GetRightItem =
                (Func<Humanoid, ItemDrop.ItemData?>)
                    Delegate.CreateDelegate(
                        typeof(Func<Humanoid, ItemDrop.ItemData?>),
                        rightItemMethod
                    );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Method Humanoid.{GetRightItemMethodName} not found via Harmony reflection"
            );
            GetRightItem = null;
        }

        var leftItemMethod = AccessTools.Method(typeof(Humanoid), GetLeftItemMethodName);
        if (leftItemMethod != null)
        {
            GetLeftItem =
                (Func<Humanoid, ItemDrop.ItemData?>)
                    Delegate.CreateDelegate(
                        typeof(Func<Humanoid, ItemDrop.ItemData?>),
                        leftItemMethod
                    );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Method Humanoid.{GetLeftItemMethodName} not found via Harmony reflection"
            );
            GetLeftItem = null;
        }
    }
}

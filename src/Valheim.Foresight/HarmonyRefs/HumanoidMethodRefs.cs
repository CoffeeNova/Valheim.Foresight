using System;
using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

public static class HumanoidMethodRefs
{
    public static readonly Func<Humanoid, ItemDrop.ItemData?>? GetRightItem;
    public static readonly Func<Humanoid, ItemDrop.ItemData?>? GetLeftItem;

    static HumanoidMethodRefs()
    {
        try
        {
            var mi = AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetRightItem));
            if (mi != null)
            {
                GetRightItem =
                    (Func<Humanoid, ItemDrop.ItemData?>)
                        Delegate.CreateDelegate(typeof(Func<Humanoid, ItemDrop.ItemData?>), mi);
            }
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError($"Failed to bind Humanoid.GetRightItem: {ex}");
            GetRightItem = null;
        }

        try
        {
            var mi = AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetLeftItem));
            if (mi != null)
            {
                GetLeftItem =
                    (Func<Humanoid, ItemDrop.ItemData?>)
                        Delegate.CreateDelegate(typeof(Func<Humanoid, ItemDrop.ItemData?>), mi);
            }
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError($"Failed to bind Humanoid.GetLeftItem: {ex}");
            GetLeftItem = null;
        }
    }
}

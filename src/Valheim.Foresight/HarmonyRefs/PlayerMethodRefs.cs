using System;
using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

public static class PlayerMethodRefs
{
    private const string GetCurrentBlockerMethodName = "GetCurrentBlocker";

    public static readonly Func<Player, ItemDrop.ItemData?>? GetCurrentBlocker;

    static PlayerMethodRefs()
    {
        var mi = AccessTools.Method(typeof(Player), GetCurrentBlockerMethodName);
        if (mi != null)
        {
            GetCurrentBlocker =
                (Func<Player, ItemDrop.ItemData?>)
                    Delegate.CreateDelegate(typeof(Func<Player, ItemDrop.ItemData?>), mi);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Method Player.{GetCurrentBlockerMethodName} not found via Harmony reflection"
            );
            GetCurrentBlocker = null;
        }
    }
}

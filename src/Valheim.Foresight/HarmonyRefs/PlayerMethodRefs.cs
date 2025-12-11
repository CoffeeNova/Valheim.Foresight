using System;
using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

public class PlayerMethodRefs
{
    public static readonly Func<Player, ItemDrop.ItemData?>? GetCurrentBlocker;

    static PlayerMethodRefs()
    {
        try
        {
            var mi = AccessTools.Method(typeof(Player), nameof(Player.GetCurrentBlocker));
            if (mi != null)
            {
                GetCurrentBlocker =
                    (Func<Player, ItemDrop.ItemData?>)
                        Delegate.CreateDelegate(typeof(Func<Player, ItemDrop.ItemData?>), mi);
            }
        }
        catch (Exception ex)
        {
            ValheimForesightPlugin.Log?.LogError($"Failed to bind Player.GetCurrentBlocker: {ex}");
            GetCurrentBlocker = null;
        }
    }
}

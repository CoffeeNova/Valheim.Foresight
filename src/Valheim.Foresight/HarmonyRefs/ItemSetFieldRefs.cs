using HarmonyLib;
using UnityEngine;

namespace Valheim.Foresight.HarmonyRefs;

/// <summary>
/// Provides reflection access to private Humanoid.ItemSet fields using Harmony
/// </summary>
public static class ItemSetFieldRefs
{
    private const string ItemsFieldName = "m_items";

    public static readonly AccessTools.FieldRef<Humanoid.ItemSet, GameObject[]?>? ItemsRef;

    static ItemSetFieldRefs()
    {
        var itemsField = AccessTools.Field(typeof(Humanoid.ItemSet), ItemsFieldName);
        if (itemsField != null)
        {
            ItemsRef = AccessTools.FieldRefAccess<Humanoid.ItemSet, GameObject[]?>(itemsField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field Humanoid.ItemSet.{ItemsFieldName} not found via Harmony reflection"
            );
            ItemsRef = null;
        }
    }
}

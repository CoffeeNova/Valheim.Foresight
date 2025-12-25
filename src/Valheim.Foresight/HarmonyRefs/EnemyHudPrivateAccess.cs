using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace Valheim.Foresight.HarmonyRefs;

/// <summary>
/// Provides access to private fields of EnemyHud using Harmony
/// </summary>
internal static class EnemyHudPrivateAccess
{
    private static readonly FieldInfo HudsField =
        AccessTools.Field(typeof(EnemyHud), "m_huds")
        ?? throw new MissingFieldException(typeof(EnemyHud).FullName, "m_huds");

    private static readonly Type HudDataType =
        typeof(EnemyHud).GetNestedType("HudData", BindingFlags.NonPublic)
        ?? throw new TypeLoadException("EnemyHud.HudData (non-public) not found");

    private static readonly AccessTools.FieldRef<object, TextMeshProUGUI> NameRef =
        AccessTools.FieldRefAccess<TextMeshProUGUI>(HudDataType, "m_name");

    /// <summary>
    /// Gets the m_huds dictionary from EnemyHud instance
    /// </summary>
    public static IDictionary? GetHudsAsDictionary(EnemyHud instance) =>
        HudsField.GetValue(instance) as IDictionary;

    /// <summary>
    /// Tries to get the name label from HudData object
    /// </summary>
    public static TextMeshProUGUI? TryGetNameLabel(object? hudData)
    {
        if (hudData is null || !HudDataType.IsInstanceOfType(hudData))
            return null;

        return NameRef(hudData);
    }
}

using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;

namespace Valheim.Foresight.HarmonyRefs;

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

    public static IDictionary? GetHudsAsDictionary(EnemyHud instance) =>
        HudsField.GetValue(instance) as IDictionary;

    public static TextMeshProUGUI? TryGetNameLabel(object? hudData)
    {
        if (hudData is null || !HudDataType.IsInstanceOfType(hudData))
            return null;

        return NameRef(hudData);
    }
}

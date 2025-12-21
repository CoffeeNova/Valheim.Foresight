// using System.Collections.Generic;
// using HarmonyLib;
//
// namespace Valheim.Foresight.HarmonyRefs;
//
// public static class EnemyHudFieldRefs
// {
//     public static readonly AccessTools.FieldRef<
//         EnemyHud,
//         Dictionary<Character, EnemyHud.HudData>
//     >? HudsRef;
//
//     static EnemyHudFieldRefs()
//     {
//         try
//         {
//             HudsRef = AccessTools.FieldRefAccess<EnemyHud, Dictionary<Character, EnemyHud.HudData>>(
//                 nameof(EnemyHud.m_huds)
//             );
//         }
//         catch (System.Exception ex)
//         {
//             ValheimForesightPlugin.Log?.LogError(
//                 $"Failed to create FieldRef for EnemyHud.{nameof(EnemyHud.m_huds)}: {ex}"
//             );
//             HudsRef = null;
//         }
//     }
// }

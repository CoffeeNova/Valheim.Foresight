using UnityEngine;
using Valheim.Foresight.HarmonyRefs;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Patches;

/// <summary>
/// Harmony patch for colorizing and annotating enemy HUD elements
/// </summary>
public static class EnemyHudPatch
{
    private static readonly Color SafeColor = Color.white;
    private static readonly Color CautionColor = new(1f, 0.75f, 0.25f);
    private static readonly Color BlockLethalColor = new(1f, 0.5f, 0.1f);
    private static readonly Color DangerColor = new(1f, 0.2f, 0.2f);

    internal static void LateUpdatePostfix(EnemyHud __instance)
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return;

        var huds = EnemyHudFieldRefs.HudsRef?.Invoke(__instance);
        if (huds == null || huds.Count == 0)
            return;

        foreach (var kvp in huds)
        {
            var character = kvp.Key;
            var hud = kvp.Value;

            if (!IsValidHud(character, hud))
                continue;

            if (
                !ValheimForesightPlugin.TryGetThreatAssessment(character, out var assessment)
                || assessment == null
            )
                continue;

            ApplyThreatVisualization(hud, assessment);
        }
    }

    private static bool IsValidHud(Character? character, EnemyHud.HudData? hud)
    {
        return character != null && hud != null && hud.m_name != null;
    }

    private static void ApplyThreatVisualization(EnemyHud.HudData hud, ThreatAssessment assessment)
    {
        ColorizeByThreatLevel(hud, assessment.Level);

        if (ValheimForesightPlugin.InstanceDebugHudEnabled)
        {
            AppendDebugInfo(hud, assessment);
        }
    }

    private static void ColorizeByThreatLevel(EnemyHud.HudData hud, ThreatLevel level)
    {
        if (hud?.m_name == null)
            return;

        hud.m_name.color = level switch
        {
            ThreatLevel.Safe => SafeColor,
            ThreatLevel.Caution => CautionColor,
            ThreatLevel.BlockLethal => BlockLethalColor,
            ThreatLevel.Danger => DangerColor,
            _ => SafeColor,
        };
    }

    private static void AppendDebugInfo(EnemyHud.HudData hud, ThreatAssessment assessment)
    {
        var mode = assessment.UsedRangedAttack ? "R" : "M";
        var levelCode = GetThreatLevelCode(assessment.Level);

        var debugSuffix =
            $" [{levelCode}-{mode} "
            + $"r={assessment.DamageToHealthRatio:F2} "
            + $"raw={assessment.DamageInfo.RawDamage:F1} "
            + $"eff={assessment.DamageInfo.EffectiveDamageWithBlock:F1}]";

        hud.m_name.text += debugSuffix;
    }

    private static string GetThreatLevelCode(ThreatLevel level) =>
        level switch
        {
            ThreatLevel.Safe => "SAFE",
            ThreatLevel.Caution => "CAUT",
            ThreatLevel.BlockLethal => "BLCK",
            ThreatLevel.Danger => "DNG",
            _ => "UNK",
        };
}

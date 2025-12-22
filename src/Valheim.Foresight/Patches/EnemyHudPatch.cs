using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Valheim.Foresight.HarmonyRefs;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Patches;

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

        var huds = EnemyHudPrivateAccess.GetHudsAsDictionary(__instance);
        if (huds == null || huds.Count == 0)
            return;

        foreach (DictionaryEntry entry in huds)
        {
            var character = entry.Key as Character;
            var hudObj = entry.Value;

            if (character == null || hudObj == null)
                continue;

            var nameLabel = EnemyHudPrivateAccess.TryGetNameLabel(hudObj);
            if (nameLabel == null)
                continue;

            var holder = nameLabel.GetComponent<OriginalNameHolder>();
            if (holder is null)
            {
                holder = nameLabel.gameObject.AddComponent<OriginalNameHolder>();
                holder.originalName = nameLabel.text;
            }

            if (
                !ValheimForesightPlugin.TryGetThreatAssessment(character, out var assessment)
                || assessment == null
            )
                continue;

            ColorizeByThreatLevel(nameLabel, assessment.Level);

            ThreatResponseHint hint;
            try
            {
                hint = ValheimForesightPlugin.ThreatResponseHintService.GetHint(assessment);
            }
            catch
            {
                hint = ThreatResponseHint.None;
            }

            ValheimForesightPlugin.HudIconRenderer?.RenderIcon(nameLabel, hint);

            if (ValheimForesightPlugin.InstanceDebugHudEnabled)
                AppendDebugInfo(nameLabel, holder.originalName, assessment);
            else
                nameLabel.text = holder.originalName;
        }
    }

    private static void ColorizeByThreatLevel(TextMeshProUGUI nameLabel, ThreatLevel level)
    {
        nameLabel.color = level switch
        {
            ThreatLevel.Safe => SafeColor,
            ThreatLevel.Caution => CautionColor,
            ThreatLevel.BlockLethal => BlockLethalColor,
            ThreatLevel.Danger => DangerColor,
            _ => SafeColor,
        };
    }

    private static void AppendDebugInfo(
        TextMeshProUGUI nameLabel,
        string originalName,
        ThreatAssessment assessment
    )
    {
        var mode = assessment.UsedRangedAttack ? "R" : "M";
        var levelCode = assessment.Level switch
        {
            ThreatLevel.Safe => "SAFE",
            ThreatLevel.Caution => "CAUT",
            ThreatLevel.BlockLethal => "BLCK",
            ThreatLevel.Danger => "DNG",
            _ => "UNK",
        };

        nameLabel.text =
            originalName
            + $" [{levelCode}-{mode} "
            + $"r={assessment.DamageToHealthRatio:F2} "
            + $"raw={assessment.DamageInfo.RawDamage:F1} "
            + $"eff={assessment.DamageInfo.EffectiveDamageWithBlock:F1}]";
    }

    private sealed class OriginalNameHolder : MonoBehaviour
    {
        public string originalName = string.Empty;
    }
}

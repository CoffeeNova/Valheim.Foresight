using HarmonyLib;

namespace Valheim.Foresight.HarmonyRefs;

/// <summary>
/// Provides reflection access to private MonsterAI fields using Harmony
/// </summary>
public static class MonsterAIFieldRefs
{
    private const string CharacterFieldName = "m_character";
    private const string AnimatorFieldName = "m_animator";
    private const string TargetCreatureFieldName = "m_targetCreature";
    private const string TimeSinceAttackingFieldName = "m_timeSinceAttacking";

    public static readonly AccessTools.FieldRef<MonsterAI, Character?>? CharacterRef;
    public static readonly AccessTools.FieldRef<MonsterAI, ZSyncAnimation?>? AnimatorRef;
    public static readonly AccessTools.FieldRef<MonsterAI, Character?>? TargetCreatureRef;
    public static readonly AccessTools.FieldRef<MonsterAI, float>? TimeSinceAttackingRef;

    static MonsterAIFieldRefs()
    {
        var characterField = AccessTools.Field(typeof(BaseAI), CharacterFieldName);
        if (characterField != null)
        {
            CharacterRef = AccessTools.FieldRefAccess<MonsterAI, Character?>(characterField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field BaseAI.{CharacterFieldName} not found via Harmony reflection"
            );
            CharacterRef = null;
        }

        var animatorField = AccessTools.Field(typeof(BaseAI), AnimatorFieldName);
        if (animatorField != null)
        {
            AnimatorRef = AccessTools.FieldRefAccess<MonsterAI, ZSyncAnimation?>(animatorField);
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field BaseAI.{AnimatorFieldName} not found via Harmony reflection"
            );
            AnimatorRef = null;
        }

        var targetCreatureField = AccessTools.Field(typeof(MonsterAI), TargetCreatureFieldName);
        if (targetCreatureField != null)
        {
            TargetCreatureRef = AccessTools.FieldRefAccess<MonsterAI, Character?>(
                targetCreatureField
            );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field MonsterAI.{TargetCreatureFieldName} not found via Harmony reflection"
            );
            TargetCreatureRef = null;
        }

        var timeSinceAttackingField = AccessTools.Field(
            typeof(MonsterAI),
            TimeSinceAttackingFieldName
        );
        if (timeSinceAttackingField != null)
        {
            TimeSinceAttackingRef = AccessTools.FieldRefAccess<MonsterAI, float>(
                timeSinceAttackingField
            );
        }
        else
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Field MonsterAI.{TimeSinceAttackingFieldName} not found via Harmony reflection"
            );
            TimeSinceAttackingRef = null;
        }
    }
}

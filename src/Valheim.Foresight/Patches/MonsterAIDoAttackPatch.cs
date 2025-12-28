using System.Collections;
using HarmonyLib;
using UnityEngine;
using Valheim.Foresight.Core;
using Valheim.Foresight.HarmonyRefs;

namespace Valheim.Foresight.Patches;

/// <summary>
/// Harmony patch for MonsterAI.DoAttack to track attack timing and display castbars.
/// Only used in solo/local games. In multiplayer, CharacterFixedUpdatePatch is used instead.
/// </summary>
// [HarmonyPatch(typeof(MonsterAI), "DoAttack")]
internal class MonsterAIDoAttackPatch
{
    /// <summary>
    /// Prefix patch that captures attack start and initiates timing tracking
    /// </summary>
    public static void Prefix(MonsterAI __instance, Character target, bool isFriend)
    {
        // Skip this patch when connected to a multiplayer server
        // In multiplayer, use CharacterFixedUpdatePatch instead for better synchronization
        if (NetworkHelper.IsConnectedToServer())
            return;

        if (target != Player.m_localPlayer)
            return;

        var character = MonsterAIFieldRefs.CharacterRef?.Invoke(__instance);
        if (character is not Humanoid humanoid)
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"DOAttack character is not Humanoid humanoid: {character?.GetType()}, value: {character}"
            );
            return;
        }

        var weapon = humanoid.GetCurrentWeapon();
        var attack = weapon?.m_shared.m_attack;
        if (attack == null)
        {
            ValheimForesightPlugin.Log.LogDebug("DOAttack weapon?.m_shared.m_attack;");
            return;
        }

        var attackStartTime = Time.time;
        var shouldIgnoreAttack = ValheimForesightPlugin.AttackTimingService?.ShouldIgnoreAttack(
            character,
            attack
        );

        if (shouldIgnoreAttack == true)
        {
            ValheimForesightPlugin.Log?.LogDebug(
                $"[{nameof(MonsterAIDoAttackPatch)}] Ignoring attack "
                    + $"{character.m_name}::{attack.m_attackAnimation} (in ignore list)"
            );
            return;
        }
        ValheimForesightPlugin.Log.LogDebug("DOAttack before GetOverriddenDuration!");
        var overriddenDuration = ValheimForesightPlugin.AttackTimingService?.GetOverriddenDuration(
            character,
            attack
        );

        if (overriddenDuration.HasValue)
        {
            AnimationHelper.TriggerParryIndicator(
                character,
                attack,
                overriddenDuration.Value,
                0,
                attackStartTime,
                attack.m_attackAnimation
            );

            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(MonsterAIDoAttackPatch)}] Using overridden duration {overriddenDuration.Value:F2}s "
                    + $"for {character.m_name}::{attack.m_attackAnimation}"
            );

            return;
        }
        ValheimForesightPlugin.Log.LogDebug("DOAttack StartCoroutine");
        character.StartCoroutine(
            AnimationDurationCoroutine(__instance, character, attack, attackStartTime)
        );
    }

    private static IEnumerator AnimationDurationCoroutine(
        MonsterAI monster,
        Character attacker,
        Attack attack,
        float attackStartTime
    )
    {
        const int maxAttempts = 30;
        AnimationHelper.AnimationDurationResult? animationResult = null;

        var animator = AnimationHelper.GetAnimatorSafe(attacker);
        if (animator == null)
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"[{nameof(AnimationDurationCoroutine)}] No animator found for {attacker.m_name}"
            );
            yield break;
        }

        var framesSkipped = 0;
        for (var i = 0; i < maxAttempts; i++)
        {
            animationResult = AnimationHelper.GetNextAttackAnimationDuration(
                animator,
                attacker,
                attack,
                monster.name
            );
            if (animationResult.HasValue)
            {
                framesSkipped = i;
                break;
            }

            animationResult = AnimationHelper.GetCurrentAttackAnimationDuration(
                animator,
                attacker,
                attack,
                monster.name
            );
            if (animationResult.HasValue)
            {
                framesSkipped = i;
                break;
            }

            yield return null;
        }

        if (animationResult.HasValue)
        {
            var elapsedTime = Time.time - attackStartTime;
            var adjustedDuration = Mathf.Max(0.01f, animationResult.Value.Duration - elapsedTime);
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(AnimationDurationCoroutine)}] Original duration: {animationResult.Value.Duration:F3}s, "
                    + $"elapsed time: {elapsedTime:F3}s ({framesSkipped} frames), "
                    + $"adjusted duration: {adjustedDuration:F3}s"
            );
            AnimationHelper.TriggerParryIndicator(
                attacker,
                attack,
                adjustedDuration,
                framesSkipped,
                attackStartTime,
                animationResult.Value.AnimationName
            );
        }
        else
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(AnimationDurationCoroutine)}] Failed to get animation duration for {attacker.m_name} after {maxAttempts} attempts"
            );
        }
    }
}

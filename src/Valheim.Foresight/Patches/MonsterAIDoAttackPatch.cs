using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Valheim.Foresight.Core;
using Valheim.Foresight.HarmonyRefs;

namespace Valheim.Foresight.Patches;

/// <summary>
/// Harmony patch for MonsterAI.DoAttack to track attack timing and display castbars
/// </summary>
[HarmonyPatch(typeof(MonsterAI), "DoAttack")]
internal class MonsterAIDoAttackPatch
{
    private readonly struct AnimationDurationResult
    {
        public AnimationDurationResult(float duration, string? animationName)
        {
            Duration = duration;
            AnimationName = animationName;
        }

        public float Duration { get; }
        public string? AnimationName { get; }
    }

    /// <summary>
    /// Prefix patch that captures attack start and initiates timing tracking
    /// </summary>
    static void Prefix(MonsterAI __instance, Character target, bool isFriend)
    {
        if (target != Player.m_localPlayer)
            return;

        var character = MonsterAIFieldRefs.CharacterRef?.Invoke(__instance);
        if (character is not Humanoid humanoid)
            return;

        var weapon = humanoid.GetCurrentWeapon();
        var attack = weapon?.m_shared.m_attack;
        if (attack == null)
            return;

        var attackStartTime = Time.time;

        var shouldIgnoreAttack = ValheimForesightPlugin.AttackTimingService?.ShouldIgnoreAttack(
            character,
            attack
        );

        if (shouldIgnoreAttack == true)
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(MonsterAIDoAttackPatch)}] Ignoring attack "
                    + $"{character.m_name}::{attack.m_attackAnimation} (in ignore list)"
            );
            return;
        }

        var overriddenDuration = ValheimForesightPlugin.AttackTimingService?.GetOverriddenDuration(
            character,
            attack
        );

        if (overriddenDuration.HasValue)
        {
            TriggerParryIndicator(
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
        AnimationDurationResult? animationResult = null;

        var framesSkipped = 0;
        for (var i = 0; i < maxAttempts; i++)
        {
            animationResult = GetNextAttackAnimationDuration(monster, attack);
            if (animationResult.HasValue)
            {
                framesSkipped = i;
                break;
            }

            animationResult = GetCurrentAttackAnimationDuration(monster, attack);
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
            TriggerParryIndicator(
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
            ValheimForesightPlugin.Log.LogWarning(
                $"[{nameof(AnimationDurationCoroutine)}] Failed to get animation duration for {attacker.m_name} after {maxAttempts} attempts"
            );
        }
    }

    private static AnimationDurationResult? GetCurrentAttackAnimationDuration(
        MonsterAI monster,
        Attack attack
    )
    {
        var character = MonsterAIFieldRefs.CharacterRef?.Invoke(monster);
        if (!character)
            return null;

        var animator = GetAnimatorSafe(character);
        if (!animator || animator.layerCount <= 0)
            return null;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        var animationName = AnimatorMethodRefs.ResolveHashRef?.Invoke(
            animator,
            stateInfo.shortNameHash
        );

        // comment out
        // var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        // ValheimForesightPlugin.Log.LogDebug(
        //     $"[CURRENT CLIPS] "
        //         + $"{string.Join(" |", clipInfo
        //         .Select(x => $"name:{x.clip.name}, length:{x.clip.length}"))}"
        // );

        ValheimForesightPlugin.Log.LogDebug(
            $"[{nameof(GetCurrentAttackAnimationDuration)}] "
                + $"moster_name: {monster.name}, "
                + $"m_attackAnimation: {attack.m_attackAnimation}, "
                + $"animationName: {animationName}, length: {stateInfo.length}, normalizedTime: {stateInfo.normalizedTime} "
                + $"loop: {stateInfo.loop}, speed: {stateInfo.speed}, speedMultiplier: {stateInfo.speedMultiplier}, "
                + $"animator.speed: {animator.speed}"
        );

        if (!AlignAndCompareNames(monster, attack, animationName))
            return null;

        var totalSpeed = animator.speed * stateInfo.speed * stateInfo.speedMultiplier;
        var duration = stateInfo.length / Mathf.Max(0.01f, totalSpeed);

        return new AnimationDurationResult(duration, animationName);
    }

    private static AnimationDurationResult? GetNextAttackAnimationDuration(
        MonsterAI monster,
        Attack attack
    )
    {
        var character = MonsterAIFieldRefs.CharacterRef?.Invoke(monster);
        if (!character)
            return null;

        var animator = GetAnimatorSafe(character);
        if (!animator || animator.layerCount <= 0)
            return null;

        var stateInfo = animator.GetNextAnimatorStateInfo(0);
        var animationName = AnimatorMethodRefs.ResolveHashRef?.Invoke(
            animator,
            stateInfo.shortNameHash
        );

        // uncomment out
        // for (var i = 0; i < animator.layerCount; i++)
        // {
        //     var clipInfo = animator.GetNextAnimatorClipInfo(i);
        //     ValheimForesightPlugin.Log.LogDebug(
        //         $"[NEXT CLIPS] layer: {i} "
        //             + $"{string.Join(" |", clipInfo
        //             .Select(x => $"name:{x.clip.name}, length:{x.clip.length}"))}"
        //     );
        // }

        ValheimForesightPlugin.Log.LogDebug(
            $"[{nameof(GetNextAttackAnimationDuration)}] "
                + $"moster_name: {monster.name}, "
                + $"m_attackAnimation: {attack.m_attackAnimation}, "
                + $"animationName: {animationName}, length: {stateInfo.length}, normalizedTime: {stateInfo.normalizedTime} "
                + $"loop: {stateInfo.loop}, speed: {stateInfo.speed}, speedMultiplier: {stateInfo.speedMultiplier}, "
                + $"animator.speed: {animator.speed}"
        );

        if (!AlignAndCompareNames(monster, attack, animationName))
            return null;

        var totalSpeed =
            animator.speed
            /** stateInfo.speed*/* stateInfo.speedMultiplier;
        var duration = stateInfo.length / Mathf.Max(0.01f, totalSpeed);

        return new AnimationDurationResult(duration, animationName);
    }

    private static bool AlignAndCompareNames(
        MonsterAI monster,
        Attack attack,
        string? stateInfoAnimationName
    )
    {
        if (string.IsNullOrEmpty(stateInfoAnimationName))
            return false;

        var character = MonsterAIFieldRefs.CharacterRef?.Invoke(monster);
        if (character == null)
            return false;

        var attackAnimationName = attack.m_attackAnimation;
        if (string.IsNullOrEmpty(attackAnimationName))
            return false;

        var prefabName = character.GetPrefabName();
        var mappedNames = ValheimForesightPlugin.AttackOverridesConfig?.GetMappedAnimationNames(
            prefabName,
            attackAnimationName!
        );

        if (mappedNames is { Count: > 0 })
        {
            var normalizedState = stateInfoAnimationName!
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);

            foreach (var mappedName in mappedNames)
            {
                var normalizedMapped = mappedName
                    .Replace("_", string.Empty)
                    .Replace(" ", string.Empty);

                if (
                    string.Equals(
                        normalizedMapped,
                        normalizedState,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        var normalizedAttack = attackAnimationName!
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        var normalizedStateAnimation = stateInfoAnimationName!
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        var namesAreEqual = string.Equals(normalizedAttack, normalizedStateAnimation);

        if (!namesAreEqual)
        {
            namesAreEqual = normalizedStateAnimation.StartsWith(normalizedAttack);
        }

        if (!namesAreEqual)
        {
            namesAreEqual = normalizedStateAnimation.Contains(normalizedAttack);
        }

        if (!namesAreEqual)
        {
            namesAreEqual = normalizedAttack.Contains(normalizedStateAnimation);
        }

        // Final attempt: similarity comparison using Levenshtein distance
        if (!namesAreEqual)
        {
            namesAreEqual = StringSimilarity.AreSimilar(normalizedAttack, normalizedStateAnimation);
            ValheimForesightPlugin.Log.LogDebug(
                $"[StringSimilarity.AreSimilar]: {namesAreEqual}. "
                    + $"Comparing '{normalizedAttack}' and '{normalizedStateAnimation}'"
            );
        }

        return namesAreEqual;
    }

    private static Animator? GetAnimatorSafe(Component root)
    {
        return root.GetComponentsInChildren<Animator>(true).FirstOrDefault();
    }

    private static void TriggerParryIndicator(
        Character attacker,
        Attack attack,
        float duration,
        int framesSkiped,
        float attackStartTime,
        string? animationName
    )
    {
        var predictedHitTime = ValheimForesightPlugin.AttackTimingService?.GetPredictedHitTime(
            attacker,
            attack
        );

        var hideParryIndicator =
            ValheimForesightPlugin.AttackTimingService?.ShouldHideParryIndicator(attacker, attack)
            ?? false;

        ValheimForesightPlugin.ActiveAttackTracker?.RegisterAttack(
            attacker,
            attack,
            duration,
            attackStartTime,
            predictedHitTime,
            animationName,
            hideParryIndicator
        );

        ValheimForesightPlugin.Log.LogDebug(
            $"[{nameof(TriggerParryIndicator)}] Enemy {attacker.m_name} started attack, duration: {duration:F2}s, "
                + $"animation: {attack.m_attackAnimation}, resolvedAnimation: {animationName ?? "null"}, "
                + $"frames skiped: {framesSkiped}, predicted_hit={predictedHitTime}, "
                + $"hide_parry: {hideParryIndicator}"
        );
    }
}

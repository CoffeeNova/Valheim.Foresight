using System.Linq;
using UnityEngine;
using Valheim.Foresight.Core;
using Valheim.Foresight.HarmonyRefs;

namespace Valheim.Foresight.Patches;

/// <summary>
/// Helper class for animation-related operations shared across patches
/// </summary>
internal static class AnimationHelper
{
    /// <summary>
    /// Compares animation names using various normalization strategies
    /// </summary>
    public static bool AlignAndCompareNames(
        Character character,
        Attack attack,
        string? stateInfoAnimationName
    )
    {
        if (string.IsNullOrEmpty(stateInfoAnimationName))
            return false;

        var attackAnimationName = attack.m_attackAnimation;
        if (string.IsNullOrEmpty(attackAnimationName))
            return false;

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
                $"[{nameof(StringSimilarity.AreSimilar)}]: {namesAreEqual}. "
                    + $"Comparing '{normalizedAttack}' and '{normalizedStateAnimation}'"
            );
        }

        return namesAreEqual;
    }

    /// <summary>
    /// Triggers parry indicator for an attack
    /// </summary>
    public static void TriggerParryIndicator(
        Character attacker,
        Attack attack,
        float duration,
        int framesSkipped,
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
                + $"name: {attack.m_attackAnimation}, resolvedAnimation: {animationName ?? "null"}, "
                + $"frames skipped: {framesSkipped}, predicted_hit={predictedHitTime}, "
                + $"hide_parry: {hideParryIndicator}"
        );
    }

    /// <summary>
    /// Gets animator from character safely
    /// </summary>
    public static Animator? GetAnimatorSafe(Character character)
    {
        return character.GetComponentsInChildren<Animator>(true).FirstOrDefault();
    }

    /// <summary>
    /// Gets current attack animation duration from animator
    /// </summary>
    public static AnimationDurationResult? GetCurrentAttackAnimationDuration(
        Animator animator,
        Character character,
        Attack attack,
        string monsterName
    )
    {
        if (animator == null || animator.layerCount <= 0)
            return null;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        var animationName = AnimatorMethodRefs.ResolveHashRef?.Invoke(
            animator,
            stateInfo.shortNameHash
        );

        if (!AlignAndCompareNames(character, attack, animationName))
            return null;

        // comment out
        // var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        // ValheimForesightPlugin.Log.LogDebug(
        //     $"[CURRENT CLIPS] "
        //         + $"{string.Join(" |", clipInfo
        //         .Select(x => $"name:{x.clip.name}, length:{x.clip.length}"))}"
        // );

        var totalSpeed = animator.speed * stateInfo.speedMultiplier;
        var duration = stateInfo.length / Mathf.Max(0.01f, totalSpeed);

        ValheimForesightPlugin.Log.LogDebug(
            $"[{nameof(GetCurrentAttackAnimationDuration)}] "
                + $"moster_name: {monsterName}, "
                + $"m_attackAnimation: {attack.m_attackAnimation}, "
                + $"animationName: {animationName}, length: {stateInfo.length}, normalizedTime: {stateInfo.normalizedTime} "
                + $"loop: {stateInfo.loop}, speed: {stateInfo.speed}, speedMultiplier: {stateInfo.speedMultiplier}, "
                + $"animator.speed: {animator.speed}"
        );

        return new AnimationDurationResult(duration, animationName);
    }

    /// <summary>
    /// Gets current attack animation duration from animator
    /// </summary>
    public static AnimationDurationResult? GetCurrentAttackAnimationDuration(Animator animator)
    {
        if (animator == null || animator.layerCount <= 0)
            return null;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        var animationName = AnimatorMethodRefs.ResolveHashRef?.Invoke(
            animator,
            stateInfo.shortNameHash
        );

        if (animationName == null)
            return null;

        var totalSpeed = animator.speed * stateInfo.speedMultiplier;
        var duration = stateInfo.length / Mathf.Max(0.01f, totalSpeed);

        return new AnimationDurationResult(duration, animationName);
    }

    public static AnimationDurationResult? GetNextAttackAnimationDuration(Animator animator)
    {
        if (animator == null || animator.layerCount <= 0)
            return null;

        var stateInfo = animator.GetNextAnimatorStateInfo(0);
        var animationName = AnimatorMethodRefs.ResolveHashRef?.Invoke(
            animator,
            stateInfo.shortNameHash
        );

        if (animationName == null)
            return null;

        var totalSpeed = animator.speed * stateInfo.speedMultiplier;
        var duration = stateInfo.length / Mathf.Max(0.01f, totalSpeed);

        return new AnimationDurationResult(duration, animationName);
    }

    /// <summary>
    /// Gets next attack animation duration from animator
    /// </summary>
    public static AnimationDurationResult? GetNextAttackAnimationDuration(
        Animator animator,
        Character character,
        Attack attack,
        string monsterName
    )
    {
        if (animator == null || animator.layerCount <= 0)
            return null;

        var stateInfo = animator.GetNextAnimatorStateInfo(0);
        var animationName = AnimatorMethodRefs.ResolveHashRef?.Invoke(
            animator,
            stateInfo.shortNameHash
        );

        if (!AlignAndCompareNames(character, attack, animationName))
            return null;

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

        var totalSpeed = animator.speed * stateInfo.speedMultiplier;
        var duration = stateInfo.length / Mathf.Max(0.01f, totalSpeed);

        ValheimForesightPlugin.Log.LogDebug(
            $"[{nameof(GetNextAttackAnimationDuration)}] "
                + $"moster_name: {monsterName}, "
                + $"m_attackAnimation: {attack.m_attackAnimation}, "
                + $"animationName: {animationName}, length: {stateInfo.length}, normalizedTime: {stateInfo.normalizedTime} "
                + $"loop: {stateInfo.loop}, speed: {stateInfo.speed}, speedMultiplier: {stateInfo.speedMultiplier}, "
                + $"animator.speed: {animator.speed}"
        );

        return new AnimationDurationResult(duration, animationName);
    }

    /// <summary>
    /// Result of animation duration calculation
    /// </summary>
    public readonly struct AnimationDurationResult
    {
        public AnimationDurationResult(float duration, string? animationName)
        {
            Duration = duration;
            AnimationName = animationName;
        }

        public float Duration { get; }
        public string? AnimationName { get; }
    }
}

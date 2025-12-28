using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Valheim.Foresight.HarmonyRefs;

namespace Valheim.Foresight.Patches;

/// <summary>
/// Monitor animator state changes to detect attacks on multiplayer clients.
/// </summary>
internal class CharacterFixedUpdatePatch
{
    private static readonly Dictionary<int, AnimatorStateInfo> LastStates = new();
    private static Dictionary<int, string?> _animationTagDictionary = new();

    static void Postfix(Character __instance)
    {
        if (__instance.IsPlayer() || __instance.IsDead())
            return;

        var animator = __instance.GetComponentInChildren<Animator>();
        if (animator == null)
            return;

        var instanceId = __instance.GetInstanceID();
        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (!_animationTagDictionary.TryGetValue(currentState.tagHash, out var tag))
        {
            tag = AnimatorMethodRefs.ResolveHashRef?.Invoke(animator, currentState.tagHash);
            _animationTagDictionary.Add(currentState.tagHash, tag);
        }

        if (tag is not "attack")
        {
            LastStates.Remove(instanceId);
            return;
        }

        var attackStartTime = Time.time;
        if (LastStates.TryGetValue(instanceId, out var lastState))
        {
            if (lastState.shortNameHash == currentState.shortNameHash)
                return;
        }

        LastStates[instanceId] = currentState;

        __instance.StartCoroutine(
            TryDetectAndRegisterAttack(__instance, animator, attackStartTime)
        );
    }

    private static IEnumerator TryDetectAndRegisterAttack(
        Character character,
        Animator animator,
        float attackStartTime
    )
    {
        const int maxAttempts = 30;
        Attack? attack = null;

        var framesSkipped = 0;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            framesSkipped = attempt;
            if (character is Humanoid humanoid)
            {
                var weapon = humanoid.GetCurrentWeapon();
                attack = weapon?.m_shared.m_attack;

                if (attack == null)
                {
                    var randomSets = humanoid.m_randomSets;
                    if (randomSets is { Length: > 0 })
                    {
                        foreach (var set in randomSets)
                        {
                            if (set == null)
                                continue;

                            var itemsField = AccessTools.Field(set.GetType(), "m_items");
                            var items = itemsField?.GetValue(set) as GameObject[];

                            if (items == null || items.Length == 0)
                                continue;

                            foreach (var itemPrefab in items)
                            {
                                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                                if (itemDrop?.m_itemData?.m_shared?.m_attack != null)
                                    attack = itemDrop.m_itemData.m_shared.m_attack;
                            }
                        }
                    }
                }

                if (attack != null)
                    break;
            }

            yield return null;
        }

        if (attack == null)
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(CharacterFixedUpdatePatch)}] No attack object found for {character.m_name}, using basic tracking"
            );
        }
        else
        {
            var shouldIgnoreAttack = ValheimForesightPlugin.AttackTimingService?.ShouldIgnoreAttack(
                character,
                attack
            );

            if (shouldIgnoreAttack == true)
            {
                ValheimForesightPlugin.Log.LogDebug(
                    $"[{nameof(MonsterAIDoAttackPatch)}] Ignoring attack "
                        + $"{character.m_name}::{attack?.m_attackAnimation} (in ignore list)"
                );
                yield break;
            }

            var overriddenDuration =
                ValheimForesightPlugin.AttackTimingService?.GetOverriddenDuration(
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

                yield break;
            }
        }

        var animationStateTag = "CURRENT";
        var animationResult = AnimationHelper.GetCurrentAttackAnimationDuration(animator);

        ValheimForesightPlugin.Log.LogDebug(
            $"[{nameof(TryDetectAndRegisterAttack)}] detected {animationStateTag} attack animation "
                + $"{animationResult?.AnimationName} for {character.m_name}"
        );

        if (!animationResult.HasValue)
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(CharacterFixedUpdatePatch)}] Failed to get animation duration for {character.m_name}"
            );
            yield break;
        }

        var shouldIgnoreAnimation =
            ValheimForesightPlugin.AttackTimingService?.ShouldIgnoreAnimation(
                character,
                animationResult.Value.AnimationName
            );

        if (shouldIgnoreAnimation == true)
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(MonsterAIDoAttackPatch)}] Ignoring animation "
                    + $"{character.m_name}::{animationResult?.AnimationName} (in ignore list)"
            );

            yield break;
        }

        var elapsedTime = Time.time - attackStartTime;
        var adjustedDuration = Mathf.Max(0.01f, animationResult.Value.Duration - elapsedTime);

        if (attack != null)
        {
            ValheimForesightPlugin.Log.LogDebug(
                $"[{nameof(CharacterFixedUpdatePatch)}] Attack detected: "
                    + $"char_name:{character.m_name}, attack_name: {attack.m_attackAnimation}, duration: {adjustedDuration:F3}s, "
                    + $"elapsed: {elapsedTime:F3}s, animation: {animationResult.Value.AnimationName}"
            );

            AnimationHelper.TriggerParryIndicator(
                character,
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
                $"[{nameof(CharacterFixedUpdatePatch)}] Basic attack detected (no weapon): "
                    + $"{character.m_name}, duration: {adjustedDuration:F3}s, "
                    + $"elapsed: {elapsedTime:F3}s, animation: {animationResult.Value.AnimationName}"
            );

            RegisterBasicAttack(
                character,
                adjustedDuration,
                attackStartTime,
                animationResult.Value.AnimationName
            );
        }
    }

    private static void RegisterBasicAttack(
        Character attacker,
        float duration,
        float attackStartTime,
        string? animationName
    )
    {
        // Create a mock attack for basic tracking without full parry indicator
        // This will show castbar but without detailed parry timing
        ValheimForesightPlugin.ActiveAttackTracker?.RegisterAttack(
            attacker,
            null!,
            duration,
            attackStartTime,
            null,
            animationName,
            true
        );
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valheim.Foresight.Core;
using Valheim.Foresight.HarmonyRefs;

namespace Valheim.Foresight.Patches;

/// <summary>
/// Monitor animator state changes to detect attacks on multiplayer clients.
/// </summary>
internal class CharacterFixedUpdatePatch
{
    private const int MaxAttackFindAttempts = 30;
    
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
        Attack? attack = null;
        var framesSkipped = 0;

        if (character is Humanoid humanoid)
        {
            for (var attempt = 0; attempt < MaxAttackFindAttempts; attempt++)
            {
                attack = TryFindAttack(humanoid);
                
                if (attack != null)
                {
                    framesSkipped = attempt;
                    break;
                }

                yield return null;
            }
        }

        if (attack == null)
        {
            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(CharacterFixedUpdatePatch)}] No attack object found for {character.m_name}, using basic tracking"
            );
        }
        else if (ShouldIgnoreOrOverrideAttack(character, attack, attackStartTime, out var shouldBreak))
        {
            if (shouldBreak)
                yield break;
        }

        var animationResult = AnimationHelper.GetCurrentAttackAnimationDuration(animator);

        ValheimForesightPlugin.Log.LogInfo(
            $"[{nameof(TryDetectAndRegisterAttack)}] detected CURRENT attack animation "
                + $"{animationResult?.AnimationName} for {character.m_name}"
        );

        if (!animationResult.HasValue)
        {
            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(CharacterFixedUpdatePatch)}] Failed to get animation duration for {character.m_name}"
            );
            yield break;
        }

        if (ShouldIgnoreAnimation(character, animationResult.Value.AnimationName))
            yield break;

        var adjustedDuration = CalculateAdjustedDuration(attackStartTime, animationResult.Value.Duration);
        
        RegisterAttack(character, attack, adjustedDuration, framesSkipped, attackStartTime, animationResult.Value);
    }

    private static Attack? TryFindAttack(Humanoid humanoid)
    {
        var weapon = humanoid.GetCurrentWeapon();
        var attack = weapon?.m_shared.m_attack;

        if (attack == null)
        {
            attack = TryGetUnarmedWeaponAttack(humanoid)
                ?? TryGetAttackFromDefaultItems(humanoid)
                ?? TryGetAttackFromRandomSets(humanoid);
        }

        return attack;
    }

    private static Attack? TryGetUnarmedWeaponAttack(Humanoid humanoid)
    {
        var unarmedWeapon = HumanoidFieldRefs.UnarmedWeaponRef?.Invoke(humanoid);
        return unarmedWeapon?.m_itemData?.m_shared?.m_attack;
    }

    private static Attack? TryGetAttackFromDefaultItems(Humanoid humanoid)
    {
        var defaultItems = HumanoidFieldRefs.DefaultItemsRef?.Invoke(humanoid);
        if (defaultItems is not { Length: > 0 })
            return null;

        foreach (var itemPrefab in defaultItems)
        {
            if (itemPrefab == null)
                continue;

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            var attack = itemDrop?.m_itemData?.m_shared?.m_attack;
            
            if (attack != null)
                return attack;
        }

        return null;
    }

    private static Attack? TryGetAttackFromRandomSets(Humanoid humanoid)
    {
        var randomSets = HumanoidFieldRefs.RandomSetsRef?.Invoke(humanoid);
        if (randomSets is not { Length: > 0 })
            return null;

        ValheimForesightPlugin.Log.LogInfo($"Length: {randomSets.Length}");

        foreach (var set in randomSets)
        {
            if (set == null)
                continue;

            var attack = TryGetAttackFromItemSet(set);
            if (attack != null)
                return attack;
        }

        return null;
    }

    private static Attack? TryGetAttackFromItemSet(Humanoid.ItemSet set)
    {
        var items = ItemSetFieldRefs.ItemsRef?.Invoke(set);
        if (items is not { Length: > 0 })
            return null;

        foreach (var itemPrefab in items)
        {
            if (itemPrefab == null)
                continue;

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            var attack = itemDrop?.m_itemData?.m_shared?.m_attack;
            
            if (attack != null)
                return attack;
        }

        return null;
    }

    private static bool ShouldIgnoreOrOverrideAttack(
        Character character,
        Attack attack,
        float attackStartTime,
        out bool shouldBreak
    )
    {
        shouldBreak = false;

        var shouldIgnoreAttack = ValheimForesightPlugin.AttackTimingService?.ShouldIgnoreAttack(
            character,
            attack
        );

        if (shouldIgnoreAttack == true)
        {
            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(MonsterAIDoAttackPatch)}] Ignoring attack "
                    + $"{character.m_name}::{attack?.m_attackAnimation} (in ignore list)"
            );
            shouldBreak = true;
            return true;
        }

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

            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(MonsterAIDoAttackPatch)}] Using overridden duration {overriddenDuration.Value:F2}s "
                    + $"for {character.m_name}::{attack.m_attackAnimation}"
            );

            shouldBreak = true;
            return true;
        }

        return false;
    }

    private static bool ShouldIgnoreAnimation(Character character, string? animationName)
    {
        var shouldIgnore = ValheimForesightPlugin.AttackTimingService?.ShouldIgnoreAnimation(
            character,
            animationName
        );

        if (shouldIgnore == true)
        {
            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(MonsterAIDoAttackPatch)}] Ignoring animation "
                    + $"{character.m_name}::{animationName} (in ignore list)"
            );
            return true;
        }

        return false;
    }

    private static float CalculateAdjustedDuration(float attackStartTime, float animationDuration)
    {
        var elapsedTime = Time.time - attackStartTime;
        return Mathf.Max(0.01f, animationDuration - elapsedTime);
    }

    private static void RegisterAttack(
        Character character,
        Attack? attack,
        float adjustedDuration,
        int framesSkipped,
        float attackStartTime,
        AnimationHelper.AnimationDurationResult animationResult
    )
    {
        var elapsedTime = Time.time - attackStartTime;

        if (attack != null)
        {
            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(CharacterFixedUpdatePatch)}] Attack detected: "
                    + $"char_name:{character.m_name}, attack_name: {attack.m_attackAnimation}, duration: {adjustedDuration:F3}s, "
                    + $"elapsed: {elapsedTime:F3}s, animation: {animationResult.AnimationName}"
            );

            AnimationHelper.TriggerParryIndicator(
                character,
                attack,
                adjustedDuration,
                framesSkipped,
                attackStartTime,
                animationResult.AnimationName
            );
        }
        else
        {
            ValheimForesightPlugin.Log.LogInfo(
                $"[{nameof(CharacterFixedUpdatePatch)}] Basic attack detected (no weapon): "
                    + $"{character.m_name}, duration: {adjustedDuration:F3}s, "
                    + $"elapsed: {elapsedTime:F3}s, animation: {animationResult.AnimationName}"
            );

            RegisterBasicAttack(
                character,
                adjustedDuration,
                attackStartTime,
                animationResult.AnimationName
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

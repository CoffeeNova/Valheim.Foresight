using HarmonyLib;
using UnityEngine;

namespace Valheim.Foresight.Patches
{
    /// <summary>
    /// Harmony patch for Character.ApplyDamage to record actual hit timings
    /// </summary>
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal class CharacterApplyDamagePatch
    {
        /// <summary>
        /// Prefix patch that records hit timing for active attacks
        /// </summary>
        static void Prefix(Character __instance, HitData hit)
        {
            if (__instance != Player.m_localPlayer)
                return;

            var attacker = hit.GetAttacker() as Character;
            if (attacker == null)
                return;

            var hitTime = Time.time;

            // Find the active attack from this attacker
            var activeAttack = ValheimForesightPlugin.ActiveAttackTracker?.GetActiveAttack(
                attacker
            );

            if (activeAttack != null)
            {
                // Record the timing
                ValheimForesightPlugin.AttackTimingService?.RecordHit(
                    attacker,
                    activeAttack.Attack,
                    hitTime,
                    activeAttack.StartTime
                );

                var hitOffset = hitTime - activeAttack.StartTime;
                var normalized = hitOffset / activeAttack.Duration;

                ValheimForesightPlugin.Log?.LogDebug(
                    $"[Hit] {attacker.m_name} | {activeAttack.AttackName} | "
                        + $"offset={hitOffset:F3}s ({normalized:P0}) | dmg={hit.GetTotalDamage():F1}"
                );
            }
            else
            {
                ValheimForesightPlugin.Log?.LogDebug(
                    $"[Hit] {attacker.m_name} hit player but no active attack tracked (dmg={hit.GetTotalDamage():F1})"
                );
            }
        }
    }
}

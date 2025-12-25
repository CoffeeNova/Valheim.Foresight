using System;
using HarmonyLib;
using UnityEngine;

namespace Valheim.Foresight.HarmonyRefs;

/// <summary>
/// Provides reflection access to Animator methods using Harmony
/// </summary>
public static class AnimatorMethodRefs
{
    private const string ResolveHashMethodName = "ResolveHash";
    public static readonly Func<Animator, int, string?>? ResolveHashRef;

    static AnimatorMethodRefs()
    {
        var resolveHashMethod = AccessTools.Method(typeof(Animator), ResolveHashMethodName);
        if (resolveHashMethod != null)
        {
            ResolveHashRef = (Func<Animator, int, string?>?)
                Delegate.CreateDelegate(
                    typeof(Func<Animator, int, string?>),
                    resolveHashMethod,
                    throwOnBindFailure: false
                );
        }

        if (ResolveHashRef == null)
        {
            ValheimForesightPlugin.Log?.LogWarning(
                $"Method Animator.{ResolveHashMethodName} not found via Harmony reflection"
            );
        }
    }
}

using System;
using UnityEngine;
using Valheim.Foresight.Configuration;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Castbar.Interfaces;

namespace Valheim.Foresight.Services.Castbar;

/// <summary>
/// Implements parry window calculation logic
/// </summary>
public sealed class ParryWindowService : IParryWindowService
{
    private readonly IForesightConfiguration _config;

    public ParryWindowService(IForesightConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public bool IsInParryWindow(ActiveAttackInfo attackInfo)
    {
        if (attackInfo == null)
            throw new ArgumentNullException(nameof(attackInfo));

        if (attackInfo.PredictedHitTime == null)
            return false;

        var currentTime = Time.time;
        var elapsedTime = currentTime - attackInfo.StartTime;
        var hitTime = attackInfo.PredictedHitTime.Value;

        var windowBefore = _config.AttackCastbarParryWindow.Value;
        const float windowAfter = 0.1f;

        return elapsedTime >= (hitTime - windowBefore)
            && elapsedTime <= (hitTime + windowAfter)
            && !attackInfo.IsExpired;
    }

    public (float startPosition, float width)? GetParryWindowInfo(
        ActiveAttackInfo attackInfo,
        float castbarDuration
    )
    {
        if (attackInfo == null)
            throw new ArgumentNullException(nameof(attackInfo));

        if (attackInfo.HideParryIndicator || attackInfo.PredictedHitTime == null)
            return null;

        const float windowAfter = 0.1f;
        var windowBefore = _config.AttackCastbarParryWindow.Value;
        var windowSize = windowBefore + windowAfter;

        var parryStartTime = attackInfo.PredictedHitTime.Value - windowBefore;
        var startPosition = Mathf.Clamp01(parryStartTime / castbarDuration);
        var width = Mathf.Clamp01(windowSize / castbarDuration);

        return (startPosition, width);
    }
}

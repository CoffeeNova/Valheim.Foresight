using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim.Foresight.Models;

/// <summary>
/// Represents an active enemy attack with timing and prediction information
/// </summary>
public sealed class ActiveAttackInfo
{
    public Character Attacker { get; }
    public Attack Attack { get; }
    public float StartTime { get; }
    public float Duration { get; }
    public string AttackName { get; }
    public string? AnimationName { get; }
    public float? PredictedHitTime { get; }
    public bool HideParryIndicator { get; }

    /// <summary>
    /// Creates a new active attack info
    /// </summary>
    public ActiveAttackInfo(
        Character attacker,
        Attack attack,
        float duration,
        float startTime,
        float? predictedHitTime,
        string? animationName,
        bool hideParryIndicator = false
    )
    {
        Attacker = attacker;
        Attack = attack;
        StartTime = startTime;
        Duration = duration;
        AnimationName = animationName;
        AttackName = GetAttackDisplayName(attack, animationName);
        PredictedHitTime = predictedHitTime;
        HideParryIndicator = hideParryIndicator;
    }

    public float Progress => Mathf.Clamp01((Time.time - StartTime) / Duration);
    public float TimeRemaining => Mathf.Max(0f, Duration - (Time.time - StartTime));
    public bool IsExpired => Time.time >= StartTime + Duration + 1.0f;

    private static string GetAttackDisplayName(Attack? attack, string? animationName)
    {
        return FormatAttackName(animationName)
            ?? FormatAttackName(attack?.m_attackAnimation)
            ?? "Attack";
    }

    private static string? FormatAttackName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return null;

        var sourceName = rawName!.Trim();

        if (
            ForbiddenWords.Any(word =>
                sourceName.Contains(word, StringComparison.OrdinalIgnoreCase)
            )
        )
            return null;

        var normalized = sourceName
            .Replace("attack", "Attack")
            .Replace("_", " ")
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized))
            return null;

        var cleaned = string.Concat(normalized.Where(c => !char.IsDigit(c))).Trim();
        if (string.IsNullOrEmpty(cleaned))
            return null;

        return cleaned;
    }

    private static readonly HashSet<string> ForbiddenWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "idle",
    };
}

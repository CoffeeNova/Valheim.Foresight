using System;
using System.Collections.Generic;
using System.Linq;

namespace Valheim.Foresight.Services.Hud;

/// <summary>
/// Formats attack names for display in HUD
/// </summary>
public static class AttackNameFormatter
{
    private static readonly HashSet<string> ForbiddenWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "idle",
    };

    /// <summary>
    /// Gets a display name for an attack from animation name or attack object
    /// </summary>
    public static string GetDisplayName(Attack? attack, string? animationName)
    {
        return FormatAttackName(animationName)
            ?? FormatAttackName(attack?.m_attackAnimation)
            ?? "Attack";
    }

    /// <summary>
    /// Formats a raw attack name for display
    /// </summary>
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
}

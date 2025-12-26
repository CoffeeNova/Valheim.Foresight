using System.Collections.Generic;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Castbar.Interfaces;

namespace Valheim.Foresight.Services.Castbar;

/// <summary>
/// Tracks active enemy attacks for castbar display
/// </summary>
public sealed class ActiveAttackTracker : IActiveAttackTracker
{
    private readonly Dictionary<Character, ActiveAttackInfo> _activeAttacks = new();

    /// <inheritdoc/>
    public void RegisterAttack(
        Character attacker,
        Attack attack,
        float duration,
        float startTime,
        float? predictedHitTime,
        string? animationName,
        bool hideParryIndicator
    )
    {
        _activeAttacks[attacker] = new ActiveAttackInfo(
            attacker,
            attack,
            duration,
            startTime,
            predictedHitTime,
            animationName,
            hideParryIndicator
        );
    }

    /// <inheritdoc/>
    public ActiveAttackInfo? GetActiveAttack(Character attacker)
    {
        if (_activeAttacks.TryGetValue(attacker, out var info))
        {
            if (info.IsExpired)
            {
                _activeAttacks.Remove(attacker);
                return null;
            }
            return info;
        }
        return null;
    }

    /// <inheritdoc/>
    public void CleanupExpired()
    {
        var keysToRemove = new List<Character>();
        foreach (var kvp in _activeAttacks)
        {
            if (kvp.Value.IsExpired)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
        {
            _activeAttacks.Remove(key);
        }
    }
}

using System;
using System.Collections.Generic;
using Valheim.Foresight.Models;

public sealed class AttackKeyComparer : IEqualityComparer<AttackKey>
{
    public static readonly AttackKeyComparer Instance = new();

    public bool Equals(AttackKey x, AttackKey y)
    {
        return string.Equals(x.CreaturePrefab, y.CreaturePrefab, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.AttackAnimation, y.AttackAnimation, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(AttackKey obj)
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CreaturePrefab),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.AttackAnimation)
        );
    }
}
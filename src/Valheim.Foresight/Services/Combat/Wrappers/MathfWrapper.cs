using UnityEngine;
using Valheim.Foresight.Services.Combat.Interfaces;

namespace Valheim.Foresight.Services.Combat.Wrappers;

/// <summary>
/// Concrete implementation wrapping Mathf static methods
/// </summary>
public sealed class MathfWrapper : IMathfWrapper
{
    /// <inheritdoc/>
    public int Max(int a, int b)
    {
        return Mathf.Max(a, b);
    }

    /// <inheritdoc/>
    public float Max(float a, float b)
    {
        return Mathf.Max(a, b);
    }
}

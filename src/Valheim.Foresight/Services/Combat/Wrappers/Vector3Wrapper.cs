using UnityEngine;
using Valheim.Foresight.Services.Combat.Interfaces;

namespace Valheim.Foresight.Services.Combat.Wrappers;

/// <summary>
/// Concrete implementation wrapping Vector3 static methods
/// </summary>
public sealed class Vector3Wrapper : IVector3Wrapper
{
    /// <inheritdoc/>
    public float Distance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }
}

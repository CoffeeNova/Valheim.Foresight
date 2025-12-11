using UnityEngine;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Wrapper interface for Vector3 static methods to enable testing
/// </summary>
public interface IVector3Wrapper
{
    /// <summary>
    /// Returns the distance between two points
    /// </summary>
    float Distance(Vector3 a, Vector3 b);
}

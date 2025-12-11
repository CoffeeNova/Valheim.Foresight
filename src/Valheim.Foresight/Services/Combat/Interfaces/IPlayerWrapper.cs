using UnityEngine;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Wrapper interface for Player static methods to enable testing
/// </summary>
public interface IPlayerWrapper
{
    /// <summary>
    /// Gets the count of players within the specified radius on the XZ plane
    /// </summary>
    int GetPlayersInRangeXZ(Vector3 position, float radius);
}

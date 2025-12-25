using UnityEngine;
using Valheim.Foresight.Services.Combat.Interfaces;

namespace Valheim.Foresight.Services.Combat.Wrappers;

/// <summary>
/// Concrete implementation wrapping Player static methods
/// </summary>
public sealed class PlayerWrapper : IPlayerWrapper
{
    /// <inheritdoc/>
    public int GetPlayersInRangeXZ(Vector3 position, float radius)
    {
        return Player.GetPlayersInRangeXZ(position, radius);
    }
}

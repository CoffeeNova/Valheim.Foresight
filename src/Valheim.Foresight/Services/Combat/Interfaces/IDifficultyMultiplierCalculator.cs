using System.Collections.Generic;
using UnityEngine;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Service for calculating difficulty multipliers
/// </summary>
public interface IDifficultyMultiplierCalculator
{
    /// <summary>
    /// Gets the total damage multiplier including world difficulty and player count
    /// </summary>
    float GetDamageMultiplier(Vector3 position);

    /// <summary>
    /// Gets the world difficulty multiplier (Easy/Normal/Hard)
    /// </summary>
    float GetWorldDifficultyMultiplier();

    /// <summary>
    /// Gets the multiplier based on nearby player count
    /// </summary>
    float GetPlayerCountMultiplier(Vector3 position);

    /// <summary>
    /// Counts nearby players within the difficulty radius
    /// </summary>
    int GetNearbyPlayerCount(Vector3 position);

    /// <summary>
    /// Gets the incoming damage factor from global keys
    /// </summary>
    float GetIncomingDamageFactor();

    /// <summary>
    /// Gets the enemy health factor (inverse of player damage)
    /// </summary>
    float GetEnemyHealthFactor();

    /// <summary>
    /// Gets all global keys for debugging
    /// </summary>
    List<string> GetAllGlobalKeys();
}

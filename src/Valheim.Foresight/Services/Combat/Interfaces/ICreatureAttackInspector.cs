namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Service for inspecting creature attack capabilities
/// </summary>
public interface ICreatureAttackInspector
{
    /// <summary>
    /// Gets maximum attack damage by prefab name (e.g., "Troll", "Skeleton")
    /// </summary>
    float GetMaxAttackByPrefabName(string prefabName);

    /// <summary>
    /// Gets maximum attack damage for a specific character instance
    /// </summary>
    float GetMaxAttackForCharacter(Character character);
}

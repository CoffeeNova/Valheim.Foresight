using UnityEngine;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Wrapper interface for ZNetScene to enable testing
/// </summary>
public interface IZNetSceneWrapper
{
    /// <summary>
    /// Gets a prefab by name from the scene
    /// </summary>
    GameObject? GetPrefab(string name);
}

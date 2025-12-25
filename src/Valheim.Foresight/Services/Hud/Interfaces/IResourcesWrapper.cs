using UnityEngine;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Thin wrapper over UnityEngine.Resources for easier testing.
/// </summary>
public interface IResourcesWrapper
{
    /// <summary>
    /// Loads a sprite from the given resource path
    /// </summary>
    Sprite? LoadSprite(string path);
}

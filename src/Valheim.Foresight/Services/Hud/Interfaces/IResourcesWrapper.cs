using UnityEngine;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Thin wrapper over UnityEngine.Resources for easier testing.
/// </summary>
public interface IResourcesWrapper
{
    Sprite? LoadSprite(string path);
}

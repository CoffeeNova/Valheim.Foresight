using UnityEngine;
using Valheim.Foresight.Services.Hud.Interfaces;

namespace Valheim.Foresight.Services.Hud.Wrappers;

/// <summary>
/// Wrapper for Unity Resources API
/// </summary>
public sealed class ResourcesWrapper : IResourcesWrapper
{
    /// <inheritdoc/>
    public Sprite? LoadSprite(string path)
    {
        return Resources.Load<Sprite>(path);
    }
}

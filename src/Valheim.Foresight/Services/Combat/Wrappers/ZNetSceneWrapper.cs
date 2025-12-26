using System;
using UnityEngine;
using Valheim.Foresight.Services.Combat.Interfaces;

namespace Valheim.Foresight.Services.Combat.Wrappers;

/// <summary>
/// Wrapper implementation for ZNetScene to enable testing
/// </summary>
public sealed class ZNetSceneWrapper : IZNetSceneWrapper
{
    private readonly ZNetScene _zNetScene;

    /// <summary>
    /// Creates a new ZNetScene wrapper
    /// </summary>
    public ZNetSceneWrapper(ZNetScene zNetScene)
    {
        _zNetScene = zNetScene ?? throw new ArgumentNullException(nameof(zNetScene));
    }

    /// <inheritdoc/>
    public GameObject? GetPrefab(string name)
    {
        return _zNetScene.GetPrefab(name);
    }
}

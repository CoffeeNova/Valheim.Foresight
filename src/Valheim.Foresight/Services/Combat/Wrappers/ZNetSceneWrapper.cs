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

    public ZNetSceneWrapper(ZNetScene zNetScene)
    {
        _zNetScene = zNetScene ?? throw new ArgumentNullException(nameof(zNetScene));
    }

    public GameObject? GetPrefab(string name)
    {
        return _zNetScene.GetPrefab(name);
    }
}

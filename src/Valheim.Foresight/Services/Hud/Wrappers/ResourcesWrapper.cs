using UnityEngine;
using Valheim.Foresight.Services.Hud.Interfaces;

namespace Valheim.Foresight.Services.Hud.Wrappers;

public sealed class ResourcesWrapper : IResourcesWrapper
{
    public Sprite? LoadSprite(string path)
    {
        return Resources.Load<Sprite>(path);
    }
}

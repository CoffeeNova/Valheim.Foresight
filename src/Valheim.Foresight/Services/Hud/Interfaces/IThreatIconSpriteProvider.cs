using UnityEngine;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Supplies sprites for specific threat response hints.
/// </summary>
public interface IThreatIconSpriteProvider
{
    Sprite? GetIcon(ThreatResponseHint hint);
}

using System;
using UnityEngine;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Supplies sprites for specific threat response hints.
/// </summary>
public interface IThreatIconSpriteProvider : IDisposable
{
    /// <summary>
    /// Gets the sprite icon for the given threat response hint
    /// </summary>
    Sprite? GetIcon(ThreatResponseHint hint);
}

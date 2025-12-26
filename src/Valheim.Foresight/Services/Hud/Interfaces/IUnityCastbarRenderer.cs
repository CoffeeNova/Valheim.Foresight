using System;
using UnityEngine;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Service for rendering attack castbars in Unity UI
/// </summary>
public interface IUnityCastbarRenderer : IDisposable
{
    /// <summary>
    /// Renders or updates the attack castbar on the enemy HUD
    /// </summary>
    void RenderCastbar(
        Transform hudParent,
        ActiveAttackInfo? attackInfo,
        Character? character = null
    );
}

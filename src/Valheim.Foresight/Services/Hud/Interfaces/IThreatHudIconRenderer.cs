using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Draws/updates the threat response icon on a specific enemy HUD.
/// Isolated behind an interface for tests.
/// </summary>
public interface IThreatHudIconRenderer
{
    void RenderIcon(EnemyHud.HudData hud, ThreatResponseHint hint);
}

using TMPro;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Draws/updates the threat response icon on a specific enemy HUD.
/// </summary>
public interface IThreatHudIconRenderer
{
    void RenderIcon(TextMeshProUGUI? hud, ThreatResponseHint hint);
}

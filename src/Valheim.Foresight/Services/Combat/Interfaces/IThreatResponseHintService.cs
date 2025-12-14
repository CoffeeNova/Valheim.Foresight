using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Maps a threat assessment to a recommended player action (block / parry / dodge).
/// Pure logic, no Unity dependencies.
/// </summary>
public interface IThreatResponseHintService
{
    ThreatResponseHint GetHint(ThreatAssessment assessment);
}

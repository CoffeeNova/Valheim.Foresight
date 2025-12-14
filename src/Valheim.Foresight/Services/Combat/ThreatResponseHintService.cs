using System;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Combat.Interfaces;

namespace Valheim.Foresight.Services.Combat;

/// <summary>
/// Default implementation of hint mapping, based only on threat level and ratios.
/// Completely unit-testable.
/// </summary>
public sealed class ThreatResponseHintService : IThreatResponseHintService
{
    // todo: move to config
    private const float HighRiskRatio = 0.7f;

    public ThreatResponseHint GetHint(ThreatAssessment assessment)
    {
        if (assessment == null)
            throw new ArgumentNullException(nameof(assessment));

        switch (assessment.Level)
        {
            case ThreatLevel.Safe:
                return ThreatResponseHint.Block;
            case ThreatLevel.Caution:
                return ThreatResponseHint.Block;
            case ThreatLevel.BlockLethal:
                return ThreatResponseHint.Parry;
            case ThreatLevel.Danger:
                return ThreatResponseHint.Dodge;
            default:
                return ThreatResponseHint.None;
        }
    }
}

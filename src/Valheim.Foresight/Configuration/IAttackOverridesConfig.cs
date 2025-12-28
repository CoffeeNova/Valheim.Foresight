using System.Collections.Generic;
using BepInEx.Configuration;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Configuration interface for attack overrides
/// </summary>
public interface IAttackOverridesConfig
{
    ConfigEntry<string> DurationOverrideList { get; }
    ConfigEntry<string> IgnoreList { get; }
    ConfigEntry<string> NoParryIndicatorList { get; }

    /// <summary>
    /// Gets the overridden duration for a specific attack
    /// </summary>
    float? GetOverriddenDuration(string creaturePrefab, string attackAnimation);

    /// <summary>
    /// Checks if the attack should be ignored (no castbar shown)
    /// </summary>
    bool ShouldIgnoreAttack(string creaturePrefab, string attackAnimation);

    /// <summary>
    /// Checks if the parry indicator should be hidden for this attack (castbar will be shown)
    /// </summary>
    bool ShouldHideParryIndicator(string creaturePrefab, string attackAnimation);

    /// <summary>
    /// Checks if there is an override for the attack
    /// </summary>
    bool HasOverride(string creaturePrefab, string attackAnimation);
}

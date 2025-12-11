namespace Valheim.Foresight.Models;

/// <summary>
/// Threat level classification for enemy attacks
/// </summary>
public enum ThreatLevel
{
    /// <summary>
    /// Player can safely block the attack
    /// </summary>
    Safe,

    /// <summary>
    /// Player will take significant damage but survive
    /// </summary>
    Caution,

    /// <summary>
    /// Player will die if blocking, but survive if parrying
    /// </summary>
    BlockLethal,

    /// <summary>
    /// Player will die even with successful parry
    /// </summary>
    Danger,
}

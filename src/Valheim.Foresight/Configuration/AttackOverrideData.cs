namespace Valheim.Foresight.Configuration;

/// <summary>
/// Attack override data (duration only)
/// </summary>
public readonly struct AttackOverrideData
{
    public readonly float Duration;

    /// <summary>
    /// Creates new attack override data with the specified duration
    /// </summary>
    public AttackOverrideData(float duration)
    {
        Duration = duration;
    }
}

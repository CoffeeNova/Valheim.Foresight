namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Wrapper interface for Mathf static methods to enable testing
/// </summary>
public interface IMathfWrapper
{
    /// <summary>
    /// Returns the larger of two integers
    /// </summary>
    int Max(int a, int b);

    /// <summary>
    /// Returns the larger of two floats
    /// </summary>
    float Max(float a, float b);
}

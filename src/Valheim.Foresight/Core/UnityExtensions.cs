namespace Valheim.Foresight.Core;

/// <summary>
/// Extension methods for Unity and Valheim objects
/// </summary>
public static class UnityExtensions
{
    private const string UnknownKeyName = "unknown";
    private const string PrefabInstanceInficator = "(Clone)";

    /// <summary>
    /// Gets the prefab name from a character, removing instance indicators
    /// </summary>
    public static string GetPrefabName(this Character? character)
    {
        if (character is null)
            return UnknownKeyName;

        var go = character.gameObject;
        var name = go.name.Replace(PrefabInstanceInficator, string.Empty).Trim();

        return string.IsNullOrEmpty(name) ? UnknownKeyName : name;
    }
}

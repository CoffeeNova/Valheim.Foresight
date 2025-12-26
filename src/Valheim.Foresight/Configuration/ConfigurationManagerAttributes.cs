namespace Valheim.Foresight.Configuration;

/// <summary>
/// Attributes for ConfigurationManager (optional)
/// </summary>
internal class ConfigurationManagerAttributes
{
    public int Order { get; set; }

    /// <summary>
    /// Custom drawer for rendering config UI in ConfigurationManager
    /// </summary>
    public System.Action<BepInEx.Configuration.ConfigEntryBase>? CustomDrawer { get; set; }

    public bool IsAdvanced { get; set; }
}

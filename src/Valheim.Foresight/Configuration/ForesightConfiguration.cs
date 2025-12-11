using System;
using BepInEx.Configuration;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Manages all plugin configuration settings
/// </summary>
public sealed class ForesightConfiguration
{
    private readonly ConfigFile _configFile;

    public ConfigEntry<bool> IsLogsEnabled { get; }
    public ConfigEntry<bool> DebugHudEnabled { get; }

    // public ConfigEntry<bool> DetailedAttackMode { get; }

    public event EventHandler? SettingsChanged;

    public ForesightConfiguration(ConfigFile configFile)
    {
        _configFile = configFile ?? throw new ArgumentNullException(nameof(configFile));

        IsLogsEnabled = _configFile.Bind(
            "Debug",
            "IsLogsEnabled",
            true,
            "Enable verbose logging for ValheimForesight (for debugging)."
        );

        DebugHudEnabled = _configFile.Bind(
            "HUD",
            "DebugHudEnabled",
            false,
            "Show debug threat info near enemy name."
        );

        // DetailedAttackMode = _configFile.Bind(
        //     "Threat",
        //     "DetailedAttackMode",
        //     false,
        //     "Use detailed melee/ranged threat calculation instead of simple global indicator."
        // );

        BindConfigChangeHandlers();
    }

    private void BindConfigChangeHandlers()
    {
        IsLogsEnabled.SettingChanged += OnSettingChanged;
        DebugHudEnabled.SettingChanged += OnSettingChanged;
        // DetailedAttackMode.SettingChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}

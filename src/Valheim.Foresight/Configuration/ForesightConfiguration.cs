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

    public ConfigEntry<bool> ThreatIconEnabled { get; }
    public ConfigEntry<float> ThreatIconSize { get; }
    public ConfigEntry<float> ThreatIconOffsetX { get; }
    public ConfigEntry<float> ThreatIconOffsetY { get; }

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

        ThreatIconEnabled = _configFile.Bind(
            "HUD",
            "ThreatIconEnabled",
            true,
            "Show threat response hint icon next to enemy name."
        );

        ThreatIconSize = _configFile.Bind(
            "HUD",
            "ThreatIconSize",
            36f,
            new ConfigDescription(
                "Size of the threat icon in pixels.",
                new AcceptableValueRange<float>(16f, 128f)
            )
        );

        ThreatIconOffsetX = _configFile.Bind(
            "HUD",
            "ThreatIconOffsetX",
            180f,
            new ConfigDescription(
                "Horizontal offset of the threat icon from enemy name (positive = right, negative = left).",
                new AcceptableValueRange<float>(-200f, 200f)
            )
        );

        ThreatIconOffsetY = _configFile.Bind(
            "HUD",
            "ThreatIconOffsetY",
            54f,
            new ConfigDescription(
                "Vertical offset of the threat icon from enemy name (positive = up, negative = down).",
                new AcceptableValueRange<float>(-100f, 100f)
            )
        );

        BindConfigChangeHandlers();
    }

    private void BindConfigChangeHandlers()
    {
        IsLogsEnabled.SettingChanged += OnSettingChanged;
        DebugHudEnabled.SettingChanged += OnSettingChanged;
        ThreatIconEnabled.SettingChanged += OnSettingChanged;
        ThreatIconSize.SettingChanged += OnSettingChanged;
        ThreatIconOffsetX.SettingChanged += OnSettingChanged;
        ThreatIconOffsetY.SettingChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}

using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Manages all plugin configuration settings
/// </summary>
public sealed class ForesightConfiguration : IForesightConfiguration
{
    private readonly ConfigFile _configFile;

    public ConfigEntry<bool> PluginEnabled { get; }
    public ConfigEntry<bool> IsLogsEnabled { get; }
    public ConfigEntry<bool> DebugEnabled { get; }

    public ConfigEntry<bool> ThreatIconEnabled { get; }
    public ConfigEntry<float> ThreatIconSize { get; }
    public ConfigEntry<float> ThreatIconOffsetX { get; }
    public ConfigEntry<float> ThreatIconOffsetY { get; }

    public ConfigEntry<bool> AttackCastbarEnabled { get; }
    public ConfigEntry<float> AttackCastbarWidth { get; }
    public ConfigEntry<float> AttackCastbarHeight { get; }
    public ConfigEntry<float> AttackCastbarOffsetX { get; }
    public ConfigEntry<float> AttackCastbarOffsetY { get; }
    public ConfigEntry<float> AttackCastbarParryWindow { get; }
    public ConfigEntry<string> TimingEditorToggleKey { get; }
    public ConfigEntry<bool> AttackTimingLearningEnabled { get; }

    public ConfigEntry<Color> CastbarFillColor { get; }
    public ConfigEntry<Color> CastbarParryIndicatorColor { get; }
    public ConfigEntry<Color> CastbarParryActiveColor { get; }
    public ConfigEntry<Color> CastbarBorderColor { get; }
    public ConfigEntry<Color> CastbarBackgroundColor { get; }
    public ConfigEntry<Color> CastbarTextColor { get; }
    public ConfigEntry<Color> CastbarTextShadowColor { get; }

    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Creates a new configuration manager
    /// </summary>
    public ForesightConfiguration(ConfigFile configFile)
    {
        _configFile = configFile ?? throw new ArgumentNullException(nameof(configFile));

        PluginEnabled = _configFile.Bind(
            "General",
            "Plugin enabled",
            true,
            new ConfigDescription(
                "Enable or disable the plugin functionality. When disabled, all features will be unloaded.",
                tags: new ConfigurationManagerAttributes { Order = 250 }
            )
        );

        ThreatIconEnabled = _configFile.Bind(
            "HUD",
            "Threat icon enabled",
            true,
            new ConfigDescription(
                "Show threat response hint icon next to enemy name.",
                tags: new ConfigurationManagerAttributes { Order = 190 }
            )
        );

        ThreatIconSize = _configFile.Bind(
            "HUD",
            "Threat icon size",
            36f,
            new ConfigDescription(
                "Size of the threat icon in pixels.",
                new AcceptableValueRange<float>(16f, 128f),
                tags: new ConfigurationManagerAttributes { Order = 185 }
            )
        );

        ThreatIconOffsetX = _configFile.Bind(
            "HUD",
            "Threat icon offset X",
            180f,
            new ConfigDescription(
                "Horizontal offset of the threat icon from enemy name (positive = right, negative = left).",
                new AcceptableValueRange<float>(-200f, 200f),
                tags: new ConfigurationManagerAttributes { Order = 180 }
            )
        );

        ThreatIconOffsetY = _configFile.Bind(
            "HUD",
            "Threat icon offset Y",
            54f,
            new ConfigDescription(
                "Vertical offset of the threat icon from enemy name (positive = up, negative = down).",
                new AcceptableValueRange<float>(-100f, 100f),
                tags: new ConfigurationManagerAttributes { Order = 175 }
            )
        );

        AttackCastbarEnabled = _configFile.Bind(
            "Attack Castbar",
            "Attack castbar enabled",
            true,
            new ConfigDescription(
                "Show attack castbar indicating when enemy is attacking and parry window.",
                tags: new ConfigurationManagerAttributes { Order = 200 }
            )
        );

        AttackCastbarWidth = _configFile.Bind(
            "Attack Castbar",
            "Attack castbar width",
            300f,
            new ConfigDescription(
                "Width of the attack castbar in pixels.",
                new AcceptableValueRange<float>(50f, 140),
                new ConfigurationManagerAttributes { Order = 195 }
            )
        );

        AttackCastbarHeight = _configFile.Bind(
            "Attack Castbar",
            "Attack castbar height",
            16f,
            new ConfigDescription(
                "Height of the attack castbar in pixels.",
                new AcceptableValueRange<float>(12f, 32f),
                new ConfigurationManagerAttributes { Order = 190 }
            )
        );

        AttackCastbarOffsetX = _configFile.Bind(
            "Attack Castbar",
            "Attack castbar offset X",
            0f,
            new ConfigDescription(
                "Horizontal offset of the castbar from center (positive = right, negative = left).",
                new AcceptableValueRange<float>(-200f, 200f),
                new ConfigurationManagerAttributes { Order = 185 }
            )
        );

        AttackCastbarOffsetY = _configFile.Bind(
            "Attack Castbar",
            "Attack castbar offset Y",
            23.5f,
            new ConfigDescription(
                "Vertical offset of the castbar from enemy HUD (positive = up, negative = down). Default -10 places it below HP bar.",
                new AcceptableValueRange<float>(-100f, 100f),
                new ConfigurationManagerAttributes { Order = 180 }
            )
        );

        AttackCastbarParryWindow = _configFile.Bind(
            "Attack Castbar",
            "Parry window size",
            0.25f,
            new ConfigDescription(
                "Seconds before predicted hit to show parry indicator (default: 0.25s)",
                new AcceptableValueRange<float>(0f, 0.5f),
                new ConfigurationManagerAttributes { Order = 175 }
            )
        );

        CastbarFillColor = _configFile.Bind(
            "Attack Castbar",
            "Fill color",
            new Color(1f, 0.9f, 0.1f, 0.85f),
            new ConfigDescription(
                "Color of the castbar fill. Default: bright yellow with transparency.",
                tags: new ConfigurationManagerAttributes { Order = 170 }
            )
        );

        CastbarParryIndicatorColor = _configFile.Bind(
            "Attack Castbar",
            "Parry window color",
            new Color(1f, 0.6f, 0.2f, 0.85f),
            new ConfigDescription(
                "Color of the parry window indicator. Default: orange.",
                tags: new ConfigurationManagerAttributes { Order = 165 }
            )
        );

        CastbarParryActiveColor = _configFile.Bind(
            "Attack Castbar",
            "Parry window active color",
            new Color(1f, 0.2f, 0.2f, 0.85f),
            new ConfigDescription(
                "Color of the parry indicator when parry window is active. Default: red.",
                tags: new ConfigurationManagerAttributes { Order = 160 }
            )
        );

        CastbarBorderColor = _configFile.Bind(
            "Attack Castbar",
            "Border color",
            new Color(0.5f, 0.5f, 0.5f, 0.2f),
            new ConfigDescription(
                "Color of the castbar border. Default: light gray with transparency.",
                tags: new ConfigurationManagerAttributes { Order = 155 }
            )
        );

        CastbarBackgroundColor = _configFile.Bind(
            "Attack Castbar",
            "Background color",
            new Color(0f, 0f, 0f, 0.9f),
            new ConfigDescription(
                "Color of the castbar background. Default: black with high opacity.",
                tags: new ConfigurationManagerAttributes { Order = 150 }
            )
        );

        CastbarTextColor = _configFile.Bind(
            "Attack Castbar",
            "Text color",
            new Color(1f, 1f, 1f, 1f),
            new ConfigDescription(
                "Color of the castbar text. Default: white.",
                tags: new ConfigurationManagerAttributes { Order = 145 }
            )
        );

        CastbarTextShadowColor = _configFile.Bind(
            "Attack Castbar",
            "Text shadow color",
            new Color(0f, 0f, 0f, 0.9f),
            new ConfigDescription(
                "Color of the text shadow. Default: black with transparency.",
                tags: new ConfigurationManagerAttributes { Order = 140 }
            )
        );

        TimingEditorToggleKey = _configFile.Bind(
            "Attack Timing",
            "Timing editor toggle key",
            "F7",
            new ConfigDescription(
                "Key to toggle the Attack Timing Editor UI. Use Unity KeyCode names (e.g., F7, F8, Insert).",
                tags: new ConfigurationManagerAttributes { Order = 145 }
            )
        );

        AttackTimingLearningEnabled = _configFile.Bind(
            "Attack Timing",
            "Learning enabled",
            true,
            new ConfigDescription(
                "Enable automatic learning of attack timings. When disabled, the mod will not update timings from observed attacks.",
                tags: new ConfigurationManagerAttributes { Order = 150 }
            )
        );

        IsLogsEnabled = _configFile.Bind(
            "Logs",
            "Logs enabled",
            true,
            new ConfigDescription(
                "Enable verbose logging for ValheimForesight (for debugging).",
                tags: new ConfigurationManagerAttributes { Order = 10 }
            )
        );

        DebugEnabled = _configFile.Bind(
            "Logs",
            "Debug enabled",
            false,
            new ConfigDescription(
                "Show debug threat info near enemy name and write debug logs.",
                tags: new ConfigurationManagerAttributes { Order = 5 }
            )
        );

        BindConfigChangeHandlers();
    }

    private void BindConfigChangeHandlers()
    {
        PluginEnabled.SettingChanged += OnSettingChanged;
        IsLogsEnabled.SettingChanged += OnSettingChanged;
        DebugEnabled.SettingChanged += OnSettingChanged;

        ThreatIconEnabled.SettingChanged += OnSettingChanged;
        ThreatIconSize.SettingChanged += OnSettingChanged;
        ThreatIconOffsetX.SettingChanged += OnSettingChanged;
        ThreatIconOffsetY.SettingChanged += OnSettingChanged;

        AttackCastbarEnabled.SettingChanged += OnSettingChanged;
        AttackCastbarWidth.SettingChanged += OnSettingChanged;
        AttackCastbarHeight.SettingChanged += OnSettingChanged;
        AttackCastbarOffsetX.SettingChanged += OnSettingChanged;
        AttackCastbarOffsetY.SettingChanged += OnSettingChanged;

        AttackCastbarParryWindow.SettingChanged += OnSettingChanged;

        TimingEditorToggleKey.SettingChanged += OnSettingChanged;
        AttackTimingLearningEnabled.SettingChanged += OnSettingChanged;

        CastbarFillColor.SettingChanged += OnSettingChanged;
        CastbarParryIndicatorColor.SettingChanged += OnSettingChanged;
        CastbarParryActiveColor.SettingChanged += OnSettingChanged;
        CastbarBorderColor.SettingChanged += OnSettingChanged;
        CastbarBackgroundColor.SettingChanged += OnSettingChanged;
        CastbarTextColor.SettingChanged += OnSettingChanged;
        CastbarTextShadowColor.SettingChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}

using System;
using BepInEx.Configuration;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Manages all plugin configuration settings
/// </summary>
public sealed class ForesightConfiguration : IForesightConfiguration
{
    private readonly ConfigFile _configFile;

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
    public ConfigEntry<float> ParryIndicatorStartPosition { get; }
    public ConfigEntry<float> AttackCastbarParryWindow { get; }
    public ConfigEntry<string> TimingEditorToggleKey { get; }
    public ConfigEntry<bool> AttackTimingLearningEnabled { get; }

    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Creates a new configuration manager
    /// </summary>
    public ForesightConfiguration(ConfigFile configFile)
    {
        _configFile = configFile ?? throw new ArgumentNullException(nameof(configFile));

        IsLogsEnabled = _configFile.Bind(
            "Logs",
            "LogsEnabled",
            true,
            "Enable verbose logging for ValheimForesight (for debugging)."
        );

        DebugEnabled = _configFile.Bind(
            "Logs",
            "DebugEnabled",
            false,
            "Show debug threat info near enemy name and write debug logs."
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

        AttackCastbarEnabled = _configFile.Bind(
            "Attack Castbar",
            "AttackCastbarEnabled",
            true,
            "Show attack castbar indicating when enemy is attacking and parry window."
        );

        AttackCastbarWidth = _configFile.Bind(
            "Attack Castbar",
            "AttackCastbarWidth",
            100f,
            new ConfigDescription(
                "Width of the attack castbar in pixels.",
                new AcceptableValueRange<float>(50f, 300f)
            )
        );

        AttackCastbarHeight = _configFile.Bind(
            "Attack Castbar",
            "AttackCastbarHeight",
            16f,
            new ConfigDescription(
                "Height of the attack castbar in pixels.",
                new AcceptableValueRange<float>(12f, 32f)
            )
        );

        AttackCastbarOffsetX = _configFile.Bind(
            "Attack Castbar",
            "AttackCastbarOffsetX",
            0f,
            new ConfigDescription(
                "Horizontal offset of the castbar from center (positive = right, negative = left).",
                new AcceptableValueRange<float>(-200f, 200f)
            )
        );

        AttackCastbarOffsetY = _configFile.Bind(
            "Attack Castbar",
            "AttackCastbarOffsetY",
            -10f,
            new ConfigDescription(
                "Vertical offset of the castbar from enemy HUD (positive = up, negative = down). Default -10 places it below HP bar.",
                new AcceptableValueRange<float>(-100f, 100f)
            )
        );

        ParryIndicatorStartPosition = _configFile.Bind(
            "Attack Castbar",
            "ParryIndicatorPosition",
            0.70f,
            new ConfigDescription(
                "Default parry indicator position on castbar (0.0-1.0) when timing data is not yet learned. "
                    + "Automatically adjusts after observing enemy attacks.",
                new AcceptableValueRange<float>(0f, 1f)
            )
        );

        AttackCastbarParryWindow = _configFile.Bind(
            "Attack Castbar",
            "ParryWindow",
            0.25f,
            new ConfigDescription(
                "Seconds before predicted hit to show parry indicator (default: 0.25s)",
                new AcceptableValueRange<float>(0f, 0.5f)
            )
        );

        TimingEditorToggleKey = _configFile.Bind(
            "UI",
            "TimingEditorToggleKey",
            "F7",
            "Key to toggle the Attack Timing Editor UI. Use Unity KeyCode names (e.g., F7, F8, Insert)."
        );

        AttackTimingLearningEnabled = _configFile.Bind(
            "UI",
            "AttackTimingLearningEnabled",
            true,
            "Enable automatic learning of attack timings. When disabled, the mod will not update timings from observed attacks."
        );

        BindConfigChangeHandlers();
    }

    private void BindConfigChangeHandlers()
    {
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

        ParryIndicatorStartPosition.SettingChanged += OnSettingChanged;
        AttackCastbarParryWindow.SettingChanged += OnSettingChanged;

        TimingEditorToggleKey.SettingChanged += OnSettingChanged;
        AttackTimingLearningEnabled.SettingChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}

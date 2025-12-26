using BepInEx.Configuration;
using UnityEngine;

namespace Valheim.Foresight.Configuration;

/// <summary>
/// Configuration interface for Foresight plugin settings
/// </summary>
public interface IForesightConfiguration
{
    ConfigEntry<bool> IsLogsEnabled { get; }
    ConfigEntry<bool> DebugEnabled { get; }
    ConfigEntry<bool> ThreatIconEnabled { get; }
    ConfigEntry<float> ThreatIconSize { get; }
    ConfigEntry<float> ThreatIconOffsetX { get; }
    ConfigEntry<float> ThreatIconOffsetY { get; }
    ConfigEntry<bool> AttackCastbarEnabled { get; }
    ConfigEntry<float> AttackCastbarWidth { get; }
    ConfigEntry<float> AttackCastbarHeight { get; }
    ConfigEntry<float> AttackCastbarOffsetX { get; }
    ConfigEntry<float> AttackCastbarOffsetY { get; }
    ConfigEntry<float> ParryIndicatorStartPosition { get; }
    ConfigEntry<float> AttackCastbarParryWindow { get; }
    ConfigEntry<string> TimingEditorToggleKey { get; }
    ConfigEntry<bool> AttackTimingLearningEnabled { get; }

    ConfigEntry<Color> CastbarFillColor { get; }
    ConfigEntry<Color> CastbarParryIndicatorColor { get; }
    ConfigEntry<Color> CastbarParryActiveColor { get; }
    ConfigEntry<Color> CastbarBorderColor { get; }
    ConfigEntry<Color> CastbarBackgroundColor { get; }
    ConfigEntry<Color> CastbarTextColor { get; }
    ConfigEntry<Color> CastbarTextShadowColor { get; }
}

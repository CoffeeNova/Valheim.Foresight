# Changelog

All notable changes to Valheim Foresight will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.1] - 2025-12-28

### Fixed
- Fixed multiplayer bug where castbars were not displaying correctly for all players

## [2.0.0] - 2025-12-26

### Added
- **Attack Castbar System**: Visual castbars display above enemy nameplates showing attack progress and timing
  - Customizable castbar appearance (size, position, colors)
  - Optional attack name and remaining time display
  - Configurable to always show or only during attacks
- **Parry Window Indicators**: Visual indicators on castbars showing optimal parry timing window
  - Orange indicator shows approaching parry window
  - Turns red when parry window is active
  - Configurable window size (default: 0.25 seconds before impact)
- **Defense Strategy Icons**: Visual icons recommend best defensive action (block/parry/dodge)
  - Shield icon for safe blocks
  - Parry icon for recommended parries
  - Dodge icon when blocking/parrying won't help
  - Fully customizable size and position
- **Intelligent Attack Timing Learning System**: Automatically learns and predicts enemy attack timings
  - Pre-learned timing database for 60+ common enemy attacks
  - Adaptive learning algorithm improves predictions over time
  - Maintains statistical data (mean, variance, sample count) for each attack
  - Stores learned data in human-readable YAML format
- **Attack Timing Editor UI**: In-game editor for managing attack timing data
  - View all learned and prelearned attack timings
  - Edit timing values manually
  - Reset attacks to prelearned defaults
  - Toggle learning on/off per attack
  - Access via configurable hotkey (default: F7)
- **Attack Override System**: Configure custom attack timings via config file
  - Override specific attack durations
  - Ignore attacks (hide castbar)
  - Map animation aliases for variant attacks
  - Disable parry indicators for specific attacks
- **Enhanced Configuration Options**: Extensive new config settings
  - Separate sections for threat icons, castbars, attack timing, and colors
  - All colors fully customizable (RGB with alpha)
  - Position and size adjustments for all visual elements
  - Configuration manager integration with proper categorization

### Known Issues
- Timing predictions may be less accurate for first few observations of new attacks
- Some boss attacks with complex multi-phase animations may require manual timing overrides
- Parry window may not align perfectly for attacks with highly variable timing

## [1.0.1] - 2025-12-22

### Fixed
- Fixed multiplayer bug where threat was calculated incorrectly
- Fixed mob level not being applied for threat calculation

## [1.0.0] - 2025-12-12

### Added
- **Four-tier threat assessment system**: Safe (White), Caution (Yellow), Block Lethal (Orange), Danger (Red)
- **Color-coded enemy nameplates**: Visual indicators show threat level at a glance
- **Block damage estimation**: Calculates effective damage when blocking attacks
- **Parry damage estimation**: Simulates damage reduction from successful parries
- **World difficulty scaling**: Accounts for game progression and world modifiers
- **Multiplayer scaling support**: Adjusts calculations based on nearby player count
- **Configurable settings**: Enable/disable logging, debug mode, and detailed attack analysis
- **Performance optimization**: Threat calculation caching and automatic cleanup system


[2.0.1]: https://github.com/CoffeeNova/Valheim.Foresight/releases/tag/v2.0.1
[2.0.0]: https://github.com/CoffeeNova/Valheim.Foresight/releases/tag/v2.0.0
[1.0.1]: https://github.com/CoffeeNova/Valheim.Foresight/releases/tag/v1.0.1
[1.0.0]: https://github.com/CoffeeNova/Valheim.Foresight/releases/tag/v1.0.0

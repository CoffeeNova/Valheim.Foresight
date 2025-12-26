# Valheim Foresight

A combat threat assessment mod that enhances your situational awareness by displaying color-coded threat indicators on enemy nameplates, attack castbars with parry timing windows, and defensive strategy recommendations. Know at a glance whether you can safely block, need to parry, or should dodge - before the enemy strikes.

![Mod showcase - different threat levels](gallery/screenshots/foresight_header.jpg)

## Features

### Real-Time Threat Assessment

Valheim Foresight calculates the effective damage of enemy attacks based on multiple factors:

- Enemy base damage and level
- World difficulty multipliers
- Player count scaling
- Your current armor and shield stats
- Block power vs. Parry power

### Four Threat Levels

The mod displays threat levels through color-coded enemy names:

| Color      | Threat Level | Meaning                                                         |
| ---------- | ------------ | --------------------------------------------------------------- |
| **White**  | Safe         | You can safely block this attack                                |
| **Yellow** | Caution      | Significant damage but survivable when blocking                 |
| **Orange** | Block Lethal | Blocking will kill you, but parrying will save you              |
| **Red**    | Danger       | Even a perfect parry won't save you - avoid or prepare to dodge |

![Threat level examples](gallery/screenshots/threat_examples.png)

### Attack Castbar System

Foresight now displays visual castbars above enemy nameplates showing when attacks will land:

- **Progress bar** fills from left to right as enemy winds up their attack
- **Parry window indicator** shows the optimal time window for parrying
- **Attack name display** (optional) shows which attack the enemy is performing
- **Remaining time** (optional) displays countdown until impact

![Castbar example](gallery/screenshots/castbar_example.png)

The castbar system uses an intelligent learning algorithm that improves its predictions over time by observing actual attack timings.

### Defense Strategy Icons

Visual icons appear next to enemy nameplates recommending your best defensive option:

- **Shield icon**: You can safely block this attack
- **Parry icon**: You should parry for best results
- **Dodge icon**: You must dodge - blocking or parrying won't save you

![Defense icons example](gallery/screenshots/defense_icons_example.png)

### Intelligent Attack Timing Learning

The mod includes a sophisticated attack prediction system:

- **Pre-learned database**: Ships with timing data for common enemies
- **Adaptive learning**: Automatically improves predictions based on observed attacks
- **Per-attack accuracy**: Tracks timing variance for each unique enemy attack animation
- **Manual overrides**: Configure custom timings through config file
- **Attack mapping**: Handles animation variations and aliases

#### How Learning Works

The learning algorithm uses a rolling mean calculation with variance tracking:

1. **Observation**: Monitors when enemy attacks start and when damage lands
2. **Statistical analysis**: Calculates mean hit timing and variance for each attack
3. **Confidence building**: Requires multiple samples before making predictions
4. **Continuous improvement**: Updates predictions as more data is collected

The attack timing database is stored at `BepInEx/config/foresight.Database/attack_timings.yml` as human-readable YAML with the following structure:

```text

timings:
Troll::attack:
meanHitOffsetSeconds: 1.33
variance: 0.0001
sampleCount: 24
lastUpdatedUtc: 1766696572
learningEnabled: true

```

### Performance Optimized

- Caches threat calculations to minimize performance impact
- Automatic cleanup of distant/dead enemies
- Configurable update intervals
- Efficient memory management for attack tracking

## Installation

### Requirements

- [BepInEx 5.4.2202+](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [YamlDotNet 16.3.1+](https://valheim.thunderstore.io/package/ValheimModding/YamlDotNet/)
- Valheim

### Using a Mod Manager (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)
2. Search for "Valheim Foresight"
3. Click Install

### Manual Installation

1. Download and install [BepInExPack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download and install [YamlDotNet](https://valheim.thunderstore.io/package/ValheimModding/YamlDotNet/)
3. Download the latest Valheim.Foresight.dll from releases
4. Extract and place `Valheim.Foresight.dll` into `BepInEx/plugins/` folder
5. Launch the game

## Configuration

Configuration file is generated at `BepInEx/config/coffeenova.valheim.foresight.cfg` after first launch.

### Available Settings

#### General

- **Plugin enabled** - Enable or disable the plugin functionality (default: true)

#### Threat Icon

- **Threat icon enabled** - Show defense recommendation icon next to enemy name (default: false)
- **Threat icon size** - Size of the icon in pixels (default: 36, range: 16-128)
- **Threat icon offset X** - Horizontal position adjustment (default: 181)
- **Threat icon offset Y** - Vertical position adjustment (default: 100)

#### Attack Castbar

- **Attack castbar enabled** - Show attack timing castbar (default: true)
- **Show text** - Display attack name and timer on castbar (default: false)
- **Always display castbar** - Keep castbar visible even when enemy isn't attacking (default: false)
- **Attack castbar width** - Castbar width in pixels (default: 300, range: 50-1400)
- **Attack castbar height** - Castbar height in pixels (default: 16, range: 12-32)
- **Attack castbar offset X** - Horizontal position adjustment (default: 0)
- **Attack castbar offset Y** - Vertical position adjustment (default: 23.5)
- **Parry window size** - Seconds before predicted hit to show parry indicator (default: 0.25, range: 0-0.5)

#### Attack Castbar Colors

- **Fill color** - Color of the castbar fill (default: bright yellow)
- **Parry window color** - Color of the parry indicator (default: orange)
- **Parry window active color** - Color when in parry window (default: red)
- **Border color** - Color of the castbar border (default: light gray)
- **Background color** - Color of the castbar background (default: black)
- **Text color** - Color of the castbar text (default: white)
- **Text shadow color** - Color of the text shadow (default: black)

#### Attack Timing

- **Timing editor toggle key** - Key to open timing editor UI (default: F7)
- **Learning enabled** - Enable automatic learning of attack timings (default: true)

#### Logs

- **Logs enabled** - Toggle mod logging (default: true)
- **Debug enabled** - Show detailed debug info and logs (default: false)

## Usage

Simply play the game normally! Foresight works automatically in the background:

1. Enemy nameplates will be colored based on threat level
2. Defense strategy icons show recommended defensive action
3. Castbars appear when enemies begin attacking
4. Watch for the parry window indicator to time perfect parries
5. The mod learns attack patterns automatically as you play

### Using the Timing Editor

Press F7 (or your configured key) to open the attack timing editor interface where you can:

- View all learned attack timings
- See statistical data (mean, variance, sample count)
- Manually edit timing values
- Reset attacks to prelearned defaults
- Disable learning for specific attacks

## How It Works

### Damage Calculation Pipeline

1. **Base Damage Detection**: Extracts weapon damage from enemy's equipped items or attacks
2. **Difficulty Multipliers**: Applies world difficulty and player count scaling
3. **Defense Simulation**: Calculates effective damage after your block/parry power
4. **Threat Classification**: Compares effective damage to your current HP
5. **Visual Feedback**: Updates enemy nameplate color and defense icon

### Block vs. Parry

The mod distinguishes between blocking and parrying:

- **Block**: Uses your shield's base block power
- **Parry**: Uses 2.5x block power (configurable by shield type)

This is why some attacks show orange (Block Lethal) - they exceed your block power but not your parry power.

### Attack Prediction System

The castbar system uses three data sources in priority order:

1. **Manual overrides** - Configured in `attack_overrides.cfg`
2. **Learned timings** - Observed during gameplay, stored in `attack_timings.yml`
3. **Prelearned timings** - Pre-packaged timing data for common enemies

When an enemy begins an attack:

1. The mod detects the attack animation start
2. Looks up timing data for that specific enemy and attack
3. Renders a castbar showing progress and parry window
4. Waits for actual hit to occur
5. Records the timing for future predictions (if learning enabled)

## Compatibility

### Confirmed Compatible

- Server-side compatible (clients without the mod won't see threat colors)
- Works with enemy scaling mods that modify damage
- Compatible with HUD mods

## For Developers

### Building from Source

#### Setup

1. Clone the repository

```bash

git clone https://github.com/yourusername/Valheim.Foresight.git
cd Valheim.Foresight

```

2. Set up environment variable `VALHEIM_FORESIGHT_PUBLISH_PATH` pointing to your `BepInEx\plugins\` directory in either:

   - Your Valheim game installation (e.g., `C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins`)
   - Your r2modman profile (e.g., `%APPDATA%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\plugins`)

3. Open `Valheim.Foresight.sln` and build

The built DLL will automatically be copied to the specified directory on successful build.

## FAQ

**Q: Does this work in multiplayer?**
A: Yes! Each client calculates threats independently based on their own stats. You can use it on vanilla servers without server-side installation.

**Q: Why do threat levels change suddenly?**
A: Threat is recalculated based on your current HP, armor, and shield. Damage or gear changes will update the assessment.

**Q: Can I customize the colors?**
A: Yes! All colors are configurable in the config file.

**Q: Does the learning system affect performance?**
A: No. Learning calculations are minimal and occur only when attacks land. The auto-save interval is tunable.

**Q: Can I share my learned timings with others?**
A: Yes! The `attack_timings.yml` file can be copied between installations.

**Q: Does this give an unfair advantage?**
A: It displays information you could observe manually and provides visual feedback for timing you can already execute. Think of it as a training tool that helps you learn enemy patterns.

**Q: Will prelearned timings be updated?**
A: Yes! Future releases may include expanded prelearned databases as more enemy attacks are documented.

## Changelog

[CHANGELOG.md](gallery/thunderstore/CHANGELOG.md)

---

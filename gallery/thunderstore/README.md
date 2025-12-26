# Valheim Foresight

Master Valheim's combat with real-time threat assessment, attack prediction castbars, and defensive strategy recommendations. See which attacks you can block, when to parry, and when you need to dodge - all displayed clearly above enemy health bars!

![Mod showcase](https://raw.githubusercontent.com/CoffeeNova/Valheim.Foresight/main/gallery/screenshots/foresight_header.jpg)

## What Does This Mod Do?

Valheim Foresight is your personal combat advisor that helps you make split-second defensive decisions. It analyzes enemy attacks, your equipment, and the current difficulty to give you actionable information right when you need it.

### Color-Coded Threat Levels

Enemy names change color to show how dangerous their next attack will be:

| Color      | Threat Level | Meaning                                                         |
| ---------- | ------------ | --------------------------------------------------------------- |
| **White**  | Safe         | You can safely block this attack                                |
| **Yellow** | Caution      | Significant damage but survivable when blocking                 |
| **Orange** | Block Lethal | Blocking will kill you, but parrying will save you              |
| **Red**    | Danger       | Even a perfect parry won't save you - avoid or prepare to dodge |

![Threat level examples](https://raw.githubusercontent.com/CoffeeNova/Valheim.Foresight/main/gallery/screenshots/threat_examples.png)

### Attack Castbars - Time Your Parries Perfectly!

**NEW!** See exactly when enemy attacks will land with visual castbars:

- Progress bar fills as the enemy winds up their attack
- Golden window shows the perfect parry timing
- Turns red when you're in the parry window - hit that block button!
- Optional attack names and countdown timers

![Castbar example](https://raw.githubusercontent.com/CoffeeNova/Valheim.Foresight/main/gallery/screenshots/castbar_example.jpg)

---

![Boss Castbar example](https://raw.githubusercontent.com/CoffeeNova/Valheim.Foresight/main/gallery/screenshots/boss_castbar_example.png)

Perfect for beginners learning enemy attack patterns and veterans optimizing their parry timing!

### Defense Strategy Icons

**NEW!** Icons show your best defensive option at a glance:

- **Shield icon**: Safe to block
- **Parry icon**: Parry recommended
- **Dodge icon**: Get out of the way!

![Defense icons](https://raw.githubusercontent.com/CoffeeNova/Valheim.Foresight/main/gallery/screenshots/defense_icons_example.png)

### Smart Learning System

The mod learns enemy attack patterns as you play!

- Starts with pre-loaded timing data for common enemies
- Automatically improves predictions by watching actual attacks
- Gets more accurate the more you play
- Works for modded enemies too!

## Why Use Foresight?

- **Learn faster**: New players can understand combat mechanics visually
- **React better**: Know your defensive options before attacks land
- **Survive longer**: Avoid unnecessary deaths from misjudged blocks
- **Master parrying**: Perfect parry timing with visual indicators
- **Multiplayer friendly**: Works on any server, no server-side installation needed

## Installation

### Easy Way (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or Thunderstore Mod Manager
2. Search for "Valheim Foresight"
3. Click Install
4. Play!

### Manual Way

1. Install [BepInExPack](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [YamlDotNet](https://valheim.thunderstore.io/package/ValheimModding/YamlDotNet/)
3. Download Foresight from Thunderstore
4. Extract to `BepInEx/plugins/`
5. Launch Valheim

## Configuration

The mod creates a config file at `BepInEx/config/coffeenova.valheim.foresight.cfg` on first launch.

### Quick Settings

**Want to customize?** Here are the main settings you might want to adjust:

- **Attack castbar enabled**: Turn castbars on/off (default: ON)
- **Threat icon enabled**: Show/hide defense recommendation icons (default: OFF)
- **Show text**: Display attack names on castbars (default: OFF)
- **Always display castbar**: Keep castbar visible even when idle (default: OFF)
- **Parry window size**: Adjust how early the parry indicator appears (default: 0.25 seconds)

All colors, sizes, and positions are fully customizable!

## How to Use

Just play normally! The mod works automatically:

1. Face an enemy - their name will be colored by threat level
2. When they attack, a castbar appears showing the windup
3. Watch for the orange parry indicator
4. When it turns red, hit block for a perfect parry!
5. Defense icons show whether to block, parry, or dodge

**Pro tip**: Press F7 to open the timing editor and see all learned attack patterns!

## Does This Work With...?

- **Multiplayer**: Yes! Works on any server
- **Dedicated servers**: Yes! Client-side only
- **Enemy mods**: Yes! Learns new enemy attacks automatically
- **HUD mods**: Yes! Compatible with most HUD mods
- **Combat mods**: Mostly yes, as long as they don't heavily modify base mechanics

## FAQ

**Q: Is this cheating?**
A: Not at all! It shows information you could learn through experience and provides visual feedback for timing you can already do manually. Think of it as training wheels for Valheim's combat system.

**Q: Will I get banned?**
A: No. This is a client-side mod that doesn't modify game mechanics or give unfair advantages. It's allowed on all servers.

**Q: Does it affect performance?**
A: Minimal impact. The mod is optimized for performance and shouldn't affect your FPS.

**Q: Do I need to enable learning?**
A: No! The mod ships with pre-learned data for common enemies. Learning is optional but makes predictions more accurate.

**Q: What if the predictions are wrong?**
A: The mod will automatically correct itself as it observes actual attack timings. You can also manually edit timings in the config or timing editor.

**Q: Can I use this to practice parrying?**
A: Absolutely! That's one of the best uses. The visual parry window helps you learn the timing for each enemy type.

## Support

- **Issues or bugs?** [Report on GitHub](https://github.com/CoffeeNova/Valheim.Foresight/issues)
- **Questions?** Ask on Thunderstore or GitHub Discussions
- **Want to contribute?** Pull requests welcome!

## Changelog

See [CHANGELOG.md](https://github.com/CoffeeNova/Valheim.Foresight/blob/main/gallery/thunderstore/CHANGELOG.md) for version history.

---

**Created by CoffeeNova** | [GitHub](https://github.com/CoffeeNova/Valheim.Foresight) | [Thunderstore](https://thunderstore.io/c/valheim/p/CoffeeNova/Valheim_Foresight/)

using Valheim.Foresight.HarmonyRefs;

namespace Valheim.Foresight.Models;

/// <summary>
/// Value object representing player defense capabilities
/// </summary>
public readonly struct PlayerDefenseStats
{
    public float Health { get; }
    public float Armor { get; }
    public float BlockingSkillLevel { get; }
    public ItemDrop.ItemData? Shield { get; }

    public PlayerDefenseStats(
        float health,
        float armor,
        float blockingSkillLevel,
        ItemDrop.ItemData? shield
    )
    {
        Health = health;
        Armor = armor;
        BlockingSkillLevel = blockingSkillLevel;
        Shield = shield;
    }

    public static PlayerDefenseStats FromPlayer(Player player)
    {
        var shield = PlayerMethodRefs.GetCurrentBlocker?.Invoke(player);

        return new PlayerDefenseStats(
            health: player.GetHealth(),
            armor: player.GetBodyArmor(),
            blockingSkillLevel: player.GetSkillLevel(Skills.SkillType.Blocking),
            shield: shield
        );
    }
}

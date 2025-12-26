using System;
using System.Linq;
using UnityEngine;
using Valheim.Foresight.HarmonyRefs;
using Valheim.Foresight.Services.Combat.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Combat;

/// <summary>
/// Inspects creature prefabs to determine their maximum attack damage
/// </summary>
public sealed class CreatureAttackInspector : ICreatureAttackInspector
{
    private readonly IZNetSceneWrapper _zNetSceneWrapper;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new creature attack inspector
    /// </summary>
    public CreatureAttackInspector(IZNetSceneWrapper zNetSceneWrapper, ILogger logger)
    {
        _zNetSceneWrapper =
            zNetSceneWrapper ?? throw new ArgumentNullException(nameof(zNetSceneWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public float GetMaxAttackByPrefabName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName) || _zNetSceneWrapper == null)
            return 0f;

        var prefab = _zNetSceneWrapper.GetPrefab(prefabName);
        return prefab == null ? 0f : InspectPrefabForMaxDamage(prefab);
    }

    /// <inheritdoc/>
    public float GetMaxAttackForCharacter(Character character)
    {
        if (character == null)
            return 0f;

        return InspectPrefabForMaxDamage(character.gameObject);
    }

    private float InspectPrefabForMaxDamage(GameObject prefabRoot)
    {
        if (prefabRoot == null)
            return 0f;

        float maxDamage = 0f;

        InspectHumanoidEquipment(prefabRoot, ref maxDamage);
        InspectItemDrops(prefabRoot, ref maxDamage);
        InspectAttackComponents(prefabRoot, ref maxDamage);
        InspectItemSets(prefabRoot, ref maxDamage);

        _logger.LogDebug($"[{nameof(InspectPrefabForMaxDamage)}] Max damage: {maxDamage}");

        return maxDamage;
    }

    private void InspectHumanoidEquipment(GameObject prefabRoot, ref float maxDamage)
    {
        var humanoid = prefabRoot.GetComponentInChildren<Humanoid>(includeInactive: true);
        if (humanoid == null)
            return;

        var rightItem = HumanoidFieldRefs.RightItemRef?.Invoke(humanoid);
        var leftItem = HumanoidFieldRefs.LeftItemRef?.Invoke(humanoid);

        UpdateMaxFromItem(rightItem, "RightItem", ref maxDamage);
        UpdateMaxFromItem(leftItem, "LeftItem", ref maxDamage);

        if (humanoid.m_unarmedWeapon != null)
        {
            UpdateMaxFromItem(humanoid.m_unarmedWeapon.m_itemData, "UnarmedWeapon", ref maxDamage);
        }
    }

    private void InspectItemDrops(GameObject prefabRoot, ref float maxDamage)
    {
        var itemDrops = prefabRoot.GetComponentsInChildren<ItemDrop>(includeInactive: true);
        foreach (var itemDrop in itemDrops)
        {
            UpdateMaxFromItem(itemDrop.m_itemData, itemDrop.name, ref maxDamage);
        }
    }

    private void InspectAttackComponents(GameObject prefabRoot, ref float maxDamage)
    {
        var attacks = prefabRoot.GetComponentsInChildren<Attack>(includeInactive: true);
        foreach (var attack in attacks)
        {
            UpdateMaxFromAttack(attack, ref maxDamage);
        }
    }

    private void InspectItemSets(GameObject prefabRoot, ref float maxDamage)
    {
        var humanoid = prefabRoot.GetComponentInChildren<Humanoid>(includeInactive: true);
        if (humanoid?.m_randomSets == null || humanoid.m_randomSets.Length == 0)
            return;

        var itemDropsQuery = humanoid
            .m_randomSets.Where(s => s.m_items != null)
            .SelectMany(s => s.m_items)
            .Where(i => i is not null)
            .SelectMany(i => i.GetComponentsInChildren<ItemDrop>(includeInactive: true));

        foreach (var itemDrop in itemDropsQuery)
        {
            UpdateMaxFromItem(itemDrop.m_itemData, itemDrop.name, ref maxDamage);
        }
    }

    private void UpdateMaxFromItem(ItemDrop.ItemData? item, string source, ref float maxDamage)
    {
        if (item == null)
            return;

        var totalDamage = CalculateTotalDamage(item.m_shared.m_damages, source);
        if (totalDamage > maxDamage)
            maxDamage = totalDamage;
    }

    private void UpdateMaxFromAttack(Attack? attack, ref float maxDamage)
    {
        if (attack == null)
            return;

        var baseTotal = 0f;

        var weapon = AttackFieldRefs.WeaponRef?.Invoke(attack);
        if (weapon != null)
        {
            baseTotal = CalculateTotalDamage(weapon.m_shared.m_damages, "Attack.Weapon");
        }

        var multiplier = attack.m_damageMultiplier > 0f ? attack.m_damageMultiplier : 1f;
        var total = baseTotal * multiplier;

        if (total > maxDamage)
            maxDamage = total;
    }

    private float CalculateTotalDamage(HitData.DamageTypes dmg, string source)
    {
        _logger.LogDebug(
            $"[{nameof(CalculateTotalDamage)}] {source}: "
                + $"physical={dmg.m_damage}, blunt={dmg.m_blunt}, slash={dmg.m_slash}, "
                + $"pierce={dmg.m_pierce}, fire={dmg.m_fire}, frost={dmg.m_frost}, "
                + $"lightning={dmg.m_lightning}, poison={dmg.m_poison}, spirit={dmg.m_spirit}"
        );

        // Players are immune to chop and pickaxe damage
        var total =
            dmg.m_damage
            + dmg.m_slash
            + dmg.m_pierce
            + dmg.m_blunt
            + dmg.m_fire
            + dmg.m_frost
            + dmg.m_lightning
            + dmg.m_poison
            + dmg.m_spirit;

        _logger.LogDebug($"[{nameof(CalculateTotalDamage)}] Total from {source}: {total}");

        return total;
    }
}

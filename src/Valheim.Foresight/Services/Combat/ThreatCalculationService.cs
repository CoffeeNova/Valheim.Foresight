using System;
using Valheim.Foresight.HarmonyRefs;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Combat.Interfaces;
using Valheim.Foresight.Services.Damage;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Combat;

/// <summary>
/// Calculates threat levels for enemies based on their damage potential
/// </summary>
public sealed class ThreatCalculationService : IThreatCalculationService
{
    private const float MeleeRangeThreshold = 10f;

    private readonly ILogger _logger;
    private readonly IDamageEstimator _blockEstimator;
    private readonly IDamageEstimator _parryEstimator;
    private readonly Lazy<ICreatureAttackInspector?> _attackInspector;
    private readonly IDifficultyMultiplierCalculator _difficultyCalculator;
    private readonly IVector3Wrapper _vector3Wrapper;
    private readonly IMathfWrapper _mathfWrapper;

    public ThreatCalculationService(
        ILogger logger,
        IDamageEstimator blockEstimator,
        IDamageEstimator parryEstimator,
        Lazy<ICreatureAttackInspector?> attackInspector,
        IDifficultyMultiplierCalculator difficultyCalculator,
        IVector3Wrapper vector3Wrapper,
        IMathfWrapper mathfWrapper
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blockEstimator = blockEstimator ?? throw new ArgumentNullException(nameof(blockEstimator));
        _parryEstimator = parryEstimator ?? throw new ArgumentNullException(nameof(parryEstimator));
        _attackInspector =
            attackInspector ?? throw new ArgumentNullException(nameof(attackInspector));
        _difficultyCalculator =
            difficultyCalculator ?? throw new ArgumentNullException(nameof(difficultyCalculator));
        _vector3Wrapper = vector3Wrapper ?? throw new ArgumentNullException(nameof(vector3Wrapper));
        _mathfWrapper = mathfWrapper ?? throw new ArgumentNullException(nameof(mathfWrapper));
    }

    public ThreatAssessment? CalculateThreat(Character enemy, Player player, bool detailedMode)
    {
        if (enemy == null || player == null)
            return null;

        if (enemy is not Humanoid humanoid)
        {
            _logger.LogDebug($"Enemy {enemy.m_name} is not Humanoid");
            return null;
        }

        var distance = _vector3Wrapper.Distance(
            enemy.transform.position,
            player.transform.position
        );

        var (baseDamage, maxMelee, maxRanged, usedRanged) = detailedMode
            ? CalculateDetailedThreat(humanoid, distance)
            : (CalculateSimpleThreat(humanoid), 0f, 0f, false);

        var rawDamage = ApplyDifficultyMultipliers(enemy, baseDamage);
        var damageInfo = CalculateDamageInfo(player, rawDamage);
        var ratio = CalculateDamageRatio(player, damageInfo.EffectiveDamageWithBlock);
        var threatLevel = DetermineThreatLevel(
            CalculateDamageRatio(player, damageInfo.EffectiveDamageWithBlock),
            CalculateDamageRatio(player, damageInfo.EffectiveDamageWithParry)
        );

        LogThreatCalculation(
            enemy,
            distance,
            baseDamage,
            rawDamage,
            damageInfo,
            ratio,
            threatLevel
        );

        return new ThreatAssessment(
            threatLevel,
            damageInfo,
            ratio,
            maxMelee,
            maxRanged,
            usedRanged
        );
    }

    public ThreatLevel DetermineThreatLevel(float blockRatio, float parryRatio)
    {
        if (parryRatio >= 1.0f)
            return ThreatLevel.Danger;

        if (blockRatio >= 1.0f && parryRatio < 1.0f)
            return ThreatLevel.BlockLethal;

        if (blockRatio >= 0.3f)
            return ThreatLevel.Caution;

        return ThreatLevel.Safe;
    }

    private float CalculateSimpleThreat(Humanoid humanoid)
    {
        if (_attackInspector.Value == null)
            return 0f;

        var maxAttack = _attackInspector.Value.GetMaxAttackForCharacter(humanoid);
        _logger.LogDebug(
            $"[{nameof(CalculateSimpleThreat)}] {humanoid.m_name}: maxAttack={maxAttack:F1}"
        );

        return maxAttack;
    }

    private (
        float baseDamage,
        float maxMelee,
        float maxRanged,
        bool usedRanged
    ) CalculateDetailedThreat(Humanoid humanoid, float distance)
    {
        var (maxMelee, maxRanged) = GetWeaponDamages(humanoid);
        var (currentAttackDamage, isRangedAttack) = GetCurrentAttackInfo(humanoid);

        var useRanged = DetermineAttackType(distance, maxMelee, maxRanged, isRangedAttack);
        var baseDamage = SelectBaseDamage(maxMelee, maxRanged, useRanged);

        _logger.LogDebug(
            $"[{nameof(CalculateDetailedThreat)}] {humanoid.m_name}: "
                + $"melee={maxMelee:F1}, ranged={maxRanged:F1}, "
                + $"currentAttack={currentAttackDamage:F1}, useRanged={useRanged}, "
                + $"dist={distance:F1}, baseDamage={baseDamage:F1}"
        );

        return (baseDamage, maxMelee, maxRanged, useRanged);
    }

    private (float maxMelee, float maxRanged) GetWeaponDamages(Humanoid humanoid)
    {
        if (humanoid == null)
            return (0f, 0f);

        var rightItem = HumanoidMethodRefs.GetRightItem?.Invoke(humanoid);
        var leftItem = HumanoidMethodRefs.GetLeftItem?.Invoke(humanoid);

        var rightDamage = GetItemDamage(rightItem);
        var leftDamage = GetItemDamage(leftItem);
        var maxMelee = _mathfWrapper.Max(rightDamage, leftDamage);

        // todo: add ranged weapon detection
        var maxRanged = 0f;

        return (maxMelee, maxRanged);
    }

    private (float damage, bool isRanged) GetCurrentAttackInfo(Humanoid humanoid)
    {
        if (humanoid == null)
            return (0f, false);

        var currentAttack = HumanoidFieldRefs.CurrentAttackRef?.Invoke(humanoid);
        if (currentAttack == null)
            return (0f, false);

        var weapon = AttackFieldRefs.WeaponRef?.Invoke(currentAttack);
        var damage = weapon != null ? GetItemDamage(weapon) : 0f;

        var damageMultiplier = AttackFieldRefs.DamageMultiplierRef?.Invoke(currentAttack) ?? 1f;
        if (damageMultiplier > 0f)
            damage *= damageMultiplier;

        var attackType =
            AttackFieldRefs.AttackTypeRef?.Invoke(currentAttack) ?? Attack.AttackType.Horizontal;
        var projectilePrefab = AttackFieldRefs.ProjectilePrefabRef?.Invoke(currentAttack);
        var isRanged = attackType == Attack.AttackType.Projectile || projectilePrefab != null;

        return (damage, isRanged);
    }

    private float GetItemDamage(ItemDrop.ItemData? item)
    {
        if (item == null)
            return 0f;

        var dmg = item.m_shared.m_damages;
        return dmg.m_damage
            + dmg.m_blunt
            + dmg.m_slash
            + dmg.m_pierce
            + dmg.m_chop
            + dmg.m_pickaxe
            + dmg.m_fire
            + dmg.m_frost
            + dmg.m_lightning
            + dmg.m_poison
            + dmg.m_spirit;
    }

    private bool DetermineAttackType(
        float distance,
        float maxMelee,
        float maxRanged,
        bool currentIsRanged
    )
    {
        if (distance <= MeleeRangeThreshold && maxMelee > 0f)
            return false;

        if (maxRanged > 0f)
            return true;

        return currentIsRanged;
    }

    private float SelectBaseDamage(float maxMelee, float maxRanged, bool useRanged)
    {
        if (useRanged && maxRanged > 0f)
            return maxRanged;

        if (!useRanged && maxMelee > 0f)
            return maxMelee;

        return _mathfWrapper.Max(maxMelee, maxRanged);
    }

    private float ApplyDifficultyMultipliers(Character enemy, float baseDamage)
    {
        var position = enemy.transform.position;
        var multiplier = _difficultyCalculator.GetDamageMultiplier(position);

        var level = enemy.GetLevel();
        var levelMultiplier = 1f + 0.4f * (level - 1);

        return baseDamage * multiplier * levelMultiplier;
    }

    private DamageInfo CalculateDamageInfo(Player player, float rawDamage)
    {
        var effectiveWithBlock = _blockEstimator.EstimateEffectiveDamage(player, rawDamage);
        var effectiveWithParry = _parryEstimator.EstimateEffectiveDamage(player, rawDamage);

        return new DamageInfo(rawDamage, effectiveWithBlock, effectiveWithParry);
    }

    private float CalculateDamageRatio(Player player, float effectiveDamage)
    {
        var playerHp = player.GetHealth();
        return playerHp > 0 ? effectiveDamage / playerHp : 0f;
    }

    private void LogThreatCalculation(
        Character enemy,
        float distance,
        float baseDamage,
        float rawDamage,
        DamageInfo damageInfo,
        float ratio,
        ThreatLevel threatLevel
    )
    {
        var position = enemy.transform.position;
        var worldDiff = _difficultyCalculator.GetWorldDifficultyMultiplier();
        var playerCount = _difficultyCalculator.GetNearbyPlayerCount(position);
        var playerMult = _difficultyCalculator.GetPlayerCountMultiplier(position);
        var totalMult = _difficultyCalculator.GetDamageMultiplier(position);

        _logger.LogDebug(
            $"Threat: {enemy.m_name} lvl{enemy.GetLevel()} dist={distance:F1}m, "
                + $"base={baseDamage:F1}, "
                + $"worldDiff={worldDiff:F2}x, "
                + $"players={playerCount} ({playerMult:F2}x), "
                + $"totalMult={totalMult:F2}x, "
                + $"raw={rawDamage:F1},"
                + $"effBlock={damageInfo.EffectiveDamageWithBlock:F1}, "
                + $"effParry={damageInfo.EffectiveDamageWithParry:F1}, "
                + $"ratio={ratio:F2}, "
                + $"threat={threatLevel}"
        );
    }
}

using System;
using Valheim.Foresight.Extensions;
using Valheim.Foresight.Models;
using Valheim.Foresight.Networking;
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

    public ThreatAssessment? CalculateThreat(Character enemy, Player player)
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

        var baseDamage = GetMaxAttackDamage(humanoid);
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

        return new ThreatAssessment(threatLevel, damageInfo, ratio);
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

    private float GetMaxAttackDamage(Humanoid humanoid)
    {
        // var local = GetMaxAttackLocal(humanoid);
        // if (local > 0f)
        //     return local;

        return GetMaxAttackNetwork(humanoid);
    }

    private float GetMaxAttackLocal(Humanoid humanoid)
    {
        if (_attackInspector.Value == null)
            return 0f;

        var maxAttack = _attackInspector.Value.GetMaxAttackForCharacter(humanoid);
        _logger.LogDebug(
            $"[{nameof(GetMaxAttackLocal)}] {humanoid.m_name}: maxAttack={maxAttack:F1}"
        );

        return maxAttack;
    }

    private float GetMaxAttackNetwork(Humanoid humanoid)
    {
        var prefabName = humanoid.GetPrefabName();
        var level = humanoid.GetLevel();

        EnemyMaxAttackRpc.RequestIfNeeded(prefabName, level);
        EnemyMaxAttackRpc.TryGet(prefabName, level, out var serverMaxAttack);
        _logger.LogDebug(
            $"[{nameof(GetMaxAttackNetwork)}] {humanoid.m_name}: maxAttack={serverMaxAttack:F1}"
        );

        return serverMaxAttack;
    }

    private float ApplyDifficultyMultipliers(Character enemy, float baseDamage)
    {
        var position = enemy.transform.position;
        var multiplier = _difficultyCalculator.GetDamageMultiplier(position);

        return baseDamage * multiplier;
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
                + $"base={baseDamage:F1}, worldDiff={worldDiff:F2}x, "
                + $"players={playerCount} ({playerMult:F2}x), totalMult={totalMult:F2}x, "
                + $"raw={rawDamage:F1}, effBlock={damageInfo.EffectiveDamageWithBlock:F1}, "
                + $"effParry={damageInfo.EffectiveDamageWithParry:F1}, ratio={ratio:F2}, "
                + $"threat={threatLevel}"
        );
    }
}

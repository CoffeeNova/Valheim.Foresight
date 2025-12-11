using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Valheim.Foresight.Services.Combat.Interfaces;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.Services.Combat;

/// <summary>
/// Calculates damage multipliers based on world difficulty and player count
/// </summary>
public sealed class DifficultyMultiplierCalculator : IDifficultyMultiplierCalculator
{
    private const float PlayerCountRadius = 200f;
    private const float DamagePerExtraPlayer = 0.04f;
    private const string EnemyDamageKey = "EnemyDamage";
    private const string PlayerDamageKey = "PlayerDamage";

    private readonly ILogger _logger;
    private readonly IPlayerWrapper _playerWrapper;
    private readonly IZoneSystemWrapper _zoneSystemWrapper;
    private readonly IMathfWrapper _mathfWrapper;

    public DifficultyMultiplierCalculator(
        ILogger logger,
        IPlayerWrapper playerWrapper,
        IZoneSystemWrapper zoneSystemWrapper,
        IMathfWrapper mathfWrapper
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _playerWrapper = playerWrapper ?? throw new ArgumentNullException(nameof(playerWrapper));
        _zoneSystemWrapper =
            zoneSystemWrapper ?? throw new ArgumentNullException(nameof(zoneSystemWrapper));
        _mathfWrapper = mathfWrapper ?? throw new ArgumentNullException(nameof(mathfWrapper));
    }

    public float GetDamageMultiplier(Vector3 position)
    {
        var difficultyScale = GetWorldDifficultyMultiplier();
        var playerMultiplier = GetPlayerCountMultiplier(position);
        var totalMultiplier = difficultyScale * playerMultiplier;

        _logger.LogDebug(
            $"[{nameof(GetDamageMultiplier)}] "
                + $"worldDifficulty={difficultyScale:F2}, "
                + $"playerMultiplier={playerMultiplier:F2}, "
                + $"total={totalMultiplier:F2}"
        );

        return totalMultiplier;
    }

    public float GetWorldDifficultyMultiplier()
    {
        var scale = GetIncomingDamageFactor();

        _logger.LogDebug($"[{nameof(GetWorldDifficultyMultiplier)}] scale={scale:F2}");

        return scale;
    }

    public float GetPlayerCountMultiplier(Vector3 position)
    {
        var playerCount = GetNearbyPlayerCount(position);
        var multiplier = 1.0f + (_mathfWrapper.Max(0, playerCount - 1) * DamagePerExtraPlayer);

        _logger.LogDebug(
            $"[{nameof(GetPlayerCountMultiplier)}] "
                + $"players={playerCount}, multiplier={multiplier:F2}"
        );

        return multiplier;
    }

    public int GetNearbyPlayerCount(Vector3 position)
    {
        var count = _playerWrapper.GetPlayersInRangeXZ(position, PlayerCountRadius);

        _logger.LogDebug(
            $"[{nameof(GetNearbyPlayerCount)}] Found {count} players within {PlayerCountRadius}m"
        );

        return count;
    }

    public float GetIncomingDamageFactor()
    {
        try
        {
            if (!_zoneSystemWrapper.IsInitialized)
            {
                _logger.LogDebug($"[{nameof(GetIncomingDamageFactor)}] ZoneSystem not initialized");
                return 1f;
            }

            if (_zoneSystemWrapper.GetGlobalKey(EnemyDamageKey, out string damageValue))
            {
                return ParseDamageMultiplier(EnemyDamageKey, damageValue);
            }

            if (_zoneSystemWrapper.TryGetGlobalKeyValue(EnemyDamageKey, out string dictValue))
            {
                return ParseDamageMultiplier(EnemyDamageKey, dictValue);
            }

            _logger.LogDebug(
                $"[{nameof(GetIncomingDamageFactor)}] {EnemyDamageKey} not set, using default 1.0x"
            );

            return 1f;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{nameof(GetIncomingDamageFactor)}] Exception: {ex.Message}");
            return 1f;
        }
    }

    public float GetEnemyHealthFactor()
    {
        try
        {
            if (!_zoneSystemWrapper.IsInitialized)
                return 1f;

            if (_zoneSystemWrapper.GetGlobalKey(PlayerDamageKey, out string damageValue))
            {
                if (
                    float.TryParse(
                        damageValue,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float percentage
                    )
                )
                {
                    var multiplier = percentage > 0f ? 100f / percentage : 1f;

                    _logger.LogDebug(
                        $"[{nameof(GetEnemyHealthFactor)}] {PlayerDamageKey}={damageValue}% -> enemy HP {multiplier:F2}x"
                    );

                    return multiplier;
                }
            }

            return 1f;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{nameof(GetEnemyHealthFactor)}] Exception: {ex.Message}");
            return 1f;
        }
    }

    public bool HasGlobalKey(string key)
    {
        return _zoneSystemWrapper.IsInitialized && _zoneSystemWrapper.GetGlobalKey(key);
    }

    List<string> IDifficultyMultiplierCalculator.GetAllGlobalKeys()
    {
        return _zoneSystemWrapper.GetGlobalKeys();
    }

    private float ParseDamageMultiplier(string keyName, string value)
    {
        if (string.IsNullOrEmpty(value))
            return 1f;

        if (
            float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float percentage
            )
        )
        {
            var multiplier = percentage / 100f;

            _logger.LogDebug(
                $"[{nameof(ParseDamageMultiplier)}] {keyName}={value}% -> {multiplier:F2}x"
            );

            return multiplier > 0f ? multiplier : 1f;
        }

        _logger.LogWarning(
            $"[{nameof(ParseDamageMultiplier)}] Failed to parse {keyName}='{value}', using default"
        );

        return 1f;
    }
}

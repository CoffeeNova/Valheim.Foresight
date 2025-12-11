using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using UnityEngine;
using Valheim.Foresight.Services.Combat;
using Valheim.Foresight.Services.Combat.Interfaces;
using Xunit;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.UnitTests.Services.Combat;

public class DifficultyMultiplierCalculatorTests : IClassFixture<GlobalFixture>
{
    private readonly IFixture _fixture;
    private readonly ILogger _logger;
    private readonly IPlayerWrapper _playerWrapper;
    private readonly IZoneSystemWrapper _zoneSystemWrapper;
    private readonly IMathfWrapper _mathfWrapper;

    public DifficultyMultiplierCalculatorTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _logger = _fixture.Freeze<ILogger>();
        _playerWrapper = _fixture.Freeze<IPlayerWrapper>();
        _zoneSystemWrapper = _fixture.Freeze<IZoneSystemWrapper>();
        _mathfWrapper = _fixture.Freeze<IMathfWrapper>();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange + Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DifficultyMultiplierCalculator(
                null!,
                _playerWrapper,
                _zoneSystemWrapper,
                _mathfWrapper
            )
        );
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPlayerWrapperIsNull()
    {
        // Arrange + Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DifficultyMultiplierCalculator(_logger, null!, _zoneSystemWrapper, _mathfWrapper)
        );
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenZoneSystemWrapperIsNull()
    {
        // Arrange + Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DifficultyMultiplierCalculator(_logger, _playerWrapper, null!, _mathfWrapper)
        );
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMathfWrapperIsNull()
    {
        // Arrange + Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DifficultyMultiplierCalculator(_logger, _playerWrapper, _zoneSystemWrapper, null!)
        );
    }

    [Fact]
    public void GetNearbyPlayerCount_CallsPlayerWrapperWithCorrectParameters()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        var position = new Vector3(100f, 50f, 200f);
        var expectedCount = 3;
        _playerWrapper.GetPlayersInRangeXZ(position, 200f).Returns(expectedCount);

        // Act
        var result = sut.GetNearbyPlayerCount(position);

        // Assert
        Assert.Equal(expectedCount, result);
        _playerWrapper.Received(1).GetPlayersInRangeXZ(position, 200f);
    }

    [Fact]
    public void GetPlayerCountMultiplier_ReturnsOnePointZero_WhenOnlyOnePlayer()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        var position = new Vector3(100f, 50f, 200f);
        _playerWrapper.GetPlayersInRangeXZ(Arg.Any<Vector3>(), Arg.Any<float>()).Returns(1);
        _mathfWrapper.Max(0, 0).Returns(0);

        // Act
        var result = sut.GetPlayerCountMultiplier(position);

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void GetPlayerCountMultiplier_ReturnsCorrectMultiplier_WhenMultiplePlayers()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        var position = new Vector3(100f, 50f, 200f);
        _playerWrapper.GetPlayersInRangeXZ(Arg.Any<Vector3>(), Arg.Any<float>()).Returns(4);
        _mathfWrapper.Max(0, 3).Returns(3);

        // Act
        var result = sut.GetPlayerCountMultiplier(position);

        // Assert
        Assert.Equal(1.12f, result, 0.01f);
    }

    [Fact]
    public void GetPlayerCountMultiplier_CallsMathfWrapperMax()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        var position = new Vector3(100f, 50f, 200f);
        _playerWrapper.GetPlayersInRangeXZ(Arg.Any<Vector3>(), Arg.Any<float>()).Returns(2);
        _mathfWrapper.Max(0, 1).Returns(1);

        // Act
        sut.GetPlayerCountMultiplier(position);

        // Assert
        _mathfWrapper.Received(1).Max(0, 1);
    }

    [Fact]
    public void GetIncomingDamageFactor_ReturnsOne_WhenZoneSystemNotInitialized()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(false);

        // Act
        var result = sut.GetIncomingDamageFactor();

        // Assert
        Assert.Equal(1f, result);
    }

    [Fact]
    public void GetIncomingDamageFactor_ReturnsMultiplier_WhenEnemyDamageKeyExists()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper
            .GetGlobalKey("EnemyDamage", out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = "150";
                return true;
            });

        // Act
        var result = sut.GetIncomingDamageFactor();

        // Assert
        Assert.Equal(1.5f, result);
    }

    [Fact]
    public void GetIncomingDamageFactor_FallsBackToDictionary_WhenGetGlobalKeyFails()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper.GetGlobalKey("EnemyDamage", out Arg.Any<string>()).Returns(false);
        _zoneSystemWrapper
            .TryGetGlobalKeyValue("EnemyDamage", out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = "200";
                return true;
            });

        // Act
        var result = sut.GetIncomingDamageFactor();

        // Assert
        Assert.Equal(2.0f, result);
    }

    [Fact]
    public void GetIncomingDamageFactor_ReturnsOne_WhenKeyNotFound()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper.GetGlobalKey("EnemyDamage", out Arg.Any<string>()).Returns(false);
        _zoneSystemWrapper
            .TryGetGlobalKeyValue("EnemyDamage", out Arg.Any<string>())
            .Returns(false);

        // Act
        var result = sut.GetIncomingDamageFactor();

        // Assert
        Assert.Equal(1f, result);
    }

    [Fact]
    public void GetEnemyHealthFactor_ReturnsOne_WhenZoneSystemNotInitialized()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(false);

        // Act
        var result = sut.GetEnemyHealthFactor();

        // Assert
        Assert.Equal(1f, result);
    }

    [Fact]
    public void GetEnemyHealthFactor_ReturnsCorrectMultiplier_WhenPlayerDamageKeyExists()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper
            .GetGlobalKey("PlayerDamage", out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = "50";
                return true;
            });

        // Act
        var result = sut.GetEnemyHealthFactor();

        // Assert
        Assert.Equal(2.0f, result);
    }

    [Fact]
    public void GetEnemyHealthFactor_ReturnsOne_WhenKeyNotFound()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper.GetGlobalKey("PlayerDamage", out Arg.Any<string>()).Returns(false);

        // Act
        var result = sut.GetEnemyHealthFactor();

        // Assert
        Assert.Equal(1f, result);
    }

    [Fact]
    public void GetWorldDifficultyMultiplier_ReturnsIncomingDamageFactor()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper
            .GetGlobalKey("EnemyDamage", out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = "125";
                return true;
            });

        // Act
        var result = sut.GetWorldDifficultyMultiplier();

        // Assert
        Assert.Equal(1.25f, result);
    }

    [Fact]
    public void GetDamageMultiplier_CombinesDifficultyAndPlayerMultipliers()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        var position = new Vector3(100f, 50f, 200f);

        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper
            .GetGlobalKey("EnemyDamage", out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = "150";
                return true;
            });

        _playerWrapper.GetPlayersInRangeXZ(Arg.Any<Vector3>(), Arg.Any<float>()).Returns(3);
        _mathfWrapper.Max(0, 2).Returns(2);

        // Act
        var result = sut.GetDamageMultiplier(position);

        // Assert
        Assert.Equal(1.62f, result, 0.01f);
    }

    [Fact]
    public void HasGlobalKey_ReturnsFalse_WhenZoneSystemNotInitialized()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(false);

        // Act
        var result = sut.HasGlobalKey("TestKey");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasGlobalKey_ReturnsTrue_WhenKeyExists()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        _zoneSystemWrapper.IsInitialized.Returns(true);
        _zoneSystemWrapper.GetGlobalKey("TestKey").Returns(true);

        // Act
        var result = sut.HasGlobalKey("TestKey");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetAllGlobalKeys_ReturnsListFromWrapper()
    {
        // Arrange
        var sut = _fixture.Create<DifficultyMultiplierCalculator>();
        var expectedKeys = new List<string> { "Key1", "Key2", "Key3" };
        _zoneSystemWrapper.GetGlobalKeys().Returns(expectedKeys);

        // Act
        var result = ((IDifficultyMultiplierCalculator)sut).GetAllGlobalKeys();

        // Assert
        Assert.Equal(expectedKeys, result);
    }
}

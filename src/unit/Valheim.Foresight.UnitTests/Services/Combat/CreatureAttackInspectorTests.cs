using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using UnityEngine;
using Valheim.Foresight.Services.Combat;
using Valheim.Foresight.Services.Combat.Interfaces;
using Xunit;
using ILogger = Valheim.Foresight.Core.ILogger;

namespace Valheim.Foresight.UnitTests.Services.Combat;

public class CreatureAttackInspectorTests : IClassFixture<GlobalFixture>
{
    private readonly IFixture _fixture;
    private readonly IZNetSceneWrapper _sceneWrapperMock;
    private readonly ILogger _loggerMock;
    private readonly CreatureAttackInspector _sut;

    public CreatureAttackInspectorTests(GlobalFixture globalFixture)
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _sceneWrapperMock = _fixture.Freeze<IZNetSceneWrapper>();
        _loggerMock = _fixture.Freeze<ILogger>();
        _sut = new CreatureAttackInspector(_sceneWrapperMock, _loggerMock);
    }

    [Fact]
    public void GetMaxAttackByPrefabName_ReturnsZero_WhenPrefabNameIsNull()
    {
        // Arrange

        // Act
        var result = _sut.GetMaxAttackByPrefabName(null!);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void GetMaxAttackByPrefabName_ReturnsZero_WhenPrefabNameIsEmpty()
    {
        // Arrange

        // Act
        var result = _sut.GetMaxAttackByPrefabName(string.Empty);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void GetMaxAttackByPrefabName_ReturnsZero_WhenPrefabNotFound()
    {
        // Arrange
        _sceneWrapperMock.GetPrefab(Arg.Any<string>()).Returns((GameObject)null!);

        // Act
        var result = _sut.GetMaxAttackByPrefabName("TestPrefab");

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void GetMaxAttackByPrefabName_CallsSceneWrapperWithCorrectName()
    {
        // Arrange
        var prefabName = "EnemyPrefab";
        _sceneWrapperMock.GetPrefab(prefabName).Returns((GameObject)null!);

        // Act
        _sut.GetMaxAttackByPrefabName(prefabName);

        // Assert
        _sceneWrapperMock.Received(1).GetPrefab(prefabName);
    }

    [Fact]
    public void GetMaxAttackForCharacter_ReturnsZero_WhenCharacterIsNull()
    {
        // Arrange

        // Act
        var result = _sut.GetMaxAttackForCharacter(null!);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenZNetSceneWrapperIsNull()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CreatureAttackInspector(null!, _loggerMock));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CreatureAttackInspector(_sceneWrapperMock, null!)
        );
    }
}

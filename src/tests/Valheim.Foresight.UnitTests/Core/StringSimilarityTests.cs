using Shouldly;
using Valheim.Foresight.Core;
using Xunit;

namespace Valheim.Foresight.UnitTests.Core;

public class StringSimilarityTests
{
    [Theory]
    [InlineData("attackslash", "attackslash2", true)]
    [InlineData("attackslash", "attackslash1", true)]
    [InlineData("attackslash", "attackslash", true)]
    [InlineData("attack", "Attack", true)]
    [InlineData("test", "test123", false)]
    [InlineData("abc", "xyz", false)]
    [InlineData("hello", "helo", false)]
    [InlineData("", "", false)]
    [InlineData("test", "", false)]
    [InlineData("", "test", false)]
    public void AreSimilar_ShouldReturnExpectedResult(string str1, string str2, bool expectedResult)
    {
        // Act
        var result = StringSimilarity.AreSimilar(str1, str2);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData("attackslash", "attackslash1", 0.91)]
    [InlineData("attackslash", "attackslash", 1.0)]
    [InlineData("abc", "xyz", 0.0)]
    [InlineData("hello", "helo", 0.8)]
    public void CalculateSimilarity_ShouldReturnExpectedPercentage(
        string str1,
        string str2,
        double expectedSimilarity
    )
    {
        // Act
        var result = StringSimilarity.CalculateSimilarity(str1, str2);

        // Assert
        result.ShouldBe(expectedSimilarity, GetSimilarityTolerance());
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("saturday", "sunday", 3)]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "xyz", 3)]
    [InlineData("", "test", 4)]
    [InlineData("test", "", 4)]
    public void CalculateLevenshteinDistance_ShouldReturnExpectedDistance(
        string str1,
        string str2,
        int expectedDistance
    )
    {
        // Act
        var result = StringSimilarity.CalculateLevenshteinDistance(str1, str2);

        // Assert
        result.ShouldBe(expectedDistance);
    }

    [Fact]
    public void AreSimilar_WithCustomThreshold_ShouldRespectThreshold()
    {
        // Arrange
        const string str1 = "test";
        const string str2 = "test1";
        const double strictThreshold = 0.95;
        const double lenientThreshold = 0.75;

        // Act & Assert
        StringSimilarity.AreSimilar(str1, str2, strictThreshold).ShouldBeFalse();
        StringSimilarity.AreSimilar(str1, str2, lenientThreshold).ShouldBeTrue();
    }

    [Fact]
    public void CalculateSimilarity_WithEmptyStrings_ShouldReturnOne()
    {
        // Act
        var result = StringSimilarity.CalculateSimilarity("", "");

        // Assert
        result.ShouldBe(1.0);
    }

    [Theory]
    [InlineData("ATTACKSLASH", "attackslash1")]
    [InlineData("AttackSlash", "attackslash1")]
    public void AreSimilar_ShouldBeCaseInsensitive(string str1, string str2)
    {
        // Act
        var result = StringSimilarity.AreSimilar(str1, str2);

        // Assert
        result.ShouldBeTrue();
    }

    private static double GetSimilarityTolerance() => 0.01;
}

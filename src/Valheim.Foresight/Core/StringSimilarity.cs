using System;

namespace Valheim.Foresight.Core;

/// <summary>
/// Provides string similarity comparison utilities using Levenshtein distance algorithm
/// </summary>
public static class StringSimilarity
{
    private const double DefaultSimilarityThreshold = 0.85;

    /// <summary>
    /// Compares two strings using Levenshtein distance to determine similarity.
    /// Returns true if strings are similar enough based on the default threshold (85%).
    /// </summary>
    /// <param name="str1">First string to compare</param>
    /// <param name="str2">Second string to compare</param>
    /// <returns>True if strings are similar enough (>= 85% similarity)</returns>
    public static bool AreSimilar(string str1, string str2)
    {
        return AreSimilar(str1, str2, DefaultSimilarityThreshold);
    }

    /// <summary>
    /// Compares two strings using Levenshtein distance to determine similarity.
    /// Returns true if strings are similar enough based on the specified threshold.
    /// </summary>
    /// <param name="str1">First string to compare</param>
    /// <param name="str2">Second string to compare</param>
    /// <param name="similarityThreshold">Minimum similarity percentage (0.0 to 1.0) to consider strings as similar</param>
    /// <returns>True if strings are similar enough (>= similarity threshold)</returns>
    public static bool AreSimilar(string str1, string str2, double similarityThreshold)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return false;

        var s1 = str1.ToLowerInvariant();
        var s2 = str2.ToLowerInvariant();

        var similarity = CalculateSimilarity(s1, s2);
        return similarity >= similarityThreshold;
    }

    /// <summary>
    /// Calculates the similarity percentage between two strings using Levenshtein distance.
    /// Returns a value between 0.0 (completely different) and 1.0 (identical).
    /// </summary>
    /// <param name="str1">First string to compare</param>
    /// <param name="str2">Second string to compare</param>
    /// <returns>Similarity percentage (0.0 to 1.0)</returns>
    public static double CalculateSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2))
            return 1.0;

        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0.0;

        var distance = CalculateLevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// Uses optimized version with single array to reduce memory allocation.
    /// Time complexity: O(m*n) where m and n are string lengths.
    /// Space complexity: O(min(m, n))
    /// </summary>
    /// <param name="s1">First string</param>
    /// <param name="s2">Second string</param>
    /// <returns>The minimum number of single-character edits required to change one string into the other</returns>
    public static int CalculateLevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;

        if (len1 == 0)
            return len2;
        if (len2 == 0)
            return len1;

        var distances = new int[len2 + 1];

        for (var j = 0; j <= len2; j++)
            distances[j] = j;

        for (var i = 1; i <= len1; i++)
        {
            var previousDiagonal = distances[0];
            distances[0] = i;

            for (var j = 1; j <= len2; j++)
            {
                var previousDistance = distances[j];
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                distances[j] = Math.Min(
                    Math.Min(distances[j] + 1, distances[j - 1] + 1),
                    previousDiagonal + cost
                );

                previousDiagonal = previousDistance;
            }
        }

        return distances[len2];
    }
}

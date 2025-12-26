# StringSimilarity

Utility class for comparing string similarity using the Levenshtein distance algorithm.

## Overview

The `StringSimilarity` class provides methods to compare strings and determine how similar they are. This is particularly useful for fuzzy matching animation names where strings might differ slightly (e.g., "attackslash" vs "attackslash1").

## Features

- **Levenshtein Distance Algorithm**: Calculates the minimum number of single-character edits (insertions, deletions, or substitutions) required to change one string into another
- **Optimized Implementation**: Uses single-array optimization to reduce memory allocation
- **Configurable Threshold**: Default 85% similarity threshold, but can be customized
- **Case-Insensitive**: Automatically normalizes strings to lowercase for comparison

## Usage

### Basic Similarity Check

```csharp
using Valheim.Foresight.Core;

// Default threshold of 85%
bool similar = StringSimilarity.AreSimilar("attackslash", "attackslash1");
// Returns: true (91.7% similarity)
```

### Custom Threshold

```csharp
// More strict comparison (95% threshold)
bool similar = StringSimilarity.AreSimilar("test", "test1", 0.95);
// Returns: false (80% similarity < 95%)

// More lenient comparison (75% threshold)
bool similar = StringSimilarity.AreSimilar("test", "test1", 0.75);
// Returns: true (80% similarity >= 75%)
```

### Calculate Similarity Percentage

```csharp
double similarity = StringSimilarity.CalculateSimilarity("attackslash", "attackslash1");
// Returns: 0.9166... (approximately 91.7%)
```

### Calculate Levenshtein Distance

```csharp
int distance = StringSimilarity.CalculateLevenshteinDistance("kitten", "sitting");
// Returns: 3 (substitute k->s, substitute e->i, insert g)
```

## Examples

| String 1 | String 2 | Distance | Similarity | AreSimilar (85%) |
|----------|----------|----------|------------|------------------|
| attackslash | attackslash1 | 1 | 91.7% | ✓ Yes |
| attackslash | attackslash | 0 | 100% | ✓ Yes |
| hello | helo | 1 | 80% | ✗ No |
| test | test123 | 3 | 42.9% | ✗ No |
| kitten | sitting | 3 | 57.1% | ✗ No |

## Performance

- **Time Complexity**: O(m × n) where m and n are the lengths of the input strings
- **Space Complexity**: O(min(m, n)) due to single-array optimization
- **Efficiency**: Suitable for short to medium strings (typical animation names)

## Implementation Notes

The algorithm uses a dynamic programming approach with space optimization:

1. Only maintains a single array instead of a full matrix
2. Processes strings character by character
3. Tracks three possible operations: insertion, deletion, substitution
4. Chooses the minimum cost at each step

## API Reference

### `AreSimilar(string str1, string str2)`
Compares two strings using the default 85% similarity threshold.

**Returns**: `true` if similarity >= 85%

### `AreSimilar(string str1, string str2, double similarityThreshold)`
Compares two strings using a custom similarity threshold.

**Parameters**:
- `str1`: First string to compare
- `str2`: Second string to compare
- `similarityThreshold`: Minimum similarity (0.0 to 1.0)

**Returns**: `true` if similarity >= threshold

### `CalculateSimilarity(string str1, string str2)`
Calculates the similarity percentage between two strings.

**Returns**: Value between 0.0 (completely different) and 1.0 (identical)

### `CalculateLevenshteinDistance(string s1, string s2)`
Calculates the Levenshtein distance between two strings.

**Returns**: Integer representing the minimum number of edits needed


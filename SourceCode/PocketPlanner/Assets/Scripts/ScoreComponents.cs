using System;

/// <summary>
/// Represents a single row in the zone breakdown table.
/// Stores the count of zones having a specific number of unique shapes
/// and the cumulative score for those zones.
/// </summary>
[Serializable]
public struct ZoneBreakdownEntry
{
    /// <summary>The number of unique shapes this row represents (1-6).</summary>
    public int uniqueShapeCount;
    /// <summary>Number of zones that have exactly this many unique shapes.</summary>
    public int zoneCount;
    /// <summary>Cumulative score for all zones with this unique shape count.</summary>
    public int totalScore;

    public ZoneBreakdownEntry(int uniqueShapeCount, int zoneCount, int totalScore)
    {
        this.uniqueShapeCount = uniqueShapeCount;
        this.zoneCount = zoneCount;
        this.totalScore = totalScore;
    }
}

/// <summary>
/// Represents the breakdown of a player's score into its component parts.
/// Used for displaying detailed score breakdown at game end.
/// </summary>
[Serializable]
public struct ScoreComponents
{
    // Zone scores
    public int industrialZoneScore;
    public int residentialZoneScore;
    public int commercialZoneScore;

    // Per-zone breakdown data (index 0 = 1 unique shape, index 5 = 6 unique shapes)
    // Each entry stores: how many zones have N unique shapes, and the total score of those zones
    public ZoneBreakdownEntry[] industrialZoneBreakdown;
    public ZoneBreakdownEntry[] residentialZoneBreakdown;
    public ZoneBreakdownEntry[] commercialZoneBreakdown;

    // Special building scores
    public int parkScore;
    public int schoolScore;

    // Star score
    public int starScore;

    // Penalties
    public int emptyCellPenalty;  // -1 per empty non-river cell
    public int wildcardCostTotal; // Total cost of wildcards used (-1, -2, -3)

    // Total score (sum of all components)
    public int totalScore;

    /// <summary>
    /// Initialize the per-zone breakdown arrays with 6 entries (unique shape counts 1-6).
    /// Must be called before populating breakdown data.
    /// </summary>
    public void InitializeBreakdownArrays()
    {
        industrialZoneBreakdown = new ZoneBreakdownEntry[6];
        residentialZoneBreakdown = new ZoneBreakdownEntry[6];
        commercialZoneBreakdown = new ZoneBreakdownEntry[6];

        for (int i = 0; i < 6; i++)
        {
            int uniqueShapes = i + 1;
            industrialZoneBreakdown[i] = new ZoneBreakdownEntry(uniqueShapes, 0, 0);
            residentialZoneBreakdown[i] = new ZoneBreakdownEntry(uniqueShapes, 0, 0);
            commercialZoneBreakdown[i] = new ZoneBreakdownEntry(uniqueShapes, 0, 0);
        }
    }

    /// <summary>
    /// Calculate total score by summing all components.
    /// Note: emptyCellPenalty and wildcardCostTotal are negative values.
    /// </summary>
    public int CalculateTotal()
    {
        totalScore = industrialZoneScore + residentialZoneScore + commercialZoneScore +
                     parkScore + schoolScore + starScore +
                     emptyCellPenalty + wildcardCostTotal;
        return totalScore;
    }

    /// <summary>
    /// Returns a formatted string showing the score breakdown.
    /// </summary>
    public override string ToString()
    {
        return $"Industrial Zones: {industrialZoneScore}\n" +
               $"Residential Zones: {residentialZoneScore}\n" +
               $"Commercial Zones: {commercialZoneScore}\n" +
               $"Parks: {parkScore}\n" +
               $"Schools: {schoolScore}\n" +
               $"Stars: {starScore}\n" +
               $"Empty Cell Penalty: {emptyCellPenalty}\n" +
               $"Wildcard Costs: {wildcardCostTotal}\n" +
               $"TOTAL: {totalScore}";
    }
}
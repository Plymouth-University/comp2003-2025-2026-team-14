using System;

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
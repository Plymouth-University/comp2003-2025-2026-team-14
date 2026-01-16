using UnityEngine;

namespace PocketPlanner.Test
{
    public class ScoreTester : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;

        void Start()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;

            if (scoreManager == null && gameManager != null)
                scoreManager = gameManager.ScoreManager;
        }

        [ContextMenu("Test Score Calculation")]
        public void TestScoreCalculation()
        {
            if (scoreManager == null)
            {
                Debug.LogError("ScoreTester: ScoreManager not found.");
                return;
            }

            ScoreComponents score = scoreManager.CalculateScore();
            Debug.Log($"Score calculated:\n{score}");

            // Log individual components
            Debug.Log($"Industrial Zones: {score.industrialZoneScore}");
            Debug.Log($"Residential Zones: {score.residentialZoneScore}");
            Debug.Log($"Commercial Zones: {score.commercialZoneScore}");
            Debug.Log($"Park Score: {score.parkScore}");
            Debug.Log($"School Score: {score.schoolScore}");
            Debug.Log($"Star Score: {score.starScore}");
            Debug.Log($"Empty Cell Penalty: {score.emptyCellPenalty}");
            Debug.Log($"Wildcard Cost Total: {score.wildcardCostTotal}");
            Debug.Log($"TOTAL: {score.totalScore}");
        }

        [ContextMenu("Test Zone Scoring")]
        public void TestZoneScoring()
        {
            if (scoreManager == null)
            {
                Debug.LogError("ScoreTester: ScoreManager not found.");
                return;
            }

            // Note: This assumes zones exist on the board
            // For a proper test, we'd need to set up test shapes
            Debug.Log("Zone scoring test - requires placed shapes");

            // We could create a mock test board, but for simplicity
            // just calculate and log whatever is there
            ScoreComponents score = scoreManager.CalculateScore();
            Debug.Log($"Zone scores - Industrial: {score.industrialZoneScore}, " +
                      $"Residential: {score.residentialZoneScore}, " +
                      $"Commercial: {score.commercialZoneScore}");
        }

        [ContextMenu("Test Park Scoring")]
        public void TestParkScoring()
        {
            if (scoreManager == null)
            {
                Debug.LogError("ScoreTester: ScoreManager not found.");
                return;
            }

            ScoreComponents score = scoreManager.CalculateScore();
            Debug.Log($"Park Score: {score.parkScore}");

            // Note: This test would need parks placed adjacent to zones
            Debug.Log("Note: Park scoring requires parks adjacent to zones");
            Debug.Log("+2 points per distinct zone orthogonally adjacent to each park");
        }

        [ContextMenu("Test School Scoring")]
        public void TestSchoolScoring()
        {
            if (scoreManager == null)
            {
                Debug.LogError("ScoreTester: ScoreManager not found.");
                return;
            }

            ScoreComponents score = scoreManager.CalculateScore();
            Debug.Log($"School Score: {score.schoolScore}");

            // Note: This test would need schools placed adjacent to residential zones
            Debug.Log("Note: School scoring requires schools adjacent to residential zones");
            Debug.Log("+2 points per residential building in largest adjacent residential zone");
            Debug.Log("One school per zone and one zone per school");
        }

        [ContextMenu("Test Penalty Calculation")]
        public void TestPenaltyCalculation()
        {
            if (scoreManager == null)
            {
                Debug.LogError("ScoreTester: ScoreManager not found.");
                return;
            }

            ScoreComponents score = scoreManager.CalculateScore();
            Debug.Log($"Empty Cell Penalty: {score.emptyCellPenalty}");

            // Note: Penalty is -1 per empty non-river cell
            Debug.Log("Penalty: -1 per empty non-river cell at game end");
            Debug.Log($"Current penalty (negative value): {score.emptyCellPenalty}");
        }

        [ContextMenu("Test Complete Score Breakdown")]
        public void TestCompleteScoreBreakdown()
        {
            if (scoreManager == null)
            {
                Debug.LogError("ScoreTester: ScoreManager not found.");
                return;
            }

            ScoreComponents score = scoreManager.CalculateScore();

            string breakdown = "=== SCORE BREAKDOWN ===\n";
            breakdown += $"Industrial Zones: {score.industrialZoneScore}\n";
            breakdown += $"Residential Zones: {score.residentialZoneScore}\n";
            breakdown += $"Commercial Zones: {score.commercialZoneScore}\n";
            breakdown += $"Park Score: {score.parkScore}\n";
            breakdown += $"School Score: {score.schoolScore}\n";
            breakdown += $"Star Score: {score.starScore}\n";
            breakdown += $"Empty Cell Penalty: {score.emptyCellPenalty}\n";
            breakdown += $"Wildcard Cost Total: {score.wildcardCostTotal}\n";
            breakdown += $"-------------------\n";
            breakdown += $"TOTAL: {score.totalScore}\n";
            breakdown += "=====================";

            Debug.Log(breakdown);
        }
    }
}
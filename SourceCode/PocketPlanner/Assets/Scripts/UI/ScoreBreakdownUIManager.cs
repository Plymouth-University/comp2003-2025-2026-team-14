using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class zoneRowTextReferences
{
    public TextMeshProUGUI quantityOfUniqueShapeCountText;
    public TextMeshProUGUI scoreOfUniqueShapeCountText;
}

public class ScoreBreakdownUIManager : MonoBehaviour
{
    [Header("Main Panel Reference")]
    [SerializeField] private GameObject scoreBreakdownPanel;

    [Header("View Score Guide Button Reference")]
    [SerializeField] private Button viewScoreGuideButton;

    [Header("Zone Breakdown Text References")]
    // Each list has 6 rows corresponding to the amount of unique shapes within a zone
    // index 0 = row 1 = zones with 1 unique shape
    // index 1 = row 2 = zones with 2 unique shapes, etc.
    [SerializeField] private List<zoneRowTextReferences> ResidentialZoneBreakdownTexts;
    [SerializeField] private List<zoneRowTextReferences> CommercialZoneBreakdownTexts;
    [SerializeField] private List<zoneRowTextReferences> IndustrialZoneBreakdownTexts;

    [Header("Total Zone Scores Text References")]
    [SerializeField] private TextMeshProUGUI ResidentialZoneScoreText;
    [SerializeField] private TextMeshProUGUI CommercialZoneScoreText;
    [SerializeField] private TextMeshProUGUI IndustrialZoneScoreText;
    [SerializeField] private TextMeshProUGUI TotalZoneScoreText;

    [Header("Special Building Score Text References")]
    [SerializeField] private TextMeshProUGUI SchoolScoreText;
    [SerializeField] private TextMeshProUGUI ParkScoreText;

    [Header("Star Score Text Reference")]
    [SerializeField] private TextMeshProUGUI StarScoreText;

    [Header("Score Penalties Text References")]
    [SerializeField] private TextMeshProUGUI WildcardCostTotalText;
    [SerializeField] private TextMeshProUGUI EmptyCellPenaltyText;

    [Header("Final Score Text Reference")]
    [SerializeField] private TextMeshProUGUI FinalScoreText;

    [Header("Return Button Reference")]
    [SerializeField] private Button returnButton; // Should hide the score breakdown panel on click

    [Header("Restart & Main Menu Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Wire up return button to hide the panel
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(Hide);
        }

        // Wire up view score guide button
        if (viewScoreGuideButton != null)
        {
            viewScoreGuideButton.onClick.AddListener(OnViewScoreGuideButtonClicked);
        }

        // Wire up restart and main menu buttons via GameManager
        if (restartButton != null && GameManager.Instance != null)
        {
            restartButton.onClick.AddListener(GameManager.Instance.RestartGame);
        }

        if (mainMenuButton != null && GameManager.Instance != null)
        {
            mainMenuButton.onClick.AddListener(GameManager.Instance.ReturnToMainMenu);
        }

        // Hide panel initially
        if (scoreBreakdownPanel != null)
        {
            scoreBreakdownPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Displays the panel without updating text references with scores, allows the user
    /// to view the layout to understand how scoring works.
    /// Handler for when View Score Guide button is clicked.
    /// </summary>
    public void OnViewScoreGuideButtonClicked()
    {
        if (scoreBreakdownPanel != null)
        {
            scoreBreakdownPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Display the full score breakdown panel populated with the given score data.
    /// Called by GameManager when the game ends.
    /// </summary>
    public void DisplayScoreBreakdown(ScoreComponents score)
    {
        if (scoreBreakdownPanel == null)
        {
            Debug.LogError("ScoreBreakdownUIManager: Score breakdown panel not assigned!");
            return;
        }

        // Populate zone breakdown tables
        PopulateZoneBreakdownTable(score.industrialZoneBreakdown, IndustrialZoneBreakdownTexts);
        PopulateZoneBreakdownTable(score.residentialZoneBreakdown, ResidentialZoneBreakdownTexts);
        PopulateZoneBreakdownTable(score.commercialZoneBreakdown, CommercialZoneBreakdownTexts);

        // Populate zone total scores
        if (IndustrialZoneScoreText != null)
            IndustrialZoneScoreText.text = FormatScore(score.industrialZoneScore);
        if (ResidentialZoneScoreText != null)
            ResidentialZoneScoreText.text = FormatScore(score.residentialZoneScore);
        if (CommercialZoneScoreText != null)
            CommercialZoneScoreText.text = FormatScore(score.commercialZoneScore);

        // Total zone score
        int totalZoneScore = score.industrialZoneScore + score.residentialZoneScore + score.commercialZoneScore;
        if (TotalZoneScoreText != null)
            TotalZoneScoreText.text = FormatScore(totalZoneScore);

        // Special building scores
        if (SchoolScoreText != null)
            SchoolScoreText.text = FormatScore(score.schoolScore);
        if (ParkScoreText != null)
            ParkScoreText.text = FormatScore(score.parkScore);

        // Stars
        if (StarScoreText != null)
            StarScoreText.text = FormatScore(score.starScore);

        // Penalties
        if (EmptyCellPenaltyText != null)
            EmptyCellPenaltyText.text = FormatScore(score.emptyCellPenalty);
        if (WildcardCostTotalText != null)
            WildcardCostTotalText.text = FormatScore(score.wildcardCostTotal);

        // Final score
        if (FinalScoreText != null)
            FinalScoreText.text = FormatScore(score.totalScore);

        // Show the panel
        scoreBreakdownPanel.SetActive(true);
    }

    /// <summary>
    /// Populate a zone breakdown table for a specific building type.
    /// Each row shows the quantity of zones with N unique shapes and their cumulative score.
    /// </summary>
    private void PopulateZoneBreakdownTable(ZoneBreakdownEntry[] breakdown, List<zoneRowTextReferences> rowTexts)
    {
        if (breakdown == null || rowTexts == null) return;

        int rowCount = Mathf.Min(breakdown.Length, rowTexts.Count);
        for (int i = 0; i < rowCount; i++)
        {
            zoneRowTextReferences row = rowTexts[i];
            if (row == null) continue;

            if (row.quantityOfUniqueShapeCountText != null)
            {
                row.quantityOfUniqueShapeCountText.text = breakdown[i].zoneCount.ToString();
            }

            if (row.scoreOfUniqueShapeCountText != null)
            {
                row.scoreOfUniqueShapeCountText.text = FormatScore(breakdown[i].totalScore);
            }
        }
    }

    /// <summary>
    /// Format a score value for display. Shows positive values with a "+" prefix
    /// and negative values with a "-" prefix (already included in penalty values).
    /// </summary>
    private string FormatScore(int value)
    {
        if (value >= 0)
            return "+" + value.ToString();
        else
            return value.ToString(); // Negative sign is already included
    }

    /// <summary>
    /// Hide the score breakdown panel.
    /// </summary>
    public void Hide()
    {
        if (scoreBreakdownPanel != null)
        {
            scoreBreakdownPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Show the score breakdown panel (without populating score data).
    /// Used for the score guide view.
    /// </summary>
    public void Show()
    {
        if (scoreBreakdownPanel != null)
        {
            scoreBreakdownPanel.SetActive(true);
        }
    }
}

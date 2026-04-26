using Unity.VisualScripting;
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
    [SerializeField] private List<zoneRowTextReferences> ResidentialZoneBreakdownTexts; // index 0 = row 1, index 1 = row 2 
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
    [SerializeField] private GameObject returnButton; // Should hide the score breakdown panel on click



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    /// <summary>
    /// Displays the panel without updating text references with scores, allows the user to view the layout to understand how scoring works.
    /// Handler for when View Score Guide button is clicked.  
    /// </summary>
    private void OnViewScoreGuideButtonClicked()
    {
        scoreBreakdownPanel.SetActive(true);
    }

}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketPlanner.Core;
using System.Collections.Generic;

namespace PocketPlanner.UI
{
    public class DiceUIManager : MonoBehaviour
    {
        [Header("Dice Manager Reference")]
        [SerializeField] private DiceManager diceManager;

        [Header("Shape Manager Reference")]
        [SerializeField] private ShapeManager shapeManager;

        [Header("Shape Dice UI Elements")]
        [SerializeField] private List<Button> shapeDiceButtons = new List<Button>(3);
        [SerializeField] private List<TextMeshProUGUI> shapeDiceTexts = new List<TextMeshProUGUI>(3);
        [SerializeField] private List<Image> shapeDiceBackgrounds = new List<Image>(3);

        [Header("Building Dice UI Elements")]
        [SerializeField] private List<Button> buildingDiceButtons = new List<Button>(3);
        [SerializeField] private List<TextMeshProUGUI> buildingDiceTexts = new List<TextMeshProUGUI>(3);
        [SerializeField] private List<Image> buildingDiceBackgrounds = new List<Image>(3);

        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color doubleColor = Color.yellow;
        [SerializeField] private Color invalidColor = Color.red;

        [Header("Water Die UI")]
        [SerializeField] private GameObject waterDiePanel;
        [SerializeField] private Button industrialButton;
        [SerializeField] private Button residentialButton;
        [SerializeField] private Button commercialButton;
        [SerializeField] private Button schoolButton;
        [SerializeField] private Button parkButton;

        private bool waterDieClickedThisFrame = false;

        void Start()
        {
            if (diceManager == null)
            {
                diceManager = FindAnyObjectByType<DiceManager>();
                if (diceManager == null)
                {
                    Debug.LogError("DiceUIManager: DiceManager not found.");
                    return;
                }
            }

            if (shapeManager == null)
            {
                shapeManager = FindAnyObjectByType<ShapeManager>();
                if (shapeManager == null)
                {
                    Debug.LogError("DiceUIManager: ShapeManager not found.");
                }
            }

            // Setup button click listeners
            for (int i = 0; i < shapeDiceButtons.Count; i++)
            {
                int index = i; // capture for closure
                shapeDiceButtons[i].onClick.AddListener(() => OnShapeDieClicked(index));
            }
            for (int i = 0; i < buildingDiceButtons.Count; i++)
            {
                int index = i;
                buildingDiceButtons[i].onClick.AddListener(() => OnBuildingDieClicked(index));
            }

            // Setup water die building type buttons
            if (industrialButton != null)
                industrialButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Industrial));
            if (residentialButton != null)
                residentialButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Residential));
            if (commercialButton != null)
                commercialButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Commercial));
            if (schoolButton != null)
                schoolButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.School));
            if (parkButton != null)
                parkButton.onClick.AddListener(() => OnWaterDieBuildingTypeClicked(BuildingType.Park));

            // Hide water die panel initially
            if (waterDiePanel != null)
                waterDiePanel.SetActive(false);

            // Initial UI update
            UpdateDiceUI();
        }

        /// <summary>
        /// Call this after dice are rolled to update UI.
        /// </summary>
        public void UpdateDiceUI()
        {
            if (diceManager == null) return;

            var shapeDice = diceManager.GetShapeDice();
            var buildingDice = diceManager.GetBuildingDice();
            var shapeDoubles = diceManager.GetDoubleFaces(DiceType.Shape);
            var buildingDoubles = diceManager.GetDoubleFaces(DiceType.Building);

            // Update shape dice
            for (int i = 0; i < shapeDice.Count && i < shapeDiceTexts.Count; i++)
            {
                shapeDiceTexts[i].text = shapeDice[i].GetFaceName();
                bool isSelected = shapeDice[i].Selected;
                bool isDouble = shapeDoubles.Contains(shapeDice[i].CurrentFace);
                shapeDiceBackgrounds[i].color = isSelected ? selectedColor : (isDouble ? doubleColor : defaultColor);
            }

            // Update building dice
            for (int i = 0; i < buildingDice.Count && i < buildingDiceTexts.Count; i++)
            {
                buildingDiceTexts[i].text = buildingDice[i].GetFaceName();
                bool isSelected = buildingDice[i].Selected;
                bool isDouble = buildingDoubles.Contains(buildingDice[i].CurrentFace);
                buildingDiceBackgrounds[i].color = isSelected ? selectedColor : (isDouble ? doubleColor : defaultColor);
            }

            // Update water die UI
            UpdateWaterDieUI();
        }

        private void OnShapeDieClicked(int index)
        {
            if (diceManager == null) return;

            diceManager.SelectShapeDie(index);
            UpdateDiceUI();

            if (shapeManager != null)
                shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        private void OnBuildingDieClicked(int index)
        {
            if (diceManager == null) return;

            // Select the building die
            diceManager.SelectBuildingDie(index);

            // Check if the selected die is water
            bool isWaterSelected = diceManager.IsSelectedBuildingWater();
            if (isWaterSelected)
            {
                waterDieClickedThisFrame = true;
            }

            UpdateDiceUI();

            if (shapeManager != null)
                shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        /// <summary>
        /// Call this when dice are rolled (e.g., from GameManager).
        /// </summary>
        public void OnDiceRolled()
        {
            // Hide water die panel when dice are rolled (new turn)
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
            UpdateDiceUI();
        }

        /// <summary>
        /// Call this when selection is cleared.
        /// </summary>
        public void OnSelectionCleared()
        {
            // Hide water die panel when selection cleared
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
            UpdateDiceUI();
        }

        /// <summary>
        /// Highlight dice with double faces (star opportunities).
        /// Now handled by UpdateDiceUI, but kept for compatibility.
        /// </summary>
        public void HighlightDoubleFaces()
        {
            UpdateDiceUI();
        }

        /// <summary>
        /// Show the water die building type selection panel.
        /// </summary>
        public void ShowWaterDiePanel()
        {
            if (waterDiePanel != null)
                waterDiePanel.SetActive(true);
            Debug.Log("Water die panel shown.");
        }

        /// <summary>
        /// Hide the water die building type selection panel.
        /// </summary>
        public void HideWaterDiePanel()
        {
            if (waterDiePanel != null)
                waterDiePanel.SetActive(false);
            Debug.Log("Water die panel hidden.");
        }

        /// <summary>
        /// Called when a building type button is clicked for water die.
        /// </summary>
        private void OnWaterDieBuildingTypeClicked(BuildingType buildingType)
        {
            if (diceManager == null) return;

            diceManager.SetWaterDieChosenBuildingType(buildingType);
            Debug.Log($"Water die building type chosen: {buildingType}");

            // Update shape if one is active
            if (shapeManager != null)
                shapeManager.UpdateActiveShapeFromSelectedDice();

            // Hide panel after selection and reset click flag
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;
        }

        /// <summary>
        /// Update UI to reflect water die selection state.
        /// Call this when dice selection changes.
        /// </summary>
        private void UpdateWaterDieUI()
        {
            if (diceManager == null) return;

            bool isWaterSelected = diceManager.IsSelectedBuildingWater();
            if (isWaterSelected)
            {
                // Show panel if either:
                // 1. No building type has been chosen yet, OR
                // 2. Water die was clicked this frame (user wants to change building type)
                if (!diceManager.IsWaterDieChosenBuildingTypeSet() || waterDieClickedThisFrame)
                {
                    ShowWaterDiePanel();
                }
                else
                {
                    // Building type already chosen and not clicked, keep panel hidden
                    HideWaterDiePanel();
                }
            }
            else
            {
                HideWaterDiePanel();
            }

            // Reset the click flag after processing
            waterDieClickedThisFrame = false;
        }
    }
}
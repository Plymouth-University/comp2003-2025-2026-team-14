using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketPlanner.Core;
using System.Collections.Generic;
using System.Collections;

namespace PocketPlanner.UI
{
    public class DiceUIManager : MonoBehaviour
    {
        [Header("Game Manager Reference")]
        [SerializeField] private GameManager gameManager;

        [Header("Dice Manager Reference")]
        [SerializeField] private DiceManager diceManager;

        [Header("Shape Manager Reference")]
        [SerializeField] private ShapeManager shapeManager;

        [Header("Shape Dice Face Sprites")]
        [SerializeField] private Sprite singleShapeFaceSprite = null;
        [SerializeField] private Sprite TShapeFaceSprite = null;
        [SerializeField] private Sprite LShapeFaceSprite = null;
        [SerializeField] private Sprite squareShapeFaceSprite = null;
        [SerializeField] private Sprite lineShapeFaceSprite = null;
        [SerializeField] private Sprite ZShapeFaceSprite = null;

        [Header("Building Dice Face Sprites")]
        [SerializeField] private Sprite industrialFaceSprite = null;
        [SerializeField] private Sprite residentialFaceSprite = null;
        [SerializeField] private Sprite commercialFaceSprite = null;
        [SerializeField] private Sprite schoolFaceSprite = null;
        [SerializeField] private Sprite parkFaceSprite = null;
        [SerializeField] private Sprite waterFaceSprite = null;

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

        [Header("Wildcard UI")]
        [SerializeField] private Button shapeWildcardButton;
        [SerializeField] private Button buildingWildcardButton;
        [SerializeField] private WildcardSelectionPanel shapeWildcardPanel;
        [SerializeField] private WildcardSelectionPanel buildingWildcardPanel;
        [SerializeField] private TextMeshProUGUI wildcardCountText;
        [SerializeField] private TextMeshProUGUI wildcardCostText;
        [SerializeField] private TextMeshProUGUI turnCountText;
        [SerializeField] private TextMeshProUGUI wildcardCounterText; // New wildcard counter 
        

        private bool waterDieClickedThisFrame = false;
        private int wildcardTargetShapeDieIndex = 0;
        private int wildcardTargetBuildingDieIndex = 0;

        void Start()
        {
            Debug.Log($"DiceUIManager.Start() on {gameObject.name}");
            // Find essential managers, but don't fail entirely if not found immediately
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
                if (gameManager == null)
                {
                    Debug.LogWarning("DiceUIManager: GameManager not found on Start. Will try again later.");
                }
            }

            if (diceManager == null)
            {
                diceManager = FindAnyObjectByType<DiceManager>();
                if (diceManager == null)
                {
                    Debug.LogWarning("DiceUIManager: DiceManager not found on Start. Will try again later.");
                }
            }

            if (shapeManager == null)
            {
                shapeManager = FindAnyObjectByType<ShapeManager>();
                if (shapeManager == null)
                {
                    Debug.LogWarning("DiceUIManager: ShapeManager not found.");
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

            // Setup wildcard buttons and panels
            if (shapeWildcardButton != null)
                shapeWildcardButton.onClick.AddListener(OnShapeWildcardButtonClicked);
            if (buildingWildcardButton != null)
                buildingWildcardButton.onClick.AddListener(OnBuildingWildcardButtonClicked);

            if (shapeWildcardPanel != null)
                shapeWildcardPanel.onSelectionMade.AddListener(OnShapeWildcardSelected);
            if (buildingWildcardPanel != null)
                buildingWildcardPanel.onSelectionMade.AddListener(OnBuildingWildcardSelected);

            // Hide water die panel initially
            if (waterDiePanel != null)
                waterDiePanel.SetActive(false);

            // Hide wildcard panels initially
            if (shapeWildcardPanel != null)
                shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null)
                buildingWildcardPanel.Hide();

            // Update wildcard UI
            UpdateWildcardUI();
            StartCoroutine(EnsureGameManagerReady());

            // Initial UI update
            UpdateDiceUI();
        }

        /// <summary>
        /// Returns the sprite for a given shape type.
        /// </summary>
        private Sprite GetShapeSprite(ShapeType shapeType)
        {
            return shapeType switch
            {
                ShapeType.SingleShape => singleShapeFaceSprite,
                ShapeType.TShape => TShapeFaceSprite,
                ShapeType.LShape => LShapeFaceSprite,
                ShapeType.SquareShape => squareShapeFaceSprite,
                ShapeType.LineShape => lineShapeFaceSprite,
                ShapeType.ZShape => ZShapeFaceSprite,
                _ => null
            };
        }

        /// <summary>
        /// Returns the sprite for a given building type.
        /// </summary>
        private Sprite GetBuildingSprite(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.Industrial => industrialFaceSprite,
                BuildingType.Residential => residentialFaceSprite,
                BuildingType.Commercial => commercialFaceSprite,
                BuildingType.School => schoolFaceSprite,
                BuildingType.Park => parkFaceSprite,
                BuildingType.Water => waterFaceSprite,
                _ => null
            };
        }

        /// <summary>
        /// Call this after dice are rolled to update UI.
        /// </summary>
        public void UpdateDiceUI()
        {
            if (diceManager == null) return;

            var shapeDice = diceManager.GetShapeDice();
            var buildingDice = diceManager.GetBuildingDice();
            //var shapeDoubles = diceManager.GetDoubleFaces(DiceType.Shape);
            var shapeOriginalDoubles = diceManager.GetOriginalDoubleFaces(DiceType.Shape);
            //var buildingDoubles = diceManager.GetDoubleFaces(DiceType.Building);
            var buildingOriginalDoubles = diceManager.GetOriginalDoubleFaces(DiceType.Building);

            // Update shape dice
            for (int i = 0; i < shapeDice.Count && i < shapeDiceTexts.Count; i++)
            {
                shapeDiceTexts[i].text = shapeDice[i].GetFaceName();
                shapeDiceBackgrounds[i].sprite = GetShapeSprite(shapeDice[i].GetShapeType());
                bool isSelected = shapeDice[i].Selected;
                bool isDouble = shapeOriginalDoubles.Contains(shapeDice[i].CurrentFace);
                shapeDiceBackgrounds[i].color = isSelected ? selectedColor : (isDouble ? doubleColor : defaultColor);
            }

            // Update building dice
            for (int i = 0; i < buildingDice.Count && i < buildingDiceTexts.Count; i++)
            {
                buildingDiceTexts[i].text = buildingDice[i].GetFaceName();
                buildingDiceBackgrounds[i].sprite = GetBuildingSprite(buildingDice[i].GetBuildingType());
                bool isSelected = buildingDice[i].Selected;
                bool isDouble = buildingOriginalDoubles.Contains(buildingDice[i].CurrentFace);
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

            // Hide wildcard panels
            if (shapeWildcardPanel != null) shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null) buildingWildcardPanel.Hide();

            UpdateDiceUI();
            UpdateWildcardUI();
        }

        /// <summary>
        /// Call this when selection is cleared.
        /// </summary>
        public void OnSelectionCleared()
        {
            // Hide water die panel when selection cleared
            HideWaterDiePanel();
            waterDieClickedThisFrame = false;

            // Hide wildcard panels
            if (shapeWildcardPanel != null) shapeWildcardPanel.Hide();
            if (buildingWildcardPanel != null) buildingWildcardPanel.Hide();

            UpdateDiceUI();
            UpdateWildcardUI();
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

        /// <summary>
        /// Coroutine that ensures GameManager is ready and updates wildcard UI.
        /// This handles the case where GameManager.Instance may not be set during Start().
        /// </summary>
        private IEnumerator EnsureGameManagerReady()
        {
            // Wait until GameManager.Instance is set
            while (GameManager.Instance == null)
            {
                yield return null; // Wait one frame
            }

            // GameManager is now available, update wildcard UI
            UpdateWildcardUI();
            Debug.Log("EnsureGameManagerReady: GameManager.Instance now available, wildcard UI updated.");
        }

        #region Wildcard Methods

        /// <summary>
        /// Called when shape wildcard button is clicked.
        /// Shows shape wildcard panel if wildcards available.
        /// </summary>
        private void OnShapeWildcardButtonClicked()
        {
            Debug.Log($"OnShapeWildcardButtonClicked called. GameManager.Instance={(GameManager.Instance != null ? "set" : "null")}");
            // Ensure GameManager reference is available
            if (GameManager.Instance == null)
            {
                // Try to find GameManager if it hasn't been initialized yet
                Debug.Log("GameManager.Instance is null, attempting to find GameManager...");
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    // GameManager exists but Instance not set yet (shouldn't happen with singleton pattern)
                    Debug.LogWarning("GameManager found but Instance not set. This may indicate a singleton initialization issue.");
                }
                else
                {
                    Debug.Log("GameManager not found in scene.");
                }
                UpdateWildcardUI(); // Update UI to reflect unavailable state
                return;
            }

            if (!GameManager.Instance.CanUseWildcard())
            {
                Debug.Log("Cannot use wildcard: no wildcards remaining.");
                return;
            }

            // Determine target die index (selected shape die, or first shape die if none selected)
            int targetDieIndex = GetTargetShapeDieIndex();
            if (targetDieIndex < 0 || targetDieIndex >= 3)
            {
                Debug.LogWarning($"Invalid target shape die index: {targetDieIndex}");
                return;
            }

            // Store target die index for when selection is made
            // We'll use a temporary variable or pass it to the panel
            // For simplicity, we'll store in a field
            wildcardTargetShapeDieIndex = targetDieIndex;

            // Show shape wildcard panel
            Debug.Log($"Shape wildcard panel={(shapeWildcardPanel != null ? "assigned" : "null")}");
            if (shapeWildcardPanel != null)
            {
                shapeWildcardPanel.Show();
            }
            else
            {
                Debug.LogError("Shape wildcard panel not assigned.");
            }
        }

        /// <summary>
        /// Called when building wildcard button is clicked.
        /// Shows building wildcard panel if wildcards available.
        /// </summary>
        private void OnBuildingWildcardButtonClicked()
        {
            Debug.Log($"OnBuildingWildcardButtonClicked called. GameManager.Instance={(GameManager.Instance != null ? "set" : "null")}");
            // Ensure GameManager reference is available
            if (GameManager.Instance == null)
            {
                // Try to find GameManager if it hasn't been initialized yet
                Debug.Log("GameManager.Instance is null, attempting to find GameManager...");
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    // GameManager exists but Instance not set yet (shouldn't happen with singleton pattern)
                    Debug.LogWarning("GameManager found but Instance not set. This may indicate a singleton initialization issue.");
                }
                else
                {
                    Debug.Log("GameManager not found in scene.");
                }
                UpdateWildcardUI(); // Update UI to reflect unavailable state
                return;
            }

            if (!GameManager.Instance.CanUseWildcard())
            {
                Debug.Log("Cannot use wildcard: no wildcards remaining.");
                return;
            }

            // Determine target die index (selected building die, or first building die if none selected)
            int targetDieIndex = GetTargetBuildingDieIndex();
            if (targetDieIndex < 0 || targetDieIndex >= 3)
            {
                Debug.LogWarning($"Invalid target building die index: {targetDieIndex}");
                return;
            }

            wildcardTargetBuildingDieIndex = targetDieIndex;

            // Show building wildcard panel
            Debug.Log($"Building wildcard panel={(buildingWildcardPanel != null ? "assigned" : "null")}");
            if (buildingWildcardPanel != null)
            {
                buildingWildcardPanel.Show();
            }
            else
            {
                Debug.LogError("Building wildcard panel not assigned.");
            }
        }

        /// <summary>
        /// Called when a shape is selected from shape wildcard panel.
        /// </summary>
        private void OnShapeWildcardSelected(int faceIndex)
        {
            Debug.Log($"Shape wildcard selected: face index {faceIndex} for die index {wildcardTargetShapeDieIndex}");

            if (diceManager == null || GameManager.Instance == null)
            {
                Debug.LogError("Missing references for wildcard application.");
                return;
            }

            // Apply wildcard override
            diceManager.ApplyWildcardOverride(DiceType.Shape, wildcardTargetShapeDieIndex, faceIndex);

            // Use wildcard (increment count, apply cost)
            bool success = GameManager.Instance.UseWildcard();
            if (!success)
            {
                Debug.LogError("Failed to use wildcard.");
                return;
            }

            // Update UI
            UpdateDiceUI();
            UpdateWildcardUI();

            // Update shape if one is active
            if (shapeManager != null)
                shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        /// <summary>
        /// Called when a building type is selected from building wildcard panel.
        /// </summary>
        private void OnBuildingWildcardSelected(int faceIndex)
        {
            Debug.Log($"Building wildcard selected: face index {faceIndex} for die index {wildcardTargetBuildingDieIndex}");

            if (diceManager == null || GameManager.Instance == null)
            {
                Debug.LogError("Missing references for wildcard application.");
                return;
            }

            // Apply wildcard override
            diceManager.ApplyWildcardOverride(DiceType.Building, wildcardTargetBuildingDieIndex, faceIndex);

            // Use wildcard
            bool success = GameManager.Instance.UseWildcard();
            if (!success)
            {
                Debug.LogError("Failed to use wildcard.");
                return;
            }

            // Update UI
            UpdateDiceUI();
            UpdateWildcardUI();

            // Update shape if one is active
            if (shapeManager != null)
                shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        /// <summary>
        /// Get target shape die index for wildcard.
        /// Returns selected shape die index, or first shape die index if none selected.
        /// </summary>
        private int GetTargetShapeDieIndex()
        {
            if (diceManager == null) return 0;

            var shapeDice = diceManager.GetShapeDice();
            for (int i = 0; i < shapeDice.Count; i++)
            {
                if (shapeDice[i].Selected)
                    return i;
            }

            // No shape die selected, return first die
            return 0;
        }

        /// <summary>
        /// Get target building die index for wildcard.
        /// Returns selected building die index, or first building die index if none selected.
        /// </summary>
        private int GetTargetBuildingDieIndex()
        {
            if (diceManager == null) return 0;

            var buildingDice = diceManager.GetBuildingDice();
            for (int i = 0; i < buildingDice.Count; i++)
            {
                if (buildingDice[i].Selected)
                    return i;
            }

            // No building die selected, return first die
            return 0;
        }

        /// <summary>
        /// Update wildcard UI elements (count, cost, button interactability).
        /// </summary>
        private void UpdateWildcardUI()
        {
            Debug.Log($"UpdateWildcardUI called. GameManager.Instance={(GameManager.Instance != null ? "set" : "null")}");
            if (GameManager.Instance == null)
            {
                // GameManager not ready yet - disable buttons and show placeholder
                if (wildcardCountText != null)
                    wildcardCountText.text = "-/-";
                if (wildcardCostText != null)
                    wildcardCostText.text = "Cost: -";
                if (shapeWildcardButton != null)
                    shapeWildcardButton.interactable = false;
                if (buildingWildcardButton != null)
                    buildingWildcardButton.interactable = false;
                return;
            }

            bool canUseWildcard = GameManager.Instance.CanUseWildcard();
            int remaining = GameManager.MAX_WILDCARDS - GameManager.Instance.WildcardsUsed;
            int nextCost = GameManager.Instance.GetNextWildcardCost();

            // Update text
            if (wildcardCountText != null)
                wildcardCountText.text = $"{remaining}/{GameManager.MAX_WILDCARDS}";
            if (wildcardCounterText != null)
                wildcardCounterText.text = $"Wildcards: {remaining}"; // New reference
            if (wildcardCostText != null)
                wildcardCostText.text = $"Cost: {nextCost}";

            // Update button interactability
            if (shapeWildcardButton != null)
                shapeWildcardButton.interactable = canUseWildcard;
            if (buildingWildcardButton != null)
                buildingWildcardButton.interactable = canUseWildcard;
            Debug.Log($"UpdateWildcardUI: canUseWildcard={canUseWildcard}, shapeButton.interactable={shapeWildcardButton?.interactable}, buildingButton.interactable={buildingWildcardButton?.interactable}");
        }

        #endregion

        public void updateTurnText(int currentTurn)
        {
            if (turnCountText != null)
                turnCountText.text = $"Turn: {currentTurn}";
        }
    }
}
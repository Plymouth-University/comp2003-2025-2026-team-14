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

            diceManager.SelectBuildingDie(index);
            UpdateDiceUI();

            if (shapeManager != null)
                shapeManager.UpdateActiveShapeFromSelectedDice();
        }

        /// <summary>
        /// Call this when dice are rolled (e.g., from GameManager).
        /// </summary>
        public void OnDiceRolled()
        {
            UpdateDiceUI();
        }

        /// <summary>
        /// Call this when selection is cleared.
        /// </summary>
        public void OnSelectionCleared()
        {
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
    }
}
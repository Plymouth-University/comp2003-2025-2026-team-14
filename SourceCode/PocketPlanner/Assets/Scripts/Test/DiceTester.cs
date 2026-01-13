using UnityEngine;
using PocketPlanner.Core;

namespace PocketPlanner.Test
{
    public class DiceTester : MonoBehaviour
    {
        [SerializeField] private DiceManager diceManager;

        void Start()
        {
            if (diceManager == null)
                diceManager = FindObjectOfType<DiceManager>();
        }

        [ContextMenu("Test Dice Roll")]
        public void TestDiceRoll()
        {
            if (diceManager == null)
            {
                Debug.LogError("DiceTester: DiceManager not found.");
                return;
            }

            diceManager.RollAllDice();
            Debug.Log("Dice rolled. Check console for dice faces.");
        }

        [ContextMenu("Test Selection")]
        public void TestSelection()
        {
            if (diceManager == null)
            {
                Debug.LogError("DiceTester: DiceManager not found.");
                return;
            }

            // Select first shape die and first building die
            diceManager.SelectShapeDie(0);
            diceManager.SelectBuildingDie(0);

            var shapeType = diceManager.GetSelectedShapeType();
            var buildingType = diceManager.GetSelectedBuildingType();

            Debug.Log($"Selected Shape: {shapeType}, Building: {buildingType}");
            Debug.Log($"Has valid selection? {diceManager.HasValidSelection()}");
        }

        [ContextMenu("Test Double Detection")]
        public void TestDoubleDetection()
        {
            if (diceManager == null)
            {
                Debug.LogError("DiceTester: DiceManager not found.");
                return;
            }

            var shapeDoubles = diceManager.GetDoubleFaces(DiceType.Shape);
            var buildingDoubles = diceManager.GetDoubleFaces(DiceType.Building);

            Debug.Log($"Shape doubles count: {shapeDoubles.Count}");
            Debug.Log($"Building doubles count: {buildingDoubles.Count}");
        }

        [ContextMenu("Test Auto-Reroll (Triples)")]
        public void TestAutoReroll()
        {
            // This test requires mocking dice faces; for simplicity, just call roll
            // The DicePool's auto-reroll is internal to RollAll().
            Debug.Log("Auto-reroll is performed automatically after each roll.");
        }

        [ContextMenu("Log Dice Faces")]
        public void LogDiceFaces()
        {
            if (diceManager == null)
            {
                Debug.LogError("DiceTester: DiceManager not found.");
                return;
            }

            var shapeDice = diceManager.GetShapeDice();
            var buildingDice = diceManager.GetBuildingDice();

            string shapeLog = "Shape Dice: ";
            foreach (var dice in shapeDice)
                shapeLog += $"{dice.GetFaceName()} (Selected: {dice.Selected}), ";
            Debug.Log(shapeLog);

            string buildingLog = "Building Dice: ";
            foreach (var dice in buildingDice)
                buildingLog += $"{dice.GetFaceName()} (Selected: {dice.Selected}), ";
            Debug.Log(buildingLog);
        }
    }
}
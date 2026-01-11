using UnityEngine;

namespace PocketPlanner.Core
{
    public class DiceManager : MonoBehaviour
    {
        [SerializeField] private DicePool dicePool;

        public DicePool DicePool => dicePool;

        void Awake()
        {
            if (dicePool == null)
                dicePool = new DicePool();
        }

        /// <summary>
        /// Roll all dice and perform auto-rerolls.
        /// </summary>
        public void RollAllDice()
        {
            dicePool.RollAll();
            Debug.Log("Dice rolled.");
            dicePool.LogDiceFaces();
        }

        /// <summary>
        /// Get the selected shape type.
        /// </summary>
        public ShapeType? GetSelectedShapeType()
        {
            return dicePool.GetSelectedShapeType();
        }

        /// <summary>
        /// Get the selected building type.
        /// </summary>
        public BuildingType? GetSelectedBuildingType()
        {
            return dicePool.GetSelectedBuildingType();
        }

        /// <summary>
        /// Check if a valid selection exists.
        /// </summary>
        public bool HasValidSelection()
        {
            return dicePool.HasValidSelection();
        }

        /// <summary>
        /// Select a shape die by index.
        /// </summary>
        public void SelectShapeDie(int index)
        {
            dicePool.SelectShapeDie(index);
        }

        /// <summary>
        /// Select a building die by index.
        /// </summary>
        public void SelectBuildingDie(int index)
        {
            dicePool.SelectBuildingDie(index);
        }

        /// <summary>
        /// Clear all dice selections.
        /// </summary>
        public void ClearSelection()
        {
            dicePool.ClearSelection();
        }

        /// <summary>
        /// Get list of shape dice.
        /// </summary>
        public System.Collections.Generic.List<Dice> GetShapeDice()
        {
            return dicePool.GetShapeDice();
        }

        /// <summary>
        /// Get list of building dice.
        /// </summary>
        public System.Collections.Generic.List<Dice> GetBuildingDice()
        {
            return dicePool.GetBuildingDice();
        }

        /// <summary>
        /// Get double faces for a given dice type.
        /// </summary>
        public System.Collections.Generic.List<int> GetDoubleFaces(DiceType type)
        {
            return dicePool.GetDoubleFaces(type);
        }

        /// <summary>
        /// Check if the selected building die is Water.
        /// </summary>
        public bool IsSelectedBuildingWater()
        {
            var selected = dicePool.GetSelectedBuildingDie();
            return selected != null && selected.GetBuildingType() == BuildingType.Water;
        }
    }
}
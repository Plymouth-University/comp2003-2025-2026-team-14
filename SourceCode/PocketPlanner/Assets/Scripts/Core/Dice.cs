using UnityEngine;

namespace PocketPlanner.Core
{
    public class Dice
    {
        public DiceType Type { get; private set; }
        public int CurrentFace { get; private set; } // 0-5 index (may be overridden)
        public bool Selected { get; set; }
        public bool IsOverridden { get; private set; }
        public int OriginalFace { get; private set; } // Face from last roll

        public Dice(DiceType type)
        {
            Type = type;
            Roll();
            Selected = false;
        }

        /// <summary>
        /// Roll the die to a random face (0-5)
        /// </summary>
        public void Roll()
        {
            CurrentFace = Random.Range(0, 6);
            OriginalFace = CurrentFace;
            IsOverridden = false;
        }

        /// <summary>
        /// Set the die face deterministically (for synchronized multiplayer).
        /// </summary>
        /// <param name="face">Face index (0-5)</param>
        public void SetFaceDeterministic(int face)
        {
            if (face < 0 || face > 5)
            {
                Debug.LogError($"Invalid face index for deterministic set: {face}");
                return;
            }

            CurrentFace = face;
            OriginalFace = face;
            IsOverridden = false;
        }

        /// <summary>
        /// Get the ShapeType corresponding to this die's current face.
        /// Only valid for Shape dice.
        /// </summary>
        public ShapeType GetShapeType()
        {
            if (Type != DiceType.Shape)
            {
                Debug.LogError("GetShapeType called on non-shape dice");
                return ShapeType.TShape; // fallback
            }

            return CurrentFace switch
            {
                0 => ShapeType.TShape,
                1 => ShapeType.ZShape,
                2 => ShapeType.SquareShape,
                3 => ShapeType.LShape,
                4 => ShapeType.LineShape,
                5 => ShapeType.SingleShape,
                _ => ShapeType.TShape
            };
        }

        /// <summary>
        /// Get the BuildingType corresponding to this die's current face.
        /// Only valid for Building dice.
        /// </summary>
        public BuildingType GetBuildingType()
        {
            if (Type != DiceType.Building)
            {
                Debug.LogError("GetBuildingType called on non-building dice");
                return BuildingType.Industrial;
            }

            return CurrentFace switch
            {
                0 => BuildingType.Industrial,
                1 => BuildingType.Residential,
                2 => BuildingType.Commercial,
                3 => BuildingType.School,
                4 => BuildingType.Park,
                5 => BuildingType.Water,
                _ => BuildingType.Industrial
            };
        }

        /// <summary>
        /// Returns the face as a string for debugging.
        /// </summary>
        public string GetFaceName()
        {
            if (Type == DiceType.Shape)
                return GetShapeType().ToString();
            else
                return GetBuildingType().ToString();
        }

        /// <summary>
        /// Override the current face with a specific face index.
        /// Used for wildcards. Resets on next roll.
        /// </summary>
        public void OverrideFace(int faceIndex)
        {
            if (faceIndex < 0 || faceIndex > 5)
            {
                Debug.LogError($"Invalid face index for override: {faceIndex}");
                return;
            }

            CurrentFace = faceIndex;
            IsOverridden = true;
        }

        /// <summary>
        /// Get the original face from last roll (not affected by overrides).
        /// Used for star awarding.
        /// </summary>
        public int GetOriginalFace()
        {
            return OriginalFace;
        }
    }
}
using UnityEngine;

namespace PocketPlanner.Core
{
    /// <summary>
    /// Maps a die face index (0-5) to the correct UV region on a texture atlas,
    /// allowing custom icons for each Shape and Building type.
    ///
    /// ── Texture Atlas Layout ────────────────────────────────────────────────
    /// Create two 3x2 sprite sheets (one for Shape dice, one for Building dice).
    /// Each cell is one face icon, arranged left-to-right, top-to-bottom:
    ///
    ///   Shape atlas:
    ///     [0] TShape   [1] ZShape   [2] SquareShape
    ///     [3] LShape   [4] LineShape [5] SingleShape
    ///
    ///   Building atlas:
    ///     [0] Industrial  [1] Residential  [2] Commercial
    ///     [3] School      [4] Park         [5] Water
    ///
    /// ── Material Setup ──────────────────────────────────────────────────────
    /// 1. Create one Material for shape dice and one for building dice.
    /// 2. Assign the matching atlas texture to each material.
    /// 3. This script adjusts the material's _MainTex tiling (1/3, 1/2) and
    ///    offset to zoom into the correct cell at runtime.
    ///
    /// ── Usage ───────────────────────────────────────────────────────────────
    /// Call ApplyFaceToMaterial(material, faceIndex, diceType) after a die settles.
    /// DiceVisual.cs calls this automatically if you wire it up via the Inspector.
    /// </summary>
    public static class DiceFaceIconMapper
    {
        // Atlas is 3 columns x 2 rows
        private const int Columns = 3;
        private const int Rows    = 2;

        private static readonly Vector2 TileSize = new Vector2(1f / Columns, 1f / Rows);

        /// <summary>
        /// Adjusts a material's texture tiling and offset so that only the cell
        /// for the given faceIndex is visible, effectively showing the correct icon.
        ///
        /// faceIndex: 0-5, matching the Dice.CurrentFace value.
        /// diceType:  used for debug labelling only; you pass the correct material in.
        /// </summary>
        public static void ApplyFaceToMaterial(Material material, int faceIndex, DiceType diceType)
        {
            if (material == null)
            {
                Debug.LogWarning($"[DiceFaceIconMapper] Null material passed for {diceType} face {faceIndex}.");
                return;
            }

            faceIndex = Mathf.Clamp(faceIndex, 0, 5);

            // Row 0 = top row  (faces 0,1,2), Row 1 = bottom row (faces 3,4,5)
            int col = faceIndex % Columns; // 0, 1, 2
            int row = faceIndex / Columns; // 0 or 1

            // UV offset: flip row because Unity UVs start at bottom-left
            float uOffset = col  * TileSize.x;
            float vOffset = (Rows - 1 - row) * TileSize.y; // flip vertically

            material.mainTextureScale  = TileSize;
            material.mainTextureOffset = new Vector2(uOffset, vOffset);
        }

        /// <summary>
        /// Returns a human-readable name for a shape die face index.
        /// Useful for debug logging without needing a Dice instance.
        /// </summary>
        public static string GetShapeFaceName(int faceIndex)
        {
            return faceIndex switch
            {
                0 => "TShape",
                1 => "ZShape",
                2 => "SquareShape",
                3 => "LShape",
                4 => "LineShape",
                5 => "SingleShape",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Returns a human-readable name for a building die face index.
        /// </summary>
        public static string GetBuildingFaceName(int faceIndex)
        {
            return faceIndex switch
            {
                0 => "Industrial",
                1 => "Residential",
                2 => "Commercial",
                3 => "School",
                4 => "Park",
                5 => "Water",
                _ => "Unknown"
            };
        }
    }
}

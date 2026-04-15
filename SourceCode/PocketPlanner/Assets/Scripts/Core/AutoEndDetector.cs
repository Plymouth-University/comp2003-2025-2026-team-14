using System.Collections.Generic;
using UnityEngine;

namespace PocketPlanner.Core
{
    public class AutoEndDetector
    {
        private TilemapManager tilemapManager;
        private DiceManager diceManager;
        private GameManager gameManager;
        private ShapeManager shapeManager;

        // Reusable dummy shape for validation (avoids creating/destroying GameObjects)
        private ShapeController dummyShape;

        public AutoEndDetector(TilemapManager tilemapManager, DiceManager diceManager,
                               GameManager gameManager, ShapeManager shapeManager)
        {
            this.tilemapManager = tilemapManager;
            this.diceManager = diceManager;
            this.gameManager = gameManager;
            this.shapeManager = shapeManager;
            InitializeDummyShape();
        }

        private void InitializeDummyShape()
        {
            // Create a hidden GameObject with ShapeController component
            GameObject dummyObj = new GameObject("AutoEndDetector_DummyShape");
            dummyObj.SetActive(false); // Hide from scene
            dummyObj.hideFlags = HideFlags.HideAndDontSave;
            dummyShape = dummyObj.AddComponent<ShapeController>();
            // ShapeData will be set per validation

            // Manually set tilemapManager reference since Start() won't be called on inactive GameObject
            if (tilemapManager != null)
            {
                dummyShape.SetTilemapManager(tilemapManager);
            }
        }

        /// <summary>
        /// Check if any valid placement exists for current dice pool.
        /// Returns true if at least one valid placement exists.
        /// </summary>
        public bool CheckAnyValidPlacementExists()
        {
            // Get shape dice from pool
            var shapeDice = diceManager.GetShapeDice();
            if (shapeDice == null || shapeDice.Count == 0) return false;

            // Check if water die is present in building dice
            bool waterDiePresent = CheckWaterDiePresent();

            Debug.Log($"AutoEndDetector: Checking valid placements. Water die present={waterDiePresent}");

            // Get positions to check once (same for all shape dice)
            List<GridPosition> positionsToCheck = GetPositionsToCheck();
            if (positionsToCheck.Count == 0) return false;

            // For each shape die, check if shape can be placed
            foreach (var shapeDie in shapeDice)
            {
                ShapeType shapeType = shapeDie.GetShapeType();
                if (CanShapeBePlaced(shapeType, waterDiePresent, positionsToCheck))
                    return true;
            }

            return false;
        }

        private bool CheckWaterDiePresent()
        {
            var buildingDice = diceManager.GetBuildingDice();
            if (buildingDice == null) return false;

            foreach (var die in buildingDice)
            {
                if (die.GetBuildingType() == BuildingType.Water)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Custom placement rules check for auto-end detection.
        /// Uses waterDiePresent flag (water die in dice pool) instead of gameManager.WaterDieUsedThisTurn.
        /// </summary>
        private bool CheckPlacementRulesForAutoEnd(ShapeController shape, bool waterDiePresent)
        {
            // Basic validation must pass
            if (!shape.isValidPosition)
                return false;

            bool firstTurnCompleted = gameManager.FirstTurnCompleted;
            int selectedStartingPos = gameManager.SelectedStartingPosition;

            // ====================================================================
            // PLACEMENT RULES LOGIC (Note: Water die does NOT bypass first turn rule)
            // ====================================================================
            // Order of checks:
            // 1. First turn: must overlap selected starting position (applies regardless of water die)
            // 2. Water die present: must be adjacent to river tile
            // 3. Subsequent turns (no water die): must be adjacent to existing building

            // First turn check (applies to ALL placements, including water die)
            if (!firstTurnCompleted)
            {
                // If no starting position selected yet, invalid
                if (selectedStartingPos == 0)
                {
                    Debug.LogWarning("AutoEndDetector: No starting position selected for first turn.");
                    return false;
                }
                bool overlaps = shape.OverlapsStartingPosition(selectedStartingPos);
                if (!overlaps) return false;

                // First turn passed, now check water die rule if applicable
                if (waterDiePresent)
                {
                    return shape.IsAdjacentToRiver();
                }

                // First turn passed, no water die - placement valid
                return true;
            }

            // First turn completed, check water die rule
            if (waterDiePresent)
            {
                return shape.IsAdjacentToRiver();
            }

            // Subsequent turn, no water die - must be adjacent to existing building
            return shape.IsAdjacentToExistingBuilding();
        }

        private bool CanShapeBePlaced(ShapeType shapeType, bool waterDiePresent, List<GridPosition> positionsToCheck)
        {
            // positionsToCheck already provided by caller
            if (positionsToCheck == null || positionsToCheck.Count == 0) return false;

            // Get shape data for this shape type
            ShapeData shapeData = GetShapeData(shapeType);
            if (shapeData == null)
            {
                Debug.LogWarning($"AutoEndDetector: No ShapeData found for shape type {shapeType}");
                return false;
            }

            // For each candidate position
            foreach (GridPosition centerPos in positionsToCheck)
            {
                // Test all rotations and flips
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    for (int flip = 0; flip < 2; flip++)
                    {
                        bool flipped = flip == 1;

                        // Configure dummy shape
                        dummyShape.shapeData = shapeData;
                        dummyShape.buildingType = BuildingType.Industrial; // Building type irrelevant for placement
                        dummyShape.position = centerPos;
                        dummyShape.RotationState = rotation;
                        dummyShape.IsFlipped = flipped;
                        dummyShape.isPlacedOnGrid = true;

                        // Update basic validity (boundaries, river, overlap)
                        dummyShape.UpdatePositionValidity();

                        // Check placement rules (adjacency, first turn, water die)
                        // Use custom validation that respects waterDiePresent flag (not waterDieUsedThisTurn)
                        if (CheckPlacementRulesForAutoEnd(dummyShape, waterDiePresent))
                            return true;
                    }
                }
            }

            return false;
        }

        private List<GridPosition> GetPositionsToCheck()
        {
            List<GridPosition> positions = new List<GridPosition>();

            bool firstTurn = !gameManager.FirstTurnCompleted;

            if (firstTurn)
            {
                // First turn: positions where shape can overlap starting position
                positions.AddRange(GetPositionsOverlappingStart(gameManager.SelectedStartingPosition));
            }
            else
            {
                // For non-first turns, check all empty non-river positions
                // Shape adjacency rules will be validated in CheckPlacementRulesForAutoEnd
                positions.AddRange(GetAllEmptyNonRiverPositions());
            }

            // Filter out positions that are occupied or river tiles (safety check)
            positions.RemoveAll(pos => tilemapManager.IsOccupied(pos) || tilemapManager.IsRiverTile(pos));

            return positions;
        }

        private List<GridPosition> GetAdjacentPositions()
        {
            List<GridPosition> adjacent = new List<GridPosition>();

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    if (tilemapManager.IsOccupied(pos)) continue;
                    if (tilemapManager.IsRiverTile(pos)) continue;

                    // Check if adjacent to any occupied tile
                    if (IsAdjacentToOccupied(pos))
                        adjacent.Add(pos);
                }
            }

            return adjacent;
        }

        private List<GridPosition> GetRiverAdjacentPositions()
        {
            List<GridPosition> adjacent = new List<GridPosition>();

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    if (tilemapManager.IsOccupied(pos)) continue;
                    if (tilemapManager.IsRiverTile(pos)) continue;

                    // Check if adjacent to any river tile
                    if (IsAdjacentToRiver(pos))
                        adjacent.Add(pos);
                }
            }

            return adjacent;
        }

        private List<GridPosition> GetPositionsOverlappingStart(int startingPositionNumber)
        {
            List<GridPosition> positions = new List<GridPosition>();

            if (startingPositionNumber <= 0) return positions; // No starting position selected

            // Get the grid position of the starting tile
            GridPosition startPos = GetStartingPosition(startingPositionNumber);
            if (startPos.x < 0) return positions; // Invalid

            // For each possible shape center position where shape could overlap startPos
            // We'll check a reasonable area around the start position (±2 tiles)
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    GridPosition center = new GridPosition(startPos.x + dx, startPos.y + dy);
                    if (tilemapManager.IsWithinGrid(center))
                        positions.Add(center);
                }
            }

            return positions;
        }

        private bool IsAdjacentToOccupied(GridPosition pos)
        {
            GridPosition[] neighbors = {
                new GridPosition(pos.x + 1, pos.y),
                new GridPosition(pos.x - 1, pos.y),
                new GridPosition(pos.x, pos.y + 1),
                new GridPosition(pos.x, pos.y - 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (tilemapManager.IsWithinGrid(neighbor) && tilemapManager.IsOccupied(neighbor))
                    return true;
            }

            return false;
        }

        private bool IsAdjacentToRiver(GridPosition pos)
        {
            GridPosition[] neighbors = {
                new GridPosition(pos.x + 1, pos.y),
                new GridPosition(pos.x - 1, pos.y),
                new GridPosition(pos.x, pos.y + 1),
                new GridPosition(pos.x, pos.y - 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (tilemapManager.IsWithinGrid(neighbor) && tilemapManager.IsRiverTile(neighbor))
                    return true;
            }

            return false;
        }

        private GridPosition GetStartingPosition(int startingNumber)
        {
            // Map starting position number (1-8) to grid coordinate
            // This logic should match TilemapManager's starting position mapping
            return tilemapManager.GetStartingTilePosition(startingNumber);
        }

        private List<GridPosition> GetAllEmptyNonRiverPositions()
        {
            List<GridPosition> positions = new List<GridPosition>();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    if (tilemapManager.IsOccupied(pos) || tilemapManager.IsRiverTile(pos))
                        continue;
                    positions.Add(pos);
                }
            }
            return positions;
        }

        private ShapeData GetShapeData(ShapeType shapeType)
        {
            // Get ShapeData from ShapeManager or Resources
            // Implementation depends on how shape data is stored
            // We'll add a method to ShapeManager to provide ShapeData
            if (shapeManager == null)
            {
                Debug.LogWarning("AutoEndDetector: ShapeManager not available");
                return null;
            }

            // Try to get ShapeData via ShapeManager
            // We'll need to add a GetShapeData method to ShapeManager
            // For now, we'll use reflection or fallback
            return shapeManager.GetShapeData(shapeType);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketPlanner.Core
{
    /// <summary>
    /// Utility class for converting between ShapeController instances and serializable data structures.
    /// Used for spectator mode to store/restore board states.
    /// </summary>
    public static class ShapeSerializationHelper
    {
        /// <summary>
        /// Convert a ShapeController to BoardShapeData for serialization.
        /// </summary>
        public static PocketPlanner.Multiplayer.BoardShapeData ShapeControllerToBoardShapeData(ShapeController shape, int starsAwarded = 0)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));

            if (shape.shapeData == null)
            {
                Debug.LogWarning("ShapeSerializationHelper: ShapeController has null shapeData");
                return null;
            }

            return new PocketPlanner.Multiplayer.BoardShapeData
            {
                shapeType = shape.shapeData.shapeName.ToString(),
                buildingType = shape.buildingType.ToString(),
                positionX = shape.position.x,
                positionY = shape.position.y,
                rotation = shape.RotationState,
                flipped = shape.IsFlipped,
                turnPlaced = 0, // TODO: Track turn placed if needed
                starsAwarded = starsAwarded
            };
        }

        /// <summary>
        /// Convert a PlacementActionData to BoardShapeData for consistent handling.
        /// </summary>
        public static PocketPlanner.Multiplayer.BoardShapeData PlacementActionToBoardShapeData(PocketPlanner.Multiplayer.PlacementActionData placement)
        {
            if (placement == null)
                throw new ArgumentNullException(nameof(placement));

            return new PocketPlanner.Multiplayer.BoardShapeData
            {
                shapeType = placement.shapeType,
                buildingType = placement.buildingType,
                positionX = placement.positionX,
                positionY = placement.positionY,
                rotation = placement.rotation,
                flipped = placement.flipped,
                turnPlaced = 0, // Placement doesn't have turnPlaced
                starsAwarded = placement.starsAwarded
            };
        }

        /// <summary>
        /// Create a ShapeController from BoardShapeData using ShapeManager.
        /// Note: This method assumes ShapeManager.Instance is available and has proper prefabs assigned.
        /// </summary>
        public static ShapeController CreateShapeFromBoardShapeData(
            PocketPlanner.Multiplayer.BoardShapeData boardShapeData,
            ShapeManager shapeManager,
            Transform parent = null)
        {
            if (boardShapeData == null)
                throw new ArgumentNullException(nameof(boardShapeData));
            if (shapeManager == null)
                throw new ArgumentNullException(nameof(shapeManager));

            // Parse enums from strings
            if (!Enum.TryParse<ShapeType>(boardShapeData.shapeType, out ShapeType shapeType))
            {
                Debug.LogError($"ShapeSerializationHelper: Failed to parse shapeType '{boardShapeData.shapeType}'");
                return null;
            }

            if (!Enum.TryParse<BuildingType>(boardShapeData.buildingType, out BuildingType buildingType))
            {
                Debug.LogError($"ShapeSerializationHelper: Failed to parse buildingType '{boardShapeData.buildingType}'");
                return null;
            }

            // Create shape at position
            GridPosition position = new GridPosition(boardShapeData.positionX, boardShapeData.positionY);
            shapeManager.CreateShape(shapeType, buildingType, position);

            ShapeController shapeController = shapeManager.activeShape;
            if (shapeController == null)
            {
                Debug.LogError("ShapeSerializationHelper: ShapeManager.CreateShape did not create active shape");
                return null;
            }

            // Apply rotation and flip
            shapeController.SetRotationState(boardShapeData.rotation);
            shapeController.SetFlipped(boardShapeData.flipped);

            // Confirm placement (makes shape permanent, not ghost)
            shapeController.MakeShapeNormal();
            shapeController.FinalizePlacement();
            shapeController.isPlacementConfirmed = true;
            shapeController.ResetDraggingState();

            // Add star visual if stars were awarded
            if (boardShapeData.starsAwarded > 0)
            {
                shapeManager.AddStarVisualToShape(shapeController, boardShapeData.starsAwarded);
            }

            // Set parent if specified
            if (parent != null)
            {
                shapeController.transform.SetParent(parent, false);
            }

            return shapeController;
        }

        /// <summary>
        /// Convert list of ShapeControllers to list of BoardShapeData.
        /// </summary>
        public static List<PocketPlanner.Multiplayer.BoardShapeData> ShapeControllersToBoardShapeDataList(
            List<ShapeController> shapes,
            Dictionary<ShapeController, int> starMap = null)
        {
            List<PocketPlanner.Multiplayer.BoardShapeData> result = new List<PocketPlanner.Multiplayer.BoardShapeData>();

            foreach (ShapeController shape in shapes)
            {
                if (shape == null || shape.shapeData == null)
                    continue;

                int stars = 0;
                if (starMap != null && starMap.TryGetValue(shape, out int starCount))
                {
                    stars = starCount;
                }

                var boardShapeData = ShapeControllerToBoardShapeData(shape, stars);
                if (boardShapeData != null)
                {
                    result.Add(boardShapeData);
                }
            }

            return result;
        }

        /// <summary>
        /// Create shapes from list of BoardShapeData.
        /// </summary>
        public static List<ShapeController> CreateShapesFromBoardShapeDataList(
            List<PocketPlanner.Multiplayer.BoardShapeData> boardShapeDataList,
            ShapeManager shapeManager,
            Transform parent = null)
        {
            List<ShapeController> result = new List<ShapeController>();

            if (boardShapeDataList == null || shapeManager == null)
                return result;

            foreach (var boardShapeData in boardShapeDataList)
            {
                ShapeController shape = CreateShapeFromBoardShapeData(boardShapeData, shapeManager, parent);
                if (shape != null)
                {
                    result.Add(shape);
                }
            }

            return result;
        }
    }
}
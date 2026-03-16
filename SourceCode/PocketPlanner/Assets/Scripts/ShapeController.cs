using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/*
T Shape Center(C):
    *C*
     *

Z Shape Center(C):
   **
    C*

Square Center(C):
    **
    C*

L Shape Center(C):
    *
    *
    C*

Line Center(C):
    *
    *
    C
    *

Single Center(C):
    C
*/

public class ShapeController : MonoBehaviour
{
    public BuildingType buildingType;
    public ShapeData shapeData;
    public GridPosition position; // Current position of the 'center' of the shape on the grid
    public int RotationState { get; private set; } // 0-3 representing the rotation of the shape
    public bool IsFlipped { get; private set; } // Whether the shape is flipped horizontally
    public bool isPlacedOnGrid = false;
    public bool isPlacementConfirmed = false; // true = Shape is no longer moveable
    public bool lastGhostValidity = false; // Used to track when validity changes for ghost color updates
    public bool isValidPosition = false; // true if shape position passes basic validation (boundaries, river, overlap)
    private Tilemap boardTilemap;
    private TilemapManager tilemapManager;
    private Vector3 cellCenterOffset = new Vector3(0.5f, 0.5f, 0); // Offset to center shape within tile



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boardTilemap = FindAnyObjectByType<Tilemap>();
        if (boardTilemap == null)
        {
            Debug.LogError("ShapeController: No Tilemap found in scene!");
        }

        tilemapManager = TilemapManager.Instance;
        if (tilemapManager == null)
        {
            Debug.LogError("ShapeController: No TilemapManager found in scene!");
        }

        // Initial validity check
        UpdatePositionValidity();
        MakeShapeGhost(); // Set initial color to ghost (valid or invalid)
    }

    private void OnShapeMovement(InputValue value) //Invoked by Input System
    {
        if (isPlacementConfirmed)
        {
            return; // No movement if already confirmed placement
        }
        Vector2 movement = value.Get<Vector2>();
        if (movement.x < 0)
        {
            MoveLeft();
        }
        else if (movement.x > 0)
        {
            MoveRight();
        }
        if (movement.y < 0)
        {
            MoveDown();
        }
        else if (movement.y > 0)
        {
            MoveUp();
        }
        UpdateGhostColor(); // Update ghost color based on new position validity
    }

    private void OnShapeConfirm() //Invoked by Input System
    {
        if (isPlacementConfirmed)
            return;

        // Check all placement rules
        if (!CheckPlacementRules())
        {
            Debug.Log("Shape placement invalid: fails validation rules.");
            return;
        }

        // Mark first turn as completed if this is the first placement
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && !gameManager.FirstTurnCompleted)
        {
            gameManager.CompleteFirstTurn();
            Debug.Log("First turn completed after successful placement.");
        }

        // Finalize placement (mark tiles occupied and make shape normal)
        MakeShapeNormal();
        FinalizePlacement();
        isPlacementConfirmed = true;
        Debug.Log("Shape placement confirmed.");

        // Notify GameManager to award stars and start new turn
        if (gameManager != null)
        {
            gameManager.OnShapePlacementConfirmed(this);
        }
    }

    public void MoveUp()
    {
        GridPosition newPos = new GridPosition(position.x, position.y + 1);
        if (!WouldBeInBounds(newPos)) return;
        transform.position += new Vector3(0, 1, 0);
        position.y += 1;
        UpdatePositionValidity();
    }
    public void MoveDown()
    {
        GridPosition newPos = new GridPosition(position.x, position.y - 1);
        if (!WouldBeInBounds(newPos)) return;
        transform.position += new Vector3(0, -1, 0);
        position.y -= 1;
        UpdatePositionValidity();
    }
    public void MoveLeft()
    {
        GridPosition newPos = new GridPosition(position.x - 1, position.y);
        if (!WouldBeInBounds(newPos)) return;
        transform.position += new Vector3(-1, 0, 0);
        position.x -= 1;
        UpdatePositionValidity();
    }
    public void MoveRight()
    {
        GridPosition newPos = new GridPosition(position.x + 1, position.y);
        if (!WouldBeInBounds(newPos)) return;
        transform.position += new Vector3(1, 0, 0);
        position.x += 1;
        UpdatePositionValidity();
    }

    private void UpdateVisual()
    {
        // Apply rotation and flip to transform
        transform.localRotation = Quaternion.Euler(0, 0, -90 * RotationState); // clockwise rotation
        transform.localScale = new Vector3(IsFlipped ? -1 : 1, 1, 1);
    }

    public void OnShapeRotate() //Invoked by Input System
    {
        if (isPlacementConfirmed) return;
        RotationState = (RotationState + 1) % 4;
        UpdateVisual();
        UpdatePositionValidity();
        UpdateGhostColor();
    }

    public void OnShapeFlip() //Invoked by Input System
    {
        if (isPlacementConfirmed) return;
        IsFlipped = !IsFlipped;
        UpdateVisual();
        UpdatePositionValidity();
        UpdateGhostColor();
    }

    public void SetRotationState(int newRotationState)
    {
        if (isPlacementConfirmed) return;
        RotationState = newRotationState % 4;
        UpdateVisual();
        UpdatePositionValidity();
    }

    public void SetFlipped(bool newFlipped)
    {
        if (isPlacementConfirmed) return;
        IsFlipped = newFlipped;
        UpdateVisual();
        UpdatePositionValidity();
    }

    private GridPosition TransformRelativePosition(GridPosition rel)
    {
        // Apply flip then rotation
        int x = rel.x;
        int y = rel.y;
        if (IsFlipped)
        {
            x = -x;
        }
        // Rotate clockwise: (x, y) -> (y, -x) for 90° rotation
        for (int i = 0; i < RotationState; i++)
        {
            int temp = x;
            x = y;
            y = -temp;
        }
        return new GridPosition(x, y);
    }

    public List<GridPosition> GetOccupiedPositions()
    {
        List<GridPosition> occupied = new List<GridPosition>();
        if (shapeData == null) return occupied;
        foreach (GridPosition rel in shapeData.relativeTilePositions)
        {
            GridPosition transformed = TransformRelativePosition(rel);
            occupied.Add(new GridPosition(position.x + transformed.x, position.y + transformed.y));
        }
        return occupied;
    }

    public List<GridPosition> GetOccupiedPositions(GridPosition center)
    {
        List<GridPosition> occupied = new List<GridPosition>();
        if (shapeData == null) return occupied;
        foreach (GridPosition rel in shapeData.relativeTilePositions)
        {
            GridPosition transformed = TransformRelativePosition(rel);
            occupied.Add(new GridPosition(center.x + transformed.x, center.y + transformed.y));
        }
        return occupied;
    }

    private bool WouldBeInBounds(GridPosition newCenter)
    {
        if (shapeData == null) return false;
        foreach (GridPosition rel in shapeData.relativeTilePositions)
        {
            GridPosition transformed = TransformRelativePosition(rel);
            GridPosition pos = new GridPosition(newCenter.x + transformed.x, newCenter.y + transformed.y);
            if (pos.x < 0 || pos.x >= 10 || pos.y < 0 || pos.y >= 10)
                return false;
        }
        return true;
    }

    public void SetGridPosition(GridPosition newPosition)
    {
        if (isPlacementConfirmed) return;

        position = newPosition;

        // Convert grid position to world position using tilemap
        if (tilemapManager != null)
        {
            // Use TilemapManager conversion (logical to world)
            Vector3 worldPos = tilemapManager.LogicalToWorld(newPosition);
            worldPos += cellCenterOffset; // Center shape within tile
            transform.position = worldPos;
        }
        else if (boardTilemap != null)
        {
            // Fallback for backward compatibility
            Vector3 worldPos = boardTilemap.CellToWorld(new Vector3Int(newPosition.x, newPosition.y, 0));
            worldPos += cellCenterOffset; // Center shape within tile
            transform.position = worldPos;
        }
        else
        {
            // Fallback: assume 1 unit per grid cell
            transform.position = new Vector3(newPosition.x, newPosition.y, 0);
        }
        UpdatePositionValidity();
    }

    /// <summary>
    /// Updates isValidPosition based on basic validation: boundaries, river tiles, overlap with existing buildings.
    /// Does not check adjacency rules (those are checked during confirmation).
    /// </summary>
    public void UpdatePositionValidity()
    {
        // Try to get TilemapManager if not set
        if (tilemapManager == null)
        {
            tilemapManager = TilemapManager.Instance;
            if (tilemapManager == null)
            {
                isValidPosition = false;
                return;
            }
        }

        if (shapeData == null)
        {
            isValidPosition = false;
            return;
        }

        List<GridPosition> occupied = GetOccupiedPositions();

        // Check boundaries (grid is 10x10)
        foreach (GridPosition pos in occupied)
        {
            if (pos.x < 0 || pos.x >= 10 || pos.y < 0 || pos.y >= 10)
            {
                isValidPosition = false;
                return;
            }
        }

        // Check river tiles and overlap
        foreach (GridPosition pos in occupied)
        {
            GridTile tile = tilemapManager.GetTile(pos);
            if (tile == null)
            {
                // Should not happen if boundaries passed, but safety
                isValidPosition = false;
                return;
            }
            if (tile.isRiverTile)
            {
                isValidPosition = false;
                return;
            }
            if (tile.occupyingShape != null && tile.occupyingShape != this)
            {
                // Overlap with another shape (excluding self)
                isValidPosition = false;
                return;
            }
        }

        // All checks passed
        isValidPosition = true;
    }

    /// <summary>
    /// Checks all placement rules (basic validation + adjacency rules).
    /// Returns true if shape can be placed at its current position.
    /// </summary>
    public bool CheckPlacementRules()
    {
        // Basic validation must pass
        if (!isValidPosition)
            return false;

        // Get game state
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("ShapeController: GameManager not found!");
            return false;
        }

        bool waterDieUsed = gameManager.WaterDieUsedThisTurn;
        bool firstTurnCompleted = gameManager.FirstTurnCompleted;
        int selectedStartingPos = gameManager.SelectedStartingPosition;

        Debug.Log($"CheckPlacementRules: waterDieUsed={waterDieUsed}, firstTurnCompleted={firstTurnCompleted}, selectedStartingPos={selectedStartingPos}");

        // ====================================================================
        // PLACEMENT RULES LOGIC (Note: Water die does NOT bypass first turn rule)
        // ====================================================================
        // Order of checks:
        // 1. First turn: must overlap selected starting position (applies regardless of water die)
        // 2. Water die: must be adjacent to river tile
        // 3. Subsequent turns (no water die): must be adjacent to existing building

        // First turn check (applies to ALL placements, including water die)
        if (!firstTurnCompleted)
        {
            Debug.Log("CheckPlacementRules: First turn not completed, checking starting position");
            // If no starting position selected yet, invalid
            if (selectedStartingPos == 0)
            {
                Debug.LogWarning("ShapeController: No starting position selected for first turn.");
                return false;
            }
            bool overlaps = OverlapsStartingPosition(selectedStartingPos);
            Debug.Log($"CheckPlacementRules: Overlaps starting position {selectedStartingPos}: {overlaps}");
            if (!overlaps) return false;

            // First turn passed, now check water die rule if applicable
            if (waterDieUsed)
            {
                Debug.Log("CheckPlacementRules: First turn + water die used, checking river adjacency");
                return IsAdjacentToRiver();
            }

            // First turn passed, no water die - placement valid
            Debug.Log("CheckPlacementRules: First turn passed, no water die - placement valid");
            return true;
        }

        // First turn completed, check water die rule
        if (waterDieUsed)
        {
            Debug.Log("CheckPlacementRules: Water die used (subsequent turn), checking river adjacency");
            return IsAdjacentToRiver();
        }

        // Subsequent turn, no water die - must be adjacent to existing building
        Debug.Log("CheckPlacementRules: Subsequent turn, checking adjacency to existing buildings");
        bool adjacent = IsAdjacentToExistingBuilding();
        Debug.Log($"CheckPlacementRules: Adjacent to existing building: {adjacent}");
        return adjacent;
    }

    /// <summary>
    /// Returns true if any tile of the shape is orthogonally adjacent to a river tile.
    /// </summary>
    private bool IsAdjacentToRiver()
    {
        if (tilemapManager == null)
        {
            tilemapManager = TilemapManager.Instance;
            if (tilemapManager == null)
            {
                Debug.LogError("IsAdjacentToRiver: TilemapManager not found!");
                return false;
            }
        }

        List<GridPosition> occupied = GetOccupiedPositions();
        Debug.Log($"IsAdjacentToRiver: Checking {occupied.Count} occupied positions for river adjacency");
        foreach (GridPosition pos in occupied)
        {
            // Check orthogonal neighbors
            GridPosition[] neighbors = new GridPosition[]
            {
                new GridPosition(pos.x + 1, pos.y),
                new GridPosition(pos.x - 1, pos.y),
                new GridPosition(pos.x, pos.y + 1),
                new GridPosition(pos.x, pos.y - 1)
            };
            foreach (GridPosition neighbor in neighbors)
            {
                bool isRiver = tilemapManager.IsRiverTile(neighbor);
                Debug.Log($"  Neighbor {neighbor} is river: {isRiver}");
                if (isRiver)
                    return true;
            }
        }
        Debug.Log("IsAdjacentToRiver: No adjacent river tiles found");
        return false;
    }

    /// <summary>
    /// Returns true if the shape overlaps the specified starting position number.
    /// </summary>
    private bool OverlapsStartingPosition(int startingPositionNumber)
    {
        // Starting position numbers are 1-8
        // Need to find which grid position corresponds to this number
        // We'll check all starting tiles and see if shape occupies that tile
        List<GridPosition> occupied = GetOccupiedPositions();
        foreach (GridPosition pos in occupied)
        {
            int number = tilemapManager.GetStartingPositionNumber(pos);
            if (number == startingPositionNumber)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the shape is orthogonally adjacent to any existing confirmed building.
    /// </summary>
    private bool IsAdjacentToExistingBuilding()
    {
        if (tilemapManager == null)
        {
            tilemapManager = TilemapManager.Instance;
            if (tilemapManager == null)
            {
                Debug.LogError("IsAdjacentToExistingBuilding: TilemapManager not found!");
                return false;
            }
        }

        List<GridPosition> occupied = GetOccupiedPositions();
        Debug.Log($"IsAdjacentToExistingBuilding: Checking {occupied.Count} occupied positions");
        foreach (GridPosition pos in occupied)
        {
            GridPosition[] neighbors = new GridPosition[]
            {
                new GridPosition(pos.x + 1, pos.y),
                new GridPosition(pos.x - 1, pos.y),
                new GridPosition(pos.x, pos.y + 1),
                new GridPosition(pos.x, pos.y - 1)
            };
            foreach (GridPosition neighbor in neighbors)
            {
                bool occupiedNeighbor = tilemapManager.IsOccupied(neighbor);
                Debug.Log($"  Neighbor {neighbor} occupied: {occupiedNeighbor}");
                if (occupiedNeighbor)
                    return true;
            }
        }
        Debug.Log("IsAdjacentToExistingBuilding: No adjacent occupied tiles found");
        return false;
    }

    /// <summary>
    /// Marks the shape's tiles as occupied by this shape.
    /// Should only be called when placement is confirmed.
    /// </summary>
    private void FinalizePlacement()
    {
        List<GridPosition> occupied = GetOccupiedPositions();
        Debug.Log($"FinalizePlacement: Marking {occupied.Count} tiles as occupied");
        foreach (GridPosition pos in occupied)
        {
            GridTile tile = tilemapManager.GetTile(pos);
            if (tile != null)
            {
                tile.occupyingShape = this;
                Debug.Log($"  Tile at logical {pos} now occupied by shape");
            }
            else
            {
                Debug.LogError($"  Tile at logical {pos} not found!");
            }
        }
    }


    /// <summary>
    /// Changes the shape's appearance to a "ghost" (semi-transparent) to indicate it's being moved for placement.
    /// Should be called when the player first places the shape.
    /// </summary>
    public void MakeShapeGhost()
    {
        if (!isPlacementConfirmed)
        {
            if (shapeData.shapeName == ShapeType.SingleShape) // Single shape has no child objects
            {
                SpriteRenderer s = transform.gameObject.GetComponent<SpriteRenderer>();
                if (CheckPlacementRules())
                {
                    s.color = new Color(16f/255f, 100f/255f, 8f/255f, 0.5f); // Semi-transparent green for valid Hex 106408
                }
                else
                {
                    s.color = new Color(100f/255f, 12f/255f, 8f/255f, 0.5f); // Semi-transparent red for invalid
                }
                s.sortingOrder = 1; // Ensure ghost is rendered above other tiles
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                SpriteRenderer s = child.gameObject.GetComponent<SpriteRenderer>();
                if (CheckPlacementRules())
                {
                    s.color = new Color(16f/255f, 100f/255f, 8f/255f, 0.5f); // Semi-transparent green for valid Hex 106408
                }
                else
                {
                    s.color = new Color(100f/255f, 12f/255f, 8f/255f, 0.5f); // Semi-transparent red for invalid
                }
                s.sortingOrder = 1; // Ensure ghost is rendered above other tiles
            } 
            
        }
    }

    /// <summary>
    /// Updates the color of the ghost shape based on its validity.
    /// Should be called when the shape moves.
    /// </summary>
    public void UpdateGhostColor()
    {
        bool isValid = CheckPlacementRules();
        if (isValid == lastGhostValidity)
        {
            return; // No change in validity, no need to update color
        }
        MakeShapeGhost(); // Reuse ghost coloring logic
        lastGhostValidity = isValid;
    }

    /// <summary>
    ///  Changes the shape's appearance back to normal (opaque, colored by building type).
    ///  Should be called when shape placement is confirmed.
    /// </summary>
    public void MakeShapeNormal() // Should be called when placement is confirmed to set color back to normal
    {
        if (shapeData.shapeName == ShapeType.SingleShape) // Single shape has no child objects
        {
            SpriteRenderer s = transform.gameObject.GetComponent<SpriteRenderer>();
            s.color = GetColorForBuildingType(buildingType);
            s.sortingOrder = 0; // Reset sorting order
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            SpriteRenderer s = child.gameObject.GetComponent<SpriteRenderer>();
            s.color = GetColorForBuildingType(buildingType);
            s.sortingOrder = 0; // Reset sorting order
        } 
    }

    public void ChangeShapeColor() // Temporary method to change color using sprites of child objects
    {
        if (shapeData.shapeName == ShapeType.SingleShape) // Single shape has no child objects
        {
            SpriteRenderer s = transform.gameObject.GetComponent<SpriteRenderer>();
            s.color = GetColorForBuildingType(buildingType);
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            SpriteRenderer s = child.gameObject.GetComponent<SpriteRenderer>();
            s.color = GetColorForBuildingType(buildingType);
        } 
    }

    private Color GetColorForBuildingType(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Industrial:
                return Color.yellow;
            case BuildingType.Residential:
                return Color.green;
            case BuildingType.Commercial:
                return Color.blue;
            case BuildingType.School:
                return Color.gray;
            case BuildingType.Park:
                return Color.white; 
            case BuildingType.Water:
                return Color.cyan;
            default:
                return Color.black;
        }
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.InputSystem;
using PocketPlanner.Core;

public class ShapeManager : MonoBehaviour
{
    [Header("Shape Type Prefabs")]
    [SerializeField] private GameObject TShapePrefab;
    [SerializeField] private GameObject LShapePrefab;
    [SerializeField] private GameObject SquareShapePrefab;
    [SerializeField] private GameObject LineShapePrefab;
    [SerializeField] private GameObject ZShapePrefab;
    [SerializeField] private GameObject SingleShapePrefab;
    public ShapeController activeShape;

    [Header("References")]
    [SerializeField] private Tilemap boardTilemap;
    [SerializeField] private DiceManager diceManager;
    private InputAction mouseClickAction;
    private InputAction mousePositionAction;
    private Camera mainCamera;
    private Vector3 centerTileWorldOffset = new Vector3(0.5f, 0.5f, 0); // Offset to center shape in tile


    void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        mouseClickAction = playerInput.actions["PlaceShapeInput"];
        mousePositionAction = playerInput.actions["MousePosition"];
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        if (boardTilemap == null)
        {
            // Try to find Tilemap in scene
            boardTilemap = FindAnyObjectByType<Tilemap>();
            if (boardTilemap == null)
            {
                Debug.LogError("ShapeManager: No Tilemap found in scene!");
            }
        }
        if (diceManager == null)
        {
            diceManager = FindAnyObjectByType<DiceManager>();
            if (diceManager == null)
            {
                Debug.LogError("ShapeManager: DiceManager not found in scene!");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void generateRandomShape(GridPosition? gridPos = null)
    {
        // Use default center if not specified
        GridPosition targetPos = gridPos.HasValue ? gridPos.Value : new GridPosition(5, 5);

        BuildingType buildingType = (BuildingType)UnityEngine.Random.Range(0, 5);
        ShapeType shapeType = (ShapeType)UnityEngine.Random.Range(0, 5);

        CreateShape(shapeType, buildingType, targetPos);
    }

    /// <summary>
    /// Create a shape with specified type and building at grid position.
    /// </summary>
    public void CreateShape(ShapeType shapeType, BuildingType buildingType, GridPosition? gridPos = null)
    {
        GridPosition targetPos = gridPos.HasValue ? gridPos.Value : new GridPosition(5, 5);
        Vector3 worldPos = GetWorldPositionFromGridPosition(targetPos);

        GameObject newShape = Instantiate(getShapePrefab(shapeType), worldPos, Quaternion.identity);
        newShape.transform.SetParent(this.transform, false);
        ShapeController shapeController = newShape.GetComponent<ShapeController>();
        activeShape = shapeController;
        activeShape.buildingType = buildingType;
        activeShape.ChangeShapeColor();
        activeShape.position = targetPos;
        activeShape.isPlacedOnGrid = true;
    }

    /// <summary>
    /// Try to create a shape using the currently selected dice.
    /// Returns true if shape was created, false if no valid dice selection.
    /// </summary>
    public bool TryCreateShapeFromSelectedDice(GridPosition? gridPos = null)
    {
        if (diceManager == null || !diceManager.HasValidSelection())
            return false;

        ShapeType? shapeType = diceManager.GetSelectedShapeType();
        BuildingType? buildingType = diceManager.GetBuildingTypeForShape();

        if (!shapeType.HasValue || !buildingType.HasValue)
        {
            // If buildingType is null, it could be because water die is selected but no building type chosen yet
            if (diceManager.IsSelectedBuildingWater() && !diceManager.IsWaterDieChosenBuildingTypeSet())
            {
                Debug.Log("Water die selected but no building type chosen yet. Please choose a building type.");
            }
            return false;
        }

        CreateShape(shapeType.Value, buildingType.Value, gridPos);
        return true;
    }

    /// <summary>
    /// Update the active shape to match currently selected dice.
    /// Only works if shape is placed but not yet confirmed.
    /// Returns true if shape was updated.
    /// </summary>
    public bool UpdateActiveShapeFromSelectedDice()
    {
        if (activeShape == null || !activeShape.isPlacedOnGrid || activeShape.isPlacementConfirmed)
            return false;
        if (diceManager == null || !diceManager.HasValidSelection())
            return false;

        ShapeType? selectedShapeType = diceManager.GetSelectedShapeType();
        BuildingType? selectedBuildingType = diceManager.GetBuildingTypeForShape();
        if (!selectedShapeType.HasValue || !selectedBuildingType.HasValue)
        {
            // If buildingType is null, it could be because water die is selected but no building type chosen yet
            if (diceManager.IsSelectedBuildingWater() && !diceManager.IsWaterDieChosenBuildingTypeSet())
            {
                Debug.Log("Water die selected but no building type chosen yet. Cannot update shape.");
            }
            return false;
        }

        // Check if shape type changed (handle null shapeData)
        bool shapeTypeChanged = activeShape.shapeData == null ||
                                activeShape.shapeData.shapeName != selectedShapeType.Value;
        bool buildingTypeChanged = activeShape.buildingType != selectedBuildingType.Value;

        if (!shapeTypeChanged && !buildingTypeChanged)
            return false; // No changes needed

        if (shapeTypeChanged)
        {
            // Need to replace shape prefab
            ReplaceShapeWithNewType(selectedShapeType.Value, selectedBuildingType.Value);
        }
        else
        {
            // Only building type changed - update color
            activeShape.buildingType = selectedBuildingType.Value;
            activeShape.ChangeShapeColor();
        }
        return true;
    }

    /// <summary>
    /// Replace the active shape with a new shape type while preserving position, rotation, and flip.
    /// </summary>
    private void ReplaceShapeWithNewType(ShapeType newShapeType, BuildingType newBuildingType)
    {
        if (activeShape == null) return;

        // Save current state
        GridPosition oldPos = activeShape.position;
        int oldRotation = activeShape.RotationState;
        bool oldFlipped = activeShape.IsFlipped;

        // Destroy old shape
        Destroy(activeShape.gameObject);

        // Create new shape
        CreateShape(newShapeType, newBuildingType, oldPos);
        if (activeShape == null) return;

        // Restore rotation and flip
        activeShape.SetRotationState(oldRotation);
        activeShape.SetFlipped(oldFlipped);
    }

    // Helper methods for grid conversion and placement
    private Vector3 GetWorldPositionFromGridPosition(GridPosition gridPos)
    {
        if (TilemapManager.Instance != null && TilemapManager.Instance.boardTilemap != null)
        {
            // Use TilemapManager conversion (logical to world)
            Vector3 p = TilemapManager.Instance.LogicalToWorld(gridPos);
            p += centerTileWorldOffset; // Center shape within tile
            return p;
        }
        else if (boardTilemap != null)
        {
            // Fallback for backward compatibility
            Vector3 p = boardTilemap.CellToWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
            p += centerTileWorldOffset;
            return p;
        }
        else
        {
            // Fallback: assume 1 unit per grid cell
            return new Vector3(gridPos.x, gridPos.y, 0);
        }
    }

    private GridPosition GetGridPositionFromScreen(Vector2 screenPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
        worldPos.z = 0;
        if (TilemapManager.Instance != null && TilemapManager.Instance.boardTilemap != null)
        {
            // Use TilemapManager conversion (world to logical)
            return TilemapManager.Instance.WorldToLogical(worldPos);
        }
        else if (boardTilemap != null)
        {
            // Fallback for backward compatibility
            Vector3Int cellPos = boardTilemap.WorldToCell(worldPos);
            return new GridPosition(cellPos.x, cellPos.y);
        }
        else
        {
            // Fallback: round to nearest integer
            return new GridPosition(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
        }
    }

    public void PlaceShapeAtGridPosition(GridPosition gridPos)
    {
        // Ensure clicked position is within grid boundaries
        if (TilemapManager.Instance == null || !TilemapManager.Instance.IsWithinGrid(gridPos))
            return;

        if (activeShape == null || activeShape.isPlacementConfirmed)
        {
            // Try to create shape from selected dice at clicked position
            bool shapeCreated = TryCreateShapeFromSelectedDice(gridPos);
            if (!shapeCreated)
            {
                Debug.LogWarning("Cannot create shape: no valid dice selection.");
                // Fallback to random shape for testing (remove later)
                // generateRandomShape(gridPos);
            }
        }
        else if (activeShape.isPlacedOnGrid && !activeShape.isPlacementConfirmed)
        {
            return; // Shape is already placed and being manipulated, do nothing
        }
        else
        {
            // Move shape to clicked position
            activeShape.SetGridPosition(gridPos);
        }
    }

    public void OnPlaceShapeInput()
    {
        // Read the current mouse position when click is performed
        Vector2 screenPosition = mousePositionAction.ReadValue<Vector2>();
        GridPosition gridPos = GetGridPositionFromScreen(screenPosition);
        PlaceShapeAtGridPosition(gridPos);
    }

    private GameObject getShapePrefab(ShapeType shapeType)
    {
        switch (shapeType)
        {
            case ShapeType.TShape:
                return TShapePrefab;
            case ShapeType.LShape:
                return LShapePrefab;
            case ShapeType.SquareShape:
                return SquareShapePrefab;
            case ShapeType.LineShape:
                return LineShapePrefab;
            case ShapeType.ZShape:
                return ZShapePrefab;
            case ShapeType.SingleShape:
                return SingleShapePrefab;
            default:
                return null;
        }
    }
}

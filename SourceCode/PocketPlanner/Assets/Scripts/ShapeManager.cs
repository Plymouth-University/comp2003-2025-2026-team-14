using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using PocketPlanner.Core;
using PocketPlanner.Multiplayer;

public class ShapeManager : MonoBehaviour
{
    // Singleton instance
    public static ShapeManager Instance { get; private set; }

    [Header("Shape Type Prefabs")]
    [SerializeField] private GameObject TShapePrefab;
    [SerializeField] private GameObject LShapePrefab;
    [SerializeField] private GameObject SquareShapePrefab;
    [SerializeField] private GameObject LineShapePrefab;
    [SerializeField] private GameObject ZShapePrefab;
    [SerializeField] private GameObject SingleShapePrefab;

    [Header("Shape Data Assets")]
    [SerializeField] private ShapeData TShapeData;
    [SerializeField] private ShapeData LShapeData;
    [SerializeField] private ShapeData SquareShapeData;
    [SerializeField] private ShapeData LineShapeData;
    [SerializeField] private ShapeData ZShapeData;
    [SerializeField] private ShapeData SingleShapeData;

    public ShapeController activeShape;

    [Header("References")]
    [SerializeField] private Tilemap boardTilemap;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private Sprite starSprite; // Sprite for stars awarded for selecting double rolls
    private InputAction mouseClickAction;
    private InputAction mousePositionAction;
    private InputAction touchPositionAction; // For mobile touch input
    private Camera mainCamera;
    private Vector3 centerTileWorldOffset = new Vector3(0.5f, 0.5f, 0); // Offset to center shape in tile


    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

      // Instead of: PlayerInput playerInput = GetComponent<PlayerInput>();
      // Use:
      PlayerInput playerInput = GameManager.Instance.GetComponent<PlayerInput>();
      if (playerInput != null)
      {
          mouseClickAction = playerInput.actions["PlaceShapeInput"];
          mousePositionAction = playerInput.actions["MousePosition"];
          touchPositionAction = playerInput.actions["TouchPosition"];
      }

        // Load ShapeData assets if not assigned
        LoadShapeDataFromResources();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;

        // Enable EnhancedTouch support for better touch input handling
        EnhancedTouchSupport.Enable();

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

    private void LoadShapeDataFromResources()
    {
        // If all ShapeData fields are already assigned, skip loading
        if (TShapeData != null && LShapeData != null && SquareShapeData != null &&
            LineShapeData != null && ZShapeData != null && SingleShapeData != null)
            return;

        // Load all ShapeData assets from Resources
        ShapeData[] allShapeData = Resources.LoadAll<ShapeData>("ScriptableObjects");
        if (allShapeData == null || allShapeData.Length == 0)
        {
            Debug.LogError("ShapeManager: No ShapeData assets found in Resources/ScriptableObjects!");
            return;
        }

        // Assign to appropriate fields based on shapeName
        foreach (ShapeData data in allShapeData)
        {
            switch (data.shapeName)
            {
                case ShapeType.TShape:
                    if (TShapeData == null) TShapeData = data;
                    break;
                case ShapeType.LShape:
                    if (LShapeData == null) LShapeData = data;
                    break;
                case ShapeType.SquareShape:
                    if (SquareShapeData == null) SquareShapeData = data;
                    break;
                case ShapeType.LineShape:
                    if (LineShapeData == null) LineShapeData = data;
                    break;
                case ShapeType.ZShape:
                    if (ZShapeData == null) ZShapeData = data;
                    break;
                case ShapeType.SingleShape:
                    if (SingleShapeData == null) SingleShapeData = data;
                    break;
                default:
                    Debug.LogWarning($"ShapeManager: Unknown shape name {data.shapeName} in loaded ShapeData");
                    break;
            }
        }

        // Log any missing ShapeData after loading
        if (TShapeData == null) Debug.LogError("ShapeManager: TShapeData still missing after loading from Resources!");
        if (LShapeData == null) Debug.LogError("ShapeManager: LShapeData still missing after loading from Resources!");
        if (SquareShapeData == null) Debug.LogError("ShapeManager: SquareShapeData still missing after loading from Resources!");
        if (LineShapeData == null) Debug.LogError("ShapeManager: LineShapeData still missing after loading from Resources!");
        if (ZShapeData == null) Debug.LogError("ShapeManager: ZShapeData still missing after loading from Resources!");
        if (SingleShapeData == null) Debug.LogError("ShapeManager: SingleShapeData still missing after loading from Resources!");
    }

    // Update is called once per frame
    void Update()
    {
        if (activeShape != null && activeShape.isPlacementConfirmed)
        {
            activeShape = null;
        }
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
        activeShape.MakeShapeGhost(); // Change to ghost after update
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
    public Vector3 GetWorldPositionFromGridPosition(GridPosition gridPos)
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
        // Disable shape placement while spectating other players
        if (GameManager.Instance != null && GameManager.Instance.IsSpectatingOtherPlayers)
        {
            Debug.Log("ShapeManager: Shape placement disabled while spectating other players.");
            return;
        }

        // Prevent shape placement in multiplayer mode if player has already completed this turn
        if (GameManager.Instance != null && MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode)
        {
            if (GameManager.Instance.IsWaitingForOtherPlayers)
            {
                Debug.LogWarning($"ShapeManager: Attempted to place shape on turn {GameManager.Instance.CurrentTurn} after already completing this turn. Placement ignored.");
                return;
            }
        }

        Vector2 screenPosition = Vector2.zero;
        bool positionFound = false;

        // Priority 1: Check for active touches using EnhancedTouch API
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            // Use the first active touch position
            UnityEngine.InputSystem.EnhancedTouch.Touch primaryTouch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
            screenPosition = primaryTouch.screenPosition;
            positionFound = true;
        }

        // Priority 2: Check touch position from InputAction (might work even if touch just ended)
        if (!positionFound)
        {
            Vector2 touchPos = touchPositionAction.ReadValue<Vector2>();
            if (touchPos != Vector2.zero)
            {
                screenPosition = touchPos;
                positionFound = true;
            }
        }

        // Priority 3: Fall back to mouse position (for desktop/editor or as last resort)
        if (!positionFound)
        {
            //screenPosition = mousePositionAction.ReadValue<Vector2>();
            // Note: mousePosition might also be (0,0) if mouse is not present
            // but we assume at least one input method is available
        }

        GridPosition gridPos = GetGridPositionFromScreen(screenPosition);

        // If first turn not completed and clicking a starting tile, skip shape placement
        // (starting tile clicks are for starting position selection)
        if (GameManager.Instance != null && !GameManager.Instance.FirstTurnCompleted &&
            TilemapManager.Instance != null && TilemapManager.Instance.IsStartingTile(gridPos))
        {
            return;
        }

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

    /// <summary>
    /// Get ShapeData for a given shape type.
    /// </summary>
    public ShapeData GetShapeData(ShapeType shapeType)
    {
        switch (shapeType)
        {
            case ShapeType.TShape:
                return TShapeData;
            case ShapeType.LShape:
                return LShapeData;
            case ShapeType.SquareShape:
                return SquareShapeData;
            case ShapeType.LineShape:
                return LineShapeData;
            case ShapeType.ZShape:
                return ZShapeData;
            case ShapeType.SingleShape:
                return SingleShapeData;
            default:
                Debug.LogError($"GetShapeData: No ShapeData found for shape type {shapeType}");
                return null;
        }
    }

    /// <summary>
    /// Adds a star visual overlay to a shape that earned stars during placement.
    /// </summary>
    /// <param name="shape">The shape controller to add star to</param>
    /// <param name="starCount">Number of stars awarded (1 or 2)</param>
    public void AddStarVisualToShape(ShapeController shape, int starCount)
    {
        if (shape == null || starCount <= 0 || starSprite == null)
        {
            Debug.LogWarning($"Cannot add star visual: shape={shape}, starCount={starCount}, starSprite={starSprite}");
            return;
        }

        // Determine parent transform for star: for SingleShape use shape itself, otherwise use first child
        Transform parentTransform;
        if (shape.shapeData == null)
        {
            Debug.LogWarning("Shape shapeData is null, using shape transform as parent for star.");
            parentTransform = shape.transform;
        }
        else if (shape.shapeData.shapeName == ShapeType.SingleShape)
        {
            parentTransform = shape.transform;
        }
        else
        {
            int childCount = shape.transform.childCount;
            // Use random child of shape as parent for star
            if (childCount == 0)
            {
                Debug.LogWarning("Shape has no children, cannot add star visual.");
                return;
            }
            UnityEngine.Random.InitState(DateTime.Now.Millisecond); // Ensure random rotation each time
            int randomChild = UnityEngine.Random.Range(0, childCount);
            parentTransform = shape.transform.GetChild(randomChild); 
        }

        // Create star GameObject
        GameObject starObject = new GameObject("Star");
        starObject.transform.SetParent(parentTransform, false);
        starObject.transform.localPosition = Vector3.zero;
        starObject.transform.localScale = Vector3.one; 

        // Add SpriteRenderer
        SpriteRenderer starRenderer = starObject.AddComponent<SpriteRenderer>();
        starRenderer.sprite = starSprite;
        starRenderer.sortingLayerID = SortingLayer.NameToID("TopGrid");
        starRenderer.sortingOrder = 3; // Ensure star renders above shape
        starRenderer.color = Color.gold; // Optional: set star color to yellow

        // If 2 stars, create a second star offset slightly
        if (starCount >= 2)
        {
            GameObject starObject2 = new GameObject("Star2");
            starObject2.transform.SetParent(parentTransform, false);
            starObject2.transform.localPosition = new Vector3(0.2f, 0.2f, 0); // Offset slightly
            starObject2.transform.localScale = Vector3.one;
            SpriteRenderer starRenderer2 = starObject2.AddComponent<SpriteRenderer>();
            starRenderer2.sprite = starSprite;
            starRenderer2.sortingLayerID = SortingLayer.NameToID("TopGrid");
            starRenderer2.sortingOrder = 3;
            starRenderer2.color = Color.gold;
        }

        Debug.Log($"Added {starCount} star visual(s) to shape {shape.shapeData.shapeName}");
    }

    /// <summary>
    /// Get all currently placed shapes on the board as BoardShapeData list.
    /// Used for backing up local player's board before switching to spectator mode.
    /// </summary>
    public System.Collections.Generic.List<PocketPlanner.Multiplayer.BoardShapeData> GetCurrentBoardState()
    {
        System.Collections.Generic.List<PocketPlanner.Multiplayer.BoardShapeData> boardState =
            new System.Collections.Generic.List<PocketPlanner.Multiplayer.BoardShapeData>();

        if (TilemapManager.Instance == null)
        {
            Debug.LogError("ShapeManager: TilemapManager.Instance is null, cannot get board state");
            return boardState;
        }

        // Get unique shapes from grid (same logic as SyncManager.SerializeBoardState)
        System.Collections.Generic.HashSet<ShapeController> uniqueShapes = new System.Collections.Generic.HashSet<ShapeController>();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                GridTile tile = TilemapManager.Instance.gridTiles[x, y];
                if (tile != null && tile.occupyingShape != null && !uniqueShapes.Contains(tile.occupyingShape))
                {
                    uniqueShapes.Add(tile.occupyingShape);
                }
            }
        }

        // Convert each shape to BoardShapeData
        foreach (ShapeController shape in uniqueShapes)
        {
            if (shape == null || shape.shapeData == null) continue;

            // TODO: Track stars per shape - for now set to 0
            var boardShapeData = PocketPlanner.Core.ShapeSerializationHelper.ShapeControllerToBoardShapeData(shape, 0);
            if (boardShapeData != null)
            {
                boardState.Add(boardShapeData);
            }
        }

        Debug.Log($"ShapeManager: GetCurrentBoardState found {boardState.Count} shapes");
        return boardState;
    }

    /// <summary>
    /// Remove all shapes from the board (destroy GameObjects).
    /// Used when switching to spectator mode to clear local player's shapes.
    /// </summary>
    public void ClearAllShapes()
    {
        if (TilemapManager.Instance == null)
        {
            Debug.LogError("ShapeManager: TilemapManager.Instance is null, cannot clear shapes");
            return;
        }

        int shapeCount = 0;
        System.Collections.Generic.HashSet<ShapeController> uniqueShapes = new System.Collections.Generic.HashSet<ShapeController>();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                GridTile tile = TilemapManager.Instance.gridTiles[x, y];
                if (tile != null && tile.occupyingShape != null && !uniqueShapes.Contains(tile.occupyingShape))
                {
                    uniqueShapes.Add(tile.occupyingShape);
                }
            }
        }

        foreach (ShapeController shape in uniqueShapes)
        {
            if (shape != null && shape.gameObject != null)
            {
                Destroy(shape.gameObject);
                shapeCount++;
            }
        }

        // Also clear active shape reference
        activeShape = null;

        Debug.Log($"ShapeManager: ClearAllShapes destroyed {shapeCount} shapes");
    }

    /// <summary>
    /// Create shapes from BoardShapeData list and place them on the board.
    /// Used for displaying opponent's board in spectator mode.
    /// </summary>
    /// <param name="boardState">List of BoardShapeData representing shapes to place</param>
    /// <returns>List of created ShapeController instances</returns>
    public System.Collections.Generic.List<ShapeController> PlaceShapesFromBoardState(
        System.Collections.Generic.List<PocketPlanner.Multiplayer.BoardShapeData> boardState)
    {
        System.Collections.Generic.List<ShapeController> createdShapes =
            new System.Collections.Generic.List<ShapeController>();

        if (boardState == null || boardState.Count == 0)
        {
            Debug.Log("ShapeManager: PlaceShapesFromBoardState - empty board state");
            return createdShapes;
        }

        // Use ShapeSerializationHelper to create shapes
        createdShapes = PocketPlanner.Core.ShapeSerializationHelper.CreateShapesFromBoardShapeDataList(
            boardState, this, this.transform);

        Debug.Log($"ShapeManager: PlaceShapesFromBoardState created {createdShapes.Count} shapes");
        return createdShapes;
    }
}

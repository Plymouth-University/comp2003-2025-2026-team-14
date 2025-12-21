using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.InputSystem;

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

        // Calculate world position from grid position
        Vector3 worldPos = GetWorldPositionFromGridPosition(targetPos);

        GameObject newShape = Instantiate(getShapePrefab(shapeType), worldPos, Quaternion.identity);
        newShape.transform.SetParent(this.transform, false);
        ShapeController shapeController = newShape.GetComponent<ShapeController>();
        activeShape = shapeController;
        activeShape.buildingType = buildingType;
        activeShape.changeShapeColor();
        activeShape.position = targetPos;
        activeShape.isPlacedOnGrid = true;
    }

    // Helper methods for grid conversion and placement
    private Vector3 GetWorldPositionFromGridPosition(GridPosition gridPos)
    {
        if (boardTilemap != null)
        {
            Vector3 p =  boardTilemap.CellToWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
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
        if (boardTilemap != null)
        {
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
        if (activeShape == null || activeShape.isPlacementConfirmed)
        {
            // Generate new shape at clicked position
            generateRandomShape(gridPos);
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

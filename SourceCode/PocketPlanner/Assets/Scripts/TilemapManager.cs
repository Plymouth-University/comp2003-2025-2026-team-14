using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TilemapManager : MonoBehaviour
{
    public static TilemapManager Instance { get; private set; }

    [Header("Board Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] public Tilemap boardTilemap;
    [SerializeField] private TileBase tileAsset; // The "WhiteTile"
    [SerializeField] private TileBase riverTileAsset; // The "RiverTile"

    // Offset to center board around (0,0)
    private int startX;
    private int startY;

    // 10x10 array of GridTiles for easy access
    public GridTile[,] gridTiles = new GridTile[10,10]; // [x,y] format

    // Y=0 is bottom row, Y=9 is top row
    // X=0 is leftmost column, X=9 is rightmost column
    private List<GridPosition> riverTiles = new List<GridPosition>{
        new GridPosition(4, 9),
        new GridPosition(4, 8),
        new GridPosition(4, 7),
        new GridPosition(4, 6),
        new GridPosition(4, 5),
        new GridPosition(5, 5),
        new GridPosition(5, 4),
        new GridPosition(5, 3),
        new GridPosition(5, 2),
        new GridPosition(5, 1),
        new GridPosition(5, 0),
    };

    private List<GridPosition> startingTiles = new List<GridPosition>{
        new GridPosition(1,8),
        new GridPosition(3,6),
        new GridPosition(0,3),
        new GridPosition(2,1),
        new GridPosition(8,2),
        new GridPosition(6,4),
        new GridPosition(9,7),
        new GridPosition(7,9),
    };

    private Dictionary<int, Color> originalStartingTileColors = new Dictionary<int, Color>();

    [Header("Camera Settings")]
    [SerializeField] private float boardPadding = 1.0f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Calculate offset to center board around (0,0)
        startX = -(width / 2);
        startY = -(height / 2);

        Debug.Log($"TilemapManager: Board offset startX={startX}, startY={startY}");
        Debug.Log($"TilemapManager: Logical (0,0) -> Tilemap cell ({startX},{startY})");
        Debug.Log($"TilemapManager: Logical (9,9) -> Tilemap cell ({startX + 9},{startY + 9})");
    }

    private void Start()
    {
        GenerateBoard();
        AdjustCamera();

        // Test coordinate conversions
        TestCoordinateConversions();
    }

    private void GenerateBoard()
    {
        // Clear any existing tiles first
        boardTilemap.ClearAllTiles();

        // Use pre-calculated offset to center the board around (0,0)
        // With Tilemaps, coordinates are integers.
        // If Width is 10, we go from -5 to +4 (total 10 tiles)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create the coordinate vector
                Vector3Int pos = new Vector3Int(startX + x, startY + y, 0);

                // Initialize river tiles 
                if (riverTiles.Contains(new GridPosition(x, y)))
                {
                    boardTilemap.SetTile(pos, riverTileAsset);
                    GameObject tileObject = boardTilemap.GetInstantiatedObject(pos);
                    InitializeRiverTile(tileObject, x, y);
                }
                // Initialize starting tiles 
                else if (startingTiles.Contains(new GridPosition(x, y)))
                {
                    // You can set a different tile for starting tiles
                    // For now, we just use the regular tileAsset and use index to get starting position number
                    boardTilemap.SetTile(pos, tileAsset);
                    GameObject tileObject = boardTilemap.GetInstantiatedObject(pos);
                    InitializeStartingTile(tileObject, x, y);
                }
                // Initialize regular tiles
                else 
                {
                    // Set the tile at this coordinate
                    boardTilemap.SetTile(pos, tileAsset);
                    GameObject tileObject = boardTilemap.GetInstantiatedObject(pos);
                    InitializeRegularTile(tileObject, x, y);
                }

                // Colorize logic can go here later (e.g. changing color based on zones)
                // boardTilemap.SetTileFlags(pos, TileFlags.None);
                // boardTilemap.SetColor(pos, Color.white);
            }
        }
        
        // Compress bounds to ensure the Tilemap knows its exact size
        boardTilemap.CompressBounds();
    }

    private void AdjustCamera()
    {
        Camera cam = Camera.main;
        
        // 1. Get the visual bounds of the populated Tilemap
        BoundsInt bounds = boardTilemap.cellBounds;
        
        // 2. Calculate the target vertical size
        // bounds.size.y gives us the height in cells (10)
        float verticalSize = (bounds.size.y / 2.0f) + boardPadding;

        // 3. Aspect Ratio Logic (Same as before)
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = (float)bounds.size.x / (float)bounds.size.y;

        if (screenRatio >= targetRatio)
        {
            cam.orthographicSize = verticalSize;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            cam.orthographicSize = verticalSize * differenceInSize;
        }
        
        // 4. Center the camera exactly on the Tilemap's center
        Vector3 center = boardTilemap.localBounds.center;
        cam.transform.position = new Vector3(center.x, center.y, -10);
    }
    private void InitializeRiverTile(GameObject tileObject, int x, int y)
    {
        GridTile gridTile = tileObject.GetComponent<GridTile>();
        gridTile.gridPosition = new GridPosition(x, y);
        gridTile.isRiverTile = true;
        gridTile.isStartingTile = false;
        tileObject.name = $"River Tile: ({x},{y})";
        gridTiles[x, y] = gridTile;
    }
    private void InitializeStartingTile(GameObject tileObject, int x, int y)
    {
        GridTile gridTile = tileObject.GetComponent<GridTile>();
        gridTile.gridPosition = new GridPosition(x, y);
        gridTile.isStartingTile = true;
        gridTile.isRiverTile = false;
        gridTile.startingPositionNumber = startingTiles.IndexOf(new GridPosition(x, y)) + 1; // 1-based index
        tileObject.name = $"Starting Tile {gridTile.startingPositionNumber}: ({x},{y})";
        TextMeshPro tmp = tileObject.AddComponent<TextMeshPro>();
        tmp.text = gridTile.startingPositionNumber.ToString();
        tmp.fontSize = 2;
        tmp.color = Color.magenta;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.margin = new Vector4(0.1f, 0.1f, 0, 0);
        tmp.rectTransform.sizeDelta = new Vector2(1, 1);
        tmp.sortingOrder = 1; // Ensure text is rendered above the tile
        gridTiles[x, y] = gridTile;
    }
    private void InitializeRegularTile(GameObject tileObject, int x, int y)
    {
        GridTile gridTile = tileObject.GetComponent<GridTile>();
        gridTile.gridPosition = new GridPosition(x, y);
        gridTile.isRiverTile = false;
        gridTile.isStartingTile = false;
        tileObject.name = $"Tile: ({x},{y})";
        gridTiles[x, y] = gridTile;
    }

    /// <summary>
    /// Returns the GridTile at the given grid position, or null if out of bounds.
    /// </summary>
    public GridTile GetTile(GridPosition pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            return null;
        return gridTiles[pos.x, pos.y];
    }

    /// <summary>
    /// Returns true if the given position is a river tile.
    /// </summary>
    public bool IsRiverTile(GridPosition pos)
    {
        return riverTiles.Contains(pos);
    }

    /// <summary>
    /// Returns true if the given position is a starting tile.
    /// </summary>
    public bool IsStartingTile(GridPosition pos)
    {
        return startingTiles.Contains(pos);
    }

    /// <summary>
    /// Returns the starting position number (1-8) at the given position, or 0 if not a starting tile.
    /// </summary>
    public int GetStartingPositionNumber(GridPosition pos)
    {
        int index = startingTiles.IndexOf(pos);
        return index >= 0 ? index + 1 : 0;
    }

    /// <summary>
    /// Get the grid position of a starting tile by its number (1-8).
    /// </summary>
    public GridPosition GetStartingTilePosition(int number)
    {
        if (number < 1 || number > 8) return new GridPosition(-1, -1);
        return startingTiles[number - 1];
    }

    /// <summary>
    /// Highlight a starting tile by number (1-8). Changes its Tilemap color tint.
    /// </summary>
    public void HighlightStartingTile(int number)
    {
        Debug.Log($"TilemapManager: Attempting to highlight starting tile {number}");
        GridPosition pos = GetStartingTilePosition(number);
        if (pos.x < 0 || pos.y < 0)
        {
            Debug.LogWarning($"TilemapManager: Invalid starting tile number {number}");
            return;
        }
        Vector3Int cellPos = LogicalToTilemapCell(pos);
        if (boardTilemap == null)
        {
            Debug.LogError("TilemapManager: boardTilemap is null");
            return;
        }

        // Get current color from tilemap
        Color originalColor = boardTilemap.GetColor(cellPos);

        // Store original color if not already stored
        if (!originalStartingTileColors.ContainsKey(number))
        {
            originalStartingTileColors[number] = originalColor;
            Debug.Log($"TilemapManager: Stored original color {originalColor} for tile {number}");
        }

        // Ensure tile flags allow color changes
        boardTilemap.SetTileFlags(cellPos, TileFlags.None);

        // Set highlight color
        boardTilemap.SetColor(cellPos, Color.yellow);
        Debug.Log($"TilemapManager: Highlighted starting tile {number} at {pos} (cell {cellPos})");
    }

    /// <summary>
    /// Unhighlight all starting tiles, restoring original colors.
    /// </summary>
    public void UnhighlightAllStartingTiles()
    {
        Debug.Log($"TilemapManager: Unhighlighting all starting tiles (count: {originalStartingTileColors.Count})");
        foreach (var kvp in originalStartingTileColors)
        {
            GridPosition pos = GetStartingTilePosition(kvp.Key);
            Vector3Int cellPos = LogicalToTilemapCell(pos);
            if (boardTilemap == null)
            {
                Debug.LogWarning("TilemapManager: boardTilemap is null during unhighlight");
                continue;
            }

            // Ensure tile flags allow color changes
            boardTilemap.SetTileFlags(cellPos, TileFlags.None);

            // Restore original color from dictionary
            boardTilemap.SetColor(cellPos, kvp.Value);
            Debug.Log($"TilemapManager: Restored original color {kvp.Value} for tile {kvp.Key} at cell {cellPos}");
        }
        originalStartingTileColors.Clear();
    }

    /// <summary>
    /// Returns true if the given position is occupied by a confirmed shape.
    /// </summary>
    public bool IsOccupied(GridPosition pos)
    {
        GridTile tile = GetTile(pos);
        bool occupied = tile != null && tile.occupyingShape != null;
        if (occupied)
        {
            Debug.Log($"IsOccupied: Tile at logical {pos} is occupied by shape");
        }
        return occupied;
    }

    /// <summary>
    /// Returns true if the given grid position is within the board boundaries (0-9).
    /// </summary>
    public bool IsWithinGrid(GridPosition pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    // ===== Coordinate Conversion Methods =====

    /// <summary>
    /// Converts logical grid coordinates (0-9) to tilemap cell coordinates (-5 to +4).
    /// </summary>
    public Vector3Int LogicalToTilemapCell(GridPosition logicalPos)
    {
        return new Vector3Int(logicalPos.x + startX, logicalPos.y + startY, 0);
    }

    /// <summary>
    /// Converts tilemap cell coordinates (-5 to +4) to logical grid coordinates (0-9).
    /// </summary>
    public GridPosition TilemapCellToLogical(Vector3Int cellPos)
    {
        return new GridPosition(cellPos.x - startX, cellPos.y - startY);
    }

    /// <summary>
    /// Converts logical grid coordinates (0-9) to world position (center of tile).
    /// </summary>
    public Vector3 LogicalToWorld(GridPosition logicalPos)
    {
        Vector3Int cellPos = LogicalToTilemapCell(logicalPos);
        return boardTilemap.CellToWorld(cellPos);
    }

    /// <summary>
    /// Converts world position to logical grid coordinates (0-9).
    /// </summary>
    public GridPosition WorldToLogical(Vector3 worldPos)
    {
        Vector3Int cellPos = boardTilemap.WorldToCell(worldPos);
        return TilemapCellToLogical(cellPos);
    }

    /// <summary>
    /// Test method to verify coordinate conversions work correctly.
    /// </summary>
    private void TestCoordinateConversions()
    {
        Debug.Log("=== Coordinate Conversion Tests ===");

        // Test basic conversions
        GridPosition logicalCenter = new GridPosition(5, 5);
        Vector3Int tilemapCell = LogicalToTilemapCell(logicalCenter);
        Vector3 worldPos = LogicalToWorld(logicalCenter);
        GridPosition convertedBack = WorldToLogical(worldPos);

        Debug.Log($"Logical {logicalCenter} -> Tilemap cell {tilemapCell}");
        Debug.Log($"Logical {logicalCenter} -> World {worldPos}");
        Debug.Log($"World {worldPos} -> Logical {convertedBack}");

        // Verify round-trip
        if (logicalCenter == convertedBack)
        {
            Debug.Log("✓ Round-trip conversion successful");
        }
        else
        {
            Debug.LogError($"✗ Round-trip failed: {logicalCenter} != {convertedBack}");
        }

        // Test river tile positions (logical coordinates)
        foreach (GridPosition riverPos in riverTiles)
        {
            if (IsRiverTile(riverPos))
            {
                Debug.Log($"✓ River tile at logical {riverPos} correctly identified");
            }
            else
            {
                Debug.LogError($"✗ River tile at logical {riverPos} not identified");
            }
        }

        Debug.Log("=== End of Coordinate Tests ===");
    }
}
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TilemapManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private Tilemap boardTilemap;
    [SerializeField] private TileBase tileAsset; // The "WhiteTile"
    [SerializeField] private TileBase riverTileAsset; // The "RiverTile" 

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

    [Header("Camera Settings")]
    [SerializeField] private float boardPadding = 1.0f;

    private void Start()
    {
        GenerateBoard();
        AdjustCamera();
    }

    private void GenerateBoard()
    {
        // Clear any existing tiles first
        boardTilemap.ClearAllTiles();

        // Calculate offset to center the board around (0,0)
        // With Tilemaps, coordinates are integers. 
        // If Width is 10, we go from -5 to +4 (total 10 tiles)
        int startX = -(width / 2);
        int startY = -(height / 2);

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
}
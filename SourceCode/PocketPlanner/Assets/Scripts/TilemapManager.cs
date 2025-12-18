using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private Tilemap boardTilemap;
    [SerializeField] private TileBase tileAsset; // The "WhiteTile"
    [SerializeField] private TileBase riverTileAsset; // The "RiverTile" 
    private List<GridPosition> riverTiles = new List<GridPosition>{
        new GridPosition(9, 4),
        new GridPosition(8, 4),
        new GridPosition(7, 4),
        new GridPosition(6, 4),
        new GridPosition(5, 4),
        new GridPosition(5, 5),
        new GridPosition(4, 5),
        new GridPosition(3, 5),
        new GridPosition(2, 5),
        new GridPosition(1, 5),
        new GridPosition(0, 5),
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
                
                // Set the tile at this coordinate
                boardTilemap.SetTile(pos, tileAsset);
                
                // Colorize logic can go here later (e.g. changing color based on zones)
                // boardTilemap.SetTileFlags(pos, TileFlags.None);
                // boardTilemap.SetColor(pos, Color.white);
            }
        }

        // Initialize river tiles for demonstration
        foreach (GridPosition pos in riverTiles)
        {
            Vector3Int tilePos = new Vector3Int(startX + pos.x, startY + pos.y, 0);
            boardTilemap.SetTile(tilePos, riverTileAsset);
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
}
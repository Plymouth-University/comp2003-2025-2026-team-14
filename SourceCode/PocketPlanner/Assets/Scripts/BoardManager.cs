using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject cellTextPrefab;
    [SerializeField] private GameObject cellRiverPrefab;
    
    // US 3.1 AC: "Entire 10x10 grid fits within Safe Area"
    [Header("Camera Settings")]
    [SerializeField] private float boardPadding = 1.0f; 

    private void Start()
    {
        GenerateGrid();
        AdjustCamera();
    }

    private void GenerateGrid()
    {
        // Calculate offset to center the board at (0,0)
        // If width is 10, the left edge is at -5, right edge at +5
        float startX = -(width / 2.0f) + 0.5f; // +0.5f because pivot is center of sprite
        float startY = -(height / 2.0f) + 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create the position vector
                Vector3 pos = new Vector3(startX + x, startY + y, 0);
                
                // Spawn the cell
                GameObject newCell = Instantiate(cellTextPrefab, pos, Quaternion.identity);
                
                // Parent it to this object to keep Hierarchy clean
                newCell.transform.SetParent(this.transform, false);
                
                // Name it for debugging (e.g., "Cell 0,0")
                newCell.name = $"Cell {x},{y}";

            }
        }
    }

    private void AdjustCamera()
    {
        // Reference the main camera
        Camera cam = Camera.main;
        
        // Target vertical size = half the board height + padding
        float verticalSize = (height / 2.0f) + boardPadding;
        
        // Check screen aspect ratio to ensure width fits (Portrait Mode handling)
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = (float)width / (float)height;

        if (screenRatio >= targetRatio)
        {
            // Screen is wider than board (Landscape), stick to vertical size
            cam.orthographicSize = verticalSize;
        }
        else
        {
            // Screen is narrower than board (Portrait), zoom out to fit width
            float differenceInSize = targetRatio / screenRatio;
            cam.orthographicSize = verticalSize * differenceInSize;
        }
    }
}
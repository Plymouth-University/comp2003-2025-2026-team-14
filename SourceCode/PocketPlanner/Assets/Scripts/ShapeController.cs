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
    private int rotationState; // 0-3 representing the rotation of the shape
    private bool isFlipped; // Whether the shape is flipped horizontally
    public bool isPlacedOnGrid = false; 
    public bool isPlacementConfirmed = false; // true = Shape is no longer moveable
    private Tilemap boardTilemap;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boardTilemap = FindAnyObjectByType<Tilemap>();
        if (boardTilemap == null)
        {
            Debug.LogError("ShapeController: No Tilemap found in scene!");
        }

    }

    // Update is called once per frame
    void Update()
    {
        
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
    }

    private void OnShapeConfirm() //Invoked by Input System
    {
        isPlacementConfirmed = true;
    }

    public void MoveUp()
    {
        transform.position += new Vector3(0, 1, 0);
        position.y += 1;
    }
    public void MoveDown()
    {
        transform.position += new Vector3(0, -1, 0);
        position.y -= 1;
    }
    public void MoveLeft()
    {
        transform.position += new Vector3(-1, 0, 0);
        position.x -= 1;
    }
    public void MoveRight()
    {
        transform.position += new Vector3(1, 0, 0);
        position.x += 1;
    }

    private void UpdateVisual()
    {
        // Apply rotation and flip to transform
        transform.localRotation = Quaternion.Euler(0, 0, -90 * rotationState); // clockwise rotation
        transform.localScale = new Vector3(isFlipped ? -1 : 1, 1, 1);
    }

    public void OnShapeRotate() //Invoked by Input System
    {
        if (isPlacementConfirmed) return;
        rotationState = (rotationState + 1) % 4;
        UpdateVisual();
    }

    public void OnShapeFlip() //Invoked by Input System
    {
        if (isPlacementConfirmed) return;
        isFlipped = !isFlipped;
        UpdateVisual();
    }

    private GridPosition TransformRelativePosition(GridPosition rel)
    {
        // Apply flip then rotation
        int x = rel.x;
        int y = rel.y;
        if (isFlipped)
        {
            x = -x;
        }
        // Rotate clockwise: (x, y) -> (y, -x) for 90° rotation
        for (int i = 0; i < rotationState; i++)
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

    public void SetGridPosition(GridPosition newPosition)
    {
        if (isPlacementConfirmed) return;

        position = newPosition;

        // Convert grid position to world position using tilemap
        if (boardTilemap != null)
        {
            Vector3 worldPos = boardTilemap.CellToWorld(new Vector3Int(newPosition.x, newPosition.y, 0));
            transform.position = worldPos;
        }
        else
        {
            // Fallback: assume 1 unit per grid cell
            transform.position = new Vector3(newPosition.x, newPosition.y, 0);
        }
    }
    
    public void changeShapeColor() // Temporary method to change color using sprites of child objects
    {
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

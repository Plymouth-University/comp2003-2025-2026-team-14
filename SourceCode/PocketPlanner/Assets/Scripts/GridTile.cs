using UnityEngine;

public class GridTile : MonoBehaviour
{
    public GridPosition gridPosition; // The position of this tile in the grid
    public bool isRiverTile; // Whether this tile is a river tile
    public bool isStartingTile; // Whether this tile is a starting tile
    public int startingPositionNumber; // The starting position number if it's a starting tile
   
    public ShapeController occupyingShape; // The shape currently on this tile (or null if none)
    public Zone zone; // The zone that the shape on this tile belongs to (or null if none)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShapeController : MonoBehaviour
{
    public BuildingType buildingType;
    public ShapeData shapeData;
    public GridPosition position; // Current position of the 'center' of the shape on the grid
    private int rotationState; // 0-3 representing the rotation of the shape
    private bool isFlipped; // Whether the shape is flipped horizontally
    public bool isPlacementConfirmed = false;


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnShapeMovement(InputValue value)
    {
        if (isPlacementConfirmed)
        {
            return; // No movement if already placed
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

    private void OnShapeConfirm()
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

}

using UnityEngine;

public class ShapeController : MonoBehaviour
{
    public ShapeData shapeData;
    private GridPosition position; // Current position on the grid
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        Initialise(shapeData, new GridPosition(0, 0));
    }

    // Update is called once per frame
    void Update()
    {
        // For testing purposes, move the shape down every second
        if (Time.frameCount % 60 == 0) // Assuming 60 FPS
        {
            MoveUp();
        }
    }

    public void MoveUp()
    {
        transform.position += new Vector3(0, 1, 0);
        position.y += 1;
    }

    public void Initialise(ShapeData data, GridPosition startPos)
    {
        shapeData = data;
        position = startPos;
        // Additional initialization logic here
    }
}

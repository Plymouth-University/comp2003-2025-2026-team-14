using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (activeShape == null || activeShape.isPlacementConfirmed)
        {
            generateRandomShape();
        }
    }

    public void generateRandomShape()
    {
        // Placeholder logic to generate a random shape
        BuildingType buildingType = (BuildingType)Random.Range(0, 5);
        ShapeType shapeType = (ShapeType)Random.Range(0, 5);
        GameObject newShape = Instantiate(getShapePrefab(shapeType), new Vector3(0.5f, 0.5f, 0), Quaternion.identity);
        newShape.transform.SetParent(this.transform, false);
        ShapeController shapeController = newShape.GetComponent<ShapeController>();
        // Additional randomization logic can go here
        activeShape = shapeController;
        activeShape.buildingType = buildingType;
        activeShape.position = new GridPosition(5, 5); // Start in center of board (0.5, 0.5) in world coords
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

using System.Collections.Generic;


public class Zone
{
    public BuildingType zoneType;
    public List<ShapeController> shapesInZone = new List<ShapeController>();

    public Zone(BuildingType type, ShapeController initialShape)
    {
        zoneType = type;
        shapesInZone.Add(initialShape);
    }

    public int getUniqueShapeCount()
    {
        HashSet<ShapeType> uniqueShapes = new HashSet<ShapeType>();
        foreach (ShapeController shape in shapesInZone)
        {
            uniqueShapes.Add(shape.shapeData.shapeName);
        }
        return uniqueShapes.Count;
    }
}
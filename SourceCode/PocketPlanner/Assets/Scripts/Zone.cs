using System.Collections.Generic;
using System.Linq;

public class Zone
{
    public BuildingType zoneType;
    public List<ShapeController> shapesInZone = new List<ShapeController>();

    public Zone(BuildingType type, ShapeController initialShape)
    {
        zoneType = type;
        shapesInZone.Add(initialShape);
    }

    public int GetUniqueShapeCount()
    {
        HashSet<ShapeType> uniqueShapes = new HashSet<ShapeType>();
        foreach (ShapeController shape in shapesInZone)
        {
            if (shape.shapeData != null)
            {
                uniqueShapes.Add(shape.shapeData.shapeName);
            }
        }
        return uniqueShapes.Count;
    }

    public void AddShape(ShapeController shape)
    {
        if (!shapesInZone.Contains(shape))
        {
            shapesInZone.Add(shape);
        }
    }

    public void MergeZone(Zone otherZone)
    {
        if (otherZone == null || otherZone.zoneType != zoneType) return;

        foreach (ShapeController shape in otherZone.shapesInZone)
        {
            AddShape(shape);
        }
        // Note: other zone should be discarded after merge
    }

    public bool ContainsShape(ShapeController shape)
    {
        return shapesInZone.Contains(shape);
    }
}
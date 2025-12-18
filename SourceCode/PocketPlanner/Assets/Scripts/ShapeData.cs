using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShapeData", menuName = "Scriptable Objects/ShapeData")]
public class ShapeData : ScriptableObject
{
    public string shapeName;
    public Sprite icon;
    public Color color;
    // Define the shape layout relative to center (0,0)
    public List<GridPosition> relativeTilePositions;
}

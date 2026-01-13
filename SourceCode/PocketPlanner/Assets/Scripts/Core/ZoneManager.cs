using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    private List<Zone> zones = new List<Zone>();
    private TilemapManager tilemapManager;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        tilemapManager = TilemapManager.Instance;
        if (tilemapManager == null)
        {
            Debug.LogError("ZoneManager: TilemapManager not found!");
        }
    }

    /// <summary>
    /// Adds a shape to the appropriate zone(s), creating or merging zones as needed.
    /// Should be called after a shape is placed and confirmed.
    /// </summary>
    public void AddShape(ShapeController shape)
    {
        if (shape == null) return;

        // Only Industrial, Residential, and Commercial buildings form zones
        BuildingType buildingType = shape.buildingType;
        if (buildingType != BuildingType.Industrial &&
            buildingType != BuildingType.Residential &&
            buildingType != BuildingType.Commercial)
        {
            // Schools and Parks do not form zones, but we still need to update tile zone references (null)
            Debug.Log($"ZoneManager: Shape is {buildingType}, not a zone type. Zone reference set to null.");
            UpdateTileZoneReferences(shape, null);
            return;
        }

        // Find all zones of the same building type that are adjacent to this shape
        List<Zone> adjacentZones = FindAdjacentZones(shape, buildingType);
        Debug.Log($"ZoneManager: Found {adjacentZones.Count} adjacent zones for {buildingType} shape.");

        if (adjacentZones.Count == 0)
        {
            // No adjacent zone: create a new zone
            Zone newZone = new Zone(buildingType, shape);
            zones.Add(newZone);
            UpdateTileZoneReferences(shape, newZone);
            Debug.Log($"ZoneManager: Created new {buildingType} zone. Total zones: {zones.Count}");
        }
        else if (adjacentZones.Count == 1)
        {
            // One adjacent zone: add shape to that zone
            Zone zone = adjacentZones[0];
            zone.AddShape(shape);
            UpdateTileZoneReferences(shape, zone);
            Debug.Log($"ZoneManager: Added shape to existing {buildingType} zone. Unique shapes in zone: {zone.GetUniqueShapeCount()}");
        }
        else
        {
            // Multiple adjacent zones: merge all into the first zone, then add shape
            Zone primaryZone = adjacentZones[0];
            Debug.Log($"ZoneManager: Merging {adjacentZones.Count} zones for {buildingType}. Primary zone has {primaryZone.shapesInZone.Count} shapes.");
            for (int i = 1; i < adjacentZones.Count; i++)
            {
                Zone zoneToMerge = adjacentZones[i];
                primaryZone.MergeZone(zoneToMerge);
                zones.Remove(zoneToMerge);
                // Update all tiles in the merged zone to reference primary zone
                UpdateZoneReferencesForShapes(zoneToMerge, primaryZone);
                Debug.Log($"ZoneManager: Merged zone with {zoneToMerge.shapesInZone.Count} shapes.");
            }
            primaryZone.AddShape(shape);
            UpdateTileZoneReferences(shape, primaryZone);
            Debug.Log($"ZoneManager: After merge, primary zone has {primaryZone.shapesInZone.Count} shapes, unique shapes: {primaryZone.GetUniqueShapeCount()}");
        }
    }

    /// <summary>
    /// Finds all zones of the specified building type that are orthogonally adjacent to any tile of the shape.
    /// </summary>
    private List<Zone> FindAdjacentZones(ShapeController shape, BuildingType buildingType)
    {
        List<Zone> adjacentZones = new List<Zone>();
        HashSet<Zone> visited = new HashSet<Zone>();

        List<GridPosition> occupiedPositions = shape.GetOccupiedPositions();
        Debug.Log($"ZoneManager: Checking adjacency for shape at {occupiedPositions.Count} tiles.");
        foreach (GridPosition pos in occupiedPositions)
        {
            // Check orthogonal neighbors
            GridPosition[] neighbors = new GridPosition[]
            {
                new GridPosition(pos.x + 1, pos.y),
                new GridPosition(pos.x - 1, pos.y),
                new GridPosition(pos.x, pos.y + 1),
                new GridPosition(pos.x, pos.y - 1)
            };

            foreach (GridPosition neighbor in neighbors)
            {
                GridTile tile = tilemapManager.GetTile(neighbor);
                if (tile == null)
                {
                    continue;
                }
                if (tile.zone == null)
                {
                    continue;
                }
                if (tile.zone.zoneType != buildingType)
                {
                    continue;
                }
                if (!visited.Contains(tile.zone))
                {
                    visited.Add(tile.zone);
                    adjacentZones.Add(tile.zone);
                    Debug.Log($"ZoneManager: Adjacent zone found at neighbor {neighbor}, zone type {tile.zone.zoneType}, shape count {tile.zone.shapesInZone.Count}");
                }
            }
        }
        return adjacentZones;
    }

    /// <summary>
    /// Updates the zone reference on each tile occupied by the shape.
    /// </summary>
    private void UpdateTileZoneReferences(ShapeController shape, Zone zone)
    {
        List<GridPosition> occupiedPositions = shape.GetOccupiedPositions();
        foreach (GridPosition pos in occupiedPositions)
        {
            GridTile tile = tilemapManager.GetTile(pos);
            if (tile != null)
            {
                tile.zone = zone;
            }
        }
    }

    /// <summary>
    /// Updates zone references for all shapes in a zone to point to a new zone (after merge).
    /// </summary>
    private void UpdateZoneReferencesForShapes(Zone oldZone, Zone newZone)
    {
        foreach (ShapeController shape in oldZone.shapesInZone)
        {
            UpdateTileZoneReferences(shape, newZone);
        }
    }

    /// <summary>
    /// Removes a shape from its zone (if any). Used when a shape is removed (not needed in current game).
    /// </summary>
    public void RemoveShape(ShapeController shape)
    {
        // Not currently used; zones are never removed.
    }

    /// <summary>
    /// Returns all zones of a specific building type.
    /// </summary>
    public List<Zone> GetZonesOfType(BuildingType buildingType)
    {
        List<Zone> result = new List<Zone>();
        foreach (Zone zone in zones)
        {
            if (zone.zoneType == buildingType)
            {
                result.Add(zone);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns all zones.
    /// </summary>
    public List<Zone> GetAllZones()
    {
        return new List<Zone>(zones);
    }

    /// <summary>
    /// Clears all zones (for game reset).
    /// </summary>
    public void ClearZones()
    {
        zones.Clear();
    }

    /// <summary>
    /// Returns all zones orthogonally adjacent to a shape.
    /// </summary>
    public List<Zone> GetAdjacentZones(ShapeController shape)
    {
        if (shape == null || tilemapManager == null) return new List<Zone>();

        HashSet<Zone> adjacentZones = new HashSet<Zone>();
        List<GridPosition> occupiedPositions = shape.GetOccupiedPositions();

        foreach (GridPosition pos in occupiedPositions)
        {
            GridPosition[] neighbors = new GridPosition[]
            {
                new GridPosition(pos.x + 1, pos.y),
                new GridPosition(pos.x - 1, pos.y),
                new GridPosition(pos.x, pos.y + 1),
                new GridPosition(pos.x, pos.y - 1)
            };

            foreach (GridPosition neighbor in neighbors)
            {
                GridTile tile = tilemapManager.GetTile(neighbor);
                if (tile == null) continue;
                if (tile.zone != null)
                {
                    adjacentZones.Add(tile.zone);
                }
            }
        }

        return adjacentZones.ToList();
    }

    /// <summary>
    /// Returns all zones orthogonally adjacent to a specific grid position.
    /// </summary>
    public List<Zone> GetZonesAdjacentToPosition(GridPosition position)
    {
        if (tilemapManager == null) return new List<Zone>();

        HashSet<Zone> adjacentZones = new HashSet<Zone>();
        GridPosition[] neighbors = new GridPosition[]
        {
            new GridPosition(position.x + 1, position.y),
            new GridPosition(position.x - 1, position.y),
            new GridPosition(position.x, position.y + 1),
            new GridPosition(position.x, position.y - 1)
        };

        foreach (GridPosition neighbor in neighbors)
        {
            GridTile tile = tilemapManager.GetTile(neighbor);
            if (tile == null) continue;
            if (tile.zone != null)
            {
                adjacentZones.Add(tile.zone);
            }
        }

        return adjacentZones.ToList();
    }
}
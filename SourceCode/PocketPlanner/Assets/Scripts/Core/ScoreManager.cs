using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private ZoneManager zoneManager;
    private TilemapManager tilemapManager;
    private GameManager gameManager;

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
        zoneManager = ZoneManager.Instance;
        tilemapManager = TilemapManager.Instance;
        gameManager = GameManager.Instance;

        if (zoneManager == null)
            Debug.LogError("ScoreManager: ZoneManager not found!");
        if (tilemapManager == null)
            Debug.LogError("ScoreManager: TilemapManager not found!");
        if (gameManager == null)
            Debug.LogError("ScoreManager: GameManager not found!");
    }

    public void RefreshReferences(ZoneManager newZoneManager, TilemapManager newTilemapManager, GameManager newGameManager)
    {
        zoneManager = newZoneManager;
        tilemapManager = newTilemapManager;
        gameManager = newGameManager;

        if (zoneManager == null)
            Debug.LogError("ScoreManager: ZoneManager not found!");
        if (tilemapManager == null)
            Debug.LogError("ScoreManager: TilemapManager not found!");
        if (gameManager == null)
            Debug.LogError("ScoreManager: GameManager not found!");
    }

    /// <summary>
    /// Calculate complete score breakdown for the current game state.
    /// </summary>
    public ScoreComponents CalculateScore()
    {
        ScoreComponents components = new ScoreComponents();

        // Zone scores (Industrial, Residential, Commercial)
        components.industrialZoneScore = CalculateZoneScore(BuildingType.Industrial);
        components.residentialZoneScore = CalculateZoneScore(BuildingType.Residential);
        components.commercialZoneScore = CalculateZoneScore(BuildingType.Commercial);

        // Park scores
        components.parkScore = CalculateParkScore();

        // School scores
        components.schoolScore = CalculateSchoolScore();

        // Star score (already tracked by GameManager)
        components.starScore = gameManager.Stars; // Each star = 1 point

        // Penalties
        components.emptyCellPenalty = CalculateEmptyCellPenalty();
        components.wildcardCostTotal = gameManager.GetWildcardCostTotal();

        // Calculate total
        components.CalculateTotal();

        Debug.Log($"Score calculated: {components}");
        return components;
    }

    /// <summary>
    /// Calculate total score for all zones of a specific building type.
    /// </summary>
    private int CalculateZoneScore(BuildingType buildingType)
    {
        if (zoneManager == null) return 0;

        List<Zone> zones = zoneManager.GetZonesOfType(buildingType);
        int total = 0;

        foreach (Zone zone in zones)
        {
            total += CalculateZoneScore(zone);
        }

        return total;
    }

    /// <summary>
    /// Calculate score for a single zone based on unique shape count.
    /// Scoring table: 1=1, 2=2, 3=4, 4=7, 5=11, 6=16
    /// </summary>
    private int CalculateZoneScore(Zone zone)
    {
        if (zone == null) return 0;

        int uniqueShapes = zone.GetUniqueShapeCount();
        return UniqueShapesToScore(uniqueShapes);
    }

    /// <summary>
    /// Convert unique shape count to points according to scoring table.
    /// </summary>
    private int UniqueShapesToScore(int uniqueShapes)
    {
        // Scoring table from ruleset.md
        switch (uniqueShapes)
        {
            case 0: return 0;
            case 1: return 1;
            case 2: return 2;
            case 3: return 4;
            case 4: return 7;
            case 5: return 11;
            case 6: return 16;
            default:
                Debug.LogWarning($"Unexpected unique shape count: {uniqueShapes}");
                return 0;
        }
    }

    /// <summary>
    /// Calculate total score from all parks.
    /// +2 points per distinct contiguous zone orthogonally adjacent to park.
    /// Multiple parks can score from same zone.
    /// </summary>
    private int CalculateParkScore()
    {
        if (tilemapManager == null || zoneManager == null) return 0;

        int total = 0;

        // Get all park shapes by scanning the grid
        List<ShapeController> parkShapes = GetAllShapesOfType(BuildingType.Park);

        foreach (ShapeController park in parkShapes)
        {
            total += CalculateParkScore(park);
        }

        return total;
    }

    /// <summary>
    /// Calculate score for a single park.
    /// </summary>
    private int CalculateParkScore(ShapeController park)
    {
        if (park == null) return 0;

        HashSet<Zone> distinctZones = new HashSet<Zone>();
        List<GridPosition> occupiedPositions = park.GetOccupiedPositions();

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
                if (tile == null) continue;

                // Only zones (Industrial, Residential, Commercial) have zone references
                if (tile.zone != null)
                {
                    distinctZones.Add(tile.zone);
                }
            }
        }

        // +2 points per distinct zone
        return distinctZones.Count * 2;
    }

    /// <summary>
    /// Calculate total score from all schools.
    /// +2 points per residential building (shape) in the largest adjacent residential zone.
    /// One school per zone and one zone per school.
    /// </summary>
    private int CalculateSchoolScore()
    {
        if (tilemapManager == null || zoneManager == null) return 0;

        // Get all school shapes
        List<ShapeController> schoolShapes = GetAllShapesOfType(BuildingType.School);
        if (schoolShapes.Count == 0) return 0;

        // Get all residential zones
        List<Zone> residentialZones = zoneManager.GetZonesOfType(BuildingType.Residential);
        if (residentialZones.Count == 0) return 0;

        // Map each school to the largest adjacent residential zone not already assigned
        Dictionary<ShapeController, Zone> schoolToZone = new Dictionary<ShapeController, Zone>();
        HashSet<Zone> assignedZones = new HashSet<Zone>();

        // For each school, find adjacent residential zones and pick the largest unassigned one
        foreach (ShapeController school in schoolShapes)
        {
            Zone bestZone = null;
            int bestZoneSize = -1;

            List<Zone> adjacentZones = GetAdjacentZones(school);
            foreach (Zone zone in adjacentZones)
            {
                if (zone.zoneType != BuildingType.Residential) continue;
                if (assignedZones.Contains(zone)) continue;

                int zoneSize = zone.shapesInZone.Count;
                if (zoneSize > bestZoneSize)
                {
                    bestZoneSize = zoneSize;
                    bestZone = zone;
                }
            }

            if (bestZone != null)
            {
                schoolToZone[school] = bestZone;
                assignedZones.Add(bestZone);
            }
        }

        // Calculate score: +2 per residential building (shape) in each assigned zone
        int total = 0;
        foreach (var kvp in schoolToZone)
        {
            Zone zone = kvp.Value;
            total += zone.shapesInZone.Count * 2;
        }

        return total;
    }

    /// <summary>
    /// Get all zones orthogonally adjacent to a shape.
    /// </summary>
    private List<Zone> GetAdjacentZones(ShapeController shape)
    {
        HashSet<Zone> zones = new HashSet<Zone>();
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
                    zones.Add(tile.zone);
                }
            }
        }

        return zones.ToList();
    }

    /// <summary>
    /// Calculate empty cell penalty: -1 per empty non-river cell.
    /// </summary>
    private int CalculateEmptyCellPenalty()
    {
        if (tilemapManager == null) return 0;

        int emptyCount = 0;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                GridPosition pos = new GridPosition(x, y);
                GridTile tile = tilemapManager.GetTile(pos);
                if (tile == null) continue;

                // Skip river tiles
                if (tile.isRiverTile) continue;

                // Count empty cells (no occupying shape)
                if (tile.occupyingShape == null)
                {
                    emptyCount++;
                }
            }
        }

        // Penalty is negative
        return -emptyCount;
    }

    /// <summary>
    /// Get all shapes of a specific building type by scanning the grid.
    /// </summary>
    private List<ShapeController> GetAllShapesOfType(BuildingType buildingType)
    {
        List<ShapeController> shapes = new List<ShapeController>();

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                GridPosition pos = new GridPosition(x, y);
                GridTile tile = tilemapManager.GetTile(pos);
                if (tile == null) continue;

                ShapeController shape = tile.occupyingShape;
                if (shape != null && shape.buildingType == buildingType)
                {
                    // Avoid duplicates (shape may occupy multiple tiles)
                    if (!shapes.Contains(shape))
                    {
                        shapes.Add(shape);
                    }
                }
            }
        }

        return shapes;
    }
}
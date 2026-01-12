using UnityEngine;
using PocketPlanner.Core;
using PocketPlanner.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Game state
    private int currentTurn;
    private int stars;
    private int wildcardsUsed;
    private DicePool dicePool;
    private bool waterDieUsedThisTurn;
    private int selectedStartingPosition;
    private bool firstTurnCompleted;

    // Public properties for game state access
    public int CurrentTurn => currentTurn;
    public int Stars => stars;
    public int WildcardsUsed => wildcardsUsed;
    public bool WaterDieUsedThisTurn => waterDieUsedThisTurn;
    public int SelectedStartingPosition => selectedStartingPosition;
    public bool FirstTurnCompleted => firstTurnCompleted;
    public DicePool DicePool => dicePool;

    [Header("Manager References")]
    [SerializeField] private TilemapManager boardManager;
    [SerializeField] private ShapeManager shapeManager;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private DiceUIManager diceUIManager;
    //private ZoneManager zoneManager;
    //private UIManager uiManager;

    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Kill this duplicate
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize game state for first turn
        selectedStartingPosition = 1; // Default starting position (player should select)
        firstTurnCompleted = false;
        waterDieUsedThisTurn = false;
        currentTurn = 1;

        // Initialize dice pool
        if (diceManager != null)
        {
            dicePool = diceManager.DicePool;
        }
        else
        {
            Debug.LogError("GameManager: DiceManager not assigned. Creating new DicePool.");
            dicePool = new DicePool();
        }

        // Try to find DiceUIManager if not assigned
        if (diceUIManager == null)
        {
            diceUIManager = FindAnyObjectByType<DiceUIManager>();
        }

        // Roll dice for first turn
        if (diceManager != null)
        {
            diceManager.RollAllDice();
            diceManager.ClearSelection();
        }
        else
        {
            dicePool.RollAll();
        }

        // Update dice UI
        if (diceUIManager != null)
        {
            diceUIManager.OnDiceRolled();
            diceUIManager.HighlightDoubleFaces();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void startNewTurn()
    {
        currentTurn++;
        waterDieUsedThisTurn = false;

        // Roll dice for new turn
        if (diceManager != null)
        {
            diceManager.RollAllDice();
            diceManager.ClearSelection();

            // Update dice UI
            if (diceUIManager != null)
            {
                diceUIManager.OnDiceRolled();
                diceUIManager.HighlightDoubleFaces();
            }
        }
        else
        {
            Debug.LogWarning("GameManager: DiceManager not available, cannot roll dice.");
        }

        // Additional turn start logic here
    }

    /// <summary>
    /// Marks the first turn as completed (after first shape placement).
    /// </summary>
    public void CompleteFirstTurn()
    {
        if (!firstTurnCompleted)
        {
            firstTurnCompleted = true;
            Debug.Log("GameManager: First turn marked as completed");
        }
    }

    /// <summary>
    /// Called when a shape placement is confirmed.
    /// Awards stars for double rolls and starts new turn.
    /// </summary>
    public void OnShapePlacementConfirmed(ShapeController shape)
    {
        // Award stars for double rolls if applicable
        AwardStarsForDoubleRolls(shape);

        // Start new turn (will roll dice and clear selection)
        startNewTurn();
    }

    /// <summary>
    /// Award stars based on double rolls matching selected dice faces.
    /// </summary>
    private void AwardStarsForDoubleRolls(ShapeController shape)
    {
        if (diceManager == null) return;
        if (shape.shapeData == null)
        {
            Debug.LogWarning("Cannot award stars: shape.shapeData is null.");
            return;
        }

        // Get double faces from current roll
        List<int> shapeDoubles = diceManager.GetDoubleFaces(DiceType.Shape);
        List<int> buildingDoubles = diceManager.GetDoubleFaces(DiceType.Building);

        // Get selected dice faces
        Dice selectedShapeDie = diceManager.GetSelectedShapeDie();
        Dice selectedBuildingDie = diceManager.GetSelectedBuildingDie();

        if (selectedShapeDie == null || selectedBuildingDie == null)
        {
            Debug.LogWarning("Cannot award stars: no dice selected.");
            return;
        }

        int shapeFaceIndex = selectedShapeDie.CurrentFace;
        int buildingFaceIndex = selectedBuildingDie.CurrentFace;

        bool shapeMatchesDouble = shapeDoubles.Contains(shapeFaceIndex);
        bool buildingMatchesDouble = buildingDoubles.Contains(buildingFaceIndex);

        if (shapeMatchesDouble && buildingMatchesDouble)
        {
            stars += 2; // Double star
            Debug.Log($"Awarded 2 stars: shape and building match double rolls.");
        }
        else if (shapeMatchesDouble || buildingMatchesDouble)
        {
            stars += 1; // Single star
            Debug.Log($"Awarded 1 star: one matches double roll.");
        }
    }

    /// <summary>
    /// Convert ShapeType to dice face index (0-5).
    /// </summary>
    private int ShapeTypeToFaceIndex(ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.TShape => 0,
            ShapeType.ZShape => 1,
            ShapeType.SquareShape => 2,
            ShapeType.LShape => 3,
            ShapeType.LineShape => 4,
            ShapeType.SingleShape => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Convert BuildingType to dice face index (0-5).
    /// </summary>
    private int BuildingTypeToFaceIndex(BuildingType buildingType)
    {
        return buildingType switch
        {
            BuildingType.Industrial => 0,
            BuildingType.Residential => 1,
            BuildingType.Commercial => 2,
            BuildingType.School => 3,
            BuildingType.Park => 4,
            BuildingType.Water => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Set whether water die is used this turn.
    /// Called by DiceManager when water die selection changes.
    /// </summary>
    public void SetWaterDieUsedThisTurn(bool used)
    {
        waterDieUsedThisTurn = used;
        Debug.Log($"Water die used this turn set to: {used}");
    }

}

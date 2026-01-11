using UnityEngine;
using PocketPlanner.Core;

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
    
}

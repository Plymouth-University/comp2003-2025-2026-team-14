using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Game state
    private int currentTurn;
    private int stars;
    private int wildcardsUsed;
    //private DicePool dicePool;
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

    [Header("Manager References")]
    [SerializeField] private TilemapManager boardManager;
    [SerializeField] private ShapeManager shapeManager;
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void startNewTurn()
    {
        currentTurn++;
        waterDieUsedThisTurn = false;
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

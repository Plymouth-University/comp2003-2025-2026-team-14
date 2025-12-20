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
    
}

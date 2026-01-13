using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PocketPlanner.Core;
using PocketPlanner.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

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
    private bool gameEnded;

    // Wildcard constants
    public const int MAX_WILDCARDS = 3;
    private static readonly int[] WILDCARD_COSTS = { -1, -2, -3 };

    // Public properties for game state access
    public int CurrentTurn => currentTurn;
    public int Stars => stars;
    public int WildcardsUsed => wildcardsUsed;
    public bool WaterDieUsedThisTurn => waterDieUsedThisTurn;
    public int SelectedStartingPosition => selectedStartingPosition;
    public bool FirstTurnCompleted => firstTurnCompleted;
    public bool GameEnded => gameEnded;
    public DicePool DicePool => dicePool;
    public ZoneManager ZoneManager => zoneManager;
    public ScoreManager ScoreManager => scoreManager;

    [Header("Manager References")]
    [SerializeField] private TilemapManager boardManager;
    [SerializeField] private ShapeManager shapeManager;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private DiceUIManager diceUIManager;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private ScoreManager scoreManager;
    //private UIManager uiManager;

    [Header("End Game UI")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI scoreBreakdownText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject); // Kill this duplicate
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameManager: Scene loaded: {scene.name}");
        RefreshManagerReferences();
        FindAndAssignUIReferences();
        HideEndGameScreen();

        // If this is the game scene, roll dice for first turn
        if (scene.name == "SampleScene")
        {
            if (diceManager != null)
            {
                diceManager.RollAllDice();
                diceManager.ClearSelection();
                Debug.Log("GameManager: Dice rolled for new game.");
            }

            // Update dice UI if available
            if (diceUIManager != null)
            {
                diceUIManager.OnDiceRolled();
                diceUIManager.HighlightDoubleFaces();
            }
        }
    }

    void FindAndAssignUIReferences()
    {
        // Find EndGamePanel by name
        GameObject panelObj = GameObject.Find("EndGamePanel");
        if (panelObj != null)
        {
            endGamePanel = panelObj;
            // Find child objects
            Transform scoreText = panelObj.transform.Find("ScoreBreakdownText");
            if (scoreText != null)
                scoreBreakdownText = scoreText.GetComponent<TextMeshProUGUI>();

            Transform restartBtn = panelObj.transform.Find("RestartButton");
            if (restartBtn != null)
                restartButton = restartBtn.GetComponent<Button>();

            Transform mainMenuBtn = panelObj.transform.Find("MainMenuButton");
            if (mainMenuBtn != null)
                mainMenuButton = mainMenuBtn.GetComponent<Button>();

            // Reinitialize button listeners
            InitializeEndGameUI();
        }
        else
        {
            Debug.LogWarning("GameManager: Could not find EndGamePanel in scene.");
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
        wildcardsUsed = 0;
        gameEnded = false;

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

        // Ensure ZoneManager exists
        if (ZoneManager.Instance == null)
        {
            GameObject zoneManagerObj = new GameObject("ZoneManager");
            zoneManager = zoneManagerObj.AddComponent<ZoneManager>();
        }
        else
        {
            zoneManager = ZoneManager.Instance;
        }

        // Ensure ScoreManager exists
        if (ScoreManager.Instance == null)
        {
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
        }
        else
        {
            scoreManager = ScoreManager.Instance;
        }

        // Initialize end game UI
        InitializeEndGameUI();

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

        // Add shape to zone system
        if (zoneManager != null)
        {
            zoneManager.AddShape(shape);
        }
        else
        {
            Debug.LogWarning("ZoneManager not found, zone detection skipped.");
        }

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

        // Get original double faces from current roll (wildcards don't affect star eligibility)
        List<int> shapeDoubles = diceManager.GetOriginalDoubleFaces(DiceType.Shape);
        List<int> buildingDoubles = diceManager.GetOriginalDoubleFaces(DiceType.Building);

        // Get selected dice
        Dice selectedShapeDie = diceManager.GetSelectedShapeDie();
        Dice selectedBuildingDie = diceManager.GetSelectedBuildingDie();

        if (selectedShapeDie == null || selectedBuildingDie == null)
        {
            Debug.LogWarning("Cannot award stars: no dice selected.");
            return;
        }

        // Use original faces for star eligibility (not wildcard overrides)
        int shapeFaceIndex = selectedShapeDie.GetOriginalFace();
        int buildingFaceIndex = selectedBuildingDie.GetOriginalFace();

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

    /// <summary>
    /// Check if a wildcard can be used (max 3 per game).
    /// </summary>
    public bool CanUseWildcard()
    {
        return wildcardsUsed < MAX_WILDCARDS;
    }

    /// <summary>
    /// Get the cost for the next wildcard use.
    /// Returns -1, -2, or -3 based on how many have been used.
    /// </summary>
    public int GetNextWildcardCost()
    {
        if (wildcardsUsed >= MAX_WILDCARDS)
            return 0; // No more wildcards available

        return WILDCARD_COSTS[wildcardsUsed];
    }

    /// <summary>
    /// Use a wildcard. Returns true if successful, false if no wildcards remaining.
    /// </summary>
    public bool UseWildcard()
    {
        if (!CanUseWildcard())
            return false;

        wildcardsUsed++;
        Debug.Log($"Wildcard used. Total used: {wildcardsUsed}, next cost: {GetNextWildcardCost()}");
        return true;
    }

    /// <summary>
    /// Get total wildcard cost penalty for all wildcards used so far.
    /// </summary>
    public int GetWildcardCostTotal()
    {
        int total = 0;
        for (int i = 0; i < wildcardsUsed && i < WILDCARD_COSTS.Length; i++)
        {
            total += WILDCARD_COSTS[i];
        }
        return total;
    }

    /// <summary>
    /// Calculate final score breakdown for the current game state.
    /// </summary>
    public ScoreComponents CalculateFinalScore()
    {
        if (scoreManager == null)
        {
            Debug.LogError("GameManager: ScoreManager not available!");
            return new ScoreComponents();
        }
        return scoreManager.CalculateScore();
    }

    /// <summary>
    /// Check if game should end (no valid placements exist).
    /// Placeholder implementation - always returns false.
    /// </summary>
    public bool CheckGameEndCondition()
    {
        // TODO: Implement actual check for valid placements
        // For now, return false (game continues)
        return false;
    }

    /// <summary>
    /// Trigger game end sequence: calculate score, show UI, stop game.
    /// </summary>
    public void TriggerGameEnd()
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log("Game ended! Calculating final score...");

        // Calculate final score
        ScoreComponents score = CalculateFinalScore();

        // Show end game UI
        ShowEndGameScreen(score);

        // Disable further game interactions
        // (Optional) Disable dice UI, shape movement, etc.
    }

    /// <summary>
    /// Input system callback for ending the game (button press).
    /// This method will be automatically called by Unity's new input system.
    /// </summary>
    public void OnGameEndInput()
    {
        Debug.Log("Game end input received");
        TriggerGameEnd();
    }

    /// <summary>
    /// Initialize end game UI elements (button listeners).
    /// Call this in Start() after UI references are set.
    /// </summary>
    private void InitializeEndGameUI()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }
        else
            Debug.LogWarning("GameManager: Restart button not assigned.");

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
            Debug.LogWarning("GameManager: Main menu button not assigned.");

        // Hide panel initially
        if (endGamePanel != null)
            endGamePanel.SetActive(false);
    }

    /// <summary>
    /// Show end game screen with score breakdown.
    /// </summary>
    private void ShowEndGameScreen(ScoreComponents score)
    {
        if (endGamePanel == null)
        {
            Debug.LogError("GameManager: End game panel not assigned!");
            return;
        }

        // Update score breakdown text
        if (scoreBreakdownText != null)
        {
            scoreBreakdownText.text = FormatScoreBreakdown(score);
        }

        // Show panel
        endGamePanel.SetActive(true);

        // Optional: pause game time
        // Time.timeScale = 0f;
    }

    /// <summary>
    /// Hide end game screen.
    /// </summary>
    private void HideEndGameScreen()
    {
        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        // Resume game time if paused
        // Time.timeScale = 1f;
    }

    /// <summary>
    /// Format score breakdown for display.
    /// </summary>
    private string FormatScoreBreakdown(ScoreComponents score)
    {
        return $"FINAL SCORE: {score.totalScore}\n\n" +
               $"Industrial Zones: {score.industrialZoneScore}\n" +
               $"Residential Zones: {score.residentialZoneScore}\n" +
               $"Commercial Zones: {score.commercialZoneScore}\n" +
               $"Parks: {score.parkScore}\n" +
               $"Schools: {score.schoolScore}\n" +
               $"Stars: {score.starScore}\n" +
               $"Empty Cell Penalty: {score.emptyCellPenalty}\n" +
               $"Wildcard Costs: {score.wildcardCostTotal}\n\n" +
               $"Total: {score.totalScore}";
    }

    /// <summary>
    /// Restart the current game (reload scene).
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        // Hide end game panel before scene reload
        HideEndGameScreen();

        // Reset game state before loading new scene
        ResetGameState();

        // Reload current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Return to main menu (placeholder).
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu (not implemented)");
        // TODO: Load main menu scene when available
        // For now, just restart
        RestartGame();
    }

    void ResetGameState()
    {
        // Reset all game state variables to initial values
        selectedStartingPosition = 1; // Default starting position (player should select)
        firstTurnCompleted = false;
        waterDieUsedThisTurn = false;
        currentTurn = 1;
        wildcardsUsed = 0;
        gameEnded = false;
        stars = 0;

        Debug.Log("GameManager: Game state reset for new game.");
    }

    void RefreshManagerReferences()
    {
        // Find manager references in the newly loaded scene
        boardManager = FindAnyObjectByType<TilemapManager>();
        shapeManager = FindAnyObjectByType<ShapeManager>();
        diceManager = FindAnyObjectByType<DiceManager>();
        diceUIManager = FindAnyObjectByType<DiceUIManager>();

        // Use singleton instances for ZoneManager and ScoreManager
        if (ZoneManager.Instance != null)
            zoneManager = ZoneManager.Instance;
        if (ScoreManager.Instance != null)
            scoreManager = ScoreManager.Instance;

        // Update dice pool reference
        if (diceManager != null)
            dicePool = diceManager.DicePool;
        else
            Debug.LogWarning("GameManager: DiceManager not found after scene load.");

        Debug.Log("GameManager: Manager references refreshed after scene load.");
    }

}

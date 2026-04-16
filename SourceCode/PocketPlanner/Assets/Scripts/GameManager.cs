using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PocketPlanner.Core;
using PocketPlanner.UI;
using PocketPlanner.Multiplayer;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Game state
    private int currentTurn;
    private ScoreComponents currentScore; // Only updated when needed to display score (like end of game), calculated from ScoreManager
    private int stars;
    private int wildcardsUsed;
    private DicePool dicePool;
    private bool waterDieUsedThisTurn;
    private int selectedStartingPosition;
    private int previouslySelectedStartingPosition = 0;
    private bool firstTurnCompleted;
    private bool gameEnded;

    // Wildcard constants
    public const int MAX_WILDCARDS = 3;
    private static readonly int[] WILDCARD_COSTS = { -1, -2, -3 };

    // Public properties for game state access
    public int CurrentTurn => currentTurn;
    public ScoreComponents CurrentScore => currentScore;
    public int Stars => stars;
    public int WildcardsUsed => wildcardsUsed;
    public bool WaterDieUsedThisTurn => waterDieUsedThisTurn;
    public int SelectedStartingPosition => selectedStartingPosition;
    public bool FirstTurnCompleted => firstTurnCompleted;
    public bool GameEnded => gameEnded;
    public DicePool DicePool => dicePool;
    public ZoneManager ZoneManager => zoneManager;
    public ScoreManager ScoreManager => scoreManager;
    public SyncManager SyncManager => syncManager != null ? syncManager : PocketPlanner.Multiplayer.SyncManager.Instance;

    [Header("Manager References")]
    [SerializeField] private TilemapManager boardManager;
    [SerializeField] private ShapeManager shapeManager;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private DiceUIManager diceUIManager;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private SyncManager syncManager;
    [SerializeField] private WildcardPromptManager wildcardPromptManager;
    //private UIManager uiManager;

    // Auto-end detection
    private AutoEndDetector autoEndDetector;
    private bool isCheckingGameEnd = false;

    [Header("End Game UI")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI scoreBreakdownText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    // Inputs
    private InputAction touchPositionAction; 
    private InputAction mousePositionAction;


    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Ensure PlayerInput component exists for input system callbacks
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = gameObject.AddComponent<PlayerInput>();
                Debug.LogWarning("GameManager: Added missing PlayerInput component. Please assign the PlayerInputs asset in the inspector.");
            }
            else
            {
                Debug.Log("GameManager: PlayerInput component found.");
            }
            mousePositionAction = playerInput.actions["MousePosition"];
            touchPositionAction = playerInput.actions["TouchPosition"];
        }
        else
        {
            Destroy(gameObject); // Kill this duplicate
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Unsubscribe from multiplayer events
        if (SyncManager != null)
        {
            SyncManager.OnPlacementActionReceived -= OnOpponentPlacementAction;
            SyncManager.OnDiceRollReceived -= OnOpponentDiceRollReceived;
            SyncManager.OnPlayerGameStateReceived -= OnOpponentGameStateReceived;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameManager: Scene loaded: {scene.name}");
        RefreshManagerReferences();
        FindAndAssignUIReferences();
        HideEndGameScreen();

        // If this is the game scene, roll dice for first turn
        if (scene.name == "MainGameScene")
        {
            initializeFirstTurnGameState(); // Reset state
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
        initializeFirstTurnGameState();

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
        InitializeAutoEndDetector();

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

        // Subscribe to multiplayer events
        if (SyncManager != null)
        {
            SyncManager.OnPlacementActionReceived += OnOpponentPlacementAction;
            SyncManager.OnDiceRollReceived += OnOpponentDiceRollReceived;
            SyncManager.OnPlayerGameStateReceived += OnOpponentGameStateReceived;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void initializeFirstTurnGameState()
    {
        // Initialize game state for first turn
        selectedStartingPosition = 1; // Default starting position (player should select)
        firstTurnCompleted = false;
        waterDieUsedThisTurn = false;
        currentTurn = 1;
        wildcardsUsed = 0;
        gameEnded = false;
    }


    public void startNewTurn()
    {
        currentTurn++;
        waterDieUsedThisTurn = false;

        // Roll dice for new turn
        if (diceManager != null)
        {
            // Check if we're in multiplayer mode with a valid shared random seed
            bool isMultiplayer = MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode;
            int sharedRandomSeed = isMultiplayer ? MultiplayerManager.Instance.SharedRandomSeed : -1;

            if (isMultiplayer && sharedRandomSeed != -1)
            {
                // Multiplayer deterministic dice rolling
                diceManager.RollDeterministicDice(sharedRandomSeed, currentTurn);
            }
            else
            {
                // Single player or multiplayer without seed yet
                diceManager.RollAllDice();
            }

            diceManager.ClearSelection();

            // Update dice UI
            if (diceUIManager != null)
            {
                diceUIManager.OnDiceRolled();
                diceUIManager.HighlightDoubleFaces();
                diceUIManager.updateTurnText(currentTurn);
            }

            // Auto-end check after dice roll
            CheckForGameEndAfterRoll();

            // Broadcast dice roll to multiplayer if active
            if (SyncManager != null && MultiplayerManager.Instance != null && !string.IsNullOrEmpty(MultiplayerManager.Instance.LobbyCode))
            {
                var shapeDice = diceManager.GetShapeDice();
                var buildingDice = diceManager.GetBuildingDice();
                int[] shapeFaces = shapeDice.Select(d => d.CurrentFace).ToArray();
                int[] buildingFaces = buildingDice.Select(d => d.CurrentFace).ToArray();
                int broadcastSeed = MultiplayerManager.Instance != null ? MultiplayerManager.Instance.SharedRandomSeed : 0;
                if (broadcastSeed == -1)
                {
                    Debug.LogWarning("GameManager: Shared random seed not yet set, using 0");
                    broadcastSeed = 0;
                }
                SyncManager.BroadcastDiceRoll(shapeFaces, buildingFaces, broadcastSeed, currentTurn);
            }
        }
        else
        {
            Debug.LogWarning("GameManager: DiceManager not available, cannot roll dice.");
        }

        // Additional turn start logic here
    }

    private void CheckForGameEndAfterRoll()
    {
        if (gameEnded) return;
        if (isCheckingGameEnd) return; // Prevent re-entrancy

        isCheckingGameEnd = true;

        bool hasValidPlacement = autoEndDetector != null && autoEndDetector.CheckAnyValidPlacementExists();

        if (!hasValidPlacement)
        {
            if (CanUseWildcard())
            {
                // Show wildcard prompt
                ShowWildcardPrompt();
            }
            else
            {
                // No wildcards left, end game
                TriggerGameEnd();
            }
        }

        isCheckingGameEnd = false;
    }

    private void ShowWildcardPrompt()
    {
        // Find WildcardPromptManager in scene
        if (wildcardPromptManager == null)
        {
            Debug.LogWarning("GameManager: WildcardPromptManager reference not set, attempting to find in scene...");
            wildcardPromptManager = FindAnyObjectByType<WildcardPromptManager>();
        }
        WildcardPromptManager promptManager = wildcardPromptManager;
        if (promptManager == null)
        {
            Debug.LogError("GameManager: No WildcardPromptManager found in scene!");
            // Fallback: end game immediately
            TriggerGameEnd();
            BroadcastGameEndToMultiplayer();
            return;
        }

        promptManager.ShowPrompt(
            wildcardsUsed,
            GetNextWildcardCost(),
            HandleWildcardChoice
        );
    }

    public void HandleWildcardChoice(bool useWildcard)
    {
        if (useWildcard)
        {
            // Player chose to use wildcard - need to open wildcard selection UI
            // We'll use the existing shape wildcard panel (since shape dice affect placement validity)
            DiceUIManager diceUI = FindAnyObjectByType<DiceUIManager>();
            if (diceUI == null)
            {
                Debug.LogError("GameManager: No DiceUIManager found for wildcard selection");
                TriggerGameEnd(); // Fallback: end game
                BroadcastGameEndToMultiplayer();
                return;
            }

            // Get reference to shape wildcard panel
            WildcardSelectionPanel shapePanel = diceUI.shapeWildcardPanel;
            // Get reference to building wildcard panel (in the case water die is chosen)
            WildcardSelectionPanel buildingPanel = diceUI.buildingWildcardPanel;

            if (shapePanel == null)
            {
                Debug.LogError("GameManager: Shape wildcard panel not found");
                TriggerGameEnd(); // Fallback: end game
                BroadcastGameEndToMultiplayer();
                return;
            }

            if (buildingPanel == null)
            {
                Debug.LogError("GameManager: Building wildcard panel not found");
            }

            // Temporarily subscribe to selection event
            UnityEngine.Events.UnityAction<int> onWildcardSelected = null;
            onWildcardSelected = (faceIndex) =>
            {
                // Unsubscribe to avoid multiple calls
                shapePanel.onSelectionMade.RemoveListener(onWildcardSelected);
                buildingPanel.onSelectionMade.RemoveListener(onWildcardSelected);


                // Wildcard has been applied (handled by DiceUIManager.OnShapeWildcardSelected)
                // Wait a frame for dice update, then re-check for valid placements
                StartCoroutine(RecheckAfterWildcard());
            };

            shapePanel.onSelectionMade.AddListener(onWildcardSelected);
            if (buildingPanel != null)
            {
                buildingPanel.onSelectionMade.AddListener(onWildcardSelected);
            }

            // Show the shape wildcard panel
            // We need to simulate a click on the shape wildcard button to ensure proper setup
            // DiceUIManager.OnShapeWildcardButtonClicked handles permission checks and panel showing
            diceUI.OnShapeWildcardButtonClicked();

            Debug.Log("GameManager: Shape wildcard panel opened for auto-end scenario");
        }
        else
        {
            // Player chose to end game
            TriggerGameEnd();
        }
    }

    private System.Collections.IEnumerator RecheckAfterWildcard()
    {
        // Wait one frame for dice update and UI to refresh
        yield return null;

        // Re-check for valid placements
        CheckForGameEndAfterRoll();
    }

    private void BroadcastGameEndToMultiplayer()
    {
        if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode)
        {
            MultiplayerManager.Instance.OnLocalGameEnded();
            SyncManager.Instance?.BroadcastGameEnd();
        }
    }

    /// <summary>
    /// Marks the first turn as completed (after first shape placement) and deactivates starting tile labels.
    /// </summary>
    public void CompleteFirstTurn()
    {
        if (!firstTurnCompleted)
        {
            firstTurnCompleted = true;
            if (boardManager != null)
            {
                boardManager.DeactivateStartingTileLabels();
                boardManager.UnhighlightAllStartingTiles();
                Debug.Log("GameManager: First turn marked as completed");
            }
        }
    }

    /// <summary>
    /// Called when a shape placement is confirmed.
    /// Awards stars for double rolls and starts new turn.
    /// </summary>
    public void OnShapePlacementConfirmed(ShapeController shape)
    {
        // Award stars for double rolls if applicable
        int starsAwarded = AwardStarsForDoubleRolls(shape);

        // Add star visual to shape if stars were awarded
        if (starsAwarded > 0 && shapeManager != null)
        {
            shapeManager.AddStarVisualToShape(shape, starsAwarded);
        }
        else if (starsAwarded > 0)
        {
            Debug.LogWarning("Stars awarded but shapeManager is null, cannot add star visual.");
        }

        // Add shape to zone system
        if (zoneManager != null)
        {
            zoneManager.AddShape(shape);
        }
        else
        {
            Debug.LogWarning("ZoneManager not found, zone detection skipped.");
        }

        // Broadcast placement action in multiplayer mode
        if (SyncManager != null && MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode)
        {
            _ = SyncManager.BroadcastPlacementAction(
                shape.shapeData.shapeName.ToString(),
                shape.buildingType.ToString(),
                shape.position.x,
                shape.position.y,
                shape.RotationState,
                shape.IsFlipped,
                starsAwarded
            );
        }

        // Start new turn (will roll dice and clear selection)
        startNewTurn();
    }

    /// <summary>
    /// Award stars based on double rolls matching selected dice faces.
    /// Returns the number of stars awarded (0, 1, or 2).
    /// </summary>
    private int AwardStarsForDoubleRolls(ShapeController shape)
    {
        if (diceManager == null) return 0;
        if (shape.shapeData == null)
        {
            Debug.LogWarning("Cannot award stars: shape.shapeData is null.");
            return 0;
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
            return 0;
        }

        // Use original faces for star eligibility (not wildcard overrides)
        int shapeFaceIndex = selectedShapeDie.GetOriginalFace();
        int buildingFaceIndex = selectedBuildingDie.GetOriginalFace();

        bool shapeMatchesDouble = shapeDoubles.Contains(shapeFaceIndex);
        bool buildingMatchesDouble = buildingDoubles.Contains(buildingFaceIndex);

        int starsAwarded = 0;
        if (shapeMatchesDouble && buildingMatchesDouble)
        {
            starsAwarded = 2; // Double star
            stars += starsAwarded;
            Debug.Log($"Awarded 2 stars: shape and building match double rolls.");
        }
        else if (shapeMatchesDouble || buildingMatchesDouble)
        {
            starsAwarded = 1; // Single star
            stars += starsAwarded;
            Debug.Log($"Awarded 1 star: one matches double roll.");
        }

        return starsAwarded;
    }


    /// <summary>
    /// Handle placement actions received from other players in multiplayer mode.
    /// Each opponent has their own board, so shape placement from opponents does not have to be rendered.
    /// </summary>
    private void OnOpponentPlacementAction(PlacementActionData placementData)
    {
        if (placementData == null) return;

        Debug.Log($"GameManager: Opponent placement received - {placementData.shapeType} {placementData.buildingType} at ({placementData.positionX},{placementData.positionY})");
 
        // For now, just log the action
    }

    /// <summary>
    /// Handle dice roll data received from other players in multiplayer mode.
    /// Dice rolls should be synchronized via shared seed, but this can be used for verification.
    /// </summary>
    private void OnOpponentDiceRollReceived(DiceRollData diceRollData)
    {
        if (diceRollData == null) return;

        Debug.Log($"GameManager: Opponent dice roll received for turn {diceRollData.turnNumber} - Shape faces: [{string.Join(",", diceRollData.shapeDiceFaces)}], Building faces: [{string.Join(",", diceRollData.buildingDiceFaces)}]");

        // For now, just log the action
        // TODO: Verify dice faces match our local deterministic generation
    }

    /// <summary>
    /// Handle player game state received from other players in multiplayer mode.
    /// Contains score, stars, wildcards used, and board state.
    /// </summary>
    private void OnOpponentGameStateReceived(PlayerGameData playerGameData)
    {
        if (playerGameData == null) return;

        Debug.Log($"GameManager: Player game state received for {playerGameData.PlayerId} - Score: {playerGameData.Score}, Stars: {playerGameData.Stars}, Wildcards: {playerGameData.WildcardsUsed}");

        // For now, just log the action
        // TODO: Update UI with opponent progress
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
        if (autoEndDetector == null) return false;
        return !autoEndDetector.CheckAnyValidPlacementExists() && !CanUseWildcard();
    }

    /// <summary>
    /// Trigger game end sequence: calculate score, show UI, stop game.
    /// </summary>
    public void TriggerGameEnd()
    {
        if (gameEnded) return;

        gameEnded = true;
        BroadcastGameEndToMultiplayer();
        Debug.Log("Game ended! Calculating final score...");

        // Calculate final score
        ScoreComponents score = CalculateFinalScore();
        currentScore = score;

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

    // Input system callback for mouse click (starting position selection).
    public void OnMouseClickTEST()
    {
        if (firstTurnCompleted) return; // Only allow selection before first turn
        Vector2 screenPos = Mouse.current.position.ReadValue();
        GridPosition gridPos = ScreenToGridPosition(screenPos);
        HandleStartingPositionSelection(gridPos);
    }

    // Input system callback for touch press (starting position selecting)
    public void OnTouchPress()
    {
        Debug.Log("OnTouchPress called");
        if (SceneManager.GetActiveScene().name != "MainGameScene")
        {
            Debug.Log("OnTouchPress: Scene is not MainGameScene");
            return;
        }  // Suppress errors in other scenes for now
        if (firstTurnCompleted)
        {
            Debug.Log("OnTouchPress: First turn completed");
            return;  
        }  // Only allow selection before first turn
        if (shapeManager.activeShape != null && shapeManager.activeShape.isBeingDragged)
        {
            Debug.Log("OnTouchPress: Shape is being dragged");
            return;
        }  // Do not allow selection during shape drag
        Vector2 screenPos = touchPositionAction.ReadValue<Vector2>();
        GridPosition gridPos = ScreenToGridPosition(screenPos);
        HandleStartingPositionSelection(gridPos);
    }

    // Temp
    public void OnPlaceShapeInput()
    {
        shapeManager.OnPlaceShapeInput();
    }

    // Input system callback for mouse position (required for input system).
    public void OnMousePosition()
    {
        // No action needed, but method must exist for input system to call.
    }

    // Input system callback for touch position (required for input system).
    public void OnTouchPosition()
    {
        // No action needed, but method must exist for input system to call.
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
        previouslySelectedStartingPosition = 0;
        firstTurnCompleted = false;
        waterDieUsedThisTurn = false;
        currentTurn = 1;
        wildcardsUsed = 0;
        gameEnded = false;
        stars = 0;

        // Unhighlight any highlighted starting tiles
        if (TilemapManager.Instance != null)
        {
            TilemapManager.Instance.UnhighlightAllStartingTiles();
        }

        Debug.Log("GameManager: Game state reset for new game.");
    }

    /// <summary>
    /// Convert screen position to grid position using TilemapManager.
    /// </summary>
    private GridPosition ScreenToGridPosition(Vector2 screenPos)
    {
        if (Camera.main == null) return new GridPosition(-1, -1);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0;
        if (TilemapManager.Instance != null)
        {
            return TilemapManager.Instance.WorldToLogical(worldPos);
        }
        else
        {
            // Fallback: round to nearest integer
            return new GridPosition(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
        }
    }

    /// <summary>
    /// Handle selection of a starting position tile.
    /// </summary>
    private void HandleStartingPositionSelection(GridPosition gridPos)
    {
        if (firstTurnCompleted)
        {
            Debug.Log("Starting position already selected (first turn completed).");
            return;
        }

        // Check if clicked tile is a starting tile
        if (TilemapManager.Instance == null)
        {
            Debug.LogError("TilemapManager instance not found.");
            return;
        }

        if (!TilemapManager.Instance.IsStartingTile(gridPos))
        {
            Debug.Log($"Clicked tile at {gridPos} is not a starting position.");
            return;
        }

        int startingNumber = TilemapManager.Instance.GetStartingPositionNumber(gridPos);
        if (startingNumber < 1 || startingNumber > 8)
        {
            Debug.LogError($"Invalid starting position number: {startingNumber}");
            return;
        }

        // If same starting position already selected, do nothing
        if (selectedStartingPosition == startingNumber)
        {
            Debug.Log($"Starting position {startingNumber} already selected.");
            return;
        }

        // Unhighlight previously selected starting tile
        if (previouslySelectedStartingPosition != 0 && previouslySelectedStartingPosition != startingNumber)
        {
            TilemapManager.Instance.UnhighlightAllStartingTiles();
        }

        selectedStartingPosition = startingNumber;
        Debug.Log($"Starting position selected: {selectedStartingPosition} at {gridPos}");

        // Highlight the newly selected starting tile
        TilemapManager.Instance.HighlightStartingTile(selectedStartingPosition);
        previouslySelectedStartingPosition = selectedStartingPosition;
    }

    void RefreshManagerReferences()
    {
        // Find manager references in the newly loaded scene
        boardManager = FindAnyObjectByType<TilemapManager>();
        shapeManager = FindAnyObjectByType<ShapeManager>();
        diceManager = FindAnyObjectByType<DiceManager>();
        diceUIManager = FindAnyObjectByType<DiceUIManager>();
        wildcardPromptManager = FindAnyObjectByType<WildcardPromptManager>();

        // Use singleton instances for ZoneManager and ScoreManager
        if (ZoneManager.Instance != null)
            zoneManager = ZoneManager.Instance;
            zoneManager.RefreshTilemapManagerReference(boardManager); // Ensure ZoneManager has updated reference to TilemapManager
        if (ScoreManager.Instance != null)
            scoreManager = ScoreManager.Instance;
            scoreManager.RefreshReferences(zoneManager, boardManager, this); // Ensure ScoreManager has updated references
        // Refresh sync manager reference
        if (SyncManager.Instance != null)
            syncManager = SyncManager.Instance;
            syncManager.RefreshReferences(); // Ensure SyncManager has updated references

        // Update dice pool reference
        if (diceManager != null)
            dicePool = diceManager.DicePool;

        else
            Debug.LogWarning("GameManager: DiceManager not found after scene load.");

        Debug.Log("GameManager: Manager references refreshed after scene load.");
        InitializeAutoEndDetector();
    }

    private void InitializeAutoEndDetector()
    {
        if (TilemapManager.Instance == null || diceManager == null || shapeManager == null)
        {
            Debug.LogWarning("GameManager: Cannot initialize AutoEndDetector - missing manager references");
            return;
        }

        autoEndDetector = new AutoEndDetector(
            TilemapManager.Instance,
            diceManager,
            this,
            shapeManager
        );
        Debug.Log("GameManager: AutoEndDetector initialized");
    }

}

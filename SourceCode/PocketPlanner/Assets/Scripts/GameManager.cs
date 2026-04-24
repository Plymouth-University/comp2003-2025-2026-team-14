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

    // Multiplayer turn tracking
    private HashSet<string> playersCompletedCurrentTurn = new HashSet<string>();
    private bool waitingForOtherPlayers = false;
    private bool _multiplayerEventsSubscribed = false;
    private bool _multiplayerManagerEventsSubscribed = false;
    private bool _firstTurnDiceRolled = false;
    private bool _isLeavingLobby = false;

    // Wildcard constants
    public const int MAX_WILDCARDS = 3;
    private static readonly int[] WILDCARD_COSTS = { -1, -2, -3 };

    // Public properties for game state access
    public int CurrentTurn => currentTurn;
    public bool IsWaitingForOtherPlayers => waitingForOtherPlayers;
    public ScoreComponents CurrentScore => currentScore;
    public int Stars => stars;
    public int WildcardsUsed => wildcardsUsed;
    public bool WaterDieUsedThisTurn => waterDieUsedThisTurn;
    public int SelectedStartingPosition => selectedStartingPosition;
    public bool FirstTurnCompleted => firstTurnCompleted;
    public bool GameEnded => gameEnded;
    public bool HasPlayerCompletedTurn(string playerId) => playersCompletedCurrentTurn != null && playersCompletedCurrentTurn.Contains(playerId);
    // Spectator mode
    public bool IsSpectatingOtherPlayers { get; private set; }
    public string CurrentSpectatedPlayerId { get; private set; }
    public event System.Action<bool> OnSpectatorModeChanged;
    public event System.Action<string> OnSpectatedPlayerChanged;
    public event System.Action<string, bool> OnPlayerTurnCompletionChanged;
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
                Debug.LogError("GameManager: Added missing PlayerInput component. Please assign the PlayerInputs asset in the inspector.");
            }
            else
            {
                Debug.Log("GameManager: PlayerInput component found.");
            }
            if (playerInput.actions != null)
            {
                mousePositionAction = playerInput.actions["MousePosition"];
                touchPositionAction = playerInput.actions["TouchPosition"];
            }
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
            SyncManager.OnTurnCompletionReceived -= OnOpponentTurnCompletionReceived;
            _multiplayerEventsSubscribed = false;
            Debug.Log("GameManager: Unsubscribed from SyncManager events");
        }

        // Unsubscribe from MultiplayerManager events
        if (MultiplayerManager.Instance != null && _multiplayerManagerEventsSubscribed)
        {
            MultiplayerManager.Instance.OnGameStarted -= OnMultiplayerGameStarted;
            _multiplayerManagerEventsSubscribed = false;
            Debug.Log("GameManager: Unsubscribed from MultiplayerManager events");
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

            // Roll dice for first turn (deterministic in multiplayer if seed available)
            // In multiplayer mode, delay rolling until shared seed is available (handled by OnMultiplayerGameStarted)
            bool isMultiplayer = MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode;
            int sharedRandomSeed = isMultiplayer ? MultiplayerManager.Instance.SharedRandomSeed : -1;

            if (isMultiplayer && sharedRandomSeed == -1)
            {
                Debug.Log("GameManager: Multiplayer mode detected but shared seed not yet available. Delaying dice roll until game starts.");
                // Dice will be rolled when OnMultiplayerGameStarted event fires
            }
            else
            {
                // Single player or multiplayer with seed already available
                if (diceManager != null)
                {
                    if (!_firstTurnDiceRolled)
                    {
                        RollDiceForCurrentTurn();
                        Debug.Log("GameManager: Dice rolled for new game.");
                    }
                    else
                    {
                        Debug.Log($"GameManager: First turn dice already rolled (flag is true), skipping roll in OnSceneLoaded()");
                    }
                }
                else
                {
                    // DiceManager not found - use fallback path
                    if (!_firstTurnDiceRolled)
                    {
                        // Check if we're in multiplayer mode with shared seed available
                        if (isMultiplayer && sharedRandomSeed != -1)
                        {
                            // Multiplayer deterministic rolling
                            dicePool.RollDeterministic(sharedRandomSeed, currentTurn);
                            Debug.Log($"GameManager: First turn dice rolled deterministically via dicePool in OnSceneLoaded (no dice manager), seed: {sharedRandomSeed}, turn: {currentTurn}");
                        }
                        else
                        {
                            // Single player or multiplayer without seed yet
                            dicePool.RollAll();
                            Debug.Log($"GameManager: First turn dice rolled via dicePool in OnSceneLoaded (no dice manager)");
                        }

                        _firstTurnDiceRolled = true;
                        Debug.Log($"GameManager: First turn dice rolled flag set to true in OnSceneLoaded");

                        // Clear dice selection
                        dicePool.ClearSelection();

                        // Still need to update UI even without dice manager
                        if (diceUIManager != null)
                        {
                            diceUIManager.OnDiceRolled();
                            diceUIManager.HighlightDoubleFaces();
                            diceUIManager.updateTurnText(currentTurn);
                        }

                        // Auto-end check after dice roll
                        CheckForGameEndAfterRoll();

                        // Broadcast dice roll to multiplayer if active
                        // Note: Skip broadcasting in fallback path since diceManager is null
                        // and we can't easily get dice faces from dicePool without manager
                        if (SyncManager != null && MultiplayerManager.Instance != null && !string.IsNullOrEmpty(MultiplayerManager.Instance.LobbyCode))
                        {
                            Debug.LogWarning("GameManager: Cannot broadcast dice roll in fallback path in OnSceneLoaded (diceManager is null)");
                        }
                    }
                    else
                    {
                        Debug.Log($"GameManager: First turn dice already rolled (flag is true), skipping roll in OnSceneLoaded() fallback path");
                    }
                }
            }

            // Initialize spectator mode for multiplayer now that the game scene is loaded.
            // OnMultiplayerGameStarted may have fired in the lobby scene before
            // SpectatorManager/SpectatorUIManager existed (they are in MainGameScene).
            TryInitializeSpectatorMode();
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
            Debug.LogWarning("GameManager: DiceManager not assigned. Creating new DicePool.");
            dicePool = new DicePool();
        }

        // Try to find DiceUIManager if not assigned
        if (diceUIManager == null)
        {
            diceUIManager = FindAnyObjectByType<DiceUIManager>();
        }

        // Try to find ZoneManager
        if (ZoneManager.Instance != null)
        {
            zoneManager = ZoneManager.Instance;
        }
        else
        {
            Debug.LogWarning("GameManager: ZoneManager instance not found.");
        }

        // Try to find ScoreManager
        if (ScoreManager.Instance != null)
        {
            scoreManager = ScoreManager.Instance;
        }
        else
        {
            Debug.LogWarning("GameManager: ScoreManager instance not found.");
        }

        // Initialize end game UI
        InitializeEndGameUI();
        InitializeAutoEndDetector();

        // Roll dice for first turn (deterministic in multiplayer if seed available)
        // In multiplayer mode, delay rolling until shared seed is available (handled by OnMultiplayerGameStarted)
        bool isMultiplayer = MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode;
        int sharedRandomSeed = isMultiplayer ? MultiplayerManager.Instance.SharedRandomSeed : -1;

        if (isMultiplayer && sharedRandomSeed == -1)
        {
            Debug.Log("GameManager: Multiplayer mode detected but shared seed not yet available. Delaying dice roll until game starts.");
            // Dice will be rolled when OnMultiplayerGameStarted event fires
        }
        else
        {
            // Single player or multiplayer with seed already available
            if (diceManager != null)
            {
                if (!_firstTurnDiceRolled)
                {
                    RollDiceForCurrentTurn();
                }
                else
                {
                    Debug.Log($"GameManager: First turn dice already rolled (flag is true), skipping roll in Start()");
                }
            }
            else
            {
                if (!_firstTurnDiceRolled)
                {
                    // Check if we're in multiplayer mode with shared seed available
                    if (isMultiplayer && sharedRandomSeed != -1)
                    {
                        // Multiplayer deterministic rolling
                        dicePool.RollDeterministic(sharedRandomSeed, currentTurn);
                        Debug.Log($"GameManager: First turn dice rolled deterministically via dicePool (no dice manager), seed: {sharedRandomSeed}, turn: {currentTurn}");
                    }
                    else
                    {
                        // Single player or multiplayer without seed yet
                        dicePool.RollAll();
                        Debug.Log($"GameManager: First turn dice rolled via dicePool (no dice manager)");
                    }

                    _firstTurnDiceRolled = true;
                    Debug.Log($"GameManager: First turn dice rolled flag set to true");

                    // Clear dice selection
                    dicePool.ClearSelection();

                    // Still need to update UI even without dice manager
                    if (diceUIManager != null)
                    {
                        diceUIManager.OnDiceRolled();
                        diceUIManager.HighlightDoubleFaces();
                        diceUIManager.updateTurnText(currentTurn);
                    }

                    // Auto-end check after dice roll
                    CheckForGameEndAfterRoll();

                    // Broadcast dice roll to multiplayer if active
                    // Note: Skip broadcasting in fallback path since diceManager is null
                    // and we can't easily get dice faces from dicePool without manager
                    if (SyncManager != null && MultiplayerManager.Instance != null && !string.IsNullOrEmpty(MultiplayerManager.Instance.LobbyCode))
                    {
                        Debug.LogWarning("GameManager: Cannot broadcast dice roll in fallback path (diceManager is null)");
                    }
                }
                else
                {
                    Debug.Log($"GameManager: First turn dice already rolled (flag is true), skipping roll in Start() fallback path");
                }
            }
        }

        // Subscribe to multiplayer events
        SubscribeToMultiplayerEvents();

        // Initialize SpectatorManager for multiplayer mode now that the game scene is fully loaded.
        // This handles the case where OnMultiplayerGameStarted fired in the lobby scene
        // before SpectatorManager/SpectatorUIManager existed (they're in MainGameScene).
        TryInitializeSpectatorMode();
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
        _firstTurnDiceRolled = false;

        // Reset multiplayer turn tracking
        playersCompletedCurrentTurn.Clear();
        waitingForOtherPlayers = false;
    }

    /// <summary>
    /// Roll dice for the current turn (does not increment turn counter).
    /// Used for initial dice roll and when game starts in multiplayer mode.
    /// </summary>
    private void RollDiceForCurrentTurn()
    {
        // Check if we're in multiplayer mode with a valid shared random seed
        bool isMultiplayer = MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode;
        int sharedRandomSeed = isMultiplayer ? MultiplayerManager.Instance.SharedRandomSeed : -1;

        if (diceManager != null)
        {
            // Use DiceManager if available
            if (isMultiplayer && sharedRandomSeed != -1)
            {
                // Multiplayer deterministic dice rolling for current turn
                diceManager.RollDeterministicDice(sharedRandomSeed, currentTurn);
                Debug.Log($"GameManager: Deterministic dice rolled for turn {currentTurn} (seed: {sharedRandomSeed})");
            }
            else
            {
                // Single player or multiplayer without seed yet
                diceManager.RollAllDice();
                Debug.Log($"GameManager: Regular dice rolled for turn {currentTurn}");
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

            // Broadcast dice roll to multiplayer if active and seed is available
            if (SyncManager != null && MultiplayerManager.Instance != null && !string.IsNullOrEmpty(MultiplayerManager.Instance.LobbyCode) && MultiplayerManager.Instance.SharedRandomSeed != -1)
            {
                var shapeDice = diceManager.GetShapeDice();
                var buildingDice = diceManager.GetBuildingDice();
                int[] shapeFaces = shapeDice.Select(d => d.CurrentFace).ToArray();
                int[] buildingFaces = buildingDice.Select(d => d.CurrentFace).ToArray();
                int broadcastSeed = MultiplayerManager.Instance.SharedRandomSeed;
                _ = SyncManager.BroadcastDiceRoll(shapeFaces, buildingFaces, broadcastSeed, currentTurn);
            }
        }
        else
        {
            // Fallback: use DicePool directly (DiceManager not available)
            Debug.LogWarning("GameManager: DiceManager not available, using DicePool fallback.");

            if (dicePool == null)
            {
                Debug.LogError("GameManager: DicePool is also null, cannot roll dice.");
                return;
            }

            if (isMultiplayer && sharedRandomSeed != -1)
            {
                // Multiplayer deterministic dice rolling for current turn
                dicePool.RollDeterministic(sharedRandomSeed, currentTurn);
                Debug.Log($"GameManager: Deterministic dice rolled via DicePool for turn {currentTurn} (seed: {sharedRandomSeed})");
            }
            else
            {
                // Single player or multiplayer without seed yet
                dicePool.RollAll();
                Debug.Log($"GameManager: Regular dice rolled via DicePool for turn {currentTurn}");
            }

            dicePool.ClearSelection();

            // Update dice UI
            if (diceUIManager != null)
            {
                diceUIManager.OnDiceRolled();
                diceUIManager.HighlightDoubleFaces();
                diceUIManager.updateTurnText(currentTurn);
            }

            // Auto-end check after dice roll
            CheckForGameEndAfterRoll();

            // Broadcast dice roll to multiplayer if active and seed is available
            // Note: We can still broadcast using dicePool faces
            if (SyncManager != null && MultiplayerManager.Instance != null && !string.IsNullOrEmpty(MultiplayerManager.Instance.LobbyCode) && MultiplayerManager.Instance.SharedRandomSeed != -1)
            {
                var shapeDice = dicePool.GetShapeDice();
                var buildingDice = dicePool.GetBuildingDice();
                int[] shapeFaces = shapeDice.Select(d => d.CurrentFace).ToArray();
                int[] buildingFaces = buildingDice.Select(d => d.CurrentFace).ToArray();
                int broadcastSeed = MultiplayerManager.Instance.SharedRandomSeed;
                _ = SyncManager.BroadcastDiceRoll(shapeFaces, buildingFaces, broadcastSeed, currentTurn);
            }
        }

        // Mark first turn dice as rolled
        if (currentTurn == 1)
        {
            _firstTurnDiceRolled = true;
            Debug.Log($"GameManager: First turn dice rolled flag set to true");
        }
    }


    public void startNewTurn()
    {
        // Return to local player's board before starting a new turn
        // so auto-end detection checks the correct (local) board
        if (SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating)
        {
            Debug.Log("GameManager: Returning to local board at start of new turn");
            SpectatorManager.Instance.SwitchToPlayer(SpectatorManager.Instance.LocalPlayerId);
        }

        Debug.Log($"GameManager: Starting new turn. Current turn was {currentTurn}, incrementing to {currentTurn + 1}");
        currentTurn++;
        waterDieUsedThisTurn = false;

        // Roll dice for new turn using helper method
        RollDiceForCurrentTurn();

        // Additional turn start logic here
    }

    // NOT CURRENTLY IN USE
    // Wrapper of CheckForGameEndAfterRoll that can be called by DiceUIManager when wildcard is used
    // IMPORTANT: Potential issue with HandleWildcardChoice's current implementation. Needs modification before use.
    public void CheckForGameEndAfterWildcardUse()
    {
        CheckForGameEndAfterRoll();
    }

    private void CheckForGameEndAfterRoll()
    {
        if (gameEnded) return;
        if (isCheckingGameEnd) return; // Prevent re-entrancy
        if (autoEndDetector == null) return; // Auto-end detector not ready
        if (!firstTurnCompleted) return; // For now don't check for game end before first turn is completed

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

    /// <summary>
    ///  Show wildcard prompt when AutoEndDetector determines no valid placements exist and player has wildcards available.
    /// </summary>
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

    /// <summary>
    ///  Hide the end game wildcard prompt (Used in TriggerGameEnd to ensure prompt is closed if game is ending in multiplayer)
    /// </summary>
    private void HideWildcardPrompt()
    {
        // Find WildcardPromptManager in scene
        if (wildcardPromptManager == null)
        {
            Debug.LogWarning("GameManager: WildcardPromptManager reference not set, attempting to find in scene...");
            wildcardPromptManager = FindAnyObjectByType<WildcardPromptManager>();
        }
        WildcardPromptManager promptManager = wildcardPromptManager;

        if (promptManager != null)
        {
            promptManager.Hide();
        }
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

        // Broadcast turn completion in multiplayer mode
        if (SyncManager != null && MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode)
        {
            string localPlayerId = MultiplayerManager.Instance.LocalPlayerId;
            if (!string.IsNullOrEmpty(localPlayerId))
            {
                // Add local player to completed tracking
                bool localAdded = playersCompletedCurrentTurn.Add(localPlayerId);
                Debug.Log($"GameManager: Added local player {localPlayerId} to completed set: {localAdded} (already in set: {!localAdded})");
                OnPlayerTurnCompletionChanged?.Invoke(localPlayerId, true);
                waitingForOtherPlayers = true;

                // Broadcast turn completion (game not ended)
                _ = SyncManager.BroadcastTurnCompletion(currentTurn, false);
                Debug.Log($"GameManager: Turn completion broadcast for turn {currentTurn}");

                // Check if all players have already completed (e.g., single player in lobby)
                CheckAllPlayersCompletedTurn();
            }
        }

        // Start new turn (will roll dice and clear selection)
        // In multiplayer mode, wait for all players to complete before starting new turn
        if (MultiplayerManager.Instance == null || !MultiplayerManager.Instance.IsMultiplayerMode)
        {
            startNewTurn();
        }
        else
        {
            Debug.Log($"GameManager: Waiting for other players to complete turn {currentTurn}");
        }
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
    /// Handle turn completion received from other players in multiplayer mode.
    /// Tracks when all players have completed the current turn.
    /// </summary>
    private void OnOpponentTurnCompletionReceived(TurnCompletionData turnCompletionData)
    {
        Debug.Log($"GameManager: OnOpponentTurnCompletionReceived method called (turnCompletionData is {(turnCompletionData != null ? "not null" : "null")})");
        if (turnCompletionData == null) return;

        Debug.Log($"GameManager: Turn completion received for player {turnCompletionData.playerId} on turn {turnCompletionData.turnNumber} (gameEnded: {turnCompletionData.gameEnded})");
        Debug.Log($"GameManager: Current turn is {currentTurn}, waitingForOtherPlayers: {waitingForOtherPlayers}, playersCompletedCurrentTurn count: {playersCompletedCurrentTurn.Count}");

        // Only track completions for the current turn
        if (turnCompletionData.turnNumber == currentTurn)
        {
            bool added = playersCompletedCurrentTurn.Add(turnCompletionData.playerId);
            Debug.Log($"GameManager: Added player {turnCompletionData.playerId} to completed set: {added} (already in set: {!added})");
            OnPlayerTurnCompletionChanged?.Invoke(turnCompletionData.playerId, true);
            CheckAllPlayersCompletedTurn();
        }
        else
        {
            Debug.Log($"GameManager: Turn completion for turn {turnCompletionData.turnNumber} ignored (current turn is {currentTurn})");
        }
    }

    /// <summary>
    /// Subscribe to SyncManager multiplayer events if not already subscribed.
    /// </summary>
    private void SubscribeToMultiplayerEvents()
    {
        // Subscribe to SyncManager events
        if (!_multiplayerEventsSubscribed)
        {
            if (SyncManager != null)
            {
                Debug.Log($"GameManager: Subscribing to SyncManager events...");
                Debug.Log($"GameManager: SyncManager instance ID: {SyncManager.GetInstanceID()}, type: {SyncManager.GetType().FullName}");
                SyncManager.OnPlacementActionReceived += OnOpponentPlacementAction;
                SyncManager.OnDiceRollReceived += OnOpponentDiceRollReceived;
                SyncManager.OnPlayerGameStateReceived += OnOpponentGameStateReceived;
                SyncManager.OnTurnCompletionReceived += OnOpponentTurnCompletionReceived;
                _multiplayerEventsSubscribed = true;
                Debug.Log($"GameManager: Successfully subscribed to SyncManager events");
            }
            else
            {
                Debug.LogWarning("GameManager: SyncManager is null, cannot subscribe to multiplayer events");
            }
        }

        // Subscribe to MultiplayerManager events
        if (!_multiplayerManagerEventsSubscribed)
        {
            if (MultiplayerManager.Instance != null)
            {
                Debug.Log($"GameManager: Subscribing to MultiplayerManager events...");
                MultiplayerManager.Instance.OnGameStarted += OnMultiplayerGameStarted;
                _multiplayerManagerEventsSubscribed = true;
                Debug.Log($"GameManager: Successfully subscribed to MultiplayerManager events");
            }
            else
            {
                Debug.LogWarning("GameManager: MultiplayerManager instance is null, cannot subscribe to events");
            }
        }

        // If game already started (seed already available) and we haven't rolled first-turn dice, roll now
        // This handles the case where the seed was received before event subscription
        if (MultiplayerManager.Instance != null &&
            MultiplayerManager.Instance.SharedRandomSeed != -1 &&
            currentTurn == 1 && !_firstTurnDiceRolled)
        {
            Debug.Log("GameManager: Seed already available, initializing game after subscription.");
            OnMultiplayerGameStarted();
        }
    }

    /// <summary>
    /// Called when multiplayer game starts (shared seed is available).
    /// Rolls deterministic dice for first turn if not already rolled.
    /// </summary>
    private void OnMultiplayerGameStarted()
    {
        Debug.Log($"GameManager: Multiplayer game started event received. Current turn: {currentTurn}, seed: {MultiplayerManager.Instance?.SharedRandomSeed ?? -1}");

        // Initialize spectator mode with all player IDs
        if (MultiplayerManager.Instance != null)
        {
            var playerIds = MultiplayerManager.Instance.Players.Keys.ToList();
            if (SpectatorManager.Instance != null && playerIds.Count > 0)
            {
                string localPlayerId = MultiplayerManager.Instance.LocalPlayerId;
                SpectatorManager.Instance.Initialize(localPlayerId, playerIds);
                Debug.Log($"GameManager: SpectatorManager initialized from OnMultiplayerGameStarted with {playerIds.Count} players");

                // Notify SpectatorUIManager that data is ready (show panel)
                SpectatorUIManager spectatorUI = FindObjectOfType<SpectatorUIManager>();
                if (spectatorUI != null)
                {
                    spectatorUI.OnSpectatorDataReady();
                    Debug.Log("GameManager: SpectatorUIManager notified of data ready");
                }
            }
        }

        // Only roll dice for first turn if we're still on turn 1 and haven't rolled dice yet
        if (currentTurn == 1 && !_firstTurnDiceRolled)
        {
            Debug.Log("GameManager: Rolling deterministic dice for first turn (game started event)");
            RollDiceForCurrentTurn();
        }
        else
        {
            if (currentTurn != 1)
            {
                Debug.Log($"GameManager: Game started event received but current turn is {currentTurn}, not rolling dice");
            }
            else if (_firstTurnDiceRolled)
            {
                Debug.Log($"GameManager: Game started event received but first turn dice already rolled (flag is true), not rolling again");
            }
            else
            {
                Debug.Log($"GameManager: Game started event received but not rolling dice for unknown reason");
            }
        }
    }

    /// <summary>
    /// Check if all players have completed the current turn.
    /// Called when a player's turn completion is received.
    /// </summary>
    private void CheckAllPlayersCompletedTurn()
    {
        if (!waitingForOtherPlayers)
        {
            Debug.Log($"GameManager: CheckAllPlayersCompletedTurn called but waitingForOtherPlayers is false (turn {currentTurn})");
            return;
        }

        // Get the multiplayer manager to access player list
        MultiplayerManager multiplayerManager = MultiplayerManager.Instance;
        if (multiplayerManager == null || !multiplayerManager.IsMultiplayerMode)
        {
            Debug.LogWarning("GameManager: Cannot check all players completed - multiplayer manager not available");
            return;
        }

        // Get all player IDs in the lobby
        var allPlayerIds = multiplayerManager.Players.Keys;
        int totalPlayers = allPlayerIds.Count;
        int completedPlayers = playersCompletedCurrentTurn.Count;

        // Debug logging for player IDs
        Debug.Log($"GameManager: Turn completion progress - {completedPlayers}/{totalPlayers} players completed turn {currentTurn}");
        Debug.Log($"GameManager: All player IDs in lobby: {string.Join(", ", allPlayerIds)}");
        Debug.Log($"GameManager: Completed player IDs: {string.Join(", ", playersCompletedCurrentTurn)}");

        // Check which players are missing from the completed set
        var missingPlayers = allPlayerIds.Where(id => !playersCompletedCurrentTurn.Contains(id)).ToList();
        if (missingPlayers.Count > 0)
        {
            Debug.Log($"GameManager: Missing completions from players: {string.Join(", ", missingPlayers)}");
        }

        // Check if all players have completed this turn
        if (playersCompletedCurrentTurn.Count >= totalPlayers)
        {
            Debug.Log($"GameManager: All {totalPlayers} players have completed turn {currentTurn}");

            // Clear the tracking for this turn
            playersCompletedCurrentTurn.Clear();
            waitingForOtherPlayers = false;

            // Notify UI that the local player is no longer 'completed' for the new turn
            string localPlayerId = multiplayerManager.LocalPlayerId;
            if (!string.IsNullOrEmpty(localPlayerId))
            {
                OnPlayerTurnCompletionChanged?.Invoke(localPlayerId, false);
            }

            // Start the next turn now that all players have completed
            startNewTurn();

            // Optional: Trigger UI update or other actions
            // OnAllPlayersCompletedTurn?.Invoke();
        }
        else
        {
            Debug.Log($"GameManager: Not all players completed yet. Still waiting for {totalPlayers - completedPlayers} more players.");
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
        if (autoEndDetector == null) return false;
        return !autoEndDetector.CheckAnyValidPlacementExists() && !CanUseWildcard();
    }

    /// <summary>
    /// Trigger game end sequence: calculate score, show UI, stop game.
    /// </summary>
    public void TriggerGameEnd()
    {
        if (gameEnded) return;

        // Return to local player's board before ending the game so
        // scoring uses the correct (local) board
        if (SpectatorManager.Instance != null && SpectatorManager.Instance.IsSpectating)
        {
            Debug.Log("GameManager: Returning to local board before game end");
            SpectatorManager.Instance.SwitchToPlayer(SpectatorManager.Instance.LocalPlayerId);
        }

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
        HideWildcardPrompt(); // Ensure wildcard prompt is closed if game is ending
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
        if (IsSpectatingOtherPlayers) return; // Disable input while spectating
        if (firstTurnCompleted) return; // Only allow selection before first turn
        Vector2 screenPos = Mouse.current.position.ReadValue();
        GridPosition gridPos = ScreenToGridPosition(screenPos);
        HandleStartingPositionSelection(gridPos);
    }

    // Input system callback for touch press (starting position selecting)
    public void OnTouchPress()
    {
        if (IsSpectatingOtherPlayers) return; // Disable input while spectating
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
        }  // Only allow selection during first turn
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
        if (IsSpectatingOtherPlayers) return; // Disable input while spectating
        if (shapeManager != null)
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
    /// Return to main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu");
        LeaveMultiplayerAndReturnToMainMenu();
    }

    /// <summary>
    /// Leave multiplayer lobby and return to main menu.
    /// If not in multiplayer mode, simply returns to main menu.
    /// </summary>
    public void LeaveMultiplayerAndReturnToMainMenu()
    {
        if (_isLeavingLobby)
        {
            Debug.LogWarning("GameManager: Already leaving lobby, ignoring duplicate call.");
            return;
        }
        StartCoroutine(LeaveMultiplayerAndReturnToMainMenuCoroutine());
    }

    private IEnumerator LeaveMultiplayerAndReturnToMainMenuCoroutine()
    {
        if (_isLeavingLobby)
        {
            Debug.LogWarning("GameManager: Already leaving lobby, ignoring duplicate call.");
            yield break;
        }

        _isLeavingLobby = true;
        try
        {
            // Check if in multiplayer mode
            if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode)
            {
                Debug.Log("GameManager: Leaving multiplayer lobby...");
                bool cleanupComplete = false;

                MultiplayerManager.Instance.DisableMultiplayerMode(() =>
                {
                    cleanupComplete = true;
                });

                // Wait for cleanup to complete (with timeout)
                float timeout = 3.0f; // 3 second timeout
                float elapsed = 0f;
                while (!cleanupComplete && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!cleanupComplete)
                {
                    Debug.LogWarning($"GameManager: Timeout waiting for DisableMultiplayerMode callback after {elapsed} seconds");
                }

                Debug.Log("GameManager: Multiplayer cleanup complete, loading main menu.");
            }
            else
            {
                Debug.Log("GameManager: Not in multiplayer mode, proceeding to main menu.");
            }

            // Load main menu scene
            PPSceneManager.LoadMainMenu();
        }
        finally
        {
            _isLeavingLobby = false;
        }
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
        zoneManager = FindAnyObjectByType<ZoneManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        syncManager = FindAnyObjectByType<SyncManager>();
        wildcardPromptManager = FindAnyObjectByType<WildcardPromptManager>();


        // Subscribe to multiplayer events if SyncManager found
        SubscribeToMultiplayerEvents();

        // Refresh references in other managers
        if (zoneManager != null)
            zoneManager.RefreshTilemapManagerReference(boardManager); // Ensure ZoneManager has updated reference to TilemapManager
        if (scoreManager != null)
            scoreManager.RefreshReferences(zoneManager, boardManager, this); // Ensure ScoreManager has updated references
        if (syncManager != null)
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

    /// <summary>
    /// Set spectator mode on/off and notify listeners.
    /// </summary>
    public void SetSpectatorMode(bool isSpectating)
    {
        if (IsSpectatingOtherPlayers == isSpectating)
            return;

        IsSpectatingOtherPlayers = isSpectating;
        OnSpectatorModeChanged?.Invoke(isSpectating);

        // Disable/enable input based on spectator mode
        // Input disabling is handled by UI managers listening to OnSpectatorModeChanged
        Debug.Log($"GameManager: Spectator mode set to {isSpectating}");
    }

    /// <summary>
    /// Set the currently spectated player ID and notify listeners.
    /// </summary>
    public void SetSpectatedPlayer(string playerId)
    {
        if (CurrentSpectatedPlayerId == playerId)
            return;

        CurrentSpectatedPlayerId = playerId;
        OnSpectatedPlayerChanged?.Invoke(playerId);
        Debug.Log($"GameManager: Spectated player set to {playerId}");
    }

    /// <summary>
    /// Initialize SpectatorManager and notify SpectatorUIManager for multiplayer mode.
    /// This is safe to call even if not in multiplayer mode or if managers aren't ready.
    /// </summary>
    private void TryInitializeSpectatorMode()
    {
        if (SpectatorManager.Instance != null &&
            MultiplayerManager.Instance != null &&
            MultiplayerManager.Instance.IsMultiplayerMode)
        {
            string localPlayerId = MultiplayerManager.Instance.LocalPlayerId;
            var playerIds = MultiplayerManager.Instance.Players.Keys.ToList();

            // Only initialize if not already initialized (avoid duplicate init)
            if (SpectatorManager.Instance.GetOpponentPlayerIds().Count == 0 && playerIds.Count > 0)
            {
                SpectatorManager.Instance.Initialize(localPlayerId, playerIds);
                Debug.Log($"GameManager: SpectatorManager initialized with {playerIds.Count} players");
            }

            SpectatorUIManager spectatorUI = FindAnyObjectByType<SpectatorUIManager>();
            if (spectatorUI != null)
            {
                spectatorUI.OnSpectatorDataReady();
                Debug.Log("GameManager: SpectatorUIManager notified of data ready");
            }
        }
    }

}

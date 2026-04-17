using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PocketPlanner.Multiplayer;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Multiplayer Managers")]
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private MultiplayerManager multiplayerManager;
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private SyncManager syncManager;

    [Header("Game Settings UI")]
    [SerializeField] private TextMeshProUGUI roomCodeText; // Text component to display the room code in the lobby
    [SerializeField] private TextMeshProUGUI maxPlayersText; // Text component to display the maximum number of players in the lobby
    [SerializeField] private TextMeshProUGUI turnTimeText; // Text component to display the turn time limit in the lobby
    [SerializeField] private TextMeshProUGUI hostNameText; // Text component to display the host's device id (temporarily) in the lobby

    [SerializeField] private Button startGameButton; // Button to start the game, only interactable by the host

    [Header("Player List UI")]
    [SerializeField] private List<GameObject> playerListPanels; // List of 8 UI panels (max 8 players) for displaying player information in the lobby;
    [SerializeField] private List<TextMeshProUGUI> playerNameTexts; // List of 8 Text components (max 8 players) for displaying player names in the lobby;
    [SerializeField] private List<Button> playerKickButtons; // List of 7 Buttons (player 2 - 8) for kicking players from the lobby (only visible to host);

    private const int MAX_PLAYERS = 8;
    private bool isHost = false;
    private string localPlayerId = string.Empty;

    // Polling for player list changes (fallback if events don't fire)
    private Dictionary<string, PlayerData> lastKnownPlayers = new Dictionary<string, PlayerData>();
    private string lastLobbyCode = string.Empty;
    private bool lastIsHost = false;
    private Coroutine pollingCoroutine;

    private void OnEnable()
    {
        SubscribeToEvents();
        // Start polling for player list changes as fallback
        pollingCoroutine = StartCoroutine(PollPlayerListChanges());
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        // Stop polling coroutine
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
    }

    void Start()
    {
        FindManagersIfMissing();
        ValidateUIReferences();
        InitializeUI();
        UpdateLastKnownPlayers();
        UpdateGameSettingsUI();
        UpdatePlayerListUI();
        UpdateButtonInteractability();
    }

    private void FindManagersIfMissing()
    {
        if (firebaseManager == null) firebaseManager = FirebaseManager.Instance;
        if (multiplayerManager == null) multiplayerManager = MultiplayerManager.Instance;

        // LobbyManager doesn't have a singleton instance; it's attached to the same GameObject as MultiplayerManager
        if (lobbyManager == null && multiplayerManager != null)
        {
            lobbyManager = multiplayerManager.GetComponent<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogWarning("LobbyUIManager: LobbyManager not found on MultiplayerManager GameObject.");
            }
        }

        if (syncManager == null) syncManager = SyncManager.Instance;
    }

    private void ValidateUIReferences()
    {
        if (playerListPanels == null || playerListPanels.Count != MAX_PLAYERS)
        {
            Debug.LogError($"LobbyUIManager: playerListPanels must have exactly {MAX_PLAYERS} elements.");
        }
        if (playerNameTexts == null || playerNameTexts.Count != MAX_PLAYERS)
        {
            Debug.LogError($"LobbyUIManager: playerNameTexts must have exactly {MAX_PLAYERS} elements.");
        }
        if (playerKickButtons == null || playerKickButtons.Count != MAX_PLAYERS - 1)
        {
            Debug.LogError($"LobbyUIManager: playerKickButtons must have exactly {MAX_PLAYERS - 1} elements (player 2-8).");
        }
    }

    private void UpdateLastKnownPlayers()
    {
        if (multiplayerManager != null)
        {
            lastKnownPlayers = new Dictionary<string, PlayerData>(multiplayerManager.Players);
            lastLobbyCode = multiplayerManager.LobbyCode;
            lastIsHost = multiplayerManager.IsLobbyHost;
        }
    }

    private void InitializeUI()
    {
        // Activate panels up to MAX_PLAYERS, disable the rest
        for (int i = 0; i < playerListPanels.Count; i++)
        {
            playerListPanels[i].SetActive(i < MAX_PLAYERS);
        }

        // Initially hide all kick buttons
        foreach (var button in playerKickButtons)
        {
            button.gameObject.SetActive(false);
        }

        startGameButton.interactable = false;
    }

    private void SubscribeToEvents()
    {
        if (multiplayerManager != null)
        {
            multiplayerManager.OnPlayerJoined += OnPlayerJoined;
            multiplayerManager.OnPlayerLeft += OnPlayerLeft;
            multiplayerManager.OnPlayerReadyChanged += OnPlayerReadyChanged;
            multiplayerManager.OnAllPlayersReady += OnAllPlayersReady;
            multiplayerManager.OnGameStarted += OnGameStarted;
            multiplayerManager.OnLobbyJoined += OnLobbyJoined;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (multiplayerManager != null)
        {
            multiplayerManager.OnPlayerJoined -= OnPlayerJoined;
            multiplayerManager.OnPlayerLeft -= OnPlayerLeft;
            multiplayerManager.OnPlayerReadyChanged -= OnPlayerReadyChanged;
            multiplayerManager.OnAllPlayersReady -= OnAllPlayersReady;
            multiplayerManager.OnGameStarted -= OnGameStarted;
            multiplayerManager.OnLobbyJoined -= OnLobbyJoined;
        }
    }

    private void UpdateGameSettingsUI()
    {
        if (multiplayerManager == null) return;

        // Room code
        string lobbyCode = multiplayerManager.LobbyCode;
        roomCodeText.text = $"Room Code: {lobbyCode}";

        // Max players: use host setting if available, otherwise default
        int maxPlayers = NetworkConstants.MAX_PLAYERS_PER_LOBBY;
        if (multiplayerManager.IsLobbyHost)
        {
            maxPlayers = multiplayerManager.HostMaxPlayers;
        }
        maxPlayersText.text = $"Max Players: {maxPlayers}";

        // Turn time limit: use host setting if available
        string turnTimeDisplay = "Unlimited";
        if (multiplayerManager.IsLobbyHost)
        {
            int turnTimeLimit = multiplayerManager.HostTurnTimeLimit;
            turnTimeDisplay = turnTimeLimit == -1 ? "Unlimited" : $"{turnTimeLimit}s";
        }
        turnTimeText.text = $"Turn Time Limit: {turnTimeDisplay}";

        // Host name: find host player
        string hostName = "Unknown";
        var players = multiplayerManager.Players;
        foreach (var player in players.Values)
        {
            if (player.IsHost)
            {
                hostName = player.DisplayName;
                break;
            }
        }
        hostNameText.text = $"Host: {hostName}";
    }

    private void UpdatePlayerListUI()
    {
        if (multiplayerManager == null) return;

        var players = multiplayerManager.Players;
        int playerIndex = 0;

        // Clear all player name texts
        foreach (var text in playerNameTexts)
        {
            text.text = "";
        }

        // Hide all kick buttons initially
        foreach (var button in playerKickButtons)
        {
            button.gameObject.SetActive(false);
        }

        // Create sorted list of players by join order (host first, then others by JoinedAt)
        List<PlayerData> sortedPlayers = new List<PlayerData>(players.Values);
        sortedPlayers.Sort((a, b) => a.JoinedAt.CompareTo(b.JoinedAt));

        // Assign each player to a panel in order (panel 0 = host, panel 1 = second player, etc.)
        foreach (var player in sortedPlayers)
        {
            if (playerIndex >= MAX_PLAYERS) break;

            // Update player name text
            playerNameTexts[playerIndex].text = $"{player.DisplayName} {(player.IsReady ? "✓" : "")}";

            // If host and player is not self, show kick button (player 2-8)
            if (isHost && !player.IsHost && playerIndex > 0)
            {
                int kickButtonIndex = playerIndex - 1; // kick buttons list is 0-6 for player 2-8
                if (kickButtonIndex < playerKickButtons.Count)
                {
                    playerKickButtons[kickButtonIndex].gameObject.SetActive(true);
                    // Store playerId in button's data for kick action
                    playerKickButtons[kickButtonIndex].onClick.RemoveAllListeners();
                    string playerId = player.PlayerId;
                    playerKickButtons[kickButtonIndex].onClick.AddListener(() => OnKickPlayerClicked(playerId));
                }
            }

            playerIndex++;
        }

        // Disable unused panels
        for (int i = playerIndex; i < MAX_PLAYERS; i++)
        {
            playerNameTexts[i].text = "Waiting for player...";
        }
    }

    private void UpdateButtonInteractability()
    {
        if (multiplayerManager == null) return;

        isHost = multiplayerManager.IsLobbyHost;
        var players = multiplayerManager.Players;
        int playerCount = players.Count;

        // Start game button: only host can click, and only when all players are ready
        bool allPlayersReady = false;
        if (playerCount > 0)
        {
            allPlayersReady = true;
            foreach (var player in players.Values)
            {
                if (!player.IsReady)
                {
                    allPlayersReady = false;
                    break;
                }
            }
        }
        startGameButton.interactable = isHost && playerCount > 0 && allPlayersReady;

        // Update kick buttons interactability based on host status
        foreach (var button in playerKickButtons)
        {
            button.interactable = isHost;
        }
    }

    // Event handlers
    private void OnPlayerJoined(string playerId)
    {
        Debug.Log($"LobbyUIManager: Player joined {playerId}");
        UpdatePlayerListUI();
        UpdateButtonInteractability();
    }

    private void OnPlayerLeft(string playerId)
    {
        Debug.Log($"LobbyUIManager: Player left {playerId}");
        UpdatePlayerListUI();
        UpdateButtonInteractability();
    }

    private void OnPlayerReadyChanged(string playerId, bool isReady)
    {
        Debug.Log($"LobbyUIManager: Player {playerId} ready state changed to {isReady}");
        UpdatePlayerListUI();
        UpdateButtonInteractability();
    }

    private void OnAllPlayersReady()
    {
        Debug.Log("LobbyUIManager: All players ready!");
        UpdateButtonInteractability(); // Update start button interactability
    }

    private void OnGameStarted()
    {
        Debug.Log("LobbyUIManager: Game started!");
        // Optionally disable UI or show loading
    }

    private void OnLobbyJoined(string lobbyCode)
    {
        Debug.Log($"LobbyUIManager: Joined lobby {lobbyCode}");
        UpdateGameSettingsUI();
        UpdatePlayerListUI();
        UpdateButtonInteractability();
    }

    // Button click handlers
    public void OnStartGameClicked()
    {
        if (multiplayerManager != null && isHost)
        {
            multiplayerManager.StartGame();
        }
    }

    private void OnKickPlayerClicked(string playerId)
    {
        if (lobbyManager != null && isHost)
        {
            lobbyManager.KickPlayer(playerId);
        }
    }

    public void OnLeaveLobbyClicked()
    {
        // This would be attached to a Leave Lobby button in the UI
        if (multiplayerManager != null)
        {
            multiplayerManager.DisableMultiplayerMode();
            // Scene transition back to main menu should be handled elsewhere
        }
    }

    private IEnumerator PollPlayerListChanges()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f); // Check every second

            if (multiplayerManager == null) continue;

            bool changed = false;
            var currentPlayers = multiplayerManager.Players;
            string currentLobbyCode = multiplayerManager.LobbyCode;
            bool currentIsHost = multiplayerManager.IsLobbyHost;

            // Check lobby code change
            if (lastLobbyCode != currentLobbyCode)
            {
                lastLobbyCode = currentLobbyCode;
                changed = true;
            }

            // Check host status change
            if (lastIsHost != currentIsHost)
            {
                lastIsHost = currentIsHost;
                changed = true;
            }

            // Check player list changes
            if (lastKnownPlayers.Count != currentPlayers.Count)
            {
                changed = true;
            }
            else
            {
                // Compare each player
                foreach (var kvp in currentPlayers)
                {
                    if (!lastKnownPlayers.ContainsKey(kvp.Key))
                    {
                        changed = true;
                        break;
                    }
                    // Optionally compare ready state or other properties
                }
            }

            if (changed)
            {
                lastKnownPlayers = new Dictionary<string, PlayerData>(currentPlayers);
                UpdateGameSettingsUI();
                UpdatePlayerListUI();
                UpdateButtonInteractability();
            }
        }
    }
}

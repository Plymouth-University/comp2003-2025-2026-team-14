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
    [SerializeField] private Button readyButton; // Button for players to toggle their ready state (host does not have this button - they should be ready by default)
    [SerializeField] private TextMeshProUGUI readyButtonText; // Text component to display "Ready" or "Unready" on the ready button

    [Header("Checkmark Sprites")]
    [SerializeField] private Sprite checkmarkSprite; // Sprite for the checkmark image
    [SerializeField] private Sprite emptyCheckmarkSprite; // Sprite for no checkmark (unready state)

    [Header("Player List UI")]
    [SerializeField] private List<GameObject> playerListPanels; // List of 8 UI panels (max 8 players) for displaying player information in the lobby;
    [SerializeField] private List<TextMeshProUGUI> playerNameTexts; // List of 8 Text components (max 8 players) for displaying player names in the lobby;
    [SerializeField] private List<Button> playerKickButtons; // List of 7 Buttons (player 2 - 8) for kicking players from the lobby (only visible to host);
    [SerializeField] private List<Image> playerReadyCheckmarks; // List of 7 Image components (player 2 - 8) for displaying ready checkmarks in the lobby;

    private const int MAX_PLAYERS = 8;
    private int CurrentMaxPlayers => lobbyManager != null ? lobbyManager.CurrentMaxPlayers : MAX_PLAYERS;
    private bool isHost = false;
    private string localPlayerId = string.Empty;
    private bool eventsSubscribed = false;

    // Polling for player list changes (fallback if events don't fire)
    private Dictionary<string, PlayerData> lastKnownPlayers = new Dictionary<string, PlayerData>();
    private string lastLobbyCode = string.Empty;
    private bool lastIsHost = false;
    private Coroutine pollingCoroutine;

    private void OnEnable()
    {
        FindManagersIfMissing();
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
        SubscribeToEvents(); // Ensure subscriptions in case OnEnable missed them
        ValidateUIReferences();
        InitializeUI();
        UpdateLastKnownPlayers();
        UpdateGameSettingsUI();
        UpdatePlayerListUI();
        UpdateButtonInteractability();
    }

    private void FindManagersIfMissing()
    {
        bool wasNull = multiplayerManager == null;

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

        // If multiplayerManager was null but now is not null, and events aren't subscribed, subscribe
        if (wasNull && multiplayerManager != null && !eventsSubscribed)
        {
            Debug.Log("LobbyUIManager: MultiplayerManager became available, subscribing to events");
            SubscribeToEvents();
        }
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
        if (playerReadyCheckmarks == null || playerReadyCheckmarks.Count != MAX_PLAYERS - 1)
        {
            Debug.LogError($"LobbyUIManager: playerReadyCheckmarks must have exactly {MAX_PLAYERS - 1} elements (player 2-8).");
        }
        if (checkmarkSprite == null)
        {
            Debug.LogError("LobbyUIManager: checkmarkSprite is not assigned.");
        }
        if (emptyCheckmarkSprite == null)
        {
            Debug.LogError("LobbyUIManager: emptyCheckmarkSprite is not assigned.");
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
            playerListPanels[i].SetActive(i < CurrentMaxPlayers);
        }

        // Initially hide all kick buttons
        foreach (var button in playerKickButtons)
        {
            button.gameObject.SetActive(false);
        }

        // Initially hide all ready checkmark images
        foreach (var checkmark in playerReadyCheckmarks)
        {
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(false);
            }
        }

        startGameButton.interactable = false;
    }

    private void SubscribeToEvents()
    {
        Debug.Log($"LobbyUIManager.SubscribeToEvents: multiplayerManager is {(multiplayerManager == null ? "null" : "not null")}, eventsSubscribed={eventsSubscribed}");

        // Unsubscribe first if already subscribed to avoid duplicates
        if (eventsSubscribed && multiplayerManager != null)
        {
            UnsubscribeFromEvents();
        }

        if (multiplayerManager != null)
        {
            // Log current subscriber counts before subscribing
            var beforeCounts = multiplayerManager.GetEventSubscriberCounts();
            Debug.Log($"LobbyUIManager: Before subscribing - " +
                $"OnPlayerJoined: {beforeCounts.GetValueOrDefault("OnPlayerJoined", 0)}, " +
                $"OnPlayerLeft: {beforeCounts.GetValueOrDefault("OnPlayerLeft", 0)}, " +
                $"OnPlayerReadyChanged: {beforeCounts.GetValueOrDefault("OnPlayerReadyChanged", 0)}, " +
                $"OnAllPlayersReady: {beforeCounts.GetValueOrDefault("OnAllPlayersReady", 0)}, " +
                $"OnGameStarted: {beforeCounts.GetValueOrDefault("OnGameStarted", 0)}, " +
                $"OnLobbyJoined: {beforeCounts.GetValueOrDefault("OnLobbyJoined", 0)}");

            multiplayerManager.OnPlayerJoined += OnPlayerJoined;
            multiplayerManager.OnPlayerLeft += OnPlayerLeft;
            multiplayerManager.OnPlayerReadyChanged += OnPlayerReadyChanged;
            multiplayerManager.OnAllPlayersReady += OnAllPlayersReady;
            multiplayerManager.OnGameStarted += OnGameStarted;
            multiplayerManager.OnLobbyJoined += OnLobbyJoined;
            eventsSubscribed = true;

            // Log subscriber counts after subscribing
            var afterCounts = multiplayerManager.GetEventSubscriberCounts();
            Debug.Log($"LobbyUIManager: After subscribing - " +
                $"OnPlayerJoined: {afterCounts.GetValueOrDefault("OnPlayerJoined", 0)}, " +
                $"OnPlayerLeft: {afterCounts.GetValueOrDefault("OnPlayerLeft", 0)}, " +
                $"OnPlayerReadyChanged: {afterCounts.GetValueOrDefault("OnPlayerReadyChanged", 0)}, " +
                $"OnAllPlayersReady: {afterCounts.GetValueOrDefault("OnAllPlayersReady", 0)}, " +
                $"OnGameStarted: {afterCounts.GetValueOrDefault("OnGameStarted", 0)}, " +
                $"OnLobbyJoined: {afterCounts.GetValueOrDefault("OnLobbyJoined", 0)}");

            Debug.Log("LobbyUIManager: Subscribed to MultiplayerManager events");
        }
        else
        {
            eventsSubscribed = false;
            Debug.LogWarning("LobbyUIManager: Cannot subscribe to events - MultiplayerManager not found");
        }
    }

    private void UnsubscribeFromEvents()
    {
        Debug.Log($"LobbyUIManager.UnsubscribeFromEvents: multiplayerManager is {(multiplayerManager == null ? "null" : "not null")}");
        if (multiplayerManager != null)
        {
            // Log subscriber counts before unsubscribing
            var beforeCounts = multiplayerManager.GetEventSubscriberCounts();
            Debug.Log($"LobbyUIManager: Before unsubscribing - " +
                $"OnPlayerJoined: {beforeCounts.GetValueOrDefault("OnPlayerJoined", 0)}, " +
                $"OnPlayerLeft: {beforeCounts.GetValueOrDefault("OnPlayerLeft", 0)}, " +
                $"OnPlayerReadyChanged: {beforeCounts.GetValueOrDefault("OnPlayerReadyChanged", 0)}, " +
                $"OnAllPlayersReady: {beforeCounts.GetValueOrDefault("OnAllPlayersReady", 0)}, " +
                $"OnGameStarted: {beforeCounts.GetValueOrDefault("OnGameStarted", 0)}, " +
                $"OnLobbyJoined: {beforeCounts.GetValueOrDefault("OnLobbyJoined", 0)}");

            multiplayerManager.OnPlayerJoined -= OnPlayerJoined;
            multiplayerManager.OnPlayerLeft -= OnPlayerLeft;
            multiplayerManager.OnPlayerReadyChanged -= OnPlayerReadyChanged;
            multiplayerManager.OnAllPlayersReady -= OnAllPlayersReady;
            multiplayerManager.OnGameStarted -= OnGameStarted;
            multiplayerManager.OnLobbyJoined -= OnLobbyJoined;
            eventsSubscribed = false;

            // Log subscriber counts after unsubscribing
            var afterCounts = multiplayerManager.GetEventSubscriberCounts();
            Debug.Log($"LobbyUIManager: After unsubscribing - " +
                $"OnPlayerJoined: {afterCounts.GetValueOrDefault("OnPlayerJoined", 0)}, " +
                $"OnPlayerLeft: {afterCounts.GetValueOrDefault("OnPlayerLeft", 0)}, " +
                $"OnPlayerReadyChanged: {afterCounts.GetValueOrDefault("OnPlayerReadyChanged", 0)}, " +
                $"OnAllPlayersReady: {afterCounts.GetValueOrDefault("OnAllPlayersReady", 0)}, " +
                $"OnGameStarted: {afterCounts.GetValueOrDefault("OnGameStarted", 0)}, " +
                $"OnLobbyJoined: {afterCounts.GetValueOrDefault("OnLobbyJoined", 0)}");

            Debug.Log("LobbyUIManager: Unsubscribed from MultiplayerManager events");
        }
    }

    private void UpdateGameSettingsUI()
    {
        FindManagersIfMissing();
        if (multiplayerManager == null) return;

        // Room code
        string lobbyCode = multiplayerManager.LobbyCode;
        roomCodeText.text = $"Room Code: {lobbyCode}";

        // Max players: use lobby setting if available, otherwise host setting, otherwise default
        int maxPlayers = MAX_PLAYERS;
        if (lobbyManager != null)
        {
            maxPlayers = lobbyManager.CurrentMaxPlayers;
        }
        else if (multiplayerManager.IsLobbyHost)
        {
            maxPlayers = multiplayerManager.HostMaxPlayers;
        }
        maxPlayersText.text = $"Max Players: {maxPlayers}";

        // Turn time limit: use lobby setting if available, otherwise host setting
        string turnTimeDisplay = "Unlimited";
        if (lobbyManager != null)
        {
            int turnTimeLimit = lobbyManager.CurrentTurnTimeLimit;
            turnTimeDisplay = turnTimeLimit == -1 ? "Unlimited" : $"{turnTimeLimit}s";
        }
        else if (multiplayerManager.IsLobbyHost)
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
                hostName = player.PlayerId; // Using PlayerId as a placeholder for name
                // Clip host name to 5 characters for UI
                if (hostName.Length > 5)
                {
                    hostName = hostName.Substring(0, 5) + "...";
                }
                break;
            }
        }
        hostNameText.text = $"Host: {hostName}";
    }

    private void UpdatePlayerListUI()
    {
        FindManagersIfMissing();
        if (multiplayerManager == null) return;


        isHost = multiplayerManager.IsLobbyHost;
        localPlayerId = multiplayerManager.LocalPlayerId;
        var players = multiplayerManager.Players;
        int playerIndex = 0;

        // Ensure host is automatically ready
        if (isHost && !string.IsNullOrEmpty(localPlayerId) && players.TryGetValue(localPlayerId, out var hostPlayer))
        {
            if (!hostPlayer.IsReady)
            {
                hostPlayer.IsReady = true;
                // Update in Firebase via LobbyManager
                if (lobbyManager != null)
                {
                    lobbyManager.UpdatePlayerReadyState(localPlayerId, true);
                }
            }
        }

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

        // Hide all ready checkmark images initially
        foreach (var checkmark in playerReadyCheckmarks)
        {
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(false);
            }
        }

        // Create sorted list of players by join order (host first, then others by JoinedAt)
        List<PlayerData> sortedPlayers = new List<PlayerData>(players.Values);
        sortedPlayers.Sort((a, b) => a.JoinedAt.CompareTo(b.JoinedAt));

        // Assign each player to a panel in order (panel 0 = host, panel 1 = second player, etc.)
        foreach (var player in sortedPlayers)
        {
            if (playerIndex >= CurrentMaxPlayers) break;

            // Update player name text
            playerNameTexts[playerIndex].text = $"{player.PlayerId}"; // Use PlayerId as placeholder for name

            // Update checkmark image for players 2-8 (playerIndex 1-7)
            if (playerIndex > 0 && playerIndex - 1 < playerReadyCheckmarks.Count)
            {
                var checkmark = playerReadyCheckmarks[playerIndex - 1];
                if (checkmark != null)
                {
                    // Show checkmark image for this player
                    checkmark.gameObject.SetActive(true);
                    // Set sprite based on ready state (with null checks for sprites)
                    if (player.IsReady)
                    {
                        if (checkmarkSprite != null)
                        {
                            checkmark.sprite = checkmarkSprite;
                        }
                        else
                        {
                            Debug.LogWarning("LobbyUIManager: checkmarkSprite is null");
                        }
                    }
                    else
                    {
                        if (emptyCheckmarkSprite != null)
                        {
                            checkmark.sprite = emptyCheckmarkSprite;
                        }
                        else
                        {
                            Debug.LogWarning("LobbyUIManager: emptyCheckmarkSprite is null");
                        }
                    }
                }
            }

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
        for (int i = playerIndex; i < CurrentMaxPlayers; i++)
        {
            playerNameTexts[i].text = "Waiting for player...";
        }
    }

    private void UpdateButtonInteractability()
    {
        FindManagersIfMissing();
        if (multiplayerManager == null) return;

        isHost = multiplayerManager.IsLobbyHost;
        localPlayerId = multiplayerManager.LocalPlayerId;
        var players = multiplayerManager.Players;
        int playerCount = players.Count;

        // Ensure host is automatically ready
        if (isHost && !string.IsNullOrEmpty(localPlayerId) && players.TryGetValue(localPlayerId, out var hostPlayer))
        {
            if (!hostPlayer.IsReady)
            {
                hostPlayer.IsReady = true;
                // Update in Firebase via LobbyManager
                if (lobbyManager != null)
                {
                    lobbyManager.UpdatePlayerReadyState(localPlayerId, true);
                }
            }
        }

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
        startGameButton.interactable = isHost && playerCount > 1 && allPlayersReady;

        // Show/hide start game button and ready button based on host status
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHost);
        }
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(!isHost);
            if (!isHost && !string.IsNullOrEmpty(localPlayerId) && players.TryGetValue(localPlayerId, out var localPlayer))
            {
                // Update ready button text based on local player's ready state
                UpdateReadyButtonText(localPlayer.IsReady);
                readyButton.interactable = true;
            }
            else if (isHost)
            {
                readyButton.interactable = false;
            }
        }

        // Update kick buttons interactability based on host status
        foreach (var button in playerKickButtons)
        {
            button.interactable = isHost;
        }
    }

    private void UpdateReadyButtonText(bool isReady)
    {
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "Unready" : "Ready up";
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

    public void OnReadyUpClicked()
    {
        if (multiplayerManager == null || string.IsNullOrEmpty(localPlayerId)) return;

        // Get current ready state of local player
        if (multiplayerManager.Players.TryGetValue(localPlayerId, out var playerData))
        {
            bool newReadyState = !playerData.IsReady;
            multiplayerManager.SetLocalPlayerReady(newReadyState);

            // Update button text immediately for local feedback
            UpdateReadyButtonText(newReadyState);
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
                // Compare each player - check for new players, removed players, or ready state changes
                foreach (var kvp in currentPlayers)
                {
                    if (!lastKnownPlayers.ContainsKey(kvp.Key))
                    {
                        // New player
                        changed = true;
                        break;
                    }
                    else
                    {
                        // Check if ready state changed
                        var lastPlayer = lastKnownPlayers[kvp.Key];
                        if (lastPlayer.IsReady != kvp.Value.IsReady)
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                // Also check for removed players (player in lastKnownPlayers but not in currentPlayers)
                if (!changed)
                {
                    foreach (var kvp in lastKnownPlayers)
                    {
                        if (!currentPlayers.ContainsKey(kvp.Key))
                        {
                            changed = true;
                            break;
                        }
                    }
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

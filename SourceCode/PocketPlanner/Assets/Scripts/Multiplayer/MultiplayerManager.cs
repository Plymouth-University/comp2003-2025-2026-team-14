using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Main manager for multiplayer gameplay coordination.
    /// Works alongside GameManager to add multiplayer functionality.
    /// </summary>
    public class MultiplayerManager : MonoBehaviour
    {
        // Singleton instance
        public static MultiplayerManager Instance { get; private set; }

        /// <summary>
        /// Ensures a MultiplayerManager instance exists in the scene.
        /// If Instance is null, creates a new GameObject with MultiplayerManager component.
        /// This should be called before using multiplayer features.
        /// </summary>
        public static void EnsureInstanceExists()
        {
            if (Instance != null) return;

            Debug.LogWarning("MultiplayerManager.EnsureInstanceExists: Instance is null. Creating new MultiplayerManager GameObject.");

            // Create new GameObject
            GameObject managerObject = new GameObject("MultiplayerManagers");
            Instance = managerObject.AddComponent<MultiplayerManager>();

            // Note: Awake() will be called immediately after AddComponent, which will set DontDestroyOnLoad
            Debug.Log($"MultiplayerManager.EnsureInstanceExists: Created new instance with ID {Instance.GetInstanceID()}");
        }

        // Multiplayer state
        public bool IsMultiplayerMode { get; private set; }
        public bool IsLobbyHost { get; private set; }
        public string LobbyCode { get; private set; }
        public string LocalPlayerId { get; private set; }
        public string LocalDisplayName { get; private set; }
        public Dictionary<string, PlayerData> Players { get; private set; }

        // Shared random seed for synchronized dice rolls
        public int SharedRandomSeed => _sharedRandomSeed;

        public void SetSharedRandomSeed(int seed)
        {
            _sharedRandomSeed = seed;
            Debug.Log($"MultiplayerManager: Shared random seed set to {seed}");

            // If game hasn't started yet, mark it as started and invoke event for non-host players
            if (!_gameStarted)
            {
                _gameStarted = true;
                _currentSynchronizedTurn = 0;
                Debug.Log($"MultiplayerManager: Game started via shared seed broadcast");
                OnGameStarted?.Invoke();
            }
        }

        // Game synchronization
        private int _sharedRandomSeed = -1;
        private int _currentSynchronizedTurn = 0;
        private bool _allPlayersReady = false;
        private bool _gameStarted = false;
        private bool _gameEnded = false;

        // Host lobby settings (set when creating lobby)
        private int _hostMaxPlayers = 8;
        private int _hostTurnTimeLimit = -1;
        private string _deviceId = string.Empty; // Cached device ID for Firebase operations
        public int HostMaxPlayers => _hostMaxPlayers;
        public int HostTurnTimeLimit => _hostTurnTimeLimit;

        // References
        private FirebaseManager _firebaseManager;
        private GameManager _gameManager;
        private LobbyManager _lobbyManager;
        private SyncManager _syncManager;

        // Events
        public event Action<string> OnLobbyJoined; // lobbyCode
        public event Action<string> OnPlayerJoined; // playerId
        public event Action<string> OnPlayerLeft; // playerId
        public event Action<string, bool> OnPlayerReadyChanged; // playerId, isReady
        public event Action OnAllPlayersReady;
        public event Action OnGameStarted;
        public event Action OnGameEnded;
        public event Action<string> OnError;

        // Debug helper methods to get subscriber counts (can only be accessed from within this class)
        private int GetOnPlayerJoinedSubscriberCount() => OnPlayerJoined?.GetInvocationList()?.Length ?? 0;
        private int GetOnPlayerLeftSubscriberCount() => OnPlayerLeft?.GetInvocationList()?.Length ?? 0;
        private int GetOnPlayerReadyChangedSubscriberCount() => OnPlayerReadyChanged?.GetInvocationList()?.Length ?? 0;
        private int GetOnAllPlayersReadySubscriberCount() => OnAllPlayersReady?.GetInvocationList()?.Length ?? 0;
        private int GetOnGameStartedSubscriberCount() => OnGameStarted?.GetInvocationList()?.Length ?? 0;
        private int GetOnLobbyJoinedSubscriberCount() => OnLobbyJoined?.GetInvocationList()?.Length ?? 0;

        // Public method for external classes to get subscriber counts for debugging
        public Dictionary<string, int> GetEventSubscriberCounts()
        {
            return new Dictionary<string, int>
            {
                { "OnPlayerJoined", GetOnPlayerJoinedSubscriberCount() },
                { "OnPlayerLeft", GetOnPlayerLeftSubscriberCount() },
                { "OnPlayerReadyChanged", GetOnPlayerReadyChangedSubscriberCount() },
                { "OnAllPlayersReady", GetOnAllPlayersReadySubscriberCount() },
                { "OnGameStarted", GetOnGameStartedSubscriberCount() },
                { "OnLobbyJoined", GetOnLobbyJoinedSubscriberCount() }
            };
        }

        private void Awake()
        {
            Debug.Log($"MultiplayerManager.Awake: Starting awake. Instance = {Instance?.GetInstanceID() ?? 0}, this = {GetInstanceID()}");

            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.Log($"MultiplayerManager.Awake: Duplicate instance detected! Destroying this gameObject (Instance ID: {Instance.GetInstanceID()}, this ID: {GetInstanceID()})");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Debug.Log($"MultiplayerManager.Awake: Singleton instance set (Instance ID: {Instance.GetInstanceID()})");

            DontDestroyOnLoad(gameObject);
            Debug.Log($"MultiplayerManager.Awake: DontDestroyOnLoad set for gameObject '{gameObject.name}' (scene: {gameObject.scene.name})");

            // Initialize state
            IsMultiplayerMode = false;
            IsLobbyHost = false;
            LobbyCode = string.Empty;
            LocalPlayerId = string.Empty;
            Players = new Dictionary<string, PlayerData>();

            // Cache device ID on main thread for use in Firebase callbacks
            _deviceId = SystemInfo.deviceUniqueIdentifier;
            Debug.Log($"MultiplayerManager.Awake: Device ID cached: {_deviceId}");
            Debug.Log($"MultiplayerManager.Awake: Awake completed successfully.");
        }

        private void Start()
        {
            // Get references to other managers
            _firebaseManager = FirebaseManager.Instance;
            _gameManager = GameManager.Instance;

            // Check if GameManager exists
            if (_gameManager == null)
            {
                Debug.LogWarning("MultiplayerManager: GameManager not found. Multiplayer features may not work correctly.");
            }

            // Initialize other managers
            InitializeSubManagers();

            // Subscribe to Firebase events
            if (_firebaseManager != null)
            {
                _firebaseManager.OnAuthenticationSuccess += OnFirebaseAuthenticated;
                _firebaseManager.OnError += OnFirebaseError;

                // If Firebase is already authenticated, set LocalPlayerId immediately
                if (_firebaseManager.IsAuthenticated && !string.IsNullOrEmpty(_firebaseManager.UserId))
                {
                    LocalPlayerId = _firebaseManager.UserId;
                    Debug.Log($"MultiplayerManager: Local player ID set on Start: {LocalPlayerId}");
                }
            }
        }

        private void InitializeSubManagers()
        {
            // Create or get LobbyManager
            _lobbyManager = GetComponent<LobbyManager>();
            if (_lobbyManager == null)
            {
                _lobbyManager = gameObject.AddComponent<LobbyManager>();
            }

            // Create or get SyncManager
            _syncManager = GetComponent<SyncManager>();
            if (_syncManager == null)
            {
                _syncManager = gameObject.AddComponent<SyncManager>();
            }
        }

        /// <summary>
        /// Called when Firebase authentication succeeds.
        /// </summary>
        private void OnFirebaseAuthenticated()
        {
            LocalPlayerId = _firebaseManager.UserId;
            Debug.Log($"MultiplayerManager: Local player ID set to: {LocalPlayerId}");
        }

        /// <summary>
        /// Called when Firebase encounters an error.
        /// </summary>
        private void OnFirebaseError(string errorMessage)
        {
            Debug.LogError($"MultiplayerManager: Firebase error - {errorMessage}");
            OnError?.Invoke($"Firebase error: {errorMessage}");
        }

        /// <summary>
        /// Enable multiplayer mode and set up as host or client.
        /// </summary>
        /// <param name="isHost">Whether this player is the lobby host</param>
        /// <param name="lobbyCode">Existing lobby code to join, or empty to create new</param>
        /// <param name="maxPlayers">Maximum number of players allowed (default: 8, host only)</param>
        /// <param name="turnTimeLimit">Turn time limit in seconds, -1 for unlimited (default: -1, host only)</param>
        public void EnableMultiplayerMode(bool isHost, string lobbyCode = "", int maxPlayers = 8, int turnTimeLimit = -1, string displayName = null)
        {
            if (!_firebaseManager.IsReady())
            {
                Debug.LogError("MultiplayerManager: Cannot enable multiplayer - Firebase not ready");
                OnError?.Invoke("Firebase not ready. Please check connection.");
                return;
            }

            // Ensure LocalPlayerId is set (in case event subscription missed the authentication event)
            if (string.IsNullOrEmpty(LocalPlayerId) && !string.IsNullOrEmpty(_firebaseManager.UserId))
            {
                LocalPlayerId = _firebaseManager.UserId;
                Debug.Log($"MultiplayerManager: Local player ID set from Firebase: {LocalPlayerId}");
            }

            if (string.IsNullOrEmpty(LocalPlayerId))
            {
                Debug.LogError("MultiplayerManager: Cannot enable multiplayer - LocalPlayerId not set");
                OnError?.Invoke("Player authentication not complete. Please try again.");
                return;
            }

            // Store display name
            LocalDisplayName = !string.IsNullOrEmpty(displayName) ? displayName : $"Player {LocalPlayerId.Substring(0, Mathf.Min(LocalPlayerId.Length, 5))}";

            IsMultiplayerMode = true;
            IsLobbyHost = isHost;

            if (isHost)
            {
                // Store host lobby settings
                _hostMaxPlayers = maxPlayers;
                _hostTurnTimeLimit = turnTimeLimit;
                // Host creates new lobby
                CreateLobby();
            }
            else
            {
                // Client joins existing lobby
                JoinLobby(lobbyCode);
            }

            Debug.Log($"MultiplayerManager: Multiplayer mode enabled (Host: {isHost}, Lobby: {lobbyCode}, PlayerId: {LocalPlayerId}, DisplayName: {LocalDisplayName})");
        }

        /// <summary>
        /// Disable multiplayer mode and clean up.
        /// </summary>
        /// <param name="onComplete">Optional callback invoked after lobby cleanup completes</param>
        public void DisableMultiplayerMode(Action onComplete = null)
        {
            Debug.Log($"MultiplayerManager.DisableMultiplayerMode: Current state - MultiplayerMode={IsMultiplayerMode}, Host={IsLobbyHost}, Lobby={LobbyCode}, PlayerCount={Players.Count}, LocalPlayerId={LocalPlayerId}");
            IsMultiplayerMode = false;
            IsLobbyHost = false;
            LobbyCode = string.Empty;
            LocalDisplayName = string.Empty;
            Players.Clear();

            // Reset game state fields
            _sharedRandomSeed = -1;
            _currentSynchronizedTurn = 0;
            _allPlayersReady = false;
            _gameStarted = false;
            _gameEnded = false;

            // Reset host settings to defaults
            _hostMaxPlayers = 8;
            _hostTurnTimeLimit = -1;

            // Clean up Firebase listeners
            if (_lobbyManager != null)
            {
                Debug.Log("MultiplayerManager: Calling _lobbyManager.LeaveLobby() with callback");
                _lobbyManager.LeaveLobby(() =>
                {
                    Debug.Log("MultiplayerManager: _lobbyManager.LeaveLobby() callback received");

                    // Stop SyncManager Firebase listeners
                    if (_syncManager != null)
                    {
                        _syncManager.StopListening();
                    }

                    Debug.Log("MultiplayerManager: Multiplayer mode disabled");
                    onComplete?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning("MultiplayerManager: _lobbyManager is null, cannot leave lobby properly");

                // Stop SyncManager Firebase listeners
                if (_syncManager != null)
                {
                    _syncManager.StopListening();
                }

                Debug.Log("MultiplayerManager: Multiplayer mode disabled (without lobby manager)");
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Create a new lobby as host.
        /// </summary>
        private void CreateLobby()
        {
            if (_lobbyManager == null)
            {
                Debug.LogError("MultiplayerManager: LobbyManager not initialized");
                return;
            }

            Debug.Log($"MultiplayerManager: Creating lobby for player {LocalPlayerId} with maxPlayers={_hostMaxPlayers}, turnTimeLimit={_hostTurnTimeLimit}...");
            _lobbyManager.CreateLobby(LocalPlayerId, OnLobbyCreated, OnLobbyError, _hostMaxPlayers, _hostTurnTimeLimit, LocalDisplayName);
        }

        /// <summary>
        /// Join an existing lobby as client.
        /// </summary>
        private void JoinLobby(string lobbyCode)
        {
            if (_lobbyManager == null)
            {
                Debug.LogError("MultiplayerManager: LobbyManager not initialized");
                return;
            }

            Debug.Log($"MultiplayerManager: Joining lobby {lobbyCode} as player {LocalPlayerId}...");
            _lobbyManager.JoinLobby(lobbyCode, LocalPlayerId, OnLobbyJoinedCallback, OnLobbyError, LocalDisplayName);
        }

        /// <summary>
        /// Callback when lobby is successfully created.
        /// </summary>
        private void OnLobbyCreated(string lobbyCode)
        {
            Debug.Log($"MultiplayerManager.OnLobbyCreated called with lobbyCode: {lobbyCode}");
            LobbyCode = lobbyCode;
            Debug.Log($"MultiplayerManager: Lobby created with code: {lobbyCode}");

            // Start listening for game synchronization events
            if (SyncManager.Instance != null)
            {
                SyncManager.Instance.StartListeningForLobby(lobbyCode);
            }
            else
            {
                Debug.LogWarning("MultiplayerManager: SyncManager instance not available for listening");
            }

            // Add host as first player
            string hostDisplayName = !string.IsNullOrEmpty(LocalDisplayName) ? LocalDisplayName : "Host";
            var hostPlayer = new PlayerData(LocalPlayerId, hostDisplayName, true, _deviceId);
            Players[LocalPlayerId] = hostPlayer;

            Debug.Log($"MultiplayerManager: Invoking OnLobbyJoined event (null: {OnLobbyJoined == null}), subscriber count: {GetOnLobbyJoinedSubscriberCount()}");
            OnLobbyJoined?.Invoke(lobbyCode);
            Debug.Log($"MultiplayerManager: OnLobbyJoined event invoked");
        }

        /// <summary>
        /// Callback when lobby is successfully joined.
        /// </summary>
        private void OnLobbyJoinedCallback(string lobbyCode, Dictionary<string, PlayerData> players)
        {
            LobbyCode = lobbyCode;
            Players = players;
            Debug.Log($"MultiplayerManager: Joined lobby {lobbyCode} with {players.Count} players");

            // Start listening for game synchronization events
            if (SyncManager.Instance != null)
            {
                SyncManager.Instance.StartListeningForLobby(lobbyCode);
            }
            else
            {
                Debug.LogWarning("MultiplayerManager: SyncManager instance not available for listening");
            }

            Debug.Log($"MultiplayerManager.OnLobbyJoinedCallback: Invoking OnLobbyJoined event (null: {OnLobbyJoined == null}), subscriber count: {GetOnLobbyJoinedSubscriberCount()}");
            OnLobbyJoined?.Invoke(lobbyCode);
            Debug.Log($"MultiplayerManager.OnLobbyJoinedCallback: OnLobbyJoined event invoked");
        }

        /// <summary>
        /// Callback when lobby operation fails.
        /// </summary>
        private void OnLobbyError(string errorMessage)
        {
            Debug.LogError($"MultiplayerManager: Lobby error - {errorMessage}");
            Debug.LogError($"MultiplayerManager: Current state - MultiplayerMode={IsMultiplayerMode}, Host={IsLobbyHost}, Lobby={LobbyCode}, PlayerId={LocalPlayerId}");
            OnError?.Invoke($"Lobby error: {errorMessage}");

            // Revert multiplayer mode
            IsMultiplayerMode = false;
            IsLobbyHost = false;
            LobbyCode = string.Empty;
        }

        /// <summary>
        /// Start the game when all players are ready (host only).
        /// </summary>
        public void StartGame()
        {
            if (!IsMultiplayerMode || !IsLobbyHost)
            {
                Debug.LogWarning("MultiplayerManager: Only host can start the game");
                return;
            }

            if (!_allPlayersReady)
            {
                Debug.LogWarning("MultiplayerManager: Cannot start game - not all players ready");
                return;
            }

            // Generate shared random seed for synchronized dice rolls
            _sharedRandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            _currentSynchronizedTurn = 0;
            _gameStarted = true;

            // Broadcast game start to all players
            if (_syncManager != null)
            {
                _ = _syncManager.BroadcastSharedSeed(_sharedRandomSeed);
            }
            else
            {
                Debug.LogError("MultiplayerManager: SyncManager not available for broadcasting seed");
            }

            Debug.Log($"MultiplayerManager: Game started with seed {_sharedRandomSeed}");
            OnGameStarted?.Invoke();
        }

        /// <summary>
        /// Get a deterministic random number for synchronized dice rolls.
        /// </summary>
        public int GetSynchronizedRandom(int min, int max, int additionalSeed = 0)
        {
            if (_sharedRandomSeed == -1)
            {
                Debug.LogWarning("MultiplayerManager: No shared seed, using local random");
                return UnityEngine.Random.Range(min, max);
            }

            // Combine shared seed with turn number and additional seed for variety
            var random = new System.Random(_sharedRandomSeed + _currentSynchronizedTurn * 1000 + additionalSeed);
            return random.Next(min, max);
        }

        /// <summary>
        /// Broadcast a player's shape placement to other players.
        /// </summary>
        public void BroadcastPlacement(string shapeType, string buildingType, int positionX, int positionY, int rotation, bool flipped, int starsAwarded)
        {
            if (!IsMultiplayerMode || !_gameStarted)
            {
                return;
            }

            // TODO: Implement Firebase broadcast of placement
            Debug.Log($"MultiplayerManager: Broadcasting placement - {shapeType} {buildingType} at ({positionX},{positionY})");
        }

        /// <summary>
        /// Update local player's ready state.
        /// </summary>
        public void SetLocalPlayerReady(bool isReady)
        {
            if (!IsMultiplayerMode || string.IsNullOrEmpty(LocalPlayerId))
            {
                return;
            }

            if (Players.TryGetValue(LocalPlayerId, out var playerData))
            {
                playerData.IsReady = isReady;

                // Update via LobbyManager
                if (_lobbyManager != null)
                {
                    _lobbyManager.UpdatePlayerReadyState(LocalPlayerId, isReady);
                }

                OnPlayerReadyChanged?.Invoke(LocalPlayerId, isReady);
                CheckAllPlayersReady();
            }
        }

        /// <summary>
        /// Update player data from Firebase events (called by LobbyManager).
        /// </summary>
        public void UpdatePlayerFromFirebase(PlayerData playerData)
        {
            if (playerData == null || string.IsNullOrEmpty(playerData.PlayerId))
                return;

            bool isNewPlayer = !Players.ContainsKey(playerData.PlayerId);
            bool readyStateChanged = false;

            if (Players.TryGetValue(playerData.PlayerId, out var existingPlayer))
            {
                readyStateChanged = existingPlayer.IsReady != playerData.IsReady;
                existingPlayer.UpdateFrom(playerData);
            }
            else
            {
                Players[playerData.PlayerId] = playerData;
            }

            // Trigger events on main thread (already on main thread from LobbyManager.InvokeOnMainThread)
            if (isNewPlayer)
            {
                Debug.Log($"MultiplayerManager: Player joined via Firebase - {playerData.DisplayName} ({playerData.PlayerId})");
                Debug.Log($"MultiplayerManager: OnPlayerJoined subscriber count: {GetOnPlayerJoinedSubscriberCount()}");
                OnPlayerJoined?.Invoke(playerData.PlayerId);
            }
            else if (readyStateChanged)
            {
                Debug.Log($"MultiplayerManager: Player ready state changed via Firebase - {playerData.DisplayName} ({playerData.PlayerId}) Ready={playerData.IsReady}");
                Debug.Log($"MultiplayerManager: OnPlayerReadyChanged subscriber count: {GetOnPlayerReadyChangedSubscriberCount()}");
                OnPlayerReadyChanged?.Invoke(playerData.PlayerId, playerData.IsReady);
            }

            // Always check if all players are ready after any player update
            CheckAllPlayersReady();
        }

        /// <summary>
        /// Remove player from Firebase events (called by LobbyManager).
        /// </summary>
        public void RemovePlayerFromFirebase(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.Log($"MultiplayerManager: RemovePlayerFromFirebase skipped - playerId null/empty");
                return;
            }

            bool isLocalPlayer = playerId == LocalPlayerId;
            bool wasInPlayers = Players.ContainsKey(playerId);

            if (wasInPlayers)
            {
                Players.Remove(playerId);
            }

            Debug.Log($"MultiplayerManager: Player left via Firebase - {playerId}");
            Debug.Log($"MultiplayerManager: wasInPlayers={wasInPlayers}, isLocalPlayer={isLocalPlayer}, LocalPlayerId={LocalPlayerId}");
            Debug.Log($"MultiplayerManager: OnPlayerLeft subscriber count: {GetOnPlayerLeftSubscriberCount()}");

            // Always invoke OnPlayerLeft if this is the local player being removed (kicked)
            // Also invoke if player was in Players dictionary (normal player leave)
            if (isLocalPlayer || wasInPlayers)
            {
                Debug.Log($"MultiplayerManager: INVOKING OnPlayerLeft for player {playerId} (isLocalPlayer={isLocalPlayer}, wasInPlayers={wasInPlayers})");
                try
                {
                    OnPlayerLeft?.Invoke(playerId);
                    Debug.Log($"MultiplayerManager: OnPlayerLeft invocation completed");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MultiplayerManager: Exception during OnPlayerLeft invocation: {ex.Message}");
                    Debug.LogError($"MultiplayerManager: Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Debug.Log($"MultiplayerManager: Not invoking OnPlayerLeft - player not in Players and not local player");
            }

            CheckAllPlayersReady();
        }

        /// <summary>
        /// Check if all players are ready.
        /// </summary>
        private void CheckAllPlayersReady()
        {
            if (Players.Count == 0)
            {
                _allPlayersReady = false;
                return;
            }

            foreach (var player in Players.Values)
            {
                if (!player.IsReady)
                {
                    _allPlayersReady = false;
                    return;
                }
            }

            _allPlayersReady = true;
            OnAllPlayersReady?.Invoke();
            Debug.Log("MultiplayerManager: All players ready!");
        }

        /// <summary>
        /// Called when the local game ends.
        /// </summary>
        public void OnLocalGameEnded()
        {
            if (!IsMultiplayerMode) return;

            _gameEnded = true;

            // Broadcast game end to other players
            if (SyncManager.Instance != null)
            {
                SyncManager.Instance.BroadcastGameEnd();
            }

            Debug.Log("MultiplayerManager: Local game ended and broadcasted");
        }

        public void OnRemoteGameEnded(string playerId)
        {
            // When any player ends game, end game for all players (board game rule)
            if (!_gameEnded)
            {
                _gameEnded = true;
                Debug.Log($"MultiplayerManager: Remote player {playerId} ended game, ending local game.");

                // Trigger local game end
                GameManager.Instance?.TriggerGameEnd();
            }
            else
            {
                Debug.Log($"MultiplayerManager: Remote player {playerId} also ended game (already ended).");
            }
        }

        /// <summary>
        /// Get a list of player names for UI display.
        /// </summary>
        public List<string> GetPlayerNames()
        {
            var names = new List<string>();
            foreach (var player in Players.Values)
            {
                names.Add(player.DisplayName);
            }
            return names;
        }

        /// <summary>
        /// Get the display name for a specific player ID.
        /// Falls back to the player ID if no display name is found.
        /// </summary>
        public string GetPlayerDisplayName(string playerId)
        {
            if (Players.TryGetValue(playerId, out PlayerData playerData))
            {
                return playerData.DisplayName;
            }
            return playerId;
        }

        /// <summary>
        /// Get the number of players in the lobby.
        /// </summary>
        public int GetPlayerCount()
        {
            return Players.Count;
        }

        /// <summary>
        /// Check if multiplayer is active and ready.
        /// </summary>
        public bool IsReady()
        {
            return IsMultiplayerMode && _firebaseManager != null && _firebaseManager.IsReady();
        }

        /// <summary>
        /// Get status string for debugging.
        /// </summary>
        public string GetStatusString()
        {
            return $"Multiplayer Status: Active={IsMultiplayerMode}, Host={IsLobbyHost}, " +
                   $"Lobby={LobbyCode}, Players={Players.Count}, GameStarted={_gameStarted}, " +
                   $"AllReady={_allPlayersReady}";
        }

        private void OnDestroy()
        {
            Debug.Log($"MultiplayerManager.OnDestroy: Destroying instance (Instance ID: {GetInstanceID()}, gameObject: '{gameObject.name}', scene: {gameObject.scene.name})");
            Debug.Log($"MultiplayerManager.OnDestroy: Instance == this? {Instance == this}");

            if (Instance == this)
            {
                Debug.Log($"MultiplayerManager.OnDestroy: Setting Instance to null (was {Instance?.GetInstanceID() ?? 0})");
                Instance = null;
            }
            else
            {
                Debug.Log($"MultiplayerManager.OnDestroy: Instance is different (Instance ID: {Instance?.GetInstanceID() ?? 0}), not clearing static Instance.");
            }

            // Unsubscribe from Firebase events
            if (_firebaseManager != null)
            {
                _firebaseManager.OnAuthenticationSuccess -= OnFirebaseAuthenticated;
                _firebaseManager.OnError -= OnFirebaseError;
                Debug.Log($"MultiplayerManager.OnDestroy: Unsubscribed from Firebase events.");
            }

            Debug.Log($"MultiplayerManager.OnDestroy: Destroy complete.");
        }
    }
}
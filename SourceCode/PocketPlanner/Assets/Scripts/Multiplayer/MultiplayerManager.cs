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

        // Multiplayer state
        public bool IsMultiplayerMode { get; private set; }
        public bool IsLobbyHost { get; private set; }
        public string LobbyCode { get; private set; }
        public string LocalPlayerId { get; private set; }
        public Dictionary<string, PlayerData> Players { get; private set; }

        // Shared random seed for synchronized dice rolls
        public int SharedRandomSeed => _sharedRandomSeed;

        public void SetSharedRandomSeed(int seed)
        {
            _sharedRandomSeed = seed;
            Debug.Log($"MultiplayerManager: Shared random seed set to {seed}");
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

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize state
            IsMultiplayerMode = false;
            IsLobbyHost = false;
            LobbyCode = string.Empty;
            LocalPlayerId = string.Empty;
            Players = new Dictionary<string, PlayerData>();
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
        public void EnableMultiplayerMode(bool isHost, string lobbyCode = "", int maxPlayers = 8, int turnTimeLimit = -1)
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

            Debug.Log($"MultiplayerManager: Multiplayer mode enabled (Host: {isHost}, Lobby: {lobbyCode}, PlayerId: {LocalPlayerId})");
        }

        /// <summary>
        /// Disable multiplayer mode and clean up.
        /// </summary>
        public void DisableMultiplayerMode()
        {
            IsMultiplayerMode = false;
            IsLobbyHost = false;
            LobbyCode = string.Empty;
            Players.Clear();

            // Clean up Firebase listeners
            if (_lobbyManager != null)
            {
                _lobbyManager.LeaveLobby();
            }

            Debug.Log("MultiplayerManager: Multiplayer mode disabled");
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
            _lobbyManager.CreateLobby(LocalPlayerId, OnLobbyCreated, OnLobbyError, _hostMaxPlayers, _hostTurnTimeLimit);
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
            _lobbyManager.JoinLobby(lobbyCode, LocalPlayerId, OnLobbyJoinedCallback, OnLobbyError);
        }

        /// <summary>
        /// Callback when lobby is successfully created.
        /// </summary>
        private void OnLobbyCreated(string lobbyCode)
        {
            Debug.Log($"MultiplayerManager.OnLobbyCreated called with lobbyCode: {lobbyCode}");
            LobbyCode = lobbyCode;
            Debug.Log($"MultiplayerManager: Lobby created with code: {lobbyCode}");

            // Add host as first player
            var hostPlayer = new PlayerData(LocalPlayerId, "Host", true, SystemInfo.deviceUniqueIdentifier);
            Players[LocalPlayerId] = hostPlayer;

            Debug.Log($"MultiplayerManager: Invoking OnLobbyJoined event (null: {OnLobbyJoined == null}), subscriber count: {OnLobbyJoined?.GetInvocationList()?.Length ?? 0}");
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

            OnLobbyJoined?.Invoke(lobbyCode);
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
            if (Instance == this)
            {
                Instance = null;
            }

            // Unsubscribe from Firebase events
            if (_firebaseManager != null)
            {
                _firebaseManager.OnAuthenticationSuccess -= OnFirebaseAuthenticated;
                _firebaseManager.OnError -= OnFirebaseError;
            }
        }
    }
}
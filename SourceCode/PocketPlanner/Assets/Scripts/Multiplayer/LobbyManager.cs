using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Manages lobby creation, joining, and player synchronization via Firebase.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        // Lobby state
        private string _currentLobbyCode = string.Empty;
        private bool _isInLobby = false;
        private Dictionary<string, PlayerData> _players = new Dictionary<string, PlayerData>();
        private int _maxPlayers = 8; // Default from NetworkConstants
        private int _turnTimeLimit = -1; // Default unlimited (-1)

        // Public properties for lobby settings
        public int CurrentMaxPlayers => _maxPlayers;
        public int CurrentTurnTimeLimit => _turnTimeLimit;

        // Main thread dispatch
        private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        // Firebase references
        private FirebaseManager _firebaseManager;

        // Callbacks
        private Action<string> _onLobbyCreatedCallback;
        private Action<string, Dictionary<string, PlayerData>> _onLobbyJoinedCallback;
        private Action<string> _onErrorCallback;

        // Firebase listener references (will be implemented when Firebase SDK is added)
        // private FirebaseDatabaseReference _lobbyRef;
        // private FirebaseDatabaseReference _playersRef;

        private void Start()
        {
            _firebaseManager = FirebaseManager.Instance;
            if (_firebaseManager == null)
            {
                Debug.LogError("LobbyManager: FirebaseManager not found!");
            }
        }

        private void Update()
        {
            // Process main thread actions
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LobbyManager: Exception in main thread action: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Create a new lobby as host.
        /// </summary>
        /// <param name="hostPlayerId">ID of the host player</param>
        /// <param name="onLobbyCreated">Callback when lobby is created</param>
        /// <param name="onError">Callback when error occurs</param>
        /// <param name="maxPlayers">Maximum number of players allowed (default: 8)</param>
        /// <param name="turnTimeLimit">Turn time limit in seconds, -1 for unlimited (default: -1)</param>
        public void CreateLobby(string hostPlayerId, Action<string> onLobbyCreated, Action<string> onError, int maxPlayers = 8, int turnTimeLimit = -1)
        {
            if (!_firebaseManager.IsReady())
            {
                InvokeOnMainThread(onError, "Firebase not ready");
                return;
            }

            if (_isInLobby)
            {
                InvokeOnMainThread(onError, "Already in a lobby");
                return;
            }

            _onLobbyCreatedCallback = onLobbyCreated;
            _onErrorCallback = onError;

            // Generate unique lobby code
            string lobbyCode = GenerateLobbyCode();

            Debug.Log($"LobbyManager: Creating lobby with code: {lobbyCode}");

            // Create lobby data structure
            var lobbyData = new Dictionary<string, object>
            {
                { "lobbyCode", lobbyCode },
                { "hostPlayerId", hostPlayerId },
                { "createdAt", GetCurrentTimestamp() },
                { "gameStarted", false },
                { "maxPlayers", maxPlayers },
                { "turnTimeLimit", turnTimeLimit },
                { "players", new Dictionary<string, object>() }
            };

            // Add host as first player
            var hostPlayer = new PlayerData(hostPlayerId, "Host", true, SystemInfo.deviceUniqueIdentifier);
            var hostPlayerDict = PlayerDataToDictionary(hostPlayer);

            // Add host to players list
            ((Dictionary<string, object>)lobbyData["players"])[hostPlayerId] = hostPlayerDict;

            // Write to Firebase
            _lobbyRef = _firebaseManager.DatabaseReference.Child("lobbies").Child(lobbyCode);
            _playersRef = _lobbyRef.Child("players");

            Debug.Log($"LobbyManager: Starting Firebase SetValueAsync for lobby {lobbyCode}");
            _lobbyRef.SetValueAsync(lobbyData).ContinueWith(task => {
                try
                {
                    Debug.Log($"LobbyManager: Firebase SetValueAsync continuation started. IsCompleted: {task.IsCompleted}, IsFaulted: {task.IsFaulted}, IsCanceled: {task.IsCanceled}");

                    if (task.IsFaulted)
                    {
                        Debug.LogError($"LobbyManager: Firebase write failed: {task.Exception?.Message}");
                        InvokeOnMainThread(onError, $"Failed to create lobby: {task.Exception?.Message}");
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        Debug.LogError("LobbyManager: Firebase write was canceled");
                        InvokeOnMainThread(onError, "Lobby creation was canceled");
                        return;
                    }

                    Debug.Log("LobbyManager: Firebase write successful, setting up listeners");

                    // Set up listeners
                    SetupLobbyListeners(lobbyCode);

                    _currentLobbyCode = lobbyCode;
                    _isInLobby = true;
                    _players[hostPlayerId] = hostPlayer;
                    _maxPlayers = maxPlayers;
                    _turnTimeLimit = turnTimeLimit;

                    Debug.Log($"LobbyManager: Calling InvokeOnMainThread for onLobbyCreated (null: {onLobbyCreated == null})");
                    InvokeOnMainThread(onLobbyCreated, lobbyCode);
                    Debug.Log("LobbyManager: InvokeOnMainThread called");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LobbyManager: Exception in Firebase continuation: {ex.Message}");
                    Debug.LogError($"LobbyManager: Stack trace: {ex.StackTrace}");
                    InvokeOnMainThread(onError, $"Exception creating lobby: {ex.Message}");
                }
            });
        }

        private System.Collections.IEnumerator SimulateLobbyCreation(string lobbyCode, Action<string> callback)
        {
            yield return new WaitForSeconds(0.5f);
            callback?.Invoke(lobbyCode);
        }

        /// <summary>
        /// Join an existing lobby.
        /// </summary>
        public void JoinLobby(string lobbyCode, string playerId, Action<string, Dictionary<string, PlayerData>> onLobbyJoined, Action<string> onError)
        {
            if (!_firebaseManager.IsReady())
            {
                InvokeOnMainThread(onError, "Firebase not ready");
                return;
            }

            if (_isInLobby)
            {
                InvokeOnMainThread(onError, "Already in a lobby");
                return;
            }

            if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6)
            {
                InvokeOnMainThread(onError, "Invalid lobby code");
                return;
            }

            _onLobbyJoinedCallback = onLobbyJoined;
            _onErrorCallback = onError;

            Debug.Log($"LobbyManager: Attempting to join lobby: {lobbyCode}");

            // Check if lobby exists and has space
            _lobbyRef = _firebaseManager.DatabaseReference.Child("lobbies").Child(lobbyCode);
            _playersRef = _lobbyRef.Child("players");

            _lobbyRef.GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    InvokeOnMainThread(onError, $"Failed to join lobby: {task.Exception?.Message}");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    InvokeOnMainThread(onError, "Lobby not found");
                    return;
                }

                // Check player count against maxPlayers setting
                int playerCount = (int)snapshot.Child("players").ChildrenCount;
                int maxPlayers = 8; // default
                if (snapshot.Child("maxPlayers").Exists)
                {
                    maxPlayers = Convert.ToInt32(snapshot.Child("maxPlayers").Value);
                }
                if (playerCount >= maxPlayers)
                {
                    InvokeOnMainThread(onError, $"Lobby is full (max {maxPlayers} players)");
                    return;
                }

                // Read turn time limit
                int turnTimeLimit = -1; // default unlimited
                if (snapshot.Child("turnTimeLimit").Exists)
                {
                    turnTimeLimit = Convert.ToInt32(snapshot.Child("turnTimeLimit").Value);
                }

                // Add player to lobby
                var playerData = new PlayerData(playerId, $"Player{playerCount + 1}", false, SystemInfo.deviceUniqueIdentifier);
                var playerDict = PlayerDataToDictionary(playerData);

                _playersRef.Child(playerId).SetValueAsync(playerDict).ContinueWith(joinTask => {
                    if (joinTask.IsFaulted)
                    {
                        InvokeOnMainThread(onError, $"Failed to join lobby: {joinTask.Exception?.Message}");
                        return;
                    }

                    // Set up listeners and get current player list
                    SetupLobbyListeners(lobbyCode);

                    _currentLobbyCode = lobbyCode;
                    _isInLobby = true;
                    _maxPlayers = maxPlayers;
                    _turnTimeLimit = turnTimeLimit;

                    // Load all players from Firebase (including the one we just added)
                    LoadCurrentPlayers(() => {
                        // Callback after players are loaded
                        InvokeOnMainThread(() => onLobbyJoined?.Invoke(lobbyCode, _players));
                    });
                });
            });
        }

        private System.Collections.IEnumerator SimulateLobbyJoin(string lobbyCode, Dictionary<string, PlayerData> players, Action<string, Dictionary<string, PlayerData>> callback)
        {
            yield return new WaitForSeconds(0.5f);
            callback?.Invoke(lobbyCode, players);
        }

        /// <summary>
        /// Leave the current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            if (!_isInLobby)
            {
                return;
            }

            // Remove player from Firebase
            if (_playersRef != null && FirebaseManager.Instance.UserId != null)
            {
                _playersRef.Child(FirebaseManager.Instance.UserId).RemoveValueAsync();
            }

            // Clean up listeners
            if (_lobbyRef != null)
            {
                _lobbyRef.ValueChanged -= OnLobbyDataChanged;
            }
            if (_playersRef != null)
            {
                _playersRef.ChildAdded -= OnPlayerAdded;
                _playersRef.ChildChanged -= OnPlayerChanged;
                _playersRef.ChildRemoved -= OnPlayerRemoved;
            }

            Debug.Log($"LobbyManager: Leaving lobby {_currentLobbyCode}");

            // Reset state
            _currentLobbyCode = string.Empty;
            _isInLobby = false;
            _players.Clear();
            _maxPlayers = 8; // Reset to default
            _turnTimeLimit = -1; // Reset to default unlimited

            _lobbyRef = null;
            _playersRef = null;
        }

        /// <summary>
        /// Update a player's ready state in the lobby.
        /// </summary>
        public void UpdatePlayerReadyState(string playerId, bool isReady)
        {
            if (!_isInLobby || string.IsNullOrEmpty(_currentLobbyCode))
            {
                return;
            }

            if (_players.TryGetValue(playerId, out var playerData))
            {
                playerData.IsReady = isReady;

                // Update in Firebase
                if (_playersRef != null)
                {
                    _playersRef.Child(playerId).Child("isReady").SetValueAsync(isReady);
                }

                Debug.Log($"LobbyManager: Player {playerId} ready state updated to {isReady}");
            }
        }

        /// <summary>
        /// Kick a player from the lobby (host only).
        /// </summary>
        public void KickPlayer(string playerId)
        {
            if (!_isInLobby || string.IsNullOrEmpty(_currentLobbyCode))
            {
                return;
            }

            // TODO: Check if local player is host
            // TODO: Implement Firebase removal

            Debug.Log($"LobbyManager: Player {playerId} kicked from lobby");
        }

        /// <summary>
        /// Generate a 6-character alphanumeric lobby code.
        /// </summary>
        private string GenerateLobbyCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No ambiguous characters
            System.Random random = new System.Random();
            char[] code = new char[6];

            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }

            return new string(code);
        }

        /// <summary>
        /// Convert PlayerData to a dictionary for Firebase storage.
        /// </summary>
        private Dictionary<string, object> PlayerDataToDictionary(PlayerData playerData)
        {
            return new Dictionary<string, object>
            {
                { "playerId", playerData.PlayerId },
                { "displayName", playerData.DisplayName },
                { "isReady", playerData.IsReady },
                { "isHost", playerData.IsHost },
                { "joinedAt", playerData.JoinedAt },
                { "deviceId", playerData.DeviceId }
            };
        }

        /// <summary>
        /// Convert a dictionary to PlayerData.
        /// </summary>
        private PlayerData DictionaryToPlayerData(Dictionary<string, object> dict)
        {
            try
            {
                return new PlayerData
                {
                    PlayerId = dict.ContainsKey("playerId") ? dict["playerId"].ToString() : "",
                    DisplayName = dict.ContainsKey("displayName") ? dict["displayName"].ToString() : "Player",
                    IsReady = dict.ContainsKey("isReady") && bool.Parse(dict["isReady"].ToString()),
                    IsHost = dict.ContainsKey("isHost") && bool.Parse(dict["isHost"].ToString()),
                    JoinedAt = dict.ContainsKey("joinedAt") ? long.Parse(dict["joinedAt"].ToString()) : GetCurrentTimestamp(),
                    DeviceId = dict.ContainsKey("deviceId") ? dict["deviceId"].ToString() : ""
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"LobbyManager: Failed to convert dictionary to PlayerData: {ex.Message}");
                return new PlayerData();
            }
        }

        /// <summary>
        /// Get current Unix timestamp in seconds.
        /// </summary>
        private long GetCurrentTimestamp()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        /// <summary>
        /// Set up Firebase listeners for lobby changes.
        /// </summary>
        private void SetupLobbyListeners(string lobbyCode)
        {
            if (_lobbyRef == null || _playersRef == null)
            {
                Debug.LogError("LobbyManager: Cannot setup listeners - Firebase references not initialized");
                return;
            }

            Debug.Log($"LobbyManager: Setting up Firebase listeners for lobby {lobbyCode}");

            // Attach lobby data change listener
            _lobbyRef.ValueChanged += OnLobbyDataChanged;

            // Attach player list listeners
            _playersRef.ChildAdded += OnPlayerAdded;
            _playersRef.ChildChanged += OnPlayerChanged;
            _playersRef.ChildRemoved += OnPlayerRemoved;

            // Load current players immediately
            LoadCurrentPlayers();
        }

        /// <summary>
        /// Load current players from Firebase.
        /// </summary>
        /// <param name="onComplete">Optional callback invoked when players are loaded</param>
        private void LoadCurrentPlayers(Action onComplete = null)
        {
            if (_playersRef == null)
            {
                Debug.LogError("LobbyManager: Cannot load players - Firebase reference not initialized");
                onComplete?.Invoke();
                return;
            }

            Debug.Log("LobbyManager: Loading current players from Firebase");

            _playersRef.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"LobbyManager: Failed to load players: {task.Exception?.Message}");
                    InvokeOnMainThread(onComplete);
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    Debug.Log("LobbyManager: No players found in lobby");
                    InvokeOnMainThread(onComplete);
                    return;
                }

                // Process snapshot data on main thread
                InvokeOnMainThread(() =>
                {
                    _players.Clear();
                    foreach (DataSnapshot playerSnapshot in snapshot.Children)
                    {
                        var playerDict = playerSnapshot.Value as Dictionary<string, object>;
                        if (playerDict != null)
                        {
                            var playerData = DictionaryToPlayerData(playerDict);
                            _players[playerData.PlayerId] = playerData;
                            Debug.Log($"LobbyManager: Loaded player {playerData.DisplayName} ({playerData.PlayerId})");
                        }
                    }

                    Debug.Log($"LobbyManager: Loaded {_players.Count} players");
                    // TODO: Trigger UI update event if needed
                    onComplete?.Invoke();
                });
            });
        }

        // Firebase event handlers (to be implemented when SDK is added)
        private void OnLobbyDataChanged(object sender, ValueChangedEventArgs args)
        {
            Debug.Log("LobbyManager: Lobby data changed");
            // Example: Check if gameStarted flag changed
            // var lobbyData = args.Snapshot.Value as Dictionary<string, object>;
            // if (lobbyData != null && lobbyData.ContainsKey("gameStarted"))
            // {
            //     bool gameStarted = (bool)lobbyData["gameStarted"];
            //     // Handle game start
            // }
        }

        private void OnPlayerAdded(object sender, ChildChangedEventArgs args)
        {
            if (args.Snapshot == null || !args.Snapshot.Exists) return;

            var playerDict = args.Snapshot.Value as Dictionary<string, object>;
            if (playerDict == null) return;

            var playerData = DictionaryToPlayerData(playerDict);
            if (!_players.ContainsKey(playerData.PlayerId))
            {
                _players[playerData.PlayerId] = playerData;
                Debug.Log($"LobbyManager: Player added - {playerData.DisplayName} ({playerData.PlayerId})");
                // TODO: Trigger UI update event
            }
        }

        private void OnPlayerChanged(object sender, ChildChangedEventArgs args)
        {
            if (args.Snapshot == null || !args.Snapshot.Exists) return;

            var playerDict = args.Snapshot.Value as Dictionary<string, object>;
            if (playerDict == null) return;

            var playerData = DictionaryToPlayerData(playerDict);
            if (_players.ContainsKey(playerData.PlayerId))
            {
                _players[playerData.PlayerId] = playerData;
                Debug.Log($"LobbyManager: Player updated - {playerData.DisplayName} ({playerData.PlayerId}) Ready={playerData.IsReady}");
                // TODO: Trigger UI update event
            }
        }

        private void OnPlayerRemoved(object sender, ChildChangedEventArgs args)
        {
            if (args.Snapshot == null || !args.Snapshot.Exists) return;

            string playerId = args.Snapshot.Key;
            if (_players.ContainsKey(playerId))
            {
                _players.Remove(playerId);
                Debug.Log($"LobbyManager: Player removed - {playerId}");
                // TODO: Trigger UI update event
            }
        }

        // Firebase database references
        private DatabaseReference _lobbyRef;
        private DatabaseReference _playersRef;

        /// <summary>
        /// Get current lobby status for debugging.
        /// </summary>
        public string GetStatusString()
        {
            return $"Lobby Status: InLobby={_isInLobby}, Code={_currentLobbyCode}, Players={_players.Count}";
        }

        /// <summary>
        /// Helper method to invoke an action on the main Unity thread.
        /// </summary>
        private void InvokeOnMainThread(Action action)
        {
            if (action == null) return;
            _mainThreadActions.Enqueue(action);
        }

        /// <summary>
        /// Helper method to invoke an action with one argument on the main Unity thread.
        /// </summary>
        private void InvokeOnMainThread<T>(Action<T> action, T arg)
        {
            if (action == null)
            {
                // Safe logging - don't call ToString() on potentially Unity objects
                Debug.LogWarning($"LobbyManager.InvokeOnMainThread<T>: action is null, skipping. Type: {typeof(T).Name}");
                return;
            }
            // Enqueue action to be executed on main thread
            _mainThreadActions.Enqueue(() => action(arg));
        }

        /// <summary>
        /// Helper method to invoke an action with two arguments on the main Unity thread.
        /// </summary>
        private void InvokeOnMainThread<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null) return;
            _mainThreadActions.Enqueue(() => action(arg1, arg2));
        }
    }
}
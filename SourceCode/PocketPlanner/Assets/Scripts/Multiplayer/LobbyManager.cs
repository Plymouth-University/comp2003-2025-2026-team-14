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
        private string _deviceId = string.Empty; // Cached device ID for Firebase operations

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

            // Cache device ID on main thread for use in Firebase callbacks
            _deviceId = SystemInfo.deviceUniqueIdentifier;
            Debug.Log($"LobbyManager: Device ID cached: {_deviceId}");
        }

        private void Update()
        {
            // Process main thread actions
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    Debug.Log($"LobbyManager.Update: Executing main thread action ({action.Method.Name})");
                    action?.Invoke();
                    Debug.Log($"LobbyManager.Update: Action executed successfully");
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
            var hostPlayer = new PlayerData(hostPlayerId, "Host", true, _deviceId);
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

            Debug.Log($"LobbyManager: Firebase references set - LobbyRef: {_lobbyRef != null}, PlayersRef: {_playersRef != null}, DatabaseRef: {_firebaseManager.DatabaseReference != null}");

            _lobbyRef.GetValueAsync().ContinueWith(task => {
                Debug.Log($"LobbyManager: GetValueAsync completed - IsCompleted: {task.IsCompleted}, IsFaulted: {task.IsFaulted}, IsCanceled: {task.IsCanceled}");
                if (task.IsFaulted)
                {
                    Debug.LogError($"LobbyManager: GetValueAsync failed - {task.Exception?.Message}");
                    InvokeOnMainThread(onError, $"Failed to join lobby: {task.Exception?.Message}");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                Debug.Log($"LobbyManager: Snapshot exists: {snapshot.Exists}, ChildrenCount: {snapshot.ChildrenCount}");
                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"LobbyManager: Lobby not found - {lobbyCode}");
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
                Debug.Log($"LobbyManager: Player count: {playerCount}, Max players: {maxPlayers}");
                if (playerCount >= maxPlayers)
                {
                    Debug.LogWarning($"LobbyManager: Lobby is full - {playerCount}/{maxPlayers} players");
                    InvokeOnMainThread(onError, $"Lobby is full (max {maxPlayers} players)");
                    return;
                }

                // Read turn time limit
                int turnTimeLimit = -1; // default unlimited
                Dictionary<string, object> playerDict = null;

                try
                {
                    if (snapshot.Child("turnTimeLimit").Exists)
                    {
                        turnTimeLimit = Convert.ToInt32(snapshot.Child("turnTimeLimit").Value);
                    }

                    Debug.Log($"LobbyManager: Turn time limit read successfully: {turnTimeLimit}");

                    // Add player to lobby
                    var playerData = new PlayerData(playerId, $"Player{playerCount + 1}", false, _deviceId);
                    Debug.Log($"LobbyManager: PlayerData created - PlayerId: {playerId}, DisplayName: {playerData.DisplayName}, DeviceId: {playerData.DeviceId}");

                    playerDict = PlayerDataToDictionary(playerData);
                    Debug.Log($"LobbyManager: PlayerData converted to dictionary with {playerDict.Count} entries");

                    if (_playersRef == null)
                    {
                        Debug.LogError("LobbyManager: _playersRef is null! Cannot add player to Firebase");
                        InvokeOnMainThread(onError, "Firebase reference lost. Please try again.");
                        return;
                    }

                    Debug.Log($"LobbyManager: Adding player to Firebase - PlayerId: {playerId}, PlayersRef valid: {_playersRef != null}");

                    // Now add player to Firebase
                    _playersRef.Child(playerId).SetValueAsync(playerDict).ContinueWith(joinTask => {
                    Debug.Log($"LobbyManager: SetValueAsync completed - IsCompleted: {joinTask.IsCompleted}, IsFaulted: {joinTask.IsFaulted}, IsCanceled: {joinTask.IsCanceled}");
                    if (joinTask.IsFaulted)
                    {
                        Debug.LogError($"LobbyManager: SetValueAsync failed - {joinTask.Exception?.Message}");
                        InvokeOnMainThread(onError, $"Failed to join lobby: {joinTask.Exception?.Message}");
                        return;
                    }

                    // Set up listeners and get current player list
                    Debug.Log($"LobbyManager: SetValueAsync succeeded, setting up listeners");
                    SetupLobbyListeners(lobbyCode);

                    _currentLobbyCode = lobbyCode;
                    _isInLobby = true;
                    _maxPlayers = maxPlayers;
                    _turnTimeLimit = turnTimeLimit;

                    Debug.Log($"LobbyManager: Local state updated - LobbyCode: {_currentLobbyCode}, InLobby: {_isInLobby}");

                    // Load all players from Firebase (including the one we just added)
                    LoadCurrentPlayers(() => {
                        // Callback after players are loaded
                        Debug.Log($"LobbyManager: LoadCurrentPlayers completed, invoking onLobbyJoined with {_players.Count} players");
                        InvokeOnMainThread(() => onLobbyJoined?.Invoke(lobbyCode, _players));
                    });
                });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LobbyManager: Exception in lobby join process: {ex.Message}");
                    Debug.LogError($"LobbyManager: Stack trace: {ex.StackTrace}");
                    InvokeOnMainThread(onError, $"Failed to join lobby: {ex.Message}");
                }
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
        /// <param name="onComplete">Optional callback invoked after Firebase removal attempt (regardless of success)</param>
        public void LeaveLobby(Action onComplete = null)
        {
            if (!_isInLobby)
            {
                onComplete?.Invoke();
                return;
            }

            // Remove player from Firebase
            string userId = FirebaseManager.Instance?.UserId;
            Debug.Log($"LobbyManager.LeaveLobby: Checking Firebase removal - playersRef={_playersRef != null}, userId={userId ?? "null"}, FirebaseManager.Instance={FirebaseManager.Instance != null}, IsHost={IsLocalPlayerHost()}");

            // Check if leaving player is host
            bool isHost = IsLocalPlayerHost();

            if (_playersRef != null && !string.IsNullOrEmpty(userId))
            {
                if (isHost)
                {
                    Debug.Log($"LobbyManager.LeaveLobby: Host {userId} is leaving, deleting entire lobby {_currentLobbyCode}");
                    // Host leaving - delete entire lobby
                    if (_lobbyRef != null)
                    {
                        _lobbyRef.RemoveValueAsync().ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                Debug.LogError($"LobbyManager: Failed to delete lobby {_currentLobbyCode} from Firebase: {task.Exception?.Message}");
                            }
                            else if (task.IsCanceled)
                            {
                                Debug.LogWarning($"LobbyManager: Lobby deletion for {_currentLobbyCode} was canceled");
                            }
                            else
                            {
                                Debug.Log($"LobbyManager: Successfully deleted lobby {_currentLobbyCode} from Firebase");
                            }

                            // Clean up listeners AFTER Firebase deletion attempt (success or failure)
                            CleanupLobbyListenersAndState();

                            // Invoke callback on main thread
                            InvokeOnMainThread(() => onComplete?.Invoke());
                        });
                    }
                    else
                    {
                        Debug.LogError($"LobbyManager.LeaveLobby: Cannot delete lobby - _lobbyRef is null");
                        CleanupLobbyListenersAndState();
                        InvokeOnMainThread(() => onComplete?.Invoke());
                    }
                }
                else
                {
                    Debug.Log($"LobbyManager.LeaveLobby: Removing player {userId} from Firebase");
                    _playersRef.Child(userId).RemoveValueAsync().ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"LobbyManager: Failed to remove player {userId} from Firebase: {task.Exception?.Message}");
                        }
                        else if (task.IsCanceled)
                        {
                            Debug.LogWarning($"LobbyManager: Player removal for {userId} was canceled");
                        }
                        else
                        {
                            Debug.Log($"LobbyManager: Successfully removed player {userId} from Firebase");
                        }

                        // Clean up listeners AFTER Firebase removal attempt (success or failure)
                        CleanupLobbyListenersAndState();

                        // Invoke callback on main thread
                        InvokeOnMainThread(() => onComplete?.Invoke());
                    });
                }
            }
            else
            {
                Debug.LogWarning($"LobbyManager.LeaveLobby: Cannot remove player from Firebase - playersRef={_playersRef != null}, userId={userId ?? "null"}, isHost={isHost}");
                // Still clean up even if Firebase removal couldn't start
                CleanupLobbyListenersAndState();
                InvokeOnMainThread(() => onComplete?.Invoke());
            }
        }

        /// <summary>
        /// Clean up Firebase listeners and reset lobby state.
        /// Called after Firebase removal attempt.
        /// </summary>
        private void CleanupLobbyListenersAndState()
        {
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
        /// Check if the local player is the host of the current lobby.
        /// </summary>
        private bool IsLocalPlayerHost()
        {
            if (!_isInLobby || string.IsNullOrEmpty(_currentLobbyCode))
                return false;

            // First check MultiplayerManager's IsLobbyHost property (source of truth)
            if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsLobbyHost)
                return true;

            // Fallback: check player data in case MultiplayerManager not yet updated
            string localPlayerId = _firebaseManager?.UserId;
            if (string.IsNullOrEmpty(localPlayerId))
            {
                // Fallback to FirebaseManager.Instance
                localPlayerId = FirebaseManager.Instance?.UserId;
                if (string.IsNullOrEmpty(localPlayerId))
                    return false;
            }

            foreach (var player in _players.Values)
            {
                if (player.PlayerId == localPlayerId && player.IsHost)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Kick a player from the lobby (host only).
        /// </summary>
        public void KickPlayer(string playerId)
        {
            if (!_isInLobby || string.IsNullOrEmpty(_currentLobbyCode))
            {
                Debug.LogWarning($"LobbyManager: Cannot kick player - not in a valid lobby");
                return;
            }

            // Check if local player is host
            if (!IsLocalPlayerHost())
            {
                Debug.LogWarning($"LobbyManager: Cannot kick player - local player is not host");
                return;
            }

            // Ensure player exists
            if (!_players.ContainsKey(playerId))
            {
                Debug.LogWarning($"LobbyManager: Cannot kick player - player {playerId} not found in lobby");
                return;
            }

            // Prevent kicking host (should not happen as UI hides kick button for host)
            if (_players[playerId].IsHost)
            {
                Debug.LogError($"LobbyManager: Cannot kick host player {playerId}");
                return;
            }

            // Remove player from Firebase
            if (_playersRef != null)
            {
                _playersRef.Child(playerId).RemoveValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"LobbyManager: Failed to kick player {playerId} from Firebase: {task.Exception?.Message}");
                    }
                    else
                    {
                        Debug.Log($"LobbyManager: Player {playerId} kicked from Firebase successfully");
                    }
                });
            }
            else
            {
                Debug.LogError($"LobbyManager: Cannot kick player - Firebase reference not initialized");
            }

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

                            // Update MultiplayerManager with loaded player data
                            Debug.Log($"LobbyManager.LoadCurrentPlayers: MultiplayerManager.Instance = {MultiplayerManager.Instance != null}");
                            if (MultiplayerManager.Instance != null)
                            {
                                Debug.Log($"LobbyManager.LoadCurrentPlayers: Calling UpdatePlayerFromFirebase for {playerData.PlayerId}");
                                MultiplayerManager.Instance.UpdatePlayerFromFirebase(playerData);
                            }
                            else
                            {
                                Debug.LogError("LobbyManager.LoadCurrentPlayers: MultiplayerManager.Instance is null!");
                            }
                        }
                    }

                    Debug.Log($"LobbyManager: Loaded {_players.Count} players");
                    onComplete?.Invoke();
                });
            });
        }

        // Firebase event handlers (to be implemented when SDK is added)
        private void OnLobbyDataChanged(object sender, ValueChangedEventArgs args)
        {
            Debug.Log("LobbyManager: Lobby data changed");

            // If we're no longer in a lobby, ignore changes (cleanup already in progress)
            if (!_isInLobby)
            {
                Debug.Log($"LobbyManager: Ignoring lobby data change - already leaving lobby {_currentLobbyCode}");
                return;
            }

            // Check if lobby was deleted (snapshot doesn't exist)
            if (args.Snapshot == null || !args.Snapshot.Exists)
            {
                Debug.Log($"LobbyManager: Lobby {_currentLobbyCode} was deleted (likely by host). Cleaning up and returning to main menu.");

                // Clean up listeners and state
                CleanupLobbyListenersAndState();

                // Notify MultiplayerManager that lobby was deleted
                InvokeOnMainThread(() =>
                {
                    if (MultiplayerManager.Instance != null)
                    {
                        Debug.Log($"LobbyManager: Notifying MultiplayerManager about lobby deletion");
                        MultiplayerManager.Instance.DisableMultiplayerMode(() =>
                        {
                            Debug.Log($"LobbyManager: Lobby deletion cleanup complete, loading main menu");
                            PPSceneManager.LoadMainMenu();
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"LobbyManager: MultiplayerManager.Instance is null, loading main menu directly");
                        PPSceneManager.LoadMainMenu();
                    }
                });
                return;
            }

            // Lobby still exists, check for data changes
            var lobbyData = args.Snapshot.Value as Dictionary<string, object>;
            if (lobbyData != null)
            {
                // Example: Check if gameStarted flag changed
                // if (lobbyData.ContainsKey("gameStarted"))
                // {
                //     bool gameStarted = (bool)lobbyData["gameStarted"];
                //     // Handle game start
                // }
                Debug.Log($"LobbyManager: Lobby data updated - keys: {string.Join(", ", lobbyData.Keys)}");
            }
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
                // Trigger MultiplayerManager events on main thread
                InvokeOnMainThread(() => {
                    Debug.Log($"LobbyManager.OnPlayerAdded: In main thread action, MultiplayerManager.Instance = {MultiplayerManager.Instance != null}");
                    if (MultiplayerManager.Instance != null)
                    {
                        Debug.Log($"LobbyManager.OnPlayerAdded: Calling UpdatePlayerFromFirebase for {playerData.PlayerId}");
                        MultiplayerManager.Instance.UpdatePlayerFromFirebase(playerData);
                    }
                    else
                    {
                        Debug.LogError("LobbyManager.OnPlayerAdded: MultiplayerManager.Instance is null!");
                    }
                });
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
                // Trigger MultiplayerManager events on main thread
                InvokeOnMainThread(() => {
                    Debug.Log($"LobbyManager.OnPlayerChanged: In main thread action, MultiplayerManager.Instance = {MultiplayerManager.Instance != null}");
                    if (MultiplayerManager.Instance != null)
                    {
                        Debug.Log($"LobbyManager.OnPlayerChanged: Calling UpdatePlayerFromFirebase for {playerData.PlayerId}");
                        MultiplayerManager.Instance.UpdatePlayerFromFirebase(playerData);
                    }
                    else
                    {
                        Debug.LogError("LobbyManager.OnPlayerChanged: MultiplayerManager.Instance is null!");
                    }
                });
            }
        }

        private void OnPlayerRemoved(object sender, ChildChangedEventArgs args)
        {
            if (args.Snapshot == null || !args.Snapshot.Exists)
            {
                Debug.Log("LobbyManager.OnPlayerRemoved: Snapshot null or doesn't exist");
                return;
            }

            string playerId = args.Snapshot.Key;
            Debug.Log($"LobbyManager.OnPlayerRemoved: Player ID from snapshot key: {playerId}");

            // Check if this is the local player being removed
            string localPlayerId = _firebaseManager?.UserId;
            if (string.IsNullOrEmpty(localPlayerId))
            {
                localPlayerId = FirebaseManager.Instance?.UserId;
            }
            Debug.Log($"LobbyManager.OnPlayerRemoved: Local player ID: {localPlayerId}, Is this player being removed the local player? {playerId == localPlayerId}");

            if (_players.ContainsKey(playerId))
            {
                _players.Remove(playerId);
                Debug.Log($"LobbyManager: Player removed from local dictionary - {playerId}");
                // Trigger MultiplayerManager events on main thread
                InvokeOnMainThread(() => {
                    Debug.Log($"LobbyManager.OnPlayerRemoved: In main thread action, MultiplayerManager.Instance = {MultiplayerManager.Instance != null}");
                    if (MultiplayerManager.Instance != null)
                    {
                        Debug.Log($"LobbyManager.OnPlayerRemoved: Calling RemovePlayerFromFirebase for {playerId}");
                        MultiplayerManager.Instance.RemovePlayerFromFirebase(playerId);
                    }
                    else
                    {
                        Debug.LogError("LobbyManager.OnPlayerRemoved: MultiplayerManager.Instance is null!");
                    }
                });
            }
            else
            {
                Debug.Log($"LobbyManager.OnPlayerRemoved: Player {playerId} not found in local _players dictionary. Dictionary count: {_players.Count}");
                // Even if not in local dictionary, still notify MultiplayerManager
                InvokeOnMainThread(() => {
                    if (MultiplayerManager.Instance != null)
                    {
                        Debug.Log($"LobbyManager.OnPlayerRemoved: Player {playerId} not in local dict, but calling RemovePlayerFromFirebase anyway");
                        MultiplayerManager.Instance.RemovePlayerFromFirebase(playerId);
                    }
                });
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
            if (action == null)
            {
                Debug.LogWarning("LobbyManager.InvokeOnMainThread: action is null");
                return;
            }
            Debug.Log($"LobbyManager.InvokeOnMainThread: Enqueuing action ({action.Method.Name})");
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
            if (action == null)
            {
                Debug.LogWarning($"LobbyManager.InvokeOnMainThread<T1,T2>: action is null, skipping. Types: {typeof(T1).Name}, {typeof(T2).Name}");
                return;
            }
            Debug.Log($"LobbyManager.InvokeOnMainThread<T1,T2>: Enqueuing action ({action.Method.Name})");
            _mainThreadActions.Enqueue(() => action(arg1, arg2));
        }
    }
}
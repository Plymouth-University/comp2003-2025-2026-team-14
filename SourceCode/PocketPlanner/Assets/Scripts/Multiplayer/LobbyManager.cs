using System;
using System.Collections;
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

        /// <summary>
        /// Create a new lobby as host.
        /// </summary>
        /// <param name="hostPlayerId">ID of the host player</param>
        /// <param name="onLobbyCreated">Callback when lobby is created</param>
        /// <param name="onError">Callback when error occurs</param>
        public void CreateLobby(string hostPlayerId, Action<string> onLobbyCreated, Action<string> onError)
        {
            if (!_firebaseManager.IsReady())
            {
                onError?.Invoke("Firebase not ready");
                return;
            }

            if (_isInLobby)
            {
                onError?.Invoke("Already in a lobby");
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
                { "maxPlayers", 8 },
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

            _lobbyRef.SetValueAsync(lobbyData).ContinueWith(task => {
                if (task.IsFaulted)
                {
                    onError?.Invoke($"Failed to create lobby: {task.Exception?.Message}");
                    return;
                }

                // Set up listeners
                SetupLobbyListeners(lobbyCode);

                _currentLobbyCode = lobbyCode;
                _isInLobby = true;
                _players[hostPlayerId] = hostPlayer;

                onLobbyCreated?.Invoke(lobbyCode);
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
                onError?.Invoke("Firebase not ready");
                return;
            }

            if (_isInLobby)
            {
                onError?.Invoke("Already in a lobby");
                return;
            }

            if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6)
            {
                onError?.Invoke("Invalid lobby code");
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
                    onError?.Invoke($"Failed to join lobby: {task.Exception?.Message}");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    onError?.Invoke("Lobby not found");
                    return;
                }

                // Check player count
                int playerCount = (int)snapshot.Child("players").ChildrenCount;
                if (playerCount >= 8)
                {
                    onError?.Invoke("Lobby is full (max 8 players)");
                    return;
                }

                // Add player to lobby
                var playerData = new PlayerData(playerId, $"Player{playerCount + 1}", false, SystemInfo.deviceUniqueIdentifier);
                var playerDict = PlayerDataToDictionary(playerData);

                _playersRef.Child(playerId).SetValueAsync(playerDict).ContinueWith(joinTask => {
                    if (joinTask.IsFaulted)
                    {
                        onError?.Invoke($"Failed to join lobby: {joinTask.Exception?.Message}");
                        return;
                    }

                    // Set up listeners and get current player list
                    SetupLobbyListeners(lobbyCode);
                    LoadCurrentPlayers();

                    _currentLobbyCode = lobbyCode;
                    _isInLobby = true;
                    _players[playerId] = playerData;

                    onLobbyJoined?.Invoke(lobbyCode, _players);
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
        private void LoadCurrentPlayers()
        {
            if (_playersRef == null)
            {
                Debug.LogError("LobbyManager: Cannot load players - Firebase reference not initialized");
                return;
            }

            Debug.Log("LobbyManager: Loading current players from Firebase");

            _playersRef.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"LobbyManager: Failed to load players: {task.Exception?.Message}");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    Debug.Log("LobbyManager: No players found in lobby");
                    return;
                }

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
    }
}
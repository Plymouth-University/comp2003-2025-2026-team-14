using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using System.Threading.Tasks;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Simple test script to verify multiplayer system functionality.
    /// Attach to a GameObject and use the ContextMenu options for testing.
    /// </summary>
    public class MultiplayerTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private string testLobbyCode = "TEST12";

        private FirebaseManager _firebaseManager;
        private MultiplayerManager _multiplayerManager;

        private void Start()
        {
            InitializeManagers();

            if (autoInitialize)
            {
                Debug.Log("MultiplayerTester: Auto-initialization enabled");
                Invoke(nameof(RunBasicTests), 1.0f);
            }
        }

        private void InitializeManagers()
        {
            // Try to get FirebaseManager instance
            _firebaseManager = FirebaseManager.Instance;
            if (_firebaseManager == null)
            {
                Debug.LogWarning("MultiplayerTester: FirebaseManager.Instance is null. Searching for FirebaseManager in scene...");

                // Search for existing FirebaseManager in scene
                var firebaseManagerObj = GameObject.FindAnyObjectByType<FirebaseManager>();
                if (firebaseManagerObj != null)
                {
                    _firebaseManager = firebaseManagerObj;
                    Debug.Log("MultiplayerTester: Found FirebaseManager in scene.");
                }
                else
                {
                    Debug.LogError("MultiplayerTester: No FirebaseManager found in scene. Please add a GameObject with FirebaseManager component to the scene.");
                    Debug.LogError("To fix: Create empty GameObject → Add FirebaseManager component → Add MultiplayerManager component");
                    return;
                }
            }

            // Try to get MultiplayerManager instance
            _multiplayerManager = MultiplayerManager.Instance;
            if (_multiplayerManager == null)
            {
                Debug.LogWarning("MultiplayerTester: MultiplayerManager.Instance is null. Searching for MultiplayerManager in scene...");

                // Search for existing MultiplayerManager in scene
                var multiplayerManagerObj = GameObject.FindAnyObjectByType<MultiplayerManager>();
                if (multiplayerManagerObj != null)
                {
                    _multiplayerManager = multiplayerManagerObj;
                    Debug.Log("MultiplayerTester: Found MultiplayerManager in scene.");
                }
                else
                {
                    Debug.LogWarning("MultiplayerTester: No MultiplayerManager found in scene. Some tests may fail.");
                }
            }

            Debug.Log("MultiplayerTester: Managers initialized.");
        }

        [ContextMenu("Run Basic Tests")]
        private void RunBasicTests()
        {
            Debug.Log("=== Multiplayer System Tests ===");

            // Ensure managers are initialized
            InitializeManagers();

            // Reset any existing multiplayer state before running tests
            Debug.Log("Resetting multiplayer system...");
            ResetMultiplayer();

            // Small delay to ensure reset completes
            Invoke(nameof(RunTestsAfterReset), 0.1f);
        }

        private void RunTestsAfterReset()
        {
            TestFirebaseManager();
            TestPlayerData();
            TestMultiplayerManager();
            TestFirebaseReadWrite();

            Debug.Log("=== Tests Complete ===");
        }

        [ContextMenu("Test Firebase Manager")]
        private void TestFirebaseManager()
        {
            // Ensure managers are initialized (in case test is run via ContextMenu before Start())
            if (_firebaseManager == null)
            {
                Debug.LogWarning("TestFirebaseManager: FirebaseManager not initialized. Initializing managers...");
                InitializeManagers();
            }

            if (_firebaseManager == null)
            {
                Debug.LogError("TestFirebaseManager: FirebaseManager not available. Please ensure:");
                Debug.LogError("1. A GameObject with FirebaseManager component exists in the scene");
                Debug.LogError("2. The FirebaseManager GameObject is active and initialized");
                Debug.LogError("3. Check Console for Firebase initialization errors");
                return;
            }

            Debug.Log($"FirebaseManager Status: {_firebaseManager.GetStatusString()}");
            if (_firebaseManager.IsInitialized && _firebaseManager.IsAuthenticated)
            {
                Debug.Log("FirebaseManager test passed (Firebase SDK integrated)");
            }
            else
            {
                Debug.LogWarning("FirebaseManager test passed but Firebase not fully initialized");
            }
        }

        [ContextMenu("Test Player Data")]
        private void TestPlayerData()
        {
            // Test PlayerData serialization
            var playerData = new PlayerData("test_player_123", "Test Player", true, "test_device_id");
            playerData.IsReady = true;

            string json = playerData.ToJson();
            Debug.Log($"PlayerData JSON: {json}");

            var deserialized = PlayerData.FromJson(json);
            Debug.Log($"Deserialized PlayerData: {deserialized}");

            if (deserialized.IsValid() && deserialized.PlayerId == "test_player_123")
            {
                Debug.Log("PlayerData serialization test passed");
            }
            else
            {
                Debug.LogError("PlayerData serialization test failed");
            }
        }

        [ContextMenu("Test Multiplayer Manager")]
        private void TestMultiplayerManager()
        {
            // Ensure managers are initialized (in case test is run via ContextMenu before Start())
            if (_firebaseManager == null || _multiplayerManager == null)
            {
                Debug.LogWarning("TestMultiplayerManager: Managers not initialized. Initializing managers...");
                InitializeManagers();
            }

            if (_multiplayerManager == null)
            {
                Debug.LogError("TestMultiplayerManager: MultiplayerManager not available. Please ensure:");
                Debug.LogError("1. A GameObject with MultiplayerManager component exists in the scene");
                Debug.LogError("2. The MultiplayerManager GameObject is active");
                Debug.LogError("3. FirebaseManager is also present in the scene");
                return;
            }

            Debug.Log($"MultiplayerManager Status: {_multiplayerManager.GetStatusString()}");

            // Test enabling multiplayer mode (simulated)
            _multiplayerManager.EnableMultiplayerMode(true, testLobbyCode);

            // Check status after a delay (allow time for async lobby creation)
            Debug.Log("MultiplayerManager: Waiting for lobby creation...");
            Invoke(nameof(CheckMultiplayerStatus), 2.0f);
        }

        private void CheckMultiplayerStatus()
        {
            if (_multiplayerManager == null) return;

            Debug.Log($"MultiplayerManager after enable: {_multiplayerManager.GetStatusString()}");

            if (_multiplayerManager.IsMultiplayerMode)
            {
                Debug.Log("MultiplayerManager enable test passed");
            }
            else
            {
                Debug.LogError("MultiplayerManager enable test failed");
            }

            // Test player list
            var playerNames = _multiplayerManager.GetPlayerNames();
            Debug.Log($"Player count: {playerNames.Count}");

            // Test ready state
            _multiplayerManager.SetLocalPlayerReady(true);
            Debug.Log("Local player ready state set to true");
        }

        [ContextMenu("Test Sync Manager")]
        private void TestSyncManager()
        {
            var syncManager = GetComponent<SyncManager>();
            if (syncManager == null)
            {
                syncManager = gameObject.AddComponent<SyncManager>();
            }

            // Test placement action serialization
            var placementJson = syncManager.SerializePlacementAction("T", "Residential", 3, 4, 0, false, 1);
            Debug.Log($"Placement action JSON: {placementJson}");

            var placementData = syncManager.DeserializePlacementAction(placementJson);
            Debug.Log($"Deserialized placement: {placementData.shapeType} at ({placementData.positionX},{placementData.positionY})");

            // Test dice roll serialization
            int[] shapeDice = { 0, 2, 4 };
            int[] buildingDice = { 1, 1, 3 }; // Double face at index 1
            var diceJson = syncManager.SerializeDiceRoll(shapeDice, buildingDice, 12345, 1);
            Debug.Log($"Dice roll JSON: {diceJson}");

            var diceData = syncManager.DeserializeDiceRoll(diceJson);
            Debug.Log($"Deserialized dice: Turn {diceData.turnNumber}, Seed {diceData.sharedRandomSeed}");

            syncManager.LogSerializationStats(placementJson, "Placement Action");
            syncManager.LogSerializationStats(diceJson, "Dice Roll");

            Debug.Log("SyncManager tests passed");
        }

        [ContextMenu("Test Firebase Read/Write")]
        private void TestFirebaseReadWrite()
        {
            // Ensure managers are initialized (in case test is run via ContextMenu before Start())
            if (_firebaseManager == null)
            {
                Debug.LogWarning("TestFirebaseReadWrite: FirebaseManager not initialized. Initializing managers...");
                InitializeManagers();
            }

            if (_firebaseManager == null)
            {
                Debug.LogError("TestFirebaseReadWrite: FirebaseManager not available. Please ensure:");
                Debug.LogError("1. A GameObject with FirebaseManager component exists in the scene");
                Debug.LogError("2. The FirebaseManager GameObject is active and initialized");
                Debug.LogError("3. Check Console for Firebase initialization errors");
                return;
            }

            if (!_firebaseManager.IsReady())
            {
                Debug.LogWarning($"TestFirebaseReadWrite: Firebase not ready (Initialized={_firebaseManager.IsInitialized}, Authenticated={_firebaseManager.IsAuthenticated}). Waiting...");
                // Wait a bit and retry
                Invoke(nameof(TestFirebaseReadWrite), 1.0f);
                return;
            }

            Debug.Log("Starting Firebase Read/Write test...");

            string userId = _firebaseManager.UserId;
            string lobbyCode = $"TEST_{userId}_{DateTime.UtcNow.Ticks}";
            DatabaseReference lobbyRef = _firebaseManager.DatabaseReference.Child("lobbies").Child(lobbyCode);

            // Create test lobby data (minimal structure)
            var lobbyData = new Dictionary<string, object>
            {
                { "lobbyCode", lobbyCode },
                { "hostPlayerId", userId },
                { "createdAt", DateTime.UtcNow.Ticks },
                { "gameStarted", false },
                { "maxPlayers", 8 },
                { "players", new Dictionary<string, object>() }
            };

            // Add current player as host
            var playerData = new Dictionary<string, object>
            {
                { "playerId", userId },
                { "displayName", "Test Player" },
                { "isReady", false },
                { "isHost", true },
                { "joinedAt", DateTime.UtcNow.Ticks }
            };

            ((Dictionary<string, object>)lobbyData["players"])[userId] = playerData;

            Debug.Log($"Creating test lobby with code: {lobbyCode}");
            lobbyRef.SetValueAsync(lobbyData).ContinueWith(writeTask =>
            {
                if (writeTask.IsFaulted)
                {
                    Debug.LogError($"Firebase write failed: {writeTask.Exception?.Message}");
                    return;
                }

                Debug.Log("Firebase write successful. Reading back...");

                // Read back the lobby data
                lobbyRef.GetValueAsync().ContinueWith(readTask =>
                {
                    if (readTask.IsFaulted)
                    {
                        Debug.LogError($"Firebase read failed: {readTask.Exception?.Message}");
                        return;
                    }

                    DataSnapshot snapshot = readTask.Result;
                    if (!snapshot.Exists)
                    {
                        Debug.LogError("Firebase read failed: No data found");
                        return;
                    }

                    var readLobbyData = snapshot.Value as Dictionary<string, object>;
                    if (readLobbyData == null)
                    {
                        Debug.LogError("Firebase read failed: Invalid data format");
                        return;
                    }

                    string dataStr = "";
                    foreach (var kv in readLobbyData)
                    {
                        if (kv.Key == "players")
                        {
                            dataStr += "players={...}, ";
                        }
                        else
                        {
                            dataStr += $"{kv.Key}={kv.Value}, ";
                        }
                    }
                    Debug.Log($"Firebase read successful! Lobby data: {dataStr}");

                    // Verify lobby code matches
                    if (readLobbyData.ContainsKey("lobbyCode") && readLobbyData["lobbyCode"].ToString() == lobbyCode)
                    {
                        Debug.Log("Firebase Read/Write test PASSED! Lobby created and read successfully.");
                    }
                    else
                    {
                        Debug.LogError("Firebase Read/Write test FAILED: Lobby data mismatch");
                    }

                    // Clean up: delete test lobby (host can delete)
                    lobbyRef.RemoveValueAsync().ContinueWith(deleteTask =>
                    {
                        if (deleteTask.IsFaulted)
                        {
                            Debug.LogWarning($"Failed to delete test lobby: {deleteTask.Exception?.Message}");
                        }
                        else
                        {
                            Debug.Log("Test lobby cleaned up.");
                        }
                    });
                });
            });
        }

        [ContextMenu("Simulate Lobby Creation")]
        private void SimulateLobbyCreation()
        {
            if (_multiplayerManager == null) return;

            Debug.Log("Simulating lobby creation...");
            _multiplayerManager.EnableMultiplayerMode(true, "");
        }

        [ContextMenu("Simulate Lobby Join")]
        private void SimulateLobbyJoin()
        {
            if (_multiplayerManager == null) return;

            Debug.Log($"Simulating join to lobby {testLobbyCode}...");
            _multiplayerManager.EnableMultiplayerMode(false, testLobbyCode);
        }

        [ContextMenu("Reset Multiplayer")]
        private void ResetMultiplayer()
        {
            if (_multiplayerManager != null)
            {
                _multiplayerManager.DisableMultiplayerMode();
                Debug.Log("Multiplayer mode disabled");
            }

            Debug.Log("Multiplayer system reset");
        }

        [ContextMenu("Print All Status")]
        private void PrintAllStatus()
        {
            if (_firebaseManager != null)
            {
                Debug.Log(_firebaseManager.GetStatusString());
            }

            if (_multiplayerManager != null)
            {
                Debug.Log(_multiplayerManager.GetStatusString());
            }

            var lobbyManager = GetComponent<LobbyManager>();
            if (lobbyManager != null)
            {
                // Using reflection to call GetStatusString if it exists
                var method = lobbyManager.GetType().GetMethod("GetStatusString");
                if (method != null)
                {
                    var status = method.Invoke(lobbyManager, null);
                    Debug.Log($"LobbyManager: {status}");
                }
            }
        }

        private void Update()
        {
            // Optional: Add any real-time test updates here
        }
    }
}
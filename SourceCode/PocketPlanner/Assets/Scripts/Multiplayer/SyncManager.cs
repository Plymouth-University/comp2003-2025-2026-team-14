using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Handles serialization and deserialization of game state for Firebase synchronization.
    /// Compresses board data and manages efficient state transmission.
    /// </summary>
    public class SyncManager : MonoBehaviour
    {
        // Singleton instance
        public static SyncManager Instance { get; private set; }
        // References
        private FirebaseManager _firebaseManager;
        private GameManager _gameManager;
        private MultiplayerManager _multiplayerManager;

        // Firebase listener references
        private DatabaseReference _lobbyRootRef;
        private DatabaseReference _gameStateRef;
        private DatabaseReference _diceRollRef;
        private DatabaseReference _placementsRef;
        private DatabaseReference _gameEndsRef;

        // Events
        public event Action<PlacementActionData> OnPlacementActionReceived;
        public event Action<DiceRollData> OnDiceRollReceived;
        public event Action<PlayerGameData> OnPlayerGameStateReceived;
        public event Action<TurnCompletionData> OnTurnCompletionReceived;

        // Serialization settings
        private const int GRID_SIZE = 10;
        private const int MAX_PLAYERS = 8;

        // Main thread dispatch
        private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log($"SyncManager: Singleton instance set (ID: {GetInstanceID()})");
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _firebaseManager = FirebaseManager.Instance;
            _gameManager = GameManager.Instance;
            _multiplayerManager = MultiplayerManager.Instance;

            if (_gameManager == null)
            {
                Debug.LogWarning("SyncManager: GameManager not found. Some features may not work.");
            }
        }

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SyncManager: Exception in main thread action: {ex.Message}");
                }
            }
        }

        private void InvokeOnMainThread(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("SyncManager.InvokeOnMainThread: action is null");
                return;
            }
            _mainThreadActions.Enqueue(action);
        }

        private void InvokeOnMainThread<T>(Action<T> action, T arg)
        {
            if (action == null)
            {
                Debug.LogWarning($"SyncManager.InvokeOnMainThread<T>: action is null, skipping. Type: {typeof(T).Name}");
                return;
            }
            _mainThreadActions.Enqueue(() => action(arg));
        }

        private void InvokeOnMainThread<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null)
            {
                Debug.LogWarning($"SyncManager.InvokeOnMainThread<T1,T2>: action is null, skipping. Types: {typeof(T1).Name}, {typeof(T2).Name}");
                return;
            }
            _mainThreadActions.Enqueue(() => action(arg1, arg2));
        }

        public void RefreshReferences()
        {
            _firebaseManager = FirebaseManager.Instance;
            _gameManager = GameManager.Instance;
            _multiplayerManager = MultiplayerManager.Instance;

            if (_firebaseManager == null)
                Debug.LogWarning("SyncManager: FirebaseManager not found!");
            if (_gameManager == null)
                Debug.LogWarning("SyncManager: GameManager not found!");
            if (_multiplayerManager == null)
                Debug.LogWarning("SyncManager: MultiplayerManager not found!");
        }

        /// <summary>
        /// Start listening for multiplayer synchronization events for a specific lobby.
        /// </summary>
        /// <param name="lobbyCode">Lobby code to listen to</param>
        public void StartListeningForLobby(string lobbyCode)
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot start listening - Firebase not ready");
                return;
            }

            Debug.Log($"SyncManager: Starting Firebase listeners for lobby {lobbyCode}");

            // Setup single value changed listener on the lobby root
            _lobbyRootRef = _firebaseManager.DatabaseReference.Child($"games/{lobbyCode}");
            _lobbyRootRef.ValueChanged += OnLobbyValueChanged;

            // Listen for game end events
            ListenForGameEnds();

            Debug.Log("SyncManager: Firebase listeners started");
        }

        /// <summary>
        /// Stop all Firebase listeners.
        /// </summary>
        public void StopListening()
        {
            if (_lobbyRootRef != null)
            {
                _lobbyRootRef.ValueChanged -= OnLobbyValueChanged;
                _lobbyRootRef = null;
            }

            // Clear other references (kept for backward compatibility)
            _diceRollRef = null;
            _placementsRef = null;
            _gameStateRef = null;
            if (_gameEndsRef != null)
            {
                _gameEndsRef.ValueChanged -= OnGameEndValueChanged;
                _gameEndsRef = null;
            }

            Debug.Log("SyncManager: Firebase listeners stopped");
        }

        // Firebase event handlers
        private void OnLobbyValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.Snapshot == null || !args.Snapshot.Exists) return;

            string path = GetRelativePath(args.Snapshot.Reference);
            Debug.Log($"SyncManager: Value changed at path: {path}");
            // Debug: log snapshot children for diagnosis
            if (args.Snapshot.HasChildren)
            {
                Debug.Log($"SyncManager: Snapshot has {args.Snapshot.ChildrenCount} children");
                foreach (DataSnapshot child in args.Snapshot.Children)
                {
                    Debug.Log($"SyncManager:   Child: {child.Key} = {child.Value}");
                }
            }

            bool handled = false;

            // First, check for child nodes in the snapshot (when root lobby node changes)
            // Check for sharedSeed child
            if (args.Snapshot.HasChild("sharedSeed"))
            {
                Debug.Log($"SyncManager: Found sharedSeed child in root snapshot");
                DataSnapshot seedSnapshot = args.Snapshot.Child("sharedSeed");
                InvokeOnMainThread<DataSnapshot>(ProcessSharedSeed, seedSnapshot);
                handled = true;
            }

            // Check for turn/{turnNumber}/diceRoll children
            if (args.Snapshot.HasChild("turn"))
            {
                DataSnapshot turnSnapshot = args.Snapshot.Child("turn");
                foreach (DataSnapshot turnChild in turnSnapshot.Children)
                {
                    if (turnChild.HasChild("diceRoll"))
                    {
                        Debug.Log($"SyncManager: Found diceRoll child in turn {turnChild.Key}");
                        DataSnapshot diceRollSnapshot = turnChild.Child("diceRoll");
                        InvokeOnMainThread<DataSnapshot>(ProcessDiceRoll, diceRollSnapshot);
                        handled = true;
                    }

                    // Check for turn/{turnNumber}/placements/{playerId} children
                    if (turnChild.HasChild("placements"))
                    {
                        DataSnapshot placementsSnapshot = turnChild.Child("placements");
                        foreach (DataSnapshot placementChild in placementsSnapshot.Children)
                        {
                            Debug.Log($"SyncManager: Found placement child for player {placementChild.Key} in turn {turnChild.Key}");
                            InvokeOnMainThread<DataSnapshot>(ProcessPlacement, placementChild);
                            handled = true;
                        }
                    }

                    // Check for turn/{turnNumber}/completions/{playerId} children
                    if (turnChild.HasChild("completions"))
                    {
                        DataSnapshot completionsSnapshot = turnChild.Child("completions");
                        foreach (DataSnapshot completionChild in completionsSnapshot.Children)
                        {
                            Debug.Log($"SyncManager: Found completion child for player {completionChild.Key} in turn {turnChild.Key}");
                            InvokeOnMainThread<DataSnapshot>(ProcessTurnCompletion, completionChild);
                            handled = true;
                        }
                    }
                }
            }

            // Check for players/{playerId}/state children
            if (args.Snapshot.HasChild("players"))
            {
                DataSnapshot playersSnapshot = args.Snapshot.Child("players");
                foreach (DataSnapshot playerChild in playersSnapshot.Children)
                {
                    if (playerChild.HasChild("state"))
                    {
                        Debug.Log($"SyncManager: Found state child for player {playerChild.Key}");
                        DataSnapshot stateSnapshot = playerChild.Child("state");
                        InvokeOnMainThread<DataSnapshot>(ProcessPlayerGameState, stateSnapshot);
                        handled = true;
                    }
                }
            }

            // Check for gameEnds/{playerId} children (though this should be handled by separate listener)
            if (args.Snapshot.HasChild("gameEnds"))
            {
                DataSnapshot gameEndsSnapshot = args.Snapshot.Child("gameEnds");
                foreach (DataSnapshot gameEndChild in gameEndsSnapshot.Children)
                {
                    Debug.Log($"SyncManager: Found gameEnd child for player {gameEndChild.Key}");
                    InvokeOnMainThread<DataSnapshot>(ProcessGameEnd, gameEndChild);
                    handled = true;
                }
            }

            // Fallback: keep original path.Contains() checks for cases where listener might be attached to deeper node
            if (!handled)
            {
                // Check if this is a dice roll node
                if (path.Contains("/diceRoll"))
                {
                    InvokeOnMainThread<DataSnapshot>(ProcessDiceRoll, args.Snapshot);
                    handled = true;
                }
                // Check if this is a placement node
                else if (path.Contains("/placements/"))
                {
                    InvokeOnMainThread<DataSnapshot>(ProcessPlacement, args.Snapshot);
                    handled = true;
                }
                // Check if this is a turn completion node
                else if (path.Contains("/completions/"))
                {
                    InvokeOnMainThread<DataSnapshot>(ProcessTurnCompletion, args.Snapshot);
                    handled = true;
                }
                // Check if this is a player game state node
                else if (path.Contains("/players/") && path.EndsWith("/state"))
                {
                    InvokeOnMainThread<DataSnapshot>(ProcessPlayerGameState, args.Snapshot);
                    handled = true;
                }
                // Check if this is a shared seed node
                else if (path.Contains("/sharedSeed"))
                {
                    InvokeOnMainThread<DataSnapshot>(ProcessSharedSeed, args.Snapshot);
                    handled = true;
                }
                // Check if this is a game end node
                else if (path.Contains("/gameEnds/"))
                {
                    InvokeOnMainThread<DataSnapshot>(ProcessGameEnd, args.Snapshot);
                    handled = true;
                }
            }

            if (!handled)
            {
                Debug.Log($"SyncManager: No matching handler for path: {path}");
            }
        }

        private void ProcessDiceRoll(DataSnapshot snapshot)
        {
            string json = snapshot.Value as string;
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var diceRollData = DeserializeDiceRoll(json);
                Debug.Log($"SyncManager: Dice roll received for turn {diceRollData.turnNumber}");
                OnDiceRollReceived?.Invoke(diceRollData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to process dice roll data: {ex.Message}");
            }
        }

        private void ProcessPlacement(DataSnapshot snapshot)
        {
            string json = snapshot.Value as string;
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var placementData = DeserializePlacementAction(json);
                // Extract player ID from path (last segment before placement)
                string path = GetRelativePath(snapshot.Reference);
                // Path format: games/{lobbyCode}/turn/{turnNumber}/placements/{playerId}
                string[] segments = path.Split('/');
                if (segments.Length >= 2)
                {
                    string playerId = segments[segments.Length - 1]; // last segment is playerId
                    // Add playerId to placementData if needed (currently not in PlacementActionData)
                    // We could create a wrapper struct, but for now log it.
                    Debug.Log($"SyncManager: Placement action received from player {playerId}");
                }
                OnPlacementActionReceived?.Invoke(placementData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to process placement data: {ex.Message}");
            }
        }

        private void ProcessPlayerGameState(DataSnapshot snapshot)
        {
            string json = snapshot.Value as string;
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var playerGameData = DeserializePlayerGameState(json);
                Debug.Log($"SyncManager: Player game state received for {playerGameData.PlayerId}");
                OnPlayerGameStateReceived?.Invoke(playerGameData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to process player game state: {ex.Message}");
            }
        }

        private void ProcessTurnCompletion(DataSnapshot snapshot)
        {
            string json = snapshot.Value as string;
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var turnCompletionData = DeserializeTurnCompletion(json);
                Debug.Log($"SyncManager: Turn completion received for player {turnCompletionData.playerId} on turn {turnCompletionData.turnNumber}");
                Debug.Log($"SyncManager: OnTurnCompletionReceived has {OnTurnCompletionReceived?.GetInvocationList()?.Length ?? 0} subscribers");
                OnTurnCompletionReceived?.Invoke(turnCompletionData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to process turn completion data: {ex.Message}");
            }
        }

        private void ProcessSharedSeed(DataSnapshot snapshot)
        {
            object value = snapshot.Value;
            if (value == null)
            {
                Debug.LogWarning("SyncManager: ProcessSharedSeed - snapshot value is null");
                return;
            }

            long? seedValue = null;

            // Try multiple conversion methods since Firebase might store numbers as different types
            if (value is long)
            {
                seedValue = (long)value;
            }
            else if (value is int)
            {
                seedValue = (long)(int)value;
            }
            else if (value is double)
            {
                // Firebase often stores numbers as doubles
                seedValue = Convert.ToInt64((double)value);
            }
            else
            {
                // Try generic conversion
                try
                {
                    seedValue = Convert.ToInt64(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SyncManager: Failed to convert shared seed value: {value} (type: {value.GetType().Name}). Error: {ex.Message}");
                    return;
                }
            }

            if (seedValue == null)
            {
                Debug.LogError($"SyncManager: ProcessSharedSeed - could not convert value: {value} (type: {value.GetType().Name}) to long");
                return;
            }

            int seed = (int)seedValue.Value;
            Debug.Log($"SyncManager: Shared seed received: {seed} (original value: {value}, type: {value.GetType().Name})");

            // Update MultiplayerManager's shared seed
            if (_multiplayerManager != null)
            {
                _multiplayerManager.SetSharedRandomSeed(seed);
            }
            else
            {
                Debug.LogWarning("SyncManager: MultiplayerManager not available to set shared seed");
            }
        }

        private void ProcessGameEnd(DataSnapshot snapshot)
        {
            // This method is called when a game end node changes
            // The actual processing is done in OnGameEndValueChanged which listens to the parent node
            // This ensures we get all game end events, not just individual player changes
            Debug.Log($"SyncManager: Game end node changed at path: {GetRelativePath(snapshot.Reference)}");
        }

        /// <summary>
        /// Serialize the current game state for a player.
        /// </summary>
        /// <param name="playerId">ID of the player whose state to serialize</param>
        /// <returns>JSON string representing the game state</returns>
        public string SerializePlayerGameState(string playerId)
        {
            if (_gameManager == null)
            {
                Debug.LogError("SyncManager: Cannot serialize - GameManager not available");
                return "{}";
            }

            try
            {
                // Create player game data
                var playerGameData = new PlayerGameData(playerId)
                {
                    Score = GetCurrentScore(),
                    Stars = _gameManager.Stars,
                    WildcardsUsed = _gameManager.WildcardsUsed,
                    IsActive = !_gameManager.GameEnded,
                    HasEnded = _gameManager.GameEnded,
                    BoardStateJson = SerializeBoardState()
                };

                return playerGameData.ToJson();
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to serialize player game state: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// Deserialize player game state from JSON.
        /// </summary>
        /// <param name="json">JSON string containing player game state</param>
        /// <returns>Deserialized PlayerGameData object</returns>
        public PlayerGameData DeserializePlayerGameState(string json)
        {
            try
            {
                return PlayerGameData.FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to deserialize player game state: {ex.Message}");
                return new PlayerGameData();
            }
        }

        /// <summary>
        /// Serialize a shape placement action for broadcasting.
        /// </summary>
        /// <param name="shapeType">Type of shape placed</param>
        /// <param name="buildingType">Type of building placed</param>
        /// <param name="positionX">X grid coordinate</param>
        /// <param name="positionY">Y grid coordinate</param>
        /// <param name="rotation">Rotation state (0-3)</param>
        /// <param name="flipped">Whether shape is flipped</param>
        /// <param name="starsAwarded">Number of stars awarded for this placement</param>
        /// <returns>JSON string representing the placement action</returns>
        public string SerializePlacementAction(string shapeType, string buildingType, int positionX, int positionY, int rotation, bool flipped, int starsAwarded)
        {
            var actionData = new PlacementActionData
            {
                shapeType = shapeType,
                buildingType = buildingType,
                positionX = positionX,
                positionY = positionY,
                rotation = rotation,
                flipped = flipped,
                starsAwarded = starsAwarded,
                timestamp = GetCurrentTimestamp()
            };

            return JsonUtility.ToJson(actionData);
        }

        /// <summary>
        /// Deserialize a placement action from JSON.
        /// </summary>
        public PlacementActionData DeserializePlacementAction(string json)
        {
            try
            {
                return JsonUtility.FromJson<PlacementActionData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to deserialize placement action: {ex.Message}");
                return new PlacementActionData();
            }
        }

        /// <summary>
        /// Serialize dice roll results for synchronization.
        /// </summary>
        /// <param name="shapeDiceFaces">Array of shape dice faces (0-5)</param>
        /// <param name="buildingDiceFaces">Array of building dice faces (0-5)</param>
        /// <param name="sharedRandomSeed">Shared random seed for deterministic rolls</param>
        /// <param name="turnNumber">Current turn number</param>
        /// <returns>JSON string representing dice roll data</returns>
        public string SerializeDiceRoll(int[] shapeDiceFaces, int[] buildingDiceFaces, int sharedRandomSeed, int turnNumber)
        {
            var diceData = new DiceRollData
            {
                shapeDiceFaces = shapeDiceFaces,
                buildingDiceFaces = buildingDiceFaces,
                sharedRandomSeed = sharedRandomSeed,
                turnNumber = turnNumber,
                doubleFaces = DetectDoubleFaces(shapeDiceFaces, buildingDiceFaces),
                timestamp = GetCurrentTimestamp()
            };

            return JsonUtility.ToJson(diceData);
        }

        /// <summary>
        /// Deserialize dice roll data from JSON.
        /// </summary>
        public DiceRollData DeserializeDiceRoll(string json)
        {
            try
            {
                return JsonUtility.FromJson<DiceRollData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to deserialize dice roll: {ex.Message}");
                return new DiceRollData();
            }
        }

        /// <summary>
        /// Serialize turn completion data for broadcasting.
        /// </summary>
        /// <param name="playerId">ID of the player who completed the turn</param>
        /// <param name="turnNumber">Turn number that was completed</param>
        /// <param name="gameEnded">Whether the player ended their game this turn (cannot continue)</param>
        /// <returns>JSON string representing turn completion data</returns>
        public string SerializeTurnCompletion(string playerId, int turnNumber, bool gameEnded)
        {
            var turnCompletionData = new TurnCompletionData
            {
                playerId = playerId,
                turnNumber = turnNumber,
                timestamp = GetCurrentTimestamp(),
                gameEnded = gameEnded
            };

            return JsonUtility.ToJson(turnCompletionData);
        }

        /// <summary>
        /// Deserialize turn completion data from JSON.
        /// </summary>
        public TurnCompletionData DeserializeTurnCompletion(string json)
        {
            try
            {
                return JsonUtility.FromJson<TurnCompletionData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to deserialize turn completion: {ex.Message}");
                return new TurnCompletionData();
            }
        }

        /// <summary>
        /// Compress board state for efficient transmission.
        /// </summary>
        private string SerializeBoardState()
        {
            var boardState = new BoardStateData
            {
                turnNumber = _gameManager?.CurrentTurn ?? 0,
                shapes = new List<BoardShapeData>(),
                compressionVersion = 1,
                serializedShapes = "" // Will be populated with JSON for compatibility
            };

            // Get all placed shapes from the board
            TilemapManager tilemapManager = TilemapManager.Instance;
            if (tilemapManager == null)
            {
                Debug.LogWarning("SyncManager: TilemapManager not found, cannot serialize board state");
                return JsonUtility.ToJson(boardState);
            }

            // Use HashSet to collect unique shapes (a shape may occupy multiple tiles)
            HashSet<ShapeController> uniqueShapes = new HashSet<ShapeController>();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    GridTile tile = tilemapManager.gridTiles[x, y];
                    if (tile != null && tile.occupyingShape != null && !uniqueShapes.Contains(tile.occupyingShape))
                    {
                        uniqueShapes.Add(tile.occupyingShape);
                    }
                }
            }

            // Convert each shape to serializable data
            foreach (ShapeController shape in uniqueShapes)
            {
                // Ensure shape data is valid
                if (shape.shapeData == null) continue;

                var shapeData = new BoardShapeData
                {
                    shapeType = shape.shapeData.shapeName.ToString(),
                    buildingType = shape.buildingType.ToString(),
                    positionX = shape.position.x,
                    positionY = shape.position.y,
                    rotation = shape.RotationState,
                    flipped = shape.IsFlipped,
                    turnPlaced = 0 // TODO: Track turn placed if needed
                };
                boardState.shapes.Add(shapeData);
            }

            // For compatibility, serialize shapes list to JSON string
            if (boardState.shapes.Count > 0)
            {
                boardState.serializedShapes = JsonUtility.ToJson(boardState.shapes);
            }
            else
            {
                boardState.serializedShapes = "[]";
            }

            return JsonUtility.ToJson(boardState);
        }

        /// <summary>
        /// Get the current player's total score.
        /// </summary>
        private int GetCurrentScore()
        {
            if (_gameManager == null || _gameManager.ScoreManager == null)
            {
                return 0;
            }

            try
            {
                var scoreComponents = _gameManager.ScoreManager.CalculateScore();
                return scoreComponents.totalScore;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Detect double faces in dice rolls for star opportunities.
        /// </summary>
        private DoubleFacesData DetectDoubleFaces(int[] shapeDiceFaces, int[] buildingDiceFaces)
        {
            var doubleFaces = new DoubleFacesData();

            // Count shape dice faces
            var shapeCounts = new Dictionary<int, int>();
            foreach (var face in shapeDiceFaces)
            {
                shapeCounts[face] = shapeCounts.ContainsKey(face) ? shapeCounts[face] + 1 : 1;
            }

            // Count building dice faces
            var buildingCounts = new Dictionary<int, int>();
            foreach (var face in buildingDiceFaces)
            {
                buildingCounts[face] = buildingCounts.ContainsKey(face) ? buildingCounts[face] + 1 : 1;
            }

            // Find doubles (exactly 2 occurrences)
            doubleFaces.shapeFaces = new List<int>();
            doubleFaces.buildingFaces = new List<int>();

            foreach (var kvp in shapeCounts)
            {
                if (kvp.Value == 2)
                {
                    doubleFaces.shapeFaces.Add(kvp.Key);
                }
            }

            foreach (var kvp in buildingCounts)
            {
                if (kvp.Value == 2)
                {
                    doubleFaces.buildingFaces.Add(kvp.Key);
                }
            }

            return doubleFaces;
        }

        /// <summary>
        /// Get current Unix timestamp in milliseconds.
        /// </summary>
        private long GetCurrentTimestamp()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }

        /// <summary>
        /// Get the size of a serialized string in bytes (approximate).
        /// </summary>
        public int GetSerializedSize(string json)
        {
            return System.Text.Encoding.UTF8.GetByteCount(json);
        }

        /// <summary>
        /// Log serialization statistics for optimization.
        /// </summary>
        public void LogSerializationStats(string json, string dataType)
        {
            int size = GetSerializedSize(json);
            Debug.Log($"SyncManager: {dataType} serialized to {size} bytes ({json.Length} chars)");
        }

        /// <summary>
        /// Get the relative path of a DatabaseReference (path relative to database root).
        /// </summary>
        private string GetRelativePath(DatabaseReference reference)
        {
            if (reference == null) return string.Empty;

            // Try to get Path property via reflection (in case SDK version differs)
            var pathProperty = reference.GetType().GetProperty("Path");
            if (pathProperty != null)
            {
                var pathValue = pathProperty.GetValue(reference) as string;
                if (!string.IsNullOrEmpty(pathValue))
                {
                    return pathValue.TrimStart('/');
                }
            }

            // Get the full URL of this reference
            string fullUrl = reference.ToString();

            // Get the root URL from FirebaseManager
            string rootUrl = _firebaseManager?.DatabaseRootPath ?? string.Empty;
            if (string.IsNullOrEmpty(rootUrl))
            {
                // Try to get root reference URL
                var rootRef = _firebaseManager?.DatabaseReference;
                if (rootRef != null)
                {
                    rootUrl = rootRef.ToString();
                }
            }

            // Remove root URL prefix to get relative path
            if (!string.IsNullOrEmpty(rootUrl) && fullUrl.StartsWith(rootUrl))
            {
                return fullUrl.Substring(rootUrl.Length).TrimStart('/');
            }

            // Fallback: return full URL (will still work for contains checks)
            return fullUrl;
        }

        /// <summary>
        /// Generate deterministic dice faces for multiplayer synchronization.
        /// Uses the same algorithm as DicePool.RollDeterministic to ensure consistency across clients.
        /// </summary>
        public static (int[] shapeFaces, int[] buildingFaces) GenerateDeterministicDiceFaces(int seed, int turn)
        {
            // Combine seed and turn to create a unique random sequence for this turn
            int combinedSeed = seed + turn * 1000;
            var rng = new System.Random(combinedSeed);

            // Generate initial faces
            int[] shapeFaces = new int[3];
            int[] buildingFaces = new int[3];
            for (int i = 0; i < 3; i++)
            {
                shapeFaces[i] = rng.Next(0, 6);
                buildingFaces[i] = rng.Next(0, 6);
            }

            // Apply deterministic auto-rerolls for triples
            ApplyDeterministicAutoRerolls(rng, shapeFaces, buildingFaces);

            return (shapeFaces, buildingFaces);
        }

        /// <summary>
        /// Apply deterministic auto-rerolls for triples (same logic as DicePool).
        /// </summary>
        private static void ApplyDeterministicAutoRerolls(System.Random rng, int[] shapeFaces, int[] buildingFaces)
        {
            bool rerolled;
            do
            {
                rerolled = false;

                // Check shape dice for triples
                var shapeTriples = GetTripleFaces(shapeFaces);
                if (shapeTriples.Count > 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        shapeFaces[i] = rng.Next(0, 6);
                    }
                    rerolled = true;
                }

                // Check building dice for triples
                var buildingTriples = GetTripleFaces(buildingFaces);
                if (buildingTriples.Count > 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        buildingFaces[i] = rng.Next(0, 6);
                    }
                    rerolled = true;
                }

                // Continue loop if any triples were found (recursive reroll)
            } while (rerolled);
        }

        /// <summary>
        /// Returns list of face indices that appear exactly three times in the given dice faces array.
        /// </summary>
        private static List<int> GetTripleFaces(int[] faces)
        {
            var faceCounts = new Dictionary<int, int>();
            foreach (var face in faces)
            {
                if (!faceCounts.ContainsKey(face))
                    faceCounts[face] = 0;
                faceCounts[face]++;
            }

            return faceCounts.Where(kvp => kvp.Value == 3).Select(kvp => kvp.Key).ToList();
        }

        // === Broadcast Methods ===

        /// <summary>
        /// Broadcast a placement action to all players in the lobby.
        /// </summary>
        public async Task BroadcastPlacementAction(string shapeType, string buildingType, int positionX, int positionY, int rotation, bool flipped, int starsAwarded)
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot broadcast placement - Firebase not ready");
                return;
            }

            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode))
            {
                Debug.LogError("SyncManager: Cannot broadcast placement - no active lobby");
                return;
            }

            string playerId = _multiplayerManager.LocalPlayerId;
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("SyncManager: Cannot broadcast placement - no local player ID");
                return;
            }

            // Serialize the action
            string json = SerializePlacementAction(shapeType, buildingType, positionX, positionY, rotation, flipped, starsAwarded);
            LogSerializationStats(json, "PlacementAction");

            // Determine Firebase path
            int turnNumber = _gameManager != null ? _gameManager.CurrentTurn : 0;
            string path = $"games/{_multiplayerManager.LobbyCode}/turn/{turnNumber}/placements/{playerId}";

            try
            {
                await _firebaseManager.DatabaseReference.Child(path).SetValueAsync(json);
                Debug.Log($"SyncManager: Placement action broadcast to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to broadcast placement action: {ex.Message}");
            }
        }

        /// <summary>
        /// Broadcast dice roll results to all players in the lobby.
        /// </summary>
        public async Task BroadcastDiceRoll(int[] shapeDiceFaces, int[] buildingDiceFaces, int sharedRandomSeed, int turnNumber)
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot broadcast dice roll - Firebase not ready");
                return;
            }

            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode))
            {
                Debug.LogError("SyncManager: Cannot broadcast dice roll - no active lobby");
                return;
            }

            // Serialize the dice roll
            string json = SerializeDiceRoll(shapeDiceFaces, buildingDiceFaces, sharedRandomSeed, turnNumber);
            LogSerializationStats(json, "DiceRoll");

            // Determine Firebase path
            string path = $"games/{_multiplayerManager.LobbyCode}/turn/{turnNumber}/diceRoll";

            try
            {
                await _firebaseManager.DatabaseReference.Child(path).SetValueAsync(json);
                Debug.Log($"SyncManager: Dice roll broadcast to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to broadcast dice roll: {ex.Message}");
            }
        }

        /// <summary>
        /// Broadcast shared random seed to all players in the lobby.
        /// Called by host when starting the game.
        /// </summary>
        public async Task BroadcastSharedSeed(int sharedRandomSeed)
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot broadcast shared seed - Firebase not ready");
                return;
            }

            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode))
            {
                Debug.LogError("SyncManager: Cannot broadcast shared seed - no active lobby");
                return;
            }

            // Determine Firebase path
            string path = $"games/{_multiplayerManager.LobbyCode}/sharedSeed";

            try
            {
                await _firebaseManager.DatabaseReference.Child(path).SetValueAsync(sharedRandomSeed);
                Debug.Log($"SyncManager: Shared seed broadcast to {path}: {sharedRandomSeed}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to broadcast shared seed: {ex.Message}");
            }
        }

        /// <summary>
        /// Broadcast the current game state for this player.
        /// </summary>
        public async Task BroadcastGameState()
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot broadcast game state - Firebase not ready");
                return;
            }

            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode))
            {
                Debug.LogError("SyncManager: Cannot broadcast game state - no active lobby");
                return;
            }

            string playerId = _multiplayerManager.LocalPlayerId;
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("SyncManager: Cannot broadcast game state - no local player ID");
                return;
            }

            // Serialize the game state
            string json = SerializePlayerGameState(playerId);
            LogSerializationStats(json, "PlayerGameState");

            // Determine Firebase path
            string path = $"games/{_multiplayerManager.LobbyCode}/players/{playerId}/state";

            try
            {
                await _firebaseManager.DatabaseReference.Child(path).SetValueAsync(json);
                Debug.Log($"SyncManager: Game state broadcast to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to broadcast game state: {ex.Message}");
            }
        }

        /// <summary>
        /// Broadcast game end to all players in the lobby.
        /// </summary>
        public async Task BroadcastGameEnd()
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot broadcast game end - Firebase not ready");
                return;
            }

            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode))
            {
                Debug.LogError("SyncManager: Cannot broadcast game end - not in multiplayer lobby");
                return;
            }

            string playerId = _multiplayerManager.LocalPlayerId;
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("SyncManager: Cannot broadcast game end - no local player ID");
                return;
            }

            try
            {
                var gameEndData = new Dictionary<string, object>
                {
                    { "playerId", playerId },
                    { "timestamp", DateTime.UtcNow.Ticks },
                    { "turn", _gameManager?.CurrentTurn ?? 0 }
                };

                await _firebaseManager.DatabaseReference
                    .Child($"games/{_multiplayerManager.LobbyCode}/gameEnds/{playerId}")
                    .SetValueAsync(gameEndData);

                Debug.Log("SyncManager: Game end broadcasted");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to broadcast game end: {ex.Message}");
            }
        }

        /// <summary>
        /// Broadcast turn completion to all players in the lobby.
        /// </summary>
        /// <param name="turnNumber">Turn number that was completed</param>
        /// <param name="gameEnded">Whether the player ended their game this turn (cannot continue)</param>
        public async Task BroadcastTurnCompletion(int turnNumber, bool gameEnded)
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady())
            {
                Debug.LogError("SyncManager: Cannot broadcast turn completion - Firebase not ready");
                return;
            }

            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode))
            {
                Debug.LogError("SyncManager: Cannot broadcast turn completion - no active lobby");
                return;
            }

            string playerId = _multiplayerManager.LocalPlayerId;
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("SyncManager: Cannot broadcast turn completion - no local player ID");
                return;
            }

            // Serialize the turn completion data
            string json = SerializeTurnCompletion(playerId, turnNumber, gameEnded);
            LogSerializationStats(json, "TurnCompletion");

            // Determine Firebase path
            string path = $"games/{_multiplayerManager.LobbyCode}/turn/{turnNumber}/completions/{playerId}";

            try
            {
                await _firebaseManager.DatabaseReference.Child(path).SetValueAsync(json);
                Debug.Log($"SyncManager: Turn completion broadcast to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SyncManager: Failed to broadcast turn completion: {ex.Message}");
            }
        }

        /// <summary>
        /// Start listening for game end events from other players.
        /// </summary>
        public void ListenForGameEnds()
        {
            if (_firebaseManager == null || !_firebaseManager.IsReady()) return;
            if (_multiplayerManager == null || string.IsNullOrEmpty(_multiplayerManager.LobbyCode)) return;

            // Remove existing listener if any
            if (_gameEndsRef != null)
            {
                _gameEndsRef.ValueChanged -= OnGameEndValueChanged;
                _gameEndsRef = null;
            }

            // Listen for game end events from all players
            _gameEndsRef = _firebaseManager.DatabaseReference
                .Child($"games/{_multiplayerManager.LobbyCode}/gameEnds");
            _gameEndsRef.ValueChanged += OnGameEndValueChanged;

            Debug.Log("SyncManager: Listening for game end events");
        }

        private void OnGameEndValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"SyncManager: Error listening for game ends: {args.DatabaseError.Message}");
                return;
            }

            if (args.Snapshot == null || !args.Snapshot.Exists) return;

            // For each player who ended game - dispatch to main thread
            InvokeOnMainThread(() =>
            {
                foreach (DataSnapshot playerSnapshot in args.Snapshot.Children)
                {
                    string playerId = playerSnapshot.Key;
                    var data = playerSnapshot.Value as Dictionary<string, object>;
                    if (data != null && data.ContainsKey("playerId"))
                    {
                        // Notify MultiplayerManager
                        _multiplayerManager?.OnRemoteGameEnded(playerId);
                    }
                }
            });
        }

    }

    // Data structures for serialization

    [Serializable]
    public class PlacementActionData
    {
        public string shapeType;
        public string buildingType;
        public int positionX;
        public int positionY;
        public int rotation;
        public bool flipped;
        public int starsAwarded;
        public long timestamp;
    }

    [Serializable]
    public class TurnCompletionData
    {
        public string playerId;
        public int turnNumber;
        public long timestamp;
        public bool gameEnded; // true if player ended game this turn (cannot continue)
    }

    [Serializable]
    public class DiceRollData
    {
        public int[] shapeDiceFaces;
        public int[] buildingDiceFaces;
        public int sharedRandomSeed;
        public int turnNumber;
        public DoubleFacesData doubleFaces;
        public long timestamp;
    }

    [Serializable]
    public class DoubleFacesData
    {
        public List<int> shapeFaces;
        public List<int> buildingFaces;
    }

    [Serializable]
    public class BoardStateData
    {
        public int turnNumber;
        public List<BoardShapeData> shapes; // List of placed shapes
        public int compressionVersion;

        // Legacy field for compatibility, will contain JSON of shapes if needed
        public string serializedShapes;
    }

    [Serializable]
    public class GameStateData
    {
        public string lobbyCode;
        public int currentTurn;
        public bool gameStarted;
        public bool gameEnded;
        public Dictionary<string, PlayerGameData> players;
        public DiceRollData currentDiceRoll;
        public List<PlacementActionData> turnHistory;
    }

    [Serializable]
    public class BoardShapeData
    {
        public string shapeType; // ShapeType enum as string
        public string buildingType; // BuildingType enum as string
        public int positionX;
        public int positionY;
        public int rotation; // 0-3
        public bool flipped;
        public int turnPlaced; // optional
    }
}
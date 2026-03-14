using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Handles serialization and deserialization of game state for Firebase synchronization.
    /// Compresses board data and manages efficient state transmission.
    /// </summary>
    public class SyncManager : MonoBehaviour
    {
        // References
        private FirebaseManager _firebaseManager;
        private GameManager _gameManager;

        // Serialization settings
        private const int GRID_SIZE = 10;
        private const int MAX_PLAYERS = 8;

        private void Start()
        {
            _firebaseManager = FirebaseManager.Instance;
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                Debug.LogWarning("SyncManager: GameManager not found. Some features may not work.");
            }
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
        /// Compress board state for efficient transmission.
        /// </summary>
        private string SerializeBoardState()
        {
            // TODO: Implement actual board state serialization
            // This should capture all placed shapes, their positions, rotations, etc.
            // For now, return a placeholder

            var boardState = new BoardStateData
            {
                turnNumber = _gameManager?.CurrentTurn ?? 0,
                serializedShapes = "[]", // Placeholder
                compressionVersion = 1
            };

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
        public string serializedShapes;
        public int compressionVersion;
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
}
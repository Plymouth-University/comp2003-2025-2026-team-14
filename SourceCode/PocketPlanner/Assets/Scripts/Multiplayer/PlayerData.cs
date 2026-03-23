using System;
using UnityEngine;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Serializable player data structure for Firebase storage.
    /// Represents a player in a lobby or game.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [SerializeField] private string playerId;
        [SerializeField] private string displayName;
        [SerializeField] private bool isReady;
        [SerializeField] private bool isHost;
        [SerializeField] private long joinedAt; // Unix timestamp in seconds
        [SerializeField] private string deviceId; // For reconnection tracking

        // Default constructor for serialization
        public PlayerData()
        {
            playerId = string.Empty;
            displayName = "Player";
            isReady = false;
            isHost = false;
            joinedAt = GetCurrentTimestamp();
            deviceId = string.Empty; // Will be set later if needed
        }

        /// <summary>
        /// Constructor for creating a new player.
        /// </summary>
        public PlayerData(string playerId, string displayName, bool isHost = false, string deviceId = "")
        {
            this.playerId = playerId;
            this.displayName = !string.IsNullOrEmpty(displayName) ? displayName : "Player";
            this.isReady = false;
            this.isHost = isHost;
            this.joinedAt = GetCurrentTimestamp();
            this.deviceId = !string.IsNullOrEmpty(deviceId) ? deviceId : string.Empty;
        }

        // Properties with getters and setters
        public string PlayerId
        {
            get => playerId;
            set => playerId = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = !string.IsNullOrEmpty(value) ? value : "Player";
        }

        public bool IsReady
        {
            get => isReady;
            set => isReady = value;
        }

        public bool IsHost
        {
            get => isHost;
            set => isHost = value;
        }

        public long JoinedAt
        {
            get => joinedAt;
            set => joinedAt = value;
        }

        public string DeviceId
        {
            get => deviceId;
            set => deviceId = !string.IsNullOrEmpty(value) ? value : string.Empty;
        }

        /// <summary>
        /// Get the current Unix timestamp in seconds.
        /// </summary>
        private static long GetCurrentTimestamp()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        /// <summary>
        /// Convert this PlayerData to a JSON string for Firebase storage.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Create a PlayerData object from a JSON string.
        /// </summary>
        public static PlayerData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<PlayerData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlayerData.FromJson failed: {ex.Message}");
                return new PlayerData();
            }
        }

        /// <summary>
        /// Create a deep copy of this PlayerData object.
        /// </summary>
        public PlayerData Clone()
        {
            return new PlayerData
            {
                playerId = playerId,
                displayName = displayName,
                isReady = isReady,
                isHost = isHost,
                joinedAt = joinedAt,
                deviceId = deviceId
            };
        }

        /// <summary>
        /// Update this PlayerData with values from another PlayerData object.
        /// </summary>
        public void UpdateFrom(PlayerData other)
        {
            if (other == null) return;

            // Don't update playerId - it should remain constant
            displayName = other.displayName;
            isReady = other.isReady;
            isHost = other.isHost;
            joinedAt = other.joinedAt;
            deviceId = other.deviceId;
        }

        /// <summary>
        /// Check if this player data is valid (has required fields).
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(displayName);
        }

        /// <summary>
        /// Get a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"{displayName} ({playerId}) - Host: {isHost}, Ready: {isReady}, Joined: {joinedAt}";
        }
    }

    /// <summary>
    /// Serializable data structure for a player's game state.
    /// Contains board state, score, and game progress information.
    /// </summary>
    [Serializable]
    public class PlayerGameData
    {
        [SerializeField] private string playerId;
        [SerializeField] private int score;
        [SerializeField] private int stars;
        [SerializeField] private int wildcardsUsed;
        [SerializeField] private bool isActive; // Player is still in the game
        [SerializeField] private bool hasEnded; // Player has ended their game
        [SerializeField] private string boardStateJson; // Serialized board representation

        public PlayerGameData()
        {
            playerId = string.Empty;
            score = 0;
            stars = 0;
            wildcardsUsed = 0;
            isActive = true;
            hasEnded = false;
            boardStateJson = string.Empty;
        }

        public PlayerGameData(string playerId)
        {
            this.playerId = playerId;
            score = 0;
            stars = 0;
            wildcardsUsed = 0;
            isActive = true;
            hasEnded = false;
            boardStateJson = string.Empty;
        }

        // Properties
        public string PlayerId
        {
            get => playerId;
            set => playerId = value;
        }

        public int Score
        {
            get => score;
            set => score = value;
        }

        public int Stars
        {
            get => stars;
            set => stars = value;
        }

        public int WildcardsUsed
        {
            get => wildcardsUsed;
            set => wildcardsUsed = value;
        }

        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public bool HasEnded
        {
            get => hasEnded;
            set => hasEnded = value;
        }

        public string BoardStateJson
        {
            get => boardStateJson;
            set => boardStateJson = value;
        }

        /// <summary>
        /// Convert to JSON for Firebase storage.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Create from JSON string.
        /// </summary>
        public static PlayerGameData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<PlayerGameData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlayerGameData.FromJson failed: {ex.Message}");
                return new PlayerGameData();
            }
        }
    }
}
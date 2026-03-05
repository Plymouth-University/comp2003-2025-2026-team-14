namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Constants for multiplayer networking configuration.
    /// </summary>
    public static class NetworkConstants
    {
        // Firebase paths
        public const string FIREBASE_LOBBIES_PATH = "lobbies";
        public const string FIREBASE_GAMES_PATH = "games";
        public const string FIREBASE_PLAYERS_PATH = "players";
        public const string FIREBASE_TURN_HISTORY_PATH = "turnHistory";

        // Lobby configuration
        public const int LOBBY_CODE_LENGTH = 6;
        public const int MAX_PLAYERS_PER_LOBBY = 8;
        public const int LOBBY_TIMEOUT_SECONDS = 3600; // 1 hour

        // Game synchronization
        public const int MAX_TURN_HISTORY = 50; // Keep last 50 turns for reconnection
        public const int SYNC_RETRY_COUNT = 3;
        public const float SYNC_RETRY_DELAY = 2.0f;

        // Data compression
        public const int BOARD_STATE_COMPRESSION_VERSION = 1;
        public const int MAX_SERIALIZED_SIZE_BYTES = 1024 * 10; // 10KB per player state

        // Reconnection
        public const int RECONNECTION_TIMEOUT_SECONDS = 30;
        public const int MAX_RECONNECTION_ATTEMPTS = 5;

        // Error codes
        public const string ERROR_LOBBY_NOT_FOUND = "LOBBY_NOT_FOUND";
        public const string ERROR_LOBBY_FULL = "LOBBY_FULL";
        public const string ERROR_PLAYER_NOT_IN_LOBBY = "PLAYER_NOT_IN_LOBBY";
        public const string ERROR_HOST_ONLY = "HOST_ONLY";
        public const string ERROR_GAME_ALREADY_STARTED = "GAME_ALREADY_STARTED";
        public const string ERROR_FIREBASE_NOT_READY = "FIREBASE_NOT_READY";

        // Firebase security rule constants
        public const string AUTH_UID = "auth.uid";
        public const string DATA_EXISTS = "data.exists()";
        public const string NEW_DATA_EXISTS = "newData.exists()";

        /// <summary>
        /// Get the full Firebase path for a lobby.
        /// </summary>
        public static string GetLobbyPath(string lobbyCode)
        {
            return $"{FIREBASE_LOBBIES_PATH}/{lobbyCode}";
        }

        /// <summary>
        /// Get the full Firebase path for a game.
        /// </summary>
        public static string GetGamePath(string lobbyCode)
        {
            return $"{FIREBASE_GAMES_PATH}/{lobbyCode}";
        }

        /// <summary>
        /// Get the Firebase path for players in a lobby.
        /// </summary>
        public static string GetLobbyPlayersPath(string lobbyCode)
        {
            return $"{GetLobbyPath(lobbyCode)}/{FIREBASE_PLAYERS_PATH}";
        }

        /// <summary>
        /// Get the Firebase path for a specific player in a lobby.
        /// </summary>
        public static string GetPlayerPath(string lobbyCode, string playerId)
        {
            return $"{GetLobbyPlayersPath(lobbyCode)}/{playerId}";
        }

        /// <summary>
        /// Validate a lobby code format.
        /// </summary>
        public static bool IsValidLobbyCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length != LOBBY_CODE_LENGTH)
            {
                return false;
            }

            // Check for ambiguous characters (no I, L, O, 0, 1)
            foreach (char c in code)
            {
                if (c == 'I' || c == 'L' || c == 'O' || c == '0' || c == '1')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Generate a default display name based on player index.
        /// </summary>
        public static string GetDefaultDisplayName(int playerIndex)
        {
            return $"Player {playerIndex + 1}";
        }
    }
}
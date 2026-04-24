using System;
using System.Collections.Generic;
using UnityEngine;
using PocketPlanner.Multiplayer;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Manages spectator mode for viewing other players' boards in multiplayer games.
    /// Handles switching between local player view and opponent views, caching board states,
    /// and coordinating with other managers for input disabling and UI updates.
    /// </summary>
    public class SpectatorManager : MonoBehaviour
    {
        // Singleton instance
        public static SpectatorManager Instance { get; private set; }

        // Spectator state
        public bool IsSpectating { get; private set; }
        public string CurrentSpectatedPlayerId { get; private set; }
        public string LocalPlayerId { get; private set; }

        // Backup of local player's shapes when spectating
        private List<BoardShapeData> localPlayerShapesBackup;

        // Cache of opponent board states (playerId -> list of BoardShapeData)
        private Dictionary<string, List<BoardShapeData>> cachedOpponentBoards;

        // List of player IDs in the game (including local player)
        private List<string> playerIds;
        private int currentSpectatedIndex = -1; // -1 = local player, 0+ = opponent index

        // References to other managers
        private SyncManager syncManager;
        private GameManager gameManager;
        private MultiplayerManager multiplayerManager;

        // Events
        public event Action<bool> OnSpectatorModeChanged;
        public event Action<string> OnSpectatedPlayerChanged;

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

            localPlayerShapesBackup = new List<BoardShapeData>();
            cachedOpponentBoards = new Dictionary<string, List<BoardShapeData>>();
            playerIds = new List<string>();
        }

        private void Start()
        {
            syncManager = SyncManager.Instance;
            gameManager = GameManager.Instance;
            multiplayerManager = MultiplayerManager.Instance;

            if (multiplayerManager != null)
            {
                LocalPlayerId = multiplayerManager.LocalPlayerId;
            }
        }

        /// <summary>
        /// Initialize spectator manager with local player ID and list of all player IDs in the game.
        /// Should be called when multiplayer game starts or when the game scene loads in multiplayer mode.
        /// Only opponent IDs (non-local) are added to the cycle list.
        /// </summary>
        public void Initialize(string localPlayerId, List<string> allPlayerIdsInGame)
        {
            LocalPlayerId = localPlayerId;
            playerIds.Clear();
            foreach (string id in allPlayerIdsInGame)
            {
                if (id != LocalPlayerId)
                {
                    playerIds.Add(id);
                }
            }
            cachedOpponentBoards.Clear();
            localPlayerShapesBackup.Clear();
            CurrentSpectatedPlayerId = LocalPlayerId;
            currentSpectatedIndex = -1;
            IsSpectating = false;

            Debug.Log($"SpectatorManager initialized with {playerIds.Count} opponents, local ID: {LocalPlayerId}");
        }

        /// <summary>
        /// Switch to spectating the next player in the player list.
        /// If currently viewing local player, start spectating first opponent.
        /// If currently spectating last opponent, cycle back to local player.
        /// </summary>
        public void CycleToNextPlayer()
        {
            if (playerIds.Count < 1)
            {
                Debug.LogWarning("SpectatorManager: No opponents to cycle through.");
                return;
            }

            // Determine next index
            int nextIndex = currentSpectatedIndex + 1;
            if (nextIndex >= playerIds.Count)
            {
                // Loop back to local player (index -1)
                nextIndex = -1;
            }

            SwitchToPlayerIndex(nextIndex);
        }

        /// <summary>
        /// Switch to spectating a specific player by ID.
        /// If playerId is local player ID, switch back to local view.
        /// </summary>
        public void SwitchToPlayer(string playerId)
        {
            if (playerId == LocalPlayerId)
            {
                SwitchToPlayerIndex(-1);
                return;
            }

            int index = playerIds.IndexOf(playerId);
            if (index < 0)
            {
                Debug.LogError($"SpectatorManager: Player ID {playerId} not found in player list.");
                return;
            }

            SwitchToPlayerIndex(index);
        }

        /// <summary>
        /// Core method to switch view to player at specified index.
        /// -1 = local player, 0+ = opponent index in playerIds list.
        /// </summary>
        private void SwitchToPlayerIndex(int targetIndex)
        {
            // If already viewing this player, do nothing
            if (currentSpectatedIndex == targetIndex)
                return;

            Debug.Log($"SpectatorManager: Switching from index {currentSpectatedIndex} to {targetIndex}");

            // Backup local player's board if switching from local to opponent
            if (currentSpectatedIndex == -1 && targetIndex >= 0)
            {
                BackupLocalPlayerBoard();
            }

            // Clear current board
            if (ShapeManager.Instance != null)
            {
                ShapeManager.Instance.ClearAllShapes();
            }
            else
            {
                Debug.LogWarning("SpectatorManager: ShapeManager not available, cannot clear board");
            }

            // Update state
            currentSpectatedIndex = targetIndex;
            bool wasSpectating = IsSpectating;
            IsSpectating = targetIndex >= 0;

            if (IsSpectating)
            {
                // Spectating opponent
                CurrentSpectatedPlayerId = playerIds[targetIndex];
                LoadOpponentBoard(CurrentSpectatedPlayerId);
            }
            else
            {
                // Switching back to local player
                CurrentSpectatedPlayerId = LocalPlayerId;
                RestoreLocalPlayerBoard();
            }

            // Notify GameManager to disable/enable input
            if (gameManager != null)
            {
                gameManager.SetSpectatorMode(IsSpectating);
                gameManager.SetSpectatedPlayer(CurrentSpectatedPlayerId);
            }

            // Fire events
            OnSpectatorModeChanged?.Invoke(IsSpectating);
            OnSpectatedPlayerChanged?.Invoke(CurrentSpectatedPlayerId);

            Debug.Log($"SpectatorManager: Now viewing player {CurrentSpectatedPlayerId}, spectating={IsSpectating}");
        }

        /// <summary>
        /// Load opponent's board — shows cached version immediately if available,
        /// then always fetches fresh data from Firebase to keep the display up to date.
        /// </summary>
        private void LoadOpponentBoard(string opponentId)
        {
            if (ShapeManager.Instance == null)
            {
                Debug.LogError("SpectatorManager: ShapeManager not available, cannot load board");
                return;
            }

            // Show cached version immediately for fast response
            if (cachedOpponentBoards.TryGetValue(opponentId, out List<BoardShapeData> cachedBoard))
            {
                ShapeManager.Instance.PlaceShapesFromBoardState(cachedBoard);
                Debug.Log($"SpectatorManager: Loaded cached board for {opponentId} ({cachedBoard.Count} shapes) — will refresh from Firebase");
            }

            // Always fetch from Firebase to get the latest board state
            Debug.Log($"SpectatorManager: Fetching fresh board state for {opponentId} from Firebase...");
            syncManager.FetchPlayerBoardState(opponentId, (boardState) =>
            {
                if (boardState != null)
                {
                    cachedOpponentBoards[opponentId] = boardState;

                    // Only update display if we're still viewing this player
                    if (CurrentSpectatedPlayerId == opponentId && ShapeManager.Instance != null)
                    {
                        ShapeManager.Instance.ClearAllShapes();
                        ShapeManager.Instance.PlaceShapesFromBoardState(boardState);
                    }
                    Debug.Log($"SpectatorManager: Refreshed board for {opponentId} from Firebase ({boardState.Count} shapes)");
                }
                else
                {
                    Debug.LogWarning($"SpectatorManager: Failed to fetch fresh board state for {opponentId} — using cached data if available");
                }
            });
        }

        /// <summary>
        /// Restore local player's board from backup.
        /// </summary>
        private void RestoreLocalPlayerBoard()
        {
            if (ShapeManager.Instance == null)
            {
                Debug.LogError("SpectatorManager: ShapeManager not available, cannot restore board");
                return;
            }

            if (localPlayerShapesBackup.Count > 0)
            {
                ShapeManager.Instance.PlaceShapesFromBoardState(localPlayerShapesBackup);
                Debug.Log($"SpectatorManager: Restored local player board ({localPlayerShapesBackup.Count} shapes)");
            }
            else
            {
                Debug.Log("SpectatorManager: No local player shapes to restore (board empty)");
            }
        }

        /// <summary>
        /// Backup local player's current board state before switching to spectator mode.
        /// Should be called when about to spectate another player.
        /// </summary>
        public void BackupLocalPlayerBoard()
        {
            if (ShapeManager.Instance == null)
            {
                Debug.LogError("SpectatorManager: ShapeManager not available, cannot backup board");
                return;
            }
            localPlayerShapesBackup = ShapeManager.Instance.GetCurrentBoardState();
            Debug.Log($"SpectatorManager: Backed up local player board ({localPlayerShapesBackup.Count} shapes)");
        }

        /// <summary>
        /// Clear local player backup (e.g., when game ends).
        /// </summary>
        public void ClearLocalPlayerBackup()
        {
            localPlayerShapesBackup.Clear();
        }

        /// <summary>
        /// Update cached board state for a player (e.g., when new placement is received).
        /// </summary>
        public void UpdateCachedBoard(string playerId, List<BoardShapeData> newBoardState)
        {
            if (cachedOpponentBoards.ContainsKey(playerId))
            {
                cachedOpponentBoards[playerId] = newBoardState;
                Debug.Log($"SpectatorManager: Updated cache for {playerId} ({newBoardState.Count} shapes)");
            }
        }

        /// <summary>
        /// Get display name for a player ID (for UI).
        /// </summary>
        public string GetPlayerDisplayName(string playerId)
        {
            if (multiplayerManager != null)
            {
                return multiplayerManager.GetPlayerDisplayName(playerId);
            }
            return playerId;
        }

        /// <summary>
        /// Get the list of opponent player IDs (excludes local player).
        /// </summary>
        public List<string> GetOpponentPlayerIds()
        {
            return new List<string>(playerIds);
        }

        /// <summary>
        /// Called when a player disconnects - remove from cache and adjust cycling.
        /// </summary>
        public void OnPlayerDisconnected(string playerId)
        {
            if (cachedOpponentBoards.ContainsKey(playerId))
            {
                cachedOpponentBoards.Remove(playerId);
            }

            int index = playerIds.IndexOf(playerId);
            if (index >= 0)
            {
                playerIds.RemoveAt(index);
                if (currentSpectatedIndex == index)
                {
                    // Currently viewing disconnected player - auto-cycle to next
                    CycleToNextPlayer();
                }
                else if (currentSpectatedIndex > index)
                {
                    // Adjust index since we removed an element before current
                    currentSpectatedIndex--;
                }
            }
        }
    }
}
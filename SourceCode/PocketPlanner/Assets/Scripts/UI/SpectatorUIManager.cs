using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketPlanner.Multiplayer;
using System.Collections.Generic;

namespace PocketPlanner.UI
{
    /// <summary>
    /// Manages the spectator UI panel that allows cycling through other players' boards.
    /// Shows current spectated player name and provides navigation buttons.
    /// </summary>
    public class SpectatorUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject spectatorPanel;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private Button cycleButton;
        [SerializeField] private TextMeshProUGUI playerIndicatorText;
        [SerializeField] private Button closeButton; // Optional: quick return to local player
        [SerializeField] private Image readyStatus; // Optional: update with readySprite if spectated player's turn is completed

        [Header("Settings")]
        [SerializeField] private string localPlayerHeader = "Your Board";
        [SerializeField] private string opponentHeaderPrefix = "Viewing: ";
        [SerializeField] private string playerIndicatorFormat = "{0} / {1}"; // current index, total opponents
        
        [Header("Ready Status Sprites")]
        [SerializeField] private Sprite readySprite;
        [SerializeField] private Sprite notReadySprite;

        // Managers
        private SpectatorManager spectatorManager;
        private GameManager gameManager;

        // List of opponent player IDs (excluding local player)
        private List<string> opponentPlayerIds = new List<string>();
        private int currentOpponentIndex = -1; // -1 = local player, 0+ = opponent index

        private void Awake()
        {
            // Ensure panel is hidden by default (will be shown when Initialize is called)
            if (spectatorPanel != null)
                spectatorPanel.SetActive(false);
        }

        private void Start()
        {
            spectatorManager = SpectatorManager.Instance;
            gameManager = GameManager.Instance;

            if (spectatorManager == null)
            {
                Debug.LogError("SpectatorUIManager: SpectatorManager instance not found!");
                return;
            }

            if (gameManager == null)
            {
                Debug.LogError("SpectatorUIManager: GameManager instance not found!");
                return;
            }

            // Subscribe to events
            spectatorManager.OnSpectatorModeChanged += OnSpectatorModeChanged;
            spectatorManager.OnSpectatedPlayerChanged += OnSpectatedPlayerChanged;
            gameManager.OnSpectatorModeChanged += OnSpectatorModeChanged;
            gameManager.OnSpectatedPlayerChanged += OnSpectatedPlayerChanged;
            gameManager.OnPlayerTurnCompletionChanged += OnPlayerTurnCompletionChanged;

            // Set up button listeners
            if (cycleButton != null)
                cycleButton.onClick.AddListener(OnCycleButtonClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            // Get opponent list (may be empty if Initialize not yet called)
            opponentPlayerIds = spectatorManager.GetOpponentPlayerIds();
        }

        /// <summary>
        /// Call this after SpectatorManager.Initialize() to show the panel
        /// when opponents are available.
        /// Uses SpectatorManager.Instance directly since this may be called
        /// from GameManager.Start() before SpectatorUIManager.Start() runs.
        /// </summary>
        public void OnSpectatorDataReady()
        {
            var specManager = spectatorManager ?? SpectatorManager.Instance;
            if (specManager != null)
                opponentPlayerIds = specManager.GetOpponentPlayerIds();

            bool hasOpponents = opponentPlayerIds != null && opponentPlayerIds.Count > 0;

            if (spectatorPanel != null)
                spectatorPanel.SetActive(hasOpponents);

            if (cycleButton != null)
                cycleButton.interactable = hasOpponents;

            // Hide close button initially (only visible when actively spectating)
            if (closeButton != null)
                closeButton.gameObject.SetActive(false);

            UpdateHeader();
            UpdatePlayerIndicator();
            UpdateReadyStatus();
        }

        private void OnDestroy()
        {
            if (spectatorManager != null)
            {
                spectatorManager.OnSpectatorModeChanged -= OnSpectatorModeChanged;
                spectatorManager.OnSpectatedPlayerChanged -= OnSpectatedPlayerChanged;
            }
            if (gameManager != null)
            {
                gameManager.OnSpectatorModeChanged -= OnSpectatorModeChanged;
                gameManager.OnSpectatedPlayerChanged -= OnSpectatedPlayerChanged;
                gameManager.OnPlayerTurnCompletionChanged -= OnPlayerTurnCompletionChanged;
            }
        }

        /// <summary>
        /// Called when spectator mode changes (from either SpectatorManager or GameManager).
        /// Updates UI content without changing panel visibility (managed by OnSpectatorDataReady).
        /// </summary>
        private void OnSpectatorModeChanged(bool isSpectating)
        {
            // Refresh opponent list
            if (spectatorManager != null)
                opponentPlayerIds = spectatorManager.GetOpponentPlayerIds();

            // Update UI content
            UpdateHeader();

            // Show/hide close button when spectating vs local view
            if (closeButton != null)
                closeButton.gameObject.SetActive(isSpectating);

            // Disable/enable cycle button if no opponents
            if (cycleButton != null)
                cycleButton.interactable = opponentPlayerIds.Count > 0;

            UpdateReadyStatus();
        }

        /// <summary>
        /// Called when spectated player changes.
        /// </summary>
        private void OnSpectatedPlayerChanged(string playerId)
        {
            UpdateHeader();
            UpdatePlayerIndicator();
            UpdateReadyStatus();
        }

        /// <summary>
        /// Update header text based on currently spectated player.
        /// </summary>
        private void UpdateHeader()
        {
            if (headerText == null) return;

            if (spectatorManager == null) return;

            string currentPlayerId = spectatorManager.CurrentSpectatedPlayerId;
            bool isSpectating = spectatorManager.IsSpectating;

            if (!isSpectating || currentPlayerId == spectatorManager.LocalPlayerId)
            {
                headerText.text = localPlayerHeader;
            }
            else
            {
                string displayName = MultiplayerManager.Instance != null
                    ? MultiplayerManager.Instance.GetPlayerDisplayName(currentPlayerId)
                    : currentPlayerId;
                // Trim to 10 characters for UI space
                if (displayName.Length > 10) displayName = displayName.Substring(0, 10) + "...";
                headerText.text = opponentHeaderPrefix + displayName;
            }
        }

        /// <summary>
        /// Update player indicator text (e.g., "2 / 5").
        /// </summary>
        private void UpdatePlayerIndicator()
        {
            if (playerIndicatorText == null) return;
            if (spectatorManager == null) return;

            if (!spectatorManager.IsSpectating || opponentPlayerIds.Count == 0)
            {
                playerIndicatorText.text = "";
                return;
            }

            string currentPlayerId = spectatorManager.CurrentSpectatedPlayerId;
            int index = opponentPlayerIds.IndexOf(currentPlayerId);
            if (index < 0) index = 0;

            playerIndicatorText.text = string.Format(playerIndicatorFormat, index + 1, opponentPlayerIds.Count);
        }

        /// <summary>
        /// Update the ready status image to reflect whether the currently viewed player
        /// has completed their turn. Uses readySprite if completed, notReadySprite if not.
        /// </summary>
        private void UpdateReadyStatus()
        {
            if (readyStatus == null || spectatorManager == null || gameManager == null)
                return;

            string currentPlayerId = spectatorManager.CurrentSpectatedPlayerId;
            bool hasCompleted = gameManager.HasPlayerCompletedTurn(currentPlayerId);

            readyStatus.sprite = hasCompleted ? readySprite : notReadySprite;
            Debug.Log($"SpectatorUIManager: Updated ready status for {currentPlayerId} - completed: {hasCompleted}");
        }

        /// <summary>
        /// Called when any player's turn completion status changes in multiplayer.
        /// Updates the ready status display if the affected player is currently being viewed.
        /// </summary>
        private void OnPlayerTurnCompletionChanged(string playerId, bool completed)
        {
            if (spectatorManager != null && spectatorManager.CurrentSpectatedPlayerId == playerId)
            {
                UpdateReadyStatus();
            }
        }

        /// <summary>
        /// Cycle button clicked - advance to next player.
        /// </summary>
        private void OnCycleButtonClicked()
        {
            if (!MultiplayerManager.Instance.IsMultiplayerMode) return; // Disable cycling in single-player mode
            if (spectatorManager == null) return;

            spectatorManager.CycleToNextPlayer();
        }

        /// <summary>
        /// Close button clicked - return to local player view.
        /// </summary>
        private void OnCloseButtonClicked()
        {
            if (spectatorManager == null) return;

            spectatorManager.SwitchToPlayer(spectatorManager.LocalPlayerId);
        }

        /// <summary>
        /// Update opponent list (in case players join/leave).
        /// </summary>
        public void RefreshOpponentList()
        {
            if (spectatorManager == null) return;

            opponentPlayerIds = spectatorManager.GetOpponentPlayerIds();
            UpdatePlayerIndicator();
        }

        /// <summary>
        /// Public method to show/hide spectator UI (e.g., from game menu).
        /// </summary>
        public void SetUIVisible(bool visible)
        {
            if (spectatorPanel != null)
                spectatorPanel.SetActive(visible);
        }
    }
}
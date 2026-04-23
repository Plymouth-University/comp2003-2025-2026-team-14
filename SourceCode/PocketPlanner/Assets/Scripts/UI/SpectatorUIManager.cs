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

        [Header("Settings")]
        [SerializeField] private string localPlayerHeader = "Your Board";
        [SerializeField] private string opponentHeaderPrefix = "Viewing: ";
        [SerializeField] private string playerIndicatorFormat = "{0} / {1}"; // current index, total opponents

        // Managers
        private SpectatorManager spectatorManager;
        private GameManager gameManager;

        // List of opponent player IDs (excluding local player)
        private List<string> opponentPlayerIds;
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
        /// </summary>
        public void OnSpectatorDataReady()
        {
            if (spectatorManager != null)
                opponentPlayerIds = spectatorManager.GetOpponentPlayerIds();

            bool hasOpponents = opponentPlayerIds.Count > 0;

            if (spectatorPanel != null)
                spectatorPanel.SetActive(hasOpponents);

            if (cycleButton != null)
                cycleButton.interactable = hasOpponents;

            // Hide close button initially (only visible when actively spectating)
            if (closeButton != null)
                closeButton.gameObject.SetActive(false);

            UpdateHeader();
            UpdatePlayerIndicator();
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
        }

        /// <summary>
        /// Called when spectated player changes.
        /// </summary>
        private void OnSpectatedPlayerChanged(string playerId)
        {
            UpdateHeader();
            UpdatePlayerIndicator();
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
                string displayName = spectatorManager.GetPlayerDisplayName(currentPlayerId);
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
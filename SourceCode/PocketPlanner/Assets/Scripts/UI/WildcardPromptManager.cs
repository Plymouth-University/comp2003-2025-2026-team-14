using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using PocketPlanner.Core;

namespace PocketPlanner.UI
{
    /// <summary>
    /// UI panel for prompting player to use wildcard or end game when no valid placements exist.
    /// </summary>
    public class WildcardPromptManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button useWildcardButton;
        [SerializeField] private Button endGameButton;
        [SerializeField] private Button cancelButton; // Optional, hidden in auto-end mode

        [Header("Events")]
        public UnityEvent<bool> onChoiceMade; // true = use wildcard, false = end game

        private System.Action<bool> callback;

        private void Awake()
        {
            if (panel == null)
                panel = gameObject;

            // Wire up button events
            if (useWildcardButton != null)
                useWildcardButton.onClick.AddListener(() => OnChoiceMade(true));

            if (endGameButton != null)
                endGameButton.onClick.AddListener(() => OnChoiceMade(false));

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnChoiceMade(false)); // Treat cancel as end game

            // Hide by default
            Hide();
        }

        /// <summary>
        /// Show the wildcard prompt with current wildcard cost.
        /// </summary>
        /// <param name="wildcardsUsed">Number of wildcards already used (0-2)</param>
        /// <param name="nextWildcardCost">Cost of using a wildcard now</param>
        /// <param name="callback">Called with true if player chooses to use wildcard, false if chooses to end game</param>
        public void ShowPrompt(int wildcardsUsed, int nextWildcardCost, System.Action<bool> callback)
        {
            this.callback = callback;

            // Update UI text
            if (messageText != null)
                messageText.text = "No valid placements available!";

            if (costText != null)
                costText.text = $"Use Wildcard ({nextWildcardCost} points)";

            // Update button text with cost
            if (useWildcardButton != null)
            {
                var buttonText = useWildcardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = $"Continue";
            }

            // Show panel
            if (panel != null)
            {
                panel.SetActive(true);

                // Ensure CanvasGroup is enabled if present
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }

            Debug.Log($"WildcardPromptManager: Showing prompt (wildcards used: {wildcardsUsed}, cost: {nextWildcardCost})");
        }

        /// <summary>
        /// Hide the prompt panel.
        /// </summary>
        public void Hide()
        {
            if (panel != null)
            {
                // Disable CanvasGroup if present
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }

                panel.SetActive(false);
            }
        }

        private void OnChoiceMade(bool useWildcard)
        {
            Debug.Log($"WildcardPromptManager: Player chose {(useWildcard ? "Use Wildcard" : "End Game")}");

            callback?.Invoke(useWildcard);
            onChoiceMade?.Invoke(useWildcard);

            Hide();
        }

        /// <summary>
        /// Check if the panel is currently active.
        /// </summary>
        public bool IsActive()
        {
            return panel != null && panel.activeSelf;
        }
    }
}
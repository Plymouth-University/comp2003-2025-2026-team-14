using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PocketPlanner.UI
{
    /// <summary>
    /// Generic panel for wildcard selection (shape or building type).
    /// Manages a set of buttons and fires an event when a selection is made.
    /// </summary>
    public class WildcardSelectionPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private List<Button> selectionButtons = new List<Button>();
        [SerializeField] private Button cancelButton;

        [Header("Events")]
        public UnityEvent<int> onSelectionMade; // int parameter is face index (0-5)

        private void Awake()
        {
            Debug.Log($"WildcardSelectionPanel.Awake() on {gameObject.name}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
            if (panel == null)
                panel = gameObject;

            // Wire up button click events
            for (int i = 0; i < selectionButtons.Count; i++)
            {
                if (selectionButtons[i] == null)
                {
                    Debug.LogWarning($"WildcardSelectionPanel: Button at index {i} is null.");
                    continue;
                }

                int index = i; // Capture for closure
                selectionButtons[i].onClick.AddListener(() => OnButtonClicked(index));
            }

            // Wire up cancel button if assigned
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            // Hide panel by default
            Hide();
        }

        private void Start()
        {
            Debug.Log($"WildcardSelectionPanel.Start() on {gameObject.name}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
        }

        /// <summary>
        /// Show the selection panel.
        /// </summary>
        public void Show()
        {
            Debug.Log($"WildcardSelectionPanel.Show() on {gameObject.name}, panel={(panel != null ? panel.name : "null")}, activeSelf={(panel != null ? panel.activeSelf.ToString() : "N/A")}, activeInHierarchy={(panel != null ? panel.activeInHierarchy.ToString() : "N/A")}");
            if (panel != null)
            {
                // Only activate if not already active
                if (!panel.activeSelf)
                {
                    panel.SetActive(true);
                    Debug.Log($"Panel activated, activeSelf={panel.activeSelf}, activeInHierarchy={panel.activeInHierarchy}");
                }
                else
                {
                    Debug.Log($"Panel already active, activeSelf={panel.activeSelf}, activeInHierarchy={panel.activeInHierarchy}");
                }

                if (panel.activeSelf && !panel.activeInHierarchy)
                {
                    Debug.LogWarning("Panel is active but parent is inactive - panel may not be visible.");
                }

                // Ensure CanvasGroup is enabled if present
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    Debug.Log($"CanvasGroup found and enabled on panel {panel.name}");
                }
            }
            else
            {
                Debug.LogError("WildcardSelectionPanel.Show(): panel is null!");
            }
        }

        /// <summary>
        /// Hide the selection panel.
        /// </summary>
        public void Hide()
        {
            Debug.Log($"WildcardSelectionPanel.Hide() on {gameObject.name}, panel={(panel != null ? panel.name : "null")}, activeSelf={(panel != null ? panel.activeSelf.ToString() : "N/A")}");
            if (panel != null)
            {
                // Disable CanvasGroup if present
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    Debug.Log($"CanvasGroup disabled on panel {panel.name}");
                }

                // Only deactivate if currently active
                if (panel.activeSelf)
                {
                    panel.SetActive(false);
                    Debug.Log($"Panel deactivated");
                }
                else
                {
                    Debug.Log($"Panel already inactive");
                }
            }
        }

        /// <summary>
        /// Called when a selection button is clicked.
        /// </summary>
        private void OnButtonClicked(int buttonIndex)
        {
            Debug.Log($"WildcardSelectionPanel: Button {buttonIndex} clicked");
            onSelectionMade?.Invoke(buttonIndex);
            Hide();
        }

        /// <summary>
        /// Called when the cancel button is clicked.
        /// Hides the panel without making a selection.
        /// </summary>
        private void OnCancelClicked()
        {
            Debug.Log("WildcardSelectionPanel: Cancel button clicked");
            Hide();
        }

        /// <summary>
        /// Set the text on a specific button.
        /// </summary>
        public void SetButtonText(int buttonIndex, string text)
        {
            if (buttonIndex >= 0 && buttonIndex < selectionButtons.Count)
            {
                var textComponent = selectionButtons[buttonIndex].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
            }
        }

        /// <summary>
        /// Set all button texts from an array.
        /// </summary>
        public void SetAllButtonTexts(string[] texts)
        {
            int count = Mathf.Min(selectionButtons.Count, texts.Length);
            for (int i = 0; i < count; i++)
            {
                SetButtonText(i, texts[i]);
            }
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
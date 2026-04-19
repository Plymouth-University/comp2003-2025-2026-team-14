using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PocketPlanner.Multiplayer;

public class JoinLobbyManager : MonoBehaviour
{
    [Header("Multiplayer Managers")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private MultiplayerManager multiplayerManager;
    [SerializeField] private SyncManager syncManager;

    [Header("Lobby Code Input")]
    [SerializeField] private List<TMP_InputField> lobbyCodeInputFields; // DEPRECATED: List of ordered input fields for lobby code entry (1 character per field). Use hiddenLobbyCodeInputField and lobbyCodeVisualFields instead.
    [SerializeField] private List<TextMeshProUGUI> lobbyCodeVisualFields; // List of 6 Visual text elements that mirror the input fields (enforce 1 char limit per field)
    [SerializeField] private TMP_InputField hiddenLobbyCodeInputField; // Hidden input field that is activated for actual input (enforce 6 char limit, auto-uppercase, and character validation)

    [SerializeField] private TextMeshProUGUI errorText; // Text element to display messages to the user (for example if lobby code is invalid, lobby is full or lobby code too short)

    private bool isJoining = false;
    private const string VALID_LOBBY_CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No ambiguous characters (0, O, 1, I, etc.)
    private Coroutine joinTimeoutCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Activate hidden input field on scene load
        if (hiddenLobbyCodeInputField != null)
        {
            hiddenLobbyCodeInputField.ActivateInputField();
        }
        else
        {
            Debug.LogWarning("JoinLobbyManager: Hidden lobby code input field not assigned.");
        }

        // Validate visual fields count
        if (lobbyCodeVisualFields == null || lobbyCodeVisualFields.Count < 6)
        {
            Debug.LogWarning("JoinLobbyManager: lobbyCodeVisualFields must have at least 6 elements.");
        }

        FindManagersIfMissing();
        SubscribeToEvents();
        ClearError();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        CancelJoinTimeout();
    }

    private void FindManagersIfMissing()
    {
        if (firebaseManager == null) firebaseManager = FirebaseManager.Instance;
        if (multiplayerManager == null) multiplayerManager = MultiplayerManager.Instance;
        if (syncManager == null) syncManager = SyncManager.Instance;

        // LobbyManager doesn't have a singleton instance; it's attached to the same GameObject as MultiplayerManager
        if (lobbyManager == null && multiplayerManager != null)
        {
            lobbyManager = multiplayerManager.GetComponent<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogWarning("JoinLobbyManager: LobbyManager not found on MultiplayerManager GameObject.");
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (multiplayerManager != null)
        {
            multiplayerManager.OnError += OnMultiplayerError;
            multiplayerManager.OnLobbyJoined += OnLobbyJoined;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (multiplayerManager != null)
        {
            multiplayerManager.OnError -= OnMultiplayerError;
            multiplayerManager.OnLobbyJoined -= OnLobbyJoined;
        }
    }

    /// <summary>
    /// Called when the Join Lobby button is clicked in the UI.
    /// </summary>
    public void OnJoinLobbyButtonClicked()
    {
        if (isJoining)
        {
            ShowError("Already attempting to join a lobby.");
            return;
        }

        string lobbyCode = GetLobbyCodeFromInputFields();
        if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6)
        {
            ShowError("Please enter a valid 6-character lobby code.");
            return;
        }

        if (multiplayerManager == null)
        {
            ShowError("Multiplayer system not ready. Please try again.");
            return;
        }

        if (!firebaseManager.IsReady())
        {
            ShowError("Firebase not ready. Please check your internet connection.");
            return;
        }

        ClearError();
        isJoining = true;

        Debug.Log($"JoinLobbyManager: Attempting to join lobby with code: {lobbyCode}");
        Debug.Log($"JoinLobbyManager: Firebase status - Initialized: {firebaseManager.IsInitialized}, Authenticated: {firebaseManager.IsAuthenticated}, UserId: {firebaseManager.UserId}");

        // Start timeout for join operation
        CancelJoinTimeout();
        joinTimeoutCoroutine = StartCoroutine(JoinTimeoutCoroutine(lobbyCode));

        // Enable multiplayer mode as client (non-host) with the provided lobby code
        multiplayerManager.EnableMultiplayerMode(false, lobbyCode);
    }

    /// <summary>
    /// Gets the lobby code from the hidden input field (already validated).
    /// </summary>
    private string GetLobbyCodeFromInputFields()
    {
        if (hiddenLobbyCodeInputField == null)
        {
            Debug.LogError("JoinLobbyManager: hiddenLobbyCodeInputField is null.");
            return "";
        }

        string code = hiddenLobbyCodeInputField.text;

        // Ensure code is exactly 6 characters
        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            return "";
        }

        // Validate each character (should already be validated, but double-check)
        foreach (char c in code)
        {
            if (!VALID_LOBBY_CODE_CHARS.Contains(c))
            {
                Debug.LogWarning($"JoinLobbyManager: Invalid character '{c}' in lobby code. Valid characters are: {VALID_LOBBY_CODE_CHARS}");
                return "";
            }
        }

        return code;
    }

    /// <summary>
    /// Called when an error occurs in multiplayer operations.
    /// </summary>
    private void OnMultiplayerError(string errorMessage)
    {
        Debug.LogError($"JoinLobbyManager: Multiplayer error - {errorMessage}");
        ShowError(errorMessage);
        ResetJoinState();
    }

    /// <summary>
    /// Called when successfully joined a lobby.
    /// </summary>
    private void OnLobbyJoined(string lobbyCode)
    {
        Debug.Log($"JoinLobbyManager: Successfully joined lobby {lobbyCode}");
        ResetJoinState();
        ClearError();
        // Load the lobby scene to show player list and ready up
        PPSceneManager.LoadLobby();
    }

    /// <summary>
    /// Displays an error message to the user.
    /// </summary>
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
        else
        {
            Debug.LogWarning($"JoinLobbyManager: Error text UI element not assigned. Error: {message}");
        }
    }

    /// <summary>
    /// Clears any displayed error message.
    /// </summary>
    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = "";
        }
    }

    public void OnHiddenInputFieldChanged()
    {
        // Attached to the hidden input field's OnValueChanged event to update visual fields and handle auto-advance/backspace logic
        // Update the visual input fields to display the content of the hidden input field (ensure 1 character per visual field)
        if (hiddenLobbyCodeInputField == null) return;

        string text = hiddenLobbyCodeInputField.text;

        // Convert to uppercase
        text = text.ToUpper();

        // Filter out invalid characters
        StringBuilder filtered = new StringBuilder();
        foreach (char c in text)
        {
            if (VALID_LOBBY_CODE_CHARS.Contains(c))
            {
                filtered.Append(c);
            }
        }
        text = filtered.ToString();

        // Limit to 6 characters
        if (text.Length > 6)
        {
            text = text.Substring(0, 6);
        }

        // Update hidden input field text if it changed (to reflect validation)
        if (hiddenLobbyCodeInputField.text != text)
        {
            hiddenLobbyCodeInputField.text = text;
            // Setting text will trigger OnValueChanged again, so return early
            return;
        }

        // Update visual fields
        if (lobbyCodeVisualFields != null && lobbyCodeVisualFields.Count >= 6)
        {
            for (int i = 0; i < 6; i++)
            {
                if (i < text.Length)
                {
                    lobbyCodeVisualFields[i].text = text[i].ToString();
                }
                else
                {
                    lobbyCodeVisualFields[i].text = "";
                }
            }
        }
    }

    /// <summary>
    /// DEPRECATED: Called when a lobby code input field changes (for auto-advance functionality).
    /// This can be attached to each input field's OnValueChanged event.
    /// Now using hidden input field; this method does nothing.
    /// </summary>
    public void OnLobbyCodeInputFieldChanged(int fieldIndex)
    {
        // No-op: Auto-advance is handled by hidden input field
    }

    public void OnVisualInputFieldClicked(int fieldIndex)
    {
        // Will be attached to each of the visual input field's on click event to activate the hidden input field (and selects the corresponding character using fieldIndex) for actual input
        if (hiddenLobbyCodeInputField == null) return;

        hiddenLobbyCodeInputField.ActivateInputField();
        hiddenLobbyCodeInputField.Select();

        // Set caret position to the clicked field index, but clamp to current text length
        // If fieldIndex is beyond current text length, place caret at end
        int caretPosition = Mathf.Clamp(fieldIndex, 0, hiddenLobbyCodeInputField.text.Length);
        hiddenLobbyCodeInputField.caretPosition = caretPosition;
        hiddenLobbyCodeInputField.selectionAnchorPosition = caretPosition;
        hiddenLobbyCodeInputField.selectionFocusPosition = caretPosition;
    }

    /// <summary>
    /// Coroutine that times out join operation after 10 seconds.
    /// </summary>
    private System.Collections.IEnumerator JoinTimeoutCoroutine(string lobbyCode)
    {
        yield return new WaitForSeconds(10f); // 10 second timeout

        if (isJoining)
        {
            Debug.LogError($"JoinLobbyManager: Join operation timed out for lobby code: {lobbyCode}");
            ShowError("Join operation timed out. Please check your internet connection and try again.");
            isJoining = false;
            CancelJoinTimeout();
        }
    }

    /// <summary>
    /// Cancel the join timeout coroutine.
    /// </summary>
    private void CancelJoinTimeout()
    {
        if (joinTimeoutCoroutine != null)
        {
            StopCoroutine(joinTimeoutCoroutine);
            joinTimeoutCoroutine = null;
        }
    }

    /// <summary>
    /// Reset join state (called on success or failure).
    /// </summary>
    private void ResetJoinState()
    {
        isJoining = false;
        CancelJoinTimeout();
    }
}

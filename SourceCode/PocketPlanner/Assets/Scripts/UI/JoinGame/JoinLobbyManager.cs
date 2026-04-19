using System.Collections.Generic;
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
    [SerializeField] private List<TMP_InputField> lobbyCodeInputFields; // List of ordered input fields for lobby code entry (1 character per field)

    [SerializeField] private TextMeshProUGUI errorText; // Text element to display messages to the user (for example if lobby code is invalid, lobby is full or lobby code too short)

    private bool isJoining = false;
    private const string VALID_LOBBY_CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No ambiguous characters (0, O, 1, I, etc.)
    private Coroutine joinTimeoutCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
    /// Concatenates the text from each input field to form the lobby code.
    /// </summary>
    private string GetLobbyCodeFromInputFields()
    {
        if (lobbyCodeInputFields == null || lobbyCodeInputFields.Count != 6)
        {
            Debug.LogError("JoinLobbyManager: lobbyCodeInputFields must have exactly 6 elements.");
            return "";
        }

        string code = "";
        for (int i = 0; i < lobbyCodeInputFields.Count; i++)
        {
            var inputField = lobbyCodeInputFields[i];
            if (inputField == null)
            {
                Debug.LogError($"JoinLobbyManager: Input field at index {i} is null.");
                return "";
            }
            string text = inputField.text?.Trim().ToUpper();
            if (string.IsNullOrEmpty(text) || text.Length != 1)
            {
                // Require all fields filled.
                return "";
            }
            char c = text[0];
            if (!VALID_LOBBY_CODE_CHARS.Contains(c))
            {
                Debug.LogWarning($"JoinLobbyManager: Invalid character '{c}' in lobby code. Valid characters are: {VALID_LOBBY_CODE_CHARS}");
                return "";
            }
            code += c;
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

    /// <summary>
    /// Called when a lobby code input field changes (for auto-advance functionality).
    /// This can be attached to each input field's OnValueChanged event.
    /// </summary>
    public void OnLobbyCodeInputFieldChanged(int fieldIndex)
    {
        // Optional: auto-advance to next field when a character is entered
        if (lobbyCodeInputFields == null || fieldIndex < 0 || fieldIndex >= lobbyCodeInputFields.Count)
            return;

        var currentField = lobbyCodeInputFields[fieldIndex];
        if (currentField.text.Length >= 1 && fieldIndex < lobbyCodeInputFields.Count - 1)
        {
            lobbyCodeInputFields[fieldIndex + 1].Select();
            lobbyCodeInputFields[fieldIndex + 1].ActivateInputField();
        }
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

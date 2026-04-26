using UnityEngine;
using PocketPlanner.Multiplayer;
using UnityEngine.UI;
using TMPro;

public class HostSettingsPanel : MonoBehaviour
{

    private int maxPlayers = 2; // Default to 2 players, can be adjusted by slider in settings panel

    // These settings are placeholders and currently not implemented in the game
    private int turnTimeLimit = -1; // Default to unlimited time per turn, can be adjusted by slider in settings panel

    [SerializeField] private GameObject hostSettingsPanel;

    [Header("Max Players Setting")]
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private TextMeshProUGUI maxPlayersCountText;

    [Header("Turn Time Setting")]
    [SerializeField] private Slider turnTimeSlider;
    [SerializeField] private TextMeshProUGUI turnTimeText;
    public int sliderTurnTimeIncrement = 30; // Assuming each slider step represents 30 seconds

    [SerializeField] private TMP_InputField displayNameInputField; // Field for host to set their display name

    [Header("Multiplayer Managers")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private MultiplayerManager multiplayerManager;
    [SerializeField] private SyncManager syncManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (hostSettingsPanel != null)
            hostSettingsPanel.SetActive(false); // Ensure panel is hidden on start
        else
            Debug.LogError("HostSettingsPanel reference is not assigned!");

        // Ensure references to multiplayer managers are assigned
        Debug.Log("HostSettingsPanel.Start: Initializing manager references...");
        // Ensure MultiplayerManager instance exists
        MultiplayerManager.EnsureInstanceExists();

        // Try to use singleton instances first for managers that have them
        if (firebaseManager == null)
        {
            Debug.Log("HostSettingsPanel.Start: Getting FirebaseManager.Instance...");
            firebaseManager = FirebaseManager.Instance;
            if (firebaseManager != null)
                Debug.Log($"HostSettingsPanel.Start: FirebaseManager found via Instance (userId: {firebaseManager.UserId})");
            else
                Debug.LogWarning("HostSettingsPanel.Start: FirebaseManager.Instance is null!");
        }

        if (multiplayerManager == null)
        {
            Debug.Log("HostSettingsPanel.Start: Getting MultiplayerManager.Instance...");
            multiplayerManager = MultiplayerManager.Instance;
            if (multiplayerManager != null)
            {
                Debug.Log($"HostSettingsPanel.Start: MultiplayerManager found via Instance (IsMultiplayerMode: {multiplayerManager.IsMultiplayerMode}, IsLobbyHost: {multiplayerManager.IsLobbyHost})");
                // If MultiplayerManager exists, try to get LobbyManager from same GameObject
                if (lobbyManager == null)
                {
                    lobbyManager = multiplayerManager.GetComponent<LobbyManager>();
                    if (lobbyManager != null)
                        Debug.Log("HostSettingsPanel.Start: LobbyManager found via MultiplayerManager GameObject");
                }
                // Try to get SyncManager from same GameObject
                if (syncManager == null)
                {
                    syncManager = multiplayerManager.GetComponent<SyncManager>();
                    if (syncManager != null)
                        Debug.Log("HostSettingsPanel.Start: SyncManager found via MultiplayerManager GameObject");
                }
            }
            else
            {
                Debug.LogWarning("HostSettingsPanel.Start: MultiplayerManager.Instance is null! Trying FindAnyObjectByType...");
                multiplayerManager = FindAnyObjectByType<MultiplayerManager>();
                if (multiplayerManager != null)
                    Debug.Log("HostSettingsPanel.Start: MultiplayerManager found via FindAnyObjectByType");
                else
                    Debug.LogError("HostSettingsPanel.Start: No MultiplayerManager found in scene!");
            }
        }

        // Fallback: FindAnyObjectByType for any remaining null references
        if (lobbyManager == null)
        {
            Debug.LogWarning("HostSettingsPanel.Start: LobbyManager reference is still null, trying FindAnyObjectByType...");
            lobbyManager = FindAnyObjectByType<LobbyManager>();
            if (lobbyManager != null)
                Debug.Log("HostSettingsPanel.Start: LobbyManager found via FindAnyObjectByType");
        }

        if (firebaseManager == null)
        {
            Debug.LogWarning("HostSettingsPanel.Start: FirebaseManager reference is still null, trying FindAnyObjectByType...");
            firebaseManager = FindAnyObjectByType<FirebaseManager>();
            if (firebaseManager != null)
                Debug.Log("HostSettingsPanel.Start: FirebaseManager found via FindAnyObjectByType");
        }

        if (syncManager == null)
        {
            Debug.LogWarning("HostSettingsPanel.Start: SyncManager reference is still null, trying FindAnyObjectByType...");
            syncManager = FindAnyObjectByType<SyncManager>();
            if (syncManager != null)
                Debug.Log("HostSettingsPanel.Start: SyncManager found via FindAnyObjectByType");
        }

        // Final check
        if (multiplayerManager == null)
            Debug.LogError("HostSettingsPanel.Start: MultiplayerManager reference is MISSING! Hosting will fail.");
        else
            Debug.Log("HostSettingsPanel.Start: MultiplayerManager reference successfully obtained.");

        if (firebaseManager == null)
            Debug.LogError("HostSettingsPanel.Start: FirebaseManager reference is MISSING!");
        else
            Debug.Log($"HostSettingsPanel.Start: FirebaseManager ready: {firebaseManager.IsReady()}, userId: {firebaseManager.UserId}");
    }

    public void displayHostSettingsPanel()
    {
        if (hostSettingsPanel != null)
        {
            hostSettingsPanel.SetActive(true);
            // Also enable canvas group to allow interactions just in case
            CanvasGroup canvasGroup = hostSettingsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            Debug.LogError("HostSettingsPanel reference is not assigned!");
        }
    }

    public void hideHostSettingsPanel()
    {
        if (hostSettingsPanel != null)
        {
            hostSettingsPanel.SetActive(false);
            // Also disable canvas group to block interactions just in case
            CanvasGroup canvasGroup = hostSettingsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            Debug.LogError("HostSettingsPanel reference is not assigned!");
        }
    }

    public void updateMaxPlayerCount()
    {
        // Update text and internal variable when slider value changes
        maxPlayers = (int)maxPlayersSlider.value;
        maxPlayersCountText.text = maxPlayers.ToString();
        
    }

    public void updateTurnTime()
    {
        // Update text and internal variable when slider value changes
        int turnTime = (int)turnTimeSlider.value;
        turnTimeLimit = turnTime * sliderTurnTimeIncrement; 
        switch (turnTime)
        {
            case 1:
                turnTimeText.text = $"{turnTimeLimit}";
                break;
            case 2:
                turnTimeText.text = $"{turnTimeLimit}";
                break;
            case 3:
                turnTimeText.text = $"{turnTimeLimit}";
                break;
            case 4:
                turnTimeText.text = $"{turnTimeLimit}";
                break;
            case 5:
                turnTimeText.text = $"∞";
                turnTimeLimit = -1; // Use -1 to represent unlimited time internally
                break;
            default:
                turnTimeText.text = "Invalid";
                break;
        }
    }

    public void ConfirmHostSettings()
    {
        Debug.Log("HostSettingsPanel.ConfirmHostSettings: Starting host confirmation...");
        // Ensure MultiplayerManager instance exists
        MultiplayerManager.EnsureInstanceExists();

        // Ensure we have a valid MultiplayerManager reference (fallback to singleton instance)
        if (multiplayerManager == null)
        {
            Debug.LogWarning("HostSettingsPanel.ConfirmHostSettings: multiplayerManager serialized field is null, trying MultiplayerManager.Instance...");
            multiplayerManager = MultiplayerManager.Instance;
        }

        if (multiplayerManager == null)
        {
            Debug.LogError("MultiplayerManager reference is missing in HostSettingsPanel. Cannot host game.");
            Debug.LogError("MultiplayerManager.Instance is null. The MultiplayerManager GameObject may have been destroyed.");
            Debug.LogError("Please ensure a MultiplayerManager GameObject exists in the scene and is marked DontDestroyOnLoad.");
            return;
        }

        Debug.Log($"HostSettingsPanel.ConfirmHostSettings: MultiplayerManager found (IsMultiplayerMode: {multiplayerManager.IsMultiplayerMode}, IsLobbyHost: {multiplayerManager.IsLobbyHost}, LocalPlayerId: {multiplayerManager.LocalPlayerId})");

        // Ensure Firebase is ready
        if (firebaseManager == null)
        {
            Debug.LogWarning("HostSettingsPanel.ConfirmHostSettings: firebaseManager serialized field is null, trying FirebaseManager.Instance...");
            firebaseManager = FirebaseManager.Instance;
        }

        if (firebaseManager == null || !firebaseManager.IsReady())
        {
            Debug.LogError("Firebase not ready. Please check connection.");
            Debug.LogError($"FirebaseManager: {(firebaseManager == null ? "null" : $"IsReady={firebaseManager.IsReady()}, UserId={firebaseManager.UserId}")}");
            // Could show error UI
            return;
        }

        Debug.Log($"HostSettingsPanel.ConfirmHostSettings: Creating lobby with maxPlayers={maxPlayers}, turnTimeLimit={turnTimeLimit}");

        // Get display name from input field
        string displayName = displayNameInputField != null ? displayNameInputField.text.Trim() : null;

        // Subscribe to events before enabling multiplayer mode
        Debug.Log("HostSettingsPanel.ConfirmHostSettings: Subscribing to MultiplayerManager events...");
        multiplayerManager.OnLobbyJoined += OnLobbyJoined;
        multiplayerManager.OnError += OnMultiplayerError;

        // Enable multiplayer mode as host with selected settings
        Debug.Log($"HostSettingsPanel.ConfirmHostSettings: Calling EnableMultiplayerMode(isHost: true, lobbyCode: \"\", maxPlayers: {maxPlayers}, turnTimeLimit: {turnTimeLimit}, displayName: \"{displayName}\")");
        multiplayerManager.EnableMultiplayerMode(true, "", maxPlayers, turnTimeLimit, displayName);
        Debug.Log("HostSettingsPanel.ConfirmHostSettings: EnableMultiplayerMode called successfully.");
    }

    private void OnLobbyJoined(string lobbyCode)
    {
        Debug.Log($"HostSettingsPanel.OnLobbyJoined called with lobbyCode: {lobbyCode}");
        Debug.Log($"HostSettingsPanel: Lobby joined successfully with code: {lobbyCode}");

        // Unsubscribe from events to prevent multiple calls
        if (multiplayerManager != null)
        {
            Debug.Log($"HostSettingsPanel: Unsubscribing from MultiplayerManager events");
            multiplayerManager.OnLobbyJoined -= OnLobbyJoined;
            multiplayerManager.OnError -= OnMultiplayerError;
        }

        Debug.Log($"HostSettingsPanel: Loading lobby scene via PPSceneManager.LoadLobby()");
        // Load the lobby scene
        PPSceneManager.LoadLobby();
        Debug.Log($"HostSettingsPanel: PPSceneManager.LoadLobby() called");
    }

    private void OnMultiplayerError(string errorMessage)
    {
        Debug.LogError($"HostSettingsPanel: Multiplayer error - {errorMessage}");

        // Unsubscribe from events
        if (multiplayerManager != null)
        {
            multiplayerManager.OnLobbyJoined -= OnLobbyJoined;
            multiplayerManager.OnError -= OnMultiplayerError;
        }

        // Show error to user (could implement UI feedback)
        // For now, just log error
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (multiplayerManager != null)
        {
            multiplayerManager.OnLobbyJoined -= OnLobbyJoined;
            multiplayerManager.OnError -= OnMultiplayerError;
        }
    }

}

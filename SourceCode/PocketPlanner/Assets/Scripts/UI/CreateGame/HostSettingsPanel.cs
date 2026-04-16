using UnityEngine;
using PocketPlanner.Multiplayer;
using UnityEngine.UI;
using TMPro;

public class HostSettingsPanel : MonoBehaviour
{

    private int maxPlayers = 2; // Default to 2 players, can be adjusted by slider in settings panel

    // These settings are placeholders and currently not implemented in the game
    private int turnTimeLimit = 60; // Default to 60 seconds per turn, can be adjusted by slider in settings panel

    [SerializeField] private GameObject hostSettingsPanel;

    [Header("Max Players Setting")]
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private TextMeshProUGUI maxPlayersCountText;

    [Header("Turn Time Setting")]
    [SerializeField] private Slider turnTimeSlider;
    [SerializeField] private TextMeshProUGUI turnTimeText;
    public int sliderTurnTimeIncrement = 30; // Assuming each slider step represents 30 seconds

    [Header("Multiplayer Managers")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private MultiplayerManager multiplayerManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (hostSettingsPanel != null)
            hostSettingsPanel.SetActive(false); // Ensure panel is hidden on start
        else
            Debug.LogError("HostSettingsPanel reference is not assigned!");
        // Ensure references to multiplayer managers are assigned
        if (lobbyManager == null)
        {
            Debug.LogWarning("LobbyManager reference is not assigned in HostSettingsPanel!");
            lobbyManager = FindAnyObjectByType<LobbyManager>();
        }
        if (firebaseManager == null)
        {
            Debug.LogWarning("FirebaseManager reference is not assigned in HostSettingsPanel!");
            firebaseManager = FindAnyObjectByType<FirebaseManager>();
        }
        if (multiplayerManager == null)
        {
            Debug.LogWarning("MultiplayerManager reference is not assigned in HostSettingsPanel!");
            multiplayerManager = FindAnyObjectByType<MultiplayerManager>();
        }
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
        if (lobbyManager != null)
        {
            // TODO: pass configured settings to lobby manager
        }
        else
        {
            Debug.LogError("LobbyManager reference is missing in HostSettingsPanel. Cannot host game.");
        }

        PPSceneManager.LoadLobby();
    }

    
}

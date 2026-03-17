using UnityEngine;
using UnityEngine.SceneManagement;

public class PPSceneManager : MonoBehaviour
{
    public static PPSceneManager Instance { get; private set; }

    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Kill this duplicate
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Implement global logic for scene transitions here

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public static void LoadCreateGame()
    {
        // Scene should include an option for playing single player or hosting a multiplayer game, which then leads to lobby scene
        SceneManager.LoadScene("CreateGameScene");
    }

    public static void LoadLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public static void LoadJoinGame()
    {
        SceneManager.LoadScene("JoinGameScene");
    }

    public static void LoadMainGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public static void LoadSettings()
    {
        // Scene for adjusting accessibility options, board color, music volume, etc.
        SceneManager.LoadScene("SettingsScene");
    }

    public static void LoadRules()
    {
        // Scene for displaying rules from main menu and in game settings
        SceneManager.LoadScene("RulesScene");
    }

    public static void LoadResults()
    {
        // Scene for end game rankings and scores (eg. 1st, 2nd, 3rd place)
        SceneManager.LoadScene("ResultsScene");
    }

    public static void LoadScoreSheet()
    {
        // Scene for detailed score breakdown at the end of the game (eg. points from each category, bonuses, etc.)
        SceneManager.LoadScene("ScoreSheetScene");
    }

}

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinGameManager : MonoBehaviour
{

    [Header("Buttons")]
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button menuButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up button listeners
        joinGameButton.onClick.AddListener(OnJoinGameButtonClicked);
        menuButton.onClick.AddListener(OnMenuButtonClicked);
    }

    void OnJoinGameButtonClicked()
    {
        // TODO: Implement join game logic here (e.g., validate input, connect to server, etc.)
        SceneManager.LoadScene("LobbyScene");
    }

    void OnMenuButtonClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}

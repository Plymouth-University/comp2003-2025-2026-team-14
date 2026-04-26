using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class EndScoreboardUIManager : MonoBehaviour
{
    // At game end, replace the old end game panel with this new UI manager
    [Header("End Scoreboard Panel Reference")]
    [SerializeField] private GameObject endScoreboardPanel;

     [Header("Player List Scoreboard UI")]
     // Activate panels up to max players in the game, disable the rest
    [SerializeField] private List<GameObject> playerListPanels; // List of 8 UI panels (index 0 corresponds to player with highest score at game end) for displaying player names and scores;
    [SerializeField] private List<TextMeshProUGUI> playerNameTexts; // List of 8 Text components (index 0 corresponds to player with highest score at game end);
    [SerializeField] private List<TextMeshProUGUI> playerScoreTexts; // List of 8 Text components (index 0 corresponds to player with highest score at game end);
    [SerializeField] private List<Button> playerScoreBreakdownButtons; // List of 8 Buttons (index 0 corresponds to player with highest score at game end); each button should open a detailed score breakdown for that player when clicked

    [Header("Return to Main Menu Button Reference")]
    [SerializeField] private Button returnToMainMenuButton; // Should call OnLeaveMultiplayerAndReturnToMainMenu on click

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    
}

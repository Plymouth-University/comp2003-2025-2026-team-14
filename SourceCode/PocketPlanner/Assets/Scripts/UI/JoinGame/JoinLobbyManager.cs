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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

}

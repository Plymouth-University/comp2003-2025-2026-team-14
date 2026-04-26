using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketPlanner.Multiplayer;

public class EndScoreboardUIManager : MonoBehaviour
{
    [Header("End Scoreboard Panel Reference")]
    [SerializeField] private GameObject endScoreboardPanel;

    [Header("Player List Scoreboard UI")]
    // Activate panels up to max players in the game, disable the rest
    // index 0 corresponds to player with highest score at game end (rank 1)
    [SerializeField] private List<GameObject> playerListPanels;
    [SerializeField] private List<TextMeshProUGUI> playerNameTexts;
    [SerializeField] private List<TextMeshProUGUI> playerScoreTexts;
    [SerializeField] private List<Button> playerScoreBreakdownButtons;

    [Header("Return to Main Menu Button Reference")]
    [SerializeField] private Button returnToMainMenuButton;

    [Header("Score Breakdown UI Reference")]
    [SerializeField] private ScoreBreakdownUIManager scoreBreakdownUIManager;

    [Header("Continue / Play Again Button")]
    [SerializeField] private Button playAgainButton;

    // Stored ranked player data for score breakdown button clicks
    private List<PlayerScoreEntry> rankedPlayers = new List<PlayerScoreEntry>();

    // Local player's ScoreComponents (for single player mode)
    private ScoreComponents localScore;

    // Is the scoreboard currently showing?
    private bool isVisible = false;

    /// <summary>
    /// Internal entry for tracking a ranked player's score data.
    /// </summary>
    private class PlayerScoreEntry
    {
        public string playerId;
        public string displayName;
        public int totalScore;
        public int rank;
        public ScoreComponents scoreComponents;
        public bool isLocalPlayer;
    }

    void Start()
    {
        // Wire up return to main menu button
        if (returnToMainMenuButton != null && GameManager.Instance != null)
        {
            returnToMainMenuButton.onClick.AddListener(GameManager.Instance.ReturnToMainMenu);
        }

        // Wire up play again button
        if (playAgainButton != null && GameManager.Instance != null)
        {
            playAgainButton.onClick.AddListener(GameManager.Instance.RestartGame);
        }

        // Find ScoreBreakdownUIManager if not assigned
        if (scoreBreakdownUIManager == null)
        {
            scoreBreakdownUIManager = FindAnyObjectByType<ScoreBreakdownUIManager>();
        }

        // Hide panel initially
        if (endScoreboardPanel != null)
        {
            endScoreboardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Display the end-game scoreboard.
    /// For single player: shows only the local player.
    /// For multiplayer: fetches all final scores from Firebase, ranks them, and displays.
    /// </summary>
    /// <param name="score">The local player's final ScoreComponents</param>
    public void DisplayScoreboard(ScoreComponents score)
    {
        localScore = score;

        bool isMultiplayer = MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode;

        if (isMultiplayer)
        {
            // Broadcast our final score to Firebase, then fetch all scores
            StartCoroutine(DisplayMultiplayerScoreboardCoroutine(score));
        }
        else
        {
            // Single player: display immediately
            DisplaySinglePlayerScoreboard(score);
        }
    }

    /// <summary>
    /// Coroutine: broadcast local score, wait briefly, then fetch all scores from Firebase.
    /// </summary>
    private IEnumerator DisplayMultiplayerScoreboardCoroutine(ScoreComponents score)
    {
        // Show loading state
        ShowPanelWithLoadingState();

        // Broadcast local score to Firebase
        if (SyncManager.Instance != null)
        {
            var broadcastTask = SyncManager.Instance.BroadcastFinalScore(score);
            while (!broadcastTask.IsCompleted)
            {
                yield return null;
            }
            Debug.Log("EndScoreboardUIManager: Final score broadcast complete.");
        }
        else
        {
            Debug.LogError("EndScoreboardUIManager: SyncManager not available for score broadcast.");
        }

        // Wait a moment for other players' scores to arrive in Firebase
        yield return new WaitForSeconds(1.0f);

        // Fetch all final scores from Firebase
        if (SyncManager.Instance != null)
        {
            bool fetchComplete = false;
            List<FinalScoreData> allScores = null;

            SyncManager.Instance.FetchAllFinalScores(scores =>
            {
                allScores = scores;
                fetchComplete = true;
            });

            // Wait for fetch to complete (timeout after 10 seconds)
            float timeout = 10.0f;
            float elapsed = 0f;
            while (!fetchComplete && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }

            if (fetchComplete && allScores != null && allScores.Count > 0)
            {
                DisplayRankedScoreboard(allScores);
            }
            else
            {
                // Fallback: show only local player
                Debug.LogWarning("EndScoreboardUIManager: No scores fetched from Firebase, showing only local player.");
                DisplaySinglePlayerScoreboard(score);
            }
        }
        else
        {
            Debug.LogError("EndScoreboardUIManager: SyncManager not available for score fetching.");
            DisplaySinglePlayerScoreboard(score);
        }
    }

    /// <summary>
    /// Display scoreboard for single player mode.
    /// </summary>
    private void DisplaySinglePlayerScoreboard(ScoreComponents score)
    {
        rankedPlayers.Clear();

        string playerName = "Player 1";
        if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMultiplayerMode)
        {
            playerName = MultiplayerManager.Instance.LocalDisplayName ?? "You";
        }

        var entry = new PlayerScoreEntry
        {
            playerId = playerName,
            displayName = playerName,
            totalScore = score.totalScore,
            rank = 1,
            scoreComponents = score,
            isLocalPlayer = true
        };
        rankedPlayers.Add(entry);

        PopulatePlayerPanels();
        ShowPanel();
    }

    /// <summary>
    /// Rank players by totalScore (descending) and populate the scoreboard.
    /// </summary>
    private void DisplayRankedScoreboard(List<FinalScoreData> allScores)
    {
        rankedPlayers.Clear();

        // Sort by totalScore descending
        var sortedScores = allScores.OrderByDescending(s => s.totalScore).ToList();

        string localPlayerId = MultiplayerManager.Instance?.LocalPlayerId ?? "";

        for (int i = 0; i < sortedScores.Count; i++)
        {
            var scoreData = sortedScores[i];
            ScoreComponents parsedComponents = new ScoreComponents();

            // Deserialize the full ScoreComponents from JSON
            if (!string.IsNullOrEmpty(scoreData.scoreComponentsJson))
            {
                try
                {
                    parsedComponents = JsonUtility.FromJson<ScoreComponents>(scoreData.scoreComponentsJson);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"EndScoreboardUIManager: Failed to parse ScoreComponents for {scoreData.playerId}: {ex.Message}");
                }
            }

            // If ScoreComponents couldn't be parsed, reconstruct from the flat fields
            if (parsedComponents.totalScore == 0 && scoreData.totalScore != 0)
            {
                parsedComponents = ReconstructScoreComponents(scoreData);
            }

            var entry = new PlayerScoreEntry
            {
                playerId = scoreData.playerId,
                displayName = scoreData.displayName,
                totalScore = scoreData.totalScore,
                rank = i + 1,
                scoreComponents = parsedComponents,
                isLocalPlayer = scoreData.playerId == localPlayerId
            };
            rankedPlayers.Add(entry);
        }

        // If our local player isn't in the fetched scores (shouldn't happen normally), add them
        if (!string.IsNullOrEmpty(localPlayerId) && !rankedPlayers.Any(p => p.playerId == localPlayerId))
        {
            var entry = new PlayerScoreEntry
            {
                playerId = localPlayerId,
                displayName = "You",
                totalScore = localScore.totalScore,
                rank = rankedPlayers.Count + 1,
                scoreComponents = localScore,
                isLocalPlayer = true
            };
            rankedPlayers.Add(entry);

            // Re-sort
            rankedPlayers = rankedPlayers.OrderByDescending(p => p.totalScore).ToList();
            for (int i = 0; i < rankedPlayers.Count; i++)
            {
                rankedPlayers[i].rank = i + 1;
            }
        }

        PopulatePlayerPanels();
        ShowPanel();
    }

    /// <summary>
    /// Reconstruct ScoreComponents from flat FinalScoreData fields.
    /// Used as fallback when JSON deserialization fails.
    /// </summary>
    private ScoreComponents ReconstructScoreComponents(FinalScoreData data)
    {
        ScoreComponents components = new ScoreComponents
        {
            totalScore = data.totalScore,
            starScore = data.stars,
            emptyCellPenalty = data.emptyCellPenalty,
            wildcardCostTotal = -data.wildcardsUsed * (data.wildcardsUsed + 1) / 2, // Approximate: -1, -3, -6
            industrialZoneScore = data.industrialZoneScore,
            residentialZoneScore = data.residentialZoneScore,
            commercialZoneScore = data.commercialZoneScore,
            parkScore = data.parkScore,
            schoolScore = data.schoolScore
        };
        components.InitializeBreakdownArrays();
        components.CalculateTotal();
        return components;
    }

    /// <summary>
    /// Populate the player list panels with ranked player data.
    /// Activate panels up to the number of players, disable the rest.
    /// </summary>
    private void PopulatePlayerPanels()
    {
        int playerCount = rankedPlayers.Count;
        int maxPanels = Mathf.Min(playerListPanels.Count, playerNameTexts.Count, playerScoreTexts.Count, playerScoreBreakdownButtons.Count);

        for (int i = 0; i < maxPanels; i++)
        {
            bool hasPlayer = i < playerCount;

            // Show/hide panel
            if (playerListPanels[i] != null)
            {
                playerListPanels[i].SetActive(hasPlayer);
            }

            if (hasPlayer)
            {
                var player = rankedPlayers[i];

                // Set player name (with rank prefix)
                if (playerNameTexts[i] != null)
                {
                    string displayName = player.displayName;
                    if (displayName.Length > 10)
                    {
                        displayName = displayName.Substring(0, 10) + "...";
                    }
                    playerNameTexts[i].text = displayName;
                }

                // Set player score
                if (playerScoreTexts[i] != null)
                {
                    playerScoreTexts[i].text = player.totalScore.ToString();
                }

                // Wire up score breakdown button
                if (playerScoreBreakdownButtons[i] != null)
                {
                    // Remove old listeners to avoid duplicates
                    playerScoreBreakdownButtons[i].onClick.RemoveAllListeners();

                    // Capture the player data in a local variable for the closure
                    var capturedPlayer = player;
                    playerScoreBreakdownButtons[i].onClick.AddListener(() =>
                    {
                        OnScoreBreakdownButtonClicked(capturedPlayer);
                    });
                }
            }
        }
    }

    /// <summary>
    /// Get a rank prefix string (1st, 2nd, 3rd, 4th, etc.).
    /// </summary>
    private string GetRankPrefix(int rank)
    {
        return rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => rank + "th"
        };
    }

    /// <summary>
    /// Handler for when a player's score breakdown button is clicked.
    /// Shows the detailed ScoreBreakdownUIManager with that player's data.
    /// </summary>
    private void OnScoreBreakdownButtonClicked(PlayerScoreEntry player)
    {
        if (scoreBreakdownUIManager == null)
        {
            scoreBreakdownUIManager = FindAnyObjectByType<ScoreBreakdownUIManager>();
            if (scoreBreakdownUIManager == null)
            {
                Debug.LogError("EndScoreboardUIManager: ScoreBreakdownUIManager not found!");
                return;
            }
        }

        Debug.Log($"EndScoreboardUIManager: Showing score breakdown for {player.displayName} (score: {player.totalScore})");
        scoreBreakdownUIManager.DisplayScoreBreakdown(player.scoreComponents);
    }

    /// <summary>
    /// Show the panel in a loading state (waiting for Firebase data).
    /// </summary>
    private void ShowPanelWithLoadingState()
    {
        if (endScoreboardPanel != null)
        {
            endScoreboardPanel.SetActive(true);
        }

        // Show only the first panel with "Loading..." text
        int maxPanels = Mathf.Min(playerListPanels.Count, playerNameTexts.Count);
        for (int i = 0; i < maxPanels; i++)
        {
            if (playerListPanels[i] != null)
            {
                playerListPanels[i].SetActive(i == 0);
            }
        }

        if (playerNameTexts.Count > 0 && playerNameTexts[0] != null)
        {
            playerNameTexts[0].text = "Loading scores...";
        }

        if (playerScoreTexts.Count > 0 && playerScoreTexts[0] != null)
        {
            playerScoreTexts[0].text = "...";
        }

        // Disable breakdown buttons during loading
        foreach (var button in playerScoreBreakdownButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }

        isVisible = true;
    }

    /// <summary>
    /// Show the scoreboard panel.
    /// </summary>
    private void ShowPanel()
    {
        if (endScoreboardPanel != null)
        {
            endScoreboardPanel.SetActive(true);
        }

        // Re-enable breakdown buttons
        foreach (var button in playerScoreBreakdownButtons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }

        isVisible = true;
    }

    /// <summary>
    /// Hide the scoreboard panel.
    /// </summary>
    public void Hide()
    {
        if (endScoreboardPanel != null)
        {
            endScoreboardPanel.SetActive(false);
        }
        isVisible = false;
    }

    /// <summary>
    /// Whether the scoreboard is currently visible.
    /// </summary>
    public bool IsVisible => isVisible;
}

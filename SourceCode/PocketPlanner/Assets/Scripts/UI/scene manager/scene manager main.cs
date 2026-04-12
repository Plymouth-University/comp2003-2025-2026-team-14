using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Required for Stack

public class NavigationManager : MonoBehaviour
{
    // The "Singleton" instance that stays alive throughout the whole game
    public static NavigationManager Instance { get; private set; }

    // The history stack to track every scene visited
    private Stack<string> sceneHistory = new Stack<string>();

    private void Awake()
    {
        // Ensure only one instance of this manager ever exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // This makes the object survive scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }


    /// Use this to move forward to a new scene.

    public void GoToScene(string sceneName)
    {
        // Save the current scene to history before leaving
        sceneHistory.Push(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(sceneName);
    }


    /// Use this for your 'Back' button.

    public void GoBack()
    {
        if (sceneHistory.Count > 0)
        {
            string previousScene = sceneHistory.Pop();
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("No more scenes in history to go back to!");
        }
    }
}
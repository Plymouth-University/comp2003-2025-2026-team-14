using Firebase;
using UnityEngine;

namespace PocketPlanner.Multiplayer.Test
{
    /// <summary>
    /// Test script to verify Firebase connectivity and authentication.
    /// Logs the initialization and authentication status to the console.
    /// </summary>
    public class TestFirebaseConnectivity : MonoBehaviour
    {
        private void Start()
        {

            // Subscribe to FirebaseManager events
            /*
            FirebaseManager.Instance.OnInitialized += HandleFirebaseInitialized;
            FirebaseManager.Instance.OnAuthenticationSuccess += HandleAuthenticationSuccess;
            FirebaseManager.Instance.OnAuthenticationFailed += HandleAuthenticationFailed;
            FirebaseManager.Instance.OnError += HandleFirebaseError;
            */
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnInitialized -= HandleFirebaseInitialized;
                FirebaseManager.Instance.OnAuthenticationSuccess -= HandleAuthenticationSuccess;
                FirebaseManager.Instance.OnAuthenticationFailed -= HandleAuthenticationFailed;
                FirebaseManager.Instance.OnError -= HandleFirebaseError;
            }
        }

        [ContextMenu("TestMyMethod")]
        void TestMyMethod()
        {
            FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWith(task =>
                {
                    if (task.Result == DependencyStatus.Available)
                    {
                        Debug.Log("Firebase dependencies are available.");
                        // Optionally, you can trigger authentication here for testing
                        // FirebaseManager.Instance.AuthenticateUser();
                    }
                    else
                    {
                        Debug.LogError($"Firebase dependencies are not available: {task.Result}");
                    }
                });
        }

        private void HandleFirebaseInitialized()
        {
            Debug.Log("Firebase initialized successfully.");
        }

        private void HandleAuthenticationSuccess()
        {
            Debug.Log($"Firebase authentication successful. User ID: {FirebaseManager.Instance.UserId}");
        }

        private void HandleAuthenticationFailed(string error)
        {
            Debug.LogError($"Firebase authentication failed: {error}");
        }

        private void HandleFirebaseError(string error)
        {
            Debug.LogError($"Firebase error: {error}");
        }
    }
}
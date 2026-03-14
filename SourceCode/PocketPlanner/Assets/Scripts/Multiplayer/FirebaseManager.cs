using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

namespace PocketPlanner.Multiplayer
{
    /// <summary>
    /// Singleton manager for Firebase initialization and authentication.
    /// Handles Firebase app setup, anonymous authentication, and provides database references.
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        // Singleton instance
        public static FirebaseManager Instance { get; private set; }

        // Firebase authentication state
        public bool IsInitialized { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public string UserId { get; private set; }

        // Firebase references
        private FirebaseApp _firebaseApp;
        private FirebaseAuth _auth;
        private DatabaseReference _databaseReference;

        // Public accessors
        public DatabaseReference DatabaseReference => _databaseReference;

        // Database references
        public string DatabaseRootPath { get; private set; }

        // Events
        public event Action OnInitialized;
        public event Action OnAuthenticationSuccess;
        public event Action<string> OnAuthenticationFailed;
        public event Action<string> OnError;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize with default values
            IsInitialized = false;
            IsAuthenticated = false;
            UserId = string.Empty;
            DatabaseRootPath = "https://pocketplanner-64c1d-default-rtdb.europe-west1.firebasedatabase.app/";
        }

        private void Start()
        {
            // Start Firebase initialization
            InitializeFirebaseAsync();
        }

        /// <summary>
        /// Initialize Firebase asynchronously.
        /// </summary>
        private async void InitializeFirebaseAsync()
        {
            try
            {
                Debug.Log("FirebaseManager: Initializing Firebase...");

                // Check and fix Firebase dependencies
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

                if (dependencyStatus == DependencyStatus.Available)
                {
                    _firebaseApp = FirebaseApp.DefaultInstance;
                    _auth = FirebaseAuth.DefaultInstance;

                    // Initialize database reference
                    _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

                    Debug.Log("FirebaseManager: Firebase initialized successfully");
                    IsInitialized = true;
                    OnInitialized?.Invoke();

                    // Proceed to anonymous authentication
                    AuthenticateAnonymouslyAsync();
                }
                else
                {
                    Debug.LogError($"FirebaseManager: Could not resolve all Firebase dependencies: {dependencyStatus}");
                    OnError?.Invoke($"Firebase dependencies not available: {dependencyStatus}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FirebaseManager: Initialization failed - {ex.Message}");
                OnError?.Invoke($"Initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Authenticate user anonymously with Firebase.
        /// </summary>
        private async void AuthenticateAnonymouslyAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("FirebaseManager: Cannot authenticate - Firebase not initialized");
                return;
            }

            try
            {
                Debug.Log("FirebaseManager: Starting anonymous authentication...");

                // Sign in anonymously
                var authResult = await _auth.SignInAnonymouslyAsync();

                if (authResult != null && authResult.User != null)
                {
                    UserId = authResult.User.UserId;
                    IsAuthenticated = true;

                    Debug.Log($"FirebaseManager: User authenticated anonymously with ID: {UserId}");
                    OnAuthenticationSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError("FirebaseManager: Authentication failed - null result");
                    OnAuthenticationFailed?.Invoke("Authentication failed: null result");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FirebaseManager: Authentication failed - {ex.Message}");
                Debug.LogError($"FirebaseManager: Authentication exception details: {ex}");
                OnAuthenticationFailed?.Invoke($"Authentication failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the full database path for a specific node.
        /// </summary>
        /// <param name="path">Relative path in the database</param>
        /// <returns>Full database path</returns>
        public string GetDatabasePath(string path)
        {
            // Remove leading slash if present
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            return $"{DatabaseRootPath}{path}";
        }

        /// <summary>
        /// Set a custom database root path (for testing or different environments).
        /// </summary>
        /// <param name="rootPath">The new root path for the database</param>
        public void SetDatabaseRootPath(string rootPath)
        {
            DatabaseRootPath = rootPath;
            Debug.Log($"FirebaseManager: Database root path set to: {DatabaseRootPath}");
        }

        /// <summary>
        /// Check if Firebase is ready for multiplayer operations.
        /// </summary>
        /// <returns>True if Firebase is initialized and user is authenticated</returns>
        public bool IsReady()
        {
            return IsInitialized && IsAuthenticated && !string.IsNullOrEmpty(UserId);
        }

        /// <summary>
        /// Get the current authentication status as a string for debugging.
        /// </summary>
        /// <returns>Formatted status string</returns>
        public string GetStatusString()
        {
            return $"Firebase Status: Initialized={IsInitialized}, Authenticated={IsAuthenticated}, UserId={UserId}";
        }

        /// <summary>
        /// Test Firebase connectivity by attempting a simple database read.
        /// </summary>
        public void TestConnectivity()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("FirebaseManager: Cannot test connectivity - Firebase not initialized");
                return;
            }

            Debug.Log("FirebaseManager: Testing connectivity...");

            // Try to read a test path to check connectivity
            _databaseReference.Child("connection_test").Child("timestamp").SetValueAsync(DateTime.UtcNow.Ticks).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"FirebaseManager: Connectivity test failed - {task.Exception?.Message}");
                    Debug.LogError($"FirebaseManager: Full exception: {task.Exception}");
                }
                else
                {
                    Debug.Log("FirebaseManager: Connectivity test passed - write successful");

                    // Now try to read it back
                    _databaseReference.Child("connection_test").Child("timestamp").GetValueAsync().ContinueWith(readTask =>
                    {
                        if (readTask.IsFaulted)
                        {
                            Debug.LogError($"FirebaseManager: Read test failed - {readTask.Exception?.Message}");
                        }
                        else
                        {
                            Debug.Log("FirebaseManager: Read test passed - Firebase connectivity confirmed");
                            // Clean up
                            _databaseReference.Child("connection_test").RemoveValueAsync();
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Simulate reconnection for testing purposes.
        /// </summary>
        public void SimulateReconnection()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("FirebaseManager: Cannot simulate reconnection - Firebase not initialized");
                return;
            }

            Debug.Log("FirebaseManager: Simulating reconnection...");
            IsAuthenticated = false;
            UserId = string.Empty;

            // Simulate re-authentication after delay
            Invoke(nameof(AuthenticateAnonymouslyAsync), 1.0f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
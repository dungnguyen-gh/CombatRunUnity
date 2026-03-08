using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// Central manager for scene transitions with loading screen support.
/// Handles persistent singletons initialization and cleanup between scenes.
/// </summary>
public class SceneTransitionManager : MonoBehaviour {
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Game";
    public string loadingSceneName = "Loading";

    [Header("Loading Settings")]
    public bool useLoadingScene = true;
    public float minimumLoadTime = 1.5f;
    
    [Header("Transition Settings")]
    public float transitionDuration = 0.5f;
    
    // State
    private bool isTransitioning = false;
    private string pendingScene = "";
    private Action onSceneLoadComplete;
    
    // Events
    public System.Action<string> OnSceneWillChange;
    public System.Action<string> OnSceneChanged;
    public System.Action<float> OnLoadingProgress;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePersistentSystems();
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initializes all persistent systems that should exist across scenes.
    /// Called once when the first scene loads.
    /// </summary>
    void InitializePersistentSystems() {
        // Ensure all persistent managers exist
        EnsureManager<UIManager>();
        EnsureManager<GameManager>();
        EnsureManager<InventoryManager>();
        EnsureManager<ShopManager>();
        EnsureManager<WeaponMasteryManager>();
        EnsureManager<DailyRunManager>();
        EnsureManager<SetBonusManager>();
        EnsureManager<DamageNumberManager>();
        EnsureManager<EnemyPool>();
        
        Debug.Log("[SceneTransitionManager] All persistent systems initialized");
    }

    /// <summary>
    /// Ensures a manager exists, creating it if necessary.
    /// </summary>
    void EnsureManager<T>() where T : MonoBehaviour {
        T manager = FindFirstObjectByType<T>();
        if (manager == null) {
            GameObject go = new GameObject(typeof(T).Name);
            manager = go.AddComponent<T>();
            Debug.Log($"[SceneTransitionManager] Created {typeof(T).Name}");
        }
    }

    #region Scene Transition Methods

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    public void GoToMainMenu() {
        LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Loads the game scene and starts a new game.
    /// </summary>
    public void GoToGame(Action onComplete = null) {
        LoadScene(gameSceneName, onComplete);
    }

    /// <summary>
    /// Loads the daily run game mode.
    /// </summary>
    public void GoToDailyRun() {
        DailyRunManager.Instance?.GenerateDailyRun();
        LoadScene(gameSceneName, () => {
            DailyRunManager.Instance?.StartDailyRun();
        });
    }

    /// <summary>
    /// Reloads the current scene (for restart).
    /// </summary>
    public void RestartCurrentScene() {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Quits the game (works in editor and build).
    /// </summary>
    public void QuitGame() {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Core method to load a scene with optional loading screen.
    /// </summary>
    public void LoadScene(string sceneName, Action onComplete = null) {
        if (isTransitioning) {
            Debug.LogWarning($"[SceneTransitionManager] Already transitioning, ignoring request to load {sceneName}");
            return;
        }

        if (!IsSceneInBuildSettings(sceneName)) {
            Debug.LogError($"[SceneTransitionManager] Scene '{sceneName}' is not in Build Settings!");
            return;
        }

        StartCoroutine(TransitionToScene(sceneName, onComplete));
    }

    /// <summary>
    /// Coroutine that handles the scene transition with fade and loading.
    /// </summary>
    IEnumerator TransitionToScene(string sceneName, Action onComplete) {
        isTransitioning = true;
        pendingScene = sceneName;
        onSceneLoadComplete = onComplete;

        OnSceneWillChange?.Invoke(sceneName);
        Debug.Log($"[SceneTransitionManager] Transitioning to {sceneName}");

        // Fade out (handled by UIManager if available)
        yield return StartCoroutine(FadeOut());

        if (useLoadingScene && !string.IsNullOrEmpty(loadingSceneName)) {
            yield return StartCoroutine(LoadWithLoadingScreen(sceneName));
        } else {
            yield return StartCoroutine(LoadDirect(sceneName));
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Complete
        isTransitioning = false;
        pendingScene = "";
        
        OnSceneChanged?.Invoke(sceneName);
        onSceneLoadComplete?.Invoke();
        onSceneLoadComplete = null;

        Debug.Log($"[SceneTransitionManager] Scene transition to {sceneName} complete");
    }

    /// <summary>
    /// Loads scene directly without loading screen.
    /// </summary>
    IEnumerator LoadDirect(string sceneName) {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        loadOp.allowSceneActivation = false;

        while (loadOp.progress < 0.9f) {
            OnLoadingProgress?.Invoke(loadOp.progress / 0.9f);
            yield return null;
        }

        // Small delay for smooth transition
        yield return new WaitForSecondsRealtime(0.2f);

        loadOp.allowSceneActivation = true;
        
        while (!loadOp.isDone) {
            yield return null;
        }
    }

    /// <summary>
    /// Loads scene through loading screen.
    /// </summary>
    IEnumerator LoadWithLoadingScreen(string sceneName) {
        // Load loading scene first
        AsyncOperation loadLoading = SceneManager.LoadSceneAsync(loadingSceneName);
        while (!loadLoading.isDone) {
            yield return null;
        }

        // Get loading screen controller
        LoadingSceneController loadingController = FindFirstObjectByType<LoadingSceneController>();
        
        float startTime = Time.realtimeSinceStartup;

        // Start loading target scene in background
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        loadOp.allowSceneActivation = false;

        // Update progress
        while (loadOp.progress < 0.9f) {
            float progress = loadOp.progress / 0.9f;
            loadingController?.UpdateProgress(progress);
            OnLoadingProgress?.Invoke(progress);
            yield return null;
        }

        // Ensure minimum load time for better UX
        float elapsed = Time.realtimeSinceStartup - startTime;
        while (elapsed < minimumLoadTime) {
            float fakeProgress = Mathf.Lerp(0.9f, 1f, elapsed / minimumLoadTime);
            loadingController?.UpdateProgress(fakeProgress);
            OnLoadingProgress?.Invoke(fakeProgress);
            yield return null;
            elapsed = Time.realtimeSinceStartup - startTime;
        }

        // Activate scene
        loadOp.allowSceneActivation = true;
        while (!loadOp.isDone) {
            yield return null;
        }
    }

    #endregion

    #region Fade Transitions

    IEnumerator FadeOut() {
        // Try to use UIManager fade, otherwise just wait
        if (UIManager.Instance != null) {
            // UIManager handles its own fade
            yield return new WaitForSecondsRealtime(transitionDuration);
        } else {
            yield return new WaitForSecondsRealtime(transitionDuration);
        }
    }

    IEnumerator FadeIn() {
        // Reset time scale after scene load
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null) {
            yield return new WaitForSecondsRealtime(transitionDuration);
        } else {
            yield return new WaitForSecondsRealtime(transitionDuration);
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if a scene is in the build settings.
    /// </summary>
    bool IsSceneInBuildSettings(string sceneName) {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the name of the currently active scene.
    /// </summary>
    public string GetCurrentSceneName() {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Checks if currently in the game scene.
    /// </summary>
    public bool IsInGameScene() {
        return GetCurrentSceneName() == gameSceneName;
    }

    /// <summary>
    /// Checks if currently in the main menu scene.
    /// </summary>
    public bool IsInMainMenu() {
        return GetCurrentSceneName() == mainMenuSceneName;
    }

    /// <summary>
    /// Returns whether a scene transition is in progress.
    /// </summary>
    public bool IsTransitioning() => isTransitioning;

    #endregion
}

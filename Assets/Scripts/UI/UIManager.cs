using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Central UI controller managing HUD updates, panel toggling, notifications, and skill cooldown displays.
/// Persistent singleton that survives scene loads.
/// 
/// <para><b>Features:</b></para>
/// <list type="bullet">
///   <item><description>Pause stack system for handling overlapping panels</description></item>
///   <item><description>Notification management with queue and auto-positioning</description></item>
///   <item><description>Panel animations with CanvasGroup fade/scale effects</description></item>
///   <item><description>Sound effect hooks for UI interactions</description></item>
///   <item><description>Gamepad navigation support</description></item>
///   <item><description>Skill cooldown display integration</description></item>
/// </list>
/// 
/// <para><b>Setup Instructions:</b></para>
/// <list type="number">
///   <item><description>Create a GameObject named "UIManager" in your scene</description></item>
///   <item><description>Attach this script to the GameObject</description></item>
///   <item><description>Assign all required references in the Inspector (see tooltips)</description></item>
///   <item><description>Ensure panels have CanvasGroup components (auto-added if missing)</description></item>
/// </list>
/// 
/// <para><b>Pause Stack System:</b></para>
/// The pause stack tracks which panels are open and manages time scale accordingly.
/// When the first panel opens, Time.timeScale is set to 0 (game paused).
/// When the last panel closes, Time.timeScale is restored to 1.
/// Pressing Escape closes the most recently opened panel (LIFO order).
/// </summary>
public class UIManager : MonoBehaviour {
    
    #region Singleton Pattern
    
    /// <summary>
    /// Global instance of the UIManager. Use this to access UI functionality from other scripts.
    /// </summary>
    public static UIManager Instance { get; private set; }
    
    #endregion

    #region Inspector Fields - HUD
    
    [Header("HUD")]
    [Tooltip("The main HUD panel GameObject (always visible during gameplay)")]
    public GameObject hudPanel;
    
    [Tooltip("Text element displaying current health (format: current/max)")]
    public TextMeshProUGUI healthText;
    
    [Tooltip("Slider element for health bar visualization")]
    public UnityEngine.UI.Slider healthSlider;
    
    [Tooltip("Text element displaying current gold amount")]
    public TextMeshProUGUI goldText;
    
    [Tooltip("Array of skill icon images (index 0-3 for skills 1-4)")]
    public UnityEngine.UI.Image[] skillIcons;
    
    [Tooltip("Array of skill cooldown overlay images (fills to show cooldown progress)")]
    public UnityEngine.UI.Image[] skillCooldownOverlays;
    
    [Tooltip("Array of skill cooldown text displays (shows remaining seconds)")]
    public TextMeshProUGUI[] skillCooldownTexts;
    
    #endregion

    #region Inspector Fields - Panels
    
    [Header("Panels")]
    [Tooltip("Inventory panel GameObject (will be managed by pause stack)")]
    public GameObject inventoryPanel;
    
    [Tooltip("Shop panel GameObject (will be managed by pause stack)")]
    public GameObject shopPanel;
    
    [Tooltip("Pause menu panel GameObject (will be managed by pause stack)")]
    public GameObject pausePanel;
    
    #endregion

    #region Inspector Fields - Notifications
    
    [Header("Notifications")]
    [Tooltip("Prefab for notification messages (should have TextMeshProUGUI component)")]
    public GameObject notificationPrefab;
    
    [Tooltip("Parent transform for notification instances")]
    public Transform notificationParent;
    
    [Tooltip("How long notifications remain visible (seconds)")]
    public float notificationDuration = 2f;
    
    [Tooltip("Vertical spacing between stacked notifications (pixels)")]
    public float notificationSpacing = 60f;
    
    #endregion

    #region Inspector Fields - Panel Animation
    
    [Header("Panel Animation")]
    [Tooltip("Duration of panel fade/open animations (seconds)")]
    public float panelFadeDuration = 0.15f;
    
    [Tooltip("Animation curve for panel transitions (ease in/out recommended)")]
    public AnimationCurve panelAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    #endregion

    #region Inspector Fields - Sound Effects
    
    [Header("Sound Effects")]
    [Tooltip("Sound to play when opening any panel")]
    public AudioClip panelOpenSound;
    
    [Tooltip("Sound to play when closing any panel")]
    public AudioClip panelCloseSound;
    
    [Tooltip("Sound to play when a button is clicked")]
    public AudioClip buttonClickSound;
    
    [Tooltip("Sound to play when navigation changes (gamepad/keyboard)")]
    public AudioClip navigationSound;
    
    [Tooltip("Sound to play when showing a notification")]
    public AudioClip notificationSound;
    
    [Tooltip("Sound to play when game over screen appears")]
    public AudioClip gameOverSound;
    
    #endregion

    #region Inspector Fields - References
    
    [Header("References")]
    [Tooltip("Reference to the player (auto-found if not assigned)")]
    public PlayerController player;
    
    [Tooltip("Reference to the player's skill caster (auto-found if not assigned)")]
    public SkillCaster skillCaster;
    
    #endregion

    #region Inspector Fields - Lives & Game Over
    
    [Header("Lives & Game Over")]
    [Tooltip("Panel shown during revive countdown")]
    public GameObject revivePanel;
    
    [Tooltip("Text showing revive countdown timer")]
    public TextMeshProUGUI reviveCountdownText;
    
    [Tooltip("Panel shown when game is over (no lives remaining)")]
    public GameObject gameOverPanel;
    
    [Tooltip("Text displaying game over statistics")]
    public TextMeshProUGUI gameOverStatsText;
    
    [Tooltip("Button to restart the game")]
    public Button playAgainButton;
    
    [Tooltip("Button to quit to main menu")]
    public Button quitToMenuButton;
    
    #endregion

    #region Private Fields
    
    private List<GameObject> activeNotifications = new List<GameObject>();
    private bool isQuitting = false;

    // Pause stack system to handle overlapping panels
    private int pauseDepth = 0;
    private List<GameObject> openPanels = new List<GameObject>();

    // Cached CanvasGroup references for panels to avoid repeated GetComponent calls
    private CanvasGroup inventoryPanelCanvasGroup;
    private CanvasGroup shopPanelCanvasGroup;
    private CanvasGroup pausePanelCanvasGroup;
    
    // Game over state
    private bool isGameOver = false;
    private CanvasGroup gameOverPanelCanvasGroup;
    
    // Audio source for UI sounds
    private AudioSource uiAudioSource;
    
    #endregion

    #region Events
    
    /// <summary>
    /// Called when any panel is opened.
    /// Parameter: The GameObject of the opened panel.
    /// </summary>
    public System.Action<GameObject> OnPanelOpened;
    
    /// <summary>
    /// Called when any panel is closed.
    /// Parameter: The GameObject of the closed panel.
    /// </summary>
    public System.Action<GameObject> OnPanelClosed;
    
    /// <summary>
    /// Called when the game pause state changes.
    /// Parameter: True if game is now paused.
    /// </summary>
    public System.Action<bool> OnPauseStateChanged;
    
    /// <summary>
    /// Called when a sound effect should be played.
    /// Parameter: The AudioClip to play.
    /// </summary>
    public System.Action<AudioClip> OnPlaySoundEffect;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        FindReferences();
        SubscribeToEvents();
        InitializeHUD();
        InitializePanels();
        InitializeReviveAndGameOverPanels();
    }

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy() {
        if (!isQuitting) {
            UnsubscribeFromEvents();
            CleanupButtonListeners();
        }
    }

    /// <summary>
    /// Called when a new scene is loaded.
    /// Initializes UI for the specific scene type.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        Debug.Log($"[UIManager] Scene loaded: {scene.name}");
        
        // Reset UI state
        isGameOver = false;
        openPanels.Clear();
        pauseDepth = 0;
        Time.timeScale = 1f;
        
        // Scene-specific initialization
        if (IsGameScene(scene.name)) {
            InitializeForGameScene();
        } else if (IsMenuScene(scene.name)) {
            InitializeForMenuScene();
        }
    }

    /// <summary>
    /// Checks if scene name indicates a game scene.
    /// </summary>
    bool IsGameScene(string sceneName) {
        return sceneName.Contains("Game") || sceneName.Contains("Level") || sceneName == "SampleScene";
    }

    /// <summary>
    /// Checks if scene name indicates a menu scene.
    /// </summary>
    bool IsMenuScene(string sceneName) {
        return sceneName.Contains("Menu") || sceneName.Contains("Title") || sceneName == "MainMenu";
    }

    /// <summary>
    /// Initializes UI for the game scene.
    /// </summary>
    void InitializeForGameScene() {
        Debug.Log("[UIManager] Initializing for game scene");
        
        // Show HUD
        if (hudPanel != null) {
            hudPanel.SetActive(true);
        }
        
        // Hide menu-specific panels
        // (Menu panels should be destroyed or disabled by scene change)
        
        // Hide game over panel
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(false);
        }
        
        // Re-subscribe to player events (new player instance)
        FindAndSubscribeToPlayer();
    }

    /// <summary>
    /// Initializes UI for the menu scene.
    /// </summary>
    void InitializeForMenuScene() {
        Debug.Log("[UIManager] Initializing for menu scene");
        
        // Hide game HUD
        if (hudPanel != null) {
            hudPanel.SetActive(false);
        }
        
        // Hide game panels
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Unsubscribe from old player (will be destroyed)
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Finds the player in the scene and subscribes to events.
    /// </summary>
    void FindAndSubscribeToPlayer() {
        PlayerController foundPlayer = FindFirstObjectByType<PlayerController>();
        if (foundPlayer != null) {
            this.player = foundPlayer;
            SubscribeToEvents();
        }
    }
    
    /// <summary>
    /// Removes button listeners to prevent memory leaks on scene reload.
    /// </summary>
    void CleanupButtonListeners() {
        if (playAgainButton != null) {
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }
        if (quitToMenuButton != null) {
            quitToMenuButton.onClick.RemoveListener(OnQuitToMenuClicked);
        }
    }

    void OnApplicationQuit() {
        isQuitting = true;
    }

    void Update() {
        // Update skill cooldowns
        UpdateSkillCooldowns();
    }
    
    #endregion

    #region Initialization

    /// <summary>
    /// Sets up the audio source for UI sound effects.
    /// </summary>
    void SetupAudioSource() {
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null) {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f; // 2D sound
        }
    }

    /// <summary>
    /// Finds required references if not assigned in inspector.
    /// Called automatically during Start().
    /// </summary>
    void FindReferences() {
        if (player == null) {
            player = FindFirstObjectByType<PlayerController>();
        }
        if (skillCaster == null && player != null) {
            skillCaster = player.GetComponent<SkillCaster>();
        }
    }

    /// <summary>
    /// Subscribes to player events for UI updates.
    /// Events: OnHealthChanged, OnGoldChanged
    /// </summary>
    void SubscribeToEvents() {
        if (player != null) {
            player.OnHealthChanged += UpdateHealth;
            player.OnGoldChanged += UpdateGold;
        }
    }

    /// <summary>
    /// Unsubscribes from player events to prevent memory leaks.
    /// Always called in OnDestroy().
    /// </summary>
    void UnsubscribeFromEvents() {
        if (player != null) {
            player.OnHealthChanged -= UpdateHealth;
            player.OnGoldChanged -= UpdateGold;
        }
    }

    /// <summary>
    /// Initializes HUD elements and skill icons.
    /// </summary>
    void InitializeHUD() {
        // Initialize skill icons
        if (skillCaster != null && skillCaster.skills != null) {
            int skillCount = Mathf.Min(4, skillCaster.skills.Length);
            for (int i = 0; i < skillCount; i++) {
                if (i < skillIcons.Length && skillCaster.skills[i] != null) {
                    skillIcons[i].sprite = skillCaster.skills[i].icon;
                }
            }
        }

        // Initial update
        if (player != null) {
            UpdateHealth(player.stats.currentHP, player.stats.MaxHP);
            UpdateGold(player.gold);
        }
    }

    /// <summary>
    /// Initializes panels by caching CanvasGroup references and setting initial state.
    /// Panels start hidden (alpha = 0, inactive).
    /// </summary>
    void InitializePanels() {
        // Hide panels initially and cache CanvasGroups
        if (inventoryPanel != null) {
            inventoryPanel.SetActive(false);
            inventoryPanelCanvasGroup = GetOrAddCanvasGroup(inventoryPanel);
            inventoryPanelCanvasGroup.alpha = 0f;
        }
        if (shopPanel != null) {
            shopPanel.SetActive(false);
            shopPanelCanvasGroup = GetOrAddCanvasGroup(shopPanel);
            shopPanelCanvasGroup.alpha = 0f;
        }
        if (pausePanel != null) {
            pausePanel.SetActive(false);
            pausePanelCanvasGroup = GetOrAddCanvasGroup(pausePanel);
            pausePanelCanvasGroup.alpha = 0f;
        }
    }
    
    #endregion

    #region HUD Update Methods

    /// <summary>
    /// Updates the health display (slider and text).
    /// Automatically subscribed to PlayerController.OnHealthChanged event.
    /// </summary>
    /// <param name="current">Current health value</param>
    /// <param name="max">Maximum health value</param>
    void UpdateHealth(int current, int max) {
        if (healthSlider != null) {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null) {
            healthText.text = $"{current}/{max}";
        }
    }

    /// <summary>
    /// Updates the gold display.
    /// Automatically subscribed to PlayerController.OnGoldChanged event.
    /// </summary>
    /// <param name="gold">Current gold amount</param>
    void UpdateGold(int gold) {
        if (goldText != null) {
            goldText.text = gold.ToString();
        }
    }

    /// <summary>
    /// Updates skill cooldown overlays and text every frame.
    /// Shows fill amount for cooldown progress and remaining time.
    /// </summary>
    void UpdateSkillCooldowns() {
        if (skillCaster == null) return;

        for (int i = 0; i < 4; i++) {
            float cooldownPercent = skillCaster.GetCooldownPercent(i);
            float cooldownRemaining = skillCaster.GetCooldownRemaining(i);

            if (i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null) {
                skillCooldownOverlays[i].fillAmount = 1f - cooldownPercent;
            }

            if (i < skillCooldownTexts.Length && skillCooldownTexts[i] != null) {
                if (cooldownRemaining > 0) {
                    skillCooldownTexts[i].text = Mathf.CeilToInt(cooldownRemaining).ToString();
                } else {
                    skillCooldownTexts[i].text = "";
                }
            }
        }
    }
    
    #endregion

    #region Input Handling

    /// <summary>
    /// Handles the Escape key press from PlayerController.
    /// If panels are open, closes the most recent one (LIFO).
    /// If no panels open, toggles the pause menu.
    /// </summary>
    public void HandleEscapeKey() {
        // If game over panel is open, don't process escape
        if (isGameOver) return;
        
        // If panels are open, close the most recent one
        if (openPanels.Count > 0) {
            CloseMostRecentPanel();
        } else {
            TogglePause();
        }
    }

    /// <summary>
    /// Closes the most recently opened panel from the pause stack.
    /// </summary>
    void CloseMostRecentPanel() {
        if (openPanels.Count == 0) return;
        
        GameObject mostRecent = openPanels[openPanels.Count - 1];
        if (mostRecent == inventoryPanel) {
            ToggleInventory();
        } else if (mostRecent == shopPanel) {
            ToggleShop();
        } else if (mostRecent == pausePanel) {
            TogglePause();
        }
    }
    
    #endregion

    #region Pause Stack System

    /// <summary>
    /// Pushes a panel onto the pause stack and pauses the game if this is the first panel.
    /// </summary>
    /// <param name="panel">The panel GameObject to push</param>
    void PushPause(GameObject panel) {
        if (pauseDepth == 0) {
            Time.timeScale = 0f;
            OnPauseStateChanged?.Invoke(true);
        }
        pauseDepth++;
        openPanels.Add(panel);
        
        // Animate panel open
        StartCoroutine(AnimatePanel(panel, true));
        
        OnPanelOpened?.Invoke(panel);
        
        // Play sound
        PlaySoundEffect(panelOpenSound);
    }

    /// <summary>
    /// Pops a panel from the pause stack and unpauses the game if this was the last panel.
    /// </summary>
    /// <param name="panel">The panel GameObject to pop</param>
    void PopPause(GameObject panel) {
        if (!openPanels.Contains(panel)) return;
        
        openPanels.Remove(panel);
        pauseDepth = Mathf.Max(0, pauseDepth - 1);
        
        if (pauseDepth == 0) {
            Time.timeScale = 1f;
            OnPauseStateChanged?.Invoke(false);
        }
        
        // Animate panel close
        StartCoroutine(AnimatePanel(panel, false));
        
        OnPanelClosed?.Invoke(panel);
        
        // Play sound
        PlaySoundEffect(panelCloseSound);
    }

    /// <summary>
    /// Gets the CanvasGroup from a panel, adding one if it doesn't exist.
    /// </summary>
    /// <param name="panel">The panel GameObject</param>
    /// <returns>The CanvasGroup component</returns>
    CanvasGroup GetOrAddCanvasGroup(GameObject panel) {
        if (panel == null) return null;
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) {
            cg = panel.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    /// <summary>
    /// Coroutine that animates a panel opening or closing.
    /// Uses unscaledDeltaTime for smooth animation even when paused.
    /// </summary>
    /// <param name="panel">The panel to animate</param>
    /// <param name="opening">True if opening, false if closing</param>
    IEnumerator AnimatePanel(GameObject panel, bool opening) {
        if (panel == null) yield break;
        
        // Use cached CanvasGroup reference if available
        CanvasGroup canvasGroup = null;
        if (panel == inventoryPanel) canvasGroup = inventoryPanelCanvasGroup;
        else if (panel == shopPanel) canvasGroup = shopPanelCanvasGroup;
        else if (panel == pausePanel) canvasGroup = pausePanelCanvasGroup;
        
        // Fallback to GetComponent if not cached
        if (canvasGroup == null) {
            canvasGroup = GetOrAddCanvasGroup(panel);
        }
        
        if (opening) {
            panel.SetActive(true);
            canvasGroup.alpha = 0f;
        }
        
        float elapsed = 0f;
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        
        while (elapsed < panelFadeDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = panelAnimationCurve.Evaluate(elapsed / panelFadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        
        if (!opening) {
            panel.SetActive(false);
        }
    }
    
    #endregion

    #region Panel Control Methods

    /// <summary>
    /// Opens or closes the inventory panel.
    /// Automatically swaps with shop panel if shop is open.
    /// Uses the pause stack system.
    /// </summary>
    public void ToggleInventory() {
        if (inventoryPanel == null) {
            Debug.LogWarning("[UIManager] Inventory panel reference is missing!");
            return;
        }
        
        bool isActive = inventoryPanel.activeSelf;
        
        if (!isActive) {
            // Opening
            PushPause(inventoryPanel);
            if (shopPanel != null && shopPanel.activeSelf) {
                PopPause(shopPanel);
            }
        } else {
            // Closing
            PopPause(inventoryPanel);
        }
    }

    /// <summary>
    /// Opens or closes the shop panel.
    /// Automatically swaps with inventory panel if inventory is open.
    /// Notifies ShopManager when opened/closed.
    /// Uses the pause stack system.
    /// </summary>
    public void ToggleShop() {
        if (shopPanel == null) {
            Debug.LogWarning("[UIManager] Shop panel reference is missing!");
            return;
        }
        
        bool isActive = shopPanel.activeSelf;
        
        if (!isActive) {
            // Opening
            PushPause(shopPanel);
            if (inventoryPanel != null && inventoryPanel.activeSelf) {
                PopPause(inventoryPanel);
            }
            if (ShopManager.Instance != null) ShopManager.Instance.OpenShop();
        } else {
            // Closing
            PopPause(shopPanel);
            if (ShopManager.Instance != null) ShopManager.Instance.CloseShop();
        }
    }

    /// <summary>
    /// Opens or closes the pause menu panel.
    /// Uses the pause stack system.
    /// </summary>
    public void TogglePause() {
        if (pausePanel == null) {
            Debug.LogWarning("[UIManager] Pause panel reference is missing!");
            return;
        }
        
        bool isActive = pausePanel.activeSelf;
        
        if (!isActive) {
            PushPause(pausePanel);
        } else {
            PopPause(pausePanel);
        }
    }
    
    /// <summary>
    /// Opens the inventory panel. Does nothing if already open.
    /// </summary>
    public void OpenInventory() {
        if (inventoryPanel != null && !inventoryPanel.activeSelf) {
            ToggleInventory();
        }
    }
    
    /// <summary>
    /// Closes the inventory panel. Does nothing if already closed.
    /// </summary>
    public void CloseInventory() {
        if (inventoryPanel != null && inventoryPanel.activeSelf) {
            ToggleInventory();
        }
    }
    
    /// <summary>
    /// Opens the shop panel. Does nothing if already open.
    /// </summary>
    public void OpenShop() {
        if (shopPanel != null && !shopPanel.activeSelf) {
            ToggleShop();
        }
    }
    
    /// <summary>
    /// Closes the shop panel. Does nothing if already closed.
    /// </summary>
    public void CloseShop() {
        if (shopPanel != null && shopPanel.activeSelf) {
            ToggleShop();
        }
    }
    
    /// <summary>
    /// Opens the pause menu. Does nothing if already open.
    /// </summary>
    public void OpenPauseMenu() {
        if (pausePanel != null && !pausePanel.activeSelf) {
            TogglePause();
        }
    }
    
    /// <summary>
    /// Closes the pause menu. Does nothing if already closed.
    /// </summary>
    public void ClosePauseMenu() {
        if (pausePanel != null && pausePanel.activeSelf) {
            TogglePause();
        }
    }

    /// <summary>
    /// Resumes the game by closing all open panels.
    /// Restores time scale and clears the pause stack.
    /// </summary>
    public void ResumeGame() {
        // Close all panels
        while (openPanels.Count > 0) {
            GameObject panel = openPanels[openPanels.Count - 1];
            PopPause(panel);
        }
    }

    /// <summary>
    /// Quits the game after resuming (to restore time scale).
    /// Works in both editor and builds.
    /// </summary>
    public void QuitGame() {
        ResumeGame();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Checks if any UI panel is currently open (in the pause stack).
    /// </summary>
    /// <returns>True if at least one panel is open</returns>
    public bool IsAnyPanelOpen() {
        return openPanels.Count > 0;
    }

    /// <summary>
    /// Gets the current game pause state.
    /// </summary>
    /// <returns>True if game is paused (pauseDepth > 0)</returns>
    public bool IsGamePaused() {
        return pauseDepth > 0;
    }
    
    /// <summary>
    /// Gets the list of currently open panels (top of stack is last element).
    /// </summary>
    /// <returns>List of open panel GameObjects</returns>
    public IReadOnlyList<GameObject> GetOpenPanels() {
        return openPanels.AsReadOnly();
    }
    
    #endregion

    #region Notification System

    /// <summary>
    /// Shows a notification message on screen.
    /// Notifications stack vertically and auto-remove after notificationDuration.
    /// Maximum of 5 notifications shown at once.
    /// </summary>
    /// <param name="message">The message text to display</param>
    public void ShowNotification(string message) {
        if (string.IsNullOrEmpty(message)) return;
        if (notificationPrefab == null || notificationParent == null) {
            Debug.Log($"[Notification] {message}");
            return;
        }

        GameObject notif = Instantiate(notificationPrefab, notificationParent);
        
        // Set text
        var text = notif.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = message;

        activeNotifications.Add(notif);
        
        // Play notification sound
        PlaySoundEffect(notificationSound);

        // Remove old notifications if too many
        while (activeNotifications.Count > 5) {
            GameObject oldNotif = activeNotifications[0];
            activeNotifications.RemoveAt(0);
            if (oldNotif != null) {
                Destroy(oldNotif);
            }
        }

        // Reposition notifications
        RepositionNotifications();

        StartCoroutine(RemoveNotification(notif));
    }

    void RepositionNotifications() {
        StopAllNotificationCoroutines();
        for (int i = 0; i < activeNotifications.Count; i++) {
            if (activeNotifications[i] != null) {
                var rect = activeNotifications[i].GetComponent<RectTransform>();
                if (rect != null) {
                    // Set position immediately to avoid accumulation
                    rect.anchoredPosition = new Vector2(0, -i * notificationSpacing);
                }
            }
        }
    }
    
    void StopAllNotificationCoroutines() {
        // Stop any running position animations to prevent accumulation
        for (int i = 0; i < activeNotifications.Count; i++) {
            if (activeNotifications[i] != null) {
                var rect = activeNotifications[i].GetComponent<RectTransform>();
                if (rect != null) {
                    StopCoroutine(AnimateNotificationPosition(rect, i));
                }
            }
        }
    }

    IEnumerator AnimateNotificationPosition(RectTransform rect, int index) {
        Vector2 targetPos = new Vector2(0, -index * notificationSpacing);
        Vector2 startPos = rect.anchoredPosition;
        
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        rect.anchoredPosition = targetPos;
    }

    IEnumerator RemoveNotification(GameObject notif) {
        yield return new WaitForSecondsRealtime(notificationDuration);
        
        if (notif != null) {
            // Fade out animation
            var canvasGroup = notif.GetComponent<CanvasGroup>();
            if (canvasGroup != null) {
                float elapsed = 0f;
                float startAlpha = canvasGroup.alpha;
                
                while (elapsed < 0.2f) {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / 0.2f);
                    yield return null;
                }
            }
            
            activeNotifications.Remove(notif);
            Destroy(notif);
            
            // Reposition remaining
            RepositionNotifications();
        }
    }
    
    #endregion

    #region Lives & Revive UI

    void InitializeReviveAndGameOverPanels() {
        // Hide revive panel initially
        if (revivePanel != null) {
            revivePanel.SetActive(false);
        }
        
        // Setup game over panel
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(false);
            gameOverPanelCanvasGroup = GetOrAddCanvasGroup(gameOverPanel);
            
            // Setup button listeners
            if (playAgainButton != null) {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
            if (quitToMenuButton != null) {
                quitToMenuButton.onClick.AddListener(OnQuitToMenuClicked);
            }
        }
    }

    /// <summary>
    /// Shows the revive countdown UI.
    /// </summary>
    /// <param name="duration">Duration of the countdown in seconds</param>
    public void ShowReviveCountdown(float duration) {
        if (revivePanel != null) {
            revivePanel.SetActive(true);
        }
        
        StartCoroutine(UpdateReviveCountdown(duration));
    }

    System.Collections.IEnumerator UpdateReviveCountdown(float duration) {
        float remaining = duration;
        
        while (remaining > 0) {
            if (reviveCountdownText != null) {
                reviveCountdownText.text = $"Reviving in... {Mathf.Ceil(remaining)}";
            }
            remaining -= Time.unscaledDeltaTime; // Use unscaled time
            yield return null;
        }
        
        // Hide revive panel
        if (revivePanel != null) {
            revivePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the game over UI with Play Again button.
    /// </summary>
    /// <param name="enemiesKilled">Number of enemies killed (shown in stats)</param>
    /// <param name="wavesCompleted">Number of waves completed (shown in stats)</param>
    public void ShowGameOver(int enemiesKilled = 0, int wavesCompleted = 0) {
        isGameOver = true;
        
        // Play game over sound
        PlaySoundEffect(gameOverSound);
        
        // Hide HUD
        if (hudPanel != null) {
            hudPanel.SetActive(false);
        }
        
        // Hide revive panel if still visible
        if (revivePanel != null) {
            revivePanel.SetActive(false);
        }
        
        // Show game over panel
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(true);
            
            // Update stats text
            if (gameOverStatsText != null) {
                gameOverStatsText.text = $"Enemies Killed: {enemiesKilled}\nWaves Completed: {wavesCompleted}";
            }
            
            // Animate in
            StartCoroutine(AnimateGameOverPanel(true));
        } else {
            ShowNotification("GAME OVER!");
        }
        
        // Ensure time is running (in case it was paused)
        Time.timeScale = 1f;
    }

    IEnumerator AnimateGameOverPanel(bool show) {
        if (gameOverPanelCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        float duration = 0.5f;
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;
        
        gameOverPanelCanvasGroup.alpha = startAlpha;
        
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = panelAnimationCurve.Evaluate(elapsed / duration);
            gameOverPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        
        gameOverPanelCanvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// Called when the Play Again button is clicked.
    /// </summary>
    void OnPlayAgainClicked() {
        Debug.Log("Play Again clicked - Restarting game...");
        PlaySoundEffect(buttonClickSound);
        RestartGame();
    }

    /// <summary>
    /// Called when the Quit to Menu button is clicked.
    /// </summary>
    void OnQuitToMenuClicked() {
        Debug.Log("Quit to Menu clicked...");
        PlaySoundEffect(buttonClickSound);
        
        // Close all panels first
        CloseAllPanels();
        
        // Use GameManager or SceneTransitionManager
        if (GameManager.Instance != null) {
            GameManager.Instance.ReturnToMenu();
        } else if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.GoToMainMenu();
        }
    }

    /// <summary>
    /// Restarts the current game/scene.
    /// </summary>
    public void RestartGame() {
        Debug.Log("[UIManager] Restarting game...");
        
        // Reset game over state
        isGameOver = false;
        
        // Hide game over panel
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(false);
        }
        
        // Show HUD
        if (hudPanel != null) {
            hudPanel.SetActive(true);
        }
        
        // Clear pause stack
        openPanels.Clear();
        pauseDepth = 0;
        
        // Use GameManager for proper restart with cleanup
        if (GameManager.Instance != null) {
            GameManager.Instance.RestartGame();
        } else if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.RestartCurrentScene();
        } else {
            // Fallback
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>
    /// Quits to the main menu scene.
    /// </summary>
    public void QuitToMainMenu() {
        Debug.Log("[UIManager] Quitting to main menu...");
        
        // Reset game over state
        isGameOver = false;
        
        // Close all panels
        CloseAllPanels();
        
        // Use SceneTransitionManager for proper transition
        if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.GoToMainMenu();
        } else if (GameManager.Instance != null) {
            GameManager.Instance.ReturnToMenu();
        } else {
            // Fallback - load scene 0
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// Closes all open UI panels.
    /// </summary>
    void CloseAllPanels() {
        // Hide all panels
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Clear pause stack
        openPanels.Clear();
        pauseDepth = 0;
        
        // Reset time scale
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Hides the game over panel (called when game is restarted externally).
    /// </summary>
    public void HideGameOverPanel() {
        isGameOver = false;
        
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(false);
        }
        
        if (hudPanel != null) {
            hudPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Checks if the game over screen is currently displayed.
    /// </summary>
    /// <returns>True if game over panel is active</returns>
    public bool IsGameOver() => isGameOver;
    
    #endregion

    #region Sound Effects
    
    /// <summary>
    /// Plays a UI sound effect.
    /// Triggers OnPlaySoundEffect event for external audio managers.
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    public void PlaySoundEffect(AudioClip clip) {
        if (clip == null) return;
        
        // Trigger event for external audio managers
        OnPlaySoundEffect?.Invoke(clip);
        
        // Play through local audio source if available
        if (uiAudioSource != null) {
            uiAudioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Plays the button click sound.
    /// Call this from button onClick events.
    /// </summary>
    public void PlayButtonClickSound() {
        PlaySoundEffect(buttonClickSound);
    }
    
    /// <summary>
    /// Plays the navigation sound.
    /// Call this when navigating UI with gamepad/keyboard.
    /// </summary>
    public void PlayNavigationSound() {
        PlaySoundEffect(navigationSound);
    }
    
    #endregion

    #region Utility Methods
    
    /// <summary>
    /// Helper to safely set button text.
    /// </summary>
    public void SetButtonText(Button button, string text) {
        if (button == null) return;
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) {
            tmp.text = text;
        }
    }
    
    /// <summary>
    /// Helper to safely set image sprite.
    /// </summary>
    public void SetImageSprite(Image image, Sprite sprite) {
        if (image != null) {
            image.sprite = sprite;
        }
    }
    
    /// <summary>
    /// Adds a custom panel to the pause stack system.
    /// Use this for dynamically created panels.
    /// </summary>
    /// <param name="panel">The panel GameObject to add</param>
    /// <param name="canvasGroup">Optional existing CanvasGroup (will auto-create if null)</param>
    /// <param name="openDuration">Optional custom animation duration</param>
    public void OpenCustomPanel(GameObject panel, CanvasGroup canvasGroup = null, float? openDuration = null) {
        if (panel == null) return;
        
        if (canvasGroup == null) {
            canvasGroup = GetOrAddCanvasGroup(panel);
        }
        
        PushPause(panel);
    }
    
    /// <summary>
    /// Closes a custom panel from the pause stack.
    /// </summary>
    /// <param name="panel">The panel GameObject to close</param>
    public void CloseCustomPanel(GameObject panel) {
        if (panel == null || !openPanels.Contains(panel)) return;
        PopPause(panel);
    }
    
    #endregion
}

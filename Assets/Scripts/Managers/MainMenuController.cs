using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controller for the Main Menu scene.
/// Handles menu navigation, game mode selection, and persistent data display.
/// </summary>
public class MainMenuController : MonoBehaviour {
    
    [Header("Menu Panels")]
    public GameObject mainPanel;
    public GameObject playPanel;
    public GameObject optionsPanel;
    public GameObject masteryPanel;
    public GameObject dailyRunPanel;
    public GameObject creditsPanel;

    [Header("Main Menu Buttons")]
    public Button playButton;
    public Button dailyRunButton;
    public Button masteryButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Play Panel")]
    public Button newGameButton;
    public Button continueButton;
    public Button backFromPlayButton;

    [Header("Daily Run Panel")]
    public TextMeshProUGUI dailyRunSeedText;
    public TextMeshProUGUI dailyRunModifiersText;
    public Button startDailyRunButton;
    public Button backFromDailyRunButton;
    public GameObject dailyRunLeaderboardEntryPrefab;
    public Transform dailyRunLeaderboardContainer;

    [Header("Mastery Panel")]
    public GameObject masteryEntryPrefab;
    public Transform masteryListContainer;
    public Button backFromMasteryButton;

    [Header("Options Panel")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Button backFromOptionsButton;

    [Header("Player Stats Display")]
    public TextMeshProUGUI totalKillsText;
    public TextMeshProUGUI highWaveText;
    public TextMeshProUGUI totalGoldText;

    [Header("Animation")]
    public float panelTransitionDuration = 0.3f;

    // State
    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Start() {
        InitializeButtons();
        ShowPanel(mainPanel);
        UpdatePlayerStatsDisplay();
        UpdateContinueButton();
        
        // Ensure time is normal in menu
        Time.timeScale = 1f;
        
        Debug.Log("[MainMenuController] Main menu initialized");
    }

    void OnEnable() {
        // Refresh data when returning to menu
        UpdatePlayerStatsDisplay();
        UpdateContinueButton();
    }

    void InitializeButtons() {
        // Main panel
        if (playButton != null)
            playButton.onClick.AddListener(() => ShowPanel(playPanel));
        
        if (dailyRunButton != null)
            dailyRunButton.onClick.AddListener(OnDailyRunClicked);
        
        if (masteryButton != null)
            masteryButton.onClick.AddListener(OnMasteryClicked);
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => ShowPanel(optionsPanel));
        
        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => ShowPanel(creditsPanel));
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Play panel
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        if (backFromPlayButton != null)
            backFromPlayButton.onClick.AddListener(GoBack);

        // Daily Run panel
        if (startDailyRunButton != null)
            startDailyRunButton.onClick.AddListener(OnStartDailyRunClicked);
        
        if (backFromDailyRunButton != null)
            backFromDailyRunButton.onClick.AddListener(GoBack);

        // Mastery panel
        if (backFromMasteryButton != null)
            backFromMasteryButton.onClick.AddListener(GoBack);

        // Options panel
        if (backFromOptionsButton != null)
            backFromOptionsButton.onClick.AddListener(GoBack);

        // Setup options listeners
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    void OnDestroy() {
        // Remove all listeners
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (dailyRunButton != null) dailyRunButton.onClick.RemoveAllListeners();
        if (masteryButton != null) masteryButton.onClick.RemoveAllListeners();
        if (optionsButton != null) optionsButton.onClick.RemoveAllListeners();
        if (creditsButton != null) creditsButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
        if (newGameButton != null) newGameButton.onClick.RemoveAllListeners();
        if (continueButton != null) continueButton.onClick.RemoveAllListeners();
        if (backFromPlayButton != null) backFromPlayButton.onClick.RemoveAllListeners();
        if (startDailyRunButton != null) startDailyRunButton.onClick.RemoveAllListeners();
        if (backFromDailyRunButton != null) backFromDailyRunButton.onClick.RemoveAllListeners();
        if (backFromMasteryButton != null) backFromMasteryButton.onClick.RemoveAllListeners();
        if (backFromOptionsButton != null) backFromOptionsButton.onClick.RemoveAllListeners();
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveAllListeners();
    }

    #region Panel Navigation

    void ShowPanel(GameObject panel) {
        if (panel == null) return;

        // Hide current panel
        if (currentPanel != null) {
            currentPanel.SetActive(false);
            panelHistory.Push(currentPanel);
        }

        // Show new panel
        panel.SetActive(true);
        currentPanel = panel;

        // Panel-specific initialization
        if (panel == dailyRunPanel) {
            InitializeDailyRunPanel();
        } else if (panel == masteryPanel) {
            InitializeMasteryPanel();
        } else if (panel == optionsPanel) {
            InitializeOptionsPanel();
        }

        Debug.Log($"[MainMenuController] Showing panel: {panel.name}");
    }

    void GoBack() {
        if (panelHistory.Count > 0) {
            GameObject previousPanel = panelHistory.Pop();
            
            if (currentPanel != null)
                currentPanel.SetActive(false);
            
            previousPanel.SetActive(true);
            currentPanel = previousPanel;
        } else {
            // No history, go to main
            ShowPanel(mainPanel);
        }
    }

    #endregion

    #region Button Handlers

    void OnNewGameClicked() {
        Debug.Log("[MainMenuController] Starting new game");
        
        // Clear any saved run
        SaveLoadManager.Instance?.ClearActiveRun();
        
        // Go to game scene
        SceneTransitionManager.Instance?.GoToGame();
    }

    void OnContinueClicked() {
        Debug.Log("[MainMenuController] Continuing saved run");
        
        // Check if we have a valid saved run
        if (SaveLoadManager.Instance?.HasActiveRun() == true) {
            var runData = SaveLoadManager.Instance.LoadActiveRun();
            if (runData != null) {
                Debug.Log($"[MainMenuController] Continuing from wave {runData.wave}, {runData.lives} lives");
            }
            
            // Go to game scene - GameManager will load the saved state
            SceneTransitionManager.Instance?.GoToGame();
        } else {
            Debug.LogWarning("[MainMenuController] No saved run to continue!");
            // Disable continue button if no save
            UpdateContinueButton();
        }
    }

    void OnDailyRunClicked() {
        ShowPanel(dailyRunPanel);
    }

    void OnStartDailyRunClicked() {
        Debug.Log("[MainMenuController] Starting daily run");
        SceneTransitionManager.Instance?.GoToDailyRun();
    }

    void OnMasteryClicked() {
        ShowPanel(masteryPanel);
    }

    void OnQuitClicked() {
        Debug.Log("[MainMenuController] Quitting game");
        SceneTransitionManager.Instance?.QuitGame();
    }

    #endregion

    #region Panel Initialization

    void InitializeDailyRunPanel() {
        // Generate today's run info
        DailyRunManager dailyRun = DailyRunManager.Instance;
        if (dailyRun != null) {
            dailyRun.GenerateDailyRun();
            
            if (dailyRunSeedText != null)
                dailyRunSeedText.text = $"Seed: {dailyRun.currentSeed}";
            
            if (dailyRunModifiersText != null)
                dailyRunModifiersText.text = dailyRun.GetModifierSummary();
        }

        // Populate leaderboard from SaveLoadManager
        PopulateDailyRunLeaderboard();
    }

    void PopulateDailyRunLeaderboard() {
        if (dailyRunLeaderboardContainer == null) return;

        // Clear existing
        foreach (Transform child in dailyRunLeaderboardContainer) {
            Destroy(child.gameObject);
        }

        // Load from SaveLoadManager
        var results = SaveLoadManager.Instance?.LoadDailyRunLeaderboard();
        if (results == null || results.Count == 0) {
            // Show "No runs yet" message
            return;
        }

        // Show top 10 results
        int count = Mathf.Min(10, results.Count);
        for (int i = 0; i < count; i++) {
            var result = results[i];
            
            if (dailyRunLeaderboardEntryPrefab != null) {
                GameObject entry = Instantiate(dailyRunLeaderboardEntryPrefab, dailyRunLeaderboardContainer);
                
                // Set entry data (assuming specific child structure)
                var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4) {
                    texts[0].text = $"#{i + 1}";
                    texts[1].text = $"Wave {result.waveReached}";
                    texts[2].text = $"{result.enemiesKilled} kills";
                    texts[3].text = result.victory ? "VICTORY" : "DEFEAT";
                }
            }
        }
    }

    void InitializeMasteryPanel() {
        if (masteryListContainer == null) return;

        // Clear existing
        foreach (Transform child in masteryListContainer) {
            Destroy(child.gameObject);
        }

        // Load mastery data from SaveLoadManager first
        if (SaveLoadManager.Instance != null && WeaponMasteryManager.Instance != null) {
            SaveLoadManager.Instance.LoadAllWeaponMastery(WeaponMasteryManager.Instance);
        }

        // Get mastery data
        WeaponMasteryManager mastery = WeaponMasteryManager.Instance;
        if (mastery == null) return;

        // Common weapon types
        string[] weaponTypes = new string[] { "Sword", "Axe", "Spear", "Dagger", "Bow", "Staff" };
        
        foreach (string weaponType in weaponTypes) {
            var data = mastery.GetMastery(weaponType);
            
            if (masteryEntryPrefab != null) {
                GameObject entry = Instantiate(masteryEntryPrefab, masteryListContainer);
                
                var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4) {
                    texts[0].text = weaponType;
                    texts[1].text = $"Level {data.masteryLevel}";
                    texts[2].text = $"{data.totalKills} kills";
                    texts[3].text = $"+{data.bonusDamage} DMG, +{data.bonusCrit:P0} Crit";
                }

                // Progress bar
                var slider = entry.GetComponentInChildren<Slider>();
                if (slider != null) {
                    slider.value = data.GetProgressToNextLevel();
                }
            }
        }
    }

    void InitializeOptionsPanel() {
        // Load current settings
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;
    }

    #endregion

    #region Options Handlers

    void OnMusicVolumeChanged(float value) {
        PlayerPrefs.SetFloat("MusicVolume", value);
        // Apply to audio mixer if available
    }

    void OnSFXVolumeChanged(float value) {
        PlayerPrefs.SetFloat("SFXVolume", value);
        // Apply to audio mixer if available
    }

    void OnFullscreenChanged(bool fullscreen) {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
    }

    #endregion

    #region UI Updates

    void UpdatePlayerStatsDisplay() {
        // Get stats from SaveLoadManager
        var stats = SaveLoadManager.Instance?.GetCumulativeStats() ?? new CumulativeStats();

        if (totalKillsText != null)
            totalKillsText.text = $"Total Kills: {stats.totalKills:N0}";
        
        if (highWaveText != null)
            highWaveText.text = $"Best Wave: {stats.highWave}";
        
        if (totalGoldText != null)
            totalGoldText.text = $"Gold Collected: {stats.totalGold:N0}";
    }

    void UpdateContinueButton() {
        if (continueButton != null) {
            bool hasSavedRun = SaveLoadManager.Instance?.HasActiveRun() ?? false;
            continueButton.interactable = hasSavedRun;
        }
    }

    #endregion
}

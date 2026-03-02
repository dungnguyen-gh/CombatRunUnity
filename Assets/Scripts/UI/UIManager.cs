using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central UI controller managing HUD updates, panel toggling, notifications, and skill cooldown displays.
/// Persistent singleton that survives scene loads.
/// Features: Pause stack system, notification management, panel animations
/// </summary>
public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public GameObject hudPanel;
    public TextMeshProUGUI healthText;
    public UnityEngine.UI.Slider healthSlider;
    public TextMeshProUGUI goldText;
    public UnityEngine.UI.Image[] skillIcons;
    public UnityEngine.UI.Image[] skillCooldownOverlays;
    public TextMeshProUGUI[] skillCooldownTexts;

    [Header("Panels")]
    public GameObject inventoryPanel;
    public GameObject shopPanel;
    public GameObject pausePanel;

    [Header("Notifications")]
    public GameObject notificationPrefab;
    public Transform notificationParent;
    public float notificationDuration = 2f;
    public float notificationSpacing = 60f;

    [Header("Panel Animation")]
    public float panelFadeDuration = 0.15f;
    public AnimationCurve panelAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    public PlayerController player;
    public SkillCaster skillCaster;

    private List<GameObject> activeNotifications = new List<GameObject>();
    private bool isQuitting = false;

    // Pause stack system to handle overlapping panels
    private int pauseDepth = 0;
    private List<GameObject> openPanels = new List<GameObject>();

    // FIX: Cached CanvasGroup references for panels to avoid repeated GetComponent calls
    private CanvasGroup inventoryPanelCanvasGroup;
    private CanvasGroup shopPanelCanvasGroup;
    private CanvasGroup pausePanelCanvasGroup;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        FindReferences();
        SubscribeToEvents();
        InitializeHUD();
        
        // FIX: Cache CanvasGroup references and initialize panels
        // Hide panels initially
        if (inventoryPanel != null) {
            inventoryPanel.SetActive(false);
            inventoryPanelCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (inventoryPanelCanvasGroup == null) inventoryPanelCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            inventoryPanelCanvasGroup.alpha = 0f;
        }
        if (shopPanel != null) {
            shopPanel.SetActive(false);
            shopPanelCanvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (shopPanelCanvasGroup == null) shopPanelCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
            shopPanelCanvasGroup.alpha = 0f;
        }
        if (pausePanel != null) {
            pausePanel.SetActive(false);
            pausePanelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (pausePanelCanvasGroup == null) pausePanelCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
            pausePanelCanvasGroup.alpha = 0f;
        }
    }

    void OnDestroy() {
        if (!isQuitting) {
            UnsubscribeFromEvents();
        }
    }

    void OnApplicationQuit() {
        isQuitting = true;
    }

    /// <summary>
    /// Finds required references if not assigned in inspector.
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
    /// </summary>
    void SubscribeToEvents() {
        if (player != null) {
            player.OnHealthChanged += UpdateHealth;
            player.OnGoldChanged += UpdateGold;
        }
    }

    /// <summary>
    /// Unsubscribes from player events to prevent memory leaks.
    /// </summary>
    void UnsubscribeFromEvents() {
        if (player != null) {
            player.OnHealthChanged -= UpdateHealth;
            player.OnGoldChanged -= UpdateGold;
        }
    }

    void Update() {
        // Update skill cooldowns
        UpdateSkillCooldowns();
        
        // Note: Input handling (I for Inventory, Escape for Pause) 
        // is now handled by PlayerController using the new Input System
    }

    public void HandleEscapeKey() {
        // If panels are open, close the most recent one
        if (openPanels.Count > 0) {
            CloseMostRecentPanel();
        } else {
            TogglePause();
        }
    }

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

    void UpdateHealth(int current, int max) {
        if (healthSlider != null) {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null) {
            healthText.text = $"{current}/{max}";
        }
    }

    void UpdateGold(int gold) {
        if (goldText != null) {
            goldText.text = gold.ToString();
        }
    }

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

    #region Pause Stack System

    void PushPause(GameObject panel) {
        if (pauseDepth == 0) {
            Time.timeScale = 0f;
        }
        pauseDepth++;
        openPanels.Add(panel);
        
        // Animate panel open
        StartCoroutine(AnimatePanel(panel, true));
    }

    void PopPause(GameObject panel) {
        if (!openPanels.Contains(panel)) return;
        
        openPanels.Remove(panel);
        pauseDepth = Mathf.Max(0, pauseDepth - 1);
        
        if (pauseDepth == 0) {
            Time.timeScale = 1f;
        }
        
        // Animate panel close
        StartCoroutine(AnimatePanel(panel, false));
    }

    IEnumerator AnimatePanel(GameObject panel, bool opening) {
        if (panel == null) yield break;
        
        // FIX: Use cached CanvasGroup reference if available
        CanvasGroup canvasGroup = null;
        if (panel == inventoryPanel) canvasGroup = inventoryPanelCanvasGroup;
        else if (panel == shopPanel) canvasGroup = shopPanelCanvasGroup;
        else if (panel == pausePanel) canvasGroup = pausePanelCanvasGroup;
        
        // Fallback to GetComponent if not cached
        if (canvasGroup == null) {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
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

    public void ToggleInventory() {
        if (inventoryPanel == null) return;
        
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

    public void ToggleShop() {
        if (shopPanel == null) return;
        
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

    public void TogglePause() {
        if (pausePanel == null) return;
        
        bool isActive = pausePanel.activeSelf;
        
        if (!isActive) {
            PushPause(pausePanel);
        } else {
            PopPause(pausePanel);
        }
    }

    /// <summary>
    /// Shows a notification message on screen.
    /// </summary>
    public void ShowNotification(string message) {
        if (notificationPrefab == null || notificationParent == null) {
            Debug.Log(message);
            return;
        }

        GameObject notif = Instantiate(notificationPrefab, notificationParent);
        
        // FIX: Add null check for GetComponentInChildren
        var text = notif.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = message;

        activeNotifications.Add(notif);

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
        for (int i = 0; i < activeNotifications.Count; i++) {
            if (activeNotifications[i] != null) {
                var rect = activeNotifications[i].GetComponent<RectTransform>();
                if (rect != null) {
                    // Animate to new position
                    StartCoroutine(AnimateNotificationPosition(rect, i));
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

    public void ResumeGame() {
        // Close all panels
        while (openPanels.Count > 0) {
            GameObject panel = openPanels[openPanels.Count - 1];
            PopPause(panel);
        }
    }

    public void QuitGame() {
        ResumeGame();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Checks if any UI panel is currently open.
    /// </summary>
    public bool IsAnyPanelOpen() {
        return openPanels.Count > 0;
    }

    /// <summary>
    /// Gets the current game pause state.
    /// </summary>
    public bool IsGamePaused() {
        return pauseDepth > 0;
    }

    #region Lives & Revive UI

    [Header("Lives & Game Over")]
    public GameObject revivePanel;
    public TextMeshProUGUI reviveCountdownText;
    public GameObject gameOverPanel;

    /// <summary>
    /// Shows the revive countdown UI.
    /// </summary>
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
            remaining -= Time.deltaTime;
            yield return null;
        }
        
        // Hide revive panel
        if (revivePanel != null) {
            revivePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the game over UI.
    /// </summary>
    public void ShowGameOver() {
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(true);
        } else {
            ShowNotification("GAME OVER!");
        }
    }

    #endregion
}

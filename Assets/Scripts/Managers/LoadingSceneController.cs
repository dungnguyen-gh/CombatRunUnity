using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller for the loading scene.
/// Displays loading progress and tips while the next scene loads.
/// </summary>
public class LoadingSceneController : MonoBehaviour {
    
    [Header("UI References")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI loadingTipText;
    public TextMeshProUGUI sceneNameText;

    [Header("Tips")]
    public string[] loadingTips = new string[] {
        "Tip: Combine skills in sequence to create powerful synergies!",
        "Tip: Freeze enemies first, then shock them for massive damage!",
        "Tip: Complete equipment sets for powerful bonuses!",
        "Tip: Master a weapon type to unlock permanent stat bonuses!",
        "Tip: Use the Daily Run to compete with other players!",
        "Tip: Dodge rolling makes you invulnerable for a short time!",
        "Tip: Burn and Poison effects stack for explosive results!",
        "Tip: Enemy elites have special abilities - watch for telegraphs!"
    };

    [Header("Animation")]
    public float tipChangeInterval = 3f;
    public float progressBarSmoothing = 0.1f;

    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float tipTimer = 0f;
    private int currentTipIndex = 0;

    void Start() {
        // Show random initial tip
        if (loadingTips.Length > 0 && loadingTipText != null) {
            currentTipIndex = Random.Range(0, loadingTips.Length);
            loadingTipText.text = loadingTips[currentTipIndex];
        }

        // Show target scene name
        if (sceneNameText != null && SceneTransitionManager.Instance != null) {
            sceneNameText.text = $"Loading {SceneTransitionManager.Instance.GetCurrentSceneName()}...";
        }

        // Initialize progress
        UpdateProgressUI(0f);
    }

    void Update() {
        // Smooth progress bar
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, progressBarSmoothing);
        UpdateProgressUI(currentProgress);

        // Rotate through tips
        tipTimer += Time.unscaledDeltaTime;
        if (tipTimer >= tipChangeInterval) {
            tipTimer = 0f;
            ShowNextTip();
        }
    }

    /// <summary>
    /// Updates the target progress (called by SceneTransitionManager).
    /// </summary>
    public void UpdateProgress(float progress) {
        targetProgress = Mathf.Clamp01(progress);
    }

    void UpdateProgressUI(float progress) {
        if (progressBar != null) {
            progressBar.value = progress;
        }

        if (progressText != null) {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }

    void ShowNextTip() {
        if (loadingTips.Length == 0 || loadingTipText == null) return;

        currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
        loadingTipText.text = loadingTips[currentTipIndex];
    }
}

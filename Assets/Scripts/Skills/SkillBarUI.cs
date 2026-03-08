using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// SkillBarUI - Displays the player's 4 skill slots with cooldowns, icons, and key bindings.
/// 
/// === SETUP GUIDE ===
/// 
/// 1. BASIC SETUP:
///    - Add this component to your UI canvas (usually under HUD)
///    - Assign skillSlotPrefab (should have SkillSlotUI component or child Image named "Icon")
///    - Set skillSlotContainer to a Transform that will hold the 4 skill slots
/// 
/// 2. AUTO-DISCOVERY (Recommended):
///    - Leave skillCaster unassigned - it will auto-find SkillCaster in the scene
///    - Leave skillSlotContainer unassigned - it will search for "SkillSlots" child
///    - Enable autoDiscoverCaster and autoDiscoverContainer in inspector
/// 
/// 3. SKILL SLOT PREFAB STRUCTURE:
///    Required children (names can vary):
///    - "Icon" or "SkillIcon" (Image) - displays skill icon
///    - "Cooldown" or "Overlay" (Image with Filled type) - shows cooldown fill
///    - "Key" or "Binding" (TextMeshProUGUI) - shows key binding (1,2,3,4)
///    - "Time" or "CooldownText" (TextMeshProUGUI) - shows remaining cooldown
///    - "Border" or "Frame" (Image) - colored border based on rarity
/// 
/// 4. EMPTY SLOTS:
///    - Slots without skills show "No Skill" with gray border
///    - Cooldown overlay is hidden for empty slots
/// 
/// 5. COOLDOWN DISPLAY:
///    - Radial fill overlay shows remaining cooldown
///    - Text shows seconds remaining (e.g., "3.5")
///    - Icon grays out while on cooldown
/// </summary>
public class SkillBarUI : MonoBehaviour {
    [Header("References")]
    [Tooltip("SkillCaster to display. If null, will auto-discover.")]
    public SkillCaster skillCaster;
    
    [Header("UI Container")]
    [Tooltip("Container for skill slots. If null, will search for 'SkillSlots' child.")]
    public Transform skillSlotContainer;
    
    [Header("Prefabs")]
    [Tooltip("Prefab for each skill slot. Must have Image component or SkillSlotUI.")]
    public GameObject skillSlotPrefab;
    
    [Header("Auto-Discovery")]
    [Tooltip("Automatically find SkillCaster in scene on Start")]
    public bool autoDiscoverCaster = true;
    [Tooltip("Automatically find skill slot container by name")]
    public bool autoDiscoverContainer = true;
    [Tooltip("Name to search for when auto-discovering container")]
    public string containerName = "SkillSlots";
    
    [Header("Empty Slot Visuals")]
    [Tooltip("Sprite to show for empty slots")]
    public Sprite emptySlotSprite;
    [Tooltip("Color for empty slot border")]
    public Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [Tooltip("Color for empty slot icon")]
    public Color emptySlotIconColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    [Tooltip("Text to show for empty slot key binding")]
    public string emptySlotText = "";
    
    private List<SkillSlotUI> skillSlots = new List<SkillSlotUI>();
    private bool isInitialized = false;
    private bool isBound = false;

    void Start() {
        FindReferences();
        InitializeSlots();
        BindToCaster();
    }

    void OnEnable() {
        if (isInitialized) {
            BindToCaster();
        }
    }

    void OnDisable() {
        UnbindFromCaster();
    }
    
    void OnDestroy() {
        UnbindFromCaster();
    }
    
    /// <summary>
    /// Finds required references using auto-discovery if enabled.
    /// </summary>
    void FindReferences() {
        // Find SkillCaster
        if (skillCaster == null && autoDiscoverCaster) {
            skillCaster = FindFirstObjectByType<SkillCaster>();
            if (skillCaster == null) {
                Debug.LogWarning("[SkillBarUI] No SkillCaster found in scene! Skill bar will not function.");
            } else if (Application.isEditor) {
                Debug.Log($"[SkillBarUI] Auto-discovered SkillCaster on '{skillCaster.gameObject.name}'");
            }
        }
        
        // Find container
        if (skillSlotContainer == null && autoDiscoverContainer) {
            skillSlotContainer = FindDeepChild(transform, containerName);
            if (skillSlotContainer == null && transform.name.Contains(containerName)) {
                skillSlotContainer = transform;
            }
        }
        
        // Validate
        if (skillSlotContainer == null) {
            Debug.LogError("[SkillBarUI] No skillSlotContainer assigned or found! Please assign in inspector.");
        }
    }

    void InitializeSlots() {
        if (skillSlotContainer == null) return;
        
        // Clear existing
        foreach (Transform child in skillSlotContainer) {
            Destroy(child.gameObject);
        }
        skillSlots.Clear();
        
        // Create slots for each skill (always 4 slots for keys 1-4)
        for (int i = 0; i < 4; i++) {
            CreateSlot(i);
        }
        
        // Set initial skill data
        RefreshAllSlots();
        isInitialized = true;
    }
    
    /// <summary>
    /// Creates a single skill slot UI.
    /// </summary>
    void CreateSlot(int index) {
        GameObject slotObj = null;
        
        if (skillSlotPrefab != null) {
            slotObj = Instantiate(skillSlotPrefab, skillSlotContainer);
        } else {
            // Create default slot if no prefab
            slotObj = CreateDefaultSlot();
            slotObj.transform.SetParent(skillSlotContainer, false);
        }
        
        if (slotObj == null) {
            Debug.LogError($"[SkillBarUI] Failed to create slot {index + 1}");
            return;
        }
        
        // Get or add SkillSlotUI
        var slotUI = slotObj.GetComponent<SkillSlotUI>();
        if (slotUI == null) {
            slotUI = slotObj.AddComponent<SkillSlotUI>();
        }
        
        // Initialize slot
        slotUI.AutoBindComponents();
        slotUI.SetSlotIndex(index);
        slotUI.SetKeyBinding((index + 1).ToString());
        
        // Set empty slot visuals initially
        slotUI.SetEmptyVisuals(emptySlotSprite, emptySlotColor, emptySlotIconColor);
        
        skillSlots.Add(slotUI);
    }
    
    /// <summary>
    /// Creates a default slot GameObject if no prefab is assigned.
    /// </summary>
    GameObject CreateDefaultSlot() {
        var go = new GameObject("SkillSlot", typeof(RectTransform));
        
        // Add background image
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        // Add icon image
        var icon = new GameObject("Icon", typeof(Image));
        icon.transform.SetParent(go.transform, false);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;
        
        // Add cooldown overlay
        var cooldown = new GameObject("Cooldown", typeof(Image));
        cooldown.transform.SetParent(go.transform, false);
        var cdRect = cooldown.GetComponent<RectTransform>();
        cdRect.anchorMin = Vector2.zero;
        cdRect.anchorMax = Vector2.one;
        cdRect.sizeDelta = Vector2.zero;
        var cdImg = cooldown.GetComponent<Image>();
        cdImg.color = new Color(0, 0, 0, 0.7f);
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Radial360;
        cdImg.fillClockwise = false;
        cdImg.fillAmount = 0;
        
        // Add border
        var border = new GameObject("Border", typeof(Image));
        border.transform.SetParent(go.transform, false);
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        var borderImg = border.GetComponent<Image>();
        borderImg.color = Color.gray;
        borderImg.type = Image.Type.Sliced;
        
        // Add key text
        var keyObj = new GameObject("Key", typeof(TextMeshProUGUI));
        keyObj.transform.SetParent(go.transform, false);
        var keyRect = keyObj.GetComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.zero;
        keyRect.sizeDelta = new Vector2(30, 30);
        keyRect.anchoredPosition = new Vector2(15, 15);
        var keyText = keyObj.GetComponent<TextMeshProUGUI>();
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.fontSize = 14;
        keyText.color = Color.white;
        
        // Add cooldown text
        var cdTextObj = new GameObject("CooldownText", typeof(TextMeshProUGUI));
        cdTextObj.transform.SetParent(go.transform, false);
        var cdTextRect = cdTextObj.GetComponent<RectTransform>();
        cdTextRect.anchorMin = Vector2.zero;
        cdTextRect.anchorMax = Vector2.one;
        cdTextRect.sizeDelta = Vector2.zero;
        var cdText = cdTextObj.GetComponent<TextMeshProUGUI>();
        cdText.alignment = TextAlignmentOptions.Center;
        cdText.fontSize = 18;
        cdText.color = Color.white;
        cdText.fontStyle = FontStyles.Bold;
        
        return go;
    }

    void BindToCaster() {
        if (skillCaster == null || isBound) return;
        
        UnbindFromCaster();
        
        skillCaster.OnCooldownStarted += OnCooldownStarted;
        skillCaster.OnCooldownUpdated += OnCooldownUpdated;
        skillCaster.OnSkillCast += OnSkillCast;
        skillCaster.OnSkillCharging += OnSkillCharging;
        skillCaster.OnSkillReleased += OnSkillReleased;
        skillCaster.OnSkillFailed += OnSkillFailed;
        
        isBound = true;
        
        // Refresh slots to show current skills
        RefreshAllSlots();
    }

    void UnbindFromCaster() {
        if (skillCaster == null || !isBound) return;
        
        skillCaster.OnCooldownStarted -= OnCooldownStarted;
        skillCaster.OnCooldownUpdated -= OnCooldownUpdated;
        skillCaster.OnSkillCast -= OnSkillCast;
        skillCaster.OnSkillCharging -= OnSkillCharging;
        skillCaster.OnSkillReleased -= OnSkillReleased;
        skillCaster.OnSkillFailed -= OnSkillFailed;
        
        isBound = false;
    }

    void Update() {
        // Continuous update for smooth cooldown fill and availability
        if (skillCaster != null && skillSlots.Count == 4) {
            for (int i = 0; i < skillSlots.Count; i++) {
                // Update cooldown fill
                float percent = skillCaster.GetCooldownPercent(i);
                skillSlots[i].UpdateCooldownFill(percent);
                
                // Check if skill available
                bool canCast = skillCaster.CanCastSkill(i);
                skillSlots[i].SetAvailable(canCast);
            }
        }
    }

    #region Event Handlers

    void OnCooldownStarted(int slot, float cooldown) {
        if (slot < skillSlots.Count) {
            skillSlots[slot].StartCooldown(cooldown);
        }
    }

    void OnCooldownUpdated(int slot, float remaining) {
        if (slot < skillSlots.Count) {
            skillSlots[slot].SetCooldownText(remaining);
        }
    }

    void OnSkillCast(int slot) {
        if (slot < skillSlots.Count) {
            skillSlots[slot].TriggerCastAnimation();
        }
    }

    void OnSkillCharging(int slot) {
        if (slot < skillSlots.Count) {
            skillSlots[slot].SetCharging(true);
        }
    }

    void OnSkillReleased(int slot) {
        if (slot < skillSlots.Count) {
            skillSlots[slot].SetCharging(false);
        }
    }
    
    void OnSkillFailed(int slot, string reason) {
        if (slot < skillSlots.Count) {
            skillSlots[slot].ShowError(reason);
        }
    }

    #endregion

    void RefreshAllSlots() {
        if (skillCaster == null) return;
        
        for (int i = 0; i < skillSlots.Count; i++) {
            var skill = skillCaster.GetSkill(i);
            if (skill != null) {
                skillSlots[i].SetSkill(skill);
            } else {
                skillSlots[i].SetNoSkill(emptySlotText);
            }
        }
    }

    Transform FindDeepChild(Transform parent, string name) {
        if (parent == null) return null;
        
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) {
            if (child.name.Contains(name)) {
                return child;
            }
        }
        return null;
    }
}

/// <summary>
/// Individual skill slot UI with auto-binding to components.
/// </summary>
public class SkillSlotUI : MonoBehaviour {
    private Image iconImage;
    private Image cooldownOverlay;
    private Image borderImage;
    private TextMeshProUGUI keyText;
    private TextMeshProUGUI cooldownText;
    private Animator animator;
    private CanvasGroup canvasGroup;
    
    private int slotIndex = -1;
    private bool isCharging = false;
    private SkillSO currentSkill;
    
    // Visual settings
    private Sprite emptySlotSprite;
    private Color emptySlotColor;
    private Color emptySlotIconColor;

    void Awake() {
        AutoBindComponents();
    }

    /// <summary>
    /// Automatically finds child components by common naming patterns.
    /// Call this if the slot prefab structure changes at runtime.
    /// </summary>
    public void AutoBindComponents() {
        // Find by common naming patterns
        iconImage = FindComponentByName<Image>("Icon", "SkillIcon", "Skill Sprite");
        cooldownOverlay = FindComponentByName<Image>("Cooldown", "Overlay", "Darken", "CD Overlay");
        borderImage = FindComponentByName<Image>("Border", "Frame", "Outline", "Edge");
        keyText = FindComponentByName<TextMeshProUGUI>("Key", "Binding", "Number", "KeyText");
        cooldownText = FindComponentByName<TextMeshProUGUI>("Time", "CooldownText", "CD Text", "Timer");
        animator = GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Set up cooldown overlay
        SetupCooldownOverlay();
    }
    
    void SetupCooldownOverlay() {
        if (cooldownOverlay != null) {
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = false;
            cooldownOverlay.fillAmount = 0;
        }
    }

    T FindComponentByName<T>(params string[] names) where T : Component {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (var comp in components) {
            string compName = comp.gameObject.name.ToLower();
            foreach (var searchName in names) {
                if (!string.IsNullOrEmpty(searchName) && compName.Contains(searchName.ToLower())) {
                    return comp;
                }
            }
        }
        return GetComponentInChildren<T>(true);
    }

    public void SetSlotIndex(int index) {
        slotIndex = index;
    }

    public void SetKeyBinding(string key) {
        if (keyText != null) {
            keyText.text = key;
        }
    }
    
    /// <summary>
    /// Sets the visual settings for empty slots.
    /// </summary>
    public void SetEmptyVisuals(Sprite sprite, Color borderColor, Color iconColor) {
        emptySlotSprite = sprite;
        emptySlotColor = borderColor;
        emptySlotIconColor = iconColor;
    }

    public void SetSkill(SkillSO skill) {
        currentSkill = skill;
        
        if (skill == null) {
            SetNoSkill();
            return;
        }
        
        // Set icon
        if (iconImage != null) {
            iconImage.sprite = skill.icon != null ? skill.icon : emptySlotSprite;
            iconImage.color = skill.icon != null ? Color.white : emptySlotIconColor;
            iconImage.enabled = true;
        }
        
        // Set border color based on rarity
        if (borderImage != null) {
            borderImage.color = skill.GetRarityColor();
            borderImage.enabled = true;
        }
        
        // Show the slot
        if (canvasGroup != null) {
            canvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// Sets the slot to show "No Skill" state.
    /// </summary>
    public void SetNoSkill(string message = "") {
        currentSkill = null;
        
        // Clear icon or show placeholder
        if (iconImage != null) {
            iconImage.sprite = emptySlotSprite;
            iconImage.color = emptySlotIconColor;
        }
        
        // Gray border
        if (borderImage != null) {
            borderImage.color = emptySlotColor;
        }
        
        // Clear cooldown
        if (cooldownOverlay != null) {
            cooldownOverlay.fillAmount = 0;
        }
        if (cooldownText != null) {
            cooldownText.text = "";
        }
    }

    /// <summary>
    /// Updates the cooldown fill amount.
    /// </summary>
    /// <param name="percent">0 = on cooldown, 1 = ready</param>
    public void UpdateCooldownFill(float percent) {
        if (cooldownOverlay != null) {
            // Invert so 1 = full overlay (on cooldown), 0 = no overlay (ready)
            cooldownOverlay.fillAmount = 1f - percent;
        }
    }

    public void StartCooldown(float duration) {
        if (cooldownOverlay != null) {
            cooldownOverlay.fillAmount = 1;
        }
    }

    public void SetCooldownText(float remaining) {
        if (cooldownText != null) {
            if (remaining > 0.1f) {
                // Show 1 decimal place for values < 10, whole numbers for > 10
                cooldownText.text = remaining < 10 ? remaining.ToString("F1") : Mathf.CeilToInt(remaining).ToString();
            } else {
                cooldownText.text = "";
            }
        }
    }

    public void SetAvailable(bool available) {
        if (iconImage != null) {
            // Gray out icon when not available (on cooldown)
            iconImage.color = available ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
    }

    public void SetCharging(bool charging) {
        isCharging = charging;
        if (borderImage != null) {
            borderImage.color = charging ? Color.yellow : 
                (currentSkill != null ? currentSkill.GetRarityColor() : emptySlotColor);
        }
    }

    public void TriggerCastAnimation() {
        if (animator != null) {
            animator.SetTrigger("Cast");
        } else {
            StartCoroutine(PunchScale());
        }
    }
    
    /// <summary>
    /// Shows a brief error visual when skill fails.
    /// </summary>
    public void ShowError(string reason) {
        if (borderImage != null) {
            StartCoroutine(FlashError());
        }
    }
    
    System.Collections.IEnumerator FlashError() {
        Color originalColor = borderImage != null ? borderImage.color : Color.white;
        
        if (borderImage != null) {
            borderImage.color = Color.red;
        }
        
        yield return new WaitForSeconds(0.2f);
        
        if (borderImage != null) {
            borderImage.color = originalColor;
        }
    }

    System.Collections.IEnumerator PunchScale() {
        Vector3 originalScale = transform.localScale;
        float duration = 0.15f;
        float elapsed = 0;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1 + Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}

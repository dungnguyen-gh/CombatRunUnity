# UI Setup Guide for Combat Run

A comprehensive guide for setting up and integrating the UI system in the Combat Run Unity project.

## Table of Contents

1. [Quick Start](#quick-start)
2. [UIManager Setup](#uimanager-setup)
3. [Panel Setup](#panel-setup)
4. [UIPanel Component](#uipanel-component)
5. [Inventory UI Setup](#inventory-ui-setup)
6. [Shop UI Setup](#shop-ui-setup)
7. [Player-UI Integration](#player-ui-integration)
8. [Manager-UI Integration](#manager-ui-integration)
9. [Adding New Panels](#adding-new-panels)
10. [Gamepad Navigation](#gamepad-navigation)
11. [Sound Effects](#sound-effects)
12. [Troubleshooting](#troubleshooting)

---

## Quick Start

For a basic setup in a new scene:

1. Create a Canvas (Screen Space - Overlay)
2. Create a UIManager GameObject
3. Add the UIManager component
4. Create HUD, Inventory, Shop, and Pause panels
5. Assign references in the UIManager inspector

---

## UIManager Setup

### Step 1: Create the UIManager GameObject

1. In your scene, create an empty GameObject named **"UIManager"**
2. Add the `UIManager.cs` script to this GameObject
3. The UIManager is a **singleton** and will persist across scene loads (Don'tDestroyOnLoad)

### Step 2: Configure UIManager References

#### HUD Section
| Field | Required | Description |
|-------|----------|-------------|
| `hudPanel` | Yes | Main HUD GameObject (always visible during gameplay) |
| `healthText` | Yes | TextMeshProUGUI showing "current/max" HP |
| `healthSlider` | Yes | Slider for health bar visualization |
| `goldText` | Yes | TextMeshProUGUI showing current gold |
| `skillIcons` | No | Array of 4 Images for skill icons |
| `skillCooldownOverlays` | No | Array of 4 Images for cooldown fill |
| `skillCooldownTexts` | No | Array of 4 TextMeshProUGUI for cooldown numbers |

#### Panels Section
| Field | Required | Description |
|-------|----------|-------------|
| `inventoryPanel` | Yes | Inventory panel GameObject |
| `shopPanel` | Yes | Shop panel GameObject |
| `pausePanel` | Yes | Pause menu panel GameObject |

#### Notifications Section
| Field | Required | Description |
|-------|----------|-------------|
| `notificationPrefab` | No | Prefab for notification messages |
| `notificationParent` | No | Transform where notifications spawn |
| `notificationDuration` | Yes | How long notifications stay visible (default: 2s) |
| `notificationSpacing` | Yes | Vertical spacing between notifications (default: 60) |

#### Sound Effects Section
| Field | Required | Description |
|-------|----------|-------------|
| `panelOpenSound` | No | AudioClip played when any panel opens |
| `panelCloseSound` | No | AudioClip played when any panel closes |
| `buttonClickSound` | No | AudioClip played on button clicks |
| `navigationSound` | No | AudioClip played on gamepad navigation |
| `notificationSound` | No | AudioClip played when showing notification |
| `gameOverSound` | No | AudioClip played on game over |

### Step 3: Auto-Reference Finding

The UIManager will automatically find references if not assigned:
- `player`: Finds first PlayerController in scene
- `skillCaster`: Gets from player component

**Recommendation**: Assign references explicitly for better performance and reliability.

---

## Panel Setup

### Required Components for All Panels

Every UI panel needs:

1. **CanvasGroup** (auto-added if missing)
   - Controls alpha fading
   - Blocks raycasts when closed
   - Makes panel non-interactable when closed

2. **UIPanel** component (recommended)
   - Handles open/close animations
   - Manages gamepad navigation
   - Provides sound effect hooks

### Panel Hierarchy Structure

```
Canvas (Screen Space - Overlay)
├── HUD
│   ├── HealthBar
│   ├── GoldDisplay
│   └── SkillBar
├── InventoryPanel (with UIPanel component)
│   ├── InventoryGrid
│   ├── EquipmentSlots
│   └── ItemDetailPanel
├── ShopPanel (with UIPanel component)
│   ├── ShopGrid
│   └── PreviewPanel
└── PausePanel (with UIPanel component)
    ├── ResumeButton
    ├── OptionsButton
    └── QuitButton
```

---

## UIPanel Component

The `UIPanel.cs` script provides reusable panel functionality.

### Adding UIPanel to a Panel

1. Select your panel GameObject
2. Add Component → UIPanel
3. Configure settings in inspector

### UIPanel Settings

| Setting | Description |
|---------|-------------|
| `panelId` | Unique identifier (auto-set to GameObject name if empty) |
| `pausesGame` | If true, panel is added to pause stack |
| `closeableByEscape` | If true, Escape key closes this panel |
| `startHidden` | If true, panel starts inactive |
| `animationDuration` | Time for open/close animation (seconds) |
| `animationCurve` | Easing curve for animations |
| `animationType` | Animation style (Fade, Scale, Slide) |
| `firstSelected` | First selectable for gamepad navigation |
| `autoFindFirstSelectable` | Auto-find first button if none assigned |
| `openSound` | Sound played when opening |
| `closeSound` | Sound played when closing |
| `navigationSound` | Sound played on navigation |

### Animation Types

| Type | Description |
|------|-------------|
| `Fade` | Simple alpha fade in/out |
| `Scale` | Scale from 0 to 1 with fade |
| `SlideFromBottom` | Slides up from bottom of screen |
| `SlideFromTop` | Slides down from top of screen |
| `SlideFromLeft` | Slides in from left |
| `SlideFromRight` | Slides in from right |

### Using UIPanel in Code

```csharp
// Get reference
UIPanel myPanel = GetComponent<UIPanel>();

// Open with animation
myPanel.Open();

// Close with animation
myPanel.Close();

// Toggle open/closed
myPanel.Toggle();

// Instant show/hide (no animation)
myPanel.ShowInstant();
myPanel.HideInstant();

// Check state
bool isOpen = myPanel.IsOpen;
bool isAnimating = myPanel.IsAnimating;

// Subscribe to events
myPanel.OnPanelOpen += () => Debug.Log("Panel opening!");
myPanel.OnPanelOpened += () => Debug.Log("Panel opened!");
myPanel.OnPanelClose += () => Debug.Log("Panel closing!");
myPanel.OnPanelClosed += () => Debug.Log("Panel closed!");
```

---

## Inventory UI Setup

### Required Setup

1. Create a panel GameObject named "InventoryPanel"
2. Add the `InventoryUI.cs` script
3. UIPanel component is auto-added (RequiredComponent)

### Inspector Assignments

#### Inventory Grid
| Field | Assignment |
|-------|------------|
| `inventoryGrid` | Transform that will hold item slots (GridLayoutGroup recommended) |
| `itemSlotPrefab` | Prefab with Image (icon) and Button components |

#### Equipment Slots
| Field | Assignment |
|-------|------------|
| `weaponSlotIcon` | Image for equipped weapon |
| `armorSlotIcon` | Image for equipped armor |
| `weaponStatText` | TextMeshPro showing weapon stats |
| `armorStatText` | TextMeshPro showing armor stats |

#### Item Details
| Field | Assignment |
|-------|------------|
| `itemDetailPanel` | Panel shown when item selected |
| `detailIcon` | Image for item icon |
| `detailName` | TextMeshPro for item name |
| `detailDescription` | TextMeshPro for description |
| `detailStats` | TextMeshPro for stats list |
| `equipButton` | Button to equip item |
| `unequipButton` | Button to unequip item |
| `sellButton` | Button to sell item |

#### Stats Display
| Field | Assignment |
|-------|------------|
| `damageText` | Current damage stat |
| `defenseText` | Current defense stat |
| `critText` | Current crit chance |
| `attackSpeedText` | Current attack speed |

### Item Slot Prefab Structure

```
ItemSlotPrefab
├── Image (background)
└── Icon (Image component - assign to item icon)
└── Button (for click handling)
```

### Integration with InventoryManager

The InventoryUI automatically:
- Subscribes to `OnInventoryChanged` → Refreshes UI
- Subscribes to `OnItemEquipped` → Updates equipment display
- Subscribes to `OnItemUnequipped` → Updates equipment display
- Unsubscribes in `OnDestroy()` to prevent memory leaks

---

## Shop UI Setup

### Required Setup

1. Create a panel GameObject named "ShopPanel"
2. Add the `ShopUI.cs` script
3. UIPanel component is auto-added (RequiredComponent)

### Inspector Assignments

#### Shop Grid
| Field | Assignment |
|-------|------------|
| `shopGrid` | Transform for shop item slots |
| `shopSlotPrefab` | Prefab with Icon, Name, Price children |

#### Shop Slot Prefab Structure

```
ShopSlotPrefab
├── Icon (Image)
├── Name (TextMeshProUGUI)
├── Price (TextMeshProUGUI)
└── Button
```

#### Preview Panel
| Field | Assignment |
|-------|------------|
| `previewPanel` | Panel showing selected item details |
| `previewIcon` | Item icon image |
| `previewName` | Item name text |
| `previewDescription` | Item description text |
| `previewPrice` | Item price text |
| `buyButton` | Button to purchase item |

#### Stats Comparison
| Field | Assignment |
|-------|------------|
| `currentDamageText` | Current damage |
| `previewDamageText` | Damage after purchase |
| `currentDefenseText` | Current defense |
| `previewDefenseText` | Defense after purchase |
| `currentCritText` | Current crit |
| `previewCritText` | Crit after purchase |

#### Buttons
| Field | Assignment |
|-------|------------|
| `openInventoryButton` | Button to open inventory |
| `refreshButton` | Button to refresh shop |
| `closeButton` | Button to close shop |

### Integration with ShopManager

The ShopUI automatically:
- Subscribes to `OnShopRefreshed` → Refreshes shop items
- Subscribes to `OnItemPurchased` → Shows feedback
- Subscribes to `InventoryManager.OnGoldChanged` → Updates gold display

---

## Player-UI Integration

### How PlayerController Connects to UIManager

The connection is event-driven:

```
PlayerController (events) → UIManager (event handlers)
```

### Event Flow

#### Health Changes
```csharp
// In PlayerController
takeDamage() → stats.TakeDamage(damage) → OnHealthChanged?.Invoke(current, max)

// In UIManager (subscribed in SubscribeToEvents)
OnHealthChanged += UpdateHealth
UpdateHealth(current, max) → Updates slider and text
```

#### Gold Changes
```csharp
// In PlayerController
AddGold(amount) → gold += amount → OnGoldChanged?.Invoke(gold)

// In UIManager
OnGoldChanged += UpdateGold
UpdateGold(gold) → Updates gold text
```

### Input Actions

The PlayerController handles input and calls UIManager methods:

| Input | Action | UIManager Method |
|-------|--------|------------------|
| I Key | Open/Close Inventory | `ToggleInventory()` |
| Escape | Handle Pause/Back | `HandleEscapeKey()` |

```csharp
// In PlayerController.cs
void OnInventoryPerformed(InputAction.CallbackContext context) {
    UIManager.Instance?.ToggleInventory();
}

void OnPausePerformed(InputAction.CallbackContext context) {
    UIManager.Instance?.HandleEscapeKey();
}
```

### Skill Cooldown Display

Skill cooldowns are updated every frame in UIManager.Update():

```csharp
void Update() {
    UpdateSkillCooldowns();
}

void UpdateSkillCooldowns() {
    // Get cooldown info from SkillCaster
    float cooldownPercent = skillCaster.GetCooldownPercent(i);
    float cooldownRemaining = skillCaster.GetCooldownRemaining(i);
    
    // Update UI overlays and text
    skillCooldownOverlays[i].fillAmount = 1f - cooldownPercent;
    skillCooldownTexts[i].text = Mathf.CeilToInt(cooldownRemaining).ToString();
}
```

---

## Manager-UI Integration

### InventoryManager → InventoryUI

```csharp
// InventoryManager events
public System.Action OnInventoryChanged;
public System.Action<ItemSO> OnItemEquipped;
public System.Action<ItemSO> OnItemUnequipped;
public System.Action<int> OnGoldChanged;

// InventoryUI subscriptions (in Start())
inventory.OnInventoryChanged += RefreshUI;
inventory.OnItemEquipped += OnItemEquipped;
inventory.OnItemUnequipped += OnItemUnequipped;

// Cleanup (in OnDestroy()) - CRITICAL!
inventory.OnInventoryChanged -= RefreshUI;
inventory.OnItemEquipped -= OnItemEquipped;
inventory.OnItemUnequipped -= OnItemUnequipped;
```

### ShopManager → ShopUI

```csharp
// ShopManager events
public System.Action OnShopRefreshed;
public System.Action<ItemSO> OnItemPurchased;
public System.Action<ItemSO> OnItemSold;

// ShopUI subscriptions (in Start())
shop.OnShopRefreshed += RefreshShopItems;
shop.OnItemPurchased += OnItemPurchased;

// Cleanup (in OnDestroy()) - CRITICAL!
shop.OnShopRefreshed -= RefreshShopItems;
shop.OnItemPurchased -= OnItemPurchased;
```

### Memory Leak Prevention

**Always unsubscribe from events in OnDestroy()!**

```csharp
void OnDestroy() {
    if (inventory != null) {
        inventory.OnInventoryChanged -= RefreshUI;
        inventory.OnItemEquipped -= OnItemEquipped;
        inventory.OnItemUnequipped -= OnItemUnequipped;
    }
}
```

---

## Adding New Panels

### Method 1: Using UIPanel Component (Recommended)

1. Create panel GameObject
2. Add UIPanel component
3. Configure settings
4. Reference from UIManager or other script

```csharp
// Custom panel script
public class MyPanel : MonoBehaviour {
    private UIPanel uiPanel;
    
    void Awake() {
        uiPanel = GetComponent<UIPanel>();
    }
    
    void Start() {
        // Subscribe to panel events
        uiPanel.OnPanelOpened += OnOpened;
        uiPanel.OnPanelClosed += OnClosed;
    }
    
    void OnDestroy() {
        uiPanel.OnPanelOpened -= OnOpened;
        uiPanel.OnPanelClosed -= OnClosed;
    }
    
    void OnOpened() {
        // Panel finished opening
    }
    
    void OnClosed() {
        // Panel finished closing
    }
}
```

### Method 2: Direct Pause Stack Integration

```csharp
// For panels that need pause stack behavior without UIPanel
public class CustomPanel : MonoBehaviour {
    
    public void Open() {
        UIManager.Instance?.OpenCustomPanel(gameObject);
    }
    
    public void Close() {
        UIManager.Instance?.CloseCustomPanel(gameObject);
    }
}
```

### Adding to Pause Stack System

The pause stack tracks open panels and manages time scale:

```csharp
// When first panel opens
Time.timeScale = 0f; // Game pauses

// When additional panels open
// Time stays paused, panel added to stack

// When panels close
// LIFO order - last opened closes first

// When last panel closes
Time.timeScale = 1f; // Game resumes
```

---

## Gamepad Navigation

### Automatic Setup

UIPanel automatically handles gamepad navigation:

1. Set `firstSelected` to the default button
2. Enable `autoFindFirstSelectable` to auto-detect
3. Panel calls `SetupNavigation()` when opened

### Manual Navigation Setup

```csharp
// In your panel script
void SetupGamepadNavigation() {
    // Find first selectable
    Button firstButton = GetComponentInChildren<Button>();
    
    // Set as selected
    if (firstButton != null && EventSystem.current != null) {
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }
}
```

### Navigation Sound

```csharp
// Play navigation sound when selection changes
public void OnNavigate(BaseEventData eventData) {
    UIManager.Instance?.PlayNavigationSound();
}
```

### Best Practices

1. **Use explicit navigation** (not automatic) for complex layouts
2. **Group related buttons** in the same navigation area
3. **Provide visual feedback** for selected state
4. **Test with both keyboard and gamepad**

---

## Sound Effects

### UIManager Sound Methods

```csharp
// Play specific sound
UIManager.Instance?.PlaySoundEffect(myAudioClip);

// Play button click sound
UIManager.Instance?.PlayButtonClickSound();

// Play navigation sound
UIManager.Instance?.PlayNavigationSound();
```

### UIPanel Sound Integration

Assign AudioClips in UIPanel inspector:
- `openSound` - Plays when panel opens
- `closeSound` - Plays when panel closes
- `navigationSound` - Plays on navigation

### Button Click Sounds

Add to any button:

```csharp
button.onClick.AddListener(() => {
    UIManager.Instance?.PlayButtonClickSound();
    // Your action here
});
```

### Custom Audio Manager Integration

Subscribe to UIManager events:

```csharp
void Start() {
    UIManager.Instance.OnPlaySoundEffect += PlaySound;
}

void PlaySound(AudioClip clip) {
    myAudioManager.Play(clip);
}
```

---

## Troubleshooting

### Common Issues

#### Panel Doesn't Open
**Symptoms:** Nothing happens when pressing input key

**Solutions:**
1. Check UIManager instance exists: `UIManager.Instance != null`
2. Verify panel reference is assigned in UIManager inspector
3. Ensure panel has CanvasGroup component (auto-added if missing)
4. Check for null reference exceptions in console

#### Panel Opens But No Animation
**Symptoms:** Panel appears instantly without fade/scale

**Solutions:**
1. Verify `panelFadeDuration` > 0 in UIManager
2. Check `panelAnimationCurve` is set (default: EaseInOut)
3. Ensure panel has CanvasGroup component
4. Check Time.timeScale (animations use unscaledDeltaTime)

#### Events Not Firing
**Symptoms:** UI doesn't update when inventory changes

**Solutions:**
1. Check event subscriptions in Start()
2. Verify cleanup in OnDestroy() isn't removing wrong handlers
3. Ensure singleton instances exist (InventoryManager, ShopManager)
4. Add Debug.Log to event handlers for testing

#### Memory Leaks
**Symptoms:** Game slows down over time, "MissingReferenceException"

**Solutions:**
1. Always unsubscribe from events in OnDestroy()
2. Remove button listeners before destroying slots
3. Clear lists/dictionaries in OnDestroy()
4. Use Unity Profiler to track allocations

```csharp
// Bad - causes memory leak
void Start() {
    inventory.OnChanged += Refresh; // Never unsubscribed!
}

// Good - properly cleaned up
void Start() {
    inventory.OnChanged += Refresh;
}

void OnDestroy() {
    inventory.OnChanged -= Refresh;
}
```

#### Game Doesn't Pause
**Symptoms:** Game continues running when panel opens

**Solutions:**
1. Check `pausesGame` is true on UIPanel
2. Verify Time.timeScale is being set (check UIManager logs)
3. Ensure pauseDepth is incrementing (check with Debug.Log)
4. Check for other scripts modifying Time.timeScale

#### Gamepad Navigation Not Working
**Symptoms:** Can't navigate with controller

**Solutions:**
1. Ensure EventSystem exists in scene
2. Check `firstSelected` is assigned on UIPanel
3. Verify buttons have Navigation set to Automatic or Explicit
4. Test with Unity's Input Debugger

#### Null Reference Exceptions
**Symptoms:** Errors in console pointing to UI scripts

**Solutions:**
1. Check all inspector references are assigned
2. Use null-conditional operator: `text?.text = "value"`
3. Add null checks before accessing components
4. Verify manager singletons exist before accessing

```csharp
// Defensive coding pattern
void UpdateGoldDisplay(int gold) {
    if (goldText == null) {
        Debug.LogWarning("[ShopUI] goldText reference is missing!");
        return;
    }
    goldText.text = $"Gold: {gold:N0}";
}
```

### Debug Logging

Enable detailed logging to diagnose issues:

```csharp
// In UIManager
void ToggleInventory() {
    Debug.Log($"[UIManager] ToggleInventory called. Panel null: {inventoryPanel == null}");
    // ...
}

// In InventoryUI
void RefreshUI() {
    Debug.Log($"[InventoryUI] RefreshUI called. Inventory null: {inventory == null}");
    // ...
}
```

### Testing Checklist

- [ ] UIManager persists across scene loads
- [ ] All panels open/close correctly
- [ ] Animations play smoothly
- [ ] Game pauses when panels open
- [ ] Escape key works for all panels
- [ ] Notifications appear and fade correctly
- [ ] Sound effects play at correct times
- [ ] Gamepad navigation works
- [ ] No null reference exceptions
- [ ] No memory leaks (check Profiler)

---

## Quick Reference

### UIManager Public Methods

```csharp
// Panel Control
void ToggleInventory()
void ToggleShop()
void TogglePause()
void OpenInventory()
void CloseInventory()
void OpenShop()
void CloseShop()
void OpenPauseMenu()
void ClosePauseMenu()
void ResumeGame()
void QuitGame()

// Custom Panels
void OpenCustomPanel(GameObject panel, CanvasGroup cg = null, float? duration = null)
void CloseCustomPanel(GameObject panel)

// Notifications
void ShowNotification(string message)

// Game Over
void ShowReviveCountdown(float duration)
void ShowGameOver(int enemiesKilled = 0, int wavesCompleted = 0)
void RestartGame()
void QuitToMainMenu()
void HideGameOverPanel()

// State Checks
bool IsAnyPanelOpen()
bool IsGamePaused()
bool IsGameOver()
IReadOnlyList<GameObject> GetOpenPanels()

// Sound
void PlaySoundEffect(AudioClip clip)
void PlayButtonClickSound()
void PlayNavigationSound()
```

### UIPanel Public Methods

```csharp
void Open(bool playSound = true)
void Close(bool playSound = true)
void Toggle()
void ShowInstant()
void HideInstant()
void SetupNavigation()
void ClearSelection()
void PlayNavigationSound()
```

### Events

```csharp
// UIManager
OnPanelOpened(GameObject panel)
OnPanelClosed(GameObject panel)
OnPauseStateChanged(bool isPaused)
OnPlaySoundEffect(AudioClip clip)

// UIPanel
OnPanelOpen
OnPanelOpened
OnPanelClose
OnPanelClosed
OnNavigationSound
```

---

## Support

For additional help:
1. Check the console for error messages
2. Review the XML documentation in the code
3. Verify your setup against this guide
4. Check the example scenes in the project

# 07 - UI System and SPUM Integration Setup Guide

This guide covers the setup and configuration of the **UI System** and **SPUM (Sprite Packer Unity Multi-part)** integration for CombatRun.

---

## Table of Contents

1. [Overview](#1-overview)
2. [UIManager Setup](#2-uimanager-setup)
3. [UIPanel Setup](#3-uipanel-setup)
4. [Input System](#4-input-system)
5. [SPUM Setup](#5-spum-setup)
6. [Equipment Visualization](#6-equipment-visualization)
7. [Dual Animation System](#7-dual-animation-system)
8. [Known Issues](#8-known-issues)
9. [Testing Checklist](#9-testing-checklist)

---

## 1. Overview

### Architecture Overview

The UI System and SPUM Integration provide two major subsystems:

```
┌─────────────────────────────────────────────────────────────┐
│                      UI SYSTEM                               │
├─────────────────────────────────────────────────────────────┤
│  UIManager (Singleton)                                       │
│  ├── HUD Panel (Health, Gold, Skills)                       │
│  ├── Pause Stack System (LIFO)                              │
│  ├── Panel Management (Inventory, Shop, Pause)              │
│  └── Notification System                                     │
│                                                              │
│  UIPanel (Component)                                         │
│  ├── CanvasGroup Animation                                   │
│  └── Gamepad Navigation                                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   SPUM INTEGRATION                           │
├─────────────────────────────────────────────────────────────┤
│  PlayerController                                            │
│  ├── SPUMPlayerBridge (Animation State Sync)                │
│  └── SPUMEquipmentManager (Visual Equipment)                │
│                                                              │
│  SPUM_Prefabs (3rd Party Package)                            │
│  └── Multi-part Character Animator                          │
└─────────────────────────────────────────────────────────────┘
```

### Key Features

| Feature | Description |
|---------|-------------|
| **Pause Stack System** | LIFO (Last-In-First-Out) panel management with automatic time scale handling |
| **Panel Animations** | Fade, Scale, and Slide animations with customizable curves |
| **Dual Animation** | Supports both Legacy (single-sprite) and SPUM (multi-part) animations |
| **Equipment Visualization** | Runtime sprite swapping for weapons, armor, and accessories |
| **Gamepad Navigation** | Full controller support with automatic first-selection |

---

## 2. UIManager Setup

### 2.1 Singleton Configuration

The `UIManager` is a **persistent singleton** that survives scene loads.

**Setup Steps:**

1. **Create UIManager GameObject**
   - GameObject → Create Empty
   - Name: `UIManager`
   - Tag: `Untagged` (not required)

2. **Add Components**
   ```
   UIManager GameObject
   ├── UIManager.cs (Script)
   └── AudioSource (Auto-added at runtime)
   ```

3. **Configure in Scene**
   - Place in the first scene (e.g., Main Menu or Initial Scene)
   - Only one instance should exist - duplicates are auto-destroyed

### 2.2 Inspector Configuration

#### HUD Section

| Field | Type | Description |
|-------|------|-------------|
| `hudPanel` | GameObject | Main HUD panel (always visible during gameplay) |
| `healthText` | TextMeshProUGUI | Format: "current/max" |
| `healthSlider` | Slider | Health bar visualization |
| `goldText` | TextMeshProUGUI | Current gold amount display |
| `skillIcons` | Image[] | Array of 4 skill icon images (index 0-3) |
| `skillCooldownOverlays` | Image[] | Fill overlays for cooldown progress |
| `skillCooldownTexts` | TextMeshProUGUI[] | Remaining cooldown seconds |

#### Panels Section

| Field | Type | Description |
|-------|------|-------------|
| `inventoryPanel` | GameObject | Inventory UI panel |
| `shopPanel` | GameObject | Shop UI panel |
| `pausePanel` | GameObject | Pause menu panel |

#### Notifications Section

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `notificationPrefab` | GameObject | - | Notification message prefab |
| `notificationParent` | Transform | - | Container for notifications |
| `notificationDuration` | float | 2.0s | How long notifications remain visible |
| `notificationSpacing` | float | 60px | Vertical spacing between notifications |

#### Panel Animation Section

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `panelFadeDuration` | float | 0.15s | Animation duration in seconds |
| `panelAnimationCurve` | AnimationCurve | EaseInOut | Transition curve |

#### Sound Effects Section

| Field | Type | Description |
|-------|------|-------------|
| `panelOpenSound` | AudioClip | Played when any panel opens |
| `panelCloseSound` | AudioClip | Played when any panel closes |
| `buttonClickSound` | AudioClip | Played on button clicks |
| `navigationSound` | AudioClip | Played during gamepad navigation |
| `notificationSound` | AudioClip | Played when showing notifications |
| `gameOverSound` | AudioClip | Played on game over |

#### Lives & Game Over Section

| Field | Type | Description |
|-------|------|-------------|
| `revivePanel` | GameObject | Panel shown during revive countdown |
| `reviveCountdownText` | TextMeshProUGUI | Revive timer display |
| `gameOverPanel` | GameObject | Game over screen |
| `gameOverStatsText` | TextMeshProUGUI | Statistics display |
| `playAgainButton` | Button | Restart game button |
| `quitToMenuButton` | Button | Quit to main menu button |

### 2.3 Pause Stack System (LIFO)

The pause stack manages overlapping panels using a **Last-In-First-Out** (LIFO) approach.

#### How It Works

```
Initial State:  pauseDepth = 0,  Time.timeScale = 1

User opens Inventory:
  → PushPause(inventoryPanel)
  → pauseDepth = 1, Time.timeScale = 0
  → openPanels = [inventoryPanel]

User opens Shop (auto-closes Inventory):
  → PopPause(inventoryPanel)
  → PushPause(shopPanel)
  → openPanels = [shopPanel]

User presses Escape:
  → CloseMostRecentPanel() → PopPause(shopPanel)
  → pauseDepth = 0, Time.timeScale = 1
  → openPanels = []
```

#### Stack Behavior Rules

1. **First Panel Opens** → Game pauses (Time.timeScale = 0)
2. **Last Panel Closes** → Game resumes (Time.timeScale = 1)
3. **Escape Key** → Closes most recent panel (LIFO)
4. **Inventory ↔ Shop** → Auto-swaps (cannot have both open)
5. **Nested Panels** → Supported (e.g., Pause → Settings)

#### Code Example

```csharp
// Open a panel (adds to pause stack)
UIManager.Instance.ToggleInventory();

// Close most recent panel
UIManager.Instance.HandleEscapeKey();

// Close all panels and resume
UIManager.Instance.ResumeGame();

// Check if game is paused
if (UIManager.Instance.IsGamePaused()) { ... }

// Check if any panel is open
if (UIManager.Instance.IsAnyPanelOpen()) { ... }

// Get list of open panels
var panels = UIManager.Instance.GetOpenPanels();
```

### 2.4 Notification System

Notifications are queued and displayed with auto-positioning.

**Features:**
- Maximum 5 notifications displayed simultaneously
- Auto-removal after `notificationDuration`
- Fade-out animation
- Vertical stacking with `notificationSpacing`

**Usage:**

```csharp
// Show a notification
UIManager.Instance.ShowNotification("Item acquired: Sword");

// Notification prefab requirements:
// - Must have TextMeshProUGUI component
// - Should have CanvasGroup for fade animation
```

---

## 3. UIPanel Setup

### 3.1 Creating Panels

The `UIPanel` component provides standardized panel behavior.

**Setup Steps:**

1. **Create Panel GameObject**
   - Right-click Canvas → UI → Panel
   - Add `UIPanel.cs` component

2. **Required Components (Auto-added)**
   ```
   Panel GameObject
   ├── RectTransform
   ├── CanvasGroup (Auto-added if missing)
   └── UIPanel (Script)
   ```

3. **Configure Inspector**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `panelId` | string | GameObject name | Unique identifier |
| `pausesGame` | bool | true | Adds to pause stack when opened |
| `closeableByEscape` | bool | true | Can be closed with Escape |
| `startHidden` | bool | true | Start in hidden state |
| `animationDuration` | float | 0.15s | Open/close animation time |
| `animationCurve` | AnimationCurve | EaseInOut | Transition curve |
| `animationType` | PanelAnimationType | Fade | Animation style |
| `firstSelected` | Selectable | null | First focused element |
| `autoFindFirstSelectable` | bool | true | Auto-find if not assigned |

### 3.2 Animation Types

```csharp
public enum PanelAnimationType {
    Fade,           // Simple alpha fade
    Scale,          // Scale from 0 to 1
    SlideFromBottom,
    SlideFromTop,
    SlideFromLeft,
    SlideFromRight
}
```

**Recommended Usage:**

| Panel Type | Recommended Animation |
|------------|----------------------|
| Inventory | SlideFromLeft or Fade |
| Shop | SlideFromRight or Fade |
| Pause Menu | Scale or Fade |
| Notifications | SlideFromTop |

### 3.3 Navigation Setup

**For Gamepad/Keyboard Support:**

1. **Assign First Selected Element**
   - In UIPanel inspector, drag the default focused button to `firstSelected`
   - Or enable `autoFindFirstSelectable` to auto-detect

2. **Navigation Setup in Buttons**
   - Select each button → Navigation → Set to "Explicit" or "Automatic"
   - Configure Up/Down/Left/Right navigation

3. **UI Navigation Sound**
   ```csharp
   // In UIPanel inspector, assign:
   navigationSound = yourNavigationAudioClip;
   ```

### 3.4 Panel Events

```csharp
UIPanel panel = GetComponent<UIPanel>();

panel.OnPanelOpen += () => { Debug.Log("Panel opening..."); };
panel.OnPanelOpened += () => { Debug.Log("Panel opened!"); };
panel.OnPanelClose += () => { Debug.Log("Panel closing..."); };
panel.OnPanelClosed += () => { Debug.Log("Panel closed!"); };
```

---

## 4. Input System

### 4.1 New Input System Setup

The game uses Unity's **New Input System** package.

**Installation:**

1. Window → Package Manager
2. Search "Input System"
3. Install "Input System" by Unity
4. Restart Unity when prompted

### 4.2 Action Asset Configuration

**Required Action Map: "Gameplay"**

| Action Name | Action Type | Default Binding | Description |
|-------------|-------------|-----------------|-------------|
| `Move` | Value (Vector2) | WASD / Arrow Keys / Left Stick | Character movement |
| `Attack` | Button | Left Click / Ctrl / Button West | Melee attack |
| `Skill1` | Button | 1 / Right Shoulder | Skill slot 1 |
| `Skill2` | Button | 2 / Left Shoulder | Skill slot 2 |
| `Skill3` | Button | 3 / Right Trigger | Skill slot 3 |
| `Skill4` | Button | 4 / Left Trigger | Skill slot 4 |
| `Inventory` | Button | I / Button North | Toggle inventory |
| `Pause` | Button | Escape / Start | Handle pause/escape |

**Action Asset Setup:**

1. Create: Right-click → Create → Input Actions
2. Name: `GameControls`
3. Double-click to edit
4. Add Action Map: "Gameplay"
5. Add actions as listed above

### 4.3 Escape Key Handling

Escape key behavior is handled through `HandleEscapeKey()`:

```
User presses Escape:
  ├─ Are any panels open?
  │   ├─ YES → Close most recent panel (LIFO)
  │   └─ NO  → Toggle Pause Menu
  └─ Is Game Over?
      └─ YES → Ignore (use buttons instead)
```

**Implementation in PlayerController:**

```csharp
void OnPausePerformed(InputAction.CallbackContext context) {
    UIManager.Instance?.HandleEscapeKey();
}
```

---

## 5. SPUM Setup

### 5.1 SPUM Package Integration

**SPUM (Sprite Packer Unity Multi-part)** is a 3rd party asset for multi-part 2D characters.

**Setup Steps:**

1. **Import SPUM Package**
   - Import from Unity Asset Store
   - Follow SPUM's setup documentation

2. **Create SPUM Character**
   - Use SPUM Editor to create character
   - Export as prefab

3. **Character Prefab Structure**
   ```
   Player (GameObject)
   ├── SPUM_Prefabs (SPUM root)
   │   ├── Body parts (multiple SpriteRenderers)
   │   ├── Weapons (Left/Right)
   │   └── Animator
   └── Scripts...
   ```

### 5.2 PlayerController Configuration

**Inspector Settings:**

| Section | Field | Value | Description |
|---------|-------|-------|-------------|
| SPUM Integration | `useSPUM` | ☑ true | Enable SPUM mode |
| SPUM Integration | `spumBridge` | Drag SPUMPlayerBridge | Animation bridge |
| SPUM Integration | `spumEquipment` | Drag SPUMEquipmentManager | Equipment manager |
| Components | `animator` | Leave empty | Not used for SPUM |
| Components | `spriteRenderer` | Leave empty | Not used for SPUM |

### 5.3 SPUMPlayerBridge Setup

**Add Component:**

1. Select Player GameObject
2. Add Component → `SPUMPlayerBridge`

**Inspector Configuration:**

| Field | Type | Description |
|-------|------|-------------|
| `spumPrefabs` | SPUM_Prefabs | Root SPUM component |
| `spumAnimator` | Animator | SPUM's animator (auto-found) |

#### Animation Indices Configuration

| Field | Default | Description |
|-------|---------|-------------|
| `idleAnimationIndex` | 0 | Animation index for idle state |
| `moveAnimationIndex` | 0 | Animation index for movement |
| `attackAnimationIndex` | 0 | Animation index for attacks |
| `damagedAnimationIndex` | 0 | Animation index for hit reaction |
| `debuffAnimationIndex` | 0 | Animation index for debuffs |
| `deathAnimationIndex` | 0 | Animation index for death |
| `skillAnimationIndices` | [1,1,1,1] | Per-skill animation indices |

**Animation Index Setup:**

```csharp
// In SPUM, animations are organized by state with multiple variations
// Index 0 = first animation in the state list
// Index 1 = second animation, etc.

// Example configuration:
public int idleAnimationIndex = 0;      // Use first idle animation
public int attackAnimationIndex = 0;    // Use first attack animation
public int[] skillAnimationIndices = new int[4] { 
    1,  // Skill 1 uses animation index 1
    2,  // Skill 2 uses animation index 2
    1,  // Skill 3 uses animation index 1
    3   // Skill 4 uses animation index 3
};
```

#### Facing Direction (Important!)

SPUM characters use **Y-axis rotation** for flipping:

```
Facing Right:  rotation = (0, 180, 0)  // Flipped 180 on Y
Facing Left:   rotation = (0, 0, 0)    // Default (SPUM faces left by default)
```

**Do NOT use spriteRenderer.flipX** - it will break SPUM's multi-part rendering.

---

## 6. Equipment Visualization

### 6.1 SPUMEquipmentManager Setup

**Add Component:**

1. Select Player GameObject
2. Add Component → `SPUMEquipmentManager`

**Inspector Configuration:**

| Field | Type | Auto-Found | Description |
|-------|------|------------|-------------|
| `spumPrefabs` | SPUM_Prefabs | ✓ | Reference to SPUM root |
| `helmetTransform` | Transform | ✓ | Helmet sprite location |
| `armorTransform` | Transform | ✓ | Armor/body sprite location |
| `leftWeaponTransform` | Transform | ✓ | Left hand weapon |
| `rightWeaponTransform` | Transform | ✓ | Right hand weapon |
| `shieldTransform` | Transform | ✓ | Shield sprite location |
| `backTransform` | Transform | ✓ | Back item (wings, etc.) |

### 6.2 Sprite Swapping

**Equipment Methods:**

```csharp
SPUMEquipmentManager equipment = GetComponent<SPUMEquipmentManager>();

// Equip weapon
equipment.EquipWeapon(swordSprite, WeaponType.Sword);
equipment.EquipWeapon(daggerSprite, WeaponType.Dagger); // Dual wield

// Equip armor
equipment.EquipArmor(armorSprite, helmetSprite);

// Equip shield
equipment.EquipShield(shieldSprite);

// Equip back item
equipment.EquipBack(wingsSprite);
```

**Weapon Types:**

```csharp
public enum WeaponType {
    Sword,      // Right hand only
    Dagger,     // Dual wield (both hands)
    Axe,        // Dual wield (both hands)
    Bow,        // Two-handed
    Staff,      // Two-handed
    // Custom types...
}
```

### 6.3 Equipment Color/Dye

**Change Equipment Color:**

```csharp
// Apply color dye
equipment.SetEquipmentColor(EquipSlot.Weapon, Color.red);
equipment.SetEquipmentColor(EquipSlot.Armor, new Color(0.5f, 0.5f, 1f));

// Reset to default
equipment.ResetEquipmentColor(EquipSlot.Weapon);
```

**Equipment Slots:**

```csharp
public enum EquipSlot {
    Weapon,
    Armor,
    Shield,
    Back
}
```

### 6.4 Shop Preview

**Preview Equipment (without equipping):**

```csharp
// Store current equipment
Sprite currentWeapon = equipment.GetEquippedSprite(EquipSlot.Weapon);

// Preview new equipment
equipment.PreviewEquipment(EquipSlot.Weapon, newWeaponSprite);

// Restore original (when exiting shop)
equipment.EndPreview(EquipSlot.Weapon, currentWeapon);
```

---

## 7. Dual Animation System

### 7.1 SPUM vs Legacy

The system supports both animation types via toggle:

| Feature | Legacy Mode | SPUM Mode |
|---------|-------------|-----------|
| `useSPUM` | false | true |
| Character Type | Single sprite | Multi-part |
| Animation | Unity Animator | SPUM_Prefabs.PlayAnimation() |
| Flipping | spriteRenderer.flipX | rotation.y = 180 |
| Equipment | Child sprites | SPUMEquipmentManager |
| Damage Flash | Sprite color change | VFX or all renderers |

### 7.2 Configuration Toggle

**In PlayerController Inspector:**

```
[☑] useSPUM    ← Check for SPUM, uncheck for Legacy
```

**Runtime Behavior:**

```csharp
// PlayerController.cs - Update()
if (!useSPUM) {
    UpdateAnimation();  // Legacy animator
}

// SPUM animation is handled by SPUMPlayerBridge.Update()
```

### 7.3 Setup Checklists

#### SPUM Setup Checklist

- [ ] Import SPUM package from Asset Store
- [ ] Create SPUM character in SPUM Editor
- [ ] Export character prefab
- [ ] Add `SPUMPlayerBridge` component to Player
- [ ] Add `SPUMEquipmentManager` component to Player
- [ ] Assign `spumPrefabs` reference
- [ ] Check `useSPUM = true` in PlayerController
- [ ] Clear `animator` field in PlayerController
- [ ] Clear `spriteRenderer` field in PlayerController
- [ ] Configure animation indices in SPUMPlayerBridge
- [ ] Set up equipment sprites

#### Legacy Setup Checklist

- [ ] Ensure `useSPUM = false` in PlayerController
- [ ] Assign `animator` (Legacy Animator Controller)
- [ ] Assign `spriteRenderer` (main character sprite)
- [ ] Create Animator Controller with parameters:
  - `IsMoving` (bool)
  - `MoveX` (float)
  - `MoveY` (float)
  - `LastMoveX` (float)
  - `LastMoveY` (float)
  - `Attack` (trigger)
  - `Die` (trigger)
  - `Revive` (trigger)
- [ ] Create animation clips (Idle, Move, Attack, Die)
- [ ] Set up transition conditions

---

## 8. Known Issues

### 8.1 Minor Method Naming Issues

**Issue:** Some methods may have inconsistent naming (e.g., `allListsHaveItemsExist()`)

**Status:** Non-breaking, works as intended

**Workaround:** None needed - method functions correctly

### 8.2 Missing Skill Animation State

**Issue:** SPUMPlayerBridge uses `PlayerState.ATTACK` for skills instead of a dedicated `SKILL` state

**Current Implementation:**
```csharp
public void PlaySkillAnimation(int skillIndex) {
    int animIndex = skillAnimationIndices[skillIndex];
    spumPrefabs?.PlayAnimation(PlayerState.ATTACK, animIndex);
    // Uses ATTACK state with different animation index
}
```

**Recommended Fix (if SPUM supports it):**
```csharp
// If SPUM has a SKILL or OTHER state:
spumPrefabs?.PlayAnimation(PlayerState.SKILL, animIndex);
// or
spumPrefabs?.PlayAnimation(PlayerState.OTHER, animIndex);
```

### 8.3 Animator Override Controller

**Issue:** SPUM's `OverrideControllerInit()` may fail if already initialized

**Current Safeguard:**
```csharp
if (spumPrefabs._anim.runtimeAnimatorController is AnimatorOverrideController) {
    // Already an override - skip initialization
    Debug.Log("[SPUMPlayerBridge] Animator already has OverrideController");
} else {
    spumPrefabs.OverrideControllerInit();
}
```

---

## 9. Testing Checklist

### 9.1 UI System Testing

#### UIManager Tests
- [ ] UIManager singleton persists across scene loads
- [ ] HUD displays correct health: "current/max"
- [ ] Health slider updates when taking damage/healing
- [ ] Gold counter increments when collecting gold
- [ ] Skill icons display correctly
- [ ] Skill cooldown overlays fill properly
- [ ] Skill cooldown texts show remaining seconds
- [ ] Notifications appear and auto-remove
- [ ] Multiple notifications stack correctly (max 5)
- [ ] Panel open sound plays
- [ ] Panel close sound plays
- [ ] Button click sound plays

#### Pause Stack Tests
- [ ] Opening first panel pauses game (Time.timeScale = 0)
- [ ] Closing last panel resumes game (Time.timeScale = 1)
- [ ] Escape key closes most recent panel (LIFO)
- [ ] Opening shop auto-closes inventory
- [ ] Opening inventory auto-closes shop
- [ ] Nested panels work correctly (Pause → Settings)
- [ ] `ResumeGame()` closes all panels
- [ ] `IsGamePaused()` returns correct state
- [ ] `IsAnyPanelOpen()` returns correct state

#### Panel Animation Tests
- [ ] Fade animation works
- [ ] Scale animation works
- [ ] Slide animations work (all directions)
- [ ] Animation curve affects timing
- [ ] Panels start hidden when `startHidden = true`

#### Game Over Tests
- [ ] Revive countdown displays correctly
- [ ] Game over panel shows stats
- [ ] Play Again button restarts game
- [ ] Quit to Menu button loads main menu
- [ ] HUD hides during game over

### 9.2 Input System Testing

- [ ] WASD moves character
- [ ] Arrow keys move character
- [ ] Gamepad left stick moves character
- [ ] Attack button triggers melee
- [ ] Skill buttons (1-4) cast skills
- [ ] I key toggles inventory
- [ ] Escape opens pause menu
- [ ] Escape closes panels in LIFO order
- [ ] Input works while panels are open (if not paused)

### 9.3 SPUM Integration Testing

#### Basic Animation Tests
- [ ] Idle animation plays when stationary
- [ ] Move animation plays when moving
- [ ] Attack animation triggers on attack
- [ ] Damaged animation plays when hit
- [ ] Death animation plays on death
- [ ] Character faces correct direction (left/right)
- [ ] No spriteRenderer.flipX usage

#### Skill Animation Tests
- [ ] Skill 1 triggers correct animation
- [ ] Skill 2 triggers correct animation
- [ ] Skill 3 triggers correct animation
- [ ] Skill 4 triggers correct animation
- [ ] Different animation indices work per skill

#### Equipment Tests
- [ ] Weapon sprite changes on equip
- [ ] Armor sprite changes on equip
- [ ] Dual wield weapons show on both hands
- [ ] Shield sprite shows/hides correctly
- [ ] Back item (wings) displays correctly
- [ ] Equipment color changes with dye
- [ ] Default equipment restores on unequip
- [ ] Shop preview shows equipment temporarily

#### Toggle Tests
- [ ] `useSPUM = true` uses SPUM animations
- [ ] `useSPUM = false` uses Legacy animations
- [ ] Switching toggle at runtime works
- [ ] No null reference errors in either mode

### 9.4 Performance Testing

- [ ] Notification system doesn't leak memory
- [ ] Panel animations run smoothly
- [ ] SPUM character renders efficiently
- [ ] Equipment changes don't cause lag
- [ ] Damage flash doesn't impact frame rate

---

## Quick Reference

### Common Code Snippets

```csharp
// Show notification
UIManager.Instance?.ShowNotification("Hello World!");

// Toggle panels
UIManager.Instance?.ToggleInventory();
UIManager.Instance?.ToggleShop();
UIManager.Instance?.TogglePause();

// Check states
bool paused = UIManager.Instance.IsGamePaused();
bool panelOpen = UIManager.Instance.IsAnyPanelOpen();

// Equip item (SPUM)
GetComponent<SPUMEquipmentManager>()?.EquipWeapon(sprite, WeaponType.Sword);

// Play animation (SPUM)
GetComponent<SPUMPlayerBridge>()?.PlayAttackAnimation();

// Get input for external scripts
Vector2 moveInput = GetComponent<PlayerController>().GetMoveInput();
Vector2 facing = GetComponent<PlayerController>().GetFacingDirection();
```

### File Locations

| Script | Path |
|--------|------|
| UIManager.cs | `Assets/Scripts/UI/UIManager.cs` |
| UIPanel.cs | `Assets/Scripts/UI/UIPanel.cs` |
| SPUMPlayerBridge.cs | `Assets/Scripts/SPUM/SPUMPlayerBridge.cs` |
| SPUMEquipmentManager.cs | `Assets/Scripts/SPUM/SPUMEquipmentManager.cs` |
| PlayerController.cs | `Assets/Scripts/PlayerController.cs` |

---

**Document Version:** 1.0  
**Last Updated:** March 2026  
**Related Guides:** See other setup guides in `system set up guide/` directory


---

## 10. Multi-Scene Support

### 10.1 UIManager Scene Handling

UIManager automatically adapts to different scenes:

```csharp
// On Game Scene Loaded:
- Show HUD panel
- Hide menu panels
- Re-subscribe to new Player
- Enable combat UI

// On Menu Scene Loaded:
- Hide HUD panel
- Hide game panels
- Unsubscribe from old Player
- Disable combat UI
```

### 10.2 Scene-Specific UI Configuration

#### MainMenu Scene
- **No HUD** - MainMenuController manages UI
- **No SkillBar** - Skills not needed
- **Persistent UIManager** - Exists but inactive

#### Loading Scene
- **Minimal UI** - LoadingSceneController manages UI
- **No UIManager interaction** - Separate controller

#### Game Scene
- **Full HUD** - Health, Gold, Skills, Combo
- **Pause Menu** - Accessible via Escape
- **Game Over Panel** - Shows on death

### 10.3 Player Reference Management

UIManager handles player references across scenes:

```csharp
void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
    if (IsGameScene(scene.name)) {
        // New player spawned in scene
        FindAndSubscribeToPlayer();
    } else {
        // Old player will be destroyed
        UnsubscribeFromEvents();
    }
}
```

### 10.4 Panel State Persistence

| Panel | Menu Scene | Game Scene |
|-------|------------|------------|
| HUD | Hidden | Visible |
| Inventory | Hidden | Toggleable |
| Shop | Hidden | Toggleable |
| Pause | Hidden | Toggleable |
| Game Over | Hidden | Conditional |

### 10.5 Multi-Scene Setup

See [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) for:
- SceneTransitionManager configuration
- Scene transition flow
- Build settings
- Testing scene transitions

### 10.6 SPUM in Multi-Scene

SPUM components handle scene transitions:

```csharp
// SPUMPlayerBridge
- Re-initializes for new Player instances
- Re-binds to SPUM_Prefabs in new scene

// SPUMEquipmentManager
- Equipment visuals persist (data in InventoryManager)
- Re-applies to new player model
```

# CombatRun - Setup Guide

Complete setup instructions for the ARPG core systems, including recent UI improvements and bug fixes.

---

## 📁 Required Project Structure

```
Assets/
├── Scripts/
│   ├── Combat/
│   ├── Data/
│   ├── Enemies/
│   ├── Inventory/
│   ├── Managers/
│   ├── Shop/
│   ├── Skills/
│   ├── SPUM/
│   ├── UI/
│   └── PlayerController.cs
├── Resources/
│   ├── Items/
│   ├── Sets/
│   └── Skills/
├── SPUM/
└── Prefabs/
```

---

## ⚙️ Unity Setup

### 1. Input System Setup

This project uses the **NEW Unity Input System Package** (not the legacy Input Manager).

**Edit > Project Settings > Player > Other Settings:**
- **Active Input Handling**: Set to `Input System Package (New)` or `Both`

**Input Action Asset:**
- Located at: `Assets/InputSystem/GameControls.inputactions`

**Controls (already configured):**
| Action | Input |
|--------|-------|
| Move | WASD or Arrow Keys |
| Attack | Space or Left Mouse Button |
| Skills | 1, 2, 3, 4 |
| Inventory | I |
| Pause | Escape |

**Assign to Player:**
1. Select your Player GameObject
2. In PlayerController, drag `GameControls.inputactions` to **Input Actions** field

**See `Assets/InputSystem/INPUT_SYSTEM_SETUP.md` for full details.**

### 2. Layers & Tags

**Create Tags:**
- `Enemy`
- `Pickup`
- `Projectile`

**Create Layers:**
- `Enemies` (Layer 6)
- `Pickups` (Layer 7)
- `Player` (Layer 8)

### 3. Physics Settings

**Edit > Project Settings > Physics 2D:**

Layer Collision Matrix:
| | Default | Enemies | Pickups | Player |
|---|---------|---------|---------|--------|
| Default | ✓ | ✓ | | ✓ |
| Enemies | ✓ | | | ✓ |
| Pickups | | | | ✓ |
| Player | ✓ | ✓ | ✓ | |

---

## 🎮 Player Setup

### Option A: Regular Sprite (Legacy)

**Create GameObject: `Player`**

Add Components:
1. **Sprite Renderer** - Your player sprite
2. **Rigidbody2D** - Kinematic, Gravity Scale: 0
3. **CircleCollider2D** or **BoxCollider2D**
4. **PlayerController**
5. **SkillCaster**
6. **ComboSystem** (optional)

**Create Child Objects:**
```
Player
├── Visuals
│   ├── Body (SpriteRenderer)
│   ├── Armor (SpriteRenderer)
│   └── Weapon (SpriteRenderer)
├── AttackPoint (Empty)
└── CastPoint (Empty)
```

**PlayerController Settings:**
| Field | Value |
|-------|-------|
| useSPUM | ☐ (UNCHECKED) |
| Move Speed | 5 |
| Melee Range | 1.5 |
| Melee Cooldown | 0.5 |
| Enemy Layer | Enemies |
| Attack Point | AttackPoint (child) |

---

### Option B: SPUM Character (Recommended)

**Step 1: Get SPUM Prefab**
1. Go to `Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/`
2. Choose a character prefab
3. Drag into scene

**Step 2: Add Components**
Add to the root GameObject:
1. **Rigidbody2D** - Kinematic, Gravity Scale: 0
2. **CircleCollider2D**
3. **PlayerController**
4. **SkillCaster**
5. **ComboSystem**
6. **SPUMPlayerBridge**
7. **SPUMEquipmentManager**

**Step 3: Configure PlayerController**
| Field | Value |
|-------|-------|
| useSPUM | ☑️ (CHECKED) |
| spumBridge | SPUMPlayerBridge (drag) |
| spumEquipment | SPUMEquipmentManager (drag) |
| useVFXDamageFlash | ☑️ (Recommended) |
| damageFlashVFX | Assign prefab |
| damageFlashDuration | 0.1 |

**Step 4: Configure SPUMPlayerBridge**
| Field | Value |
|-------|-------|
| idleAnimationIndex | 0 |
| moveAnimationIndex | 0 |
| attackAnimationIndex | 0 |
| skillAnimationIndices | [1,1,1,1] |

**Important:** Recent fixes add automatic bounds checking for animation indices.

---

## 🖥️ UI Setup

### Canvas Setup

**Create: UI > Canvas**
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale with Screen Size (1920x1080)

### UIManager Setup

**Create empty GameObject: `UIManager`**

Add **UIManager** script with these settings:

```
UIManager
├── HUD Panel (assign)
├── Health Bar (Slider)
├── Gold Text (TextMeshPro)
├── Skill Icons (4x Image)
├── Cooldown Overlays (4x Image)
├── Cooldown Texts (4x TextMeshPro)
├── Inventory Panel (assign)
├── Shop Panel (assign)
├── Pause Panel (assign)
├── Notification Prefab (assign)
└── Notification Parent (Transform)
```

**New Features to Configure:**

```csharp
[Header("Panel Animation")]
public float panelFadeDuration = 0.15f;
public AnimationCurve panelAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

[Header("Notifications")]
public float notificationSpacing = 60f;
```

### Panel Setup

Each panel needs a **CanvasGroup** component:
1. Select Inventory Panel
2. Add Component → CanvasGroup
3. Repeat for Shop Panel and Pause Panel

This enables the smooth fade animations.

---

## 📷 Camera Setup

**Main Camera:**
- Projection: Orthographic
- Size: 8

Add **CameraFollow** script with these new features:

```csharp
[Header("Smoothing")]
public bool useSmoothDamp = true;      // Recommended: true
public float smoothTime = 0.15f;

[Header("Dead Zone")]
public bool useDeadZone = true;        // Prevents jitter
public Vector2 deadZoneSize = new Vector2(0.5f, 0.5f);

[Header("Look Ahead")]
public bool useLookAhead = true;       // Camera leads movement
public float lookAheadDistance = 2f;

[Header("Bounds")]
public bool useBounds = false;         // Optional level bounds
public Vector2 minBounds;
public Vector2 maxBounds;
```

**Target Assignment:**
- Assign Player to "Target" field

---

## 🧩 Manager Setup

Create empty GameObject: `Managers`

Add scripts:
1. **GameManager**
2. **UIManager** (DontDestroyOnLoad) ← Has pause stack system
3. **InventoryManager**
4. **ShopManager**
5. **SetBonusManager**
6. **WeaponMasteryManager** (DontDestroyOnLoad)
7. **SkillSynergyManager**
8. **GambleSystem**
9. **DailyRunManager** (DontDestroyOnLoad)
10. **DamageNumberManager**

**UIManager Features:**
- ✅ Pause stack system (no timeScale conflicts)
- ✅ Panel fade animations
- ✅ Notification queue management
- ✅ Proper event cleanup

---

## 🎮 Controls Reference

| Action | Input |
|--------|-------|
| Move | W, A, S, D or Arrow Keys |
| Melee Attack | Space or Left Click |
| Finisher | Hold Attack button (when combo ready) |
| Skill 1-4 | 1, 2, 3, 4 |
| Toggle Inventory | I |
| Pause / Close Panels | Escape |

**Escape Key Behavior:**
- If panels are open → closes most recent panel first
- If no panels open → toggles pause menu

---

## 🔧 Testing Checklist

### Basic Functionality
- [ ] Player moves with WASD
- [ ] Player faces movement direction
- [ ] Melee attack hits enemies
- [ ] Skills cast with 1-4 keys
- [ ] Damage numbers display
- [ ] Gold updates on pickup

### Input System Tests (New)
- [ ] Input Actions assigned to PlayerController
- [ ] Player moves with WASD (via Input Action)
- [ ] Player moves with Arrow Keys (via Input Action)
- [ ] Attack works with Space (Input Action: Attack)
- [ ] Attack works with Left Click (Input Action: Attack)
- [ ] Skills cast with 1-4 keys (Input Actions)
- [ ] Inventory toggles with I (Input Action: Inventory)
- [ ] Pause works with Escape (Input Action: Pause)
- [ ] No "Input Manager" warnings in console

### UI System Tests (New)
- [ ] Open Inventory - timeScale = 0, panel fades in
- [ ] Open Shop while Inventory open - Inventory closes smoothly
- [ ] Press Escape - closes most recent panel
- [ ] Open multiple panels - pause depth tracks correctly
- [ ] Notifications - max 5, fade out properly, reposition

### Camera Tests (New)
- [ ] Camera follows player smoothly (SmoothDamp)
- [ ] Dead zone works (small movements don't move camera)
- [ ] Look ahead works (camera leads movement)
- [ ] Camera shake works (if implemented)

### SPUM Tests
- [ ] SPUM animations play
- [ ] Animation indices validate
- [ ] Character faces correct direction
- [ ] Equipment changes visual

### Critical Bug Fix Tests
- [ ] Status Effect: Freeze 100% slow - no crash
- [ ] Inventory: Fill and unequip - warns, doesn't lose item
- [ ] Synergy: Multiple damage reductions - proper cleanup
- [ ] Daily Run: DateTime serialization works cross-platform
- [ ] Camera: No Find() calls in Update loop
- [ ] UI: All CanvasGroup references cached

---

## 🐛 Common Issues

### "Panel animations not working"
**Solution:** Ensure all panels have CanvasGroup component

### "Notifications overlap"
**Solution:** Check notificationSpacing value in UIManager (default: 60f)

### "Escape key not working"
**Solution:** Verify UIManager is in scene and initialized

### "Camera jerky movement"
**Solution:** Enable useSmoothDamp, adjust smoothTime (0.15f recommended)

### "timeScale conflicts"
**Solution:** Use UIManager.ToggleInventory() / ToggleShop() instead of direct SetActive

### "SPUM animations not validating"
**Solution:** Check console for "Invalid animation index" warnings, adjust indices

### "Input Action Asset not assigned!"
**Solution:** 
1. Select Player GameObject
2. Drag `Assets/InputSystem/GameControls.inputactions` to PlayerController's **Input Actions** field

### "No Input System is enabled"
**Solution:**
1. Go to **Edit > Project Settings > Player > Other Settings**
2. Set **Active Input Handling** to `Input System Package (New)` or `Both`

---

## 📚 Documentation

- **FEATURES_SUMMARY.md** - Features and bug fixes
- **IMPLEMENTATION_PLAN.md** - Development roadmap
- **INPUT_SYSTEM_SETUP.md** - Input System Package setup guide
- **SPUM_INTEGRATION_README.md** - Detailed SPUM setup
- **OPTIMIZATION_UPDATE_SUMMARY.md** - Bug fixes detail
- **TEST_GUIDE.md** - Testing procedures

---

## 🔧 Implementation Notes

### Input System
The project uses the Unity Input System Package for all input handling:
- Event-driven input (no polling overhead)
- Action maps: "Gameplay" and "UI"
- Supports multiple control schemes (Keyboard, Gamepad)
- See `INPUT_SYSTEM_SETUP.md` for details

### Component Caching
For performance, the following components are now cached at initialization:
- **PlayerController**: Input Actions references
- **UIManager**: CanvasGroup references for all panels
- **InventoryUI**: Image and Button components
- **DamageNumberManager**: TextMeshPro components

### DateTime Serialization
`DailyRunManager` now uses Unix timestamp (long) instead of DateTime objects for cross-platform save compatibility.

### Event Delegate Storage
`SetBonusManager` now stores delegates as fields to ensure reliable unsubscription and prevent memory leaks.

---

*Setup Complete! Test Input System and UI thoroughly - both have been significantly improved.*

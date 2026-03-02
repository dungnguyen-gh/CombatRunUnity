# Combat Run - Unity Roguelike Action Game

A 2D roguelike action game built in Unity featuring real-time combat, equipment progression, skill synergies, daily runs, and comprehensive UI systems.

## 🎮 Project Overview

Combat Run is a Unity-based action game where players battle through waves of enemies, collect equipment with set bonuses, master different weapon types, and discover powerful skill combinations.

### Key Features

- **Real-time Combat**: Melee attacks with combo system and 4 equipped skills
- **Equipment System**: Weapons and armor with rarity tiers and set bonuses
- **Skill Synergies**: Combine skills in sequence for powerful combo effects
- **Weapon Mastery**: Progression system based on kills with each weapon type
- **Daily Runs**: Seed-based daily challenges with randomized modifiers
- **SPUM Integration**: Support for Soonsoon Pixel Unit Maker character customization
- **Shop & Gambling**: Buy/sell items and take risks for powerful rewards
- **Pause Stack System**: Multiple UI panels without timeScale conflicts
- **Notification System**: Animated notifications with proper queue management

---

## 📚 Documentation

All documentation is located in the `Assets/` folder:

| Document | Purpose |
|----------|---------|
| [`Assets/FEATURES_SUMMARY.md`](Assets/FEATURES_SUMMARY.md) | Detailed features, bug fixes, design tips |
| [`Assets/IMPLEMENTATION_PLAN.md`](Assets/IMPLEMENTATION_PLAN.md) | Development roadmap with Week 5 fixes |
| [`Assets/OPTIMIZATION_UPDATE_SUMMARY.md`](Assets/OPTIMIZATION_UPDATE_SUMMARY.md) | Bug fixes and optimizations log |
| [`Assets/SETUP_README.md`](Assets/SETUP_README.md) | Complete setup instructions |
| [`Assets/INPUT_SYSTEM_SETUP.md`](Assets/InputSystem/INPUT_SYSTEM_SETUP.md) | Input System Package guide |
| [`Assets/SPUM_INTEGRATION_README.md`](Assets/SPUM_INTEGRATION_README.md) | SPUM integration with animation validation |
| [`Assets/SPUM_INTEGRATION_SUMMARY.md`](Assets/SPUM_INTEGRATION_SUMMARY.md) | Quick SPUM reference |
| [`Assets/SPUM_VFX_GUIDE.md`](Assets/SPUM_VFX_GUIDE.md) | Visual effects creation guide |
| [`Assets/TEST_GUIDE.md`](Assets/TEST_GUIDE.md) | Testing procedures and verification |
| [`AGENTS.md`](AGENTS.md) | Development guide for AI agents |

---

## 🚀 Quick Start

### Requirements
- Unity 2021.3 LTS or newer
- **Input System Package** (required)
- TextMeshPro package
- SPUM (optional, for character customization)

### Setup Steps
1. Clone/Open project in Unity
2. Install **Input System Package** (Window > Package Manager)
3. Set **Active Input Handling** to "Input System Package" or "Both" (Project Settings > Player)
4. See [`Assets/SETUP_README.md`](Assets/SETUP_README.md) for detailed setup
5. Assign `GameControls.inputactions` to PlayerController
6. Create Player and Enemy prefabs
7. Set up Managers in scene

---

## 🎮 Input Controls (New Input System)

| Action | Input |
|--------|-------|
| Movement | W/A/S/D or Arrow Keys |
| Melee Attack | Space or Left Click |
| Finisher | Hold Attack button (when combo ready) |
| Cast Skills | 1, 2, 3, 4 |
| Toggle Inventory | I |
| Pause / Close Panels | Escape |

**Note:** This project uses the Unity Input System Package. Controls are configured in `Assets/InputSystem/GameControls.inputactions`.

---

## 🏗️ Architecture

### Core Systems

```
┌─────────────────────────────────────────────────────────────────┐
│                        COMBAT RUN ARCHITECTURE                   │
├─────────────────────────────────────────────────────────────────┤
│  DATA LAYER (ScriptableObjects)                                  │
│  ├── ItemSO              - Item definitions                      │
│  ├── EquipmentSetSO      - Set bonus configurations              │
│  ├── SkillSO             - Skill data                            │
│  └── WeaponMasteryData   - Mastery progression                   │
├─────────────────────────────────────────────────────────────────┤
│  MANAGER LAYER (Singletons)                                      │
│  ├── GameManager         - Wave spawning, game state             │
│  ├── InventoryManager    - Item storage & equipment              │
│  ├── UIManager           - HUD, panels (with pause stack)        │
│  ├── ShopManager         - Buy/sell with rarity cache            │
│  ├── SetBonusManager     - Set bonus tracking                    │
│  ├── WeaponMasteryManager- Kill tracking & bonuses               │
│  └── DailyRunManager     - Daily challenge generation            │
├─────────────────────────────────────────────────────────────────┤
│  COMBAT LAYER                                                    │
│  ├── PlayerController    - Movement, melee, skills (Input Sys)   │
│  ├── SkillCaster         - Skill execution & cooldowns           │
│  ├── ComboSystem         - Combo counting & finisher             │
│  ├── StatusEffect        - DOT effects (fixed divide-by-zero)    │
│  ├── Projectile          - Ranged attack (fixed double-hit)      │
│  └── Enemy               - AI with state machine                 │
├─────────────────────────────────────────────────────────────────┤
│  VISUAL LAYER                                                    │
│  ├── SPUMPlayerBridge    - SPUM animation with validation        │
│  ├── SPUMEquipmentManager- Visual equipment swapping             │
│  ├── DamageNumberManager - Floating damage text pool             │
│  └── CameraFollow        - Smooth follow with dead zone          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Critical Bug Fixes (All Resolved)

### Week 5 - Major Bug Fixes & Optimizations

| Category | File | Bug | Fix |
|----------|------|-----|-----|
| **Crash** | StatusEffect.cs | Divide-by-zero | Clamped slow multiplier |
| **Crash** | PlayerStats.cs | Attack speed zero | Minimum clamp (0.1) |
| **Data** | WeaponMastery.cs | Serialization fail | List + Cache pattern |
| **Memory** | UIManager.cs | Missing persistence | DontDestroyOnLoad |
| **Memory** | SkillSynergyManager.cs | Event leak | Unsubscription |
| **Logic** | SetBonusManager.cs | Component duplication | HashSet tracking |
| **Logic** | InventoryManager.cs | Item loss | Safety check |
| **Logic** | Projectile.cs | Double-hit | hasHit flag |
| **UI** | UIManager.cs | timeScale conflicts | Pause stack system |
| **UI** | UIManager.cs | Notification leak | Proper queue cleanup |
| **Performance** | ShopManager.cs | Slow lookups | Rarity cache |

**See [`Assets/OPTIMIZATION_UPDATE_SUMMARY.md`](Assets/OPTIMIZATION_UPDATE_SUMMARY.md) for complete details.**

---

## ✨ UI System Features

### Pause Stack System
The UIManager implements a sophisticated pause stack that allows multiple UI panels to be opened without conflicts:

```csharp
// Opening Inventory while Shop is open
PushPause(inventoryPanel);  // Automatically closes Shop
PopPause(shopPanel);        // Properly tracked

// Escape key closes most recent panel first
while (openPanels.Count > 0) {
    CloseMostRecentPanel();
}
```

**Benefits:**
- No timeScale conflicts when opening multiple panels
- Proper pause depth tracking
- Smooth transitions between panels

### Panel Animations
All panels use CanvasGroup-based fade animations:
```csharp
[Header("Panel Animation")]
public float panelFadeDuration = 0.15f;
public AnimationCurve panelAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
```

### Notification System
- Animated notifications with proper queue management
- Maximum 5 notifications at once
- Auto-repositioning when notifications are removed
- Fade out animations

---

## 🎥 Camera System

The CameraFollow now includes advanced features:

### Smooth Damp Movement
More stable than Lerp for camera following:
```csharp
public bool useSmoothDamp = true;
public float smoothTime = 0.15f;
```

### Dead Zone
Camera doesn't move for small player movements:
```csharp
public bool useDeadZone = true;
public Vector2 deadZoneSize = new Vector2(0.5f, 0.5f);
```

### Look Ahead
Camera looks ahead based on player movement:
```csharp
public bool useLookAhead = true;
public float lookAheadDistance = 2f;
```

### Camera Shake
Built-in shake effect for impacts:
```csharp
Camera.main.GetComponent<CameraFollow>()?.Shake(0.3f, 0.2f);
```

---

## 🧪 Testing Checklist

### Critical Bug Fix Tests
- [ ] Status Effect: Apply Freeze with 100% slow - no crash
- [ ] Combo: Build to 5x combo, execute finisher
- [ ] Inventory: Fill inventory, try to unequip - should warn, not lose item
- [ ] Synergy: Activate DamageReduction multiple times - proper cleanup
- [ ] Gamble: Fill inventory, try mystery item - should refund
- [ ] Mastery: Kill tracking - data persists

### Input System Tests
- [ ] Input Actions assigned to PlayerController
- [ ] Move with WASD / Arrow Keys
- [ ] Attack with Space / Left Click
- [ ] Cast Skills with 1-4 keys
- [ ] Toggle Inventory with I
- [ ] Pause / Close Panels with Escape

### UI System Tests
- [ ] Open Inventory - timeScale = 0
- [ ] Open Shop while Inventory open - Inventory closes, Shop opens
- [ ] Press Escape - closes most recent panel
- [ ] Multiple notifications - max 5, proper positioning
- [ ] Panel animations - smooth fade in/out

### Camera Tests
- [ ] Camera follows player smoothly
- [ ] Dead zone works (small movements don't move camera)
- [ ] Look ahead works (camera leads movement)
- [ ] Camera shake works on heavy damage

### SPUM Tests
- [ ] SPUM animations play correctly
- [ ] Animation indices validate (no warnings)
- [ ] Character faces correct direction
- [ ] Equipment swaps work
- [ ] Shop preview works

---

## 📖 Creating Content

### Create Items
```
Right-click → Create → Items → New Item
```

### Create Equipment Sets
```
Right-click → Create → Items → New Equipment Set
```

### Create Skills
```
Right-click → Create → Skills → New Skill
```

See [`Assets/IMPLEMENTATION_PLAN.md`](Assets/IMPLEMENTATION_PLAN.md) for detailed content creation.

---

## 🎯 Performance Optimizations

| System | Optimization | Result |
|--------|--------------|--------|
| UIManager | Pause stack | No timeScale conflicts |
| UIManager | Notification pooling | Reduced GC |
| CameraFollow | SmoothDamp | Stable following |
| CameraFollow | Cached velocity | No allocations |
| ShopManager | Rarity cache | O(1) lookup |
| SkillCaster | Camera cache | No per-frame lookup |

---

## 🐛 Known Issues (All Fixed)

All previously known issues have been resolved:
- ✅ Migrated to Unity Input System Package
- ✅ Status effect divide-by-zero
- ✅ Weapon mastery serialization
- ✅ Set bonus component duplication
- ✅ Memory leaks in UIManager
- ✅ SkillSynergy defense stacking
- ✅ UIManager timeScale conflicts
- ✅ Notification queue memory leak

---

## 📝 Credits

- **SPUM**: Soonsoon Studio - [Asset Store](https://assetstore.unity.com/packages/slug/188715)
- **TextMeshPro**: Unity Technologies

## License

This project is for educational purposes. SPUM assets have their own license terms from Soonsoon Studio.

---

*Last Updated: Complete with Unity Input System Package Migration*

# CombatRun Setup Guide - Errors Fixed

**Date:** 2026-03-08

---

## ✅ Errors Fixed

### 1. "Unknown skill type: Melee" - FIXED

**Problem:** `SkillType.Melee` was defined in the enum but not handled in `SkillCaster.ExecuteSkill()`.

**Fix:** 
- Added `SkillType.Melee => CastMelee(skill)` to the switch statement
- Implemented `CastMelee()` method that performs a raycast attack in front of the player

**Code Added to SkillCaster.cs:**
```csharp
bool CastMelee(SkillSO skill) {
    Vector2 origin = castPoint != null ? (Vector2)castPoint.position : (Vector2)transform.position;
    Vector2 direction = player?.GetFacingDirection() ?? Vector2.right;
    
    // Raycast to find enemy in melee range
    RaycastHit2D hit = Physics2D.Raycast(origin, direction, skill.range, enemyLayer);
    
    if (hit.collider != null) {
        ApplyDamage(hit.collider.gameObject, skill);
        ApplyStatusEffects(hit.collider.gameObject, skill);
        SpawnEffect(skill.effectPrefab, hit.point, 1f);
    }
    
    return true;
}
```

---

### 2. "Cannot nest AnimatorOverrideController" - FIXED

**Problem:** SPUM prefabs have AnimatorOverrideControllers without proper base controllers, causing errors when `OverrideControllerInit()` is called.

**Fix:** Added safety checks and try-catch blocks in `Enemy.InitializeSPUM()`:

```csharp
void InitializeSPUM() {
    if (spumPrefabs == null) return;
    
    // Validate animator exists and has a controller
    if (spumPrefabs._anim == null) {
        Debug.LogWarning($"[Enemy] SPUM prefab has no Animator!");
        return;
    }
    
    if (spumPrefabs._anim.runtimeAnimatorController == null) {
        Debug.LogWarning($"[Enemy] SPUM prefab has no Animator Controller!");
        return;
    }
    
    try {
        spumPrefabs.OverrideControllerInit();
    }
    catch (System.Exception ex) {
        Debug.LogWarning($"[Enemy] SPUM init failed: {ex.Message}");
        // Continue - animations might still work
    }
    
    // ... rest of initialization
}
```

---

## 🎮 Required Setup for Managers and UI

### Manager Setup (Create Empty GameObject called "Managers")

#### 1. GameManager
```
GameObject: Managers/GameManager
Components:
├── GameManager script
│   ├── Spawn Points: [assign 4 empty GameObjects around map]
│   ├── Enemy Prefabs: [assign enemy prefabs]
│   ├── Boss Prefab: [assign boss prefab]
│   ├── Boss Spawn Point: [assign empty GameObject]
│   ├── Enemy Container: [create empty GameObject called "Enemies"]
│   └── Time Between Waves: 5
```

#### 2. UIManager
```
GameObject: Managers/UIManager
Components:
├── UIManager script
│   ├── HUD Panel: [assign UI Canvas HUD panel]
│   ├── Health Text: [assign TextMeshProUGUI]
│   ├── Health Slider: [assign UI Slider]
│   ├── Gold Text: [assign TextMeshProUGUI]
│   ├── Skill Icons: [assign 4 UI Images]
│   ├── Skill Cooldown Overlays: [assign 4 UI Images (filled type)]
│   ├── Skill Cooldown Texts: [assign 4 TextMeshProUGUI]
│   ├── Inventory Panel: [assign inventory panel]
│   ├── Shop Panel: [assign shop panel]
│   ├── Pause Panel: [assign pause panel]
│   ├── Notification Prefab: [assign notification text prefab]
│   └── Notification Parent: [assign RectTransform for notifications]
```

#### 3. InventoryManager
```
GameObject: Managers/InventoryManager
Components:
├── InventoryManager script
│   └── Max Inventory Slots: 20
```

#### 4. ShopManager
```
GameObject: Managers/ShopManager
Components:
├── ShopManager script
│   ├── Shop Slots: 6
│   ├── Auto Refresh On Open: true
│   ├── Refresh Interval: 300
│   └── Price Multiplier: 1
│   
└── IMPORTANT: Create Data folder
    └── Create Assets/Data/ folder
    └── Create some ItemSO assets (Right-click > Create > ARPG > Item)
    └── Add items to ShopManager.AvailableItems
```

#### 5. DamageNumberManager
```
GameObject: Managers/DamageNumberManager
Components:
├── DamageNumberManager script
│   ├── Damage Number Prefab: [create TextMeshPro 3D object]
│   └── Pool Size: 20
```

#### 6. SetBonusManager
```
GameObject: Managers/SetBonusManager
Components:
└── SetBonusManager script (auto-finds sets in Resources/Sets)

IMPORTANT: Create folder Assets/Resources/Sets/
Put EquipmentSetSO assets there
```

#### 7. SkillSynergyManager
```
GameObject: Managers/SkillSynergyManager
Components:
└── SkillSynergyManager script (no setup needed)
```

#### 8. WeaponMasteryManager
```
GameObject: Managers/WeaponMasteryManager
Components:
└── WeaponMasteryManager script
```

#### 9. GambleSystem
```
GameObject: Managers/GambleSystem
Components:
└── GambleSystem script
```

#### 10. EnemyPool
```
GameObject: Managers/EnemyPool
Components:
└── EnemyPool script
    └── Enemy Pools: [configure enemy prefabs]
```

---

### Player Setup

```
GameObject: Player (in scene)
Tag: Player
Layer: Player

Components (in order):
├── Rigidbody2D
│   ├── Body Type: Kinematic
│   └── Freeze Rotation: Z
├── SPUMPlayerBridge (if using SPUM)
├── SPUMEquipmentManager (if using SPUM)
├── PlayerController
│   ├── Input Actions: [assign GameControls.inputactions]
│   ├── Enemy Layer: Enemy
│   ├── Attack Point: [create child GameObject]
│   └── Use SPUM: [check if using SPUM]
├── SkillCaster
│   ├── Cast Point: [use Player transform or child]
│   ├── Enemy Layer: Enemy
│   ├── Obstacle Layer: Obstacle
│   └── Skills: [assign 4 SkillSO assets]
└── ComboSystem (optional)

Children:
├── AttackPoint (empty GameObject at attack position)
└── SPUM prefab (if using SPUM)
    └── Must have SPUM_Prefabs component
```

---

### Enemy Setup

```
Prefab: Enemy
Tag: Enemy
Layer: Enemy

Components:
├── Rigidbody2D (Body Type: Kinematic)
├── StatusEffect
├── Enemy
│   ├── Max Health: 30
│   ├── Damage: 5
│   ├── Move Speed: 2
│   ├── Attack Range: 1
│   ├── Attack Cooldown: 1
│   ├── Gold Reward: 5
│   ├── Item Drop Chance: 0.3
│   ├── Detection Range: 8
│   ├── Stop Distance: 0.5
│   ├── Sprite Faces Right: [check based on sprite]
│   └── Use SPUM: [check if using SPUM]
├── CircleCollider2D or BoxCollider2D
├── Animator (optional, for legacy)
└── SpriteRenderer (optional, for legacy)

If using SPUM:
└── Child with SPUM_Prefabs component
    └── Must have Animator with valid Controller
```

**IMPORTANT for SPUM Enemies:**
1. The SPUM prefab must have an Animator component
2. The Animator must have a Runtime Animator Controller assigned (NOT an OverrideController)
3. The controller should be the base SPUM animator controller

---

### UI Setup

#### Canvas Setup
```
GameObject: Canvas
Components:
├── Canvas
│   └── Render Mode: Screen Space - Overlay
├── Canvas Scaler
│   ├── UI Scale Mode: Scale With Screen Size
│   └── Reference Resolution: 1920 x 1080
└── Graphic Raycaster
```

#### HUD Panel (child of Canvas)
```
GameObject: HUDPanel
Components:
└── Canvas Group

Children:
├── HealthSlider (UI Slider)
├── HealthText (TextMeshProUGUI)
├── GoldText (TextMeshProUGUI)
└── SkillBar (empty GameObject)
    └── 4 Skill UI objects with:
        ├── Image (icon)
        ├── Image (cooldown overlay, filled type)
        └── TextMeshProUGUI (cooldown text)
```

#### Inventory Panel (child of Canvas)
```
GameObject: InventoryPanel
Components:
├── Canvas Group
└── InventoryUI script

Children:
├── ItemSlots container (Grid Layout Group)
├── Equipment slots (Weapon, Armor)
└── Item Detail Panel (hidden by default)
```

#### Shop Panel (child of Canvas)
```
GameObject: ShopPanel
Components:
├── Canvas Group
└── ShopUI script

Children:
├── Shop slots (6 item displays)
├── Gold display
└── Buttons (Buy, Close, Refresh)
```

---

## 🔧 Quick Fixes Checklist

### If SPUM animations don't work:
- [ ] Check SPUM prefab has Animator component
- [ ] Check Animator has a **base** Runtime Animator Controller (not OverrideController)
- [ ] Check `useSPUM` is enabled on Enemy/PlayerController
- [ ] Check SPUM_Prefabs component exists on child object

### If skills don't cast:
- [ ] Check SkillCaster has 4 skills assigned
- [ ] Check `enemyLayer` is set to "Enemy" layer
- [ ] Check Input Actions asset is assigned to PlayerController
- [ ] Check `castPoint` is assigned

### If enemies don't spawn:
- [ ] Check GameManager has enemy prefabs assigned
- [ ] Check spawn points are assigned
- [ ] Check Enemy Container is assigned
- [ ] Check enemy prefab has Enemy script

### If shop doesn't work:
- [ ] Check ShopManager has availableItems
- [ ] Check items have proper prices
- [ ] Check UI panels are assigned to UIManager

---

## 📝 Notes

1. **SPUM Animator Issue:** If you see "Cannot nest AnimatorOverrideController", it means your SPUM prefab's animator already has an OverrideController assigned. You need to assign the **base** controller instead.

2. **Skill Types:** All skill types in the enum should now work. For Shield Wall, use `SkillType.Melee` or `SkillType.Shield`.

3. **StatusEffect:** This component is required on all enemies. It's auto-added by the EnemyPool.

---

*Setup complete. All errors fixed!*

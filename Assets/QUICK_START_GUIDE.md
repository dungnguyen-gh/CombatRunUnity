# CombatRun - Quick Start Guide

Get your project running in 10 steps.

---

## ⚠️ BEFORE YOU START

**Critical Issues You Will Encounter (and how to fix):**

1. **"WeaponMasteryManager not found"** 
   - Solution: Use `Assets/Scripts/Managers/WeaponMasteryManager.cs` (new file)

2. **"SPUM_Prefabs not found"**
   - Solution: Expand SPUM character, drag SPUM_Prefabs component to field

3. **"DamageNumberManager: No prefab assigned"**
   - Solution: Create DamageNumber prefab (Step 5 below)

4. **"Input Actions not assigned"**
   - Solution: Drag `GameControls.inputactions` to PlayerController

---

## 🚀 10-Step Quick Start

### Step 1: Install Input System
```
Window > Package Manager > Input System > Install
Restart Unity when prompted
```

### Step 2: Configure Project
```
Edit > Project Settings > Player > Other Settings
Active Input Handling: "Both"
```

### Step 3: Setup Layers & Tags
**Tags:** `Player`, `Enemy`, `Pickup`

**Layers:**
- `Enemies` (6)
- `Pickups` (7)  
- `Player` (8)

**Physics 2D:**
- Enemies ↔ Player: ON
- Enemies ↔ Enemies: OFF
- Player ↔ Pickups: ON

### Step 4: Create Managers
```
1. Create Empty GameObject named "Managers"
2. Add these 10 components:
   - GameManager
   - UIManager
   - InventoryManager
   - ShopManager
   - SetBonusManager
   - WeaponMasteryManager ← IMPORTANT: Use the one from Managers folder!
   - SkillSynergyManager
   - DailyRunManager
   - GambleSystem
   - DamageNumberManager
```

### Step 5: Create Critical Prefabs

**A. DamageNumber Prefab**
```
1. Right-click Canvas > UI > Text - TextMeshPro
2. Name: "DamageNumber"
3. Settings:
   - Text: "999"
   - Font Size: 36
   - Alignment: Center
   - Add Outline component (Black, 0.5)
4. Drag to Assets/Prefabs/
5. Assign to: DamageNumberManager.damageNumberPrefab
```

**B. GoldPickup Prefab**
```
1. Right-click Hierarchy > 2D Object > Sprites > Circle
2. Name: "GoldPickup", Color: Yellow
3. Add CircleCollider2D (Is Trigger: true)
4. Add GoldPickup script
5. Tag: "Pickup", Layer: "Pickups"
6. Drag to Assets/Prefabs/
7. Assign to: Enemy.goldPickupPrefab (after creating enemy)
```

**C. Projectile Prefab**
```
1. Right-click Hierarchy > 2D Object > Sprites > Circle
2. Name: "Projectile", Color: Orange
3. Scale: (0.3, 0.3, 1)
4. Add Rigidbody2D (Kinematic, Gravity Scale: 0)
5. Add CircleCollider2D (Is Trigger: true)
6. Add Projectile script
7. Tag: "Projectile"
8. Drag to Assets/Prefabs/
9. Assign to: SkillCaster.projectilePrefab
```

### Step 6: Setup Player

**Add SPUM Character:**
```
1. Go to Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/
2. Drag Human/SPUM_Hero to scene
3. Add components (in order):
   - Rigidbody2D (Kinematic, Gravity 0)
   - CircleCollider2D
   - PlayerController
   - SkillCaster
   - ComboSystem
   - SPUMPlayerBridge
   - SPUMEquipmentManager
```

**Configure PlayerController:**
```
PlayerController:
├── Input Actions: [Drag GameControls.inputactions]
├── Move Speed: 5
├── Melee Range: 1.5
├── Enemy Layer: "Enemies"
├── useSPUM: ☑️
├── spumBridge: [SPUMPlayerBridge]
└── spumEquipment: [SPUMEquipmentManager]
```

**Configure SPUMPlayerBridge:**
```
SPUMPlayerBridge:
├── spumPrefabs: [Expand character, find SPUM_Prefabs, drag here]
├── idleAnimationIndex: 0
├── moveAnimationIndex: 0
└── attackAnimationIndex: 0
```

**Tag & Layer:**
- Tag: `Player`
- Layer: `Player`

### Step 7: Setup Enemy

**Create Enemy:**
```
1. Drag SPUM character (monster) to scene
2. Add components:
   - Rigidbody2D (Dynamic, Gravity 0)
   - BoxCollider2D
   - Enemy script
3. Configure Enemy:
   - useSPUM: ☑️
   - spumPrefabs: [Assign from child]
   - goldPickupPrefab: [Drag from prefabs]
4. Tag: "Enemy", Layer: "Enemies"
5. Drag to Assets/Prefabs/Enemy_Basic
6. Delete from scene
```

**Assign to GameManager:**
```
Select Managers > GameManager
Enemy Prefabs: [Add element, drag Enemy_Basic]
```

### Step 8: Setup UI

**Create Canvas:**
```
1. Right-click > UI > Canvas
2. Render Mode: Screen Space - Overlay
3. Canvas Scaler: Scale With Screen Size (1920x1080)
```

**Create EventSystem:**
```
1. Right-click > UI > Event System
2. Add Input System UI Input Module
3. Remove Standalone Input Module
```

**Create HUD:**
```
1. Right-click Canvas > UI > Panel (Name: HUDPanel)
2. Add children:
   - Slider (Name: HealthSlider) - Position left
   - TextMeshPro (Name: GoldText) - Position right
   - 4x Image (Name: Skill1-4) - Bottom center
```

**Create Panels:**
```
1. InventoryPanel (UI > Panel, add CanvasGroup, DISABLE)
2. ShopPanel (UI > Panel, add CanvasGroup, DISABLE)
3. PausePanel (UI > Panel, add CanvasGroup, DISABLE)
```

**Create Notification Prefab:**
```
1. Right-click Canvas > UI > TextMeshPro (Name: NotificationText)
2. Font Size: 24, Color: White
3. Add CanvasGroup
4. Drag to Assets/Prefabs/
5. Assign to: UIManager.notificationPrefab
```

**Configure UIManager:**
```
Select Managers > UIManager
├── Hud Panel: [HUDPanel]
├── Health Slider: [HealthSlider]
├── Gold Text: [GoldText]
├── Skill Icons: [Add 4, assign Skill1-4]
├── Inventory Panel: [InventoryPanel]
├── Shop Panel: [ShopPanel]
├── Pause Panel: [PausePanel]
└── Notification Prefab: [NotificationText prefab]
```

### Step 9: Assign References

**DamageNumberManager:**
```
Select Managers > DamageNumberManager
damageNumberPrefab: [DamageNumber prefab]
```

**SkillCaster (on Player):**
```
Select Player > SkillCaster
projectilePrefab: [Projectile prefab]
```

**Enemy Prefab (if not done):**
```
Open Enemy_Basic prefab
goldPickupPrefab: [GoldPickup prefab]
```

### Step 10: Test!

**Press Play and Check:**
- [ ] WASD moves player
- [ ] Player faces movement direction
- [ ] Space/Click attacks
- [ ] SPUM animations play
- [ ] Enemy chases player
- [ ] Damage numbers appear
- [ ] Gold drops from enemies

---

## 🔧 TROUBLESHOOTING

### "SPUM_Prefabs not found!"
```
1. Expand your SPUM character GameObject
2. Look for child with "SPUM_Prefabs" component
3. Drag it to SPUMPlayerBridge.spumPrefabs
```

### "WeaponMasteryManager not found"
```
1. Make sure you're using Assets/Scripts/Managers/WeaponMasteryManager.cs
2. Not the old file in Data folder
```

### "Input Actions not assigned"
```
1. Select Player
2. Find "Input Actions" field in PlayerController
3. Drag GameControls.inputactions from Assets/InputSystem/
```

### "DamageNumberManager: No prefab"
```
Create the prefab first! See Step 5A above.
```

### Enemy not moving
```
Check:
- Enemy Tag = "Enemy"
- Enemy Layer = "Enemies"
- Rigidbody2D Body Type = "Dynamic"
- Has Enemy script enabled
```

### No damage numbers
```
Check:
- DamageNumber prefab assigned
- Prefab has TextMeshPro component
- DamageNumberManager pool size > 0
```

---

## 📋 MINIMUM REQUIRED FILES

Before running, these MUST exist:
- [ ] `Assets/InputSystem/GameControls.inputactions`
- [ ] `Assets/Prefabs/DamageNumber.prefab`
- [ ] `Assets/Prefabs/GoldPickup.prefab`
- [ ] `Assets/Prefabs/Projectile.prefab`
- [ ] `Assets/Prefabs/Enemy_Basic.prefab`
- [ ] `Assets/Prefabs/NotificationText.prefab`

---

## 🎯 SUCCESS CRITERIA

Your project is working when:
1. Player moves with WASD
2. Player attacks with Space/Click
3. SPUM animations play correctly
4. Enemy chases and attacks
5. Damage numbers appear on hits
6. Gold pickups spawn and can be collected

---

## 📚 FULL DOCUMENTATION

| File | Use When... |
|------|-------------|
| `QUICK_START_GUIDE.md` | You need to get running fast (this file) |
| `SETUP_DETAILED.md` | You need step-by-step for every component |
| `IMPLEMENTATION_PLAN.md` | You want to understand the project structure |
| `BUG_FIXES_SUMMARY.md` | You're debugging errors |
| `SPUM_INTEGRATION_README.md` | SPUM setup issues |

---

*Last Updated: With Critical Bug Fixes and Prefab Requirements*

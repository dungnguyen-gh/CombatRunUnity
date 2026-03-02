# CombatRun - Next Steps Plan

Clear action items to get your game running and tested.

---

## 🎯 CURRENT STATUS

**Phase:** 4 (Content & Polish) - Setup Required  
**SPUM Integration:** Code Complete, Needs Field Assignment  
**Critical Blockers:** Missing Prefabs

---

## 📋 PRIORITY 1: CRITICAL SETUP (Do First)

These are BLOCKING - the game won't run properly without them.

### Step 1: Create Required Prefabs (30 minutes)

#### A. DamageNumber Prefab
```
1. Right-click Canvas → UI → Text - TextMeshPro
2. Name: "DamageNumber"
3. Configure:
   - Text: "999"
   - Font Size: 36
   - Alignment: Center
   - Color: White
4. Add Component: Outline
   - Effect Color: Black
   - Effect Distance: 0.5
5. Drag to Assets/Prefabs/ folder
6. Assign to: Managers/DamageNumberManager.damageNumberPrefab
```

#### B. GoldPickup Prefab
```
1. Right-click Hierarchy → 2D Object → Sprites → Circle
2. Name: "GoldPickup", Color: Yellow
3. Add CircleCollider2D: Is Trigger = true
4. Add Script: GoldPickup
5. Tag: "Pickup", Layer: "Pickups"
6. Drag to Assets/Prefabs/
7. Assign to: Enemy prefab (goldPickupPrefab field)
```

#### C. Projectile Prefab
```
1. Right-click Hierarchy → 2D Object → Sprites → Circle
2. Name: "Projectile", Color: Orange
3. Scale: (0.3, 0.3, 1)
4. Add Rigidbody2D: Body Type = Kinematic, Gravity = 0
5. Add CircleCollider2D: Is Trigger = true
6. Add Script: Projectile
7. Tag: "Projectile"
8. Drag to Assets/Prefabs/
9. Assign to: Player/SkillCaster.projectilePrefab
```

#### D. NotificationText Prefab
```
1. Right-click Canvas → UI → Text - TextMeshPro
2. Name: "NotificationText"
3. Font Size: 24, Color: White
4. Add CanvasGroup component
5. Drag to Assets/Prefabs/
6. Assign to: Managers/UIManager.notificationPrefab
```

---

### Step 2: Setup Managers (15 minutes)

Create empty GameObject "Managers" and add:

| Order | Component | Critical Fields to Assign |
|-------|-----------|---------------------------|
| 1 | GameManager | enemyPrefabs[], player |
| 2 | UIManager | All panels, notificationPrefab |
| 3 | InventoryManager | None |
| 4 | ShopManager | None |
| 5 | SetBonusManager | None |
| 6 | **WeaponMasteryManager** | None (from Managers folder!) |
| 7 | SkillSynergyManager | None |
| 8 | DailyRunManager | None |
| 9 | GambleSystem | None |
| 10 | **DamageNumberManager** | **damageNumberPrefab** |

**Important:** Use `WeaponMasteryManager` from `Assets/Scripts/Managers/` NOT from Data folder!

---

### Step 3: Setup Player with SPUM (20 minutes)

#### A. Add Components
```
1. Drag SPUM character to scene
2. Add Rigidbody2D: Kinematic, Gravity Scale = 0
3. Add CircleCollider2D
4. Add PlayerController
5. Add SkillCaster
6. Add ComboSystem
7. Add SPUMPlayerBridge
8. Add SPUMEquipmentManager
```

#### B. Configure PlayerController
```yaml
Input Actions: [Drag GameControls.inputactions]
Move Speed: 5
Melee Range: 1.5
Enemy Layer: Enemies
useSPUM: ☑️ CHECK THIS
spumBridge: [Drag SPUMPlayerBridge]
spumEquipment: [Drag SPUMEquipmentManager]
```

#### C. Configure SPUMPlayerBridge (CRITICAL!)
```yaml
spumPrefabs: [EXPAND CHARACTER, FIND SPUM_Prefabs, DRAG HERE]
idleAnimationIndex: 0
moveAnimationIndex: 0
attackAnimationIndex: 0
skillAnimationIndices: Size 4, values [1, 1, 1, 1]
```

#### D. Configure SkillCaster
```yaml
projectilePrefab: [Drag Projectile prefab]
enemyLayer: Enemies
```

#### E. Tag & Layer
```
Tag: Player
Layer: Player
```

---

### Step 4: Setup Enemy with SPUM (15 minutes)

#### A. Create Enemy Prefab
```
1. Drag SPUM monster to scene
2. Add Rigidbody2D: Dynamic, Gravity Scale = 0
3. Add BoxCollider2D
4. Add Enemy script
```

#### B. Configure Enemy (CRITICAL!)
```yaml
useSPUM: ☑️ CHECK THIS
spumPrefabs: [EXPAND CHARACTER, FIND SPUM_Prefabs, DRAG HERE]
idleAnimationIndex: 0
moveAnimationIndex: 0
attackAnimationIndex: 0
hitAnimationIndex: 0
deathAnimationIndex: 0
goldPickupPrefab: [Drag GoldPickup prefab]
```

#### C. Tag & Layer
```
Tag: Enemy
Layer: Enemies
```

#### D. Make Prefab
```
1. Drag from scene to Assets/Prefabs/
2. Name: "Enemy_Basic"
3. Delete from scene
4. Assign to: Managers/GameManager.enemyPrefabs[0]
```

---

### Step 5: Setup UI (15 minutes)

#### A. Canvas & EventSystem
```
1. Create UI → Canvas
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler: Scale With Screen Size (1920x1080)
2. Create UI → Event System
   - Add Input System UI Input Module
   - Remove Standalone Input Module
```

#### B. HUD Panel
```
1. Create UI → Panel (Name: HUDPanel)
2. Add children:
   - Slider (Name: HealthSlider) - Left side
   - TextMeshPro (Name: GoldText) - Right side
   - 4x Image (Name: Skill1-4) - Bottom center
```

#### C. Other Panels
```
1. InventoryPanel (UI Panel + CanvasGroup, DISABLED)
2. ShopPanel (UI Panel + CanvasGroup, DISABLED)
3. PausePanel (UI Panel + CanvasGroup, DISABLED)
```

#### D. Assign to UIManager
```yaml
hudPanel: [HUDPanel]
healthSlider: [HealthSlider]
goldText: [GoldText]
skillIcons: [Add 4, assign Skill1-4]
inventoryPanel: [InventoryPanel]
shopPanel: [ShopPanel]
pausePanel: [PausePanel]
notificationPrefab: [NotificationText prefab]
```

---

## 📋 PRIORITY 2: TESTING PLAN (Do After Setup)

### Test 1: Basic Movement & Animation
```
[ ] Press Play
[ ] Press WASD - Player moves
[ ] Check SPUM walk animation plays
[ ] Check player faces movement direction
```

### Test 2: Combat
```
[ ] Press Space - Attack animation plays
[ ] Enemy takes damage
[ ] Damage number appears
[ ] Enemy hit animation plays
```

### Test 3: Enemy AI
```
[ ] Enemy chases player when close
[ ] Enemy walks animation plays while chasing
[ ] Enemy attacks when in range
[ ] Enemy drops gold on death
```

### Test 4: Skills
```
[ ] Press 1 - Skill casts, animation plays
[ ] Cooldown overlay shows
[ ] Projectile spawns (if projectile skill)
```

### Test 5: UI
```
[ ] Press I - Inventory opens
[ ] Press Escape - Pause menu opens
[ ] Gold text updates
[ ] Health slider updates
```

---

## 🔧 TROUBLESHOOTING COMMON ISSUES

### "SPUM_Prefabs not found"
```
✓ Expand your SPUM character GameObject
✓ Look for child object with "SPUM_Prefabs" component
✓ Drag it to the field in Inspector
```

### "WeaponMasteryManager not found"
```
✓ Use file at: Assets/Scripts/Managers/WeaponMasteryManager.cs
✓ NOT the old one in Data folder
```

### "DamageNumberManager: No prefab"
```
✓ Create prefab first (Step 1A above)
✓ Assign to DamageNumberManager component
```

### Enemy not moving
```
✓ Check Tag = "Enemy"
✓ Check Layer = "Enemies"
✓ Check Rigidbody2D Body Type = "Dynamic"
✓ Check Enemy script is enabled
```

### No damage numbers
```
✓ Check prefab assigned
✓ Check prefab has TextMeshPro component
✓ Check DamageNumberManager pool size > 0
```

---

## 📊 ESTIMATED TIME

| Task | Time |
|------|------|
| Create Prefabs | 30 min |
| Setup Managers | 15 min |
| Setup Player | 20 min |
| Setup Enemy | 15 min |
| Setup UI | 15 min |
| Testing | 15 min |
| **TOTAL** | **~2 hours** |

---

## ✅ SUCCESS CRITERIA

Your game is working when:
- ✅ Player moves with WASD and SPUM animates
- ✅ Player attacks with Space and SPUM animates
- ✅ Enemy chases player and SPUM animates
- ✅ Damage numbers appear on hits
- ✅ Gold drops from enemies
- ✅ UI panels open/close correctly

---

## 📚 REFERENCE DOCUMENTS

| File | Use When... |
|------|-------------|
| `NEXT_STEPS_PLAN.md` | This file - follow step by step |
| `SPUM_INTEGRATION_REVIEW.md` | SPUM not working correctly |
| `BUG_FIXES_SUMMARY.md` | Debugging errors |
| `SETUP_DETAILED.md` | Need more detailed instructions |
| `IMPLEMENTATION_PLAN.md` | Understand project structure |

---

*Follow this plan in order. Don't skip steps!*

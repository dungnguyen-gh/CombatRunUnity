# CombatRun - Implementation Plan

A 2D top-down action RPG with deep combat mechanics, progression systems, and roguelite elements.

**Input System:** Unity Input System Package (Modern, event-driven input handling)  
**Character System:** SPUM (2D Survival Character) with animation override controllers  
**Status:** Phase 4 (Content & Polish) - IN PROGRESS

---

## 📋 Core Game Loop

```
Move (WASD/Arrows) → Attack/Skills → Kill Enemies → Collect Loot → 
Shop/Buy Items → Equip Upgrades → Face Boss → Victory!
```

**Input:** Unity Input System Package with GameControls input actions

---

## 🐛 BUG FIXES STATUS

### Critical Bugs (FIXED)
| Bug | File | Fix |
|-----|------|-----|
| Lambda delegate memory leak | PlayerController.cs | Store delegates as fields |
| Event subscription leak | GameManager.cs | Unsubscribe in death handlers |
| Broken SO GUIDs | Resources/Items/*.asset | Fixed all 7 asset files |
| WeaponMasteryManager missing | New file created | Separated from data class |
| SPUM animation null refs | Enemy.cs | Added null checks |

### High Priority Issues (REQUIRE ATTENTION)
| Issue | Location | Action Required |
|-------|----------|-----------------|
| DamageNumber prefab missing | DamageNumberManager | Create prefab |
| GoldPickup prefab missing | Enemy script | Create prefab |
| Projectile prefab missing | SkillCaster | Create prefab |
| Enemy prefabs missing | GameManager | Create prefabs |
| Notification prefab missing | UIManager | Create prefab |

---

## 🗓️ Development Timeline

### Phase 1 - Core Foundation ✅ COMPLETE
**Goal**: Playable movement, combat, and basic enemies

| Day | Task | Status |
|-----|------|--------|
| 1 | Project setup, Input System | ✅ |
| 2 | Player movement + SPUM facing | ✅ |
| 3 | Melee attack with animations | ✅ |
| 4 | Skill system + cooldowns | ✅ |
| 5 | Enemy AI + SPUM support | ✅ |
| 6-7 | Manager singletons setup | ✅ |

**Key Fixes:**
- PlayerController lambda delegates fixed
- Input System Package integration complete
- SPUM animation bridge working

---

### Phase 2 - Progression Systems ✅ COMPLETE
**Goal**: Items, inventory, equipment, and stats

| Day | Task | Status |
|-----|------|--------|
| 1 | Item ScriptableObjects | ✅ |
| 2 | Inventory UI with animations | ✅ |
| 3 | Equipment system | ✅ |
| 4 | Shop system with rarity cache | ✅ |
| 5 | Item preview with SPUM | ✅ |
| 6-7 | All managers configured | ✅ |

**Key Fixes:**
- WeaponMasteryManager file separated
- All ItemSO/SetSO GUIDs fixed
- Shop rarity cache implemented

---

### Phase 3 - Advanced Combat ✅ COMPLETE
**Goal**: Deep combat mechanics and build variety

| Day | Task | Status |
|-----|------|--------|
| 1 | Combo system | ✅ |
| 2 | Status effects (Burn, Freeze, etc.) | ✅ |
| 3 | Skill synergies | ✅ |
| 4 | Equipment set bonuses | ✅ |
| 5 | Weapon mastery system | ✅ |
| 6-7 | Integration & testing | ✅ |

**Key Fixes:**
- [RequireComponent] attributes added
- Event subscription leaks fixed
- Null checks added throughout

---

### Phase 4 - Content & Polish 🎯 CURRENT
**Goal**: Boss, prefabs, UI polish, replayability

| Day | Task | Status | Notes |
|-----|------|--------|-------|
| 1 | Create all prefabs | 🔄 | See Prefab Checklist below |
| 2 | Boss enemy setup | 🔄 | Needs prefab |
| 3 | Gamble system | ✅ | Ready |
| 4 | Daily run modifiers | ✅ | Ready |
| 5 | UI animations | ✅ | CanvasGroup fade working |
| 6 | Audio & VFX | 🔄 | Needs effect prefabs |
| 7 | Testing & build | 🔄 | Blocked by prefabs |

**Acceptance Criteria:** Complete run from spawn to boss, no console errors.

---

## 📦 PREFAB CREATION CHECKLIST (REQUIRED)

### Critical Prefabs (Must Create)

#### 1. DamageNumber Prefab
```
Components:
├── TextMeshPro (Text: "999", Font Size: 36, Center alignment)
├── Outline (Effect Color: Black, Distance: 0.5)
└── CanvasGroup (optional)

Assign to: DamageNumberManager.damageNumberPrefab
```

#### 2. GoldPickup Prefab
```
Components:
├── SpriteRenderer (Sprite: Circle, Color: Yellow)
├── CircleCollider2D (Is Trigger: true)
├── GoldPickup script
└── Tag: "Pickup", Layer: "Pickups"

Assign to: Enemy.goldPickupPrefab
```

#### 3. Projectile Prefab
```
Components:
├── SpriteRenderer (Sprite: Circle, Color: Orange, Scale: 0.3)
├── Rigidbody2D (Body Type: Kinematic, Gravity Scale: 0)
├── CircleCollider2D (Is Trigger: true)
├── Projectile script
└── Tag: "Projectile"

Assign to: SkillCaster.projectilePrefab
```

#### 4. Enemy_Basic Prefab
```
Components:
├── SpriteRenderer or SPUM_Prefabs
├── Rigidbody2D (Dynamic, Gravity Scale: 0)
├── BoxCollider2D
├── Enemy script
│   ├── useSPUM: true (if using SPUM)
│   ├── spumPrefabs: [assigned]
│   ├── goldPickupPrefab: [assigned]
│   └── itemDropPrefabs: [optional]
├── Tag: "Enemy"
└── Layer: "Enemies"

Assign to: GameManager.enemyPrefabs[0]
```

#### 5. NotificationText Prefab
```
Components:
├── TextMeshPro (Font Size: 24, Color: White)
└── CanvasGroup

Assign to: UIManager.notificationPrefab
```

#### 6. Boss Prefab (Optional for now)
```
Same as Enemy_Basic but with higher stats
Assign to: GameManager.bossPrefab
```

---

## 🎨 SPUM Integration Status

### Player SPUM Setup ✅ COMPLETE
**Required Components:**
- SPUMPlayerBridge (assigned to PlayerController.spumBridge)
- SPUMEquipmentManager (assigned to PlayerController.spumEquipment)

**Configuration:**
```yaml
SPUMPlayerBridge:
  spumPrefabs: [SPUM_Prefabs component from child]
  idleAnimationIndex: 0
  moveAnimationIndex: 0
  attackAnimationIndex: 0
  skillAnimationIndices: [1, 1, 1, 1]
```

### Enemy SPUM Setup ✅ COMPLETE
**Configuration:**
```yaml
Enemy:
  useSPUM: true
  spumPrefabs: [SPUM_Prefabs component from child]
  idleAnimationIndex: 0
  moveAnimationIndex: 0
  attackAnimationIndex: 0
  hitAnimationIndex: 0
  deathAnimationIndex: 0
```

**Animation States Used:**
- IDLE - Standing still
- MOVE - Walking/chasing
- ATTACK - Attack animation
- DAMAGED - Hit reaction
- DEATH - Death animation

---

## 🎮 Feature Implementation Status

### Core Combat
| Feature | Status | Notes |
|---------|--------|-------|
| Input System (WASD/Arrows) | ✅ | Fully working |
| Melee Attack (Space/Click) | ✅ | With SPUM animations |
| Skills (1-4) | ✅ | Cooldowns working |
| Combo System | ✅ | Requires ComboSystem component |
| SPUM Player Animations | ✅ | All states implemented |
| SPUM Enemy Animations | ✅ | All states implemented |

### Progression Systems
| Feature | Status | Notes |
|---------|--------|-------|
| Gold Economy | ✅ | Drops working |
| Weapon Mastery | ✅ | Manager ready, needs kills |
| Equipment Sets | ✅ | SetBonusManager ready |
| Skill Synergies | ✅ | Sequence tracking ready |
| Shop | ✅ | Rarity cache implemented |

### UI Systems
| Feature | Status | Notes |
|---------|--------|-------|
| HUD (Health/Gold/Skills) | ✅ | Working |
| Inventory Panel | ✅ | CanvasGroup fade |
| Shop Panel | ✅ | CanvasGroup fade |
| Pause Panel | ✅ | CanvasGroup fade |
| Damage Numbers | 🔄 | Needs prefab |
| Notifications | 🔄 | Needs prefab |

---

## 🔧 MANAGERS SETUP CHECKLIST

Create empty GameObject "Managers" with these components:

| # | Component | DontDestroy | Setup Required |
|---|-----------|-------------|----------------|
| 1 | GameManager | ✅ | enemyPrefabs[], player |
| 2 | UIManager | ✅ | All panels, notificationPrefab |
| 3 | InventoryManager | ✅ | None |
| 4 | ShopManager | ✅ | None |
| 5 | SetBonusManager | ✅ | None |
| 6 | WeaponMasteryManager | ✅ | None |
| 7 | SkillSynergyManager | ✅ | None |
| 8 | DailyRunManager | ✅ | None |
| 9 | GambleSystem | ✅ | None |
| 10 | DamageNumberManager | ✅ | damageNumberPrefab |

---

## 📋 REQUIRED SETUP ORDER

### Step 1: Project Configuration
1. Install Input System Package
2. Set Active Input Handling to "Both"
3. Create Tags: Player, Enemy, Pickup, Projectile
4. Create Layers: Enemies(6), Pickups(7), Player(8)
5. Configure Physics 2D collision matrix

### Step 2: Create Prefabs
1. Create DamageNumber prefab → Assign to DamageNumberManager
2. Create GoldPickup prefab → Assign to Enemy
3. Create Projectile prefab → Assign to SkillCaster
4. Create Enemy_Basic prefab → Assign to GameManager
5. Create NotificationText prefab → Assign to UIManager

### Step 3: Setup Player
1. Add SPUM character to scene
2. Add components: Rigidbody2D, PlayerController, SkillCaster, ComboSystem
3. Add SPUMPlayerBridge (assign spumPrefabs)
4. Add SPUMEquipmentManager
5. Assign Input Actions (GameControls.inputactions)
6. Tag: Player, Layer: Player

### Step 4: Setup Managers
1. Create Managers GameObject
2. Add all 10 manager components
3. Assign all prefab references
4. Assign UI panel references

### Step 5: Setup Enemy
1. Add SPUM character to scene (or use sprite)
2. Add Enemy script
3. Configure: useSPUM, spumPrefabs, goldPickupPrefab
4. Tag: Enemy, Layer: Enemies
5. Make prefab, assign to GameManager

### Step 6: Setup UI
1. Create Canvas
2. Create EventSystem with Input System UI Input Module
3. Create HUD Panel with all UI elements
4. Create Inventory/Shop/Pause panels (disabled by default)
5. Assign all to UIManager

### Step 7: Test
1. Movement (WASD/Arrows)
2. Attack (Space/Click)
3. Skills (1-4)
4. Enemy AI (chase/attack)
5. Damage numbers appear
6. Gold pickup works

---

## 🐛 KNOWN ISSUES & WORKAROUNDS

| Issue | Workaround |
|-------|------------|
| "SPUM_Prefabs not found" | Manually assign spumPrefabs field |
| "DamageNumberManager: No prefab" | Create prefab before running |
| "WeaponMasteryManager missing" | Add component from Managers folder |
| "Input Actions not assigned" | Assign GameControls.inputassets |
| Enemy not moving | Check Tag="Enemy", Layer="Enemies" |
| No damage numbers | Check prefab assigned and pool size |

---

## 🎯 IMMEDIATE NEXT STEPS

### Priority 1: Create Prefabs (Blocking)
- [ ] DamageNumber prefab
- [ ] GoldPickup prefab
- [ ] Projectile prefab
- [ ] Enemy_Basic prefab
- [ ] NotificationText prefab

### Priority 2: Assign References (Blocking)
- [ ] DamageNumberManager.damageNumberPrefab
- [ ] Enemy.goldPickupPrefab
- [ ] SkillCaster.projectilePrefab
- [ ] GameManager.enemyPrefabs[]
- [ ] UIManager.notificationPrefab

### Priority 3: Test Core Loop
- [ ] Player movement
- [ ] Player attack
- [ ] Enemy chase/attack
- [ ] Damage numbers
- [ ] Gold pickup

---

## 📚 Documentation

| File | Purpose |
|------|---------|
| `FEATURES_SUMMARY.md` | Features and design |
| `IMPLEMENTATION_PLAN.md` | This file - roadmap |
| `SETUP_DETAILED.md` | Step-by-step setup |
| `SETUP_README.md` | Quick setup reference |
| `BUG_FIXES_SUMMARY.md` | All bugs found/fixed |
| `INPUT_SYSTEM_SETUP.md` | Input System guide |
| `SPUM_INTEGRATION_README.md` | SPUM setup |
| `TEST_GUIDE.md` | Testing procedures |

---

## 📝 NOTES

- Use `ScriptableObjects` for all data-driven content
- Object pool projectiles and effects
- Use events for loose coupling between systems
- Balance numbers through playtesting, not guessing
- Polish is 50% of the work - audio and VFX matter!

### Critical Implementation Notes
1. **Input System**: Always store lambda delegates for proper unsubscription
2. **SPUM**: Use `PlayerState` enum (not strings) for animations
3. **Events**: Always unsubscribe in OnDestroy
4. **Prefabs**: Create all prefabs BEFORE running scene
5. **Managers**: All 10 managers must be in scene

---

*Last Updated: With Complete Bug Review and Prefab Checklist*

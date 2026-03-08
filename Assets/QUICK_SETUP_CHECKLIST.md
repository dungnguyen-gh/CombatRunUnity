# Quick Setup Checklist - Combat Run

Use this checklist to get your game running quickly and correctly.

---

## PHASE 1: Unity Project Setup (Do First!)

### 1. Install Required Packages
```
Window → Package Manager → Unity Registry
```
- [ ] **Input System** (com.unity.inputsystem) - REQUIRED
- [ ] **TextMeshPro** (com.unity.textmeshpro) - Usually pre-installed
- [ ] **SPUM** (from Asset Store) - Optional but recommended

### 2. Configure Input System
```
Edit → Project Settings → Player → Configuration
```
- [ ] **Active Input Handling**: Set to `Input System Package (New)` or `Both`
- [ ] Unity will restart - click "Yes" when prompted

### 3. Create Layers & Tags
```
Edit → Project Settings → Tags and Layers
```
**Tags (add these):**
- [ ] `Player`
- [ ] `Enemy`
- [ ] `Pickup`
- [ ] `Projectile`

**Layers (add these):**
- [ ] `Enemies` (User Layer 6)
- [ ] `Pickups` (User Layer 7)
- [ ] `Player` (User Layer 8)

### 4. Set Physics Collision Matrix
```
Edit → Project Settings → Physics 2D → Layer Collision Matrix
```
| | Default | Enemies | Pickups | Player |
|---|:---:|:---:|:---:|:---:|
| **Default** | ✓ | ✓ | | ✓ |
| **Enemies** | ✓ | | | ✓ |
| **Pickups** | | | | ✓ |
| **Player** | ✓ | ✓ | ✓ | |

### 5. Create Sorting Layers
```
Edit → Project Settings → Tags and Layers → Sorting Layers
```
Add in this order (back to front):
1. [ ] `Background`
2. [ ] `Ground`
3. [ ] `Items`
4. [ ] `Characters`
5. [ ] `Projectiles`
6. [ ] `Effects`
7. [ ] `UI`

---

## PHASE 2: Create Folders

Create this folder structure in Assets:
```
Assets/
├── Resources/
│   ├── Items/          (for ItemSO assets)
│   ├── Sets/           (for EquipmentSetSO assets)
│   └── Skills/         (for SkillSO assets)
├── Prefabs/
│   ├── Projectiles/    (for projectile prefabs)
│   ├── Effects/        (for skill effect prefabs)
│   └── UI/             (for UI prefabs)
└── SPUM/               (SPUM asset folder - if using)
```

---

## PHASE 3: Player Setup

### 1. Create Player GameObject
- [ ] Right-click Hierarchy → Create Empty → Name: `Player`
- [ ] Set Tag: `Player`

### 2. Add Required Components (in this order)

#### Rigidbody2D
- [ ] Body Type: `Kinematic`
- [ ] Gravity Scale: `0`
- [ ] Constraints: ☑️ Freeze Rotation (Z)

#### PlayerController Script
**Critical Fields to Assign:**
- [ ] **Input Actions**: Create or assign `GameControls.inputactions`
- [ ] **Enemy Layer**: Select `Enemies` layer
- [ ] **Attack Point**: Create child empty called "AttackPoint", position at (0, 0.5, 0)

#### SkillCaster Script
- [ ] **Cast Point**: Use Player transform or AttackPoint
- [ ] **Enemy Layer**: Select `Enemies` layer
- [ ] **Player**: Will auto-find (leave empty if on same object)

### 3. Create Input Action Asset (if not exists)
```
Right-click Project → Create → Input Actions → Name: GameControls
```
**Create Action Map "Gameplay":**

| Action | Type | Bindings |
|--------|------|----------|
| `Move` | Value (Vector2) | W/A/S/D or Arrows |
| `Attack` | Button | Left Mouse Button |
| `Skill1` | Button | Key: 1 |
| `Skill2` | Button | Key: 2 |
| `Skill3` | Button | Key: 3 |
| `Skill4` | Button | Key: 4 |
| `Inventory` | Button | Key: I |
| `Pause` | Button | Key: Escape |

- [ ] Click **Save Asset**
- [ ] Assign to PlayerController's **Input Actions** field

---

## PHASE 4: SPUM Integration (If Using SPUM)

### 1. Add SPUM Character
- [ ] Find SPUM prefab: `Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/Human/`
- [ ] Drag SPUM prefab into scene as **CHILD** of Player
- [ ] Reset SPUM child transform to (0, 0, 0)
- [ ] Remove any colliders/rigidbodies from SPUM child (keep on parent only)

### 2. Add SPUM Scripts to Player
- [ ] **SPUMPlayerBridge** (on Player root)
- [ ] **SPUMEquipmentManager** (on Player root)

### 3. Configure PlayerController for SPUM
```
PlayerController Inspector:
```
- [ ] ☑️ **Use SPUM** (CHECK THIS!)
- [ ] **SPUM Bridge**: Assign SPUMPlayerBridge component
- [ ] **SPUM Equipment**: Assign SPUMEquipmentManager component
- [ ] ☑️ **Use VFX Damage Flash** (Recommended)

### 4. Configure SPUMPlayerBridge
- [ ] **SPUM Prefabs**: Drag the child SPUM_Prefabs component
- [ ] **Idle Animation Index**: 0
- [ ] **Move Animation Index**: 0
- [ ] **Attack Animation Index**: 0
- [ ] **Skill Animation Indices**: [1, 1, 1, 1]

---

## PHASE 5: Managers Setup

Create empty GameObject `Managers` in scene, then add children:

### 1. GameManager
- [ ] Create child: `GameManager`
- [ ] Add `GameManager` script

**Create Required Objects:**
- [ ] Create empty `SpawnPoints` container
- [ ] Create 4+ empty children as spawn positions around map
- [ ] Create empty `BossSpawnPoint` at boss location
- [ ] Create empty `EnemyContainer` for spawned enemies

**Assign to GameManager:**
- [ ] **Spawn Points**: Assign the spawn point empties
- [ ] **Enemy Prefabs**: Create/assign later (see Phase 8)
- [ ] **Boss Prefab**: Create/assign later
- [ ] **Boss Spawn Point**: Assign boss spawn empty
- [ ] **Enemy Container**: Assign EnemyContainer

### 2. UIManager
- [ ] Create child: `UIManager`
- [ ] Add `UIManager` script

**Will be configured with UI in Phase 6**

### 3. InventoryManager
- [ ] Create child: `InventoryManager`
- [ ] Add `InventoryManager` script
- [ ] Set **Max Inventory Slots**: 20

### 4. ShopManager
- [ ] Create child: `ShopManager`
- [ ] Add `ShopManager` script
- [ ] Set **Shop Slots**: 6
- [ ] **Available Items**: Add ItemSO assets (create in Phase 7)

### 5. DamageNumberManager
- [ ] Create child: `DamageNumberManager`
- [ ] Add `DamageNumberManager` script
- [ ] **Pool Size**: 20

### 6. SetBonusManager
- [ ] Create child: `SetBonusManager`
- [ ] Add `SetBonusManager` script

### 7. SkillSynergyManager (Optional)
- [ ] Create child: `SkillSynergyManager`
- [ ] Add `SkillSynergyManager` script

### 8. WeaponMasteryManager
- [ ] Create child: `WeaponMasteryManager`
- [ ] Add `WeaponMasteryManager` script

### 9. GambleSystem (Optional)
- [ ] Create child: `GambleSystem`
- [ ] Add `GambleSystem` script
- [ ] Assign Player, Inventory, Shop references

---

## PHASE 6: UI Setup

### 1. Create Canvas
- [ ] Right-click → UI → Canvas
- [ ] **Render Mode**: Screen Space - Overlay
- [ ] **Canvas Scaler**: Scale With Screen Size
- [ ] **Reference Resolution**: 1920 x 1080

### 2. Create EventSystem
- [ ] Right-click → UI → Event System
- [ ] Add `Input System UI Input Module` (replaces Standalone Input Module)
- [ ] Assign `GameControls` input actions

### 3. Create HUD Panel
- [ ] Create Panel under Canvas: `HUDPanel`
- [ ] Anchor: Stretch-Stretch (fills screen)

**Add Children:**

#### Health Bar
- [ ] Create Slider → Name: `HealthSlider`
- [ ] Direction: Left to Right
- [ ] Remove Handle Slide Area
- [ ] Background: Dark color | Fill: Red

#### Health Text
- [ ] Create TextMeshPro: `HealthText`
- [ ] Text: "100/100"

#### Gold Text
- [ ] Create TextMeshPro: `GoldText`
- [ ] Text: "0"
- [ ] Position: Top-right

#### Skill Bar
- [ ] Create Empty: `SkillBar`
- [ ] Add Horizontal Layout Group
- [ ] Create 4 children: `Skill1`, `Skill2`, `Skill3`, `Skill4`

Each Skill child needs:
- [ ] Image (background/icon)
- [ ] Child Image named "CooldownOverlay" (Filled type, Radial 360)
- [ ] Child TextMeshPro named "CooldownText"

### 4. Create Inventory Panel
- [ ] Create Panel: `InventoryPanel`
- [ ] Add `CanvasGroup` component
- [ ] Set **Active: False** (hidden by default)
- [ ] Add `AutoBindingInventoryUI` script
- [ ] Create `ItemSlots` container with Grid Layout Group
- [ ] Assign prefabs to AutoBindingInventoryUI

### 5. Create Shop Panel
- [ ] Create Panel: `ShopPanel`
- [ ] Add `CanvasGroup` component
- [ ] Set **Active: False**
- [ ] Add children:
  - [ ] `ShopItems` container (for shop items)
  - [ ] Gold display text
  - [ ] Close button
  - [ ] Refresh button

### 6. Assign to UIManager
```
Select UIManager GameObject, assign:
```
- [ ] **HUD Panel**: HUDPanel
- [ ] **Health Text**: HealthText
- [ ] **Health Slider**: HealthSlider
- [ ] **Gold Text**: GoldText
- [ ] **Skill Icons**: [Skill1, Skill2, Skill3, Skill4] images
- [ ] **Skill Cooldown Overlays**: [CooldownOverlay from each skill]
- [ ] **Skill Cooldown Texts**: [CooldownText from each skill]
- [ ] **Inventory Panel**: InventoryPanel
- [ ] **Shop Panel**: ShopPanel
- [ ] **Pause Panel**: Create or assign

---

## PHASE 7: Create ScriptableObjects

### 1. Create ItemSO Assets
```
Right-click → Create → ARPG → Item
```

Create at least these items in `Assets/Resources/Items/`:

| Item Name | Slot | Rarity | Damage | Defense | Weapon Type |
|-----------|------|--------|--------|---------|-------------|
| Iron Sword | Weapon | Common | 5 | 0 | Sword |
| Steel Sword | Weapon | Uncommon | 10 | 0 | Sword |
| Leather Armor | Armor | Common | 0 | 3 | - |
| Steel Armor | Armor | Uncommon | 0 | 6 | - |

**Required Fields:**
- [ ] Item ID (unique, no spaces)
- [ ] Item Name
- [ ] Icon sprite
- [ ] Price

### 2. Create EquipmentSetSO (Optional)
```
Right-click → Create → ARPG → Equipment Set
```
- [ ] Set ID (must match ItemSO setId)
- [ ] Add 2-piece bonuses
- [ ] Assign items to set

### 3. Create SkillSO Assets
```
Right-click → Create → ARPG → Skill
```

Create 4 skills for testing:

| Skill | Type | Slot | Cooldown | Damage Mult |
|-------|------|------|----------|-------------|
| Spin Attack | CircleAOE | 0 | 5 | 1.2 |
| Fireball | Projectile | 1 | 5 | 1.5 |
| Meteor | GroundAOE | 2 | 8 | 2.0 |
| Shield | Shield | 3 | 12 | - |

**Assign to Player:**
- [ ] Select Player → SkillCaster
- [ ] Set Skills array size: 4
- [ ] Drag skills into slots 0-3

---

## PHASE 8: Create Prefabs

### 1. Enemy Prefab
- [ ] Create Sprite in scene (Square or your sprite)
- [ ] Set Tag: `Enemy`, Layer: `Enemies`
- [ ] Add **Rigidbody2D** (Kinematic)
- [ ] Add **BoxCollider2D**
- [ ] Add **Enemy** script

**Enemy Script Settings:**
- [ ] Max Health: 30
- [ ] Damage: 5
- [ ] Move Speed: 2
- [ ] Attack Range: 1
- [ ] Gold Reward: 5-10
- [ ] Item Drop Chance: 0.3

**Save as:** `Assets/Prefabs/Enemy.prefab`
- [ ] Assign to GameManager's Enemy Prefabs array

### 2. Boss Prefab
- [ ] Duplicate Enemy prefab
- [ ] Name: `Boss`
- [ ] Scale: 1.5x
- [ ] Stats: HP 200, Damage 15
- [ ] Save as: `Assets/Prefabs/Boss.prefab`
- [ ] Assign to GameManager's Boss Prefab

### 3. GoldPickup Prefab
- [ ] Create Sprite (Circle)
- [ ] Set Tag: `Pickup`, Layer: `Pickups`
- [ ] Add **CircleCollider2D** (Is Trigger: ✓)
- [ ] Add **GoldPickup** script
- [ ] Add SpriteRenderer (Gold coin sprite)
- [ ] Save as: `Assets/Prefabs/GoldPickup.prefab`
- [ ] Assign to Enemy's Gold Pickup Prefab field

### 4. ItemPickup Prefab
- [ ] Create Sprite (Square)
- [ ] Set Tag: `Pickup`, Layer: `Pickups`
- [ ] Add **BoxCollider2D** (Is Trigger: ✓)
- [ ] Add **ItemPickup** script
- [ ] Save as: `Assets/Prefabs/ItemPickup.prefab`
- [ ] Assign to Enemy's Item Drop Prefabs array

### 5. Projectile Prefab
- [ ] Create Sprite (Circle or arrow)
- [ ] Scale: (0.3, 0.3, 0.3)
- [ ] Add **Rigidbody2D** (Dynamic, Gravity Scale: 0)
- [ ] Add **CircleCollider2D** (Is Trigger: ✓, Radius: 0.15)
- [ ] Add **Projectile** script
- [ ] Add SpriteRenderer (Fireball sprite, Sorting Layer: Projectiles)
- [ ] Save as: `Assets/Prefabs/Projectile.prefab`
- [ ] Assign to SkillCaster's Projectile Prefab

---

## PHASE 9: Camera & Scene Setup

### 1. Camera Setup
- [ ] Select Main Camera
- [ ] Position: (0, 0, -10)
- [ ] Add **CameraFollow** script
- [ ] Tag: `MainCamera` (CRITICAL!)

**CameraFollow Settings:**
- [ ] Target: None (will auto-find Player)
- [ ] Smooth Speed: 5
- [ ] Offset: (0, 0, -10)

### 2. Camera Settings
- [ ] Projection: Orthographic
- [ ] Size: 5-7 (adjust to your level)
- [ ] Background: Dark color
- [ ] Clear Flags: Solid Color

### 3. Create Level Boundaries
- [ ] Create 4 empty GameObjects with BoxCollider2D
- [ ] Position as walls around playable area
- [ ] Tag: `Wall` (optional)

---

## PHASE 10: Final Checks

### Before Running Game, Verify:

**Player:**
- [ ] Has Rigidbody2D (Kinematic)
- [ ] Has PlayerController with Input Actions assigned
- [ ] Has SkillCaster with 4 skills assigned
- [ ] SPUM set up correctly (if using)

**Managers:**
- [ ] All managers in scene under "Managers" parent
- [ ] GameManager has spawn points and enemy prefabs
- [ ] UIManager has UI panels assigned
- [ ] InventoryManager exists
- [ ] ShopManager has available items

**Physics:**
- [ ] Layers configured (Enemies, Pickups, Player)
- [ ] Collision matrix set correctly
- [ ] Tags created (Player, Enemy, Pickup)

**UI:**
- [ ] Canvas set up
- [ ] EventSystem with Input System UI module
- [ ] UIManager fields assigned
- [ ] Panels start inactive (hidden)

**Input:**
- [ ] GameControls.inputactions created and saved
- [ ] Assigned to PlayerController
- [ ] Assigned to EventSystem

---

## TESTING ORDER

Once everything is set up, test in this order:

1. [ ] **Movement** - WASD/Arrow keys move player
2. [ ] **Animation** - Player plays idle/move animations (SPUM or regular)
3. [ ] **Attack** - Left click performs melee attack
4. [ ] **Skills** - Keys 1-4 cast skills
5. [ ] **Cooldowns** - Skill UI shows cooldown fill
6. [ ] **Enemies** - Enemies spawn from GameManager
7. [ ] **Combat** - Attacks deal damage, numbers appear
8. [ ] **Drops** - Enemies drop gold/items
9. [ ] **Pickup** - Walk over gold/items to collect
10. [ ] **Inventory** - Press I to open inventory
11. [ ] **Equipment** - Equip items, stats change
12. [ ] **Shop** - Test buy/sell (if implemented)
13. [ ] **Pause** - Escape pauses game

---

## Common Issues & Quick Fixes

| Issue | Quick Fix |
|-------|-----------|
| "No Input Action Asset assigned!" | Assign GameControls to PlayerController |
| "SPUM_Prefabs not found" | Check Use SPUM checkbox, assign spumBridge |
| "No Main Camera found" | Set Camera tag to "MainCamera" |
| Skills not casting | Check SkillCaster has skills array populated |
| Enemies not spawning | Check GameManager has spawn points and prefabs |
| UI not updating | Check UIManager player reference |
| Can't move | Check Input System is installed and enabled |
| Animations not playing | Check SPUMPlayerBridge has spumPrefabs assigned |

---

## Need Help?

See these detailed guides:
- `Assets/SETUP_GUIDE.md` - Complete 1000-line setup guide
- `Assets/SPUM_INTEGRATION_README.md` - SPUM-specific setup
- `Assets/Documentation/GameplayPolishGuide.md` - Advanced features
- `Assets/Documentation/CodeReviewFixes.md` - Recent bug fixes

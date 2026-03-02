# CombatRun - Detailed Setup Guide

Complete step-by-step setup instructions for all prefabs and components.

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Managers Setup](#managers-setup)
3. [Player Setup](#player-setup)
4. [Enemy Setup (SPUM)](#enemy-setup-spum)
5. [UI Setup](#ui-setup)
6. [Prefab Creation](#prefab-creation)
7. [Testing Checklist](#testing-checklist)

---

## Prerequisites

### 1. Install Input System Package
1. **Window > Package Manager**
2. Search "Input System"
3. Click Install
4. Restart Unity when prompted

### 2. Configure Project Settings
**Edit > Project Settings > Player > Other Settings:**
- **Active Input Handling**: `Both` or `Input System Package (New)`

### 3. Setup Layers and Tags
**Tags:** `Player`, `Enemy`, `Pickup`, `Projectile`

**Layers:**
- `Enemies` (Layer 6)
- `Pickups` (Layer 7)
- `Player` (Layer 8)

**Physics 2D Collision Matrix:**
- Enemies ↔ Player: ✓
- Enemies ↔ Enemies: ✗
- Player ↔ Pickups: ✓

---

## Managers Setup

### Step 1: Create Managers GameObject
1. **Right-click Hierarchy** → **Create Empty**
2. Name: `Managers`
3. Position: (0, 0, 0)

### Step 2: Add Components
Add these scripts to the **Managers** GameObject (in order):

| Order | Component | Purpose | DontDestroy |
|-------|-----------|---------|-------------|
| 1 | **Game Manager** | Spawns enemies, manages waves | ✓ |
| 2 | **UIManager** | HUD, panels, notifications | ✓ |
| 3 | **Inventory Manager** | Item storage, equipment | ✓ |
| 4 | **Shop Manager** | Buy/sell items | ✓ |
| 5 | **Set Bonus Manager** | Set bonus tracking | ✓ |
| 6 | **Weapon Mastery Manager** | Weapon progression | ✓ |
| 7 | **Skill Synergy Manager** | Skill combos | ✓ |
| 8 | **Daily Run Manager** | Daily challenges | ✓ |
| 9 | **Gamble System** | Risk/reward mechanics | ✓ |
| 10 | **Damage Number Manager** | Floating damage text | ✓ |

### Step 3: Configure DamageNumberManager
1. Select **Managers** GameObject
2. Find **Damage Number Manager** component
3. **Damage Number Prefab**: [Create this prefab first - see Prefab Creation section]
4. **Pool Size**: 20
5. **Number Lifetime**: 1
6. **Float Speed**: 1

---

## Player Setup

### Step 1: Create or Use SPUM Character
1. Go to `Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/`
2. Choose a character (e.g., `Human/SPUM_Hero.prefab`)
3. **Drag into scene**

### Step 2: Add Required Components
Add to the **Player** GameObject:

| Component | Settings |
|-----------|----------|
| **Rigidbody 2D** | Body Type: `Kinematic`, Gravity Scale: `0` |
| **Circle Collider 2D** | Adjust radius to fit character |
| **Player Controller** | See Step 3 |
| **Skill Caster** | See Step 4 |
| **Combo System** | Leave default |
| **SPUM Player Bridge** | See Step 5 |
| **SPUM Equipment Manager** | Leave default (auto-finds) |

### Step 3: Configure PlayerController
```
PlayerController
├── Input
│   └── Input Actions: [Drag GameControls.inputactions]
├── Movement
│   └── Move Speed: 5
├── Combat
│   ├── Melee Range: 1.5
│   ├── Melee Cooldown: 0.5
│   └── Enemy Layer: Enemies
├── Components
│   └── Attack Point: [Drag AttackPoint child]
├── Damage Flash
│   ├── Use VFX Damage Flash: ☑️
│   └── Damage Flash Duration: 0.1
└── SPUM Integration
    ├── Use SPUM: ☑️ CHECK!
    ├── Spum Bridge: [Drag SPUMPlayerBridge]
    └── Spum Equipment: [Drag SPUMEquipmentManager]
```

### Step 4: Configure SkillCaster
```
SkillCaster
├── Skills
│   └── Skills: [Size 4, assign SkillSOs if available]
├── References
│   ├── Cast Point: [Drag CastPoint child or leave empty]
│   └── Enemy Layer: Enemies
└── Prefabs
    ├── Projectile Prefab: [Create/assign later]
    └── Shield Effect Prefab: [Optional]
```

### Step 5: Configure SPUMPlayerBridge
```
SPUMPlayerBridge
├── SPUM Components
│   ├── Spum Prefabs: [Drag from SPUM character child]
│   └── Spum Animator: [Should auto-assign]
└── Animation State Indices
    ├── Idle Animation Index: 0
    ├── Move Animation Index: 0
    ├── Attack Animation Index: 0
    ├── Damaged Animation Index: 0
    ├── Debuff Animation Index: 0
    ├── Death Animation Index: 0
    └── Skill Animation Indices: [1, 1, 1, 1]
```

**How to find Spum Prefabs:**
1. Expand your SPUM character GameObject
2. Look for child object with `SPUM_Prefabs` component
3. Drag it to the field

### Step 6: Create Child Objects
Right-click Player → **Create Empty**:

```
Player
├── AttackPoint (Empty)
│   └── Position: At weapon hand
└── CastPoint (Empty)
    └── Position: Where projectiles spawn
```

Assign these to PlayerController and SkillCaster.

### Step 7: Tag and Layer
- **Tag**: `Player`
- **Layer**: `Player`

---

## Enemy Setup (SPUM)

### Step 1: Create or Use SPUM Character for Enemy
1. Go to `Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/`
2. Choose a monster character
3. **Drag into scene**

### Step 2: Add Components
| Component | Settings |
|-----------|----------|
| **Rigidbody 2D** | Body Type: `Dynamic`, Gravity Scale: `0` |
| **Box Collider 2D** | Adjust to fit |
| **Enemy** | See Step 3 |

### Step 3: Configure Enemy Script
```
Enemy
├── Stats
│   ├── Max Health: 30
│   ├── Damage: 5
│   ├── Move Speed: 2
│   ├── Attack Range: 1
│   ├── Attack Cooldown: 1
│   ├── Gold Reward: 5
│   └── Item Drop Chance: 0.3
├── AI
│   ├── Detection Range: 8
│   ├── Stop Distance: 0.5
│   └── Patrol: ☐ (check if patrolling)
├── Components - Legacy
│   ├── Animator: [Leave empty for SPUM]
│   └── Sprite Renderer: [Leave empty for SPUM]
├── SPUM Integration
│   ├── Use SPUM: ☑️ CHECK!
│   ├── Spum Prefabs: [Drag from child]
│   ├── Idle Animation Index: 0
│   ├── Move Animation Index: 0
│   ├── Attack Animation Index: 0
│   ├── Hit Animation Index: 0
│   └── Death Animation Index: 0
└── Drops
    ├── Gold Pickup Prefab: [Create/assign]
    └── Item Drop Prefabs: [Create/assign array]
```

### Step 4: Tag and Layer
- **Tag**: `Enemy`
- **Layer**: `Enemies`

### Step 5: Make Prefab
1. Drag enemy from scene to `Assets/Prefabs/`
2. Name: `Enemy_Basic`
3. Delete from scene
4. Assign to **GameManager** → **Enemy Prefabs**

---

## UI Setup

### Step 1: Create Canvas
1. **Right-click Hierarchy** → **UI** → **Canvas**
2. **Render Mode**: Screen Space - Overlay
3. **Canvas Scaler** → **UI Scale Mode**: Scale With Screen Size
4. **Reference Resolution**: 1920 x 1080

### Step 2: Create EventSystem
1. **Right-click Hierarchy** → **UI** → **Event System**
2. Add **Input System UI Input Module** component
3. Remove **Standalone Input Module** (if present)

### Step 3: Create HUD Panel
1. **Right-click Canvas** → **UI** → **Panel**
2. Name: `HUDPanel`
3. **Anchor Preset**: Top Stretch (Alt+Click)
4. **Height**: 100
5. Add children:

**Health Bar:**
- Right-click HUDPanel → **UI** → **Slider**
- Name: `HealthSlider`
- Position: Left side (x: -400)

**Gold Text:**
- Right-click HUDPanel → **UI** → **Text - TextMeshPro**
- Name: `GoldText`
- Text: "0 💰"
- Position: Right side (x: 400)

**Skill Icons (4):**
- Right-click HUDPanel → **UI** → **Image** (x4)
- Names: `Skill1`, `Skill2`, `Skill3`, `Skill4`
- Position: Bottom center (y: -200)
- Add cooldown overlay (black semi-transparent image) as child of each

### Step 4: Create Inventory Panel
1. **Right-click Canvas** → **UI** → **Panel**
2. Name: `InventoryPanel`
3. Add **Canvas Group** component
4. **Disable** (uncheck at top)
5. Add children:
   - Title: TextMeshPro "INVENTORY"
   - Grid: Empty with **Grid Layout Group**
   - Close Button: UI Button with "X"

### Step 5: Create Shop Panel
Same as Inventory but name it `ShopPanel`

### Step 6: Create Pause Panel
1. **Right-click Canvas** → **UI** → **Panel**
2. Name: `PausePanel`
3. Add **Canvas Group**
4. **Disable**
5. Add children:
   - Title: "PAUSED"
   - Resume Button
   - Quit Button

### Step 7: Configure UIManager
Select **Managers** GameObject, find **UIManager**:

```
UIManager
├── HUD
│   ├── Hud Panel: [Drag HUDPanel]
│   ├── Health Slider: [Drag HealthSlider]
│   ├── Gold Text: [Drag GoldText]
│   ├── Skill Icons: [Add 4, drag Skill1-4]
│   └── Skill Cooldown Overlays: [Add 4, drag overlays]
├── Panels
│   ├── Inventory Panel: [Drag InventoryPanel]
│   ├── Shop Panel: [Drag ShopPanel]
│   └── Pause Panel: [Drag PausePanel]
├── Notifications
│   ├── Notification Prefab: [Create prefab - see below]
│   └── Notification Parent: [Drag Canvas]
└── References
    ├── Player: [Drag Player]
    └── Skill Caster: [Leave empty or drag Player]
```

---

## Prefab Creation

### 1. DamageNumber Prefab
1. **Right-click Canvas** → **UI** → **Text - TextMeshPro**
2. Name: `DamageNumber`
3. Configure:
   - Text: "999"
   - Font Size: 36
   - Alignment: Center
   - Color: White
4. Add **Outline** component:
   - Effect Color: Black
   - Effect Distance: (0.5, 0.5)
5. Drag to `Assets/Prefabs/`
6. Delete from scene
7. Assign to **DamageNumberManager**

### 2. GoldPickup Prefab
1. **Right-click Hierarchy** → **2D Object** → **Sprites** → **Circle**
2. Name: `GoldPickup`
3. Sprite color: Yellow
4. Add **Circle Collider 2D**:
   - Is Trigger: ☑️
5. Add **GoldPickup** script
6. Tag: `Pickup`
7. Layer: `Pickups`
8. Drag to `Assets/Prefabs/`
9. Assign to **Enemy** → **Gold Pickup Prefab**

### 3. ItemPickup Prefab
1. **Right-click Hierarchy** → **2D Object** → **Sprites** → **Square**
2. Name: `ItemPickup`
3. Add **Circle Collider 2D** (trigger)
4. Add **ItemPickup** script
5. Tag: `Pickup`
6. Layer: `Pickups`
7. Drag to `Assets/Prefabs/`
8. Assign to **Enemy** → **Item Drop Prefabs** array

### 4. Projectile Prefab
1. **Right-click Hierarchy** → **2D Object** → **Sprites** → **Circle**
2. Name: `Projectile`
3. Scale: (0.3, 0.3, 1)
4. Color: Orange/Red
5. Add **Rigidbody 2D**:
   - Body Type: `Kinematic`
   - Gravity Scale: 0
6. Add **Circle Collider 2D**:
   - Is Trigger: ☑️
7. Add **Projectile** script
8. Tag: `Projectile`
9. Drag to `Assets/Prefabs/`
10. Assign to **SkillCaster** → **Projectile Prefab**

### 5. Notification Prefab
1. **Right-click Canvas** → **UI** → **Text - TextMeshPro**
2. Name: `NotificationText`
3. Configure:
   - Font Size: 24
   - Color: White
   - Alignment: Center
4. Add **Canvas Group** component
5. Drag to `Assets/Prefabs/`
6. Delete from scene
7. Assign to **UIManager**

---

## Testing Checklist

### Basic Setup
- [ ] Input System Package installed
- [ ] Active Input Handling set to "Both"
- [ ] All Managers added to Managers GameObject
- [ ] Player has all required components
- [ ] Player Input Actions assigned
- [ ] SPUMPlayerBridge has SpumPrefabs assigned

### Player Tests
- [ ] WASD moves player
- [ ] Player faces movement direction
- [ ] Space/Click attacks
- [ ] Attack animation plays
- [ ] Skills 1-4 cast
- [ ] SPUM walk animation plays

### Enemy Tests
- [ ] Enemy chases player
- [ ] Enemy attacks in range
- [ ] Enemy SPUM animation works
- [ ] Enemy takes damage
- [ ] Damage numbers appear
- [ ] Enemy dies and drops gold

### UI Tests
- [ ] I key opens Inventory
- [ ] Escape opens Pause
- [ ] HUD shows health
- [ ] HUD shows gold
- [ ] Skill cooldowns visible

---

## Troubleshooting

### "WeaponMasteryManager not found"
→ Use the new file at `Assets/Scripts/Managers/WeaponMasteryManager.cs`

### "SPUM_Prefabs not found"
→ Expand your SPUM character, find child with SPUM_Prefabs component, drag to field

### "DamageNumberManager: No prefab assigned"
→ Create DamageNumber prefab and assign it

### "Input Actions not assigned"
→ Select Player, drag GameControls.inputactions to field

### Enemy not moving
→ Check Enemy has Rigidbody2D (Dynamic, Gravity Scale 0)
→ Check Enemy Layer is "Enemies"

---

*Last Updated: With SPUM Enemy Support*

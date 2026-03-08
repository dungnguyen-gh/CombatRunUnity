# Skill System Setup Guide

A comprehensive guide for setting up and configuring the ARPG Skill System in CombatRun.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Step-by-Step Setup](#step-by-step-setup)
4. [Skill Types Reference](#skill-types-reference)
5. [Input System Integration](#input-system-integration)
6. [Known Issues & Workarounds](#known-issues--workarounds)
7. [Testing Checklist](#testing-checklist)
8. [Quick Reference Table](#quick-reference-table)

---

## Overview

The Skill System is a flexible, data-driven framework for creating and executing player abilities in a 2D ARPG environment. It supports 21 distinct skill types with customizable targeting, effects, cooldowns, and synergies.

### Key Features

| Feature | Description |
|---------|-------------|
| **4 Skill Slots** | Players have 4 skill slots bound to keys 1-4 |
| **21 Skill Types** | From basic attacks to advanced summons and utility |
| **Cooldown System** | Per-skill cooldowns with visual UI feedback |
| **Synergy Combos** | Special effects triggered by casting skills in sequence |
| **Status Effects** | Burn, Freeze, Poison, Shock application |
| **Screen Effects** | Camera shake and slow-motion support |
| **Data-Driven** | All skills defined via ScriptableObjects |
| **SPUM Compatible** | Works with SPUM animation system |

### Core Components

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   SkillCaster   │────▶│     SkillSO     │◄────│  SkillBarUI     │
│  (Player comp)  │     │  (Data Asset)   │     │   (UI display)  │
└────────┬────────┘     └─────────────────┘     └─────────────────┘
         │                                            │
         ▼                                            ▼
┌─────────────────┐                         ┌─────────────────┐
│   Projectile    │                         │  SkillTooltip   │
│   (Spawned)     │                         │  (Hover info)   │
└─────────────────┘                         └─────────────────┘
         │
         ▼
┌─────────────────┐
│SkillSynergyMgr  │
│  (Combo system) │
└─────────────────┘
```

---

## Architecture

### Data Flow Diagram

```
Input Key (1-4) Pressed
         │
         ▼
┌─────────────────┐
│ PlayerController│
│  (Input System) │
└────────┬────────┘
         │ TryCastSkill(index)
         ▼
┌─────────────────┐     Validation:
│   SkillCaster   │────▶ Skill assigned?
│                 │────▶ Cooldown ready?
│                 │────▶ Has resources?
└────────┬────────┘
         │ ExecuteSkill()
         ▼
┌─────────────────┐
│    SkillSO      │────▶ SkillType determines execution
│  (Skill Data)   │────▶ Damage/range/duration values
└────────┬────────┘
         │
    ┌────┴────┬────────┬────────┬────────┐
    ▼         ▼        ▼        ▼        ▼
 Projectile CircleAOE Dash     Buff    Heal
    │         │        │        │        │
    ▼         ▼        ▼        ▼        ▼
 Spawn    Damage   Move     Modify   Heal HP
 Prefab   Enemies  Player   Stats
```

### Component Relationships

| Component | Responsibility | Dependencies |
|-----------|---------------|--------------|
| `SkillCaster` | Executes skills, manages cooldowns | PlayerController, SkillSO[] |
| `SkillSO` | Defines skill data/properties | None (ScriptableObject) |
| `SkillBarUI` | Displays skill slots/cooldowns | SkillCaster, UI Canvas |
| `SkillTooltip` | Shows skill info on hover | SkillSO data |
| `SkillSynergyManager` | Tracks combos, applies synergy effects | SkillCaster |
| `Projectile` | Handles projectile movement/collision | SkillSO reference |

---

## Step-by-Step Setup

### Prerequisites

- Unity 2022.3+ with 2D packages
- New Input System package installed
- TextMeshPro package
- Player GameObject with PlayerController component

---

### Step 1: Setting up SkillCaster on Player

The `SkillCaster` component should be added to your Player GameObject.

#### 1.1 Add SkillCaster Component

```csharp
// SkillCaster is auto-added via [RequireComponent(typeof(PlayerController))]
// Or manually: Add Component > Scripts > SkillCaster
```

#### 1.2 Configure Inspector Settings

```
Player (GameObject)
├── PlayerController
├── SkillCaster                    ← ADD THIS
│   ├── Skills (Size: 4)          ← Assign 4 SkillSO assets
│   │   ├── Element 0: Fireball   ← Key 1
│   │   ├── Element 1: Meteor     ← Key 2
│   │   ├── Element 2: Shield     ← Key 3
│   │   └── Element 3: Heal       ← Key 4
│   │
│   ├── Cast Point: Player        ← Where projectiles spawn
│   ├── Enemy Layer: Enemy        ← LayerMask for enemies
│   ├── Obstacle Layer: Default   ← For dash collision
│   │
│   ├── Default Projectile: [Prefab]
│   └── Default Shield Effect: [Prefab]
```

#### Required References

| Field | Description | Required |
|-------|-------------|----------|
| `castPoint` | Transform where projectiles spawn | Recommended (defaults to player) |
| `enemyLayer` | LayerMask for enemy detection | **YES** |
| `obstacleLayer` | LayerMask for wall collision | For Dash skills |
| `defaultProjectilePrefab` | Fallback projectile | For Projectile skills without prefab |
| `defaultShieldEffectPrefab` | Fallback shield visual | For Shield/Reflect skills |

---

### Step 2: Creating SkillSO Assets

#### 2.1 Create Skill Asset

```
Project Window:
└── Right-click > Create > ARPG > Skill
    └── Rename to "Skill_Fireball"
```

#### 2.2 Configure Basic Properties

```yaml
# Basic Info
skillId: "fireball_01"
skillName: "Fireball"
description: "Launch a fiery projectile that explodes on impact"
icon: [Sprite: fireball_icon]
skillSlot: 0          # 0-3 corresponds to keys 1-4
rarity: Rare

# Targeting & Casting
skillType: Projectile
targeting: MousePosition
castTime: 0           # 0 = instant, >0 = charging
canMoveWhileCasting: true
requiresLineOfSight: false

# Cooldown & Cost
cooldownTime: 3.0
manaCost: 20
healthCost: 0

# Damage & Effects
damageMultiplier: 1.5
flatDamageBonus: 10
critChanceBonus: 0.1
critDamageMultiplier: 1.5

# Area & Range
range: 10
radius: 2
coneAngle: 360

# Duration & Ticks
duration: 0
tickRate: 1
maxStacks: 1

# Visual & Audio
castEffectPrefab: [Prefab: fire_cast_vfx]
effectPrefab: [Prefab: explosion_vfx]
projectilePrefab: [Prefab: fireball_projectile]
persistentEffectPrefab: null
castSound: [AudioClip]
impactSound: [AudioClip]
```

#### 2.3 Assign to SkillCaster

Drag the created SkillSO into the corresponding slot in SkillCaster's Skills array:
- Slot 0 → Key 1
- Slot 1 → Key 2
- Slot 2 → Key 3
- Slot 3 → Key 4

---

### Step 3: Setting up UI

#### 3.1 Create Skill Bar UI

```
Hierarchy:
Canvas (Screen Space - Overlay)
└── HUD
    └── SkillBarUI (GameObject)       ← Add SkillBarUI component
        ├── SkillSlotContainer: SkillSlots
        └── SkillSlotPrefab: [Prefab]
```

#### 3.2 Skill Slot Prefab Structure

```
SkillSlot (Prefab)
├── Background (Image)              # Dark background
├── Icon (Image)                    # Skill icon display
├── Cooldown (Image - Filled)       # Radial cooldown overlay
│   ├── Type: Filled
│   ├── Fill Method: Radial 360
│   └── Fill Clockwise: false
├── Border (Image)                  # Rarity-colored border
├── Key (TextMeshProUGUI)           # Key binding (1, 2, 3, 4)
└── CooldownText (TextMeshProUGUI)  # Remaining cooldown
```

#### 3.3 Auto-Discovery Setup (Recommended)

Enable these options on SkillBarUI to auto-find components:

```csharp
// SkillBarUI Inspector Settings
autoDiscoverCaster: true      // Finds SkillCaster automatically
autoDiscoverContainer: true   // Finds container by name
containerName: "SkillSlots"   // Name to search for
```

#### 3.4 SkillTooltip Setup

```
Canvas
└── SkillTooltip (GameObject)       ← Add SkillTooltip component
    ├── TooltipPanel (RectTransform)
    ├── NameText (TextMeshProUGUI)
    ├── DescriptionText (TextMeshProUGUI)
    ├── StatsText (TextMeshProUGUI)
    ├── SkillIcon (Image)
    └── RarityBorder (Image)
```

Add `SkillTooltipTrigger` to each skill slot:
```csharp
// On SkillSlot GameObject
SkillTooltipTrigger tooltipTrigger;
tooltipTrigger.tooltip = [Reference to SkillTooltip];
```

---

### Step 4: Setting up SkillSynergyManager

#### 4.1 Create Synergy Manager

```
Hierarchy:
└── SkillSynergyManager (GameObject)    ← Add SkillSynergyManager
    └── Tag: "GameController" (optional)
```

#### 4.2 Default Synergies (Auto-Configured)

The manager automatically creates these synergies on Start:

| Synergy Name | Sequence | Effect | Duration |
|--------------|----------|--------|----------|
| **Inferno** | 1 → 2 | Damage Boost (1.5x) | 5s |
| **Shattered Earth** | 2 → 3 | Empower Next Skill (2x) | 3s |
| **Reflecting Shield** | 3 → 4 | Damage Reduction (50%) | 4s |
| **Elemental Overload** | 1 → 2 → 3 | Chain Lightning | 6s |
| **Avatar of Power** | 1 → 2 → 3 → 4 | Infinite Mana (No cooldowns) | 3s |

#### 4.3 Custom Synergy Setup

```csharp
// Add custom synergies in Inspector
synergies:
  - synergyName: "My Combo"
    description: "Custom combo effect"
    requiredSkillSequence: [0, 2, 1]  // Skills in order
    timeWindow: 4.0                    // Seconds to complete
    effect: DamageBoost
    effectDuration: 5.0
    damageMultiplier: 2.0
```

---

### Step 5: Creating Projectile Prefabs

#### 5.1 Projectile Prefab Structure

```
Projectile (Prefab)
├── SpriteRenderer                  # Visual sprite
├── CircleCollider2D (Trigger)      # Hit detection
├── Rigidbody2D (Kinematic)         # Physics
└── Projectile (Script)             # Logic component
```

#### 5.2 Projectile Component Settings

```csharp
// Projectile.cs Inspector
speed: 10f                // Movement speed
maxLifetime: 5f           // Auto-destroy after time
spriteRenderer: [Auto]    // Visual reference
trailParticles: [Prefab]  // Optional trail effect
```

#### 5.3 Runtime Properties

These are set by SkillCaster when spawning:

```csharp
// Set via Initialize()
direction: Vector2        // Movement direction
range: float              // Max travel distance
damage: int               // Damage to deal
hitLayers: LayerMask      // What to hit
sourceSkill: SkillSO      // Reference for effects

// Optional flags
pierce: bool              // Go through enemies
homing: bool              // Track targets
homingStrength: float     // Tracking aggressiveness
explodeOnImpact: bool     // Explosion on hit
explosionRadius: float    // Explosion size
```

---

## Skill Types Reference

### **IMPLEMENTED (14 Types)**

---

#### 1. CircleAOE
Area attack centered on player.

```yaml
skillType: CircleAOE
targeting: Self
radius: 3              # Attack radius
damageMultiplier: 1.2

# Visual
effectPrefab: [Shockwave effect]
castEffectPrefab: [Charge effect]

# Optional
applyBurn: true        # Apply burn status
statusDuration: 3
```

**Setup Requirements:**
- Assign `effectPrefab` for impact visual
- Set `radius` for damage area
- Ensure enemies have "Enemy" tag

---

#### 2. GroundAOE
Targeted area attack with delay.

```yaml
skillType: GroundAOE
targeting: MousePosition
radius: 2              # Impact radius
range: 10              # Max targeting distance
damageMultiplier: 1.5

# Advanced
explodeOnImpact: true
explosionRadius: 4

# Visual
castEffectPrefab: [Targeting indicator]
effectPrefab: [Explosion effect]
```

**Setup Requirements:**
- Requires Main Camera for mouse targeting
- `castEffectPrefab` shows targeting preview
- 0.4s delay before impact

---

#### 3. Projectile
Launches a projectile in aim direction.

```yaml
skillType: Projectile
targeting: MousePosition  # or Directional
range: 15
damageMultiplier: 1.0

# Projectile Settings
projectilePrefab: [REQUIRED]
pierceEnemies: false
homing: false
homingStrength: 5
explodeOnImpact: true
explosionRadius: 3

# Visual
castEffectPrefab: [Muzzle flash]
effectPrefab: [Impact effect]
```

**Setup Requirements:**
- **MUST** assign `projectilePrefab` in SkillSO OR `defaultProjectilePrefab` on SkillCaster
- Projectile prefab needs `Projectile.cs` component
- Configure `enemyLayer` on SkillCaster

---

#### 4. Melee
Single-target melee attack.

```yaml
skillType: Melee
targeting: Directional
range: 2               # Attack reach
damageMultiplier: 1.0

# Visual
effectPrefab: [Slash effect]
castEffectPrefab: [Swing effect]

# Status
applyFreeze: true
statusDuration: 2
```

**Setup Requirements:**
- Uses raycast in facing direction
- `range` determines attack reach
- Spawns effect at hit point or max range

---

#### 5. Shield
Damage reduction buff.

```yaml
skillType: Shield
targeting: Self
duration: 5            # Shield duration

# Visual
persistentEffectPrefab: [Shield bubble effect]

# Animation
animationTrigger: "Skill"
```

**Setup Requirements:**
- Calls `player.SetShieldActive(true/false)`
- Visual effect follows player (parented)
- `defaultShieldEffectPrefab` used as fallback

---

#### 6. Dash
Quick movement in facing direction.

```yaml
skillType: Dash
targeting: Directional
dashDistance: 5        # Distance to travel
dashSpeed: 20          # Movement speed
dashInvulnerable: true # i-frames during dash
leaveTrail: true       # Leave trail effect

# Visual
effectPrefab: [Trail/Wisp effect]
```

**Setup Requirements:**
- Set `obstacleLayer` for wall collision
- Stops before walls (raycast check)
- Uses `player.GetFacingDirection()` for direction

---

#### 7. Summon
Spawns allied units.

```yaml
skillType: Summon
summonPrefab: [REQUIRED Minion prefab]
summonCount: 3         # Number to spawn
summonDuration: 10     # Lifetime in seconds
summonFollowPlayer: true

# Visual
castEffectPrefab: [Summon portal effect]
```

**Setup Requirements:**
- **MUST** assign `summonPrefab` in SkillSO
- Minions spawn in random circle around player
- Auto-destroyed after `summonDuration`

---

#### 8. Buff
Stat enhancement.

```yaml
skillType: Buff
targeting: Self
duration: 10           # Buff duration
damageMultiplier: 1.5  # Damage bonus (applied as modifier)
critChanceBonus: 0.15  # +15% crit chance

# Visual
persistentEffectPrefab: [Buff aura effect]
```

**Setup Requirements:**
- Modifies `player.stats.damageMod` and `critMod`
- Stats reverted when duration ends
- Visual effect follows player

---

#### 9. Heal
Restores health.

```yaml
skillType: Heal
targeting: Self
damageMultiplier: 2.0  # Heal = baseHeal * multiplier

# Visual
effectPrefab: [Heal effect]
```

**Setup Requirements:**
- Heal amount = (MaxHP / 10) * damageMultiplier
- Calls `player.Heal(amount)`

---

#### 10. Chain
Bouncing lightning between enemies.

```yaml
skillType: Chain
targeting: Self        # Auto-targets nearest enemy
range: 8               # Initial search range
chainBounces: 4        # Number of bounces
chainRange: 6          # Bounce range
chainDamageFalloff: 0.8 # 80% damage per bounce

# Visual
effectPrefab: [Lightning effect]
```

**Setup Requirements:**
- Requires enemies on `enemyLayer` within `range`
- Visualized via Debug.DrawLine (yellow)
- 0.1s delay between bounces

---

#### 11. Beam
Continuous channeled attack.

```yaml
skillType: Beam
targeting: Directional
castTime: 0            # Instant channel start
range: 12
damageMultiplier: 0.5  # Damage per tick
tickRate: 0.5          # Damage interval

# Visual
effectPrefab: [Beam effect]
loopSound: [AudioClip]
```

**Setup Requirements:**
- Channeling lasts while cooldown is active
- Uses raycast from castPoint
- ApplyChannelingEffect called every tickRate seconds

---

#### 12. Trap
Deployable hazard at mouse position.

```yaml
skillType: Trap
targeting: MousePosition
duration: 30           # Trap lifetime

# Visual
effectPrefab: [Trap prefab]
castEffectPrefab: [Placement effect]
```

**Setup Requirements:**
- Trap spawns at mouse position
- If no valid mouse pos, spawns 2 units in front of player
- Auto-destroyed after `duration`

---

#### 13. Teleport
Instant position change.

```yaml
skillType: Teleport
targeting: MousePosition
requiresLineOfSight: true  # Check for obstacles

# Visual
castEffectPrefab: [Teleport start effect]
effectPrefab: [Teleport end effect]
```

**Setup Requirements:**
- Requires Main Camera for mouse targeting
- Checks line of sight if `requiresLineOfSight` is true
- Shows notification if blocked

---

#### 14. Reflect
**PARTIALLY IMPLEMENTED** - Functions as Shield

```yaml
skillType: Reflect
targeting: Self
duration: 5

# Visual
persistentEffectPrefab: [Reflect barrier effect]
```

**Note:** Currently redirects to Shield implementation. Reflect-specific projectile reflection logic needs custom implementation.

---

### **NOT IMPLEMENTED (7 Types)**

| Type | Description | Workaround |
|------|-------------|------------|
| **Blink** | Short-range teleport | Use Teleport with shorter range |
| **Turret** | Stationary defensive unit | Use Summon with stationary AI |
| **Totem** | Buff/debuff area | Use Summon with aura effect |
| **Channel** | Hold-to-charge skill | Use castTime > 0 on other types |
| **AreaDenial** | Persistent damaging zone | Use Trap with damage over time |
| **Transform** | Change form/abilities | Manual implementation required |
| **TimeWarp** | Slow enemies/speed self | Manual implementation required |

---

## Input System Integration

### New Input System Setup

#### 1. Create Input Actions Asset

```
Project Window:
└── Create > Input Actions
    └── Name: "GameControls"
```

#### 2. Configure Skill Actions

```yaml
# GameControls Input Map
Action Map: "Player"

Actions:
  - Name: "Skill1"
    Action Type: Button
    Binding: <Keyboard>/1
    
  - Name: "Skill2"
    Action Type: Button
    Binding: <Keyboard>/2
    
  - Name: "Skill3"
    Action Type: Button
    Binding: <Keyboard>/3
    
  - Name: "Skill4"
    Action Type: Button
    Binding: <Keyboard>/4
```

#### 3. Bind in PlayerController

```csharp
// In PlayerController.SetupInputActions()
inputActions["Skill1"].performed += _ => skillCaster?.TryCastSkill(0);
inputActions["Skill2"].performed += _ => skillCaster?.TryCastSkill(1);
inputActions["Skill3"].performed += _ => skillCaster?.TryCastSkill(2);
inputActions["Skill4"].performed += _ => skillCaster?.TryCastSkill(3);

// For channeled skills (Beam)
inputActions["Skill4"].canceled += _ => skillCaster?.TryReleaseSkill(3);
```

#### 4. Generate C# Class (Optional)

```
Input Actions Asset:
└── Generate C# Class (checked)
    └── Apply
```

---

## Known Issues & Workarounds

### 1. Missing Skill Type Implementations

**Issue:** 7 skill types from the enum are not implemented in SkillCaster.ExecuteSkill().

**Affected Types:**
- `Blink` - Redirects to nothing
- `Turret` - No implementation
- `Totem` - No implementation
- `Channel` - No implementation
- `AreaDenial` - No implementation
- `Transform` - No implementation
- `TimeWarp` - No implementation

**Workaround:** Use functionally similar implemented types:
- Use `Teleport` instead of `Blink`
- Use `Summon` instead of `Turret`/`Totem`
- Use `Beam` with castTime for channeling effect
- Use `GroundAOE` with long duration for area denial

---

### 2. Reflect Skill Not Implemented

**Issue:** Reflect skill calls `CastShield()` instead of having unique reflection logic.

**Current Behavior:**
```csharp
bool CastReflect(SkillSO skill, int index) {
    return CastShield(skill, index); // Just a shield
}
```

**Workaround:** Extend CastReflect to detect and reflect projectiles:
```csharp
// Custom implementation needed:
// 1. Spawn reflect barrier
// 2. On projectile hit, reverse direction
// 3. Set projectile to hit enemies
```

---

### 3. Resource System Not Implemented

**Issue:** Mana/Health costs are defined in SkillSO but not enforced.

**Current Behavior:**
```csharp
bool HasResources(SkillSO skill) {
    return true; // Always returns true
}

void ConsumeResources(SkillSO skill) {
    // Does nothing
}
```

**Workaround:** Implement custom resource system:
```csharp
// In SkillCaster.cs
bool HasResources(SkillSO skill) {
    if (player.stats == null) return true;
    return player.stats.MP >= skill.manaCost && 
           player.stats.HP > skill.healthCost;
}

void ConsumeResources(SkillSO skill) {
    player.stats.MP -= skill.manaCost;
    player.stats.HP -= skill.healthCost;
}
```

---

### 4. SPUM Animation Dependency

**Issue:** Some visual effects may depend on SPUM animation system.

**Workaround:**
```csharp
// In PlayerController
debugMode: true  // Set to true to disable SPUM requirement
```

Skills will work normally without SPUM animations.

---

### 5. Missing Projectile Component

**Issue:** Projectile prefabs without Projectile.cs component won't function correctly.

**Symptom:** "Projectile prefab has no Projectile component!" warning

**Fix:** Ensure all projectile prefabs have:
```csharp
// Required components on projectile prefab:
- SpriteRenderer (or visual)
- Collider2D (Trigger)
- Rigidbody2D (Kinematic)
- Projectile.cs (Script)
```

---

### 6. Enemy Layer Not Set

**Issue:** Skills won't hit enemies if enemyLayer is not configured.

**Symptom:** Skills cast but nothing happens, "enemyLayer is not set!" warning

**Fix:**
```
SkillCaster Inspector:
└── Enemy Layer: Select "Enemy" layer

Enemy GameObjects:
└── Layer: Enemy
```

---

### 7. Camera Required for Mouse Skills

**Issue:** GroundAOE, Teleport, and Trap require Main Camera.

**Symptom:** "No MainCamera found!" error, skills fail silently

**Fix:** Ensure scene has camera with "MainCamera" tag:
```
Camera GameObject:
└── Tag: MainCamera
```

---

## Testing Checklist

### Basic Functionality

- [ ] Player has SkillCaster component
- [ ] 4 SkillSO assets created and assigned
- [ ] Each skill slot shows correct icon
- [ ] Input keys 1-4 trigger skills
- [ ] Cooldown overlay appears after casting
- [ ] Cooldown text counts down correctly
- [ ] Skills cannot be cast while on cooldown

### Skill Type Testing

- [ ] **CircleAOE:** Hits enemies within radius
- [ ] **GroundAOE:** Targets at mouse position
- [ ] **Projectile:** Spawns and moves correctly
- [ ] **Melee:** Hits enemy in facing direction
- [ ] **Shield:** Reduces damage for duration
- [ ] **Dash:** Moves player, stops at walls
- [ ] **Summon:** Spawns minions that persist
- [ ] **Buff:** Increases stats for duration
- [ ] **Heal:** Restores HP
- [ ] **Chain:** Bounces between enemies
- [ ] **Beam:** Continuous damage while held
- [ ] **Trap:** Places at mouse position
- [ ] **Teleport:** Moves to mouse position

### Projectile Features

- [ ] Pierce: Goes through multiple enemies
- [ ] Homing: Tracks nearest enemy
- [ ] Explosion: AOE damage on impact
- [ ] Max range: Destroys after distance

### Status Effects

- [ ] Burn: Damage over time
- [ ] Freeze: Movement slow
- [ ] Poison: Damage over time
- [ ] Shock: Stun effect

### Synergy System

- [ ] SkillSynergyManager exists in scene
- [ ] Casting 1→2 triggers Inferno synergy
- [ ] Synergy notification appears
- [ ] Damage boost applies correctly
- [ ] Synergy expires after duration

### UI Testing

- [ ] Skill icons display correctly
- [ ] Empty slots show grayed out
- [ ] Hover shows tooltip with stats
- [ ] Cooldown fill is smooth
- [ ] Rarity colors on borders
- [ ] Error flash when cast fails

### Edge Cases

- [ ] Casting while moving (if canMoveWhileCasting=false)
- [ ] Casting with no enemies in range
- [ ] Casting at max range
- [ ] Rapid-fire casting
- [ ] Casting while another skill is channeling

---

## Quick Reference Table

### All 21 Skill Types

| # | Type | Status | Targeting | Key Fields | Prefab Required |
|---|------|--------|-----------|------------|-----------------|
| 1 | **CircleAOE** | ✅ | Self | `radius` | effectPrefab |
| 2 | **GroundAOE** | ✅ | MousePosition | `radius`, `explosionRadius` | castEffectPrefab |
| 3 | **Projectile** | ✅ | Mouse/Directional | `range`, `pierceEnemies`, `homing` | **projectilePrefab** |
| 4 | **Melee** | ✅ | Directional | `range` | effectPrefab |
| 5 | **Shield** | ✅ | Self | `duration` | persistentEffectPrefab |
| 6 | **Dash** | ✅ | Directional | `dashDistance`, `dashSpeed` | effectPrefab |
| 7 | **Summon** | ✅ | Self | `summonCount`, `summonDuration` | **summonPrefab** |
| 8 | **Buff** | ✅ | Self | `duration`, `damageMultiplier` | persistentEffectPrefab |
| 9 | **Heal** | ✅ | Self | `damageMultiplier` | effectPrefab |
| 10 | **Chain** | ✅ | Auto | `chainBounces`, `chainRange` | effectPrefab |
| 11 | **Beam** | ✅ | Directional | `tickRate`, `range` | effectPrefab |
| 12 | **Trap** | ✅ | MousePosition | `duration` | effectPrefab |
| 13 | **Teleport** | ✅ | MousePosition | `requiresLineOfSight` | cast+effect Prefab |
| 14 | **Reflect** | ⚠️ Partial | Self | `duration` | persistentEffectPrefab |
| 15 | Blink | ❌ | - | - | - |
| 16 | Turret | ❌ | - | - | - |
| 17 | Totem | ❌ | - | - | - |
| 18 | Channel | ❌ | - | - | - |
| 19 | AreaDenial | ❌ | - | - | - |
| 20 | Transform | ❌ | - | - | - |
| 21 | TimeWarp | ❌ | - | - | - |

### Legend
- ✅ Fully Implemented
- ⚠️ Partially Implemented
- ❌ Not Implemented

### Common Field Combinations

```yaml
# Fireball (Projectile)
skillType: Projectile
targeting: MousePosition
range: 15
damageMultiplier: 1.2
projectilePrefab: [Fireball]
explodeOnImpact: true
explosionRadius: 3
applyBurn: true

# Whirlwind (CircleAOE)
skillType: CircleAOE
targeting: Self
radius: 3
damageMultiplier: 1.0
cooldownTime: 8

# Meteor (GroundAOE)
skillType: GroundAOE
targeting: MousePosition
radius: 4
damageMultiplier: 2.0
explodeOnImpact: true
explosionRadius: 5
castTime: 0.5

# Shadow Dash (Dash)
skillType: Dash
dashDistance: 8
dashSpeed: 25
dashInvulnerable: true
leaveTrail: true

# Heal (Heal)
skillType: Heal
damageMultiplier: 3.0
cooldownTime: 15

# Lightning Chain (Chain)
skillType: Chain
range: 10
chainBounces: 5
chainDamageFalloff: 0.75
```

---

## File Reference

| File | Purpose |
|------|---------|
| `Assets/Scripts/Skills/SkillCaster.cs` | Main skill execution logic |
| `Assets/Scripts/Data/SkillSO.cs` | Skill data ScriptableObject |
| `Assets/Scripts/Data/SkillType.cs` | Skill type enums |
| `Assets/Scripts/Skills/SkillBarUI.cs` | Skill bar UI display |
| `Assets/Scripts/Skills/SkillTooltip.cs` | Hover tooltip system |
| `Assets/Scripts/Skills/SkillSynergyManager.cs` | Combo/synergy system |
| `Assets/Scripts/Skills/Projectile.cs` | Projectile behavior |

---

## Support

For issues or questions:
1. Check the [Known Issues](#known-issues--workarounds) section
2. Enable `verboseLogging` on SkillCaster for detailed logs
3. Validate setup with [Testing Checklist](#testing-checklist)

---

*Last Updated: 2026-03-08*
*Skill System Version: 1.0*

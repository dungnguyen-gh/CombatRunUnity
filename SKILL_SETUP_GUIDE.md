# Complete Skill System Setup Guide

**Date:** 2026-03-08  
**Version:** 2.0 - Full System Integration Guide

---

## 📋 Table of Contents

1. [System Architecture Overview](#1-system-architecture-overview)
2. [Required Components](#2-required-components)
3. [Player Setup (SkillCaster)](#3-player-setup-skillcaster)
4. [Input System Setup](#4-input-system-setup)
5. [UI Setup (SkillBarUI)](#5-ui-setup-skillbarui)
6. [Synergy System Setup](#6-synergy-system-setup)
7. [Tooltip Setup](#7-tooltip-setup)
8. [Creating SkillSO Assets](#8-creating-skillso-assets)
9. [Prefab Requirements by Skill Type](#9-prefab-requirements-by-skill-type)
10. [Testing & Debugging](#10-testing--debugging)

---

## 1. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         SKILL SYSTEM FLOW                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   INPUT SYSTEM              PLAYER                  SKILL CASTER     │
│   ┌──────────┐            ┌──────────┐            ┌──────────────┐  │
│   │ Skill1-4 │───────────▶│ Player   │───────────▶│ Execute      │  │
│   │ Keys     │            │Controller│            │ Skill Logic  │  │
│   └──────────┘            └──────────┘            └──────────────┘  │
│                               │                           │          │
│                               ▼                           ▼          │
│                         ┌──────────┐               ┌──────────────┐ │
│                         │  SPUM    │               │ SkillSO Data │ │
│                         │ Bridge   │               │ (14 Types)   │ │
│                         └──────────┘               └──────────────┘ │
│                                                            │         │
│   UI SYSTEM                                                ▼         │
│   ┌──────────────┐     ┌──────────────┐          ┌──────────────┐   │
│   │ SkillBarUI   │◀────│  Events      │◀─────────│ Apply Damage │   │
│   │ (4 Slots)    │     │  (Cooldown,  │          │ Spawn FX     │   │
│   └──────────────┘     │  Cast)       │          └──────────────┘   │
│                        └──────────────┘                   │          │
│                                                          ▼           │
│   SYNERGY SYSTEM                                    ┌──────────┐    │
│   ┌──────────────┐     ┌──────────────┐             │  Enemy   │    │
│   │ Synergy      │◀────│ Skill Cast   │             │ TakeDamage│   │
│   │ Manager      │     │ Sequence     │             └──────────┘    │
│   └──────────────┘     └──────────────┘                              │
└─────────────────────────────────────────────────────────────────────┘
```

### Component Relationships

| Component | Purpose | Connects To |
|-----------|---------|-------------|
| **SkillCaster** | Core execution logic | PlayerController, SkillSO, SkillBarUI |
| **SkillBarUI** | Displays 4 skill slots | SkillCaster (events), SkillTooltip |
| **SkillSynergyManager** | Tracks combos | PlayerController (OnSkillCast), SkillCaster |
| **SkillTooltip** | Hover tooltips | SkillBarUI slots |
| **Projectile** | Projectile behavior | SkillCaster (Spawn), Enemy (Damage) |

---

## 2. Required Components

### Scene Hierarchy Setup

```
Scene
├── Player (GameObject)
│   ├── Sprite/SPUM Prefab
│   ├── Rigidbody2D
│   ├── PlayerController (Script)
│   ├── SkillCaster (Script) ← Auto-added
│   └── SPUMPlayerBridge (if using SPUM)
│
├── Canvas (UI)
│   ├── HUD Panel
│   │   ├── SkillBarUI (Script)
│   │   │   └── SkillSlots (Container)
│   │   │       ├── Slot1 (Key 1)
│   │   │       ├── Slot2 (Key 2)
│   │   │       ├── Slot3 (Key 3)
│   │   │       └── Slot4 (Key 4)
│   │   └── SkillTooltip (Script)
│   └── ...
│
├── SkillSynergyManager (GameObject) ← Can be anywhere
│   └── SkillSynergyManager (Script)
│
└── Main Camera
    └── CameraFollow (Script)
```

---

## 3. Player Setup (SkillCaster)

### Step 3.1: Add SkillCaster to Player

The `SkillCaster` component is **auto-added** via `[RequireComponent]` on PlayerController, but you need to configure it:

```csharp
// Player GameObject Inspector - SkillCaster Component:

[Header("Skills")]
Skills (Size 4)
├── Element 0: SkillSO_Fireball
├── Element 1: SkillSO_Shield
├── Element 2: SkillSO_Dash
└── Element 3: SkillSO_Heal

[Header("References")]
Cast Point: Player/AttackPoint (Transform)
Enemy Layer: Enemy (LayerMask)
Obstacle Layer: Obstacles (LayerMask)
Player: (Auto-assigned from GetComponent)

[Header("Prefabs")]
Default Projectile Prefab: Prefabs/Projectiles/DefaultProjectile
Default Shield Effect Prefab: Prefabs/Effects/ShieldBubble

[Header("Debug")]
Verbose Logging: [✓] (Enable during setup)
```

### Step 3.2: Cast Point Setup

Create a child GameObject for cast origin:

```
Player
├── Sprite/Visuals
├── AttackPoint (Empty GameObject)
│   └── Position: (0.5, 0, 0) - Slightly in front of player
└── CastPoint ← Drag this to SkillCaster.castPoint
```

### Step 3.3: Layer Configuration

Assign these layers in the inspector:

| Layer | Used For | Setup |
|-------|----------|-------|
| **Enemy** | Enemy detection | Add "Enemy" layer in Tags & Layers, assign to all enemies |
| **Obstacles** | Dash collision | Add "Obstacles" layer, assign to walls/impassable objects |

---

## 4. Input System Setup

### Step 4.1: Create Input Action Asset

```
1. Project window: Right-click > Create > Input Actions
2. Name it "GameControls"
3. Double-click to open Input Actions editor
```

### Step 4.2: Configure Action Map

```yaml
Action Map: Gameplay

Actions:
  Move:
    Action Type: Value
    Control Type: Vector2
    Bindings:
      - WASD (Composite)
      - Arrow Keys (Composite)
      - Left Stick (Gamepad)

  Attack:
    Action Type: Button
    Bindings:
      - Left Click [Mouse]
      - Button South [Gamepad]

  Skill1:
    Action Type: Button
    Bindings:
      - 1 Key [Keyboard]
      - D-Pad Up [Gamepad]

  Skill2:
    Action Type: Button
    Bindings:
      - 2 Key [Keyboard]
      - D-Pad Right [Gamepad]

  Skill3:
    Action Type: Button
    Bindings:
      - 3 Key [Keyboard]
      - D-Pad Down [Gamepad]

  Skill4:
    Action Type: Button
    Bindings:
      - 4 Key [Keyboard]
      - D-Pad Left [Gamepad]

  Inventory:
    Action Type: Button
    Bindings:
      - I Key [Keyboard]
      - Button East [Gamepad]

  Pause:
    Action Type: Button
    Bindings:
      - Escape [Keyboard]
      - Start [Gamepad]
```

### Step 4.3: Assign to PlayerController

```
Player GameObject - PlayerController Component:

Input Actions: GameControls (drag your asset here)
```

### Step 4.4: Input Flow

```
Input System ──▶ PlayerController ──▶ SkillCaster ──▶ Execute Skill
     │                                    │
     │                                    ▼
     │                            PlayerController
     │                            (SPUM Animation)
     │                                    │
     └────────────────────────────────────┘
              (Skill Cast Event)
```

---

## 5. UI Setup (SkillBarUI)

### Step 5.1: Create Skill Bar UI

```
Canvas (Screen Space - Overlay)
└── HUD
    └── SkillBar (GameObject)
        ├── SkillBarUI (Script) ← Add this component
        └── SkillSlots (Empty GameObject)
            ├── Slot1
            ├── Slot2
            ├── Slot3
            └── Slot4
```

### Step 5.2: SkillSlot Prefab Structure

Each slot should have this hierarchy:

```
SkillSlot (GameObject with RectTransform)
├── Background (Image) - Dark background
├── Icon (Image) - Skill icon sprite
├── Cooldown (Image) - Filled radial cooldown overlay
│   └── Image Type: Filled
│   └── Fill Method: Radial 360
├── Border (Image) - Rarity-colored border
├── Key (TextMeshProUGUI) - "1", "2", "3", "4"
└── CooldownText (TextMeshProUGUI) - Remaining seconds
```

### Step 5.3: SkillBarUI Component Setup

```
SkillBar GameObject - SkillBarUI Component:

[References]
Skill Caster: (Leave empty - auto-discover)
Skill Slot Container: SkillSlots (Transform)
Skill Slot Prefab: Prefabs/UI/SkillSlot (optional)

[Auto-Discovery]
Auto Discover Caster: [✓]
Auto Discover Container: [✓]
Container Name: "SkillSlots"

[Empty Slot Visuals]
Empty Slot Sprite: UI/EmptySlot
Empty Slot Color: #444444 (Gray)
Empty Slot Icon Color: #222222 (Dark)
Empty Slot Text: ""
```

### Step 5.4: Event Binding (Automatic)

The SkillBarUI automatically binds to SkillCaster events:

```csharp
// These events are automatically connected:
skillCaster.OnCooldownStarted += OnCooldownStarted;
skillCaster.OnCooldownUpdated += OnCooldownUpdated;
skillCaster.OnSkillCast += OnSkillCast;
skillCaster.OnSkillFailed += OnSkillFailed;
```

### Step 5.5: Visual States

| State | Visual |
|-------|--------|
| **Ready** | Full color icon, no overlay |
| **Cooldown** | Gray icon, radial fill overlay, timer text |
| **Charging** | Yellow border pulse |
| **Empty** | Dark gray slot, no icon |
| **Error** | Red border flash |

---

## 6. Synergy System Setup

### Step 6.1: Create Synergy Manager

```
1. Create Empty GameObject: "SkillSynergyManager"
2. Add Component: SkillSynergyManager
```

### Step 6.2: Default Synergies (Auto-Initialized)

These synergies are created automatically in `Start()`:

| Synergy | Sequence | Effect | Duration |
|---------|----------|--------|----------|
| **Inferno** | 0→1 (Skill1 then Skill2) | Damage Boost | 5s |
| **Shattered Earth** | 1→2 | Empower Next | 3s |
| **Reflecting Shield** | 2→3 | Damage Reduction | 4s |
| **Elemental Overload** | 0→1→2 | Chain Lightning | 6s |
| **Avatar of Power** | 0→1→2→3 | Infinite Mana | 3s |

### Step 6.3: Custom Synergies

To add custom synergies, modify `InitializeDefaultSynergies()` or add via inspector:

```csharp
synergies.Add(new SkillSynergy {
    synergyName = "My Combo",
    description = "Custom combo description",
    requiredSkillSequence = new int[] { 0, 2 }, // Skill1 → Skill3
    timeWindow = 3f,
    effect = SynergyEffect.DamageBoost,
    damageMultiplier = 1.5f,
    effectDuration = 5f
});
```

### Step 6.4: How Synergy Works

```
1. Player casts Skill1 (index 0)
2. Skill cast recorded with timestamp
3. Player casts Skill2 (index 1) within timeWindow
4. Sequence [0,1] matches "Inferno" synergy
5. Synergy activated:
   - Notification shown
   - Damage multiplier applied to next skills
   - Timer starts
6. After duration, synergy ends
```

---

## 7. Tooltip Setup

### Step 7.1: Create Tooltip UI

```
Canvas
└── HUD
    └── SkillTooltip (GameObject)
        └── SkillTooltip (Script)
            ├── TooltipPanel (RectTransform)
            │   ├── Background (Image)
            │   ├── NameText (TextMeshProUGUI)
            │   ├── DescriptionText (TextMeshProUGUI)
            │   ├── StatsText (TextMeshProUGUI)
            │   ├── SkillIcon (Image)
            │   └── RarityBorder (Image)
            └── (Script settings)
```

### Step 7.2: SkillTooltip Component

```
SkillTooltip GameObject:

[References]
Tooltip Panel: TooltipPanel (RectTransform)
Name Text: NameText (TextMeshProUGUI)
Description Text: DescriptionText (TextMeshProUGUI)
Stats Text: StatsText (TextMeshProUGUI)
Skill Icon: SkillIcon (Image)
Rarity Border: RarityBorder (Image)

[Settings]
Offset: (15, 15) - Offset from mouse
Follow Speed: 15 - Smooth follow speed
Constrain To Screen: [✓] - Keep on screen
```

### Step 7.3: Add Triggers to Slots

Add `SkillTooltipTrigger` to each skill slot:

```
Slot1 (GameObject)
├── (Existing slot components)
└── SkillTooltipTrigger (Script)
    ├── Skill: (Auto-set by SkillBarUI)
    └── Tooltip: SkillTooltip (drag from scene)
```

Or use `SkillSlotUI` which includes tooltip trigger automatically.

---

## 8. Creating SkillSO Assets

### Step 8.1: Create Skill

```
1. Project window: Right-click
2. Create > ARPG > Skill
3. Name it "Skill_Fireball"
```

### Step 8.2: Configure by Skill Type

See [SKILL_COMPLETE_REFERENCE.md](SKILL_COMPLETE_REFERENCE.md) for all 14 skill type templates.

Quick reference:

| Skill Type | Required Prefabs | Key Settings |
|------------|------------------|--------------|
| **Projectile** | projectilePrefab | range, speed, homing |
| **CircleAOE** | effectPrefab | radius |
| **GroundAOE** | effectPrefab | radius, castTime |
| **Shield** | persistentEffectPrefab | duration |
| **Dash** | effectPrefab (trail) | dashDistance, dashSpeed |
| **Summon** | summonPrefab | summonCount, summonDuration |
| **Buff** | persistentEffectPrefab | duration, damageMultiplier |
| **Heal** | effectPrefab | damageMultiplier (heal amount) |
| **Chain** | effectPrefab | chainBounces, chainRange |
| **Beam** | persistentEffectPrefab | duration, tickRate |
| **Trap** | effectPrefab | duration |
| **Teleport** | castEffectPrefab, effectPrefab | range |
| **Reflect** | persistentEffectPrefab | duration |
| **Melee** | effectPrefab | range |

---

## 9. Prefab Requirements by Skill Type

### 9.1 Projectile Prefab

```
ProjectilePrefab (GameObject)
├── SpriteRenderer
│   └── Sprite: ProjectileSprite
├── Rigidbody2D
│   └── Gravity Scale: 0
│   └── Collision Detection: Continuous
├── CircleCollider2D
│   └── Is Trigger: [✓]
└── Projectile (Script)
    └── Speed: 10
    └── Max Lifetime: 5
```

### 9.2 Shield Effect Prefab

```
ShieldBubble (GameObject)
├── SpriteRenderer or ParticleSystem
│   └── Sorting Order: 10 (above player)
└── (No collider needed)
```

### 9.3 Summon Prefab

```
AllyPrefab (GameObject)
├── SpriteRenderer
├── Rigidbody2D
├── Collider2D
├── SimpleAI (Script - optional)
│   └── followPlayer: true
│   └── attackEnemies: true
└── DestroyAfterTime (Script)
    └── lifetime: 10
```

### 9.4 Effect Prefabs

```
ExplosionEffect (GameObject)
└── ParticleSystem
    ├── Duration: 0.5
    ├── Start Lifetime: 0.5
    ├── Start Size: 2
    └── Auto Destroy: (Destroy when finished)
```

---

## 10. Testing & Debugging

### 10.1 Enable Verbose Logging

```
SkillCaster Component:
└── Verbose Logging: [✓]
```

Console will show:
```
[SkillCaster] TryCastSkill(0) - CanCastSkill returned true
[SkillCaster] Skill 'Fireball' (slot 1) executed successfully
[SkillCaster] CircleAOE hit 3 enemies
```

### 10.2 Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Skills array must have exactly 4 slots" | Array size wrong | SkillCaster auto-fixes this |
| "Projectile skill needs projectilePrefab" | Missing prefab | Assign in SkillSO or SkillCaster default |
| "No MainCamera found" | Camera tag wrong | Tag camera as "MainCamera" |
| Skill casts but no effect | Missing effectPrefab | Assign visual prefab in SkillSO |
| Skill casts but no damage | enemyLayer not set | Set enemyLayer in SkillCaster |
| Cooldown UI not updating | SkillBarUI not bound | Check SkillCaster reference |
| Synergy not triggering | Wrong sequence | Check skill indices match |

### 10.3 Validation Checklist

```
□ Player has PlayerController
□ Player has SkillCaster
□ SkillCaster has 4 skills assigned
□ SkillCaster.enemyLayer is set to "Enemy"
□ All SkillSO assets have unique skillId
□ All SkillSO assets have icons assigned
□ Projectile skills have projectilePrefab
□ Summon skills have summonPrefab
□ Input Actions asset assigned to PlayerController
□ SkillBarUI has skillSlotContainer assigned
□ SkillSynergyManager exists in scene
```

### 10.4 Testing Commands

Add this test script temporarily:

```csharp
// Add to PlayerController for testing
void Update() {
    // Test skills with number keys
    if (Keyboard.current.numpad1Key.wasPressedThisFrame)
        TryCastSkill(0);
    if (Keyboard.current.numpad2Key.wasPressedThisFrame)
        TryCastSkill(1);
    if (Keyboard.current.numpad3Key.wasPressedThisFrame)
        TryCastSkill(2);
    if (Keyboard.current.numpad4Key.wasPressedThisFrame)
        TryCastSkill(3);
        
    // Reset cooldowns
    if (Keyboard.current.rKey.wasPressedThisFrame)
        skillCaster?.ResetAllCooldowns();
}
```

---

## Quick Start Checklist

```
□ 1. Create Player GameObject with:
   - Rigidbody2D
   - PlayerController (assign Input Actions)
   - SkillCaster (will auto-add)

□ 2. Create 4 SkillSO assets:
   - Right-click > Create > ARPG > Skill
   - Configure each skill type

□ 3. Assign Skills to SkillCaster:
   - Drag SkillSO to Skills[0-3]

□ 4. Set enemyLayer in SkillCaster:
   - Select "Enemy" layer

□ 5. Create SkillBar UI:
   - Add SkillBarUI to Canvas
   - Create 4 slot GameObjects
   - Assign skillSlotContainer

□ 6. Create SkillSynergyManager:
   - Empty GameObject + SkillSynergyManager script

□ 7. Test:
   - Enter Play Mode
   - Press 1-4 to cast skills
   - Check console for errors
```

---

**Related Documents:**
- [SKILL_COMPLETE_REFERENCE.md](SKILL_COMPLETE_REFERENCE.md) - All SkillSO fields explained
- [SETUP_GUIDE_ERRORS_FIXED.md](SETUP_GUIDE_ERRORS_FIXED.md) - Common errors and solutions

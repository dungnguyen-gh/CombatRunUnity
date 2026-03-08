# CombatRun - Overall System Setup Guide

**Project:** Unity 2D ARPG Combat System  
**Date:** 2026-03-08  
**Version:** 1.0

---

## 📚 Table of Contents

1. [Project Overview](#1-project-overview)
2. [System Architecture](#2-system-architecture)
3. [Setup Order - Step by Step](#3-setup-order---step-by-step)
4. [Scene Hierarchy Template](#4-scene-hierarchy-template)
5. [Feature Relationships](#5-feature-relationships)
6. [Critical Issues Summary](#6-critical-issues-summary)
7. [Quick Reference](#7-quick-reference)
8. [Individual Guides Index](#8-individual-guides-index)

---

## 1. Project Overview

CombatRun is a Unity 2D ARPG framework featuring:

| Feature | Description |
|---------|-------------|
| **14 Skill Types** | Projectile, AOE, Shield, Dash, Summon, Buff, Heal, Chain, Beam, Trap, Teleport, Reflect, Melee |
| **Combat System** | Melee combos, elemental status effects, critical hits |
| **Inventory** | 20-slot inventory with weapon/armor equipment |
| **Set Bonuses** | 2-piece and 4-piece equipment set bonuses |
| **Shop** | Buy/sell with weighted rarity system + gambling |
| **Enemy AI** | 6 personality types with 9 skill types |
| **Progression** | Weapon mastery, daily runs with modifiers, wave system |
| **SPUM Support** | Full integration with SPUM 2D character system |

### Technology Stack
- **Unity Version:** 2022.3 LTS+
- **Input System:** New Input System (Unity Input System package)
- **Animation:** Dual system (SPUM multi-part OR legacy Animator)
- **UI:** Unity UI (uGUI) with TextMeshPro
- **Architecture:** Singleton pattern, ScriptableObjects, object pooling

---

## 2. System Architecture

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              GAME ARCHITECTURE                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                        PLAYER SYSTEM                                 │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │    │
│  │  │PlayerController│ │SkillCaster  │ │ComboSystem  │ │PlayerStats │ │    │
│  │  │ - Movement    │  │ - 4 Skills  │  │ - Combos   │  │ - HP/DMG   │ │    │
│  │  │ - Input       │  │ - Cooldowns │  │ - Finisher │  │ - Crit     │ │    │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────┬──────┘ │    │
│  │         │                │                │               │        │    │
│  │         └────────────────┴────────────────┴───────────────┘        │    │
│  │                              │                                      │    │
│  │  ┌───────────────────────────┴───────────────────────────┐         │    │
│  │  │              SPUMPlayerBridge                          │         │    │
│  │  │         (Animation & Equipment Visuals)                │         │    │
│  │  └───────────────────────────┬───────────────────────────┘         │    │
│  └──────────────────────────────┼──────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────┼──────────────────────────────────────┐    │
│  │                         COMBAT SYSTEM                               │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐  │    │
│  │  │StatusEffect │  │Projectile   │  │    DamageNumberManager      │  │    │
│  │  │ - Burn      │  │ - Movement  │  │    - Floating text          │  │    │
│  │  │ - Freeze    │  │ - Homing    │  │    - Object pooling         │  │    │
│  │  │ - Poison    │  │ - Pierce    │  │                             │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────┼──────────────────────────────────────┐    │
│  │                         ENEMY SYSTEM                                │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │    │
│  │  │   Enemy     │  │   EnemyAI   │  │ EnemySkillSO│  │ EnemyPool  │ │    │
│  │  │ - Health    │  │ - States    │  │ - 9 Skills  │  │ - Spawning │ │    │
│  │  │ - Damage    │  │ - Personality│ │ - Cooldowns │  │ - Pooling  │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────┼──────────────────────────────────────┐    │
│  │                      INVENTORY & SHOP                               │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │    │
│  │  │  Inventory  │  │   ItemSO    │  │  ShopManager│  │SetBonusMgr │ │    │
│  │  │ - 20 Slots  │  │ - Stats     │  │ - Buy/Sell  │  │ - 2pc/4pc  │ │    │
│  │  │ - Equipment │  │ - Rarity    │  │ - Gambling  │  │ - Effects  │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────┼──────────────────────────────────────┐    │
│  │                      PROGRESSION SYSTEMS                            │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │    │
│  │  │GameManager  │  │WeaponMastery│  │  DailyRun   │  │ CameraFollow│ │    │
│  │  │ - Waves     │  │ - Kill Count│  │ - Modifiers │  │ - Follow   │ │    │
│  │  │ - Spawning  │  │ - Levels    │  │ - Seed      │  │ - Shake    │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                 │                                            │
│  ┌──────────────────────────────┼──────────────────────────────────────┐    │
│  │                           UI SYSTEM                                 │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │    │
│  │  │  UIManager  │  │  SkillBarUI │  │InventoryUI  │  │  ShopUI    │ │    │
│  │  │ - Panels    │  │ - 4 Slots   │  │ - Grid      │  │ - Preview  │ │    │
│  │  │ - Pause     │  │ - Cooldowns │  │ - Drag/Drop │  │ - Compare  │ │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow Diagram

```
Input Action (Skill1) 
    │
    ▼
PlayerController.TryCastSkill(0)
    │
    ▼
SkillCaster.TryCastSkill(0)
    │
    ├───► Validate (cooldown, resources)
    │
    ├───► ExecuteSkill(0, skillSO)
    │         │
    │         ├───► Spawn Effects
    │         ├───► Apply Damage (Enemy.TakeDamage)
    │         ├───► Apply StatusEffects
    │         └───► Screen Effects (shake)
    │
    ├───► Start Cooldown
    │
    ├───► Fire Events ───► SkillBarUI (update cooldown)
    │                     SkillSynergyManager (check combos)
    │
    └───► Play Animation (SPUMPlayerBridge)
```

---

## 3. Setup Order - Step by Step

**Choose Your Architecture:**

| Architecture | Best For | Setup Guide |
|--------------|----------|-------------|
| **Single Scene** (Arcade) | Quick sessions, roguelike | Follow steps below |
| **Multi-Scene** (Full Game) | Full experience, menus, progression | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) |

---

### Option A: Single Scene Setup

Follow this order for arcade-style single scene setup:

### Phase 1: Core Infrastructure

| Step | Task | Guide | Time |
|------|------|-------|------|
| 1 | Install Unity Input System package | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 10 min |
| 2 | Create Input Action Asset (GameControls) | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 15 min |
| 3 | Create UIManager singleton | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 20 min |
| 4 | Create UI Canvas with HUD | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 30 min |

### Phase 2: Player Setup

| Step | Task | Guide | Time |
|------|------|-------|------|
| 5 | Create Player GameObject | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 20 min |
| 6 | Add PlayerController + configure Input | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 15 min |
| 7 | Add SkillCaster + assign layers | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 20 min |
| 8 | Add ComboSystem | [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) | 10 min |
| 9 | Configure SPUM (if using) | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) | 30 min |

### Phase 3: Combat Systems

| Step | Task | Guide | Time |
|------|------|-------|------|
| 10 | Create DamageNumberManager | [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) | 15 min |
| 11 | Create DamageNumber prefab | [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) | 10 min |
| 12 | Create status effect prefabs | [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) | 30 min |

### Phase 4: Skill System

| Step | Task | Guide | Time |
|------|------|-------|------|
| 13 | Create SkillSO assets (4 skills) | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 30 min |
| 14 | Create projectile prefabs (if needed) | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 20 min |
| 15 | Setup SkillBarUI | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 30 min |
| 16 | Setup SkillTooltip | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 15 min |
| 17 | Add SkillSynergyManager | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 10 min |

### Phase 5: Enemy System

| Step | Task | Guide | Time |
|------|------|-------|------|
| 18 | Create Enemy prefab | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) | 30 min |
| 19 | Configure EnemyAI + personality | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) | 20 min |
| 20 | Create EnemySkillSO (if using skills) | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) | 20 min |
| 21 | Setup EnemyPool | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) | 15 min |

### Phase 6: Game Flow

| Step | Task | Guide | Time |
|------|------|-------|------|
| 22 | Create GameManager | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) | 20 min |
| 23 | Configure spawn points | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) | 10 min |
| 24 | Setup CameraFollow | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) | 10 min |
| 25 | Add WeaponMasteryManager | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) | 10 min |

### Phase 7: Inventory & Shop

| Step | Task | Guide | Time |
|------|------|-------|------|
| 26 | Create InventoryManager | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) | 15 min |
| 27 | Create ItemSO assets | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) | 30 min |
| 28 | Setup InventoryUI | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) | 30 min |
| 29 | Setup SetBonusManager | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) | 15 min |
| 30 | Create EquipmentSetSO | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) | 20 min |
| 31 | Setup ShopManager | [04_SHOP_SYSTEM_SETUP.md](04_SHOP_SYSTEM_SETUP.md) | 15 min |
| 32 | Setup ShopUI | [04_SHOP_SYSTEM_SETUP.md](04_SHOP_SYSTEM_SETUP.md) | 30 min |

### Phase 8: Testing

| Step | Task | Guide | Time |
|------|------|-------|------|
| 33 | Test all skills | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) | 30 min |
| 34 | Test combat (combo, status) | [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) | 20 min |
| 35 | Test inventory/equipment | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) | 20 min |
| 36 | Test shop | [04_SHOP_SYSTEM_SETUP.md](04_SHOP_SYSTEM_SETUP.md) | 15 min |
| 37 | Test enemy AI | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) | 20 min |
| 38 | Test game flow (waves, lives) | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) | 15 min |

**Total Estimated Time:** 9-11 hours for complete setup

---

### Phase 9: Multi-Scene & Save/Load

| Step | Task | Guide | Time |
|------|------|-------|------|
| 39 | Setup multi-scene architecture | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 90 min |
| 40 | Implement save/load system | [09_SAVE_LOAD_SETUP.md](09_SAVE_LOAD_SETUP.md) | 30 min |
| 41 | Test scene transitions | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 20 min |
| 42 | Test continue functionality | [09_SAVE_LOAD_SETUP.md](09_SAVE_LOAD_SETUP.md) | 15 min |

---

### Phase 10: Build & Distribution

| Step | Task | Guide | Time |
|------|------|-------|------|
| 43 | Configure build settings | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) | 20 min |
| 44 | Setup icons and splash | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) | 30 min |
| 45 | Create development build | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) | 15 min |
| 46 | Test on target platform | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) | 30 min |
| 47 | Create release build | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) | 15 min |
| 48 | Prepare for distribution | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) | 30 min |

---

### Option B: Multi-Scene Setup (Alternative Order)

For a complete game with Main Menu, Loading Screen, and Game scenes:

| Phase | Task | Guide | Time |
|-------|------|-------|------|
| 1 | Complete Single Scene Setup (above) | All guides | 9-11 hours |
| 2 | Setup Multi-Scene Architecture | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 1.5 hours |
| 3 | Configure Main Menu | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 30 min |
| 4 | Configure Loading Screen | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 15 min |
| 5 | Configure Build Settings | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 5 min |
| 6 | Test Scene Transitions | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) | 20 min |

**Total Estimated Time:** 11-14 hours for complete multi-scene setup

---

## 4. Scene Hierarchy Template

```
Scene
├── Managers
│   ├── GameManager
│   │   └── GameManager (Script)
│   ├── UIManager
│   │   └── UIManager (Script)
│   ├── InventoryManager
│   │   └── InventoryManager (Script)
│   ├── ShopManager
│   │   └── ShopManager (Script)
│   ├── WeaponMasteryManager
│   │   └── WeaponMasteryManager (Script)
│   ├── DailyRunManager
│   │   └── DailyRunManager (Script)
│   ├── DamageNumberManager
│   │   └── DamageNumberManager (Script)
│   ├── SkillSynergyManager
│   │   └── SkillSynergyManager (Script)
│   ├── SetBonusManager
│   │   └── SetBonusManager (Script)
│   └── EnemyPool
│       └── EnemyPool (Script)
│
├── Player
│   ├── Sprite (or SPUM Prefab)
│   ├── Rigidbody2D
│   ├── PlayerController (Script)
│   │   └── Input Actions: GameControls
│   ├── SkillCaster (Script)
│   │   └── Skills[4]: [SkillSO assets]
│   │   └── Enemy Layer: Enemy
│   ├── ComboSystem (Script)
│   ├── SPUMPlayerBridge (Script) [if using SPUM]
│   └── SPUMEquipmentManager (Script) [if using SPUM]
│
├── Enemies
│   ├── SpawnPoints
│   │   ├── SpawnPoint_01
│   │   ├── SpawnPoint_02
│   │   └── ...
│   └── EnemyContainer (empty for spawned enemies)
│
├── Canvas (UI)
│   ├── HUD
│   │   ├── HealthBar
│   │   ├── GoldText
│   │   ├── SkillBar
│   │   │   └── SkillBarUI (Script)
│   │   │       └── SkillSlots
│   │   │           ├── Slot1
│   │   │           ├── Slot2
│   │   │           ├── Slot3
│   │   │           └── Slot4
│   │   ├── ComboText
│   │   └── WaveText
│   │
│   ├── InventoryPanel
│   │   └── InventoryUI (Script)
│   │
│   ├── ShopPanel
│   │   └── ShopUI (Script)
│   │
│   ├── SkillTooltip
│   │   └── SkillTooltip (Script)
│   │
│   ├── PauseMenu
│   │   └── UIPanel (Script)
│   │
│   ├── GameOverPanel
│   │   └── UIPanel (Script)
│   │
│   └── NotificationArea
│
├── Camera
│   ├── Camera
│   └── CameraFollow (Script)
│
└── World
    └── [Level geometry, obstacles, etc.]
```

---

## 5. Feature Relationships

### Dependency Matrix

| System | Depends On | Used By |
|--------|------------|---------|
| **SkillCaster** | PlayerController, PlayerStats, Input System | SkillBarUI, SkillSynergyManager |
| **SkillBarUI** | SkillCaster, UIManager | - |
| **ComboSystem** | PlayerController | UIManager |
| **StatusEffect** | Enemy, DamageNumberManager | SkillCaster |
| **InventoryManager** | PlayerStats, PlayerController | ShopManager, SetBonusManager, InventoryUI |
| **ShopManager** | InventoryManager, UIManager | ShopUI |
| **Enemy** | PlayerController, DamageNumberManager | GameManager, EnemyPool |
| **EnemyAI** | Enemy, PlayerController | - |
| **GameManager** | PlayerController, Enemy, EnemyPool | - |
| **WeaponMastery** | PlayerController | - |
| **SetBonusManager** | InventoryManager, PlayerController | - |
| **UIManager** | - | All UI panels |
| **SPUMPlayerBridge** | PlayerController | PlayerController |

### Event Flow Example: Player Attack

```
1. Input System → PlayerController.TryMeleeAttack()
                     │
                     ├───► ComboSystem.RegisterHit()
                     │         └───► OnComboChanged → UI Update
                     │
                     ├───► Enemy.TakeDamage()
                     │         ├───► DamageNumberManager.ShowDamage()
                     │         ├───► StatusEffect.ApplyStatus() (if burn on hit)
                     │         └───► OnDeath → GameManager.OnEnemyDeath()
                     │                      └───► WeaponMasteryManager.RegisterKill()
                     │
                     └───► SPUMPlayerBridge.PlayAttackAnimation()
```

---

## 6. Critical Issues Summary

### 🔴 Must Fix Before Release

| Issue | System | Impact | Fix Location |
|-------|--------|--------|--------------|
| **4-piece set bonus impossible** | Inventory | Players can never get 4-piece bonus | See [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) Section 6 |
| **LifeSteal/BurnOnHit never trigger** | Set Bonus | Special effects don't work | See [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) Section 6 |
| **GambleSystem AddGold bug** | Shop | Crash when gambling | See [04_SHOP_SYSTEM_SETUP.md](04_SHOP_SYSTEM_SETUP.md) Section 6 |
| **Elemental reactions not triggered** | Combat | Burn+Poison reaction never fires | See [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) Section 6 |

### 🟡 Should Fix

| Issue | System | Impact | Workaround |
|-------|--------|--------|------------|
| 7 missing skill types | Skills | Some skill types fail | Don't use Blink, Turret, Totem, Channel, AreaDenial, Transform, TimeWarp |
| Reflect skill not implemented | Skills | Reflect just shields | See [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) Section 6 |
| Resource costs not enforced | Skills | Mana costs ignored | N/A (always has resources) |
| Buff skill not implemented | Enemy AI | Enemy buffs do nothing | Don't use Buff skill type on enemies |
| Missing modifier implementations | Daily Run | 5 modifiers don't work | See [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) Section 6 |

### 🟢 Known Limitations

| Issue | System | Note |
|-------|--------|------|
| Item stacking not implemented | Inventory | Each item takes 1 slot regardless of isStackable |
| Gold tracking in two places | Inventory | PlayerController.gold and InventoryManager.Gold may desync |
| Sell price confusing | Shop | Sell price is 10% of buy price (not 50% as expected) |
| Accessory slot unused | Inventory | EquipSlot.Accessory exists but not implemented |

---

## 7. Quick Reference

### Tag Requirements

| Tag | Used By | Purpose |
|-----|---------|---------|
| **Player** | EnemyAI, CameraFollow | Target for enemies, camera |
| **Enemy** | PlayerController, SkillCaster, StatusEffect | Target for player attacks |

### Layer Requirements

| Layer | Used By | Purpose |
|-------|---------|---------|
| **Enemy** | SkillCaster, PlayerController, EnemyPool | Hit detection for attacks |
| **Obstacles** | SkillCaster (Dash) | Wall collision for dash |

### Input Actions Required

| Action | Type | Default Binding |
|--------|------|-----------------|
| Move | Value (Vector2) | WASD / Arrow Keys |
| Attack | Button | Left Click / Ctrl |
| Skill1 | Button | 1 Key |
| Skill2 | Button | 2 Key |
| Skill3 | Button | 3 Key |
| Skill4 | Button | 4 Key |
| Inventory | Button | I Key |
| Pause | Button | Escape |

### Folder Structure for Resources

```
Assets/
├── Resources/
│   └── Sets/              # EquipmentSetSO assets
│       └── [Your sets here]
├── ScriptableObjects/
│   ├── Skills/            # SkillSO assets
│   ├── Items/             # ItemSO assets
│   └── EnemySkills/       # EnemySkillSO assets
├── Prefabs/
│   ├── Projectiles/       # Projectile prefabs
│   ├── Effects/           # Status effect prefabs
│   ├── Enemies/           # Enemy prefabs
│   └── UI/                # UI prefabs
└── Scenes/
    └── MainScene.unity
```

---

## 8. Individual Guides Index

| # | Guide | Topics | File |
|---|-------|--------|------|
| 01 | **Skill System Setup** | SkillCaster, SkillSO, 14 skill types, UI, synergies | [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md) |
| 02 | **Combat System Setup** | ComboSystem, StatusEffect, DamageNumberManager, elemental reactions | [02_COMBAT_SYSTEM_SETUP.md](02_COMBAT_SYSTEM_SETUP.md) |
| 03 | **Inventory & Equipment Setup** | InventoryManager, ItemSO, SetBonusManager, equipment sets | [03_INVENTORY_SYSTEM_SETUP.md](03_INVENTORY_SYSTEM_SETUP.md) |
| 04 | **Shop System Setup** | ShopManager, ShopUI, GambleSystem, buy/sell flow | [04_SHOP_SYSTEM_SETUP.md](04_SHOP_SYSTEM_SETUP.md) |
| 05 | **Enemy System Setup** | Enemy, EnemyAI, EnemySkillSO, EnemyPool, AI personalities | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) |
| 06 | **Progression Systems Setup** | GameManager, WeaponMastery, DailyRun, CameraFollow | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) |
| 07 | **UI & SPUM Setup** | UIManager, Input System, SPUMPlayerBridge, dual animation | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) |
| 08 | **Multi-Scene Setup** | SceneTransitionManager, MainMenu, Loading screen, scene architecture | [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) |
| 09 | **Save/Load Setup** | SaveLoadManager, persistent data, continue functionality | [09_SAVE_LOAD_SETUP.md](09_SAVE_LOAD_SETUP.md) |
| 10 | **Build Setup** | Build settings, platforms, distribution | [10_BUILD_SETUP.md](10_BUILD_SETUP.md) |
| 05 | **Enemy System Setup** | Enemy, EnemyAI, EnemySkillSO, EnemyPool, AI personalities | [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md) |
| 06 | **Progression Systems Setup** | GameManager, WeaponMastery, DailyRun, CameraFollow | [06_PROGRESSION_SYSTEMS_SETUP.md](06_PROGRESSION_SYSTEMS_SETUP.md) |
| 07 | **UI & SPUM Setup** | UIManager, Input System, SPUMPlayerBridge, dual animation | [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) |

---

## Getting Started Checklist

### Phase 1: Foundation
- [ ] Read this Overall Setup Guide completely
- [ ] Decide: SPUM or Legacy animation system?
- [ ] Decide: Single scene or Multi-scene architecture?
- [ ] Complete Phase 1: Core Infrastructure
- [ ] Complete Phase 2: Player Setup
- [ ] Complete Phase 3: Combat Systems

### Phase 2: Core Gameplay
- [ ] Create at least 4 SkillSO assets
- [ ] Test basic skill casting
- [ ] Create 1-2 enemy types
- [ ] Test combat (player vs enemy)
- [ ] Complete Phase 6: Game flow

### Phase 3: Full Game
- [ ] Complete Phase 7: Inventory & Shop
- [ ] Review all Critical Issues and apply fixes
- [ ] Setup Multi-Scene architecture (if using)
- [ ] Implement Save/Load system
- [ ] Test Continue functionality

### Phase 4: Polish & Release
- [ ] Configure Build Settings
- [ ] Setup Icons and Splash Screen
- [ ] Create Development Build
- [ ] Test on target platform
- [ ] Create Release Build
- [ ] Full playtest
- [ ] Distribute!

---

**Next Steps:** Choose a system to set up and refer to its individual guide:
- New to the project? Start with [07_UI_AND_SPUM_SETUP.md](07_UI_AND_SPUM_SETUP.md) (Player + Input)
- Setting up skills? Go to [01_SKILL_SYSTEM_SETUP.md](01_SKILL_SYSTEM_SETUP.md)
- Creating enemies? See [05_ENEMY_SYSTEM_SETUP.md](05_ENEMY_SYSTEM_SETUP.md)

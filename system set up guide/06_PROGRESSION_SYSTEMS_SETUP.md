# Progression Systems Setup Guide

## Table of Contents
1. [Overview](#1-overview)
2. [Weapon Mastery System](#2-weapon-mastery-system)
3. [Daily Run System](#3-daily-run-system)
4. [Wave System (GameManager)](#4-wave-system-gamemanager)
5. [Camera System](#5-camera-system)
6. [Known Issues](#6-known-issues)
7. [Scene Setup Requirements](#7-scene-setup-requirements)
8. [Testing Checklist](#8-testing-checklist)

---

## 1. Overview

The Combat Run progression systems provide long-term engagement through persistent weapon mastery tracking, daily challenge runs with modifiers, and structured wave-based gameplay.

### Systems Included:
| System | Purpose | Persistence |
|--------|---------|-------------|
| **Weapon Mastery** | Track weapon-specific kills and grant permanent bonuses | Save/Load |
| **Daily Run** | Daily seeded challenges with random modifiers | Session-based |
| **Wave System** | Core gameplay loop with scaling difficulty | None |
| **Camera System** | Follow player with screen shake effects | None |

---

## 2. Weapon Mastery System

### 2.1 Files Overview

| File | Path | Purpose |
|------|------|---------|
| `WeaponMasteryManager.cs` | `Assets/Scripts/Managers/WeaponMasteryManager.cs` | Main manager singleton |
| `WeaponMastery.cs` | `Assets/Scripts/Data/WeaponMastery.cs` | Data structure and logic |

### 2.2 Setup and Configuration

#### Step 1: Create the Manager
1. Create an empty GameObject in your scene
2. Name it `WeaponMasteryManager`
3. Attach the `WeaponMasteryManager` script
4. The script is marked `DontDestroyOnLoad` - only needs to exist in one scene

#### Step 2: Configure UI Notifications
In the Inspector:
```
WeaponMasteryManager
├── Show Mastery Notifications: [✓] (checked)
└── Mastery List: (auto-populated at runtime)
```

### 2.3 Weapon Type Tracking

Weapon types are identified by string identifiers. Common types:

```csharp
// Supported weapon types (configurable in your weapon system)
"Sword"
"Axe"
"Spear"
"Bow"
"Dagger"
"Staff"
"Hammer"
"Whip"
```

#### Registering Kills
```csharp
// From your weapon/attack system when an enemy dies
WeaponMasteryManager.Instance?.RegisterKill("Sword");
```

### 2.4 Level Thresholds and Bonuses

#### Level Progression Table

| Level | Kills Required | Total Kills | Bonus Damage | Bonus Crit | Bonus Attack Speed |
|-------|----------------|-------------|--------------|------------|-------------------|
| 1 | 10 | 10 | +2 | +2% | +5% |
| 2 | 25 | 35 | +4 | +4% | +10% |
| 3 | 50 | 85 | +6 | +6% | +15% |
| 4 | 100 | 185 | +8 | +8% | +20% |
| 5 | 200 | 385 | +10 | +10% | +25% |

#### Bonus Scaling Formula
```csharp
// Located in WeaponMasteryData.CalculateBonuses()
bonusDamage = masteryLevel * 2;
bonusCrit = masteryLevel * 0.02f;      // 2% per level
bonusAttackSpeed = masteryLevel * 0.05f; // 5% per level
```

### 2.5 UI Notifications

Level-up notifications are automatically displayed through `UIManager.Instance.ShowNotification()`.

#### Notification Format
```
{Sword} Mastery Level {3}!
+{6} DMG, +{6}% Crit
```

#### Events Available
```csharp
// Subscribe to mastery events
WeaponMasteryManager.Instance.OnMasteryLevelUp += (weaponType, level) => {
    Debug.Log($"{weaponType} leveled up to {level}!");
};

WeaponMasteryManager.Instance.OnEnemyKilled += (weaponType) => {
    // Update UI counters, etc.
};
```

### 2.6 Save/Load Integration

#### Save Mastery Data
```csharp
// Get serializable data
List<WeaponMasteryEntry> masteryData = WeaponMasteryManager.Instance.GetMasteryDataForSave();

// Save using your preferred method (JSON, PlayerPrefs, etc.)
string json = JsonUtility.ToJson(new Serialization<WeaponMasteryEntry>(masteryData));
PlayerPrefs.SetString("WeaponMastery", json);
```

#### Load Mastery Data
```csharp
// Load from storage
string json = PlayerPrefs.GetString("WeaponMastery", "");
List<WeaponMasteryEntry> loadedData = JsonUtility.FromJson<Serialization<WeaponMasteryEntry>>(json).ToList();

// Apply to manager
WeaponMasteryManager.Instance.LoadMasteryData(loadedData);
```

#### Serialization Helper
```csharp
[System.Serializable]
public class Serialization<T> {
    [SerializeField] List<T> target;
    public Serialization(List<T> target) { this.target = target; }
    public List<T> ToList() { return target; }
}
```

---

## 3. Daily Run System

### 3.1 Files Overview

| File | Path | Purpose |
|------|------|---------|
| `DailyRunManager.cs` | `Assets/Scripts/Managers/DailyRunManager.cs` | Manager singleton |

### 3.2 Seed Generation

The daily run uses a date-based seed for deterministic randomization:

```csharp
// Seed format: YYYYMMDD (e.g., "20260308")
RunDate = DateTime.Now;
currentSeed = RunDate.ToString("yyyyMMdd");

// Used for consistent random selection
System.Random rng = new System.Random(currentSeed.GetHashCode());
```

### 3.3 Modifier Types and Effects

#### Available Modifiers

| Modifier | Type Enum | Player Effect | Enemy Effect | Other Effects |
|----------|-----------|---------------|--------------|---------------|
| **Double Damage** | `DoubleDamage` | 2x Damage | None | None |
| **Glass Cannon** | `GlassCannon` | 2x Damage | 2x Damage dealt to player | -5 Defense |
| **Tank** | `Tank` | 2x Defense | None | 0.5x Attack Speed |
| **Gold Rush** | `PoorStart` | None | None | 3x Gold drops |
| **Enemy Swarm** | `EnemySwarm` | None | 0.7x HP | 2x Enemy count per wave |
| **Hardcore** | `Hardcore` | None | None | Permadeath (no continues) |

#### Not Yet Implemented

| Modifier | Type Enum | Planned Effect | Status |
|----------|-----------|----------------|--------|
| **Speed Demon** | `SpeedDemon` | 2x Player Speed, Faster Enemies | ❌ Missing |
| **Rich Start** | `RichStart` | Start with 500 gold | ❌ Missing |
| **Poor Start** | `PoorStart` | Start with 0 gold, enemies drop 2x | ❌ Missing |
| **Random Skills** | `RandomSkills` | Skills randomized every wave | ❌ Missing |
| **Boss Rush** | `BossRush` | Boss every 3 waves | ❌ Missing |

### 3.4 Leaderboard Setup

#### Result Structure
```csharp
public class DailyRunResult {
    public long dateTimestamp;    // Serializable DateTime
    public int waveReached;       // Highest wave achieved
    public int enemiesKilled;     // Total kills
    public int goldCollected;     // Gold earned
    public bool victory;          // Defeated boss?
    public DailyModifier[] modifiers; // Active modifiers
}
```

#### Submitting Results
```csharp
// Called at end of daily run
DailyRunManager.Instance.SubmitRunResult(
    wave: GameManager.Instance.currentWave,
    kills: GameManager.Instance.enemiesKilled,
    gold: player.gold,
    victory: bossDefeated
);
```

#### Leaderboard Sorting
Results are sorted by:
1. **Wave reached** (descending)
2. **Enemies killed** (descending)

### 3.5 Usage

#### Generate Daily Run
```csharp
// Auto-generates based on today's date
DailyRunManager.Instance.GenerateDailyRun();
```

#### Start Daily Run
```csharp
// Applies modifiers and fires event
DailyRunManager.Instance.StartDailyRun();

// Subscribe to start event
DailyRunManager.Instance.OnDailyRunStarted += (modifiers) => {
    foreach (var mod in modifiers) {
        Debug.Log($"Active: {mod.modifierName}");
    }
};
```

#### Check If Daily Run
```csharp
bool isDaily = DailyRunManager.Instance.isDailyRun;
string seed = DailyRunManager.Instance.currentSeed;
string modifierList = DailyRunManager.Instance.GetModifierSummary();
```

---

## 4. Wave System (GameManager)

### 4.1 Files Overview

| File | Path | Purpose |
|------|------|---------|
| `GameManager.cs` | `Assets/Scripts/Managers/GameManager.cs` | Core game loop manager |

### 4.2 Game Flow Setup

```
[Game Start] → [Wave 1 Spawn] → [Clear Enemies] → [Wait Timer] → [Wave 2] → ... → [Boss Wave] → [Victory/Defeat]
```

### 4.3 Wave Configuration

#### Inspector Settings
```
GameManager
├── Game State
│   ├── Is Game Active: (runtime)
│   └── Current Wave: (runtime)
├── Spawning
│   ├── Time Between Waves: 5 seconds
│   ├── Enemies Per Wave: 5 (base)
│   └── Max Enemies: 20 (concurrent cap)
├── Boss
│   ├── Boss Wave: 5 (waves before boss)
│   ├── Boss Prefab: [assign]
│   └── Boss Spawn Point: [assign]
├── References
│   ├── Player: [auto-detected]
│   ├── Enemy Container: [optional parent]
│   └── Enemy Pool: [optional]
└── Player Lives
    └── Starting Lives: 3
```

### 4.4 Enemy Spawning

#### Wave Scaling Formula
```csharp
// Located in GameManager.StartNextWave()
int enemyCount = enemiesPerWave + (currentWave * 2);

// Example scaling:
// Wave 1: 5 + (1 * 2) = 7 enemies
// Wave 2: 5 + (2 * 2) = 9 enemies
// Wave 3: 5 + (3 * 2) = 11 enemies
// Wave 4: 5 + (4 * 2) = 13 enemies
```

#### Spawn Points
1. Create empty GameObjects at desired spawn locations
2. Assign them to `spawnPoints` array in GameManager
3. Enemies spawn randomly at these points

#### Enemy Prefabs
Assign enemy prefabs to `enemyPrefabs` array:
```
enemyPrefabs
├── [0] BasicEnemy
├── [1] FastEnemy
├── [2] TankEnemy
└── ... (add more variants)
```

### 4.5 Boss Setup

#### Boss Wave Trigger
```csharp
// Boss spawns when: currentWave + 1 >= bossWave
// Default: Wave 5 is boss wave
```

#### Boss Configuration
1. Create boss enemy prefab with `Enemy` component
2. Assign to `bossPrefab` field
3. Create dedicated spawn point and assign to `bossSpawnPoint`
4. The boss replaces the normal wave spawn

### 4.6 Lives and Revive System

#### Life System Flow
```
[Player Dies] → [Check Lives > 0] → [Yes: Revive at current wave] → [No: Game Over]
```

#### Events
```csharp
// Subscribe to GameManager events
GameManager.Instance.OnWaveStarted += (waveNumber) => {
    UIManager.Instance?.ShowNotification($"Wave {waveNumber}");
};

GameManager.Instance.OnBossSpawned += () => {
    // Boss music, effects, etc.
};

GameManager.Instance.OnGameOver += () => {
    // Show game over screen
};

GameManager.Instance.OnGameWon += () => {
    // Victory sequence
};

GameManager.Instance.OnGameRestarted += () => {
    // Cleanup, reset state
};
```

#### Player Life Events
```csharp
// PlayerController events (subscribed by GameManager)
player.OnPlayerDied += () => { /* Handle death */ };
player.OnPlayerRevived += () => { /* Handle revive */ };
```

---

## 5. Camera System

### 5.1 Files Overview

| File | Path | Purpose |
|------|------|---------|
| `CameraFollow.cs` | `Assets/Scripts/Managers/CameraFollow.cs` | Camera controller |

### 5.2 CameraFollow Setup

#### Required Components
1. Camera GameObject with `Camera` component
2. `CameraFollow` script attached
3. Ensure camera is in orthographic mode (for 2D)

#### Inspector Settings
```
CameraFollow
├── Follow Settings
│   ├── Target: [auto-detected, or assign Player]
│   ├── Smooth Speed: 15 (higher = snappier)
│   └── Offset: (0, 0, -10) for 2D
├── Look Ahead (Optional)
│   ├── Use Look Ahead: [ ] (default: off)
│   ├── Look Ahead Distance: 0.5
│   └── Look Ahead Speed: 5
├── Bounds (Optional)
│   ├── Use Bounds: [ ]
│   ├── Min Bounds: (x, y)
│   └── Max Bounds: (x, y)
└── Zoom (Optional)
    ├── Use Dynamic Zoom: [ ]
    ├── Min Zoom: 4
    ├── Max Zoom: 8
    └── Zoom Speed: 2
```

### 5.3 Target Detection Priority

Camera automatically finds player in this order:
1. **Assigned Target** (manual assignment)
2. **"Player" Tag** (`GameObject.FindGameObjectWithTag("Player")`)
3. **PlayerController Component** (`FindFirstObjectByType<PlayerController>()`)
4. **GameManager Reference** (`GameManager.Instance.player`)

#### Manual Target Assignment
```csharp
// For cutscenes or special sequences
CameraFollow.Instance.SetTarget(bossTransform);

// Clear target
CameraFollow.Instance.ClearTarget();

// Reacquire player
CameraFollow.Instance.TryFindPlayer();
```

### 5.4 Screen Shake

#### Trigger Shake
```csharp
// Simple shake
CameraFollow.Instance.Shake(duration: 0.5f, magnitude: 0.3f);

// Heavy impact
CameraFollow.Instance.Shake(duration: 1.0f, magnitude: 0.5f);

// Light shake
CameraFollow.Instance.Shake(duration: 0.2f, magnitude: 0.1f);
```

#### Common Shake Presets
```csharp
public static class ShakePresets {
    public static void LightHit() => 
        CameraFollow.Instance?.Shake(0.2f, 0.1f);
    public static void HeavyHit() => 
        CameraFollow.Instance?.Shake(0.5f, 0.3f);
    public static void Explosion() => 
        CameraFollow.Instance?.Shake(1.0f, 0.5f);
    public static void Footstep() => 
        CameraFollow.Instance?.Shake(0.1f, 0.02f);
}
```

### 5.5 Smooth Following

#### Smooth Speed Recommendations
| Value | Feel | Use Case |
|-------|------|----------|
| 5-10 | Smooth/Cinematic | Slow-paced, exploration |
| 15-25 | Responsive | Action combat (recommended) |
| 30+ | Snappy/Tight | Fast-paced, precise platforming |

#### Position Update
```csharp
// Uses Lerp for responsive following
float t = smoothSpeed * Time.deltaTime;
t = Mathf.Clamp01(t); // Prevent overshoot
currentTargetPos = Vector3.Lerp(transform.position, desiredPos, t);
```

---

## 6. Known Issues

### 6.1 Missing Modifier Implementations

**Status:** ⚠️ 5 modifiers defined but not implemented in `ApplyModifier()`

#### Missing Implementations in DailyRunManager.cs

| Modifier | Issue | Fix Required |
|----------|-------|--------------|
| `SpeedDemon` | Not in switch statement | Add speed multiplier logic |
| `RichStart` | Not in switch statement | Add starting gold |
| `PoorStart` | Listed but incomplete | Complete 2x enemy drops |
| `RandomSkills` | Not in switch statement | Implement skill randomization |
| `BossRush` | Not in switch statement | Modify `bossWave` interval |

#### Suggested Fix
```csharp
// In DailyRunManager.ApplyModifier(), add:
case ModifierType.SpeedDemon:
    player.stats.moveSpeed *= 2f;
    // Also need to increase enemy speed
    break;
    
case ModifierType.RichStart:
    player.AddGold(500);
    break;
    
case ModifierType.RandomSkills:
    // Hook into wave start event to randomize
    GameManager.Instance.OnWaveStarted += (_) => player.RandomizeSkills();
    break;
    
case ModifierType.BossRush:
    var gm = FindFirstObjectByType<GameManager>();
    if (gm != null) gm.bossWave = 3; // Every 3 waves
    break;
```

### 6.2 Weapon Mastery Bonuses Not Applied

**Status:** ⚠️ Method exists but not called from player/equipment system

#### Current State
```csharp
// WeaponMasteryManager.cs line 109-114
public void ApplyMasteryBonuses(PlayerStats stats, string weaponType) {
    var mastery = GetMastery(weaponType);
    stats.damageMod += mastery.bonusDamage;
    stats.critMod += mastery.bonusCrit;
    stats.attackSpeedMod += mastery.bonusAttackSpeed;
}
```

#### Required Fix
```csharp
// In PlayerController or EquipmentManager when equipping weapon:
void EquipWeapon(Weapon newWeapon) {
    currentWeapon = newWeapon;
    
    // Apply mastery bonuses
    WeaponMasteryManager.Instance?.ApplyMasteryBonuses(
        stats, 
        newWeapon.weaponType
    );
    
    UpdateStatsFromEquipment();
}
```

### 6.3 Daily Run Not Submitted

**Status:** ⚠️ `SubmitRunResult()` exists but no caller

#### Required Fix
```csharp
// In GameManager.OnPlayerGameOver():
public void OnPlayerGameOver() {
    // ... existing code ...
    
    // Submit daily run result if applicable
    if (DailyRunManager.Instance?.isDailyRun == true) {
        DailyRunManager.Instance.SubmitRunResult(
            currentWave,
            enemiesKilled,
            player.gold,
            false // victory = false on game over
        );
    }
    
    // ... rest of method ...
}

// In GameManager.OnBossDeath():
void OnBossDeath(Enemy boss) {
    OnGameWon?.Invoke();
    
    // Submit daily run result if applicable
    if (DailyRunManager.Instance?.isDailyRun == true) {
        DailyRunManager.Instance.SubmitRunResult(
            currentWave,
            enemiesKilled,
            player.gold,
            true // victory = true
        );
    }
    
    // ... rest of method ...
}
```

### 6.4 Tank Modifier Not Working

**Status:** ⚠️ Applies to wrong stats

#### Current (Buggy)
```csharp
case ModifierType.Tank:
    player.stats.baseDefense *= 2;
    player.stats.baseAttackSpeed *= 0.5f;
    break;
```

#### Issue
- `baseDefense` may not be the stat used in damage calculations
- Need to verify `PlayerStats` field names match

#### Suggested Fix
```csharp
case ModifierType.Tank:
    // Verify correct stat field names
    player.stats.defenseMod += player.stats.baseDefense; // +100% defense
    player.stats.attackSpeedMod -= 0.5f; // -50% attack speed
    break;
```

---

## 7. Scene Setup Requirements

### 7.1 Required GameObjects

Create the following hierarchy in your main scene:

```
Managers (Empty GameObject)
├── GameManager
│   ├── Script: GameManager.cs
│   └── Tag: Untagged
├── WeaponMasteryManager
│   ├── Script: WeaponMasteryManager.cs
│   └── Tag: Untagged
├── DailyRunManager
│   ├── Script: DailyRunManager.cs
│   └── Tag: Untagged
└── EnemyPool (optional)
    └── Script: EnemyPool.cs

Camera
├── Camera
├── Script: CameraFollow.cs
├── Tag: MainCamera
└── Projection: Orthographic

Spawning
├── SpawnPoints (Empty parent)
│   ├── SpawnPoint_1 (Transform)
│   ├── SpawnPoint_2 (Transform)
│   ├── SpawnPoint_3 (Transform)
│   └── SpawnPoint_4 (Transform)
└── BossSpawnPoint (Transform)

Enemies
└── EnemyContainer (Empty parent for organization)
    └── (enemies spawned here at runtime)
```

### 7.2 Script Execution Order

Recommended execution order:
1. `DailyRunManager` (needs to exist first for modifiers)
2. `WeaponMasteryManager` (needs to exist for save/load)
3. `GameManager` (depends on other managers)
4. `CameraFollow` (follows player, can be any order)

### 7.3 Prefab References

#### GameManager Required References
```csharp
// Assign in Inspector
spawnPoints: Transform[]      // Minimum: 1
enemyPrefabs: GameObject[]   // Minimum: 1
bossPrefab: GameObject       // Optional (game ends without boss)
bossSpawnPoint: Transform    // Optional
enemyContainer: Transform    // Optional (organization)
enemyPool: EnemyPool         // Optional (performance)
```

#### CameraFollow Setup
```csharp
// Auto-detects, but can pre-assign
target: Transform            // Player transform (optional)
cam: Camera                  // Auto-cached from GetComponent()
```

---

## 8. Testing Checklist

### 8.1 Weapon Mastery Tests

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 1 | Kill 10 enemies with Sword | Mastery Level 1, +2 DMG bonus | [ ] |
| 2 | Kill 25 enemies with Sword | Mastery Level 2, +4 DMG bonus | [ ] |
| 3 | Level up notification | UI shows "Sword Mastery Level X!" | [ ] |
| 4 | Save and reload game | Mastery persists correctly | [ ] |
| 5 | Apply mastery bonuses | Stats increase when weapon equipped | [ ] |
| 6 | Multiple weapon types | Each tracked separately | [ ] |

### 8.2 Daily Run Tests

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 1 | Generate daily run | Seed = today's date (YYYYMMDD) | [ ] |
| 2 | 2-3 modifiers selected | Console shows modifiers list | [ ] |
| 3 | Double Damage modifier | Player deals 2x damage | [ ] |
| 4 | Glass Cannon modifier | 2x damage dealt and taken | [ ] |
| 5 | Enemy Swarm modifier | 2x enemies, 0.7x HP | [ ] |
| 6 | Hardcore modifier | No continues allowed | [ ] |
| 7 | Submit run result | Result appears in leaderboard | [ ] |
| 8 | Leaderboard sorting | Highest wave first, then kills | [ ] |

### 8.3 Wave System Tests

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 1 | Start game | Wave 1 spawns after 2s delay | [ ] |
| 2 | Wave scaling | Each wave has 2 more enemies | [ ] |
| 3 | Wave timer | 5s between waves (configurable) | [ ] |
| 4 | Max enemies | Never exceeds maxEnemies cap | [ ] |
| 5 | Boss wave | Boss spawns at configured wave | [ ] |
| 6 | Boss defeat | Victory screen appears | [ ] |
| 7 | Player death with lives | Revive at current wave | [ ] |
| 8 | Player death no lives | Game over screen | [ ] |
| 9 | Restart game | Scene reloads, state resets | [ ] |

### 8.4 Camera System Tests

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 1 | Player following | Camera follows player smoothly | [ ] |
| 2 | Target detection | Finds player automatically | [ ] |
| 3 | Screen shake | Visual shake effect on trigger | [ ] |
| 4 | Shake decay | Smoothly returns to normal | [ ] |
| 5 | Bounds (if enabled) | Camera stays within bounds | [ ] |
| 6 | Look ahead (if enabled) | Leads player movement | [ ] |

### 8.5 Integration Tests

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 1 | Daily + Weapon Mastery | Kills counted during daily run | [ ] |
| 2 | Daily + Wave System | Modifiers affect wave spawning | [ ] |
| 3 | Boss + Camera | Camera shakes on boss death | [ ] |
| 4 | Game Over + Daily Submit | Result submitted automatically | [ ] |
| 5 | Full game loop | Start → Waves → Boss → Victory | [ ] |

---

## Quick Reference

### Event Subscriptions
```csharp
// Weapon Mastery
WeaponMasteryManager.Instance.OnMasteryLevelUp += OnMasteryUp;
WeaponMasteryManager.Instance.OnEnemyKilled += OnEnemyKilled;

// Daily Run
DailyRunManager.Instance.OnDailyRunStarted += OnDailyStart;

// Game State
GameManager.Instance.OnWaveStarted += OnWaveStart;
GameManager.Instance.OnBossSpawned += OnBossSpawn;
GameManager.Instance.OnGameOver += OnGameOver;
GameManager.Instance.OnGameWon += OnVictory;
GameManager.Instance.OnGameRestarted += OnRestart;
```

### Common Configuration Values
```csharp
// Weapon Mastery
killsPerLevel = { 10, 25, 50, 100, 200 }

// Wave System
timeBetweenWaves = 5f
enemiesPerWave = 5
maxEnemies = 20
bossWave = 5
startingLives = 3

// Camera
smoothSpeed = 15f
offset = new Vector3(0, 0, -10)
```

---

*Last Updated: 2026-03-08*
*For Combat Run v1.0*


---

## 9. Multi-Scene Support

### 9.1 Persistent Singletons

All progression managers support multi-scene architecture:

| Manager | Persistence | Scene Handling |
|---------|-------------|----------------|
| **GameManager** | DontDestroyOnLoad | Re-initializes on Game scene load |
| **WeaponMasteryManager** | DontDestroyOnLoad | Persists across all scenes |
| **DailyRunManager** | DontDestroyOnLoad | Persists across all scenes |
| **EnemyPool** | DontDestroyOnLoad | Clears pools when leaving Game scene |

### 9.2 GameManager Scene Transitions

GameManager automatically handles scene changes:

```csharp
// On Game scene loaded:
// 1. Cleanup old player references
// 2. Find new player in scene
// 3. Find spawn points (by tag)
// 4. Start wave system

// On other scenes:
// 1. Stop wave system
// 2. Return enemies to pool
// 3. Cleanup game state
```

### 9.3 Progress Saving on Scene Exit

GameManager automatically saves progress when leaving:

```csharp
// Saved to PlayerPrefs:
- HighWave (best wave reached)
- TotalKills (accumulated across runs)
- HasActiveRun (for Continue button)
```

### 9.4 Multi-Scene Setup

See [08_MULTI_SCENE_SETUP.md](08_MULTI_SCENE_SETUP.md) for complete multi-scene configuration including:
- SceneTransitionManager setup
- MainMenu scene
- Loading scene
- Build settings configuration

### 9.5 Scene-Specific Notes

#### MainMenu Scene
- GameManager exists but `isGameActive = false`
- Wave system does not run
- Player stats shown from saved data

#### Loading Scene
- Minimal scene (UI only)
- Progress bar updates via SceneTransitionManager
- Tips rotate during load

#### Game Scene
- Full game loop active
- Wave spawning begins
- All combat systems active

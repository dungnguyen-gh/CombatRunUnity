# Save/Load System Setup Guide

**Date:** 2026-03-08  
**Version:** 1.0

---

## 📋 Table of Contents

1. [Overview](#1-overview)
2. [What Gets Saved](#2-what-gets-saved)
3. [Setup Instructions](#3-setup-instructions)
4. [Save/Load Flow](#4-saveload-flow)
5. [Data Structures](#5-data-structures)
6. [Manual Save/Load](#6-manual-saveload)
7. [Testing Checklist](#7-testing-checklist)

---

## 1. Overview

The Save/Load system uses **PlayerPrefs** for simplicity (can be upgraded to JSON/Binary for more complex needs).

### Features:
- ✅ Cumulative stats (total kills, high wave, total gold)
- ✅ Weapon mastery levels and kills
- ✅ Active run state (for Continue functionality)
- ✅ Daily run leaderboard
- ✅ Options settings
- ✅ Version migration support

---

## 2. What Gets Saved

### Cumulative Stats (Permanent)
| Stat | Key | Description |
|------|-----|-------------|
| Total Kills | `TotalKills` | Accumulated across all runs |
| High Wave | `HighWave` | Best wave reached |
| Total Gold | `TotalGoldCollected` | Accumulated across all runs |

### Active Run (Session)
| Data | Key | Description |
|------|-----|-------------|
| Has Active Run | `HasActiveRun` | 1 if run can be continued |
| Current Wave | `CurrentRunWave` | Wave to resume from |
| Kills | `CurrentRunKills` | Kills in current run |
| Gold | `CurrentRunGold` | Gold in current run |
| Lives | `CurrentRunLives` | Remaining lives |
| Weapon ID | `PlayerWeapon` | Equipped weapon |
| Armor ID | `PlayerArmor` | Equipped armor |

### Weapon Mastery
| Data | Key Pattern | Description |
|------|-------------|-------------|
| Mastery Level | `Mastery_{Type}_Level` | Level for each weapon type |
| Total Kills | `Mastery_{Type}_Kills` | Kills for each weapon type |

### Daily Run Leaderboard
| Data | Key Pattern | Description |
|------|-------------|-------------|
| Entry Count | `DailyRun_Count` | Number of saved results |
| Wave | `DailyRun_{i}_Wave` | Wave reached |
| Kills | `DailyRun_{i}_Kills` | Enemies killed |
| Gold | `DailyRun_{i}_Gold` | Gold collected |
| Victory | `DailyRun_{i}_Victory` | 1 if boss defeated |

### Options
| Setting | Key | Default |
|---------|-----|---------|
| Music Volume | `MusicVolume` | 1.0 |
| SFX Volume | `SFXVolume` | 1.0 |
| Fullscreen | `Fullscreen` | 1 (true) |

---

## 3. Setup Instructions

### Step 1: Add SaveLoadManager

Create in **MainMenu scene only** (will persist):

```
GameObject: SaveLoadManager
Components:
  - SaveLoadManager (Script)
    - Log Save Load: [ ] (enable for debugging)
```

This is automatically created by SceneTransitionManager if not present.

### Step 2: Ensure Proper Execution Order

No special setup needed - SaveLoadManager auto-initializes on Awake.

### Step 3: Test Save/Load

1. Play game, kill some enemies
2. Exit to menu
3. Check stats are saved
4. Continue game - should restore state

---

## 4. Save/Load Flow

### Automatic Saving

| Trigger | What Gets Saved |
|---------|-----------------|
| Game Over | Cumulative stats, weapon mastery, clear active run |
| Victory (boss killed) | Cumulative stats, weapon mastery, clear active run |
| Return to Menu | Cumulative stats, weapon mastery, active run state |
| Weapon Mastery Level Up | Weapon mastery data |
| Daily Run Complete | Leaderboard entry |

### Automatic Loading

| Trigger | What Gets Loaded |
|---------|------------------|
| Main Menu Opens | Cumulative stats (for display) |
| Continue Clicked | Active run state |
| Daily Run Panel Opens | Leaderboard entries |
| Mastery Panel Opens | Weapon mastery data |
| Game Starts | Equipment (if continuing) |

### Continue Game Flow

```
Player clicks "Continue"
       │
       ▼
┌─────────────────────┐
│ SaveLoadManager     │
│ LoadActiveRun()     │
└──────────┬──────────┘
           │
           ├─── Wave, Kills, Gold, Lives
           ├─── Weapon ID, Armor ID
           └─── HasActiveRun = true
                      │
                      ▼
┌─────────────────────┐
│ SceneTransition     │
│ GoToGame()          │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Game Scene Loads    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ GameManager.Start() │
│ (if continuing)     │
│ - Set wave          │
│ - Restore lives     │
│ - Equip items       │
└─────────────────────┘
```

---

## 5. Data Structures

### CumulativeStats
```csharp
public struct CumulativeStats {
    public int totalKills;   // Total enemies killed across all runs
    public int highWave;     // Best wave reached
    public int totalGold;    // Total gold collected across all runs
}
```

### ActiveRunData
```csharp
public class ActiveRunData {
    public int wave;         // Current wave
    public int kills;        // Enemies killed this run
    public int gold;         // Gold collected this run
    public int lives;        // Remaining lives
    public string weaponId;  // Equipped weapon item ID
    public string armorId;   // Equipped armor item ID
}
```

### DailyRunResult
```csharp
public class DailyRunResult {
    public long dateTimestamp;      // When the run occurred
    public int waveReached;         // Highest wave
    public int enemiesKilled;       // Total kills
    public int goldCollected;       // Gold earned
    public bool victory;            // Defeated boss?
    public DailyModifier[] modifiers; // Active modifiers
}
```

---

## 6. Manual Save/Load

### From Scripts

```csharp
// Save cumulative stats
SaveLoadManager.Instance.SaveCumulativeStats(
    runKills: 50,
    runWave: 10,
    runGold: 500
);

// Save active run (for Continue)
SaveLoadManager.Instance.SaveActiveRun(
    wave: 5,
    kills: 25,
    gold: 200,
    lives: 2,
    weaponId: "iron_sword",
    armorId: "leather_armor"
);

// Load active run
ActiveRunData run = SaveLoadManager.Instance.LoadActiveRun();
if (run != null) {
    Debug.Log($"Resuming from wave {run.wave}");
}

// Check if can continue
bool canContinue = SaveLoadManager.Instance.HasActiveRun();

// Save weapon mastery
var masteryData = WeaponMasteryManager.Instance.GetMastery("Sword");
SaveLoadManager.Instance.SaveWeaponMastery("Sword", masteryData);

// Load weapon mastery
var loadedMastery = SaveLoadManager.Instance.LoadWeaponMastery("Sword");

// Save options
SaveLoadManager.Instance.SaveOptions(
    musicVolume: 0.8f,
    sfxVolume: 1.0f,
    fullscreen: true
);

// Load options
OptionsData options = SaveLoadManager.Instance.LoadOptions();

// Delete all saves (for testing)
SaveLoadManager.Instance.DeleteAllSaveData();

// Force immediate save
SaveLoadManager.Instance.ForceSave();
```

---

## 7. Testing Checklist

### Cumulative Stats

| # | Test | Expected Result |
|---|------|-----------------|
| 1 | Kill 10 enemies, exit to menu | Total Kills shows 10 |
| 2 | Kill 10 more, exit to menu | Total Kills shows 20 |
| 3 | Reach wave 5 | High Wave shows 5 |
| 4 | Reach wave 3 (lower) | High Wave still shows 5 |
| 5 | Reach wave 7 | High Wave updates to 7 |

### Continue Functionality

| # | Test | Expected Result |
|---|------|-----------------|
| 1 | Start game, reach wave 3, exit | Continue button enabled |
| 2 | Click Continue | Resumes at wave 3 |
| 3 | Die on wave 3 | Continue button disabled |
| 4 | Start new game | Continue uses new run |
| 5 | Exit mid-wave, restart game | Continue available |

### Weapon Mastery

| # | Test | Expected Result |
|---|------|-----------------|
| 1 | Get 10 sword kills, exit | Sword shows Level 1 |
| 2 | Restart game | Sword still Level 1 |
| 3 | Get 15 more kills | Sword shows Level 2 |
| 4 | Check other weapon | Different weapon, Level 0 |

### Daily Run Leaderboard

| # | Test | Expected Result |
|---|------|-----------------|
| 1 | Complete daily run | Result appears in leaderboard |
| 2 | Get wave 5 | Shows #1 Wave 5 |
| 3 | Get wave 3 | Shows #2 Wave 3 |
| 4 | Get wave 7 | Shows #1 Wave 7 (sorted) |
| 5 | Restart game | Leaderboard persists |

### Options

| # | Test | Expected Result |
|---|------|-----------------|
| 1 | Set music to 50% | Music quieter |
| 2 | Exit game, restart | Music still 50% |
| 3 | Toggle fullscreen | Window changes |
| 4 | Restart game | Still fullscreen |

---

## Troubleshooting

### Issue: Stats not saving
**Cause:** SaveLoadManager not in scene
**Fix:** Ensure SaveLoadManager exists in MainMenu scene

### Issue: Continue button always disabled
**Cause:** Active run not being saved on exit
**Fix:** Ensure ReturnToMenu() calls SaveProgress()

### Issue: Mastery not persisting
**Cause:** Mastery data not loaded on menu open
**Fix:** MainMenuController.InitializeMasteryPanel() calls LoadAllWeaponMastery()

### Issue: "KeyNotFoundException"
**Cause:** Trying to load before any saves
**Fix:** SaveLoadManager provides defaults, check for null

---

## Upgrading to JSON/Binary

To upgrade from PlayerPrefs:

1. Create `Assets/Scripts/Managers/JsonSaveLoadManager.cs`
2. Copy interface from SaveLoadManager
3. Replace PlayerPrefs calls with File.WriteAllText/File.ReadAllText
4. Use JsonUtility for serialization
5. Save to Application.persistentDataPath

Template:
```csharp
string path = Application.persistentDataPath + "/save.json";
string json = JsonUtility.ToJson(saveData);
File.WriteAllText(path, json);
```

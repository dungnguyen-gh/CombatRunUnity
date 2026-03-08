# Multi-Scene Setup Guide

**Date:** 2026-03-08  
**Version:** 1.0

---

## 📋 Table of Contents

1. [Overview](#1-overview)
2. [Scene Architecture](#2-scene-architecture)
3. [Scene Setup Order](#3-scene-setup-order)
4. [Creating Each Scene](#4-creating-each-scene)
5. [Persistent Systems](#5-persistent-systems)
6. [Build Settings Configuration](#6-build-settings-configuration)
7. [Code Reference](#7-code-reference)
8. [Testing Checklist](#8-testing-checklist)

---

## 1. Overview

This guide covers converting from single-scene to multi-scene architecture with:

| Scene | Purpose | Contents |
|-------|---------|----------|
| **MainMenu** | Game entry point, mode selection | UI panels only |
| **Loading** | Loading screen with progress/tips | Loading UI only |
| **Game** | Main gameplay | Player, enemies, world |

### Key Benefits
- Clean separation of menu and gameplay
- Proper save/load flow
- Loading screens for better UX
- Easier to add new game modes

---

## 2. Scene Architecture

### Scene Flow

```
Player Opens Game
       │
       ▼
┌─────────────────┐
│   MainMenu      │ ← Menu music, stats display
│   Scene         │
│                 │
│  ┌───────────┐  │
│  │ Start Game│  │ ───────────────────┐
│  │ Daily Run │  │ ───────────────────┤
│  │ Mastery   │  │ (view only)        │
│  │ Options   │  │                    │
│  │ Quit      │  │                    │
│  └───────────┘  │                    │
└─────────────────┘                    │
       │                               │
       │ Click Start/Daily Run         │
       ▼                               │
┌─────────────────┐                    │
│   Loading       │ ← Tips, progress   │
│   Scene         │    bar             │
└─────────────────┘                    │
       │                               │
       │ Scene load complete           │
       ▼                               │
┌─────────────────┐                    │
│   Game          │ ← Spawn player     │
│   Scene         │    Start waves     │
│                 │                    │
│  Player dies?   │                    │
│   ├─ Has lives? │                    │
│   │   ├─ Revive │                    │
│   │   └─ Game   │                    │
│   │      Over   │                    │
│   │             │                    │
│   └─ Game Over  │                    │
│      Panel      │                    │
│      ├─ Retry   │ ───────────────────┤
│      └─ Menu    │ ───────────────────┘
└─────────────────┘
```

### Persistent vs Scene-Specific

| Type | Systems | Lifetime |
|------|---------|----------|
| **Persistent** | UIManager, GameManager, InventoryManager, ShopManager, WeaponMasteryManager, DailyRunManager, SetBonusManager, EnemyPool, SceneTransitionManager | Entire game session |
| **Scene-Specific** | Player, Camera, SpawnPoints, WorldGeometry, EnemyContainer, SkillBarUI | Per scene |

---

## 3. Scene Setup Order

Follow this exact order:

| Step | Task | Time |
|------|------|------|
| 1 | Create MainMenu scene | 30 min |
| 2 | Create Loading scene | 15 min |
| 3 | Rename SampleScene to Game | 5 min |
| 4 | Setup SceneTransitionManager | 15 min |
| 5 | Configure Build Settings | 5 min |
| 6 | Test all transitions | 20 min |

**Total: ~90 minutes**

---

## 4. Creating Each Scene

### Scene 1: MainMenu

#### Step 1: Create Scene
```
File → New Scene
File → Save As → Assets/Scenes/MainMenu.unity
```

#### Step 2: Scene Structure
```
MainMenu (Scene)
├── Managers
│   └── SceneTransitionManager (optional, will be created by STM)
│
├── Canvas (Screen Space - Overlay)
│   └── MainMenuController (Script)
│       ├── MainPanel
│       │   ├── TitleText (TextMeshProUGUI)
│       │   ├── PlayButton
│       │   ├── DailyRunButton
│       │   ├── MasteryButton
│       │   ├── OptionsButton
│       │   ├── CreditsButton
│       │   └── QuitButton
│       │
│       ├── PlayPanel (initially disabled)
│       │   ├── NewGameButton
│       │   ├── ContinueButton
│       │   └── BackButton
│       │
│       ├── DailyRunPanel (initially disabled)
│       │   ├── SeedText
│       │   ├── ModifiersText
│       │   ├── LeaderboardContainer
│       │   ├── StartDailyRunButton
│       │   └── BackButton
│       │
│       ├── MasteryPanel (initially disabled)
│       │   ├── MasteryListContainer
│       │   └── BackButton
│       │
│       ├── OptionsPanel (initially disabled)
│       │   ├── MusicVolumeSlider
│       │   ├── SFXVolumeSlider
│       │   ├── FullscreenToggle
│       │   └── BackButton
│       │
│       └── CreditsPanel (initially disabled)
│           └── BackButton
│
└── EventSystem
```

#### Step 3: Configure MainMenuController

```csharp
// Inspector Settings:

Main Panel:
- Main Panel: [drag MainPanel]
- Play Button: [drag PlayButton]
- Daily Run Button: [drag DailyRunButton]
- Mastery Button: [drag MasteryButton]
- Options Button: [drag OptionsButton]
- Credits Button: [drag CreditsButton]
- Quit Button: [drag QuitButton]

Play Panel:
- New Game Button: [drag NewGameButton]
- Continue Button: [drag ContinueButton]
- Back From Play Button: [drag BackButton]

Daily Run Panel:
- Daily Run Seed Text: [drag SeedText]
- Daily Run Modifiers Text: [drag ModifiersText]
- Daily Run Leaderboard Container: [drag Container]
- Start Daily Run Button: [drag StartButton]
- Back From Daily Run Button: [drag BackButton]

// ... etc for other panels
```

#### Step 4: Stats Display (Optional)

Add to MainPanel:
```
StatsPanel (Horizontal Layout)
├── TotalKillsText (TextMeshProUGUI)
├── HighWaveText (TextMeshProUGUI)
└── TotalGoldText (TextMeshProUGUI)
```

Assign to MainMenuController inspector fields.

---

### Scene 2: Loading

#### Step 1: Create Scene
```
File → New Scene
File → Save As → Assets/Scenes/Loading.unity
```

#### Step 2: Scene Structure
```
Loading (Scene)
├── Canvas (Screen Space - Overlay)
│   └── LoadingSceneController (Script)
│       ├── Background (Image - dark background)
│       ├── TitleText (TextMeshProUGUI) - "Loading..."
│       ├── ProgressBar (Slider)
│       ├── ProgressText (TextMeshProUGUI) - "0%"
│       ├── TipText (TextMeshProUGUI) - rotating tips
│       └── SceneNameText (TextMeshProUGUI) - "Loading Game..."
│
└── EventSystem
```

#### Step 3: Configure LoadingSceneController

```csharp
// Inspector Settings:

UI References:
- Progress Bar: [drag Slider]
- Progress Text: [drag TextMeshPro]
- Loading Tip Text: [drag TextMeshPro]
- Scene Name Text: [drag TextMeshPro]

Tips (array):
- "Tip: Combine skills in sequence to create powerful synergies!"
- "Tip: Freeze enemies first, then shock them for massive damage!"
- "Tip: Complete equipment sets for powerful bonuses!"
- "Tip: Master a weapon type to unlock permanent stat bonuses!"
- "Tip: Use the Daily Run to compete with other players!"
- "Tip: Dodge rolling makes you invulnerable for a short time!"
- "Tip: Burn and Poison effects stack for explosive results!"

Animation:
- Tip Change Interval: 3
- Progress Bar Smoothing: 0.1
```

---

### Scene 3: Game

#### Step 1: Rename Existing Scene
```
Select SampleScene.unity in Project
Right-click → Rename → "Game.unity"
```

#### Step 2: Update Scene Tags

Add these tags (if not exists):
```
Tags & Layers:
  Tags:
    - SpawnPoint
    - BossSpawn
    - EnemyContainer
```

#### Step 3: Scene Structure Update
```
Game (Scene)
├── Managers
│   └── (empty - all persistent managers will auto-create)
│
├── Player
│   └── (existing player setup)
│
├── World
│   ├── SpawnPoints (GameObject)
│   │   ├── SpawnPoint_01 (tag: SpawnPoint)
│   │   ├── SpawnPoint_02 (tag: SpawnPoint)
│   │   └── ...
│   ├── BossSpawn (GameObject, tag: BossSpawn)
│   ├── EnemyContainer (GameObject, tag: EnemyContainer)
│   └── LevelGeometry
│       ├── Walls
│       ├── Ground
│       └── ...
│
├── Canvas
│   └── (existing UI setup)
│
├── Camera
│   └── Main Camera with CameraFollow
│
└── Lighting
    └── (existing lighting)
```

#### Step 4: Verify GameManager References

GameManager will auto-find these on scene load:
- Player (via FindFirstObjectByType)
- SpawnPoints (via FindGameObjectsWithTag("SpawnPoint"))
- BossSpawn (via FindWithTag("BossSpawn"))
- EnemyContainer (via FindWithTag("EnemyContainer"))

---

## 5. Persistent Systems

### SceneTransitionManager Setup

Create in MainMenu scene only (will persist):

```
GameObject: SceneTransitionManager
Components:
  - SceneTransitionManager (Script)
    - Main Menu Scene Name: "MainMenu"
    - Game Scene Name: "Game"
    - Loading Scene Name: "Loading"
    - Use Loading Scene: [✓]
    - Minimum Load Time: 1.5
    - Transition Duration: 0.5
```

This manager:
1. Survives scene loads (DontDestroyOnLoad)
2. Creates other persistent managers on first load
3. Handles all scene transitions with fade + loading
4. Ensures proper cleanup between scenes

### Auto-Created Persistent Managers

SceneTransitionManager will automatically create these if they don't exist:

| Manager | Purpose | Auto-Creates |
|---------|---------|--------------|
| UIManager | UI coordination | ✓ |
| GameManager | Wave/game state | ✓ |
| InventoryManager | Player inventory | ✓ |
| ShopManager | Shop system | ✓ |
| WeaponMasteryManager | Weapon progression | ✓ |
| DailyRunManager | Daily challenges | ✓ |
| SetBonusManager | Equipment sets | ✓ |
| DamageNumberManager | Combat text | ✓ |
| EnemyPool | Enemy pooling | ✓ |

---

## 6. Build Settings Configuration

### Step 1: Open Build Settings
```
File → Build Settings (Ctrl+Shift+B)
```

### Step 2: Add Scenes in Order

Drag scenes to Build Settings in this order:

| Index | Scene | Description |
|-------|-------|-------------|
| 0 | MainMenu | First scene loaded |
| 1 | Loading | Loading screen |
| 2 | Game | Main gameplay |

### Step 3: Verify

Build Settings should show:
```
☑ Scenes/MainMenu.unity
☑ Scenes/Loading.unity
☑ Scenes/Game.unity
```

---

## 7. Code Reference

### Scene Transition Methods

```csharp
// From any script, use SceneTransitionManager:

// Go to main menu
SceneTransitionManager.Instance.GoToMainMenu();

// Start new game
SceneTransitionManager.Instance.GoToGame();

// Start daily run
SceneTransitionManager.Instance.GoToDailyRun();

// Restart current scene
SceneTransitionManager.Instance.RestartCurrentScene();

// Quit game
SceneTransitionManager.Instance.QuitGame();

// Custom scene
SceneTransitionManager.Instance.LoadScene("MyScene");

// With callback
SceneTransitionManager.Instance.LoadScene("Game", () => {
    Debug.Log("Game scene loaded!");
});
```

### Checking Current Scene

```csharp
// Check if in game scene
if (SceneTransitionManager.Instance.IsInGameScene()) {
    // Do game-specific logic
}

// Check if in main menu
if (SceneTransitionManager.Instance.IsInMainMenu()) {
    // Do menu-specific logic
}

// Get current scene name
string sceneName = SceneTransitionManager.Instance.GetCurrentSceneName();
```

### Scene Load Events

```csharp
// Subscribe to transition events
SceneTransitionManager.Instance.OnSceneWillChange += (sceneName) => {
    Debug.Log($"About to load: {sceneName}");
};

SceneTransitionManager.Instance.OnSceneChanged += (sceneName) => {
    Debug.Log($"Loaded: {sceneName}");
};

SceneTransitionManager.Instance.OnLoadingProgress += (progress) => {
    Debug.Log($"Loading: {progress * 100}%");
};
```

---

## 8. Testing Checklist

### Main Menu Tests

- [ ] Game opens to MainMenu scene
- [ ] Title displays correctly
- [ ] All buttons are clickable
- [ ] Stats display (if implemented)
- [ ] Play button opens PlayPanel
- [ ] New Game button starts transition
- [ ] Continue button works (if save exists)
- [ ] Daily Run button shows daily run panel
- [ ] Mastery button shows mastery panel
- [ ] Options button shows options panel
- [ ] Options sliders/toggles work
- [ ] Credits button shows credits
- [ ] Back buttons return to previous panel
- [ ] Quit button exits game

### Loading Scene Tests

- [ ] Loading scene appears during transitions
- [ ] Progress bar fills smoothly
- [ ] Percentage text updates
- [ ] Tips rotate every 3 seconds
- [ ] Minimum load time is respected (1.5s)
- [ ] Scene loads after progress completes

### Game Scene Tests

- [ ] Game scene loads correctly
- [ ] Player spawns at correct position
- [ ] HUD displays properly
- [ ] GameManager starts wave system
- [ ] First wave spawns after delay
- [ ] Pause menu works
- [ ] Game Over panel appears on death
- [ ] Retry button restarts game
- [ ] Main Menu button returns to menu

### Transition Tests

- [ ] MainMenu → Game (via New Game)
- [ ] MainMenu → Game (via Daily Run)
- [ ] Game → MainMenu (via button)
- [ ] Game → Game (Restart)
- [ ] Transitions have fade effect
- [ ] No duplicate managers created
- [ ] TimeScale correct in each scene

### Persistent Data Tests

- [ ] Weapon mastery persists between scenes
- [ ] Inventory persists between scenes
- [ ] Total kills stat accumulates
- [ ] High wave saves correctly
- [ ] Daily run leaderboard persists

### Edge Cases

- [ ] Rapid button clicks don't break transitions
- [ ] Transition during transition is prevented
- [ ] Scene load fails gracefully (invalid scene name)
- [ ] Alt+F4 during transition
- [ ] Mobile back button handling (if applicable)

---

## Troubleshooting

### Issue: Duplicate Managers
**Symptom:** Console warnings about existing singletons
**Fix:** Ensure SceneTransitionManager only exists in first scene. Check for prefabs with managers.

### Issue: Scenes Not Loading
**Symptom:** "Scene not in Build Settings" error
**Fix:** Add scenes to Build Settings (File → Build Settings)

### Issue: Player Not Found
**Symptom:** GameManager can't find player
**Fix:** Ensure Player has PlayerController component and "Player" tag

### Issue: Enemies Not Spawning
**Symptom:** No waves spawn in game scene
**Fix:** Add SpawnPoint tags to spawn point objects

### Issue: UI Not Updating
**Symptom:** HUD shows old data after scene change
**Fix:** UIManager.OnSceneLoaded should re-subscribe to player events

---

## Quick Reference

### Scene Names
```csharp
// SceneTransitionManager defaults
mainMenuSceneName = "MainMenu";
gameSceneName = "Game";
loadingSceneName = "Loading";
```

### Required Tags
```
Player          - For player GameObject
SpawnPoint      - For enemy spawn points
BossSpawn       - For boss spawn point
EnemyContainer  - For enemy parent object
Enemy           - For enemy detection
```

### Required Layers
```
Enemy     - For enemy colliders
Obstacles - For wall collisions (dash)
```

### File Locations
```
Assets/Scripts/Managers/SceneTransitionManager.cs
Assets/Scripts/Managers/MainMenuController.cs
Assets/Scripts/Managers/LoadingSceneController.cs
Assets/Scenes/MainMenu.unity
Assets/Scenes/Loading.unity
Assets/Scenes/Game.unity
```

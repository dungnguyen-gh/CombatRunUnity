# CombatRun Unity 2D Action RPG - Setup Guide

## Table of Contents
1. [Project Overview](#project-overview)
2. [Prerequisites](#prerequisites)
3. [Player Setup](#player-setup)
4. [Managers Setup](#managers-setup)
5. [UI Setup](#ui-setup)
6. [UI Animation Setup (DOTween)](#ui-animation-setup-dotween)
7. [Prefab Creation](#prefab-creation)
8. [ScriptableObject Setup](#scriptableobject-setup)
9. [Scene Setup](#scene-setup)
10. [Common Errors & Solutions](#common-errors--solutions)

---

## Project Overview

**Project Location:** `D:\Unity\UnityProjects\CombatRun`

**Key Systems:**
- Unity Input System Package (NEW Input System)
- SPUM (2D Sprite Character System) for character visuals
- ScriptableObjects for Items, Equipment Sets, and Skills
- Object pooling for damage numbers
- Wave-based enemy spawning
- Equipment set bonuses with special effects
- Skill synergy system

---

## Prerequisites

1. **Unity Version:** Unity 2022.3 LTS or newer (with 2D URP template recommended)
2. **Required Packages:**
   - Unity Input System (com.unity.inputsystem)
   - TextMeshPro (com.unity.textmeshpro)
   - Universal RP (com.unity.render-pipelines.universal) - optional but recommended
3. **SPUM Package:** Must be imported from Unity Asset Store

**Package Installation:**
```
Window → Package Manager → Unity Registry → Search "Input System" → Install
```

**Input System Setup:**
- When prompted to enable the new Input System, click **Yes**
- Unity will restart

---

## Player Setup

### Step 1: Create Player GameObject

1. In Hierarchy, right-click → **Create Empty**
2. Name it: `Player`
3. Set Tag: `Player` (create tag if needed: Tag dropdown → Add Tag)

### Step 2: Add Required Components

Add these components in order:

#### 1. Rigidbody2D
- **Body Type:** `Kinematic`
- **Use Full Kinematic Contacts:** ✓ Checked (optional but recommended)
- **Interpolate:** `Interpolate`
- **Collision Detection:** `Continuous`
- **Constraints:** Freeze Rotation (Z)

#### 2. Add Scripts (Component order matters for dependencies)

**Script 1: SPUMPlayerBridge**
```
Add Component → SPUMPlayerBridge
```
- **SPUM Prefabs:** Leave empty (will auto-find)

**Script 2: SPUMEquipmentManager**
```
Add Component → SPUMEquipmentManager
```
- **SPUM Prefabs:** Leave empty (will auto-find)

**Script 3: PlayerController**
```
Add Component → PlayerController
```
Required references to assign:
| Field | What to Assign |
|-------|----------------|
| Input Actions | Create/Assign Input Action Asset (see below) |
| Enemy Layer | Create "Enemy" layer, assign it |
| Attack Point | Create empty child called "AttackPoint", assign it |

**Script 4: SkillCaster**
```
Add Component → SkillCaster
```
- **Cast Point:** Use Player transform or create "CastPoint" child
- **Enemy Layer:** Same as above
- **Player:** Will auto-find (leave empty)
- **Projectile Prefab:** Create/assign later (see Prefab Creation)
- **Shield Effect Prefab:** Create/assign later (optional)

**Script 5: ComboSystem** (optional but recommended)
```
Add Component → ComboSystem
```

### Step 3: Create Input Action Asset

1. In Project window, right-click → **Create → Input Actions**
2. Name it: `GameControls`
3. Double-click to open Input Action Editor

**Create Action Map: "Gameplay"**

Add these Actions:

| Action Name | Action Type | Binding | Path |
|-------------|-------------|---------|------|
| Move | Value (Vector2) | | |
| | | Up | W / Up Arrow |
| | | Down | S / Down Arrow |
| | | Left | A / Left Arrow |
| | | Right | D / Right Arrow |
| Attack | Button | | Mouse Left Button |
| Skill1 | Button | | 1 (Key) |
| Skill2 | Button | | 2 (Key) |
| Skill3 | Button | | 3 (Key) |
| Skill4 | Button | | 4 (Key) |
| Inventory | Button | | I (Key) |
| Pause | Button | | Escape (Key) |

4. Click **Save Asset** in editor
5. Back on Player, assign `GameControls` to PlayerController's **Input Actions** field

### Step 4: Add SPUM Character

#### Finding SPUM Prefabs:
```
Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/Human/
```

Available character prefabs:
- SPUM_20240911215638389.prefab through SPUM_20240911215640352.prefab

#### Setup Steps:

1. Drag a SPUM prefab into the scene as a **child** of Player
2. Reset its Transform to (0, 0, 0)
3. **IMPORTANT:** The SPUM prefab should NOT have colliders or rigidbodies - remove them if present

4. On the parent Player GameObject, find these scripts and assign:
   - **SPUMPlayerBridge → SPUM Prefabs:** Drag the child SPUM_Prefabs component
   - **SPUMEquipmentManager → SPUM Prefabs:** Drag the child SPUM_Prefabs component
   - **PlayerController → Use SPUM:** ✓ Check the checkbox
   - **PlayerController → SPUM Bridge:** Drag SPUMPlayerBridge component
   - **PlayerController → SPUM Equipment:** Drag SPUMEquipmentManager component

5. On SPUMPlayerBridge, configure animation indices:
```
Idle Animation Index: 0
Move Animation Index: 0
Attack Animation Index: 0
Damaged Animation Index: 0
Debuff Animation Index: 0
Death Animation Index: 0
Skill Animation Indices: [1, 1, 1, 1]  // Customize per skill
```

### Step 5: Create AttackPoint Child

1. Right-click Player → **Create Empty**
2. Name: `AttackPoint`
3. Position: (0, 0.5, 0) - adjust based on your character size
4. Assign to PlayerController's **Attack Point** field

### Step 6: Physics Layers Setup

Go to **Edit → Project Settings → Tags and Layers**

Add these layers:
- **Layer 6:** `Enemy`
- **Layer 7:** `Player` (if not default)

Set collision matrix:
- Player (layer) should collide with Enemy layer
- Enemy should collide with Player layer

---

## Managers Setup

Create an empty GameObject called `Managers` in the scene.

All managers listed below need `DontDestroyOnLoad` - they are singletons that persist across scenes.

### Manager 1: GameManager

1. Create empty child under Managers: `GameManager`
2. Add Component: `GameManager`

**Required References:**
| Field | Assignment |
|-------|------------|
| Spawn Points | Create empty GameObjects around map, assign them |
| Enemy Prefabs | Assign enemy prefabs (see Prefab Creation) |
| Boss Prefab | Assign boss prefab |
| Boss Spawn Point | Create empty GameObject at boss spawn location |
| Player | Will auto-find |
| Enemy Container | Create empty GameObject called "Enemies", assign it |

**Settings:**
```
Time Between Waves: 5
Enemies Per Wave: 5
Max Enemies: 20
Boss Wave: 5
```

### Manager 2: UIManager

1. Create empty child: `UIManager`
2. Add Component: `UIManager`

**Required References:**
| Field | Assignment |
|-------|------------|
| HUD Panel | Create UI panel for HUD (see UI Setup) |
| Health Text | TextMeshProUGUI for HP display |
| Health Slider | UI Slider for HP bar |
| Gold Text | TextMeshProUGUI for gold |
| Skill Icons | Array of 4 UI Images for skill icons |
| Skill Cooldown Overlays | Array of 4 UI Images (filled type) |
| Skill Cooldown Texts | Array of 4 TextMeshProUGUI |
| Inventory Panel | Assign inventory UI panel |
| Shop Panel | Assign shop UI panel |
| Pause Panel | Assign pause menu panel |
| Notification Prefab | Assign NotificationText prefab |
| Notification Parent | RectTransform for notification positioning |
| Player | Will auto-find |
| Skill Caster | Will auto-find from player |

### Manager 3: InventoryManager

1. Create empty child: `InventoryManager`
2. Add Component: `InventoryManager`

**Settings:**
```
Max Inventory Slots: 20
```

**References:**
- Player: Will auto-find

### Manager 4: ShopManager

1. Create empty child: `ShopManager`
2. Add Component: `ShopManager`

**Settings:**
```
Shop Slot Count: 6
Refresh Cost: 50
```

**Rarity Distribution:**
```
Element 0: Common
Element 1: Common
Element 2: Common
Element 3: Uncommon
Element 4: Uncommon
Element 5: Rare
```

### Manager 5: DamageNumberManager

1. Create empty child: `DamageNumberManager`
2. Add Component: `DamageNumberManager`

**Required References:**
| Field | Assignment |
|-------|------------|
| Damage Number Prefab | Create/assign (see Prefab Creation) |

**Settings:**
```
Pool Size: 20
Number Lifetime: 1
Float Speed: 1
Crit Font Size Multiplier: 1.5
```

### Manager 6: SetBonusManager

1. Create empty child: `SetBonusManager`
2. Add Component: `SetBonusManager`

**References:**
- Inventory: Will auto-find
- Player: Will auto-find
- All Sets: Will auto-load from Resources/Sets

### Manager 7: SkillSynergyManager

1. Create empty child: `SkillSynergyManager`
2. Add Component: `SkillSynergyManager`

No manual references needed - auto-finds SkillCaster and Player.

### Manager Hierarchy Summary

```
Managers (Empty parent)
├── GameManager
├── UIManager
├── InventoryManager
├── ShopManager
├── DamageNumberManager
├── SetBonusManager
└── SkillSynergyManager
```

---

## UI Setup

### Step 1: Create Canvas

1. Right-click Hierarchy → **UI → Canvas**
2. **Render Mode:** Screen Space - Overlay
3. **Canvas Scaler:**
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Screen Match Mode: Match Width or Height (0.5)

### Step 2: Create EventSystem

1. Right-click Hierarchy → **UI → Event System**
2. Add Component: `Input System UI Input Module` (replaces Standalone Input Module)
3. Assign your `GameControls` input actions to the module

### Step 3: Create HUD Panel

1. Right-click Canvas → **UI → Panel**
2. Name: `HUDPanel`
3. Anchor: Stretch-Stretch (fill entire screen)
4. Add children:

#### Health Bar
- Create Slider
- Name: `HealthSlider`
- Direction: Left to Right
- Remove Handle Slide Area
- Background: Dark color
- Fill: Red color

#### Health Text
- Create Text - TextMeshPro
- Name: `HealthText`
- Text: "100/100"
- Position over health bar

#### Gold Text
- Create Text - TextMeshPro
- Name: `GoldText`
- Text: "0"
- Position: Top-right corner

#### Skill Bar
- Create Empty GameObject: `SkillBar`
- Add Horizontal Layout Group
- Create 4 children: `Skill1`, `Skill2`, `Skill3`, `Skill4`
- Each Skill child should have:
  - Image (icon background)
  - Child Image named "CooldownOverlay" (fill amount image, dark semi-transparent)
  - Child TextMeshProUGUI named "CooldownText" (for numbers)

### Step 4: Create Inventory Panel

1. Right-click Canvas → **UI → Panel**
2. Name: `InventoryPanel`
3. Add `CanvasGroup` component
4. Set Active: False (hidden by default)
5. Design inventory grid:
   - Create Grid Layout Group
   - Cell Size: 80 x 80
   - Spacing: 10 x 10
   - Create 20 item slots as children

### Step 5: Create Shop Panel

1. Right-click Canvas → **UI → Panel**
2. Name: `ShopPanel`
3. Add `CanvasGroup` component
4. Set Active: False
5. Add:
   - 6 item display slots
   - "Refresh" button
   - "Close" button
   - Gold display

### Step 6: Create Pause Panel

1. Right-click Canvas → **UI → Panel**
2. Name: `PausePanel`
3. Add `CanvasGroup` component
4. Set Active: False
5. Add:
   - "Resume" button
   - "Quit" button

### Step 7: Create Notification Area

1. Create Empty GameObject under Canvas: `Notifications`
2. Add Vertical Layout Group
3. Position: Top-center of screen
4. This is assigned to UIManager's **Notification Parent**

---

## UI Animation Setup (DOTween)

This project uses **DOTween** for smooth, high-performance UI animations. DOTween is ~10x faster than Unity's Animator and creates zero GC allocations.

### Step 1: DOTween Initial Setup

**After importing DOTween from Asset Store:**

1. Go to **Tools → Demigiant → DOTween Utility Panel**
2. Click **"Setup DOTween..."**
3. In the Modules panel, ensure these are enabled:
   - ✓ DOTween (core)
   - ✓ DOTweenModuleUI (required for UI animations)
   - ✓ DOTweenModuleAudio (optional, for audio tweening)
4. Click **"Apply"**

**Verify Setup:**
- Check `Assets/Resources/DOTweenSettings.asset` exists
- No compiler errors should appear

### Step 2: Add UIAnimationManager

**Purpose:** Central manager for all UI animations with preset configurations.

1. Under your `Managers` GameObject, create: `UIAnimationManager`
2. Add Component: `UIAnimationManager`
3. Configure settings:

```
Animation Settings:
  Default Duration: 0.3
  Default Ease: OutCubic
  Bounce Ease: OutBack
  Elastic Ease: OutElastic

Panel Animation:
  Panel Fade In Duration: 0.25
  Panel Fade Out Duration: 0.2
  Panel Scale In Duration: 0.3
  Panel Scale Out Duration: 0.2
  Panel Slide Distance: 500

Button Animation:
  Button Hover Scale: 1.1
  Button Click Scale: 0.9
  Button Animation Duration: 0.15
```

### Step 3: Add UIPanel Component to Panels

**Purpose:** Handles open/close animations for any UI panel.

1. Select your UI panel (e.g., `InventoryPanel`, `ShopPanel`, `PausePanel`)
2. Add Component: `UIPanel`
3. Configure:

```
Panel Settings:
  Panel Id: "Inventory" (unique identifier)
  Pauses Game: ✓ (check if this should pause gameplay)
  Closeable By Escape: ✓ (check if Escape key should close it)
  Start Hidden: ✓ (usually true)

Animation:
  Animation Duration: 0.3
  Animation Type: Scale (or Fade, SlideFromBottom, etc.)
  
DOTween Animation:
  Use DOTween: ✓ (enable for smooth animations)
  Bounce Intensity: 1.2
  Slide Distance: 500

Gamepad Navigation:
  First Selected: [Assign first button for controller navigation]
  Auto Find First Selectable: ✓ (finds automatically if empty)
```

**Animation Types Available:**
| Type | Description | Best For |
|------|-------------|----------|
| Fade | Simple alpha fade | Notifications, toasts |
| Scale | Scale from 0 to 1 with bounce | Dialogs, popups |
| SlideFromBottom | Slide up from bottom | Mobile-style menus |
| SlideFromTop | Slide down from top | Dropdown menus |
| SlideFromLeft | Slide from left | Side panels |
| SlideFromRight | Slide from right | Side panels |

### Step 4: Add AnimatedButton to Buttons

**Purpose:** Automatic hover, press, and click animations.

1. Select a Button GameObject
2. Add Component: `AnimatedButton`
3. Configure:

```
Animation Settings:
  Animate Hover: ✓
  Animate Click: ✓
  Animate Press: ✓

Hover:
  Hover Scale: 1.1
  Hover Duration: 0.15
  Hover Ease: OutBack

Click:
  Click Punch: 0.2
  Click Duration: 0.2

Press:
  Press Scale: 0.9
  Press Duration: 0.1

Colors (Optional):
  Animate Color: ☐ (enable to change colors)
  Hover Color: White
  Pressed Color: Gray
```

### Step 5: Add UIAnimator to Any UI Element

**Purpose:** Generic animator for any UI element (not just buttons).

**Use Cases:**
- Animated icons
- Floating damage numbers
- Pulsing notifications
- Floating menu items

**Setup:**
1. Select any UI GameObject
2. Add Component: `UIAnimator`
3. Configure based on need:

```
Hover Animation:
  Animate Hover: ✓
  Hover Scale: 1.1
  Hover Duration: 0.15

Click Animation:
  Animate Click: ✓
  Click Scale: 0.9
  Click Duration: 0.1

Idle Animation (Optional):
  Animate Idle: ✓
  Idle Type: Pulse (or Float, Shake, Rotate)
  Idle Duration: 1
  Idle Magnitude: 0.1
```

**Idle Animation Types:**
| Type | Effect |
|------|--------|
| Pulse | Scale up/down continuously |
| Float | Move up/down gently |
| Shake | Subtle shake effect |
| Rotate | Gentle rotation |

### Step 6: Scripting UI Animations

**Opening a Panel via Code:**
```csharp
// Get the UIPanel component
UIPanel panel = GetComponent<UIPanel>();

// Open with animation
panel.Open();

// Close with animation
panel.Close();

// Instant show/hide (no animation)
panel.ShowInstant();
panel.HideInstant();
```

**Using UIAnimationManager:**
```csharp
// Animate panel open with scale effect
UIAnimationManager.Instance.AnimatePanelOpen(panelRect, panelCanvasGroup);

// Animate button hover
UIAnimationManager.Instance.AnimateButtonHover(buttonTransform);

// Animate notification
UIAnimationManager.Instance.AnimateNotificationShow(notificationRect);

// Screen shake effect
UIAnimationManager.Instance.ShakeScreen(cameraTransform, 0.5f, 1f);
```

**Custom DOTween Animations:**
```csharp
using DG.Tweening;

// Fade in
myImage.DOFade(1f, 0.3f).SetEase(Ease.OutCubic);

// Scale with bounce
myRect.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack);

// Move to position
myRect.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutCubic);

// Punch effect (wobble)
myRect.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f);

// Shake
myRect.DOShakeAnchorPos(0.5f, 10f, 10, 90);

// Chained sequence
Sequence seq = DOTween.Sequence();
seq.Append(myRect.DOScale(1.5f, 0.2f));
seq.Append(myRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
seq.Play();
```

### Step 7: Common Animation Patterns

**Damage Number Float-Up:**
```csharp
// Float up and fade out
Sequence seq = DOTween.Sequence();
seq.Join(damageRect.DOAnchorPosY(endY, 1f));
seq.Join(damageCanvas.DOFade(0f, 0.5f).SetDelay(0.5f));
seq.OnComplete(() => Destroy(damageRect.gameObject));
```

**Skill Cooldown Pulse:**
```csharp
// Pulsing effect when skill is ready
skillIcon.DOScale(1.1f, 0.5f)
    .SetEase(Ease.InOutSine)
    .SetLoops(-1, LoopType.Yoyo);
```

**Gold Gain Animation:**
```csharp
// Float up gold text
UIAnimationManager.Instance.AnimateGoldGain(goldTextRect);
```

**Critical Hit Effect:**
```csharp
// Scale punch + shake
Sequence seq = DOTween.Sequence();
seq.Append(damageText.DOScale(1.5f, 0.1f));
seq.Append(damageText.DOShakeScale(0.3f, 0.3f));
seq.Append(damageText.DOScale(1f, 0.2f));
```

### DOTween Best Practices

**1. Always Kill Tweens:**
```csharp
// When destroying objects
void OnDestroy() {
    DOTween.Kill(transform);
}

// Before starting new tween on same object
DOTween.Kill(myRect);
myRect.DOScale(2f, 0.3f);
```

**2. Use SetTarget for Cleanup:**
```csharp
myRect.DOScale(2f, 0.3f).SetTarget(this);
// Now DOTween.Kill(this) will kill this tween
```

**3. Scene Transition Cleanup:**
```csharp
// In your scene transition manager
void OnSceneUnload() {
    UIAnimationManager.Instance?.KillAllTweens();
    DOTween.KillAll();
}
```

**4. TimeScale-Independent Animations:**
```csharp
// For pause menu animations when time is paused
myPanel.DOFade(1f, 0.3f).SetUpdate(true);
```

**5. Performance Tips:**
- Reuse sequences instead of creating new ones
- Use `SetAutoKill(false)` for reusable tweens
- Pool animated objects when possible
- Avoid animating too many elements simultaneously

---

## Prefab Creation

### Prefab 1: DamageNumber

**Purpose:** Floating damage numbers when hitting enemies

1. Create Text - TextMeshPro in scene
2. Convert to World Space (for 3D positioning):
   - **Better approach:** Use TextMeshPro (3D) instead of TextMeshProUGUI
   - Create: Right-click → **3D Object → Text - TextMeshPro**

3. Add Component: `DamageNumber` (if you create a custom script) OR just use vanilla TextMeshPro

**Components needed:**
- `Transform` (position at origin)
- `TextMeshPro` (3D version, not UI)
- **Font Size:** 5
- **Alignment:** Center

4. Drag to Project window → `Assets/Prefabs/DamageNumber.prefab`
5. Delete from scene

**Assign to:** DamageNumberManager's **Damage Number Prefab**

### Prefab 2: Enemy

**Purpose:** Base enemy for spawning

1. Create Sprite in scene: Right-click → **2D Object → Sprites → Square**
2. Name: `Enemy`
3. Set Tag: `Enemy`
4. Set Layer: `Enemy`

**Components:**
1. **Rigidbody2D:**
   - Body Type: Kinematic
   - Constraints: Freeze Rotation

2. **BoxCollider2D** or **CircleCollider2D:**
   - Is Trigger: false
   - Adjust size to fit sprite

3. **SpriteRenderer:**
   - Color: Red (or use enemy sprite)
   - Sorting Layer: Characters

4. **Animator:**
   - Create Animator Controller: `EnemyController`
   - Assign parameters: `IsMoving` (bool), `Attack` (trigger), `Hit` (trigger), `Die` (trigger)

5. **Enemy Script:**
   ```
   Add Component → Enemy
   ```

**Enemy Script Settings:**
```
Max Health: 30
Damage: 5
Move Speed: 2
Attack Range: 1
Attack Cooldown: 1
Gold Reward: 5
Item Drop Chance: 0.3
Detection Range: 8
Stop Distance: 0.5
Patrol: false
```

**References to assign:**
- Animator: Assign the Animator component
- Sprite Renderer: Assign the SpriteRenderer
- RB: Will auto-get Rigidbody2D
- Gold Pickup Prefab: Create/assign (see below)
- Item Drop Prefabs: Array of item pickup prefabs

6. Drag to Project window: `Assets/Prefabs/Enemy.prefab`

### Prefab 3: Boss Enemy

Duplicate Enemy prefab, rename to `Boss`, adjust stats:
```
Max Health: 200
Damage: 15
Move Speed: 1.5
Scale: 1.5x larger
Gold Reward: 100
```

### Prefab 4: GoldPickup

1. Create Sprite: Circle
2. Name: `GoldPickup`
3. Add `CircleCollider2D` - Is Trigger: **true**

4. Add Script: `GoldPickup`

5. Add SpriteRenderer:
   - Sprite: Gold coin sprite (or yellow circle)
   - Sorting Layer: Items

6. Drag to Project: `Assets/Prefabs/GoldPickup.prefab`

### Prefab 5: ItemPickup

1. Create Sprite: Square
2. Name: `ItemPickup`
3. Add `BoxCollider2D` - Is Trigger: **true**

4. Add Script: `ItemPickup`
5. Add SpriteRenderer for item visual

6. Drag to Project: `Assets/Prefabs/ItemPickup.prefab`

### Prefab 6: Projectile

1. Create Sprite: Circle or Arrow sprite
2. Name: `Projectile`
3. Set Scale: (0.3, 0.3, 0.3)

**Components:**
1. **Rigidbody2D:**
   - Body Type: Dynamic
   - Gravity Scale: 0
   - Constraints: Freeze Rotation

2. **CircleCollider2D:**
   - Is Trigger: **true**
   - Radius: 0.15

3. **Projectile Script:**
   ```
   Add Component → Projectile
   ```

4. **SpriteRenderer:**
   - Color: Yellow/Orange (fireball look)
   - Sorting Layer: Projectiles

5. Drag to Project: `Assets/Prefabs/Projectile.prefab`

**Assign to:** SkillCaster's **Projectile Prefab**

### Prefab 7: ShieldEffect (Optional)

1. Create Particle System or Sprite
2. Add as child of player position
3. Disable by default
4. Drag to Project

### Prefab 8: SkillEffect Prefabs

Create visual effect prefabs for each skill type:
- CircleAOE: Explosion effect
- GroundAOE: Ground crack/mark effect
- Shield: Bubble/shield visual

Assign these to respective SkillSO assets.

---

## ScriptableObject Setup

### Folder Structure Setup

Create folders in Assets:
```
Assets/
└── Resources/
    ├── Items/
    ├── Sets/
    └── Skills/
```

### Creating ItemSO Assets

1. Right-click Project window → **Create → ARPG → Item**
2. Name it: `IronSword`

**Configure:**
```
Basic Info:
  Item Id: iron_sword_001
  Item Name: Iron Sword
  Description: A basic iron sword.
  Icon: [Assign sword icon sprite]
  Rarity: Common
  Slot: Weapon
  Price: 50

Stats:
  Damage Bonus: 5
  Defense Bonus: 0
  Crit Bonus: 0.05
  Attack Speed Bonus: 0
  Max HP Bonus: 0

Weapon Type: Sword

Visual:
  Item Sprite: [Assign equipped weapon sprite]

Set Bonus:
  Set Id: (leave empty or add to set)
```

**Example Items to Create:**

| Item Name | Slot | Rarity | Damage | Defense | Set ID |
|-----------|------|--------|--------|---------|--------|
| Iron Sword | Weapon | Common | 5 | 0 | - |
| Steel Sword | Weapon | Uncommon | 10 | 0 | - |
| Leather Armor | Armor | Common | 0 | 3 | - |
| Steel Armor | Armor | Uncommon | 0 | 6 | - |
| Crit Ring | Weapon | Rare | 3 | 0 | - |
| Dragon Slayer Sword | Weapon | Epic | 20 | 0 | dragon_slayer |
| Dragon Slayer Plate | Armor | Epic | 0 | 15 | dragon_slayer |

### Creating EquipmentSetSO Assets

1. Right-click → **Create → ARPG → EquipmentSet**
2. Name it: `DragonSlayerSet`

**Configure:**
```
Set Info:
  Set Id: dragon_slayer
  Set Name: Dragon Slayer's Armament
  Description: Worn by those who hunt the mightiest beasts.
  Set Icon: [Assign icon]
  Set Color: Orange/Red

Set Pieces (Item IDs):
  Element 0: dragon_slayer_sword
  Element 1: dragon_slayer_plate

2-Piece Bonus:
  Has 2 Piece Bonus: ✓
  Damage Bonus 2: 15
  Defense Bonus 2: 5
  Crit Bonus 2: 0.1
  Attack Speed Bonus 2: 0
  Max HP Bonus 2: 50
  Bonus Description 2: +15 Damage, +5 Defense, +10% Crit

4-Piece Bonus:
  Has 4 Piece Bonus: ✓ (if 4 items exist)
  Special Effect 4: LifeSteal
  Bonus Description 4: Attacks heal for 10% of damage
```

**Special Effects Available:**
- None
- LifeSteal (heal on hit)
- BurnOnHit (apply burn status)
- DoubleGold (2x gold drops)
- ShieldOnHit (chance for shield)
- CriticalOverload (crits deal AOE)
- VampireTouch (damage heals)

### Creating SkillSO Assets

1. Right-click → **Create → ARPG → Skill**
2. Name it: `Fireball`

**Configure:**
```
Basic Info:
  Skill Id: fireball
  Skill Name: Fireball
  Description: Hurls a ball of fire.
  Icon: [Assign skill icon]
  Skill Slot: 2 (0-3 for keys 1-4)

Cooldown:
  Cooldown Time: 5

Skill Type & Stats:
  Skill Type: Projectile
  Damage Multiplier: 1.5
  Range: 10
  Radius: 0 (not used for projectile)
  Duration: 0
  Effect Prefab: [Assign explosion effect prefab]
  Cast Sound: [Assign audio clip]
```

**Skill Types:**
- **CircleAOE:** Damage around player
- **GroundAOE:** Targeted area damage
- **Projectile:** Fires a projectile
- **Shield:** Damage reduction buff

**Example Skills:**

| Skill Name | Type | Slot | Cooldown | Damage Mult | Range |
|------------|------|------|----------|-------------|-------|
| Fireball | Projectile | 2 | 5 | 1.5 | 10 |
| Meteor Strike | GroundAOE | 1 | 8 | 2.0 | 15 |
| Spin Attack | CircleAOE | 0 | 6 | 1.2 | 3 |
| Shield Wall | Shield | 3 | 12 | - | - |

**Assign Skills to Player:**
1. Select Player in scene
2. Find SkillCaster component
3. Set Skills array size: 4
4. Drag SkillSO assets into slots 0-3

---

## Scene Setup

### Step 1: Camera Setup

1. Select Main Camera
2. Position: (0, 0, -10)
3. Add Component: `CameraFollow`

**CameraFollow Settings:**
```
Target: None (will auto-find Player)
Smooth Speed: 0.125
Offset: (0, 0, -10)
Use Bounds: true (optional)
Min Bounds: (-20, -20)
Max Bounds: (20, 20)
```

4. **Camera Component Settings:**
   - Projection: Orthographic
   - Size: 5 (adjust based on your level size)
   - Background: Dark color
   - Clear Flags: Solid Color

### Step 2: Create Level Boundaries

1. Create 4 empty GameObjects as walls
2. Add BoxCollider2D to each
3. Position around playable area
4. Tag: `Wall` (optional)

### Step 3: Create Spawn Points

1. Create empty GameObjects: `SpawnPoint1`, `SpawnPoint2`, etc.
2. Position around map edges
3. Assign all to GameManager's **Spawn Points** array

### Step 4: Create Boss Spawn Point

1. Create empty GameObject: `BossSpawnPoint`
2. Position at boss arena location
3. Assign to GameManager's **Boss Spawn Point**

### Step 5: Create Enemy Container

1. Create empty GameObject: `Enemies`
2. This keeps spawned enemies organized
3. Assign to GameManager's **Enemy Container**

### Step 6: Lighting (Optional)

1. Create Global Light 2D
2. Color: White
3. Intensity: 1

### Step 7: Sorting Layers

Go to **Edit → Project Settings → Tags and Layers → Sorting Layers**

Add in order (back to front):
1. Background
2. Ground
3. Items
4. Characters
5. Projectiles
6. Effects
7. UI

Assign sprites to appropriate layers.

---

## Common Errors & Solutions

### Error: "No Input Action Asset assigned!"

**Cause:** PlayerController's inputActions field is null

**Fix:**
1. Create Input Action Asset (GameControls)
2. Configure with "Gameplay" action map
3. Assign to PlayerController

### Error: "Could not find 'Gameplay' action map"

**Cause:** Action map name mismatch

**Fix:**
- Ensure action map is named exactly "Gameplay"
- Check for typos or extra spaces

### Error: "SPUM_Prefabs not found"

**Cause:** SPUM character not set up as child

**Fix:**
1. Add SPUM prefab as child of Player
2. Assign SPUM_Prefabs component to SPUMPlayerBridge and SPUMEquipmentManager
3. Check "Use SPUM" on PlayerController

### Error: "No main camera found"

**Cause:** Camera doesn't have "MainCamera" tag

**Fix:**
- Select Camera
- Tag dropdown → "MainCamera"

### Error: "Projectile prefab missing Projectile component"

**Cause:** Projectile prefab doesn't have the Projectile script

**Fix:**
- Ensure Projectile script is on the prefab
- Re-create prefab if needed

### Error: "DamageNumberManager: No prefab assigned"

**Cause:** DamageNumberManager's prefab field is empty

**Fix:**
- Create DamageNumber prefab
- Assign to DamageNumberManager

### Error: Skills not casting

**Possible Causes:**
1. SkillSO not assigned in SkillCaster
2. Cooldown not reset (check console for cooldown messages)
3. Input actions not bound properly

**Fix:**
- Verify SkillCaster.skills array has SkillSO assets
- Check that input actions use correct action names (Skill1, Skill2, etc.)
- Ensure SkillCaster has projectile prefab assigned for projectile skills

### Error: "Player or stats is null" when casting skills

**Cause:** SkillCaster.player is null

**Fix:**
- SkillCaster should be on same GameObject as PlayerController
- Or manually assign PlayerController to SkillCaster.player field

### Error: Enemies not spawning

**Possible Causes:**
1. GameManager.spawnPoints is empty
2. GameManager.enemyPrefabs is empty
3. Enemy prefab missing Enemy script

**Fix:**
- Create spawn point GameObjects and assign them
- Assign enemy prefabs to array
- Verify Enemy script is on prefab

### Error: UI not updating

**Possible Causes:**
1. UIManager.player not assigned
2. Player events not subscribed

**Fix:**
- UIManager auto-finds player on Start
- Ensure Player is tagged "Player"
- Check that PlayerController events are being invoked

### Error: "Inventory full!" when picking up items

**Cause:** InventoryManager.maxInventorySlots reached

**Fix:**
- Increase maxInventorySlots
- Or remove items from inventory

### Error: Set bonuses not applying

**Possible Causes:**
1. SetBonusManager not in scene
2. Items don't have matching setId
3. SetSO not in Resources/Sets folder

**Fix:**
- Ensure SetBonusManager exists
- Check setId matches between ItemSO and EquipmentSetSO
- Move SetSO to Resources/Sets folder

### Error: Animations not playing (SPUM)

**Possible Causes:**
1. SPUMPlayerBridge.spumPrefabs not assigned
2. Animation lists empty in SPUM_Prefabs

**Fix:**
- Assign SPUM_Prefabs to bridge
- Select SPUM prefab, click "Populate Animation Lists" in context menu

### Error: Input not working in build

**Cause:** Input System backend not configured

**Fix:**
```
Edit → Project Settings → Player → Configuration
Input System Package: Both or Input System Package (New)
```

### Error: "FindObjectOfType is obsolete"

**Cause:** Using old Unity API

**Fix:** The scripts use `FindFirstObjectByType` which is correct for Unity 2023+
For older versions, replace with `FindObjectOfType`

### Error: "The type or namespace name 'DG' could not be found"

**Cause:** DOTween not imported or not set up

**Fix:**
1. Import DOTween from Asset Store
2. Run **Tools → Demigiant → DOTween Utility Panel → Setup DOTween**
3. Wait for Unity to compile

### Error: "DOFade/DOScale/etc is not a member of..."

**Cause:** Missing DOTween namespace or module

**Fix:**
1. Add `using DG.Tweening;` at top of script
2. Ensure DOTweenModuleUI is enabled in DOTween Utility Panel
3. Target must be RectTransform, CanvasGroup, or Graphic

### Error: UI animations not playing / jerky

**Possible Causes:**
1. `useDOTween` not checked on UIPanel
2. Conflicting animations running
3. DOTween not initialized

**Fix:**
- Check "Use DOTween" on UIPanel component
- Kill existing tweens: `DOTween.Kill(transform)` before new animation
- Ensure UIAnimationManager exists in scene (calls DOTween.Init())

### Error: "Tween was killed while playing"

**Cause:** Object destroyed while tweening

**Fix:**
```csharp
// Kill tweens before destroying
void OnDestroy() {
    DOTween.Kill(transform);
}
```

### Error: Animations continue after scene change

**Cause:** Tweens not cleaned up between scenes

**Fix:**
```csharp
// In scene loading code
DOTween.KillAll();
// or
UIAnimationManager.Instance.KillAllTweens();
```

---

## Quick Reference Checklist

### Before First Play:

- [ ] Player has Rigidbody2D (Kinematic)
- [ ] Player has PlayerController with Input Actions assigned
- [ ] Player has SkillCaster with 4 skills assigned
- [ ] Player has SPUMPlayerBridge (if using SPUM)
- [ ] Player has SPUMEquipmentManager (if using SPUM)
- [ ] SPUM prefab is child of Player (if using SPUM)
- [ ] All Managers are in scene with DontDestroyOnLoad
- [ ] UIManager has all UI panels assigned
- [ ] DamageNumberManager has prefab assigned
- [ ] GameManager has spawn points and enemy prefabs
- [ ] Camera has "MainCamera" tag
- [ ] Player has "Player" tag
- [ ] Enemy prefabs have "Enemy" tag and layer
- [ ] EventSystem uses Input System UI Input Module
- [ ] All ScriptableObjects are in Resources folders

### Testing Order:

1. Test movement (WASD/Arrow keys)
2. Test attack (Left Click)
3. Test skills (1-4 keys)
4. Test inventory (I key)
5. Test pause (Escape key)
6. Verify enemies spawn
7. Verify damage numbers appear
8. Verify gold pickup works
9. Test equipment equipping
10. Test shop (if implemented)

---

## Additional Notes

### Performance Tips:
- DamageNumberManager uses object pooling (20 objects default)
- SetBonusManager caches items by rarity
- CameraFollow limits FindGameObjectWithTag calls
- SPUMEquipmentManager caches SpriteRenderer references

### Extension Points:
- Add more skill types in SkillType enum
- Add more set special effects in SetSpecialEffect enum
- Add status effects via StatusEffect component
- Add weapon mastery via WeaponMasteryManager
- Add daily run system via DailyRunManager

### Save/Load Support:
PlayerStats class is marked [System.Serializable] for easy JSON serialization. Equipment and inventory can be saved by storing itemIds.

---

**Setup Complete!** Run the scene and test all systems.

# Combat System Setup Guide

This guide covers the complete setup of the Combat System including Combo System, Status Effects, Damage Numbers, and Player Stats integration.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Combo System Setup](#2-combo-system-setup)
3. [Status Effect System Setup](#3-status-effect-system-setup)
4. [Damage Numbers Setup](#4-damage-numbers-setup)
5. [Player Stats Integration](#5-player-stats-integration)
6. [Known Issues & Fixes](#6-known-issues--fixes)
7. [Testing Checklist](#7-testing-checklist)
8. [Visual Prefab Requirements](#8-visual-prefab-requirements)

---

## 1. Overview

The Combat System consists of four interconnected components:

| Component | File | Purpose |
|-----------|------|---------|
| **ComboSystem** | `ComboSystem.cs` | Tracks consecutive hits with damage/attack speed bonuses and finisher attacks |
| **StatusEffect** | `StatusEffect.cs` | Manages elemental effects (Burn, Freeze, Poison, Shock, Bleed) and reactions |
| **DamageNumberManager** | `DamageNumberManager.cs` | Object pool for floating damage text with crit support |
| **PlayerStats** | `PlayerStats.cs` | Serializable stats with equipment modifier system |

### Key Features

- **Combo System**: Progressive damage bonuses (up to 5x), finisher attacks at max combo
- **Status Effects**: 5 elemental types with priority-based overriding and elemental reactions
- **Damage Numbers**: Pooled UI elements to prevent GC pressure
- **Stats System**: Base stats + modifiers from equipment

---

## 2. Combo System Setup

### 2.1 Adding ComboSystem to Player

The `ComboSystem` requires a `PlayerController` component on the same GameObject.

**Setup Steps:**

1. Select your Player GameObject in the Hierarchy
2. Ensure it has a `PlayerController` component attached
3. Add the `ComboSystem` component:
   - **Component > Scripts > Combat > Combo System**

### 2.2 Configuration

Configure the following settings in the Inspector:

| Setting | Default | Description |
|---------|---------|-------------|
| `comboWindow` | 2.0 | Time (seconds) between attacks to maintain combo |
| `maxCombo` | 5 | Maximum combo count cap |
| `damageBonusPerCombo` | 0.2 | Damage multiplier added per combo level (20%) |
| `attackSpeedBonusPerCombo` | 0.1 | Attack speed bonus per combo level (10%) |
| `comboEffectPrefab` | null | Visual effect prefab for combo milestones |
| `comboTextOffset` | (0, 1.5, 0) | World offset for combo text spawn |

**Finisher Settings:**

| Setting | Default | Description |
|---------|---------|-------------|
| `finisherComboRequirement` | 5 | Combo count needed to unlock finisher |
| `finisherDamageMultiplier` | 3.0 | Damage multiplier for finisher attack |
| `finisherKnockback` | 5.0 | Knockback force applied to enemies |
| `finisherEffect` | null | Visual effect prefab for finisher execution |

### 2.3 Finisher Mechanics

The finisher system works as follows:

1. When combo reaches `finisherComboRequirement` (default: 5), `canFinisher` becomes true
2. The `OnFinisherReady` event fires (use for UI notifications)
3. Call `RegisterHit(true)` to execute the finisher
4. The finisher deals AOE damage in `meleeRange * 1.5` radius
5. All enemies hit receive damage and knockback
6. Combo resets after finisher execution

**Example Integration Code:**

```csharp
// In your attack script
public class PlayerAttack : MonoBehaviour {
    private ComboSystem comboSystem;
    
    void Awake() {
        comboSystem = GetComponent<ComboSystem>();
    }
    
    void Update() {
        // Normal attack
        if (Input.GetButtonDown("Fire1") && !Input.GetKey(KeyCode.LeftShift)) {
            PerformAttack();
            comboSystem.RegisterHit(false); // Regular hit
        }
        
        // Finisher attack (Hold Shift + Attack)
        if (Input.GetButtonDown("Fire1") && Input.GetKey(KeyCode.LeftShift)) {
            if (comboSystem.IsFinisherReady()) {
                comboSystem.RegisterHit(true); // Execute finisher
            }
        }
    }
    
    void PerformAttack() {
        // Apply combo damage bonus
        float damageBonus = comboSystem.GetCurrentDamageBonus();
        float attackSpeedBonus = comboSystem.GetCurrentAttackSpeedBonus();
        
        int finalDamage = Mathf.RoundToInt(baseDamage * damageBonus);
        // Apply damage to enemy...
    }
}
```

### 2.4 Visual Effects Setup

**Combo Effect Prefab Requirements:**

The `comboEffectPrefab` should contain:
- A child GameObject with `TextMeshPro` component
- The script automatically sets text to `x{comboCount}` and applies color

**Color Progression:**

| Combo | Color |
|-------|-------|
| x1 | White |
| x2 | Yellow |
| x3 | Orange (1, 0.5, 0) |
| x4 | Red |
| x5 | Purple (0.8, 0, 1) |

**Finisher Effect Prefab:**
- Particle system for impact visual
- Optional: Screen shake, camera effects
- Destroyed automatically after particles finish

---

## 3. Status Effect System Setup

### 3.1 Adding StatusEffect to Enemy Prefabs

Every enemy that can receive status effects needs the `StatusEffect` component.

**Setup Steps:**

1. Select your Enemy prefab
2. Add the `StatusEffect` component:
   - **Component > Scripts > Combat > Status Effect**
3. Assign the `SpriteRenderer` field (auto-detected if on same object)
4. Assign effect prefabs (see section 3.2)

### 3.2 Required Effect Prefabs

Assign prefabs to the following fields in the Inspector:

| Field | Effect Type | Description |
|-------|-------------|-------------|
| `burnEffect` | Particle System | Fire/embers effect |
| `freezeEffect` | Particle System | Ice crystals/frost |
| `poisonEffect` | Particle System | Green gas/bubbles |
| `shockEffect` | Particle System | Electricity/sparks |
| `bleedEffect` | Particle System | Blood particles |

### 3.3 StatusEffectData Structure

Create status effects using the `StatusEffectData` class:

```csharp
// Example: Creating a Burn effect
StatusEffectData burnData = new StatusEffectData {
    type = StatusType.Burn,
    duration = 5f,
    tickRate = 1f,           // Damage every 1 second
    damagePerTick = 5,
    effectColor = new Color(1f, 0.3f, 0f), // Orange-red
    visualEffect = burnEffectPrefab
};

// Apply to enemy
enemyStatusEffect.ApplyStatus(burnData);
```

**Status Effect Presets:**

| Type | Duration | Tick Rate | Damage/Tick | Slow % | Color |
|------|----------|-----------|-------------|--------|-------|
| Burn | 5s | 1s | 5 | 0% | Orange-Red |
| Freeze | 3s | - | 0 | 100% | Cyan |
| Poison | 8s | 2s | 3 | 20% | Green |
| Shock | 2s | - | 0 | 0% | Yellow |
| Bleed | 6s | 1.5s | 4 | 10% | Dark Red |

### 3.4 Priority System Explanation

Status effects have a priority hierarchy for overriding:

```
Priority: Freeze (5) > Shock (4) > Burn (3) > Poison (2) > Bleed (1)
```

**Rules:**
- Higher priority effects replace lower priority ones
- Equal priority: new effect replaces old
- `StatusType.None` has priority 0 (can be overridden by anything)

**Example:**
- Enemy is Poisoned (priority 2)
- Applying Burn (priority 3) → Replaces Poison
- Applying Freeze (priority 5) → Replaces Burn
- Applying Poison (priority 2) → Blocked by Freeze

### 3.5 Elemental Reactions

The system includes two elemental reactions:

#### Explosion (Burn + Poison)

When an enemy with `Burn` receives `Poison` (or vice versa):
- Triggers AOE explosion (3 unit radius)
- Deals 50 damage to all nearby enemies
- Shows "EXPLOSION!" notification
- Clears the status

#### Shatter (Freeze + Shock)

When an enemy with `Freeze` receives `Shock`:
- Deals 25% of enemy's max HP as damage
- Shows "SHATTER!" notification  
- Clears the status

**To trigger reactions in your weapon/item scripts:**

```csharp
// In your weapon's OnHit method
public void ApplyElementalEffect(StatusEffect enemyStatus, StatusType elementType) {
    // Check for elemental reaction first
    bool reacted = enemyStatus.TryElementalReaction(elementType);
    
    if (!reacted) {
        // No reaction - apply normal status
        StatusEffectData data = GetEffectData(elementType);
        enemyStatus.ApplyStatus(data);
    }
}
```

---

## 4. Damage Numbers Setup

### 4.1 Setting up DamageNumberManager

The `DamageNumberManager` is a singleton that persists across scenes.

**Setup Steps:**

1. Create an empty GameObject named "DamageNumberManager"
2. Add the `DamageNumberManager` component
3. Assign the `damageNumberPrefab` (see section 4.2)
4. Configure pool settings (optional)

**Configuration:**

| Setting | Default | Description |
|---------|---------|-------------|
| `poolSize` | 50 | Initial pool capacity |
| `maxPoolSize` | 200 | Maximum pool size |
| `allowPoolExpansion` | true | Allow dynamic pool growth |
| `numberLifetime` | 1.0 | Seconds before returning to pool |
| `floatSpeed` | 1.0 | Upward movement speed |
| `critFontSizeMultiplier` | 1.5 | Size increase for critical hits |

### 4.2 Creating Damage Number Prefab

Create a prefab with the following structure:

```
DamageNumber (GameObject)
├── TextMeshPro component
│   - Font Asset: Any readable font
│   - Font Size: 6
│   - Alignment: Center
│   - Color: White (default)
```

**Required Components:**
- `TextMeshPro` (from Unity's TextMeshPro package)

**Recommended Settings:**
- Sorting Layer: "UI" or "Effects"
- Order in Layer: 100 (above enemies)

### 4.3 Pool Configuration

The pool automatically:
- Pre-instantiates objects on Awake
- Expands by 10 objects when exhausted (if `allowPoolExpansion` is true)
- Caches TextMeshPro references for performance
- Resets font size when returning to pool

**Usage Example:**

```csharp
// In your damage dealing code
public void DealDamage(int damage, Vector3 position, bool isCritical) {
    enemy.TakeDamage(damage);
    
    // Show damage number
    if (DamageNumberManager.Instance != null) {
        DamageNumberManager.Instance.ShowDamage(damage, position, isCritical);
    }
}
```

---

## 5. Player Stats Integration

### 5.1 Stat Modification Flow

The `PlayerStats` class uses a base + modifier system:

```
Final Stat = Base Stat + Equipment Modifier
```

**Stat Properties:**

| Property | Base Field | Modifier Field | Min Value |
|----------|------------|----------------|-----------|
| MaxHP | `baseMaxHP` | `maxHPMod` | - |
| Damage | `baseDamage` | `damageMod` | 1 |
| Defense | `baseDefense` | `defenseMod` | 0 |
| Crit | `baseCrit` | `critMod` | 0-1 |
| AttackSpeed | `baseAttackSpeed` | `attackSpeedMod` | 0.1 |

### 5.2 Equipment Bonus Application

**When Equipping Items:**

```csharp
// In your equipment system
public void EquipItem(ItemSO item, PlayerStats stats) {
    stats.ApplyItem(item);
    // Item is now contributing to stats
}

public void UnequipItem(ItemSO item, PlayerStats stats) {
    stats.RemoveItem(item);
    // Item bonuses removed
}
```

**When Changing Equipment (Full Recalculation):**

```csharp
// Recalculate all modifiers
public void RecalculateStats(PlayerStats stats, List<ItemSO> equippedItems) {
    stats.ResetMods(); // Clear all modifiers
    
    foreach (var item in equippedItems) {
        stats.ApplyItem(item);
    }
}
```

**ItemSO Required Fields:**

```csharp
public class ItemSO : ScriptableObject {
    public int damageBonus;
    public int defenseBonus;
    public float critBonus;
    public float attackSpeedBonus;
    public int maxHPBonus;
}
```

**Integration with Combo System:**

```csharp
// In ComboSystem.cs (already implemented)
int damage = Mathf.RoundToInt(
    player.stats.Damage * finisherDamageMultiplier * (1 + currentCombo * damageBonusPerCombo)
);
```

---

## 6. Known Issues & Fixes

### 6.1 Elemental Reactions Not Triggered

**Issue:** The `TryElementalReaction` method exists but is never called in the default implementation.

**Fix:** Modify your damage/weapon system to check for reactions before applying status:

```csharp
// Add this method to your weapon or projectile script
public void ApplyStatusWithReaction(Enemy target, StatusType newStatus, StatusEffectData data) {
    StatusEffect statusEffect = target.GetComponent<StatusEffect>();
    if (statusEffect == null) return;
    
    // Try elemental reaction first
    bool reactionOccurred = statusEffect.TryElementalReaction(newStatus);
    
    if (!reactionOccurred) {
        // No reaction - apply status normally
        statusEffect.ApplyStatus(data);
    }
}
```

### 6.2 Stun Not Fully Implemented

**Issue:** The Shock status has a `ShockStun` coroutine, but stun only affects movement speed. There's no attack interruption.

**Current Behavior:**
- Shock sets `moveSpeed = 0` for 0.5 seconds
- Enemy can still attack

**Recommended Enhancement:**

```csharp
// Add to StatusEffect.cs
private bool isStunned = false;

// Add to Enemy.cs
public bool IsStunned => statusEffect.IsCurrentlyStunned;

// In Enemy attack code
void Update() {
    if (IsStunned) return; // Skip attack logic when stunned
    // ... normal attack code
}
```

### 6.3 Missing Bleed Effect Prefab

**Issue:** The `Bleed` status type exists but `bleedEffect` field may be unassigned.

**Fix:** Create a bleed particle effect prefab:

1. Create a new Particle System
2. Configure for blood effect:
   - Start Color: Dark Red (0.5, 0, 0, 1)
   - Start Size: 0.1 - 0.3
   - Emission: 10-20 particles/second
   - Shape: Hemisphere (emit from enemy surface)
   - Velocity: Slight downward drift
   - Lifetime: 0.5 - 1.0 seconds

3. Save as prefab in `Prefabs/Effects/BleedEffect.prefab`
4. Assign to all enemy StatusEffect components

---

## 7. Testing Checklist

### Combo System Tests

- [ ] Hit enemy multiple times - combo counter increases
- [ ] Wait 2+ seconds without hitting - combo resets
- [ ] Reach 5 combo - "FINISHER READY" notification appears
- [ ] Hold attack at 5 combo - finisher executes
- [ ] Finisher hits multiple enemies in range
- [ ] Finisher applies knockback to enemies
- [ ] Damage increases with combo level (check 20% per level)
- [ ] Combo colors change correctly (White → Yellow → Orange → Red → Purple)

### Status Effect Tests

- [ ] Burn applies damage over time (1 tick/second)
- [ ] Freeze stops enemy movement
- [ ] Poison applies damage every 2 seconds
- [ ] Shock stuns enemy for 0.5 seconds
- [ ] Bleed applies damage every 1.5 seconds
- [ ] Higher priority effects override lower priority
- [ ] Burn + Poison = Explosion reaction
- [ ] Freeze + Shock = Shatter reaction
- [ ] Effect clears when duration expires
- [ ] Visual effects spawn and destroy correctly

### Damage Number Tests

- [ ] Damage numbers appear on enemy hit
- [ ] Critical hits show larger/yellow numbers
- [ ] Numbers float upward and fade
- [ ] Pool reuses objects (no memory growth)
- [ ] Multiple simultaneous hits show all numbers
- [ ] Status tick damage shows numbers

### Player Stats Tests

- [ ] Base stats display correctly
- [ ] Equipping items increases stats
- [ ] Unequipping items decreases stats
- [ ] Damage never goes below 1
- [ ] Attack speed never goes below 0.1
- [ ] Defense never goes below 0
- [ ] Crit chance stays 0-1 range
- [ ] HP updates when MaxHP changes

---

## 8. Visual Prefab Requirements

### Combo Effect Prefab

**Structure:**
```
ComboEffect
├── TextMeshPro (child)
│   - Font: Bold, readable
│   - Font Size: 8
│   - Alignment: Center Middle
```

**Requirements:**
- Must have `TextMeshPro` in children (script searches with `GetComponentInChildren`)
- Auto-destroy after 1 second (script handles this)

### Finisher Effect Prefab

**Structure:**
```
FinisherEffect
├── Particle System (impact burst)
├── Particle System (shockwave ring)
└── Optional: Screen Shake script
```

**Requirements:**
- One-shot particle burst (not looping)
- Large enough to be visible (AOE indicator)
- Auto-destroy or manual cleanup

### Status Effect Prefabs

**Burn Effect:**
- Emission: 15-20 particles/sec
- Color: Orange to Yellow gradient
- Shape: Box (cover enemy)
- Add light flicker for intensity

**Freeze Effect:**
- Emission: Low (5-10 particles/sec)
- Color: Light Blue/Cyan
- Shape: Hemisphere (top of enemy)
- Add frost overlay sprite (optional)

**Poison Effect:**
- Emission: 10 particles/sec
- Color: Green to Dark Green
- Shape: Cone (rising from ground)
- Bubbling/gas visual

**Shock Effect:**
- Emission: Bursts (use texture sheet for lightning)
- Color: Yellow/White
- Duration: Short flashes
- Add screen shake on apply

**Bleed Effect:**
- Emission: Blood drops falling
- Color: Dark Red
- Shape: Hemisphere (enemy surface)
- Add blood splatter decal (optional)

### Damage Number Prefab

**Requirements:**
- `TextMeshPro` component on root
- World Space canvas or regular GameObject
- High sort order (renders above enemies)
- No colliders or rigidbodies needed

---

## Quick Reference

### Common Configuration Values

```csharp
// ComboSystem defaults
comboWindow = 2.0f;
maxCombo = 5;
damageBonusPerCombo = 0.2f;  // 20%
attackSpeedBonusPerCombo = 0.1f;  // 10%
finisherComboRequirement = 5;
finisherDamageMultiplier = 3.0f;

// DamageNumberManager defaults
poolSize = 50;
maxPoolSize = 200;
numberLifetime = 1.0f;
floatSpeed = 1.0f;
critFontSizeMultiplier = 1.5f;

// StatusEffect priorities
Freeze = 5, Shock = 4, Burn = 3, Poison = 2, Bleed = 1;
```

### Event Subscriptions

```csharp
// ComboSystem events
comboSystem.OnComboChanged += (combo) => UpdateUI(combo);
comboSystem.OnComboReset += () => HideComboUI();
comboSystem.OnFinisherReady += () => ShowFinisherPrompt();
comboSystem.OnFinisherUsed += () => PlayFinisherSound();
```

---

**Last Updated:** Combat System v1.0

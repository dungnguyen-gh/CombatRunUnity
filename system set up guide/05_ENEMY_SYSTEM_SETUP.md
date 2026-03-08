# Enemy System Setup Guide

## Table of Contents
1. [Overview](#1-overview)
2. [Enemy Prefab Setup](#2-enemy-prefab-setup)
3. [EnemyAI Configuration](#3-enemyai-configuration)
4. [Enemy Skills](#4-enemy-skills)
5. [Object Pooling](#5-object-pooling)
6. [SPUM Integration](#6-spum-integration)
7. [Known Issues](#7-known-issues)
8. [Testing Checklist](#8-testing-checklist)

---

## 1. Overview

### System Architecture

The Enemy System uses a modular architecture with separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      ENEMY SYSTEM                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐     ┌──────────────┐     ┌────────────┐  │
│  │   Enemy.cs   │◄────│  EnemyAI.cs  │◄────│EnemySkillSO│  │
│  │              │     │              │     │            │  │
│  │ - Health     │     │ - State      │     │ - Skill    │  │
│  │ - Movement   │     │   Machine    │     │   Data     │  │
│  │ - Animation  │     │ - Skills     │     │ - Cooldown │  │
│  │ - Status FX  │     │ - Personality│     │ - Effects  │  │
│  └──────────────┘     └──────────────┘     └────────────┘  │
│          ▲                                                  │
│          │                                                  │
│  ┌──────────────────────────────────────┐                  │
│  │           EnemyPool.cs               │                  │
│  │  - Object pooling for performance    │                  │
│  │  - Auto-component injection          │                  │
│  │  - State reset on spawn              │                  │
│  └──────────────────────────────────────┘                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Component Relationships

| Component | Responsibility | Dependencies |
|-----------|---------------|--------------|
| `Enemy` | Core enemy behavior, health, movement, animations | `Rigidbody2D`, `StatusEffect`, `SPUM_Prefabs` (optional) |
| `EnemyAI` | Advanced AI, skill usage, personality behaviors | `Enemy`, `EnemySkillSO[]` |
| `EnemySkillSO` | Skill data and configuration | None (ScriptableObject) |
| `EnemyPool` | Object pooling and lifecycle management | All enemy prefabs |

---

## 2. Enemy Prefab Setup

### 2.1 Creating an Enemy Prefab

1. **Create a new GameObject** in the scene
2. **Add the Enemy component** - Required components are auto-added:
   - `Rigidbody2D` (gravity = 0, freeze rotation)
   - `StatusEffect`
   - `Collider2D` (if missing, CircleCollider2D is added)

### 2.2 Required Components

The `EnemyPool` automatically injects these components if missing:

```csharp
// Auto-added by EnemyPool.EnsureEnemyComponents()
- StatusEffect      // Required by Enemy [RequireComponent]
- Rigidbody2D       // Required by Enemy [RequireComponent]
  ├─ gravityScale = 0
  └─ freezeRotation = true
- CircleCollider2D  // For collision detection
```

### 2.3 Visual Setup

#### Option A: Legacy SpriteRenderer Setup

```csharp
// On Enemy component:
[Header("Components - Legacy (for non-SPUM)")]
public Animator animator;        // Assign if using Animator
public SpriteRenderer spriteRenderer;  // Assign sprite renderer

[Header("Sprite Facing")]
public bool spriteFacesRight = true;  // Set based on sprite default facing
```

**Setup Steps:**
1. Add `SpriteRenderer` as child object
2. Assign to `spriteRenderer` field
3. Configure `spriteFacesRight` based on default sprite orientation
4. Add Animator if using animation controller

#### Option B: SPUM Setup (Recommended)

See [SPUM Integration](#6-spum-integration) section for detailed setup.

### 2.4 Collider2D Configuration

```csharp
// CircleCollider2D (recommended for most enemies)
- Radius: 0.3 - 0.5 (adjust based on sprite size)
- Is Trigger: false (for physical collision)

// Alternative: BoxCollider2D
- Size: Adjust to match sprite bounds
```

### 2.5 Tag and Layer Assignment

| Setting | Value | Purpose |
|---------|-------|---------|
| Tag | `Enemy` | Identification for targeting |
| Layer | `Enemy` or `Default` | Collision filtering |

**Setup:**
1. Select enemy prefab
2. Set Tag: `Enemy` (create if doesn't exist)
3. Set Layer: `Enemy` (create in Tags & Layers if needed)

### 2.6 Enemy Component Configuration

```csharp
[Header("Stats")]
public int maxHealth = 30;           // Starting/max health
public int damage = 5;               // Base attack damage
public float moveSpeed = 2f;         // Movement speed
public float attackRange = 1f;       // Attack reach
public float attackCooldown = 1f;    // Seconds between attacks
public int goldReward = 5;           // Gold dropped on death
public float itemDropChance = 0.3f;  // Chance to drop item (0-1)

[Header("AI")]
public float detectionRange = 8f;    // Range to detect player
public float stopDistance = 0.5f;    // Stop this close to player
public bool patrol = false;          // Enable patrol behavior
public Vector2[] patrolPoints;       // Patrol waypoints (if patrol = true)
```

---

## 3. EnemyAI Configuration

### 3.1 Adding EnemyAI Component

1. Add `EnemyAI` component to your enemy prefab
2. Configure personality and skills
3. The AI will override basic Enemy state machine

```csharp
[Header("AI Personality")]
public AIPersonality personality = AIPersonality.Aggressive;
```

### 3.2 AI Personality Types (6 Types)

| Personality | Behavior | Best For |
|-------------|----------|----------|
| **Aggressive** | Charges player, never retreats, uses skills often | Standard melee enemies |
| **Defensive** | Keeps distance, blocks/parries, retreats when hurt (<25% HP) | Knights, tanks |
| **Ranged** | Maintains optimal distance, kites player | Archers, mages |
| **Tactical** | Adapts behavior, sometimes repositions, retreat when very low | Bosses, elites |
| **Berserker** | Never retreats, charges constantly, low health = more aggressive | Barbarians, frenzied |
| **Support** | Stays at range, buffs allies, summons minions | Healers, summoners |

#### Personality Behaviors in Detail

```csharp
// AGGRESSIVE - Always attacks
if (distanceToPlayer <= enemy.attackRange)
    return AIState.Attack;
return AIState.Chase;

// DEFENSIVE - Backs up when hurt
if (healthPercent < retreatHealthThreshold)  // 0.25
    return AIState.Retreat;
if (distanceToPlayer < enemy.attackRange * 0.5f)
    return AIState.Reposition;  // Too close

// RANGED - Maintains optimal distance
float optimalRange = enemy.attackRange * 0.8f;
if (distanceToPlayer < enemy.attackRange * 0.3f)
    return AIState.Retreat;  // Too close, kite back
if (distanceToPlayer < optimalRange)
    return AIState.Reposition;  // Reposition to optimal

// TACTICAL - Adaptable
if (healthPercent < 0.3f && Random.value < 0.5f)
    return AIState.Retreat;  // Sometimes retreat
if (Random.value < 0.2f && stateTimer > 2f)
    return AIState.Reposition;  // Sometimes reposition

// BERSERKER - Never retreats
if (distanceToPlayer <= enemy.attackRange)
    return AIState.Attack;
return AIState.Chase;

// SUPPORT - Stays at range, uses skills
if (distanceToPlayer < repositionMinDistance)  // 4f default
    return AIState.Retreat;
if (HasAvailableSummonSkill())
    return AIState.UseSkill;
return AIState.Idle;  // Wait for cooldowns
```

### 3.3 AI State Machine

```
                    ┌─────────┐
         ┌─────────►│  Idle   │◄────────┐
         │          └────┬────┘         │
         │               │              │
    Player detected      No threat    Retreat ends
         │               │              │
         ▼               ▼              │
    ┌─────────┐    ┌─────────┐         │
    │  Chase  │───►│  Attack │─────────┤
    └────┬────┘    └────┬────┘         │
         │              │              │
    In range      Out of range    Retreat condition
         │              │              │
         └──────────────┼──────────────┘
                      │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
    ┌─────────┐  ┌─────────┐  ┌──────────┐
    │ Retreat │  │Reposition│  │ UseSkill │
    └─────────┘  └─────────┘  └──────────┘
```

**States:**
- `Idle` - Waiting for player
- `Chase` - Moving toward player
- `Attack` - Within attack range
- `UseSkill` - Casting a skill
- `Retreat` - Moving away from player
- `Reposition` - Adjusting position
- `Dead` - Death state

### 3.4 Skill Assignment

```csharp
[Header("Skills")]
public EnemySkillSO[] skills = new EnemySkillSO[0];  // Assign skill SOs
public bool useSkills = true;                        // Enable/disable skills

[Header("Skill Usage")]
public float skillCheckInterval = 1f;      // How often to check for skill usage
public float minTimeBetweenSkills = 2f;    // Minimum time between any skills
public float randomSkillChance = 0.3f;     // Chance to use skill when available
```

### 3.5 Detection and Attack Ranges

```csharp
[Header("Retreat Settings")]
public bool canRetreat = true;
public float retreatHealthThreshold = 0.25f;  // Retreat when health below 25%
public float retreatDuration = 3f;            // How long to retreat
public float repositionMinDistance = 4f;      // Minimum distance for reposition

[Header("Combat Timing")]
public float reactionTime = 0.2f;       // Delay before reacting
public float attackCommitment = 0.5f;   // How long to commit to attack
```

---

## 4. Enemy Skills

### 4.1 Creating EnemySkillSO

1. **Right-click in Project window** → Create → ARPG → Enemy Skill
2. **Name the asset** (e.g., `EnemySkill_Fireball`, `EnemySkill_DashAttack`)
3. **Configure properties** in Inspector

### 4.2 Skill Types (9 Types)

| Skill Type | Description | Key Properties |
|------------|-------------|----------------|
| **MeleeAttack** | Close range physical attack | `damage`, `knockbackForce` |
| **RangedProjectile** | Fires a projectile | `projectilePrefab`, `maxRange` |
| **DashAttack** | Dashes toward player then attacks | `dashDistance`, `dashSpeed` |
| **AOEAttack** | Area effect around enemy | `aoeRadius`, `effectPrefab` |
| **Summon** | Spawns minions | `summonPrefab`, `summonCount`, `summonDuration` |
| **SelfHeal** | Restores own health | `damage` (negative = heal amount) |
| **Buff** | Temporary stat boost | `buffDuration`, `buffValue` |
| **Retreat** | Moves away from player | `retreatDuration`, `retreatDistance` |
| **ChargeAttack** | Wind up then powerful attack | `dashDistance`, `dashSpeed` |

### 4.3 Skill Configuration Reference

```csharp
[Header("Basic Info")]
public string skillId = "skill_fireball";      // Unique identifier
public string skillName = "Fireball";          // Display name
public EnemySkillType skillType = EnemySkillType.RangedProjectile;

[Header("Damage & Effects")]
public int damage = 10;                        // Base damage
public float damageMultiplier = 1f;            // Multiplier on enemy base damage
public float knockbackForce = 0f;              // Knockback amount
public bool applyStatusEffect = false;         // Apply status effect
public StatusType statusType = StatusType.None;
public float statusDuration = 3f;

[Header("Cooldown & Timing")]
public float cooldownTime = 3f;                // Seconds between uses
public float castTime = 0.5f;                  // Wind-up time
public float recoveryTime = 0.5f;              // Recovery after skill

[Header("Range & Area")]
public float minRange = 0f;                    // Minimum distance to use
public float maxRange = 5f;                    // Maximum distance to use
public float aoeRadius = 2f;                   // For AOE skills

[Header("Movement")]
public float dashDistance = 3f;                // For dash attacks
public float dashSpeed = 10f;
public float retreatDistance = 3f;             // For retreat skill
public float retreatDuration = 2f;

[Header("Summoning")]
public GameObject summonPrefab;                // Minion to spawn
public int summonCount = 2;
public float summonDuration = 10f;             // 0 = permanent

[Header("Visual & Audio")]
public GameObject castEffectPrefab;            // Effect during cast
public GameObject effectPrefab;                // Effect on impact
public GameObject projectilePrefab;            // For ranged skills
public AudioClip castSound;
public AudioClip impactSound;

[Header("AI Behavior")]
public SkillUsageCondition usageCondition = SkillUsageCondition.Anytime;
public float priority = 1f;                    // Higher = more likely to use
public bool canBeInterrupted = true;           // Can player interrupt cast
public bool facePlayerDuringCast = true;

[Header("Buff")]
public float buffDuration = 5f;
public float buffValue = 0.5f;                 // 0.5 = +50%

[Header("Animation")]
public string animationTrigger = "Attack";
public int spumAnimationIndex = 2;             // Default attack animation
```

### 4.4 Usage Conditions

| Condition | When Used |
|-----------|-----------|
| `Anytime` | No restrictions |
| `HealthAbove50` | Only when health > 50% |
| `HealthBelow50` | Only when health < 50% |
| `HealthBelow25` | Only when health < 25% (emergency skills) |
| `PlayerClose` | Only when player within close range |
| `PlayerFar` | Only when player beyond attack range |
| `OnCooldownOnly` | Use only when other skills are on cooldown |

### 4.5 Skill Examples

#### Example 1: Ranged Fireball
```csharp
skillId: "fireball"
skillName: "Fireball"
skillType: RangedProjectile
damage: 15
cooldownTime: 4f
castTime: 0.5f
minRange: 2f
maxRange: 8f
projectilePrefab: [assign fireball prefab]
usageCondition: PlayerFar
priority: 2f
```

#### Example 2: Dash Attack
```csharp
skillId: "dash_slash"
skillName: "Dash Slash"
skillType: DashAttack
damage: 20
cooldownTime: 6f
dashDistance: 4f
dashSpeed: 15f
maxRange: 5f
usageCondition: Anytime
priority: 1.5f
canBeInterrupted: false
```

#### Example 3: Summon Minions
```csharp
skillId: "summon_skeletons"
skillName: "Raise Dead"
skillType: Summon
damage: 0
cooldownTime: 15f
castTime: 1f
summonPrefab: [assign skeleton prefab]
summonCount: 3
summonDuration: 20f
usageCondition: OnCooldownOnly
priority: 3f
```

#### Example 4: Emergency Heal
```csharp
skillId: "emergency_heal"
skillName: "Dark Pact"
skillType: SelfHeal
damage: -25  // Negative = healing
cooldownTime: 20f
castTime: 0.8f
usageCondition: HealthBelow25
priority: 5f
```

### 4.6 Cooldown and Priority System

```csharp
// Priority selection uses weighted random
float totalPriority = 0f;
foreach (int index in availableSkills) {
    totalPriority += skills[index].priority;
}

float random = Random.value * totalPriority;
float current = 0f;

foreach (int index in availableSkills) {
    current += skills[index].priority;
    if (random <= current) {
        return index;  // Selected skill
    }
}
```

---

## 5. Object Pooling

### 5.1 EnemyPool Setup

1. **Create an empty GameObject** in scene (e.g., `EnemyPool`)
2. **Add `EnemyPool` component**
3. **Configure pools** in Inspector

### 5.2 Pool Configuration

```csharp
[System.Serializable]
public class PoolConfig {
    public string poolId = "Goblin";           // Pool identifier
    public GameObject prefab;                  // Enemy prefab
    public int initialSize = 10;               // Pre-instantiated count
    public bool allowGrowth = true;            // Can pool expand
    public int maxSize = 50;                   // Maximum pool size
}

[Header("Pool Configuration")]
public PoolConfig[] enemyPools;                // Configure all enemy types
public Transform poolContainer;                // Parent for pooled objects (optional)

[Header("Spawn Settings")]
public bool autoFacePlayer = true;             // Auto-face on spawn
public float spawnInvulnerabilityTime = 0.5f;  // Spawn protection
```

### 5.3 Auto-Component Injection

The pool automatically adds required components to prefabs:

```csharp
void EnsureEnemyComponents(GameObject obj) {
    // 1. StatusEffect (required by Enemy)
    if (obj.GetComponent<StatusEffect>() == null)
        obj.AddComponent<StatusEffect>();
    
    // 2. Rigidbody2D (required by Enemy)
    if (obj.GetComponent<Rigidbody2D>() == null) {
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }
    
    // 3. Enemy component
    if (obj.GetComponent<Enemy>() == null)
        obj.AddComponent<Enemy>();
    
    // 4. Collider2D
    if (obj.GetComponent<Collider2D>() == null) {
        var col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = false;
    }
}
```

### 5.4 Spawning Enemies

```csharp
// Basic spawn from pool
GameObject enemy = EnemyPool.Instance.GetFromPool(prefab, position, rotation);

// Spawn facing player
GameObject enemy = EnemyPool.Instance.GetFromPoolFacingPlayer(prefab, position);

// Spawn with explicit facing direction
GameObject enemy = EnemyPool.Instance.GetFromPoolWithFacing(
    prefab, 
    position, 
    Vector2.left  // Face left
);
```

### 5.5 State Reset on Spawn

The pool automatically resets enemy state:

```csharp
void ResetEnemyState(GameObject obj, Vector3 spawnPosition) {
    // Reset Enemy component
    Enemy enemy = obj.GetComponent<Enemy>();
    if (enemy != null) enemy.ResetFromPool();
    
    // Reset AI
    EnemyAI ai = obj.GetComponent<EnemyAI>();
    if (ai != null) ai.ResetAI();
    
    // Clear status effects
    StatusEffect status = obj.GetComponent<StatusEffect>();
    if (status != null) status.ClearStatus();
    
    // Reset physics
    Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
    if (rb != null) {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.simulated = true;
    }
    
    // Enable colliders
    Collider2D[] colliders = obj.GetComponents<Collider2D>();
    foreach (var col in colliders) col.enabled = true;
    
    // Reset renderers
    SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
    foreach (var renderer in renderers) renderer.color = Color.white;
    
    // Clear particles
    ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
    foreach (var ps in particles) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
}
```

### 5.6 Returning to Pool

Enemies automatically return to pool after death:

```csharp
// In Enemy.Die():
StartTrackedCoroutine(ReturnToPoolAfterDelay(3f));

IEnumerator ReturnToPoolAfterDelay(float delay) {
    yield return new WaitForSeconds(delay);
    if (EnemyPool.Instance != null)
        EnemyPool.Instance.ReturnToPool(gameObject);
    else
        Destroy(gameObject);  // Fallback
}
```

Manual return:
```csharp
EnemyPool.Instance.ReturnToPool(enemyObject);

// Or with delay
EnemyPool.Instance.ReturnToPoolAfterDelay(enemyObject, 2f);
```

### 5.7 Pool Management

```csharp
// Pre-warm pool with additional objects
EnemyPool.Instance.PreWarmPool(prefab, 10);

// Get pool statistics
int available, active, total;
EnemyPool.Instance.GetPoolStats(prefab, out available, out active, out total);
Debug.Log($"Pool: {available} available, {active} active, {total} total");

// Clear all pools (scene change, game reset)
EnemyPool.Instance.ClearAllPools();
```

---

## 6. SPUM Integration

### 6.1 SPUM Setup for Enemies

SPUM (Sprite Preview Unity Maker) provides enhanced animation control.

**Setup Steps:**

1. **Create SPUM prefab** using SPUM tool
2. **Add as child** to enemy GameObject
3. **Configure Enemy component:**

```csharp
[Header("SPUM Integration")]
public bool useSPUM = true;                    // Enable SPUM
public SPUM_Prefabs spumPrefabs;               // Reference to SPUM component
public int idleAnimationIndex = 0;             // Index for idle animation
public int moveAnimationIndex = 0;             // Index for move animation
public int attackAnimationIndex = 0;           // Index for attack animation
public int hitAnimationIndex = 0;              // Index for hit animation
public int deathAnimationIndex = 0;            // Index for death animation
```

### 6.2 Animation Indices

Configure indices based on your SPUM animation setup:

```csharp
// Example SPUM Animation Configuration:
// StateAnimationPairs["IDLE"] = [Idle_0, Idle_1, Idle_2...]
// StateAnimationPairs["MOVE"] = [Run_0, Run_1...]
// StateAnimationPairs["ATTACK"] = [Attack_0, Attack_1, Attack_2...]
// StateAnimationPairs["DAMAGED"] = [Hit_0, Hit_1...]
// StateAnimationPairs["DEATH"] = [Death_0, Death_1...]

// If your SPUM has:
// - 3 idle animations (index 0-2)
// - 2 attack animations (index 0-1)
// Set:
idleAnimationIndex = 0;      // Use first idle
attackAnimationIndex = 1;    // Use second attack variation
```

### 6.3 Sprite Facing Configuration

```csharp
[Header("Sprite Facing")]
public bool spriteFacesRight = true;
```

**Facing Logic:**
- If `spriteFacesRight = true`: Default sprite faces RIGHT
  - Moving right: flipX = false (no flip)
  - Moving left: flipX = true (flip)
  
- If `spriteFacesRight = false`: Default sprite faces LEFT
  - Moving right: flipX = true (flip)
  - Moving left: flipX = false (no flip)

### 6.4 SPUM Initialization

```csharp
void InitializeSPUM() {
    if (spumPrefabs == null) return;
    
    // Validate animator setup
    if (spumPrefabs._anim == null) {
        Debug.LogWarning("SPUM prefab has no Animator assigned!");
        return;
    }
    
    if (spumPrefabs._anim.runtimeAnimatorController == null) {
        Debug.LogWarning("SPUM prefab has no Animator Controller!");
        return;
    }
    
    // Initialize if not already override controller
    if (!(spumPrefabs._anim.runtimeAnimatorController is AnimatorOverrideController)) {
        spumPrefabs.OverrideControllerInit();
    }
    
    // Populate animation lists
    if (!spumPrefabs.allListsHaveItemsExist()) {
        spumPrefabs.PopulateAnimationLists();
    }
    
    PlayIdleAnimation();
}
```

### 6.5 SPUM Facing for 2D

```csharp
void UpdateSPUMFacing(Vector2 direction) {
    if (spumPrefabs == null) return;
    
    Vector3 scale = spumPrefabs.transform.localScale;
    float absScaleX = Mathf.Abs(scale.x);
    
    if (direction.x > 0.1f) {
        // Facing right
        scale.x = spriteFacesRight ? absScaleX : -absScaleX;
    } else if (direction.x < -0.1f) {
        // Facing left
        scale.x = spriteFacesRight ? -absScaleX : absScaleX;
    }
    spumPrefabs.transform.localScale = scale;
}
```

---

## 7. Known Issues

### 7.1 Buff Skill Not Implemented

**Status:** ⚠️ Partially implemented in `EnemySkillSO` but execution logic missing

**Current State:**
```csharp
case EnemySkillType.Buff:
    // TODO: Implement buff logic
    // Buff fields exist in EnemySkillSO:
    // - buffDuration
    // - buffValue
    break;
```

**Workaround:** Use `SelfHeal` or modify stats directly via custom script.

### 7.2 Knockback Commented Out

**Status:** ⚠️ Code exists but is commented

**Location:** `EnemyAI.cs` line 645-649

```csharp
// Apply knockback
if (skill.knockbackForce > 0) {
    Vector2 knockbackDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
    // Apply knockback to player (if player controller supports it)
    // playerController.ApplyKnockback(knockbackDir * skill.knockbackForce);
}
```

**To Enable:**
1. Add `ApplyKnockback(Vector2 force)` method to `PlayerController`
2. Uncomment the code above
3. Ensure player has appropriate physics response

### 7.3 Status Effect Application Missing

**Status:** ⚠️ Placeholder code

**Location:** `EnemyAI.cs` line 651-654

```csharp
// Apply status effect
if (skill.applyStatusEffect) {
    // Apply to player if player has status effect system
}
```

**To Enable:**
```csharp
if (skill.applyStatusEffect && skill.statusType != StatusType.None) {
    var playerStatusEffect = player.GetComponent<StatusEffect>();
    if (playerStatusEffect != null) {
        playerStatusEffect.ApplyStatus(skill.statusType, skill.statusDuration);
    }
}
```

---

## 8. Testing Checklist

### 8.1 Basic Enemy Setup

- [ ] Enemy prefab created with all required components
- [ ] `Rigidbody2D` configured (gravity = 0, freeze rotation)
- [ ] `Collider2D` added and sized appropriately
- [ ] `StatusEffect` component present
- [ ] Tag set to "Enemy"
- [ ] Layer configured for collision

### 8.2 Visual Setup

**Legacy Animation:**
- [ ] `SpriteRenderer` assigned
- [ ] `spriteFacesRight` configured correctly
- [ ] `Animator` assigned (if using)

**SPUM:**
- [ ] `useSPUM` enabled
- [ ] `spumPrefabs` assigned
- [ ] Animation indices configured
- [ ] Sprite facing works correctly in both directions

### 8.3 Stats and Combat

- [ ] `maxHealth` set appropriately
- [ ] `damage` balanced for game difficulty
- [ ] `moveSpeed` tested with scene scale
- [ ] `attackRange` matches visual reach
- [ ] `attackCooldown` not too fast/slow
- [ ] `detectionRange` appropriate for level design
- [ ] `stopDistance` prevents overlapping

### 8.4 AI Configuration

- [ ] `EnemyAI` component added (for advanced enemies)
- [ ] Personality type selected appropriately
- [ ] Skills assigned (if using)
- [ ] Skill cooldowns balanced
- [ ] Skill ranges appropriate
- [ ] Usage conditions make sense for AI personality

### 8.5 Skill Testing

| Skill Type | Test | Expected Result |
|------------|------|-----------------|
| MeleeAttack | Get in range | Attack animation plays, damage dealt |
| RangedProjectile | Stay at distance | Projectile fires toward player |
| DashAttack | Stay at mid-range | Enemy dashes and deals damage |
| AOEAttack | Get close | AOE effect spawns, damage in radius |
| Summon | Wait for cooldown | Minions spawn at enemy position |
| SelfHeal | Damage enemy to <25% | Enemy heals when condition met |
| ChargeAttack | Stay in line | Enemy charges, high damage on hit |

### 8.6 Object Pooling

- [ ] `EnemyPool` present in scene
- [ ] Pool configurations set up
- [ ] Enemies spawn from pool correctly
- [ ] Enemies return to pool after death
- [ ] State resets properly on respawn
- [ ] No memory leaks over extended play

### 8.7 Integration Testing

- [ ] Enemy detects player correctly
- [ ] Enemy chases player
- [ ] Enemy attacks when in range
- [ ] Enemy takes damage from player
- [ ] Enemy dies and drops rewards
- [ ] Death animation plays correctly
- [ ] Enemy returns to pool after death delay
- [ ] Multiple enemies can exist simultaneously
- [ ] Enemies don't collide with each other inappropriately

### 8.8 AI Personality Testing

| Personality | Test Scenario | Expected Behavior |
|-------------|---------------|-------------------|
| Aggressive | Stay at any range | Always chases and attacks |
| Defensive | Damage to <25% | Retreats and re-engages |
| Ranged | Get very close | Retreats to maintain distance |
| Tactical | Fight for >10 seconds | Occasionally repositions |
| Berserker | Damage to <25% | Continues attacking, no retreat |
| Support | Spawn with allies | Stays back, attempts to summon |

### 8.9 Performance Testing

- [ ] Pool pre-instantiation working
- [ ] No stutter when spawning enemies
- [ ] Pool growth doesn't exceed max size
- [ ] Cleanup working on scene change
- [ ] No null reference errors from pooled objects

---

## Quick Reference

### Minimum Viable Enemy Setup

```csharp
// Required components (auto-added by pool)
- Enemy
- Rigidbody2D (gravity=0, freezeRotation)
- CircleCollider2D
- StatusEffect

// Minimum configuration
Enemy:
  maxHealth: 30
  damage: 5
  moveSpeed: 2
  attackRange: 1
  detectionRange: 8
  
// Add for AI
EnemyAI:
  personality: Aggressive
  useSkills: false  // Disable until skills configured
```

### Recommended Folder Structure

```
Assets/
├── ScriptableObjects/
│   └── EnemySkills/
│       ├── EnemySkill_MeleeSlash.asset
│       ├── EnemySkill_Fireball.asset
│       └── EnemySkill_SummonSkeletons.asset
├── Prefabs/
│   └── Enemies/
│       ├── Enemy_Goblin.prefab
│       ├── Enemy_Skeleton.prefab
│       └── Enemy_OrcBoss.prefab
└── Scripts/
    ├── Enemies/
    │   ├── Enemy.cs
    │   └── EnemyAI.cs
    ├── Data/
    │   └── EnemySkillSO.cs
    └── Managers/
        └── EnemyPool.cs
```

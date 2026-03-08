# SkillSO Complete Reference Guide

**Date:** 2026-03-08  
**Version:** 1.0 - All Fields Explained

---

## 📋 Table of Contents

1. [Basic Info](#1-basic-info)
2. [Targeting & Casting](#2-targeting--casting)
3. [Cooldown & Cost](#3-cooldown--cost)
4. [Damage & Effects](#4-damage--effects)
5. [Area & Range](#5-area--range)
6. [Duration & Ticks](#6-duration--ticks)
7. [Movement (Dash/Teleport)](#7-movement-dashteleport)
8. [Summon Settings](#8-summon-settings)
9. [Chain Settings](#9-chain-settings)
10. [Visual & Audio](#10-visual--audio)
11. [Animation](#11-animation)
12. [Screen Effects](#12-screen-effects)
13. [Status Effects](#13-status-effects)
14. [Advanced](#14-advanced)

---

## 1. Basic Info

### `skillId` (string)
**Purpose:** Unique identifier for the skill  
**Example:** `"fireball_001"`, `"spin_attack"`  
**Required:** Yes  
**Note:** Used for saving/loading and skill synergies

### `skillName` (string)
**Purpose:** Display name shown in UI  
**Example:** `"Fireball"`, `"Shield Wall"`  
**Required:** Yes

### `description` (string)
**Purpose:** Tooltip description  
**Example:** `"Hurls a ball of fire that burns enemies."`  
**Required:** No

### `icon` (Sprite)
**Purpose:** UI icon for skill slot  
**Required:** Yes  
**Tip:** Use 64x64 or 128x128 pixel images

### `skillSlot` (int)
**Purpose:** Which key slot (0-3 = keys 1-4)  
**Default:** 0  
**Note:** Usually assigned at runtime, not in SO

### `rarity` (SkillRarity)
**Purpose:** Skill tier affecting color/power  
**Options:** Common, Uncommon, Rare, Epic, Legendary  
**Default:** Common  
**Effect:** Changes UI color (Common=White, Legendary=Yellow)

---

## 2. Targeting & Casting

### `skillType` (SkillType)
**Purpose:** Determines skill behavior  
**Options:**
- `CircleAOE` - Spin attack around player
- `GroundAOE` - Meteor at mouse position
- `Projectile` - Fireball/arrows
- `Melee` - Single target raycast
- `Shield` - Damage reduction buff
- `Dash` - Quick movement
- `Summon` - Spawn allies
- `Buff` - Stat enhancement
- `Heal` - Restore HP
- `Chain` - Lightning bounce
- `Beam` - Continuous channeling
- `Trap` - Deployable hazard
- `Teleport` - Instant movement
- `Reflect` - Projectile shield

### `targeting` (SkillTargeting)
**Purpose:** How skill determines target  
**Options:**
- `Self` - Centered on player
- `MousePosition` - At cursor (for GroundAOE, Teleport)
- `Directional` - In facing direction (for Projectile, Melee)
- `TargetEnemy` - Requires enemy under cursor
- `TargetAlly` - Requires ally under cursor
- `AreaSelect` - Click and drag area
- `AutoTarget` - Automatically targets nearest

### `castTime` (float)
**Purpose:** Charging/wind-up time before skill fires  
**Default:** 0 (instant)  
**Example:** 0.5 = half second delay  
**Used by:** All skill types  
**Effect:** Player can cancel by moving (if `canMoveWhileCasting` = false)

### `canMoveWhileCasting` (bool)
**Purpose:** Allow movement during cast time  
**Default:** true  
**Effect:** 
- true = Can move, skill fires after delay
- false = Moving cancels the skill

### `requiresLineOfSight` (bool)
**Purpose:** Check for obstacles between player and target  
**Default:** false  
**Used by:** Teleport, some targeted skills  
**Effect:** Prevents casting through walls

---

## 3. Cooldown & Cost

### `cooldownTime` (float)
**Purpose:** Seconds before skill can be used again  
**Default:** 5  
**Example:** 3 = fast skill, 20 = powerful skill  
**Required:** Yes (must be > 0)

### `manaCost` (int)
**Purpose:** Resource cost (if using mana system)  
**Default:** 0  
**Note:** Currently not implemented in base game

### `healthCost` (int)
**Purpose:** HP cost for "blood magic" skills  
**Default:** 0  
**Example:** 10 = costs 10 HP to cast  
**Note:** Currently not implemented in base game

---

## 4. Damage & Effects

### `damageMultiplier` (float)
**Purpose:** Multiplies player's base damage  
**Default:** 1  
**Formula:** `finalDamage = (baseDamage * damageMultiplier) + flatDamageBonus`
**Examples:**
- 1.0 = 100% of base damage
- 1.5 = 150% of base damage (strong skill)
- 0.5 = 50% of base damage (weak/fast skill)
- 2.0 = 200% of base damage (ultimate)

### `flatDamageBonus` (int)
**Purpose:** Fixed damage added after multiplier  
**Default:** 0  
**Example:** 10 = +10 damage  
**Use case:** Guaranteed minimum damage

### `critChanceBonus` (float)
**Purpose:** Additional crit chance  
**Default:** 0  
**Example:** 0.15 = +15% crit chance  
**Calculation:** Added to player's base crit chance

### `critDamageMultiplier` (float)
**Purpose:** Damage multiplier when critting  
**Default:** 1 (no extra damage)  
**Example:** 2.0 = crits do 2x damage  
**Note:** 1.5-2.0 is typical for crit skills

---

## 5. Area & Range

### `range` (float)
**Purpose:** Maximum distance skill can reach  
**Default:** 3  
**Used by:**
- Projectile: Travel distance
- Melee: Raycast length
- Chain: Initial target search range
- Dash: Movement distance
- Teleport: Max range

### `radius` (float)
**Purpose:** AOE explosion size  
**Default:** 2  
**Used by:**
- CircleAOE: Damage radius
- GroundAOE: Explosion radius
- Projectile (if explodeOnImpact): Explosion size

### `coneAngle` (float)
**Purpose:** Angle for cone-shaped attacks  
**Default:** 360  
**Example:** 60 = narrow cone, 360 = full circle  
**Note:** Currently not widely used

---

## 6. Duration & Ticks

### `duration` (float)
**Purpose:** How long effect lasts  
**Default:** 0 (instant)  
**Used by:**
- Shield: Duration of protection
- Buff: Stat boost duration
- Summon: Ally lifetime
- Trap: Trap persistence
- Beam: Channeling duration
- Status Effects: Debuff duration

### `tickRate` (float)
**Purpose:** Interval between damage ticks  
**Default:** 1 (second)  
**Used by:**
- Beam: Damage interval
- DOT effects: Tick interval
**Example:** 0.5 = damage every half second

### `maxStacks` (int)
**Purpose:** Maximum stack count for debuffs/buffs  
**Default:** 1  
**Note:** Currently not implemented

---

## 7. Movement (Dash/Teleport)

### `dashDistance` (float)
**Purpose:** How far dash/teleport moves  
**Default:** 0  
**Used by:** Dash, Teleport
**Example:** 5 = 5 units distance

### `dashSpeed` (float)
**Purpose:** How fast dash animation plays  
**Default:** 20  
**Note:** Higher = faster dash

### `dashInvulnerable` (bool)
**Purpose:** Invincibility during dash  
**Default:** false  
**Effect:** Player takes no damage while dashing

### `leaveTrail` (bool)
**Purpose:** Visual trail effect  
**Default:** false  
**Effect:** Leaves ghost images behind player

---

## 8. Summon Settings

### `summonPrefab` (GameObject)
**Purpose:** Ally unit to spawn  
**Required for:** Summon skill type
**Prefab should have:**
- SpriteRenderer
- Rigidbody2D
- Collider2D
- Simple AI script (optional)

### `summonCount` (int)
**Purpose:** Number of units to summon  
**Default:** 1  
**Example:** 3 = spawns 3 allies

### `summonDuration` (float)
**Purpose:** How long allies live  
**Default:** 10 (seconds)  
**Note:** 0 = permanent (not recommended)

### `summonFollowPlayer` (bool)
**Purpose:** Allies follow player  
**Default:** true  
**Note:** Requires AI implementation on summon prefab

---

## 9. Chain Settings

### `chainBounces` (int)
**Purpose:** Number of times lightning jumps  
**Default:** 3  
**Example:** 5 = hits 6 enemies total (initial + 5 bounces)

### `chainRange` (float)
**Purpose:** Maximum bounce distance  
**Default:** 5  
**Note:** Enemy must be within this range to bounce

### `chainDamageFalloff` (float)
**Purpose:** Damage reduction per bounce  
**Default:** 0.8 (80% = 20% less damage)  
**Example:**
- 1.0 = full damage (no falloff)
- 0.8 = 80% damage each bounce
- 0.5 = 50% damage each bounce

---

## 10. Visual & Audio

### `castEffectPrefab` (GameObject)
**Purpose:** Effect at skill start  
**Examples:**
- Projectile: Muzzle flash
- GroundAOE: Targeting circle
- Shield: Casting circle

### `effectPrefab` (GameObject)
**Purpose:** Effect at impact/hit  
**Examples:**
- Projectile: Explosion
- Melee: Hit spark
- Heal: Healing particles

### `projectilePrefab` (GameObject)
**Purpose:** Projectile object for Projectile skill type  
**Required for:** Projectile skills  
**Prefab should have:** Projectile script

### `persistentEffectPrefab` (GameObject)
**Purpose:** Lasting visual effect  
**Used by:** Shield, Buff  
**Examples:** Bubble shield, aura particles

### `castSound` (AudioClip)
**Purpose:** Sound when skill starts  
**Examples:** Casting sound, voice line

### `impactSound` (AudioClip)
**Purpose:** Sound when skill hits  
**Examples:** Explosion, hit sound

### `loopSound` (AudioClip)
**Purpose:** Continuous sound for channeling  
**Used by:** Beam, Channel skills

---

## 11. Animation

### `animationTrigger` (string)
**Purpose:** Animator parameter name  
**Default:** "Skill"  
**Note:** Trigger parameter in Animator

### `spumAnimationIndex` (int)
**Purpose:** Which SPUM animation to play  
**Default:** 0  
**Note:** Index in SPUM animation list

### `animationSpeedMultiplier` (float)
**Purpose:** Speed up/slow down animation  
**Default:** 1  
**Example:** 2.0 = 2x speed, 0.5 = half speed

---

## 12. Screen Effects

### `useScreenShake` (bool)
**Purpose:** Shake camera on cast  
**Default:** false

### `screenShakeIntensity` (float)
**Purpose:** How much camera shakes  
**Default:** 0.3  
**Range:** 0.1 - 1.0

### `screenShakeDuration` (float)
**Purpose:** How long shake lasts  
**Default:** 0.2 (seconds)

### `useSlowMotion` (bool)
**Purpose:** Slow time on cast  
**Default:** false  
**Use case:** Ultimate skills, dramatic moments

### `slowMotionScale` (float)
**Purpose:** Time speed (0-1)  
**Default:** 0.5  
**Example:** 0.5 = 50% speed (half speed)

### `slowMotionDuration` (float)
**Purpose:** How long slow mo lasts  
**Default:** 0.3 (seconds)  
**Note:** Keep short (0.1-0.5) for gameplay feel

---

## 13. Status Effects

### `applyBurn` (bool)
**Purpose:** Apply fire DOT  
**Default:** false  
**Effect:** Damage over time + visual

### `applyFreeze` (bool)
**Purpose:** Apply slow effect  
**Default:** false  
**Effect:** Movement slow + blue tint

### `applyPoison` (bool)
**Purpose:** Apply poison DOT  
**Default:** false  
**Effect:** Damage over time + green tint

### `applyShock` (bool)
**Purpose:** Apply stun  
**Default:** false  
**Effect:** Brief stun + yellow effect

### `applyStun` (bool)
**Purpose:** Apply longer stun  
**Default:** false  
**Effect:** Full stun (can't move/attack)

### `statusDuration` (float)
**Purpose:** How long status effects last  
**Default:** 3 (seconds)  
**Applies to:** Burn, Freeze, Poison, Shock, Stun

---

## 14. Advanced

### `pierceEnemies` (bool)
**Purpose:** Projectile goes through enemies  
**Default:** false  
**Effect:** Hits multiple enemies in line

### `explodeOnImpact` (bool)
**Purpose:** Projectile explodes on hit  
**Default:** false  
**Effect:** AOE damage at impact point

### `explosionRadius` (float)
**Purpose:** Size of explosion  
**Default:** 2  
**Used with:** explodeOnImpact

### `homing` (bool)
**Purpose:** Projectile tracks enemies  
**Default:** false  
**Effect:** Curves toward nearest enemy

### `homingStrength` (float)
**Purpose:** How aggressively projectile turns  
**Default:** 5  
**Higher = sharper turns

---

## 🎯 Complete Skill Templates (All 14 Types)

### 1. Fireball (Projectile + Explosion)
```yaml
Basic Info:
  skillId: fireball
  skillName: Fireball
  description: Hurls a blazing fireball that explodes on impact, burning enemies.
  icon: [fireball_icon]
  rarity: Common

Targeting & Casting:
  skillType: Projectile
  targeting: Directional
  castTime: 0.2
  canMoveWhileCasting: true
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 4
  manaCost: 10
  healthCost: 0

Damage & Effects:
  damageMultiplier: 1.3
  flatDamageBonus: 5
  critChanceBonus: 0.1
  critDamageMultiplier: 1.5

Area & Range:
  range: 12
  radius: 2
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [muzzle_flash]
  effectPrefab: [explosion_fire]
  projectilePrefab: [fireball_projectile]
  persistentEffectPrefab: null
  castSound: [fire_cast]
  impactSound: [explosion_sound]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 1
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.2
  screenShakeDuration: 0.15
  useSlowMotion: false
  slowMotionScale: 0.5
  slowMotionDuration: 0.2

Status Effects:
  applyBurn: true
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: true
  explosionRadius: 2.5
  homing: false
  homingStrength: 5
```

---

### 2. Spin Attack (CircleAOE)
```yaml
Basic Info:
  skillId: spin_attack
  skillName: Whirlwind
  description: Spin with your weapon, damaging all nearby enemies.
  icon: [spin_icon]
  rarity: Common

Targeting & Casting:
  skillType: CircleAOE
  targeting: Self
  castTime: 0.1
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 6
  manaCost: 15
  healthCost: 0

Damage & Effects:
  damageMultiplier: 1.0
  flatDamageBonus: 0
  critChanceBonus: 0.15
  critDamageMultiplier: 1.5

Area & Range:
  range: 3
  radius: 3
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [spin_windup]
  effectPrefab: [slash_circle]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [sword_swing]
  impactSound: [hit_sound]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 2
  animationSpeedMultiplier: 1.2

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.15
  screenShakeDuration: 0.1
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 3. Meteor Strike (GroundAOE)
```yaml
Basic Info:
  skillId: meteor_strike
  skillName: Meteor Strike
  description: Call down a meteor at target location after a brief delay.
  icon: [meteor_icon]
  rarity: Rare

Targeting & Casting:
  skillType: GroundAOE
  targeting: MousePosition
  castTime: 0.5
  canMoveWhileCasting: true
  requiresLineOfSight: true

Cooldown & Cost:
  cooldownTime: 10
  manaCost: 25
  healthCost: 0

Damage & Effects:
  damageMultiplier: 2.0
  flatDamageBonus: 10
  critChanceBonus: 0.05
  critDamageMultiplier: 2.0

Area & Range:
  range: 15
  radius: 4
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [targeting_circle]
  effectPrefab: [meteor_explosion]
  projectilePrefab: [meteor_falling]
  persistentEffectPrefab: null
  castSound: [meteor_cast]
  impactSound: [meteor_impact]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 3
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.5
  screenShakeDuration: 0.3
  useSlowMotion: true
  slowMotionScale: 0.3
  slowMotionDuration: 0.2

Status Effects:
  applyBurn: true
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 4

Advanced:
  pierceEnemies: false
  explodeOnImpact: true
  explosionRadius: 4
  homing: false
  homingStrength: 5
```

---

### 4. Shield Wall (Shield)
```yaml
Basic Info:
  skillId: shield_wall
  skillName: Shield Wall
  description: Summon a protective barrier that reduces incoming damage.
  icon: [shield_icon]
  rarity: Uncommon

Targeting & Casting:
  skillType: Shield
  targeting: Self
  castTime: 0.3
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 12
  manaCost: 20
  healthCost: 0

Damage & Effects:
  damageMultiplier: 0.0
  flatDamageBonus: 0
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 0
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 5
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [shield_cast_effect]
  effectPrefab: [shield_burst]
  projectilePrefab: null
  persistentEffectPrefab: [shield_bubble_effect]
  castSound: [shield_cast]
  impactSound: null
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 4
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.1
  screenShakeDuration: 0.1
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 5. Dash (Dash)
```yaml
Basic Info:
  skillId: dash
  skillName: Shadow Step
  description: Quickly dash in your facing direction, becoming invulnerable.
  icon: [dash_icon]
  rarity: Common

Targeting & Casting:
  skillType: Dash
  targeting: Directional
  castTime: 0
  canMoveWhileCasting: true
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 5
  manaCost: 10
  healthCost: 0

Damage & Effects:
  damageMultiplier: 0
  flatDamageBonus: 0
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 0
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Movement:
  dashDistance: 6
  dashSpeed: 25
  dashInvulnerable: true
  leaveTrail: true

Visual & Audio:
  castEffectPrefab: [dash_trail]
  effectPrefab: [dash_afterimage]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [dash_sound]
  impactSound: null
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 5
  animationSpeedMultiplier: 1.5

Screen Effects:
  useScreenShake: false
  screenShakeIntensity: 0
  screenShakeDuration: 0
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 6. Summon Wolves (Summon)
```yaml
Basic Info:
  skillId: summon_wolves
  skillName: Call of the Wild
  description: Summon spirit wolves to fight alongside you.
  icon: [summon_icon]
  rarity: Rare

Targeting & Casting:
  skillType: Summon
  targeting: Self
  castTime: 0.8
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 20
  manaCost: 40
  healthCost: 0

Damage & Effects:
  damageMultiplier: 0
  flatDamageBonus: 0
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 0
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Summon Settings:
  summonPrefab: [wolf_minion]
  summonCount: 3
  summonDuration: 15
  summonFollowPlayer: true

Visual & Audio:
  castEffectPrefab: [summon_circle]
  effectPrefab: [wolf_appear]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [howl_sound]
  impactSound: null
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 6
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.2
  screenShakeDuration: 0.2
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 7. Berserker Rage (Buff)
```yaml
Basic Info:
  skillId: berserker_rage
  skillName: Berserker Rage
  description: Enter a rage state, increasing damage and crit chance.
  icon: [buff_icon]
  rarity: Uncommon

Targeting & Casting:
  skillType: Buff
  targeting: Self
  castTime: 0.4
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 15
  manaCost: 0
  healthCost: 10

Damage & Effects:
  damageMultiplier: 1.5
  flatDamageBonus: 0
  critChanceBonus: 0.25
  critDamageMultiplier: 2.0

Area & Range:
  range: 0
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 8
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [rage_aura_cast]
  effectPrefab: [rage_burst]
  projectilePrefab: null
  persistentEffectPrefab: [rage_aura_loop]
  castSound: [rage_shout]
  impactSound: null
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 7
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.2
  screenShakeDuration: 0.2
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 8. Heal (Heal)
```yaml
Basic Info:
  skillId: heal
  skillName: Divine Heal
  description: Restore health instantly.
  icon: [heal_icon]
  rarity: Common

Targeting & Casting:
  skillType: Heal
  targeting: Self
  castTime: 0.3
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 8
  manaCost: 15
  healthCost: 0

Damage & Effects:
  damageMultiplier: 2.0
  flatDamageBonus: 20
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 0
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [heal_cast]
  effectPrefab: [heal_burst]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [heal_sound]
  impactSound: null
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 8
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: false
  screenShakeIntensity: 0
  screenShakeDuration: 0
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 9. Chain Lightning (Chain)
```yaml
Basic Info:
  skillId: chain_lightning
  skillName: Chain Lightning
  description: Unleash lightning that jumps between enemies.
  icon: [lightning_icon]
  rarity: Epic

Targeting & Casting:
  skillType: Chain
  targeting: Directional
  castTime: 0.2
  canMoveWhileCasting: true
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 10
  manaCost: 30
  healthCost: 0

Damage & Effects:
  damageMultiplier: 1.0
  flatDamageBonus: 5
  critChanceBonus: 0.1
  critDamageMultiplier: 1.8

Area & Range:
  range: 10
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Chain Settings:
  chainBounces: 5
  chainRange: 6
  chainDamageFalloff: 0.75

Visual & Audio:
  castEffectPrefab: [lightning_cast_hand]
  effectPrefab: [lightning_arc]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [lightning_cast]
  impactSound: [zap_sound]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 9
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.15
  screenShakeDuration: 0.1
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: true
  applyStun: false
  statusDuration: 1

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 10. Laser Beam (Beam)
```yaml
Basic Info:
  skillId: laser_beam
  skillName: Death Ray
  description: Channel a powerful beam that deals continuous damage.
  icon: [beam_icon]
  rarity: Epic

Targeting & Casting:
  skillType: Beam
  targeting: Directional
  castTime: 0.3
  canMoveWhileCasting: false
  requiresLineOfSight: true

Cooldown & Cost:
  cooldownTime: 15
  manaCost: 5
  healthCost: 0

Damage & Effects:
  damageMultiplier: 0.5
  flatDamageBonus: 2
  critChanceBonus: 0.05
  critDamageMultiplier: 1.5

Area & Range:
  range: 12
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 5
  tickRate: 0.2
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [beam_charge]
  effectPrefab: [beam_impact]
  projectilePrefab: null
  persistentEffectPrefab: [beam_effect_loop]
  castSound: [beam_start]
  impactSound: null
  loopSound: [beam_loop]

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 10
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.1
  screenShakeDuration: 0.05
  useSlowMotion: false

Status Effects:
  applyBurn: true
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 2

Advanced:
  pierceEnemies: true
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 11. Bear Trap (Trap)
```yaml
Basic Info:
  skillId: bear_trap
  skillName: Bear Trap
  description: Place a trap that damages and immobilizes enemies.
  icon: [trap_icon]
  rarity: Uncommon

Targeting & Casting:
  skillType: Trap
  targeting: MousePosition
  castTime: 0.4
  canMoveWhileCasting: true
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 12
  manaCost: 15
  healthCost: 0

Damage & Effects:
  damageMultiplier: 1.2
  flatDamageBonus: 10
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 8
  radius: 1
  coneAngle: 360

Duration & Ticks:
  duration: 30
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [trap_placement]
  effectPrefab: [trap_trigger]
  projectilePrefab: null
  persistentEffectPrefab: [trap_idle]
  castSound: [trap_set]
  impactSound: [trap_snap]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 11
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: false
  screenShakeIntensity: 0
  screenShakeDuration: 0
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: true
  statusDuration: 2

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 12. Teleport (Teleport)
```yaml
Basic Info:
  skillId: teleport
  skillName: Blink
  description: Instantly teleport to target location.
  icon: [teleport_icon]
  rarity: Rare

Targeting & Casting:
  skillType: Teleport
  targeting: MousePosition
  castTime: 0.1
  canMoveWhileCasting: true
  requiresLineOfSight: true

Cooldown & Cost:
  cooldownTime: 8
  manaCost: 20
  healthCost: 0

Damage & Effects:
  damageMultiplier: 0
  flatDamageBonus: 0
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 10
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Movement:
  dashDistance: 0
  dashSpeed: 0
  dashInvulnerable: false
  leaveTrail: false

Visual & Audio:
  castEffectPrefab: [teleport_start]
  effectPrefab: [teleport_end]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [teleport_out]
  impactSound: [teleport_in]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 12
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: false
  screenShakeIntensity: 0
  screenShakeDuration: 0
  useSlowMotion: true
  slowMotionScale: 0.5
  slowMotionDuration: 0.1

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 13. Reflect Shield (Reflect)
```yaml
Basic Info:
  skillId: reflect_shield
  skillName: Mirror Shield
  description: Create a barrier that reflects projectiles back at enemies.
  icon: [reflect_icon]
  rarity: Rare

Targeting & Casting:
  skillType: Reflect
  targeting: Self
  castTime: 0.3
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 14
  manaCost: 25
  healthCost: 0

Damage & Effects:
  damageMultiplier: 0
  flatDamageBonus: 0
  critChanceBonus: 0
  critDamageMultiplier: 1

Area & Range:
  range: 0
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 6
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [reflect_cast]
  effectPrefab: [reflect_burst]
  projectilePrefab: null
  persistentEffectPrefab: [reflect_shield_effect]
  castSound: [reflect_cast]
  impactSound: [reflect_hit]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 13
  animationSpeedMultiplier: 1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.1
  screenShakeDuration: 0.1
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: false
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

### 14. Slash (Melee)
```yaml
Basic Info:
  skillId: slash
  skillName: Power Slash
  description: A powerful melee strike with extended range.
  icon: [slash_icon]
  rarity: Common

Targeting & Casting:
  skillType: Melee
  targeting: Directional
  castTime: 0.2
  canMoveWhileCasting: false
  requiresLineOfSight: false

Cooldown & Cost:
  cooldownTime: 3
  manaCost: 0
  healthCost: 0

Damage & Effects:
  damageMultiplier: 1.5
  flatDamageBonus: 5
  critChanceBonus: 0.15
  critDamageMultiplier: 1.8

Area & Range:
  range: 4
  radius: 0
  coneAngle: 360

Duration & Ticks:
  duration: 0
  tickRate: 1
  maxStacks: 1

Visual & Audio:
  castEffectPrefab: [slash_windup]
  effectPrefab: [slash_hit]
  projectilePrefab: null
  persistentEffectPrefab: null
  castSound: [slash_swing]
  impactSound: [slash_impact]
  loopSound: null

Animation:
  animationTrigger: Skill
  spumAnimationIndex: 14
  animationSpeedMultiplier: 1.1

Screen Effects:
  useScreenShake: true
  screenShakeIntensity: 0.15
  screenShakeDuration: 0.1
  useSlowMotion: false

Status Effects:
  applyBurn: false
  applyFreeze: false
  applyPoison: false
  applyShock: false
  applyStun: false
  statusDuration: 3

Advanced:
  pierceEnemies: true
  explodeOnImpact: false
  explosionRadius: 0
  homing: false
  homingStrength: 5
```

---

## 🎨 How to Use These Templates

### Step 1: Create SkillSO Asset
1. Right-click in Project window
2. Create > ARPG > Skill
3. Name it (e.g., "Skill_Fireball")

### Step 2: Copy Template
1. Find the template above for your skill type
2. Copy all the fields
3. Paste/reference when filling the Inspector

### Step 3: Assign Prefabs
Replace bracketed placeholders like `[fireball_icon]` with actual assets:
- Drag sprites to icon fields
- Drag prefabs to effect fields
- Drag audio clips to sound fields

### Step 4: Adjust Values
- Increase `damageMultiplier` for stronger skills
- Increase `cooldownTime` for balance
- Adjust `duration` for buffs/shields

### Step 5: Test
1. Assign to Player's SkillCaster
2. Enter Play Mode
3. Press skill key (1-4)
4. Check console for errors

---

## 🔧 Validation Checklist

Before testing a skill, verify:

- [ ] `skillId` is unique
- [ ] `skillName` is set
- [ ] `icon` is assigned
- [ ] `skillType` is correct
- [ ] `cooldownTime` > 0
- [ ] `damageMultiplier` appropriate for skill type
- [ ] Required prefabs assigned:
  - [ ] Projectile: `projectilePrefab`
  - [ ] Summon: `summonPrefab`
  - [ ] Shield/Buff: `persistentEffectPrefab`
- [ ] `range` appropriate (not 0)
- [ ] Visual effects assigned

---

## 🐛 Troubleshooting

### "Unknown skill type"
→ Check `skillType` is in the enum and implemented

### "Skill needs projectilePrefab"
→ Assign prefab for Projectile type

### "Skill needs summonPrefab"
→ Assign prefab for Summon type

### Skill does no damage
→ Check `damageMultiplier` > 0  
→ Check `enemyLayer` on SkillCaster  
→ Check enemy has "Enemy" tag

### No visual effects
→ Check `effectPrefab` assigned  
→ Check prefab has visible SpriteRenderer/Particles

### No sound
→ Check `castSound`/`impactSound` assigned  
→ Check AudioListener in scene

---

*Complete SkillSO reference - every field explained!*

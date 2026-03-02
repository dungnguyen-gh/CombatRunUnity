# Skill System Setup Guide

Complete guide to creating and setting up skills for your game.

---

## Overview

The skill system uses **ScriptableObjects** (SkillSO) for data and **SkillCaster** component for execution.

**Files:**
- `SkillSO.cs` - Data container for skills
- `SkillCaster.cs` - Casts skills with cooldowns
- `SkillType.cs` - Enum for skill types

---

## Step 1: Create SkillSO Assets

### A. Create Resources Folder Structure
```
Assets/
└── Resources/
    └── Skills/
```

### B. Create Skill ScriptableObjects

**Right-click → Create → ARPG → Skill**

Create 4 skills for slots 1-4:

#### Skill 1: Spin Attack (Circle AOE)
```yaml
Skill Name: "Spin Attack"
Skill Id: "spin_attack"
Description: "Spin and damage all nearby enemies"
Icon: [Assign a sprite]
Skill Slot: 0 (Key 1)
Cooldown Time: 3
Skill Type: CircleAOE
Damage Multiplier: 1.5
Range: 0 (not used for CircleAOE)
Radius: 2
Duration: 0
Effect Prefab: [Optional - particle effect]
Cast Sound: [Optional - audio clip]
```

#### Skill 2: Fireball (Projectile)
```yaml
Skill Name: "Fireball"
Skill Id: "fireball"
Description: "Launch a piercing fireball"
Icon: [Assign a sprite]
Skill Slot: 1 (Key 2)
Cooldown Time: 5
Skill Type: Projectile
Damage Multiplier: 2
Range: 10
Radius: 0 (not used for Projectile)
Duration: 0
Effect Prefab: [Optional]
Cast Sound: [Optional]
```

#### Skill 3: Meteor Strike (Ground AOE)
```yaml
Skill Name: "Meteor Strike"
Skill Id: "meteor_strike"
Description: "Call down a meteor at target location"
Icon: [Assign a sprite]
Skill Slot: 2 (Key 3)
Cooldown Time: 8
Skill Type: GroundAOE
Damage Multiplier: 3
Range: 0 (uses mouse position)
Radius: 3
Duration: 0
Effect Prefab: [Optional - explosion effect]
Cast Sound: [Optional]
```

#### Skill 4: Shield Wall (Buff)
```yaml
Skill Name: "Shield Wall"
Skill Id: "shield_wall"
Description: "Reduce damage taken by 50% for 5 seconds"
Icon: [Assign a sprite]
Skill Slot: 3 (Key 4)
Cooldown Time: 10
Skill Type: Shield
Damage Multiplier: 0 (no damage)
Range: 0
Radius: 0
Duration: 5
Effect Prefab: [Optional - shield visual]
Cast Sound: [Optional]
```

---

## Step 2: Assign Skills to Player

### Select Player GameObject

```
Player
└── SkillCaster component
    └── Skills:
        ├── Element 0: [Drag SpinAttack skillSO]
        ├── Element 1: [Drag Fireball skillSO]
        ├── Element 2: [Drag MeteorStrike skillSO]
        └── Element 3: [Drag ShieldWall skillSO]
```

**Important:** Array size must be 4 (for keys 1-4).

---

## Step 3: Setup Skill Visuals

### Skill Icons in UI

```
HUDPanel
├── Skill1 (Image)
├── Skill2 (Image)
├── Skill3 (Image)
└── Skill4 (Image)
    └── CooldownOverlay (Image - black semi-transparent)
```

**Assign to UIManager:**
```yaml
UIManager:
├── Skill Icons:
│   ├── Element 0: [Skill1 Image]
│   ├── Element 1: [Skill2 Image]
│   ├── Element 2: [Skill3 Image]
│   └── Element 3: [Skill4 Image]
└── Skill Cooldown Overlays:
    ├── Element 0: [Skill1 CooldownOverlay]
    ├── Element 1: [Skill2 CooldownOverlay]
    ├── Element 2: [Skill3 CooldownOverlay]
    └── Element 3: [Skill4 CooldownOverlay]
```

### Assign Icons

For each SkillX Image:
```
Image Component:
└── Source Image: [Drag corresponding skill icon]
```

---

## Step 4: Setup Projectile Prefab (For Fireball)

If using Projectile skill type:

### Create Projectile Prefab
```
1. Right-click → 2D Object → Sprites → Circle
2. Name: "Projectile"
3. Scale: (0.3, 0.3, 1)
4. Color: Orange/Red
5. Add Components:
   ├── Rigidbody2D (Kinematic, Gravity Scale: 0)
   ├── CircleCollider2D (Is Trigger: true)
   └── Projectile script
6. Tag: "Projectile"
7. Drag to Assets/Prefabs/
```

### Assign to SkillCaster
```
Player → SkillCaster:
└── Projectile Prefab: [Drag Projectile prefab]
```

---

## Step 5: Setup Shield Effect (Optional)

For Shield skill type:

### Create Shield Effect Prefab
```
1. Create Empty GameObject: "ShieldEffect"
2. Add Components:
   ├── SpriteRenderer (Shield visual)
   └── ParticleSystem (Optional sparkles)
3. Drag to Assets/Prefabs/
```

### Assign to SkillCaster
```
Player → SkillCaster:
└── Shield Effect Prefab: [Drag ShieldEffect prefab]
```

---

## Step 6: Configure Skill Animation Indices

If using SPUM:

```
Player → SPUMPlayerBridge:
└── Skill Animation Indices:
    ├── Element 0: 1 (Skill 1 uses ATTACK_List[1])
    ├── Element 1: 1 (Skill 2 uses ATTACK_List[1])
    ├── Element 2: 1 (Skill 3 uses ATTACK_List[1])
    └── Element 3: 1 (Skill 4 uses ATTACK_List[1])
```

**Note:** Index 0 = default attack, Index 1+ = skill variations.

---

## Skill Type Behaviors

| Type | Behavior | Requires |
|------|----------|----------|
| CircleAOE | Damage enemies around player | Radius |
| GroundAOE | Damage at mouse position | Radius |
| Projectile | Launch projectile | projectilePrefab |
| Shield | Damage reduction buff | duration, shieldEffectPrefab |

---

## Testing Skills

1. Press Play
2. Press 1 - Spin Attack should play
3. Check:
   - [ ] Animation plays
   - [ ] Cooldown starts
   - [ ] Cooldown overlay fills
   - [ ] Damage dealt to enemies
   - [ ] Can't cast while on cooldown

---

## Troubleshooting

### "Skill not casting"
- Check SkillCaster.skills array has SkillSO assigned
- Check skillSlot matches array index (0=Key1, 1=Key2, etc.)
- Check cooldown is 0

### "No animation"
- Check SPUMPlayerBridge.skillAnimationIndices has valid index
- Check spumPrefabs is assigned

### "Projectile not spawning"
- Check SkillCaster.projectilePrefab is assigned
- Check Projectile script is on prefab

### "Cooldown UI not showing"
- Check UIManager.skillCooldownOverlays assigned
- Check UIManager.skillCaster assigned

---

## Adding New Skills

1. Create new SkillSO asset
2. Assign unique skillId
3. Set appropriate SkillType
4. Configure stats (damage, range, etc.)
5. Assign icon sprite
6. Add to SkillCaster.skills array
7. Test in game

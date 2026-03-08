# Complete Skill Setup Guide

## Why Skills Don't Show Visuals

When you cast a skill and see nothing, it's almost always because **effect prefabs are not assigned**.

---

## Required vs Optional SkillSO Fields

### ALL Skills Need:
- ✓ **Skill Name** - Display name
- ✓ **Skill ID** - Unique identifier  
- ✓ **Icon** - UI image
- ✓ **Skill Type** - Determines behavior
- ✓ **Cooldown Time** - Seconds before reuse
- ✓ **Effect Prefab** - Visual effect (CRITICAL!)

### Type-Specific Requirements:

| Skill Type | Required Fields | Optional But Recommended |
|------------|-----------------|-------------------------|
| **Projectile** | Projectile Prefab | Cast Effect Prefab, Impact Sound |
| **CircleAOE** | Effect Prefab | Cast Effect Prefab |
| **GroundAOE** | Effect Prefab, Cast Effect Prefab | Impact Sound |
| **Shield** | Persistent Effect Prefab | Cast Sound |
| **Dash** | Effect Prefab (trail) | Cast Sound |
| **Heal** | Effect Prefab | Cast Sound |

---

## Step-by-Step Skill Creation

### Step 1: Create Effect Prefabs

#### For Projectile Skills:
```
1. Create > 2D Object > Sprites > Circle
2. Name: "FireballVisual"
3. Scale: (0.5, 0.5, 0.5)
4. Add Particle System (optional for trail)
5. Add SpriteRenderer: Color = Orange/Red
6. Drag to Project: Assets/Prefabs/Effects/FireballVisual.prefab
```

#### For AOE Skills:
```
1. Create > Effects > Particle System
2. Name: "ExplosionEffect"
3. Configure:
   - Duration: 0.5
   - Start Lifetime: 0.5
   - Start Size: 2
   - Max Particles: 50
   - Shape: Circle, Radius: 2
   - Color over Lifetime: Fade out
4. Drag to Project: Assets/Prefabs/Effects/ExplosionEffect.prefab
```

#### For Shield Skills:
```
1. Create > 2D Object > Sprites > Circle
2. Name: "ShieldBubble"
3. Scale: (2, 2, 2)
4. SpriteRenderer:
   - Color: Blue/Cyan with alpha 0.3
   - Sorting Layer: Effects
5. Drag to Project: Assets/Prefabs/Effects/ShieldBubble.prefab
```

---

### Step 2: Create SkillSO Assets

```
Right-click Project > Create > ARPG > Skill
Name: FireballSkill
```

**Configure Inspector:**

```
Basic Info:
  Skill Id: fireball
  Skill Name: Fireball
  Description: Hurls a ball of fire
  Icon: [Assign fireball icon sprite]
  Skill Slot: 1 (maps to key "2")
  Cooldown Time: 5

Targeting & Casting:
  Skill Type: Projectile
  Targeting: Directional
  Cast Time: 0
  Can Move While Casting: true

Damage & Effects:
  Damage Multiplier: 1.5
  Range: 10
  Radius: 0
  
Visual & Audio:
  Projectile Prefab: [Assign FireballVisual.prefab]
  Effect Prefab: [Assign ExplosionEffect.prefab]
  Cast Sound: [Assign fire sound if you have one]

Screen Effects (Optional Polish):
  Use Screen Shake: true
  Screen Shake Intensity: 0.2
  Screen Shake Duration: 0.1
```

---

### Step 3: Assign Skills to Player

1. Select Player GameObject
2. Find **SkillCaster** component
3. Set Skills array Size: 4
4. Assign your SkillSOs:
   - Element 0: SpinAttack (CircleAOE) → Key 1
   - Element 1: Fireball (Projectile) → Key 2
   - Element 2: Meteor (GroundAOE) → Key 3
   - Element 3: Shield (Shield) → Key 4

---

## Skill Type-Specific Setup

### 1. Projectile Skill Setup

**Required in SkillSO:**
- Skill Type = Projectile
- Projectile Prefab = assigned
- Range = how far it travels
- Damage Multiplier

**Optional for polish:**
- Homing = true (tracks enemies)
- Pierce Enemies = true (goes through multiple)
- Explode On Impact = true (AOE on hit)
- Explosion Radius = 2

**Create Projectile Prefab:**
```
1. Create empty GameObject: "Projectile"
2. Add SpriteRenderer: Circle or arrow sprite
3. Add Rigidbody2D: Dynamic, Gravity Scale = 0
4. Add CircleCollider2D: Is Trigger = true
5. Add Projectile script (included in project)
6. Save as prefab
```

### 2. CircleAOE (Spin Attack) Setup

**Required in SkillSO:**
- Skill Type = CircleAOE
- Effect Prefab = assigned (explosion/spin visual)
- Radius = damage radius (e.g., 3)
- Damage Multiplier

**How it works:**
- Instantly damages all enemies around player
- Spawns effect at player position

### 3. GroundAOE (Meteor) Setup

**Required in SkillSO:**
- Skill Type = GroundAOE
- Cast Effect Prefab = warning indicator at target
- Effect Prefab = impact explosion
- Range = max cast distance
- Radius = explosion radius

**How it works:**
1. Shows warning at mouse position
2. Delay (0.4s built-in)
3. Explosion damages enemies in radius

### 4. Shield Setup

**Required in SkillSO:**
- Skill Type = Shield
- Persistent Effect Prefab = shield visual
- Duration = how long shield lasts

**How it works:**
- Spawns effect attached to player
- Reduces incoming damage by 50%
- Effect destroyed after duration

---

## Quick Skill Test

Add this temporary debug code to test if skills work:

```csharp
// Add to PlayerController.cs temporarily
void Update() {
    // Press T to test skill 0
    if (Keyboard.current.tKey.wasPressedThisFrame) {
        Debug.Log("Testing skill 0");
        skillCaster.TryCastSkill(0);
    }
}
```

If skill still doesn't show:
1. Check Console for validation errors
2. Verify effect prefab has visible SpriteRenderer
3. Check prefab scale is not 0

---

## Common Skill Problems & Solutions

### Problem: "Skill validation failed: Projectile skill needs projectilePrefab!"
**Fix:** Assign a prefab to SkillSO's Projectile Prefab field

### Problem: Skill casts but nothing visible
**Fix:** Assign Effect Prefab (instantiates at cast location)

### Problem: Shield doesn't show
**Fix:** Assign Persistent Effect Prefab (not Effect Prefab)

### Problem: Skills hit nothing
**Fix:** Check Enemy Layer is set to layer with enemies

### Problem: GroundAOE shows no warning
**Fix:** Assign Cast Effect Prefab (warning indicator)

---

## Minimal Working Skill Setup

To get ANY skill working with minimum effort:

1. Create a Sphere or Cube prefab
2. Assign it to SkillSO's Effect Prefab
3. Set Skill Type
4. Assign to SkillCaster
5. Test

Once you see the prefab appear, you know skills work. Then add:
- Particle effects for polish
- Sounds
- Screen shake
- Proper collision

---

## Recommended Default Settings

For testing, use these settings:

```
Cooldown Time: 2 (fast for testing)
Damage Multiplier: 1
Range: 5
Radius: 2 (for AOE)
Duration: 3 (for Shield)
```

Once working, adjust for balance:
```
Cooldown Time: 5-12
Damage Multiplier: 1.2-2.0
```

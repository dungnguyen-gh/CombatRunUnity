# Complete Configuration Guide

## Understanding the Warnings

### Warning 1: "[ShopManager] No available items to stock!"

**Cause:** ShopManager's `availableItems` list is empty.

**Fix:** 
1. Select your ShopManager GameObject in the scene
2. In Inspector, find "Available Items" field
3. Set array size and drag ItemSO assets into the slots

Or create items first:
```
Right-click Project → Create → ARPG → Item
Create at least 3-6 items, then assign them to ShopManager
```

---

### Warning 2: "[SkillCaster] Skill validation failed: Projectile skill needs projectilePrefab!"

**Cause:** You created a SkillSO with type "Projectile" but didn't assign a projectile prefab.

**Fix:**
1. Select your SkillSO asset
2. In Inspector, find "Projectile Prefab" field
3. Assign a prefab (create one if needed)

---

## Quick Fix Steps

### Step 1: Create Shop Items

Create folder: `Assets/Resources/Items/`

Create 3+ items:
```
Right-click → Create → ARPG → Item
```

Example items:
- **Iron Sword**: Weapon, Common, Damage +5, Price 50
- **Steel Sword**: Weapon, Uncommon, Damage +10, Price 100
- **Leather Armor**: Armor, Common, Defense +3, Price 40
- **Health Potion**: Consumable (set isEquippable=false), Price 25

Then assign to ShopManager:
1. Select ShopManager GameObject
2. Set "Available Items" array size to match your items
3. Drag each ItemSO into the slots

---

### Step 2: Create Projectile Prefab

1. Create Sprite in scene (Circle or arrow shape)
2. Set scale to (0.3, 0.3, 0.3)
3. Add components:
   - Rigidbody2D: Dynamic, Gravity Scale = 0
   - CircleCollider2D: Is Trigger = true, Radius = 0.15
   - Projectile script
4. Save as prefab: `Assets/Prefabs/Projectile.prefab`
5. Delete from scene

Assign to SkillCaster:
1. Select Player GameObject
2. Find SkillCaster component
3. Drag Projectile prefab to "Default Projectile Prefab" field

Also assign to individual SkillSO:
1. Select your Fireball SkillSO
2. Drag Projectile prefab to "Projectile Prefab" field

---

### Step 3: Create Skill Effects

Create folder: `Assets/Prefabs/Effects/`

For each skill type, create visual effects:

#### CircleAOE Effect (Spin Attack)
1. Create Particle System
2. Configure:
   - Shape: Circle, Radius 2
   - Duration: 0.5
   - Start Lifetime: 0.3
   - Start Size: 1
   - Color: Orange/Red
3. Save as: `Assets/Prefabs/Effects/SpinEffect.prefab`

#### GroundAOE Effect (Meteor)
1. Create Sprite (Circle with red/orange gradient)
2. Add simple animation or scale up/down script
3. Save as: `Assets/Prefabs/Effects/MeteorEffect.prefab`

#### Shield Effect
1. Create Sprite (Bubble or hexagon shape)
2. Make it semi-transparent (alpha 0.3-0.5)
3. Save as: `Assets/Prefabs/Effects/ShieldEffect.prefab`

Assign these to SkillSO "Effect Prefab" fields.

---

## SkillSO Configuration Explained

### Basic Settings (All Skills Need These)

| Field | What It Does | Example Values |
|-------|--------------|----------------|
| **Skill Name** | Display name | "Fireball", "Spin Attack" |
| **Skill ID** | Unique identifier | "fireball", "spin_attack" |
| **Icon** | UI image | Assign sprite |
| **Skill Slot** | Which key (0=1, 1=2, etc.) | 0, 1, 2, 3 |
| **Cooldown Time** | Seconds before reuse | 5, 8, 12 |
| **Skill Type** | Behavior type | Projectile, CircleAOE, etc. |

### Combat Settings

| Field | What It Does | When to Use |
|-------|--------------|-------------|
| **Damage Multiplier** | Multiplies base damage | 1.5 = 150% damage |
| **Flat Damage Bonus** | Added after multiplier | +10 flat damage |
| **Range** | How far skill reaches | 10 for fireball, 3 for spin |
| **Radius** | AOE size | 2 for circle attacks |

### Type-Specific Settings

#### For Projectile Skills:
- **Projectile Prefab**: REQUIRED - The visual projectile
- **Homing**: Check if it should track enemies
- **Pierce Enemies**: Check to go through multiple enemies
- **Explode On Impact**: Check for AOE explosion

#### For Shield Skills:
- **Duration**: How long shield lasts (seconds)
- **Persistent Effect Prefab**: Visual shield bubble

#### For AOE Skills:
- **Effect Prefab**: Visual explosion/impact
- **Cast Effect Prefab**: Warning/telegraph effect
- **Tick Rate**: For DoT effects (damage per second)

### Visual & Audio

| Field | Purpose |
|-------|---------|
| **Effect Prefab** | Visual effect at impact |
| **Cast Effect Prefab** | Effect when casting starts |
| **Cast Sound** | Audio when skill used |
| **Impact Sound** | Audio when skill hits |

### Screen Effects (Polish)

| Field | Effect |
|-------|--------|
| **Use Screen Shake** | Camera shakes on cast |
| **Shake Intensity** | How much shake (0.1-0.5) |
| **Shake Duration** | How long shake lasts |
| **Use Slow Motion** | Time slows briefly |
| **Slow Motion Scale** | 0.5 = 50% speed |

---

## Complete Skill Setup Example

### Skill 1: Spin Attack (CircleAOE)
```
Skill Name: Spin Attack
Skill ID: spin_attack
Icon: [Assign spin icon]
Skill Slot: 0
Cooldown Time: 6
Skill Type: CircleAOE
Damage Multiplier: 1.2
Range: 3
Radius: 3
Duration: 0
Effect Prefab: SpinEffect prefab
Cast Sound: [Assign swoosh sound]
Use Screen Shake: true
Shake Intensity: 0.2
Shake Duration: 0.15
```

### Skill 2: Fireball (Projectile)
```
Skill Name: Fireball
Skill ID: fireball
Icon: [Assign fireball icon]
Skill Slot: 1
Cooldown Time: 5
Skill Type: Projectile
Damage Multiplier: 1.5
Range: 10
Projectile Prefab: Projectile prefab
Homing: false
Pierce Enemies: false
Explode On Impact: true
Explosion Radius: 2
Effect Prefab: ExplosionEffect prefab
Cast Sound: [Assign fire sound]
Impact Sound: [Assign explosion sound]
```

### Skill 3: Meteor (GroundAOE)
```
Skill Name: Meteor Strike
Skill ID: meteor_strike
Icon: [Assign meteor icon]
Skill Slot: 2
Cooldown Time: 8
Skill Type: GroundAOE
Damage Multiplier: 2.0
Range: 15
Radius: 4
Duration: 0
Cast Effect Prefab: MeteorWarning prefab
Effect Prefab: MeteorImpact prefab
Cast Sound: [Assign rumble sound]
Use Screen Shake: true
Shake Intensity: 0.4
Shake Duration: 0.3
Use Slow Motion: true
Slow Motion Scale: 0.7
Slow Motion Duration: 0.2
```

### Skill 4: Shield (Shield)
```
Skill Name: Shield Wall
Skill ID: shield_wall
Icon: [Assign shield icon]
Skill Slot: 3
Cooldown Time: 12
Skill Type: Shield
Duration: 5
Persistent Effect Prefab: ShieldEffect prefab
Cast Sound: [Assign shield sound]
```

---

## Dynamic System Configuration Checklist

### 1. Player Setup
```
Player GameObject
├── Rigidbody2D (Kinematic)
├── PlayerController
│   ├── Input Actions: GameControls
│   ├── Enemy Layer: Enemies
│   └── Attack Point: [Assigned]
├── SkillCaster
│   ├── Skills: [4 SkillSOs assigned]
│   ├── Cast Point: [Assigned]
│   ├── Enemy Layer: Enemies
│   └── Default Projectile Prefab: [Assigned]
└── SPUMPlayerBridge (if using SPUM)
```

### 2. Managers Setup
```
Managers (Empty parent)
├── GameManager
│   ├── Spawn Points: [4+ points assigned]
│   ├── Enemy Prefabs: [Enemy prefab assigned]
│   ├── Boss Prefab: [Boss prefab assigned]
│   └── Enemy Container: [Empty GameObject assigned]
├── UIManager
│   ├── HUD Panel: [Assigned]
│   ├── Skill Icons: [4 Images assigned]
│   ├── Skill Cooldown Overlays: [4 Images assigned]
│   └── Inventory/Shop/Pause Panels: [Assigned]
├── InventoryManager
│   └── Max Slots: 20
├── ShopManager
│   ├── Available Items: [3+ ItemSOs assigned]
│   └── Shop Slots: 6
└── DamageNumberManager
    └── Damage Number Prefab: [Assigned]
```

### 3. Prefabs Setup
```
Assets/Prefabs/
├── Enemy.prefab
│   ├── SpriteRenderer
│   ├── Rigidbody2D (Kinematic)
│   ├── Collider2D
│   └── Enemy script
├── Projectile.prefab
│   ├── SpriteRenderer
│   ├── Rigidbody2D (Dynamic, Gravity=0)
│   ├── Collider2D (IsTrigger)
│   └── Projectile script
├── GoldPickup.prefab
│   ├── SpriteRenderer
│   └── Collider2D (IsTrigger)
├── ItemPickup.prefab
│   ├── SpriteRenderer
│   └── Collider2D (IsTrigger)
└── Effects/
    ├── SpinEffect.prefab
    ├── ExplosionEffect.prefab
    ├── ShieldEffect.prefab
    └── MeteorEffect.prefab
```

### 4. Resources Folder
```
Assets/Resources/
├── Items/
│   ├── IronSword.asset
│   ├── SteelSword.asset
│   ├── LeatherArmor.asset
│   └── HealthPotion.asset
├── Sets/ (optional)
│   └── DragonSlayerSet.asset
└── Skills/
    ├── SpinAttack.asset
    ├── Fireball.asset
    ├── MeteorStrike.asset
    └── ShieldWall.asset
```

---

## Testing Each System

### Test Shop
1. Enter Play mode
2. Check console - should NOT see "No available items to stock!"
3. Open Shop UI (if implemented)
4. Should see items displayed with prices

### Test Skills
1. Enter Play mode
2. Press 1, 2, 3, 4 keys
3. Check console - should NOT see validation errors
4. Each skill should:
   - Play visual effect
   - Go on cooldown (UI shows fill)
   - Deal damage to enemies

### Test All At Once
Create a test scenario:
1. Place Player in scene
2. Place 3 Enemy prefabs nearby
3. Enter Play mode
4. Kill enemies with skills
5. Pick up gold/items
6. Open inventory
7. Equip items
8. Test shop (if NPC exists)

---

## Common Configuration Mistakes

| Mistake | Result | Fix |
|---------|--------|-----|
| No items in ShopManager | Empty shop | Add ItemSOs to Available Items |
| No projectile prefab | Validation error | Create and assign prefab |
| No effect prefabs | Skills invisible | Create particle effects |
| Wrong layer masks | Skills don't hit | Set Enemy layer correctly |
| Missing SkillCaster reference | Null reference | Ensure SkillCaster is on Player |
| Empty skills array | Can't cast | Assign 4 SkillSOs to SkillCaster |

---

## Quick Reference: Required vs Optional

### Required for All Skills:
- Skill Name, ID, Icon
- Skill Type
- Cooldown Time
- Damage Multiplier

### Required for Projectile:
- Projectile Prefab

### Required for AOE:
- Effect Prefab (recommended)

### Optional (for polish):
- Screen Shake
- Slow Motion
- Sounds
- Status Effects

---

## Still Getting Warnings?

If you see warnings after setup:

1. **"No available items"** → Check ShopManager has items in scene
2. **"Projectile needs prefab"** → Check SkillSO has projectile assigned
3. **"Skill validation failed"** → Read the specific error, check that field
4. **"No Main Camera"** → Set Camera tag to "MainCamera"
5. **"SPUM_Prefabs not found"** → Check Use SPUM checkbox and assignments

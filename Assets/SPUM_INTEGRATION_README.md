# SPUM Integration Guide

This guide explains how to integrate the SPUM (2D Survival Character) asset package with CombatRun's systems, including recent bug fixes, validation improvements, and the Unity Input System Package.

---

## 📋 Overview

SPUM uses a different approach than standard 2D sprites:
- **Multiple body parts** as separate sprites (Head, Body, Arms, Weapons, etc.)
- **Animator Override Controller** for animation management
- **Specific animation states**: IDLE, MOVE, ATTACK, DAMAGED, DEBUFF, DEATH, OTHER
- **Scale-based flipping** for facing direction (not SpriteRenderer.flipX)

Our integration provides:
- Automatic animation syncing between PlayerController and SPUM
- Equipment visual swapping (weapons, armor)
- Damage flash effects (VFX-based, recommended)
- Shop preview support
- Skill casting animations
- **Recent**: Animation index validation to prevent crashes

---

## 🚀 Quick Setup

### Prerequisites

Ensure you have assigned the Input Action Asset to PlayerController:
1. Select your Player GameObject
2. In PlayerController, drag `GameControls.inputactions` to the **Input Actions** field
3. See `INPUT_SYSTEM_SETUP.md` for details

### Step 1: Prepare Your SPUM Character

1. **Create or select a SPUM character prefab**
   - Go to `Assets/SPUM/Resources/Addons/BasicPack/2_Prefab/`
   - Choose a character (e.g., `Human/SPUM_*.prefab`)
   - Drag it into your scene

2. **Add Required Components**
   Add these components to the root GameObject:
   - `Rigidbody2D` (Kinematic, Gravity Scale: 0)
   - `CircleCollider2D` or `BoxCollider2D`
   - `PlayerController` script (uses Unity Input System)
   - `SkillCaster` script
   - `ComboSystem` script (optional)
   - `SPUMPlayerBridge` script
   - `SPUMEquipmentManager` script

### Step 2: Configure PlayerController

```
PlayerController
├── Input
│   └── inputActions: GameControls (drag from Assets/InputSystem/)
├── useSPUM: ☑️ (CHECK THIS!)
├── spumBridge: SPUMPlayerBridge (drag from same object)
├── spumEquipment: SPUMEquipmentManager (drag from same object)
├── spriteRenderer: LEAVE EMPTY (not used for SPUM)
├── animator: LEAVE EMPTY (not used for SPUM)
└── ... (other settings)
```

**Important Settings:**
| Field | Value | Notes |
|-------|-------|-------|
| `useSPUM` | ☑️ | Enable SPUM mode |
| `spumBridge` | Assigned | Reference to SPUMPlayerBridge |
| `spumEquipment` | Assigned | Reference to SPUMEquipmentManager |
| `spriteRenderer` | Empty | Not used for SPUM |
| `animator` | Empty | Not used for SPUM |
| `useVFXDamageFlash` | ☑️ | Recommended for performance |
| `flashAllSpriteRenderers` | ☐ | Legacy method (slow, avoid) |

### Step 3: Configure SPUMPlayerBridge

| Field | Description |
|-------|-------------|
| `spumPrefabs` | Auto-found; references SPUM_Prefabs on child object |
| `spumAnimator` | Auto-found; references the Animator |
| `idleAnimationIndex` | Which idle animation to play (0 = first) |
| `moveAnimationIndex` | Which move animation to play |
| `attackAnimationIndex` | Which attack animation to play |
| `skillAnimationIndices` | Array of 4 indices for skills 1-4 |

**Recent Improvements:**
- ✓ Added automatic bounds checking for animation indices
- ✓ Added validation to prevent crashes from invalid indices
- ✓ Added initialization guard to prevent double-init

**Validation:** If you see "Invalid animation index" warnings in console, check that your indices are within bounds of the animation lists in SPUM_Prefabs.

### Step 4: Configure SPUMEquipmentManager

This component auto-finds equipment slots based on naming conventions:

| Part Name | Expected GameObject Name |
|-----------|-------------------------|
| Helmet | Contains "Helmet" |
| Armor | Contains "Armor" or "Body" |
| Right Weapon | Contains "Weapon" (not "left") |
| Left Weapon | Contains "Weapon_Left" |
| Shield | Contains "Shield" |
| Back | Contains "Back" |

**How it works:**
- The manager searches all SpriteRenderers in the SPUM prefab
- Matches are made based on GameObject names (case-insensitive)
- If auto-find fails, you can manually assign in Inspector

**Recent Fix:** Removed duplicate helmet check and improved null validation.

**Fixer Agent Improvements:**
- ✓ Added null checks for GetComponent<SpriteRenderer>() calls
- ✓ Improved error handling for missing equipment parts

### Step 5: Set Up ItemSO for SPUM

When creating items for SPUM:

```csharp
// In ItemSO Inspector:
itemName: "Steel Sword"
weaponType: Sword  // IMPORTANT for mastery tracking
itemSprite: [Drag SPUM weapon sprite here]
// e.g., Assets/SPUM/Resources/Addons/Legacy/0_Unit/0_Sprite/6_Weapons/0_Sword/Sword_1.png
```

**Weapon Type Mapping:**
| SPUM Folder | WeaponType Enum |
|-------------|-----------------|
| 0_Sword | Sword |
| 1_Axe | Axe |
| 4_Spear | Spear |
| 6_Hammer | Mace |
| 5_Wand | None (use Dagger for small weapons) |

---

## 🎬 Animation System

### How Animation Syncing Works

```
PlayerController detects input
        ↓
SPUMPlayerBridge.UpdateAnimationState()
        ↓
spumPrefabs.PlayAnimation(PlayerState, index)
        ↓
SPUM Animator Override Controller applies animation
```

### Available PlayerStates

| State | Trigger Parameter | Used For |
|-------|-------------------|----------|
| IDLE | Bool "1_Move" = false | Standing still |
| MOVE | Bool "1_Move" = true | Walking/Running |
| ATTACK | Trigger "2_Attack" | Melee attacks |
| DAMAGED | Trigger "3_Damaged" | Taking damage |
| DEATH | Trigger "4_Death" | Dying |
| DEBUFF | Bool "5_Debuff" | Stunned/CC |
| OTHER | Trigger "6_Other" | Special actions |

### Adding Custom Skill Animations

1. **Create animation clips** in SPUM format
2. **Add to SPUM_Prefabs lists:**
   - For melee skills: Add to `ATTACK_List`
   - For magic skills: Add to `OTHER_List`
3. **Set indices in SPUMPlayerBridge:**
   - `skillAnimationIndices[0]` = Skill 1 animation index
   - `skillAnimationIndices[1]` = Skill 2 animation index
   - etc.

**Important:** Recent validation ensures indices are checked before use. If an index is out of bounds, a warning is logged and the animation won't play (instead of crashing).

### Skill Casting with SPUM

When skills are cast, the PlayerController automatically triggers SPUM animations:

```csharp
// In PlayerController.TryCastSkill()
if (skillCaster.CastSkill(index)) {
    OnSkillCast?.Invoke(index);
    
    // Trigger SPUM skill animation
    if (useSPUM && spumBridge != null) {
        spumBridge.PlaySkillAnimation(index);
    }
}
```

**Configuration:**
- Skill 1 (Spin Attack): `skillAnimationIndices[0]` = 1 (default skill anim)
- Skill 2 (Meteor): `skillAnimationIndices[1]` = 1
- Skill 3 (Fireball): `skillAnimationIndices[2]` = 1  
- Skill 4 (Shield): `skillAnimationIndices[3]` = 1

---

## 👕 Equipment System

### Visual Equipment Changes

When you equip an item, the system:

1. **Weapon Equip:**
   ```csharp
   // Right hand gets the weapon sprite
   rightWeaponTransform.GetComponent<SpriteRenderer>().sprite = newSprite;
   
   // Left hand may also get sprite for dual-wield weapons
   if (weaponType == Dagger || weaponType == Axe)
       leftWeaponTransform.GetComponent<SpriteRenderer>().sprite = newSprite;
   ```

2. **Armor Equip:**
   ```csharp
   // Body armor sprite changed
   armorTransform.GetComponent<SpriteRenderer>().sprite = armorSprite;
   
   // Helmet sprite changed (if provided)
   helmetTransform.GetComponent<SpriteRenderer>().sprite = helmetSprite;
   ```

**Recent Fix:** SetEquipmentColor now properly handles all slots including Helmet.

### Shop Preview with SPUM

The shop preview system works seamlessly:

```csharp
// When previewing in shop:
ShopManager.PreviewItem(item)
    ↓
player.SetWeaponVisual(item.itemSprite)
    ↓
// PlayerController checks useSPUM
if (useSPUM && spumEquipment != null)
    spumEquipment.EquipWeapon(sprite, weaponType)
    ↓
// Visual changes immediately, no stats applied until purchase

// When ending preview:
ShopManager.EndPreview()
    ↓
// Restores original equipment visuals through same path
```

**Note:** ShopManager uses PlayerController's SetWeaponVisual/SetArmorVisual which automatically handle both SPUM and legacy modes.

---

## 🗡️ Weapon Casting & Skills

### Cast Point for SPUM

For projectile skills, you may want to adjust the cast point:

```csharp
// In PlayerController or SkillCaster, set castPoint to:
// - Right hand position for most weapons
// - Center of body for magic/ground skills

// Example setup in Unity Editor:
Player
└── CastPoint (Empty GameObject)
    └── Position at character's right hand
```

**Tip:** Create a child GameObject named "CastPoint" positioned at the character's hand and assign it to PlayerController's `attackPoint` field.

### Skill Animation Variations

Different weapon types can have different skill animations:

| Weapon Type | Skill 1 Animation | Skill 2 Animation |
|-------------|-------------------|-------------------|
| Sword | Slash combo | Ground slam |
| Axe | Heavy swing | Spin attack |
| Spear | Thrust | Charge |

**Setup:**
1. Add different animations to SPUM_Prefabs.ATTACK_List
2. Set `skillAnimationIndices` based on equipped weapon type
3. Or create a script that changes indices when weapon changes

---

## 🎯 Facing Direction

We use **rotation Y** to flip the character (more reliable than scale):

```csharp
// SPUMPlayerBridge.UpdateFacingDirection()
if (facing.x > 0.1f)
    spumPrefabs.transform.rotation = Quaternion.Euler(0, 180, 0); // Face LEFT
else if (facing.x < -0.1f)
    spumPrefabs.transform.rotation = Quaternion.Euler(0, 0, 0);   // Face RIGHT
```

### Why Rotation Instead of Scale?

| Method | Pros | Cons |
|--------|------|------|
| **Rotation Y** | Preserves colliders, better for physics | May need to adjust child object positions |
| **Scale X** | Simple | Can invert colliders, mess up physics |

### Setup Notes
- Character should be designed facing **right** by default (0 degrees)
- When facing left, rotate 180 degrees around Y axis
- Ensure all child objects are centered properly

---

## 💥 Damage Flash Effect

When the player takes damage, a VFX is spawned for optimal performance.

### How It Works (VFX Method - Recommended)

```csharp
// PlayerController.DamageFlash()
if (useVFXDamageFlash && damageFlashVFX != null) {
    GameObject flash = Instantiate(damageFlashVFX, transform.position, Quaternion.identity);
    flash.transform.SetParent(transform);
    Destroy(flash, damageFlashDuration);
}
```

### Creating a Damage Flash VFX

1. **Create an empty GameObject** named "DamageFlashVFX"
2. **Add a SpriteRenderer** or **ParticleSystem**
3. **Set the sprite** to a white/red glow or flash effect
4. **Set Sorting Order** high (e.g., 100) to appear above character
5. **Make it a prefab** and assign to PlayerController

**Simple VFX Example:**
```
DamageFlashVFX (GameObject)
├── SpriteRenderer
│   ├── Sprite: WhiteCircle or GlowSprite
│   ├── Color: Red with alpha 0.5
│   ├── SortingOrder: 100
│   └── Scale: 2x player size
└── Destroy after 0.1s (script or animation)
```

### Alternative Methods

| Method | When to Use | Performance |
|--------|-------------|-------------|
| **VFX** (Recommended) | Always | ⭐⭐⭐ Excellent |
| `flashAllSpriteRenderers` | Quick test only | ⭐ Poor (loops all sprites) |
| Single `spriteRenderer` | Legacy single-sprite | ⭐⭐ Good |

### Configuration

| Field | Description |
|-------|-------------|
| `useVFXDamageFlash` | Enable VFX-based flash (recommended) |
| `damageFlashVFX` | Prefab to spawn for damage flash |
| `damageFlashDuration` | How long the flash lasts (default: 0.1s) |
| `flashAllSpriteRenderers` | Legacy: Flash all sprites (slow, avoid) |

---

## ⚔️ Combo System with SPUM

The ComboSystem spawns combo text above the player:

```csharp
void ShowComboEffect() {
    // Combo text spawns at transform.position + comboTextOffset
    GameObject effect = Instantiate(comboEffectPrefab, 
        transform.position + comboTextOffset, Quaternion.identity);
}
```

**For SPUM:** Adjust `comboTextOffset` in ComboSystem to position text above the character's head (default is 1.5 units up).

---

## 📦 Folder Structure

```
Assets/
├── SPUM/                              # SPUM asset package
│   ├── Core/
│   │   ├── Basic_Resources/Animator/  # Animation controllers
│   │   └── Script/Data/               # SPUM scripts
│   └── Resources/Addons/              # Character parts & prefabs
├── Scripts/
│   ├── SPUM/                          # Our integration scripts
│   │   ├── SPUMPlayerBridge.cs        # Animation bridge
│   │   └── SPUMEquipmentManager.cs    # Equipment visuals
│   └── ...                            # Other game systems
```

---

## 🔧 Troubleshooting

### "SPUM_Prefabs not found!"
**Solution:** 
- Make sure your SPUM character prefab has `SPUM_Prefabs` component
- It's usually on the root or first child of the prefab

### Animations not playing
**Solution:**
1. Check that `spumPrefabs.allListsHaveItemsExist()` returns true
2. Call `spumPrefabs.PopulateAnimationLists()` if needed
3. Verify animation clips are assigned to the lists in SPUM_Prefabs
4. **NEW:** Check console for "Invalid animation index" warnings

### "Invalid animation index" warning
**Solution:**
- Recent fix adds validation - check that your indices are valid
- Verify index is within bounds of the animation list
- Check SPUM_Prefabs.ATTACK_List has entries at your indices

### Equipment not showing
**Solution:**
1. Check that equipment part names match expected patterns
2. Manually assign transforms in SPUMEquipmentManager Inspector
3. Verify itemSprite is assigned in ItemSO

### Damage flash not working
**Solution:**
1. **Recommended:** Enable `useVFXDamageFlash` and assign a `damageFlashVFX` prefab
2. Create a simple VFX prefab with a SpriteRenderer (glow/flash sprite)
3. Alternative: Enable `flashAllSpriteRenderers` (slower)

### VFX not showing
**Solution:**
1. Check that `damageFlashVFX` prefab is assigned
2. Ensure VFX has SpriteRenderer with high SortingOrder (e.g., 100)
3. Check that VFX is not spawning inside the character (adjust Z position)

### Character facing wrong direction
**Solution:**
- SPUM uses rotation Y (not SpriteRenderer.flipX)
- Check that facing direction logic matches your sprite orientation
- Character should face RIGHT by default (0 degrees)

### Attack animations don't deal damage
**Solution:**
- Add Animation Events to your SPUM attack animations
- Create event at the "hit frame" that calls `OnAttackAnimationHit()`
- Or handle damage in PlayerController based on animation state

### Skills not showing animation
**Solution:**
- Check `skillAnimationIndices` array has valid indices
- Ensure SPUM_Prefabs.ATTACK_List has animations at those indices
- Verify skills are being cast (check cooldowns)
- Check console for validation warnings

### Shop preview not working
**Solution:**
- Check that ShopManager.player reference is assigned
- Verify item.itemSprite is not null
- Ensure SPUMEquipmentManager is properly configured

---

## 🎨 Design Tips

### Animation Timing
- SPUM animations should have clear "impact frames"
- Add animation events at damage frames
- Keep attack animations snappy (0.3-0.5s) for game feel

### Equipment Positioning
- Weapon sprites should align with hand position in idle animation
- Armor sprites should match body proportions
- Test equipment changes at different animation states

### Performance
- SPUM characters have many SpriteRenderers
- Use object pooling for projectiles/effects
- Consider LOD for very large enemy counts
- **NEW:** SpriteRenderer caching is now automatic in SPUMEquipmentManager

---

## 🔄 Migration from Regular Sprites to SPUM

If you have existing code using regular sprites:

1. **Check the `useSPUM` toggle** on PlayerController
2. **Add SPUM components** to your player prefab
3. **Update item sprites** to reference SPUM weapon/armor sprites
4. **Test animations** - ensure all states work correctly

The code automatically handles both cases:
```csharp
if (useSPUM && spumBridge != null)
    spumBridge.PlayAttackAnimation();
else
    animator.SetTrigger("Attack");
```

---

## 🎨 Creating Custom SPUM Characters

### Using SPUM Editor
1. Window → SPUM → SPUM Editor
2. Customize your character
3. Save as prefab
4. Add integration components as described above

### Manual Prefab Creation
1. Create empty GameObject
2. Add child objects for each body part
3. Add SpriteRenderer to each part
4. Add SPUM_Prefabs component
5. Assign Animator with SPUMController
6. Add our integration components

---

## 📚 Code Reference

### Key Methods

**SPUMPlayerBridge:**
```csharp
PlayIdleAnimation()      // Play idle state
PlayMoveAnimation()      // Play move state  
PlayAttackAnimation()    // Play attack state
PlaySkillAnimation(int)  // Play skill by index (0-3)
PlayDamagedAnimation()   // Play damaged state
PlayDeathAnimation()     // Play death state
SetAnimationSpeed(float) // Adjust playback speed

// Recent additions:
IsValidAnimationIndex(state, index)  // Check if index is valid
ValidateAnimationIndices()           // Validate all configured indices
```

**SPUMEquipmentManager:**
```csharp
EquipWeapon(Sprite, WeaponType)  // Equip weapon visual
UnequipWeapon()                  // Remove weapon visual
EquipArmor(Sprite)               // Equip armor visual
UnequipArmor()                   // Remove armor visual
EquipShield(Sprite)              // Equip shield
PreviewEquipment(slot, sprite)   // Preview without equipping
EndPreview()                     // End preview

// Recent improvements:
// - Better null validation
// - Fixed duplicate helmet check
// - Improved SpriteRenderer caching
// - Added null checks for GetComponent<SpriteRenderer>()
```

**PlayerController (SPUM-aware):**
```csharp
SetWeaponVisual(Sprite)    // Works with SPUM or legacy
SetArmorVisual(Sprite)     // Works with SPUM or legacy
TakeDamage(int)            // Handles SPUM damage flash (VFX-based)
```

---

## ✅ Testing Checklist

### Input System
- [ ] Input Actions assigned to PlayerController
- [ ] Player moves with WASD/Arrows
- [ ] Attack works with Space/Left Click
- [ ] Skills trigger with 1-4 keys
- [ ] Inventory toggles with I
- [ ] Pause works with Escape

### SPUM
- [ ] SPUM animations play correctly
- [ ] Animation indices are validated (no warnings)
- [ ] Character faces correct direction
- [ ] Equipment swaps work
- [ ] Shop preview works
- [ ] Damage flash VFX appears
- [ ] Skills trigger animations
- [ ] No console errors

---

*For Input System setup, see INPUT_SYSTEM_SETUP.md*
*For questions about SPUM itself, refer to the SPUM documentation and sample scenes.*
*For bug fixes and updates, see OPTIMIZATION_UPDATE_SUMMARY.md*

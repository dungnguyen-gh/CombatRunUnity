# Complete Fix Guide - All Issues Resolved

## Summary of Your Issues & Solutions

| Issue | Root Cause | Solution |
|-------|-----------|----------|
| Skills invisible | Missing effect prefabs | Assign prefabs to SkillSO |
| Camera laggy | SmoothDamp too slow | Use Lerp + increase speed |
| Enemies pushed | Rigidbody Dynamic | Change to Kinematic |
| UI not updating | Missing references | Assign in UIManager inspector |

---

## FIX 1: Skills Not Showing (CRITICAL)

### The Problem
You assigned default projectile/shield prefabs to SkillCaster, but **each SkillSO also needs its own effect prefab assigned**.

### Quick Fix (2 minutes)

1. **Create a simple sphere prefab:**
   - Create > 3D Object > Sphere
   - Remove Collider
   - Scale: (0.5, 0.5, 0.5)
   - Drag to Project: `Assets/Prefabs/DebugEffect.prefab`

2. **Assign to ALL your SkillSOs:**
   - Select Fireball SkillSO
   - Effect Prefab: DebugEffect
   - Projectile Prefab: DebugEffect (for projectile type)
   - Repeat for all 4 skills

3. **Test:** Press 1/2/3/4 - you should see spheres appear

### Proper Fix (10 minutes)

Create proper effect prefabs for each skill type:

#### For Projectile (Fireball):
```
1. Create Sprite (Circle)
2. Scale: (0.3, 0.3, 0.3)
3. SpriteRenderer: Orange/Red color
4. Add Trail (optional)
5. Save: Assets/Prefabs/Effects/Fireball.prefab
6. Assign to SkillSO > Projectile Prefab
```

#### For AOE (Spin/Meteor):
```
1. Create Particle System
2. Duration: 0.5
3. Start Lifetime: 0.5
4. Start Size: 2
5. Max Particles: 50
6. Shape: Circle
7. Save: Assets/Prefabs/Effects/Explosion.prefab
8. Assign to SkillSO > Effect Prefab
```

#### For Shield:
```
1. Create Sprite (Circle)
2. Scale: (2, 2, 2)
3. SpriteRenderer: Blue, Alpha 0.3
4. Save: Assets/Prefabs/Effects/Shield.prefab
5. Assign to SkillSO > Persistent Effect Prefab
```

---

## FIX 2: Camera Lagging

### The Problem
`Vector3.SmoothDamp` with `1f / smoothSpeed` causes heavy lag.

### The Fix

1. **Select Main Camera**
2. **CameraFollow settings:**
```
smoothSpeed: 15 (was 5)
useLookAhead: false (uncheck)
```

Or replace the entire CameraFollow.cs with the updated version that uses Lerp instead of SmoothDamp.

### Alternative Quick Fix
In CameraFollow.cs, change line 76:
```csharp
// FROM (laggy):
transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / smoothSpeed);

// TO (responsive):
transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
```

---

## FIX 3: Enemies Get Pushed

### The Problem
Enemy Rigidbody2D is set to "Dynamic" which means it gets pushed by collisions.

### The Fix

1. **Select Enemy prefab**
2. **Rigidbody2D component:**
```
Body Type: Kinematic  (NOT Dynamic!)
```

3. **Ensure movement uses MovePosition:**
In Enemy.cs, the movement should be:
```csharp
// Kinematic movement - won't be pushed
rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
```

NOT:
```csharp
// This pushes other objects
rb.AddForce(direction * moveSpeed);
```

### Alternative: Make Enemies Heavy
If you need Dynamic for physics:
```
Body Type: Dynamic
Mass: 1000
Linear Drag: 5
```

---

## FIX 4: UI Not Reflecting Status

### The Problem
UIManager inspector fields are empty or events not connected.

### The Fix

#### Step 1: Assign UI References
Select **UIManager** GameObject, assign ALL these fields:

```
HUD Panel: HUDPanel (GameObject)
Health Text: HealthText (TextMeshProUGUI)
Health Slider: HealthSlider (Slider)
Gold Text: GoldText (TextMeshProUGUI)

Skill Icons: [4 Image components]
  - SkillIcon1
  - SkillIcon2
  - SkillIcon3
  - SkillIcon4

Skill Cooldown Overlays: [4 Images with Filled type]
  - CooldownOverlay1
  - CooldownOverlay2
  - CooldownOverlay3
  - CooldownOverlay4

Skill Cooldown Texts: [4 TextMeshProUGUI]
  - CooldownText1
  - CooldownText2
  - CooldownText3
  - CooldownText4

Inventory Panel: InventoryPanel (GameObject)
Shop Panel: ShopPanel (GameObject)
Pause Panel: PausePanel (GameObject)

Notification Prefab: [Assign a text prefab]
Notification Parent: [Assign a transform for positioning]
```

#### Step 2: Verify Event Connection
Add this debug code temporarily to PlayerController:
```csharp
void Start() {
    // Test events fire
    Invoke(nameof(TestEvents), 2f);
}

void TestEvents() {
    Debug.Log("Testing events...");
    OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);
    OnGoldChanged?.Invoke(gold);
}
```

If you see "Testing events..." in console but UI doesn't update, UIManager isn't subscribed.

#### Step 3: Ensure Correct Order
Both PlayerController and UIManager should use this pattern:
```csharp
void Awake() {
    // Set up singletons
}

void Start() {
    // Wait one frame for everything to exist
    StartCoroutine(Initialize());
}

IEnumerator Initialize() {
    yield return null; // Wait one frame
    
    // Now find references and subscribe
    FindReferences();
    SubscribeToEvents();
}
```

---

## COMPLETE SETUP CHECKLIST

### Player Setup
```
□ Player has Rigidbody2D (Kinematic)
□ Player has PlayerController
□ Player has SkillCaster
  □ Skills array has 4 SkillSOs
  □ Cast Point assigned
  □ Enemy Layer assigned
□ Player has SPUMPlayerBridge (if using SPUM)
```

### SkillSO Setup
```
For EACH of the 4 skills:
□ Skill Name assigned
□ Skill Type selected
□ Cooldown Time > 0
□ Effect Prefab assigned (CRITICAL!)
□ Type-specific prefab assigned:
  - Projectile: Projectile Prefab
  - Shield: Persistent Effect Prefab
  - AOE: Effect Prefab
```

### Camera Setup
```
□ Main Camera tagged "MainCamera"
□ CameraFollow component added
□ smoothSpeed: 15
□ useLookAhead: false
□ Target assigned or auto-finds Player
```

### Enemy Setup
```
□ Enemy prefab has Rigidbody2D: Kinematic
□ Enemy has "Enemy" tag
□ Enemy on "Enemies" layer
□ Movement uses MovePosition or transform
```

### UI Setup
```
□ Canvas exists with correct settings
□ UIManager GameObject exists
□ ALL fields assigned in UIManager inspector
□ EventSystem with Input System UI Module
□ Panels start inactive (hidden)
```

### Managers Setup
```
□ GameManager: spawn points, enemy prefabs assigned
□ UIManager: all UI references assigned
□ InventoryManager: exists
□ ShopManager: available items assigned
□ DamageNumberManager: prefab assigned
```

---

## TESTING EACH SYSTEM

### Test Skills
1. Enter Play mode
2. Press 1, 2, 3, 4
3. Check Console:
   - No "validation failed" warnings
   - No null reference errors
4. Visual check:
   - See effects spawn
   - Cooldown UI fills

### Test Camera
1. Move player with WASD
2. Camera should follow immediately
3. No delay or "floaty" feeling
4. Check CameraFollow settings if laggy

### Test Enemies
1. Walk into enemy
2. Enemy should NOT move
3. Player should be blocked
4. If enemy moves, check Rigidbody is Kinematic

### Test UI
1. Take damage - health bar should decrease
2. Pick up gold - gold text should increase
3. Open inventory - press I
4. Check all skill icons show correct sprites

---

## MOST COMMON MISTAKES

1. **Skills invisible**: Forgot to assign Effect Prefab to SkillSO
2. **Camera lag**: smoothSpeed too low or using SmoothDamp
3. **Enemies pushed**: Rigidbody set to Dynamic instead of Kinematic
4. **UI not updating**: UIManager fields not assigned in Inspector
5. **Skills don't cast**: Validation fails (missing required prefab)
6. **No damage numbers**: DamageNumberManager prefab not assigned
7. **Shop empty**: ShopManager.availableItems is empty

---

## DEBUG COMMANDS

Add these temporarily to test:

```csharp
// In PlayerController.cs
void Update() {
    // Test skill with T key
    if (Keyboard.current.tKey.wasPressedThisFrame) {
        Debug.Log("Casting skill 0");
        skillCaster.TryCastSkill(0);
    }
    
    // Test damage with Y key
    if (Keyboard.current.yKey.wasPressedThisFrame) {
        Debug.Log("Taking 10 damage");
        TakeDamage(10);
    }
    
    // Test gold with U key
    if (Keyboard.current.uKey.wasPressedThisFrame) {
        Debug.Log("Adding 50 gold");
        AddGold(50);
    }
}
```

---

## WHEN TO READ WHICH GUIDE

| Problem | Read This |
|---------|-----------|
| Skills not working | SKILL_SETUP_COMPLETE.md |
| Camera issues | This file (FIX 2) |
| Enemy physics | This file (FIX 3) |
| UI not updating | This file (FIX 4) |
| General setup | SETUP_README.md |
| Quick reference | QUICK_SETUP_CHECKLIST.md |
| Specific errors | TROUBLESHOOTING_GUIDE.md |

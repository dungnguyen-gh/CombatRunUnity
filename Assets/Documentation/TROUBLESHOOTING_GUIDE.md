# Troubleshooting Guide - Common Issues & Fixes

## Issue 1: Skills Not Working (No Visual Effects)

### Symptoms
- Pressing 1/2/3/4 does nothing
- No projectiles appear
- No shield visual
- Skills go on cooldown but no effect

### Root Causes & Fixes

#### Cause A: Missing Effect Prefabs
**Check:** SkillSO assets don't have effect prefabs assigned.

**Fix:**
1. Select your SkillSO (e.g., "Fireball")
2. In Inspector, check these fields:
   - **Effect Prefab**: Visual effect at impact
   - **Cast Effect Prefab**: Effect when casting
   - **Projectile Prefab**: REQUIRED for Projectile type
   - **Persistent Effect Prefab**: REQUIRED for Shield type

**Quick Test:**
- Create a simple Sphere or Particle System prefab
- Assign it to Effect Prefab
- Test again - you should see the sphere appear

#### Cause B: Effect Instantiating But Not Visible
**Check:** Effect prefab might be spawning but:
- Too small (scale = 0)
- Behind player (z-position wrong)
- Destroyed immediately

**Fix:**
Ensure your effect prefabs have:
- Scale: At least (1, 1, 1)
- Position: z = 0 for 2D
- SpriteRenderer or ParticleSystem enabled
- Duration > 0

#### Cause C: Skill Type Not Handled
**Check:** SkillSO has skillType that SkillCaster doesn't handle.

**Fix:** Check SkillCaster.cs line ~130. Make sure your skill type is in the switch statement.

#### Cause D: Cast Point Not Set
**Check:** SkillCaster.castPoint is null.

**Fix:** 
1. Select Player
2. Find SkillCaster component
3. Assign **Cast Point**: Use Player transform or create an empty child

---

## Issue 2: Camera Lagging / Feels Heavy

### Symptoms
- Camera follows slowly behind player
- Camera feels "floaty"
- Delayed response to movement

### Root Causes & Fixes

#### Cause A: SmoothDamp Settings
**Current Problem:**
```csharp
// Current code (LAGGY)
transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1f / smoothSpeed);
// With smoothSpeed = 5, that's 0.2s delay - TOO MUCH
```

**Fix Options:**

**Option 1: Increase smoothSpeed**
- Select Camera
- CameraFollow.smoothSpeed = 15-20 (instead of 5)

**Option 2: Use Direct Follow**
```csharp
// Replace SmoothDamp with Lerp for snappier feel
// In CameraFollow.cs, change:
transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
```

**Option 3: Disable Look Ahead**
- Uncheck "Use Look Ahead" in CameraFollow
- Look ahead causes additional delay

**Recommended Settings:**
```
smoothSpeed: 10-15
useLookAhead: false (or reduce lookAheadDistance to 0.5)
```

---

## Issue 3: Enemies Get Pushed By Player

### Symptoms
- Player walks into enemy, enemy slides away
- Enemies don't block player movement
- Enemies feel "weightless"

### Root Cause: Rigidbody2D Body Type

**Enemy Setup Wrong:**
```
Rigidbody2D:
  Body Type: Dynamic  ← WRONG! Gets pushed by collisions
```

**Correct Setup:**
```
Rigidbody2D:
  Body Type: Kinematic  ← CORRECT! Won't be pushed
  
  OR
  
Body Type: Dynamic
  Mass: 1000  ← Very heavy, won't be pushed easily
  Constraints: Freeze Position X/Y (if using pathfinding)
```

### Fix Steps

1. **Select Enemy prefab**
2. **Find Rigidbody2D**
3. **Change Body Type to Kinematic**
4. **Ensure movement is via code:**

```csharp
// In Enemy.cs, move via direct position (Kinematic)
void ChasePlayer() {
    Vector2 direction = (player.position - transform.position).normalized;
    // Kinematic movement - won't be pushed
    rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
}
```

**Alternative: Use Collision Layers**
If you want enemies to be solid but not pushed:
1. Create "Obstacles" layer
2. Set Enemy layer to "Obstacles"
3. In Physics2D settings: Player and Obstacles collide, but enemies don't push each other

---

## Issue 4: UI Not Reflecting Player Status

### Symptoms
- Health bar doesn't update when taking damage
- Gold doesn't show current amount
- Skill cooldowns not visible
- Inventory empty

### Root Causes & Fixes

#### Cause A: UIManager Not Subscribed to Events
**Check:** PlayerController events not connected to UI.

**Fix:**
Ensure UIManager subscribes in Start():
```csharp
void Start() {
    FindReferences();
    SubscribeToEvents();
}

void SubscribeToEvents() {
    if (player != null) {
        player.OnHealthChanged += UpdateHealth;
        player.OnGoldChanged += UpdateGold;
    }
}
```

#### Cause B: Player Events Not Firing
**Check:** PlayerController invokes events when stats change.

**Fix in PlayerController.cs:**
```csharp
public void TakeDamage(int damage) {
    stats.TakeDamage(damage);
    
    // MUST invoke event!
    OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);
}

public void AddGold(int amount) {
    gold += amount;
    
    // MUST invoke event!
    OnGoldChanged?.Invoke(gold);
}
```

#### Cause C: UI References Not Assigned
**Check:** UIManager inspector fields are empty.

**Fix:**
Select UIManager, assign:
- [ ] Health Slider
- [ ] Health Text  
- [ ] Gold Text
- [ ] Skill Icons (4 images)
- [ ] Skill Cooldown Overlays (4 images)
- [ ] Skill Cooldown Texts (4 text components)

#### Cause D: Events Firing Before UI Ready
**Check:** Race condition - player spawns before UIManager.

**Fix:** Ensure initialization order:
```csharp
// In UIManager.cs
void Start() {
    // Wait one frame for all objects to initialize
    StartCoroutine(InitializeNextFrame());
}

IEnumerator InitializeNextFrame() {
    yield return null;
    FindReferences();
    SubscribeToEvents();
    InitializeHUD();
}
```

---

## Quick Diagnostic Checklist

### Skills Not Working?
```
□ SkillCaster has skills array populated (4 skills)
□ Each SkillSO has skillType assigned
□ Projectile skills have projectilePrefab
□ Shield skills have persistentEffectPrefab
□ AOE skills have effectPrefab
□ Cast Point is assigned
□ Player can cast (not dead, not stunned)
□ Cooldown is ready (0)
```

### Camera Lag?
```
□ CameraFollow.smoothSpeed >= 10
□ Use Look Ahead is unchecked (or distance small)
□ Target is assigned (Player)
□ Using LateUpdate (not Update)
```

### Enemies Pushed?
```
□ Enemy Rigidbody2D is Kinematic (not Dynamic)
□ Enemy movement uses MovePosition or direct transform
□ Collision layers correct
```

### UI Not Updating?
```
□ UIManager has all fields assigned in Inspector
□ PlayerController invokes OnHealthChanged/OnGoldChanged
□ UIManager subscribes to these events
□ Events fire in correct order (UI ready before player)
```

---

## Test Commands (Add Temporarily)

Add these to test if systems work:

### Test Skill Casting
```csharp
// In PlayerController Update()
if (Keyboard.current.tKey.wasPressedThisFrame) {
    Debug.Log("Test casting skill 0");
    skillCaster.TryCastSkill(0);
}
```

### Test UI Updates
```csharp
// In UIManager Update()
if (player != null) {
    Debug.Log($"HP: {player.stats.currentHP}, Gold: {player.gold}");
}
```

### Test Camera
```csharp
// In CameraFollow Update()
Debug.Log($"Target: {target?.name}, Distance: {Vector3.Distance(transform.position, target.position)}");
```

---

## Most Common Fix Summary

| Issue | Most Common Fix |
|-------|-----------------|
| Skills invisible | Assign effect prefabs to SkillSO |
| Camera lag | Set smoothSpeed to 15+ |
| Enemies pushed | Set Rigidbody2D to Kinematic |
| UI not updating | Assign UI references in UIManager inspector |
| Skills don't cast | Check validation passes (no console warnings) |
| No damage numbers | Assign prefab to DamageNumberManager |
| Shop empty | Add ItemSOs to ShopManager.availableItems |

# CombatRun - Testing Guide

Complete testing procedures for verifying all bug fixes, features, UI improvements, and Input System functionality.

---

## 🎯 Quick Test Suite

Run these tests in order to verify everything works.

---

## Test 0: Input System - Basic Controls

**Purpose:** Verify Unity Input System Package is working

### Prerequisites:
1. Input System Package installed
2. `GameControls.inputactions` assigned to PlayerController
3. Active Input Handling set to "Input System Package" or "Both"

### Steps:
1. Start game
2. **Test Movement:**
   - Press `W` / `A` / `S` / `D`
     - ✅ Player moves in all directions
   - Press Arrow Keys
     - ✅ Player moves in all directions
3. **Test Attack:**
   - Press `Space`
     - ✅ Player attacks
   - Click Left Mouse Button
     - ✅ Player attacks
4. **Test Skills:**
   - Press `1`, `2`, `3`, `4`
     - ✅ Skills cast (if learned/off cooldown)
5. **Test Inventory:**
   - Press `I`
     - ✅ Inventory panel opens
   - Press `I` again
     - ✅ Inventory panel closes
6. **Test Pause:**
   - Press `Escape`
     - ✅ Pause menu opens
   - Press `Escape` again
     - ✅ Pause menu closes

**Expected Result:** All controls work smoothly via Input Actions

---

## Test 1: UI System - Pause Stack

**Purpose:** Verify multiple UI panels don't conflict with Input System

### Steps:
1. Start game (timeScale should be 1)
2. Press `I` (Input Action: Inventory) to open Inventory
   - ✅ timeScale = 0
   - ✅ Panel fades in smoothly
3. Trigger Shop (via button or test code)
   - ✅ Inventory closes smoothly
   - ✅ Shop opens
   - ✅ timeScale stays 0
4. Press `Escape` (Input Action: Pause)
   - ✅ Shop closes
   - ✅ timeScale = 1
5. Repeat: Open Inventory → Open Shop → Press Escape
   - ✅ Should work the same way

**Expected Result:** No timeScale conflicts, smooth transitions, Input Actions work correctly

---

## Test 2: UI System - Notification Queue

**Purpose:** Verify notification system works correctly

### Steps:
1. Trigger 10 notifications rapidly
   ```
   Option A: Buy/Sell items quickly
   Option B: Add test code calling ShowNotification()
   ```
2. Watch notification area
   - ✅ Max 5 notifications visible
   - ✅ Oldest notifications fade out
   - ✅ New ones stack from bottom
3. Wait for all to fade
   - ✅ No null reference errors
   - ✅ Notifications clean up properly

**Expected Result:** Smooth notification queue, no memory leaks

---

## Test 3: Status Effect - Freeze Bug Fix

**Purpose:** Verify divide-by-zero fix

### Steps:
1. Give player a skill that applies Freeze with 100% slow
2. Cast on enemy
3. Check console
   - ✅ No "Divide by zero" error
   - ✅ Enemy speed becomes minimum (0.01 * original)
4. Wait for effect to wear off
   - ✅ Enemy speed returns to normal
5. Reapply multiple times
   - ✅ No crashes

**Expected Result:** Freeze works without crashes, even at 100% slow

---

## Test 4: Camera System - Smooth Following

**Purpose:** Verify new CameraFollow features

### Steps:
1. Move player around
   - ✅ Camera follows smoothly (not jerky)
2. Make small movements (within dead zone)
   - ✅ Camera doesn't move
3. Move continuously in one direction
   - ✅ Camera looks ahead of player
4. (Optional) Test camera shake
   ```csharp
   Camera.main.GetComponent<CameraFollow>().Shake(0.3f, 0.2f);
   ```

**Expected Result:** Smooth, professional camera behavior

---

## Test 5: Weapon Mastery - Persistence

**Purpose:** Verify Dictionary → List fix

### Steps:
1. Kill 5 enemies with Sword
2. Check WeaponMastery UI (if exists)
   - ✅ Shows 5 kills
3. Exit play mode
4. Enter play mode again
5. Kill 1 more enemy with Sword
   - ✅ Shows 6 kills (data persisted)
6. Check console
   - ✅ No serialization warnings

**Expected Result:** Mastery data saves and loads correctly

---

## Test 6: Combo System - Finisher

**Purpose:** Verify combo fixes

### Steps:
1. Attack enemies 4 times (build combo)
   - ✅ Combo counter increases
2. 5th attack with normal click
   - ✅ Regular attack, combo resets
3. Attack 4 times again
4. 5th attack HOLD for 0.5s then release
   - ✅ Finisher animation plays
   - ✅ Massive damage dealt
   - ✅ Combo resets

**Expected Result:** Finisher works with hold mechanic

---

## Test 7: Inventory - Full Inventory Safety

**Purpose:** Verify item loss prevention

### Steps:
1. Fill inventory completely
2. Try to buy item from Shop
   - ✅ Warning message shown
   - ✅ Gold not deducted
3. Try to unequip item
   - ✅ Warning message shown
   - ✅ Item stays equipped
4. Drop an item
5. Unequip item
   - ✅ Now works (has space)

**Expected Result:** Items never lost due to full inventory

---

## Test 8: Skill Synergy - Multiple Applications

**Purpose:** Verify cleanup doesn't break synergies

### Steps:
1. Activate synergy (e.g., Fireball → Ice = Damage Reduction)
2. Take damage
   - ✅ Damage reduced
3. Activate same synergy again
   - ✅ No errors
   - ✅ Defense doesn't stack infinitely
4. Wait for synergy to expire
5. Take damage
   - ✅ Normal damage

**Expected Result:** Proper cleanup, no defense stacking

---

## Test 9: Projectile - No Double Hit

**Purpose:** Verify hasHit flag fix

### Steps:
1. Use projectile skill
2. Hit enemy that takes multiple frames to process
   - ✅ Damage applied once
   - ✅ Enemy HP correct
3. Fire multiple projectiles
   - ✅ Each does correct damage

**Expected Result:** No double damage from projectiles

---

## Test 10: Set Bonus - No Duplicate Components

**Purpose:** Verify HashSet tracking

### Steps:
1. Equip 2-piece set
   - ✅ Bonus applied
2. Unequip one item
   - ✅ Bonus removed
3. Re-equip
   - ✅ Bonus applied
4. Check player components
   ```csharp
   GetComponents<DamageReductionEffect>().Length // Should be 0 or 1
   ```

**Expected Result:** No duplicate effect components

---

## Test 11: SPUM - Animation Validation

**Purpose:** Verify automatic bounds checking

### Steps:
1. Set invalid animation index (e.g., 999)
2. Play animation
   - ✅ Console warning: "Invalid animation index"
   - ✅ Falls back to valid index
3. Set valid index
   - ✅ No warnings
   - ✅ Animation plays

**Expected Result:** No crashes from invalid indices

---

## Test 12: Shop - Rarity Cache Performance

**Purpose:** Verify caching works

### Steps:
1. Open shop 10 times
2. Watch performance
   - ✅ No lag spikes
3. Check console (debug build)
   - ✅ No "Slow shop refresh" warnings

**Expected Result:** Fast shop loading

---

## Test 13: Gamble System - Full Inventory

**Purpose:** Verify refund mechanism

### Steps:
1. Fill inventory completely
2. Use Gamble (pay gold)
3. Try to get item
   - ✅ Warning shown
   - ✅ Gold refunded

**Expected Result:** No gold lost when inventory full

---

## Test 14: Daily Run - Seed Generation

**Purpose:** Verify deterministic daily runs

### Steps:
1. Start Daily Run
2. Note the seed and layout
3. Exit and restart Daily Run (same day)
   - ✅ Same seed
   - ✅ Same layout
4. Start new run tomorrow
   - ✅ Different seed

**Expected Result:** Consistent daily challenge

---

## 🚨 Critical Test - Memory Safety

### Steps:
1. Open Inventory
2. Close Inventory
3. Repeat 100 times rapidly
   ```
   For i in 1..100:
       ToggleInventory()
       ToggleInventory()
   ```
4. Check console
   - ✅ No "ObjectDisposedException"
   - ✅ No "MissingReferenceException"
5. Check Profiler (if available)
   - ✅ No memory growth

**Expected Result:** Stable memory usage

---

## 📝 Automated Test Script

Add this to a test GameObject for automated verification:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class QuickTest : MonoBehaviour {
    void Update() {
        // Test 0: Input System Check
        if (Keyboard.current != null) {
            // Test 1: Rapid Inventory Toggle (Key T)
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                for (int i = 0; i < 10; i++) {
                    UIManager.Instance?.ToggleInventory();
                }
                Debug.Log("Rapid toggle test complete");
            }
            
            // Test 2: Camera Shake (Key Y)
            if (Keyboard.current.yKey.wasPressedThisFrame) {
                var cam = Camera.main?.GetComponent<CameraFollow>();
                cam?.Shake(0.3f, 0.2f);
                Debug.Log("Camera shake test");
            }
            
            // Test 3: Multiple Notifications (Key U)
            if (Keyboard.current.uKey.wasPressedThisFrame) {
                for (int i = 0; i < 8; i++) {
                    UIManager.Instance?.ShowNotification($"Test {i}");
                }
                Debug.Log("Notification flood test");
            }
            
            // Test 4: Check timeScale (Key G)
            if (Keyboard.current.gKey.wasPressedThisFrame) {
                Debug.Log($"Current timeScale: {Time.timeScale}");
            }
        }
    }
}
```

**Note:** Uses `Keyboard.current` from new Input System. For legacy Input Manager compatibility during transition, use `Input.GetKeyDown(KeyCode.T)` etc.

---

## ✅ Final Verification Checklist

### Input System
- [ ] Input Actions assigned to PlayerController
- [ ] WASD movement works
- [ ] Arrow key movement works
- [ ] Space/LeftClick attack works
- [ ] Skills 1-4 work
- [ ] I key opens inventory
- [ ] Escape key pauses/closes panels
- [ ] Mouse aiming works for skills
- [ ] No input lag or dropped inputs

### UI System
- [ ] Pause stack works correctly
- [ ] Panel animations smooth
- [ ] Notifications queue properly
- [ ] Escape key behavior correct (via Input System)
- [ ] No timeScale conflicts

### Combat
- [ ] Status effects work
- [ ] Freeze 100% slow no crash
- [ ] Combo finisher works
- [ ] Projectiles hit once
- [ ] Damage numbers display

### Systems
- [ ] Inventory no item loss
- [ ] Shop cache works
- [ ] Mastery persists
- [ ] Synergies clean up
- [ ] Set bonuses no duplicates

### Visual
- [ ] Camera smooth follow
- [ ] SPUM animations valid
- [ ] Damage flash works

---

## 🐛 If Tests Fail

### Issue: timeScale stuck at 0
**Fix:** Check UIManager pauseDepth, may need manual reset:
```csharp
UIManager.Instance.pauseDepth = 0;
Time.timeScale = 1f;
```

### Issue: Notifications not cleaning up
**Fix:** Check for null references in RemoveNotification

### Issue: Camera jerky
**Fix:** Ensure useSmoothDamp = true, increase smoothTime

---

*Run all tests before committing changes! Input System tests are critical for gameplay.*

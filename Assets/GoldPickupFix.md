# GoldPickup Fix - Why Gold Falls Through Floor

## Problem
GoldPickup falls to the bottom of the screen because it's missing a **Collider2D** component.

## Root Cause
The GoldPickup script doesn't have a Rigidbody2D or Collider2D requirement, but if you added a Rigidbody2D to the prefab (for physics), it will fall due to gravity.

## Solution

### Option 1: No Physics (Recommended)
Remove Rigidbody2D from GoldPickup prefab:
1. Select GoldPickup prefab
2. Remove Rigidbody2D component if present
3. Keep only CircleCollider2D (Is Trigger = true)

### Option 2: With Physics (If you want bouncing)
If you want gold to bounce/roll:
1. Keep Rigidbody2D but set:
   - Body Type: Dynamic
   - Gravity Scale: 0 (NO GRAVITY)
   - Constraints: Freeze Rotation Z
2. Add CircleCollider2D (NOT trigger)
   - Or keep as trigger and handle collision manually

### Correct GoldPickup Prefab Setup:
```
GoldPickup GameObject:
├── Transform
├── SpriteRenderer (Circle, Yellow)
├── CircleCollider2D (Is Trigger: true)
├── GoldPickup script
└── NO Rigidbody2D (or Gravity Scale = 0)
```

## Additional Fix: Layer Check
Make sure GoldPickup is on the correct layer:
- Layer: "Pickups"
- Physics2D: Pickups should NOT collide with Enemies

## Verification
After fix:
1. Kill enemy
2. Gold should spawn and stay at enemy death position
3. Gold should move toward player when close (magnet effect)
4. Gold should be collected when touching player

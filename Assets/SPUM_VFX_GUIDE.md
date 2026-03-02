# SPUM VFX Creation Guide

Quick guide for creating optimized VFX for SPUM characters.

**Project Uses:** Unity Input System Package for controls (see `INPUT_SYSTEM_SETUP.md`)

---

## 💥 Damage Flash VFX

### Method 1: Simple Sprite Flash (Recommended)

**Step 1: Create the prefab**
```
1. Create Empty GameObject: "DamageFlashVFX"
2. Add Component: SpriteRenderer
3. Set Sprite: WhiteCircle or any glow texture
4. Set Color: (255, 0, 0, 128) - Red, 50% transparent
5. Set SortingOrder: 100 (above character)
6. Set Scale: (3, 3, 1) - adjust to cover character
```

**Step 2: Add fade out (optional)**
```csharp
// Add this script to the VFX prefab
using UnityEngine;

public class AutoDestroy : MonoBehaviour {
    public float lifetime = 0.1f;
    
    void Start() {
        Destroy(gameObject, lifetime);
    }
}
```

**Step 3: Assign to PlayerController**
- Drag prefab to `damageFlashVFX` field
- Check `useVFXDamageFlash`
- Set `damageFlashDuration` to match VFX lifetime

---

### Method 2: Particle System

**For more visual impact:**
```
1. Create Empty GameObject: "DamageFlashVFX"
2. Add Component: ParticleSystem
3. Configure:
   - Duration: 0.1
   - Looping: false
   - Start Color: Red
   - Start Size: 2
   - Max Particles: 10
   - Emission: Burst of 10
   - Shape: Circle
   - Renderer: SortingOrder 100
```

---

## 🎯 Facing Direction with Rotation Y

### How It Works

Instead of scaling X (which can mess up colliders), we rotate 180 degrees on Y axis:

```
Facing Right (Default):
  Rotation: (0, 0, 0)
  Character faces right →

Facing Left:
  Rotation: (0, 180, 0)
  Character faces left ←
```

### Why This Is Better

| Aspect | Scale X | Rotation Y |
|--------|---------|------------|
| Colliders | Get inverted (bad!) | Stay intact (good!) |
| Physics | Can break | Works normally |
| Children | All scale changes | Only rotation changes |
| Performance | Same | Same |

### Setup in Unity

No special setup needed! The code handles it:

```csharp
// In SPUMPlayerBridge.UpdateFacingDirection()
if (facing.x > 0.1f)
    spumPrefabs.transform.rotation = Quaternion.Euler(0, 180, 0); // Left
else if (facing.x < -0.1f)
    spumPrefabs.transform.rotation = Quaternion.Euler(0, 0, 0);   // Right
```

---

## 🎨 Other Recommended VFX

### 1. Weapon Trail
```
WeaponTrailVFX
├── TrailRenderer
│   ├── Time: 0.1
│   ├── StartWidth: 0.2
│   ├── EndWidth: 0
│   ├── Color: Weapon color
│   └── Material: Additive shader
```
Attach to weapon transform, enable during attack.

### 2. Footstep Dust
```
FootstepVFX
├── ParticleSystem
│   ├── Duration: 0.2
│   ├── StartSize: 0.3
│   ├── StartColor: Brown/Tan
│   └── Gravity: 1
```
Spawn at feet position when moving.

### 3. Level Up Effect
```
LevelUpVFX
├── ParticleSystem (shiny sparkles)
├── Light (point light, yellow)
└── SpriteRenderer (level up icon)
```

---

## ⚡ Performance Tips

### Do:
- ✅ Use simple sprites for damage flash
- ✅ Destroy VFX after duration (don't pool short effects)
- ✅ Use SortingOrder to control layering
- ✅ Keep particle counts low (< 50)
- ✅ Use object pooling for frequently spawned VFX

### Don't:
- ❌ Flash all body parts individually (too slow)
- ❌ Use complex shaders for simple effects
- ❌ Spawn particles every frame
- ❌ Forget to destroy VFX (memory leak!)

---

## 🔧 Quick Reference

### VFX Components

| VFX Type | Component | Key Settings |
|----------|-----------|--------------|
| Damage Flash | SpriteRenderer | SortingOrder: 100, Color: Red |
| Weapon Trail | TrailRenderer | Time: 0.1, Width: 0.2→0 |
| Dust/Footstep | ParticleSystem | Duration: 0.2, Gravity: 1 |
| Glow/Aura | SpriteRenderer | Scale: 2x, Alpha: 0.3 |

### PlayerController VFX Settings

| Field | Recommended Value |
|-------|-------------------|
| useVFXDamageFlash | ☑️ (checked) |
| damageFlashVFX | Your VFX prefab |
| damageFlashDuration | 0.1 - 0.2 seconds |
| flashAllSpriteRenderers | ☐ (unchecked) |

---

## 📁 File Organization

```
Assets/
├── Prefabs/
│   └── VFX/
│       ├── DamageFlashVFX.prefab
│       ├── WeaponTrailVFX.prefab
│       └── FootstepDustVFX.prefab
├── Materials/
│   └── VFX/
│       ├── GlowAdditive.mat
│       └── TrailMaterial.mat
└── Sprites/
    └── VFX/
        ├── WhiteCircle.png
        └── GlowTexture.png
```

---

## 🔧 Code Quality Improvements

### Recent Fixes (Fixer Agent)

| Component | Improvement |
|-----------|-------------|
| `PlayerController` | Migrated to Unity Input System Package |
| `SPUMEquipmentManager` | Added null checks for GetComponent<SpriteRenderer>() |
| `CameraFollow` | Find() now retries once per second instead of every frame |
| `UIManager` | CanvasGroup references now cached for performance |
| `InventoryUI` | Image and Button components now cached |
| `SkillCaster` | Updated to use Mouse.current for position reading |

---

*Simple VFX = Better Performance!*

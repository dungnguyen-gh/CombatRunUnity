# Gameplay Polish Guide

This document covers the enhanced gameplay systems for dynamic skills, auto-binding UI, and visual feedback.

---

## 1. Enhanced Skill System

### New Skill Types (15 Total)

| Type | Description | Key Features |
|------|-------------|--------------|
| **CircleAOE** | Spin attack around player | Instant damage |
| **GroundAOE** | Meteor/delayed explosion | Targeted area |
| **Projectile** | Fireball/arrows | Homing, piercing |
| **Melee** | Single target attack | Extended range |
| **Shield** | Damage reduction buff | Duration-based |
| **Reflect** | Reflects projectiles | Active blocking |
| **Heal** | Self or AoE healing | Based on max HP |
| **Dash** | Quick movement | Invulnerability option |
| **Teleport** | Instant reposition | Line of sight check |
| **Blink** | Short range teleport | Lower cooldown |
| **Summon** | Spawn allied units | Duration control |
| **Turret** | Stationary defense | Auto-targeting |
| **Totem** | Buff/debuff zone | Persistent effect |
| **Beam** | Continuous laser | Channeled |
| **Channel** | Hold to charge | Variable power |
| **Buff** | Stat enhancement | Stackable |
| **Trap** | Deployable hazard | Trigger-based |
| **Chain** | Bouncing damage | Damage falloff |
| **AreaDenial** | Persistent zone | DoT effect |

### SkillSO Configuration

```csharp
[CreateAssetMenu(fileName="Skill_Fireball", menuName="ARPG/Skill")]
public class SkillSO : ScriptableObject {
    // Basic Info
    public string skillId = "fireball";
    public string skillName = "Fireball";
    public SkillType skillType = SkillType.Projectile;
    public SkillRarity rarity = SkillRarity.Rare;
    
    // Combat
    public float damageMultiplier = 2f;
    public float cooldownTime = 5f;
    public int manaCost = 20;
    
    // Projectile Settings
    public bool homing = true;
    public bool pierceEnemies = false;
    public bool explodeOnImpact = true;
    
    // Visual
    public GameObject projectilePrefab;
    public GameObject effectPrefab;
    public AudioClip castSound;
    
    // Screen Effects
    public bool useScreenShake = true;
    public float screenShakeIntensity = 0.3f;
}
```

### Creating a New Skill

1. **Right-click in Project window** → Create → ARPG → Skill
2. **Configure the skill** using the inspector
3. **Assign to SkillCaster** on Player prefab
4. **Set skill slot** (0-3 for keys 1-4)

### SkillCaster Setup

The `SkillCaster` component automatically handles:
- Cooldown management
- Resource costs (mana/health)
- Screen effects (shake, slow-mo)
- Visual feedback

```csharp
// In PlayerController, skills are cast via input
void OnSkill1(InputAction.CallbackContext ctx) {
    if (ctx.performed) {
        skillCaster.TryCastSkill(0); // Cast skill in slot 0
    }
}
```

---

## 2. Auto-Binding UI System

The new UI system **automatically discovers components** by naming convention. No manual wiring needed!

### Naming Conventions

| Component | Auto-Discovered Names |
|-----------|----------------------|
| Gold Text | "Gold", "GoldText", "GoldAmount" |
| Capacity Text | "Capacity", "Slots", "InventoryCount" |
| Close Button | "Close", "CloseButton", "X", "Exit" |
| Sell Button | "Sell", "SellButton", "SellItem" |
| Item Slots Container | "ItemSlots" (configurable) |
| Equipment Container | "EquipmentSlots" |
| Shop Container | "ShopItems" |
| Item Icon | "Icon", "ItemIcon" |
| Rarity Border | "Border", "Rarity", "Frame" |
| Count Text | "Count", "Amount", "Stack" |

### Setting Up Inventory UI

1. **Create Canvas** with InventoryPanel
2. **Create child objects** with the naming conventions above
3. **Add `AutoBindingInventoryUI`** component to root panel
4. **Assign prefabs** (ItemSlotPrefab, ShopItemPrefab)

```csharp
// AutoBindingInventoryUI will automatically:
- Find all UI components
- Create item slots
- Bind to InventoryManager events
- Refresh UI when inventory changes
```

### Setting Up Skill Bar

1. **Create Skill Bar Panel** with 4 child slots
2. **Name slots** "SkillSlot1", "SkillSlot2", etc.
3. **Add `SkillBarUI`** component
4. **Assign SkillSlotPrefab** with:
   - Icon (Image) - named "Icon" or "SkillIcon"
   - Cooldown Overlay (Image) - named "Cooldown" or "Overlay"
   - Key Text (TMP) - named "Key" or "Binding"
   - Cooldown Text (TMP) - named "Time" or "CooldownText"

### Creating UI Prefabs

#### Item Slot Prefab Structure:
```
ItemSlot (GameObject)
├── Background (Image)
├── Icon (Image) - main item sprite
├── Border (Image) - rarity color
└── Count (TextMeshPro) - stack count
```

#### Skill Slot Prefab Structure:
```
SkillSlot (GameObject)
├── Background (Image)
├── Icon (Image) - skill icon
├── CooldownOverlay (Image - Filled type)
├── Border (Image)
├── KeyText (TextMeshPro) - "1", "2", etc.
└── CooldownText (TextMeshPro) - remaining time
```

---

## 3. Visual Feedback System

### Camera Effects

The `CameraFollow` component provides:

```csharp
// Smooth follow with lookahead
cameraFollow.smoothSpeed = 5f;
cameraFollow.useLookAhead = true;
cameraFollow.lookAheadDistance = 2f;

// Screen shake
cameraFollow.Shake(duration: 0.3f, magnitude: 0.5f);

// Bounds constraint
cameraFollow.useBounds = true;
cameraFollow.minBounds = new Vector2(-50, -50);
cameraFollow.maxBounds = new Vector2(50, 50);
```

### Skill Screen Effects

In SkillSO, enable effects:

```csharp
// Screen shake on impact
useScreenShake = true;
screenShakeIntensity = 0.3f;
screenShakeDuration = 0.2f;

// Slow motion on cast
useSlowMotion = true;
slowMotionScale = 0.5f; // 50% speed
slowMotionDuration = 0.3f;
```

### Notification System

```csharp
// Show floating notification
UIManager.Instance.ShowNotification("Item acquired!");
UIManager.Instance.ShowNotification("Not enough mana!", duration: 1.5f);
```

---

## 4. Shop System

### ShopManager Setup

1. **Create ShopManager** GameObject
2. **Add `ShopManager`** component
3. **Populate Available Items** list with ItemSO assets
4. **Set Shop Slots** (default: 6)

### Shop Refresh

```csharp
// Auto-refresh every 5 minutes
shopManager.refreshInterval = 300f;
shopManager.autoRefreshOnOpen = true;

// Manual refresh
shopManager.ForceRefresh();
```

### Pricing

```csharp
// Base prices are in ItemSO
item.price = 100;

// Shop applies multiplier
shopManager.priceMultiplier = 1.2f; // 20% markup

// Sell price is calculated
sellPrice = item.sellPrice * 0.5f;
```

---

## 5. Quick Setup Checklist

### For New Skills:
- [ ] Create SkillSO asset
- [ ] Assign icon sprite
- [ ] Configure damage/cooldown
- [ ] Set targeting type
- [ ] Assign effect prefabs
- [ ] Add to SkillCaster skills array
- [ ] Set skill slot (0-3)

### For Inventory UI:
- [ ] Create Canvas with panel
- [ ] Name child objects correctly
- [ ] Add AutoBindingInventoryUI component
- [ ] Assign slot prefabs
- [ ] Test add/remove items

### For Skill Bar:
- [ ] Create skill bar panel
- [ ] Add SkillBarUI component
- [ ] Create skill slot prefab
- [ ] Name prefab children correctly
- [ ] Assign to SkillCaster

### For Shop:
- [ ] Create ShopManager
- [ ] Add items to Available Items
- [ ] Configure refresh interval
- [ ] Create shop UI panel
- [ ] Test buy/sell

---

## 6. Troubleshooting

### Skills not casting:
- Check SkillCaster has skill assigned in array
- Verify cooldown is complete (check SkillBarUI fill)
- Ensure player has required resources

### UI not binding:
- Check GameObject names match conventions
- Verify AutoBindingInventoryUI is enabled
- Check console for auto-discovery logs

### Cooldown not showing:
- Ensure SkillSlotUI has CooldownOverlay Image
- Set Image type to "Filled"
- Set Fill Method to "Radial 360"

### Shop not refreshing:
- Check ShopManager has Available Items
- Verify autoRefreshOnOpen is enabled
- Check console for errors

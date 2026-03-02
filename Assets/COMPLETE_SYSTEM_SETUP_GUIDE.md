# Complete System Setup Guide

Inventory, Shop, Skills, Managers, and UI setup.

---

## Table of Contents
1. [Inventory System](#inventory-system)
2. [Shop System](#shop-system)
3. [Skill System](#skill-system)
4. [Managers Setup](#managers-setup)
5. [UI Setup](#ui-setup)
6. [Testing Checklist](#testing-checklist)

---

## Inventory System

### Components Needed
- **InventoryManager** (singleton)
- **InventoryUI** (UI panel)
- **ItemSO** assets
- **PlayerController** (reference)

### Step 1: Create ItemSO Assets

**Right-click → Create → ARPG → Item**

Create items in `Assets/Resources/Items/`:

#### Weapon Example: Iron Sword
```yaml
Item Id: "iron_sword_001"
Item Name: "Iron Sword"
Description: "A basic iron sword"
Item Sprite: [SPUM weapon sprite]
Icon: [Inventory icon]
Rarity: Common
Slot: Weapon
Price: 100
Damage Bonus: 5
Weapon Type: Sword
```

#### Armor Example: Leather Armor
```yaml
Item Id: "leather_armor_001"
Item Name: "Leather Armor"
Description: "Basic protection"
Item Sprite: [SPUM armor sprite]
Icon: [Inventory icon]
Rarity: Common
Slot: Armor
Price: 80
Defense Bonus: 3
```

### Step 2: Setup InventoryManager

Already added to Managers GameObject. Configuration:
```yaml
InventoryManager:
├── Max Inventory Slots: 20
└── [Other fields auto-managed]
```

### Step 3: Create Inventory UI

```
Canvas
└── InventoryPanel (Panel + CanvasGroup, DISABLED by default)
    ├── Title (TextMeshPro): "INVENTORY"
    ├── CloseButton (Button with "X")
    ├── GridContainer (Empty with Grid Layout Group)
    │   └── SlotPrefab (UI Image x 20 slots)
    └── EquippedSection
        ├── WeaponSlot (Image + Text)
        └── ArmorSlot (Image + Text)
```

**SlotPrefab:**
```
SlotPrefab (UI Image):
├── Background (Image - gray)
├── ItemIcon (Image - item sprite)
└── QuantityText (TextMeshPro)
```

### Step 4: Assign to UIManager

```yaml
UIManager:
└── Inventory Panel: [Drag InventoryPanel]
```

---

## Shop System

### Components Needed
- **ShopManager** (singleton)
- **ShopUI** (UI panel)
- **ItemSO** assets (same as inventory)

### Step 1: Setup ShopManager

Already added to Managers GameObject.

### Step 2: Create Shop UI

```
Canvas
└── ShopPanel (Panel + CanvasGroup, DISABLED by default)
    ├── Title: "SHOP"
    ├── CloseButton
    ├── GoldText: "Gold: 999"
    ├── ItemList (ScrollView)
    │   └── Content (Vertical Layout Group)
    │       └── ShopItemPrefab
    └── RefreshButton
```

**ShopItemPrefab:**
```
ShopItemPrefab:
├── ItemIcon (Image)
├── ItemName (TextMeshPro)
├── ItemPrice (TextMeshPro)
├── BuyButton (Button)
└── PreviewButton (Button - optional)
```

### Step 3: Assign to UIManager

```yaml
UIManager:
└── Shop Panel: [Drag ShopPanel]
```

### Step 4: Setup Shop Trigger

Create Shop NPC:
```
ShopNPC:
├── SpriteRenderer
├── CircleCollider2D (Is Trigger: true)
└── ShopTrigger script (or use collision)
```

**ShopTrigger Script:**
```csharp
void OnTriggerEnter2D(Collider2D other) {
    if (other.CompareTag("Player")) {
        UIManager.Instance?.ToggleShop();
    }
}
```

---

## Skill System

See `SKILL_SETUP_GUIDE.md` for detailed instructions.

### Quick Setup:

1. Create 4 SkillSO assets in `Assets/Resources/Skills/`
2. Assign to Player/SkillCaster.skills[]
3. Setup skill icons in HUD
4. Assign projectile/shield prefabs if needed
5. Configure SPUM animation indices

---

## Managers Setup

### Complete Manager Configuration

Create **Managers** GameObject and add all components:

#### 1. GameManager
```yaml
GameManager:
├── Spawn Points: [Array of transforms]
├── Enemy Prefabs: [Array of enemy prefabs]
├── Boss Prefab: [Boss prefab]
├── Boss Spawn Point: [Transform]
├── Player: [Player reference]
└── Enemy Container: [Empty parent for spawned enemies]
```

#### 2. UIManager
```yaml
UIManager:
├── HUD Panel: [HUDPanel]
├── Health Slider: [HealthSlider]
├── Gold Text: [GoldText]
├── Skill Icons: [4x Image]
├── Skill Cooldown Overlays: [4x Image]
├── Inventory Panel: [InventoryPanel]
├── Shop Panel: [ShopPanel]
├── Pause Panel: [PausePanel]
├── Notification Prefab: [NotificationText prefab]
├── Notification Parent: [Canvas or container]
├── Revive Panel: [Optional - for lives system]
├── Game Over Panel: [Optional]
└── Player: [Auto-finds if null]
```

#### 3. InventoryManager
```yaml
InventoryManager:
└── Auto-configures
```

#### 4. ShopManager
```yaml
ShopManager:
└── Auto-loads items from Resources/Items/
```

#### 5. SetBonusManager
```yaml
SetBonusManager:
└── Auto-loads sets from Resources/Sets/
```

#### 6. WeaponMasteryManager
```yaml
WeaponMasteryManager:
└── Auto-configures
```

#### 7. SkillSynergyManager
```yaml
SkillSynergyManager:
└── Auto-configures
```

#### 8. DailyRunManager
```yaml
DailyRunManager:
└── Auto-configures
```

#### 9. GambleSystem
```yaml
GambleSystem:
└── Auto-finds references
```

#### 10. DamageNumberManager
```yaml
DamageNumberManager:
├── Damage Number Prefab: [REQUIRED - assign prefab]
├── Pool Size: 20
└── [Other settings]
```

---

## UI Setup

### Step 1: Canvas Setup

```
Canvas:
├── Render Mode: Screen Space - Overlay
├── Canvas Scaler: Scale With Screen Size (1920x1080)
└── Reference Resolution: 1920 x 1080
```

### Step 2: EventSystem

```
EventSystem:
├── Input System UI Input Module
└── Remove: Standalone Input Module (if present)
```

### Step 3: HUD Layout

```
Canvas
└── HUDPanel (Top Stretch, Height: 100)
    ├── HealthSection (Left)
    │   ├── HealthText: "HP: 100/100"
    │   └── HealthSlider (Slider)
    ├── GoldSection (Right)
    │   └── GoldText: "0 💰"
    └── SkillSection (Bottom Center)
        ├── Skill1 (Image + CooldownOverlay)
        ├── Skill2 (Image + CooldownOverlay)
        ├── Skill3 (Image + CooldownOverlay)
        └── Skill4 (Image + CooldownOverlay)
```

### Step 4: Panels

All panels need **CanvasGroup** component:

```
InventoryPanel:
├── CanvasGroup (Alpha: 0, Interactable: false, Blocks Raycasts: false)
└── SetActive(false) in scene

ShopPanel:
├── CanvasGroup (Alpha: 0)
└── SetActive(false)

PausePanel:
├── CanvasGroup (Alpha: 0)
└── SetActive(false)
```

### Step 5: Input Actions

PlayerController Input Actions:
```yaml
Input Actions: [Drag GameControls.inputactions]
```

Default controls:
- W/A/S/D or Arrows: Move
- Space or Left Click: Attack
- 1/2/3/4: Skills
- I: Inventory
- Escape: Pause

---

## Testing Checklist

### System Integration Tests

#### Inventory System
- [ ] Press I opens inventory
- [ ] Items display in grid
- [ ] Can equip weapon (visual changes)
- [ ] Can equip armor (visual changes)
- [ ] Stats update on equip
- [ ] Press I again closes inventory

#### Shop System
- [ ] Shop UI opens
- [ ] Items display with prices
- [ ] Can buy item (gold decreases)
- [ ] Item appears in inventory
- [ ] Can sell item
- [ ] Gold increases on sell

#### Skill System
- [ ] Skill 1 casts with animation
- [ ] Skill 2 casts with projectile
- [ ] Skill 3 casts with AOE
- [ ] Skill 4 casts shield
- [ ] Cooldowns display correctly
- [ ] Can't cast while on cooldown

#### Lives & Revive
- [ ] Player has 3 lives
- [ ] Death reduces life count
- [ ] Revive countdown shows
- [ ] Player revives with full HP
- [ ] Game over after 3 deaths

#### Enemy Drops
- [ ] Enemy drops gold on death
- [ ] Gold magnetizes to player
- [ ] Gold adds to total
- [ ] (Optional) Items drop

---

## Common Issues & Solutions

### "Inventory not opening"
- Check UIManager.inventoryPanel assigned
- Check PlayerController Input Actions assigned

### "Shop items not showing"
- Check Resources/Items/ folder has ItemSO assets
- Check ShopManager loads items in Start()

### "Skills not working"
- Check SkillCaster.skills[] has SkillSOs
- Check SPUMPlayerBridge.skillAnimationIndices set

### "Damage numbers not showing"
- Check DamageNumberManager.damageNumberPrefab assigned
- Check prefab has TextMeshPro component

### "Player dies permanently"
- Check PlayerController.maxLives > 0
- Check UIManager has revivePanel assigned

---

## File Locations

| System | Key Files |
|--------|-----------|
| Inventory | `InventoryManager.cs`, `InventoryUI.cs`, `ItemSO.cs` |
| Shop | `ShopManager.cs`, `ShopUI.cs` |
| Skills | `SkillCaster.cs`, `SkillSO.cs`, `SkillType.cs` |
| Lives | `PlayerController.cs`, `UIManager.cs` |
| UI | `UIManager.cs`, `DamageNumberManager.cs` |

---

## Next Steps After Setup

1. **Balance Testing**
   - Adjust enemy HP/damage
   - Adjust player stats
   - Adjust skill cooldowns

2. **Content Creation**
   - Create more items
   - Create more enemy types
   - Create boss encounter

3. **Polish**
   - Add sound effects
   - Add particle effects
   - Add screen shake

4. **Save/Load**
   - Implement save system
   - Persist inventory
   - Persist weapon mastery

---

*Complete system setup guide*

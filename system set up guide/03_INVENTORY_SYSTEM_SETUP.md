# Inventory & Equipment System Setup Guide

## Table of Contents
1. [Overview](#1-overview)
2. [InventoryManager Setup](#2-inventorymanager-setup)
3. [Item Creation](#3-item-creation)
4. [Equipment System](#4-equipment-system)
5. [Set Bonus System](#5-set-bonus-system)
6. [Known Critical Issues](#6-known-critical-issues)
7. [UI Integration](#7-ui-integration)
8. [Testing Checklist](#8-testing-checklist)

---

## 1. Overview

The Inventory & Equipment System is a comprehensive solution for managing player items, equipment slots, and set bonuses in an ARPG-style game.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     INVENTORY SYSTEM ARCHITECTURE               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────┐ │
│  │  InventoryManager│    │ SetBonusManager │    │    ItemSO   │ │
│  │    (Singleton)   │◄──►│    (Singleton)  │◄──►│  (Assets)   │ │
│  └────────┬────────┘    └────────┬────────┘    └─────────────┘ │
│           │                      │                             │
│           ▼                      ▼                             │
│  ┌─────────────────┐    ┌─────────────────┐                    │
│  │  Player Stats   │    │ EquipmentSetSO  │                    │
│  │   (Modifiers)   │◄──►│   (Resources)   │                    │
│  └─────────────────┘    └─────────────────┘                    │
│                                                                 │
│  Equipment Slots: 2 (Weapon, Armor)                            │
│  Inventory Slots: Configurable (default: 20)                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `InventoryManager` | Manages items, gold, equipment slots | `Assets/Scripts/Inventory/InventoryManager.cs` |
| `SetBonusManager` | Tracks and applies set bonuses | `Assets/Scripts/Inventory/SetBonusManager.cs` |
| `ItemSO` | Data definition for items | `Assets/Scripts/Data/ItemSO.cs` |
| `EquipmentSetSO` | Data definition for equipment sets | `Assets/Scripts/Data/EquipmentSetSO.cs` |

---

## 2. InventoryManager Setup

### 2.1 Creating the Singleton

The `InventoryManager` uses the singleton pattern and persists across scenes.

**Setup Steps:**

1. Create an empty GameObject in your scene
2. Name it `InventoryManager`
3. Attach the `InventoryManager` script
4. Assign the `PlayerController` reference (or leave empty for auto-find)

```csharp
// The manager auto-configures as a singleton in Awake()
void Awake() {
    if (Instance == null) {
        Instance = this;
        DontDestroyOnLoad(gameObject);  // Persists across scenes
    } else {
        Destroy(gameObject);  // Prevent duplicates
    }
}
```

### 2.2 Configuration

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `maxInventorySlots` | int | 20 | Maximum number of items in inventory |
| `gold` | int | 0 | Player's current gold (use property) |
| `equippedWeapon` | ItemSO | null | Currently equipped weapon |
| `equippedArmor` | ItemSO | null | Currently equipped armor |
| `player` | PlayerController | null | Reference to player (auto-finds if null) |

**Inspector Configuration:**
```
InventoryManager GameObject:
├── Inventory
│   ├── Max Inventory Slots: 20
├── Currency
│   └── Gold: 0 (runtime only)
├── Equipment
│   ├── Equipped Weapon: None
│   └── Equipped Armor: None
└── References
    └── Player: [PlayerController]
```

### 2.3 Gold System

The gold system provides safe transaction methods with events:

```csharp
// Adding gold
InventoryManager.Instance.AddGold(100);

// Removing gold (returns false if insufficient funds)
bool success = InventoryManager.Instance.RemoveGold(50);

// Checking gold
int currentGold = InventoryManager.Instance.Gold;

// Events
InventoryManager.Instance.OnGoldChanged += (newAmount) => {
    Debug.Log($"Gold changed to: {newAmount}");
};
```

**Gold Events:**
- `OnGoldChanged(int newAmount)` - Fired when gold changes
- `OnInventoryChanged` - Fired when any inventory change occurs

---

## 3. Item Creation

### 3.1 Creating ItemSO Assets

**Via Unity Menu:**
1. Right-click in Project window
2. Select `Create > ARPG > Item`
3. Name your item (e.g., `Item_IronSword`)

**Via Code:**
```csharp
[CreateAssetMenu(fileName="Item_", menuName="ARPG/Item")]
public class ItemSO : ScriptableObject {
    // Item definition
}
```

### 3.2 All Fields Explained

#### Basic Info Section
```csharp
[Header("Basic Info")]
public string itemId;           // Unique identifier (e.g., "iron_sword_01")
public string itemName;         // Display name (e.g., "Iron Sword")
public string description;      // Item description text
public Sprite icon;             // Inventory UI icon
public ItemRarity rarity;       // Common, Uncommon, Rare, Epic, Legendary
public EquipSlot slot;          // Weapon or Armor
public int price;               // Buy price in gold
public int sellPrice;           // Auto-calculated: Max(1, price / 5)
```

#### Item Type Section
```csharp
[Header("Item Type")]
public bool isEquippable;       // Can be equipped?
public bool isStackable;        // Can stack multiple? (NOT IMPLEMENTED)
public int stackCount;          // Current stack size
public int maxStackSize;        // Max per stack (default: 99)
```

#### Stats Section
```csharp
[Header("Stats")]
public int damageBonus;         // Flat damage increase
public int defenseBonus;        // Flat defense increase
public float critBonus;         // Critical chance bonus (0.15 = +15%)
public float attackSpeedBonus;  // Attack speed multiplier
public int maxHPBonus;          // Maximum HP increase
```

#### Weapon Type Section
```csharp
[Header("Weapon Type")]
public WeaponType weaponType;   // None, Sword, Axe, Spear, etc.
                                // Used for weapon mastery tracking
```

#### Visual Section
```csharp
[Header("Visual")]
public Sprite itemSprite;       // Sprite shown when equipped
public Sprite worldSprite;      // 3D pickup display sprite
public Color rarityColor;       // Auto-generated from rarity
```

#### Set Bonus Section
```csharp
[Header("Set Bonus")]
public string setId;            // Empty = no set, else = set identifier
```

### 3.3 Rarity System

| Rarity | Color | Hex Value | Description |
|--------|-------|-----------|-------------|
| Common | Gray | `#B3B3B3` | Basic items |
| Uncommon | Green | `#33CC33` | Slightly improved |
| Rare | Blue | `#3380FF` | Notable bonuses |
| Epic | Purple | `#CC33E6` | Strong bonuses |
| Legendary | Gold | `#FFCC33` | Best items |

**Rarity is automatically applied to:**
- Item name color in tooltips
- UI border effects (when implemented)
- Sorting/prioritization

### 3.4 Stat Bonuses

**Example Item Configurations:**

```
🗡️ Iron Sword (Common Weapon)
├── itemId: "iron_sword_01"
├── damageBonus: 5
├── weaponType: Sword
└── price: 50

🛡️ Steel Armor (Rare Armor)
├── itemId: "steel_armor_01"
├── defenseBonus: 10
├── maxHPBonus: 20
└── price: 150

⚔️ Dragon Slayer (Legendary Weapon)
├── itemId: "dragon_sword_01"
├── damageBonus: 25
├── critBonus: 0.15
├── weaponType: Sword
├── setId: "dragon_set"
└── price: 2000
```

---

## 4. Equipment System

### 4.1 Weapon and Armor Slots

The system supports exactly **2 equipment slots**:

```csharp
public enum EquipSlot {
    Weapon,  // Primary weapon slot
    Armor    // Body armor slot
}
```

**Equipping Items:**
```csharp
// Equip an item from inventory
ItemSO sword = InventoryManager.Instance.GetItem(0);
InventoryManager.Instance.Equip(sword);

// The item is removed from inventory and equipped
// Previous equipped item (if any) goes to inventory
```

**Unequipping Items:**
```csharp
// Unequip by slot
InventoryManager.Instance.UnequipSlot(EquipSlot.Weapon);
InventoryManager.Instance.UnequipSlot(EquipSlot.Armor);

// Unequip by item reference
InventoryManager.Instance.Unequip(currentWeapon);
```

### 4.2 Stat Modification Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    STAT MODIFICATION FLOW                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. Equip Item                                              │
│      │                                                      │
│      ▼                                                      │
│  2. Remove from Inventory                                  │
│      │                                                      │
│      ▼                                                      │
│  3. RefreshPlayerStats() called                            │
│      │                                                      │
│      ▼                                                      │
│  4. player.stats.ResetMods()                               │
│      │                                                      │
│      ▼                                                      │
│  5. Apply equippedWeapon stats                             │
│  6. Apply equippedArmor stats                              │
│      │                                                      │
│      ▼                                                      │
│  7. player.UpdateStatsFromEquipment()                      │
│      │                                                      │
│      ▼                                                      │
│  8. SetBonusManager.UpdateSetBonuses()                     │
│      │                                                      │
│      ▼                                                      │
│  9. Apply set bonus stats                                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.3 Visual System

The system supports two visual modes:

#### Regular Visual Mode
```csharp
// Standard sprite swapping
player.SetWeaponVisual(item.itemSprite);
player.SetArmorVisual(item.itemSprite);
```

#### SPUM (2D Character Editor) Mode
```csharp
// SPUM equipment integration
if (player.useSPUM && player.spumEquipment != null) {
    player.spumEquipment.EquipWeapon(item.itemSprite, item.weaponType);
    player.spumEquipment.EquipArmor(item.itemSprite);
}
```

**Configuration in PlayerController:**
```csharp
public bool useSPUM = false;           // Toggle SPUM mode
public SPUMEquipment spumEquipment;    // SPUM component reference
```

**Visual Preview (Shop):**
```csharp
// Preview equipment without actually equipping
InventoryManager.Instance.PreviewEquip(item);

// End preview and restore actual equipment
InventoryManager.Instance.EndPreview();
```

---

## 5. Set Bonus System

### 5.1 Creating EquipmentSetSO

**Via Unity Menu:**
1. Right-click in Project window
2. Select `Create > ARPG > EquipmentSet`
3. Name your set (e.g., `Set_DragonSlayer`)

**Required Folder Structure:**
```
Assets/
└── Resources/
    └── Sets/                    # MUST be in Resources/Sets
        ├── Set_DragonSlayer.asset
        ├── Set_PaladinBlessing.asset
        └── Set_ShadowAssassin.asset
```

### 5.2 EquipmentSetSO Fields

```csharp
[Header("Set Info")]
public string setId;                    // Unique identifier
public string setName;                  // Display name
public string description;              // Set description
public Sprite setIcon;                  // Set UI icon
public Color setColor = Color.yellow;   // Set theme color

[Header("Set Pieces (Item IDs)")]
public List<string> setPieceIds;        // List of itemIds in this set
```

### 5.3 2-Piece and 4-Piece Bonuses

```csharp
[Header("2-Piece Bonus")]
public bool has2PieceBonus = true;
public int damageBonus2 = 0;
public int defenseBonus2 = 0;
public float critBonus2 = 0f;
public float attackSpeedBonus2 = 0f;
public int maxHPBonus2 = 0;
public string bonusDescription2 = "+10 Damage";

[Header("4-Piece Bonus")]
public bool has4PieceBonus = true;
public int damageBonus4 = 0;
public int defenseBonus4 = 0;
public float critBonus4 = 0f;
public float attackSpeedBonus4 = 0f;
public int maxHPBonus4 = 0;
public string bonusDescription4 = "Special Set Effect";
public SetSpecialEffect specialEffect4 = SetSpecialEffect.None;
```

### 5.4 Special Effects

Available special effects for 4-piece bonus:

```csharp
public enum SetSpecialEffect {
    None,
    LifeSteal,          // Heal for % of damage dealt
    BurnOnHit,          // Apply burn status to enemies
    DoubleGold,         // 2x gold drops (NOT IMPLEMENTED)
    ShieldOnHit,        // Chance to gain shield when hit (NOT IMPLEMENTED)
    CriticalOverload,   // Crits deal AOE damage (NOT IMPLEMENTED)
    VampireTouch        // Damage heals you (NOT IMPLEMENTED)
}
```

**Implemented Effects:**
- ✅ `LifeSteal` - Adds `LifeStealEffect` component
- ✅ `BurnOnHit` - Adds `BurnOnHitEffect` component

**Note:** These effects require proper trigger integration (see Known Issues).

### 5.5 Complete Set Example

**Dragon Slayer Set Configuration:**

```
SetSO: Set_DragonSlayer
├── setId: "dragon_slayer"
├── setName: "Dragon Slayer"
├── setColor: #FF6600 (Orange)
├── setPieceIds:
│   ├── "dragon_sword_01"
│   └── "dragon_armor_01"
├── 2-Piece Bonus:
│   ├── damageBonus2: 15
│   ├── maxHPBonus2: 50
│   └── bonusDescription2: "+15 Damage, +50 HP"
└── 4-Piece Bonus:
    ├── has4PieceBonus: false  # Impossible with 2 slots!
    └── specialEffect4: None
```

**Matching Items:**
```
Item: Item_DragonSword
├── itemId: "dragon_sword_01"
├── setId: "dragon_slayer"
└── slot: Weapon

Item: Item_DragonArmor
├── itemId: "dragon_armor_01"
├── setId: "dragon_slayer"
└── slot: Armor
```

---

## 6. Known Critical Issues

### ⚠️ CRITICAL ISSUE 1: 4-Piece Bonus Impossible

**Problem:** The system only has 2 equipment slots (Weapon, Armor) but set bonuses require 4 pieces.

```csharp
// CountSetPieces only checks 2 slots!
int CountSetPieces(EquipmentSetSO set) {
    int count = 0;
    if (inventory.equippedWeapon != null && set.setPieceIds.Contains(inventory.equippedWeapon.itemId))
        count++;
    if (inventory.equippedArmor != null && set.setPieceIds.Contains(inventory.equippedWeapon.itemId))
        count++;
    return count;  // MAXIMUM POSSIBLE: 2
}
```

**Workarounds:**

**Option A: Use Only 2-Piece Bonuses**
```csharp
// Disable 4-piece bonuses in your set configurations
[Header("4-Piece Bonus")]
public bool has4PieceBonus = false;  // Disable it
```

**Option B: Add More Equipment Slots**
```csharp
// In InventoryManager.cs, add new slots:
public ItemSO equippedHelmet;    // Add this
public ItemSO equippedBoots;     // Add this

// Update CountSetPieces to check new slots
// Update Equip/Unequip methods
// Update RefreshPlayerStats
```

**Option C: Allow Inventory Items to Count**
```csharp
// Modify CountSetPieces to include inventory items
int CountSetPieces(EquipmentSetSO set) {
    int count = 0;
    // Check equipped items...
    
    // Also check inventory
    foreach (var item in inventory.Items) {
        if (set.setPieceIds.Contains(item.itemId))
            count++;
    }
    return count;
}
```

---

### ⚠️ CRITICAL ISSUE 2: LifeSteal/BurnOnHit Never Triggered

**Problem:** The `LifeStealEffect` and `BurnOnHitEffect` components are added to the player, but nothing calls their methods.

```csharp
// These components are added but NEVER triggered:
public class LifeStealEffect : MonoBehaviour {
    public void OnDealDamage(int damage) {  // Who calls this?
        int heal = Mathf.RoundToInt(damage * lifeStealPercent);
        player?.Heal(heal);
    }
}

public class BurnOnHitEffect : MonoBehaviour {
    public void ApplyBurn(Enemy enemy) {  // Who calls this?
        // ...
    }
}
```

**Fix - Add to PlayerController:**

```csharp
// In PlayerController.cs, add to your damage dealing method:
public void DealDamage(Enemy enemy, int damage) {
    // Apply damage
    enemy.TakeDamage(damage);
    
    // Trigger LifeSteal
    var lifeSteal = GetComponent<LifeStealEffect>();
    if (lifeSteal != null) {
        lifeSteal.OnDealDamage(damage);
    }
    
    // Trigger BurnOnHit
    var burnOnHit = GetComponent<BurnOnHitEffect>();
    if (burnOnHit != null) {
        burnOnHit.ApplyBurn(enemy);
    }
}
```

---

### ⚠️ CRITICAL ISSUE 3: Stacking Not Implemented

**Problem:** While `ItemSO` has stack fields, the inventory system doesn't actually support stacking.

```csharp
// These fields exist but are IGNORED:
public bool isStackable = false;
public int stackCount = 1;
public int maxStackSize = 99;

// AddItem simply adds duplicates:
public bool AddItem(ItemSO item) {
    items.Add(item);  // No stack checking!
    // ...
}
```

**Current Behavior:**
- Each item takes 1 slot regardless of stackability
- `CreateStack()` creates a copy but inventory doesn't merge stacks

**Workaround:** Don't use `isStackable = true` until implemented, or implement stacking:

```csharp
// Modified AddItem with stacking support
public bool AddItem(ItemSO item) {
    if (item.isStackable) {
        // Find existing stack
        foreach (var existing in items) {
            if (existing.itemId == item.itemId && existing.stackCount < existing.maxStackSize) {
                int space = existing.maxStackSize - existing.stackCount;
                int add = Mathf.Min(space, item.stackCount);
                existing.stackCount += add;
                item.stackCount -= add;
                if (item.stackCount <= 0) return true;
            }
        }
    }
    // ... rest of AddItem
}
```

---

### ⚠️ CRITICAL ISSUE 4: Gold Desync Risk

**Problem:** Gold is stored in a serialized field but modified through the property. Direct field modification in Inspector can cause desync.

```csharp
[SerializeField] private int gold = 0;  // Can be edited directly!
public int Gold {
    get => gold;
    private set {
        gold = value;
        OnGoldChanged?.Invoke(gold);  // Events only fire through property!
        OnInventoryChanged?.Invoke();
    }
}
```

**Risk:** Directly editing `gold` field in Inspector won't trigger events.

**Workaround:** Always use the property method:
```csharp
// Instead of: gold = 100;  (DON'T DO THIS)
// Use: AddGold(100); or RemoveGold(100);

// For initialization, ensure events fire:
public void SetInitialGold(int amount) {
    Gold = amount;  // Use property, not field
}
```

---

## 7. UI Integration

### 7.1 AutoBindingInventoryUI

The inventory UI automatically binds to `InventoryManager` events:

```csharp
// Subscribe to events
void OnEnable() {
    InventoryManager.Instance.OnInventoryChanged += RefreshUI;
    InventoryManager.Instance.OnItemEquipped += OnItemEquipped;
    InventoryManager.Instance.OnItemUnequipped += OnItemUnequipped;
    InventoryManager.Instance.OnGoldChanged += UpdateGoldDisplay;
}

void OnDisable() {
    InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    InventoryManager.Instance.OnItemEquipped -= OnItemEquipped;
    InventoryManager.Instance.OnItemUnequipped -= OnItemUnequipped;
    InventoryManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
}
```

### 7.2 Key Events for UI

| Event | Triggered When | Use For |
|-------|----------------|---------|
| `OnInventoryChanged` | Any inventory change | Refresh item grid |
| `OnItemEquipped(ItemSO)` | Item equipped | Update equipment slots |
| `OnItemUnequipped(ItemSO)` | Item unequipped | Update equipment slots |
| `OnGoldChanged(int)` | Gold amount changes | Update gold display |

### 7.3 Set Bonus UI Integration

```csharp
// Subscribe to SetBonusManager events
void Start() {
    SetBonusManager.Instance.OnSetBonusActivated += (set, pieces) => {
        ShowSetBonusPopup(set, pieces);
    };
    
    SetBonusManager.Instance.OnSetBonusLost += (set) => {
        ShowSetBonusLost(set);
    };
    
    SetBonusManager.Instance.OnSetProgressChanged += (set, count) => {
        UpdateSetProgressUI(set, count);
    };
}
```

### 7.4 Tooltips

Items provide formatted tooltip text:

```csharp
// Basic tooltip
string tooltip = item.GetTooltipText();

// Tooltip with set bonus info
var set = SetBonusManager.Instance.GetSetsForItem(item).FirstOrDefault();
int pieceCount = SetBonusManager.Instance.GetPieceCount(set);
string tooltipWithSet = item.GetTooltipTextWithSetBonus(set, pieceCount);
```

---

## 8. Testing Checklist

### 8.1 Basic Functionality

- [ ] Create `InventoryManager` GameObject in scene
- [ ] Verify singleton pattern (only one instance persists)
- [ ] Test `AddItem()` - items appear in inventory
- [ ] Test `RemoveItem()` - items removed from inventory
- [ ] Verify max inventory slots enforcement
- [ ] Test inventory full notification

### 8.2 Equipment System

- [ ] Create test Weapon ItemSO
- [ ] Create test Armor ItemSO
- [ ] Test `Equip()` - item moves to equipment slot
- [ ] Test `UnequipSlot()` - item returns to inventory
- [ ] Verify stat changes when equipping/unequipping
- [ ] Test inventory full during unequip (should fail gracefully)
- [ ] Verify visual updates (weapon/armor sprites)

### 8.3 Gold System

- [ ] Test `AddGold()` - gold increases
- [ ] Test `RemoveGold()` - gold decreases
- [ ] Test `RemoveGold()` with insufficient funds (should return false)
- [ ] Verify `OnGoldChanged` event fires
- [ ] Test `GetSellPrice()` calculation

### 8.4 Set Bonus System

- [ ] Create EquipmentSetSO in `Resources/Sets` folder
- [ ] Assign matching `setId` to items
- [ ] Add `setPieceIds` to EquipmentSetSO
- [ ] Equip one set piece - verify no bonus
- [ ] Equip two set pieces - verify 2-piece bonus activates
- [ ] Verify `OnSetBonusActivated` event fires
- [ ] Unequip one piece - verify `OnSetBonusLost` fires
- [ ] Test notification display for set bonuses

### 8.5 Known Issues Verification

- [ ] ⚠️ Verify 4-piece bonus is IMPOSSIBLE to achieve
- [ ] ⚠️ Verify LifeSteal doesn't work without fix
- [ ] ⚠️ Verify BurnOnHit doesn't work without fix
- [ ] ⚠️ Verify stackable items still take multiple slots

### 8.6 Edge Cases

- [ ] Equip item not in inventory (should still work)
- [ ] Unequip empty slot (should do nothing)
- [ ] Add null item (should return false)
- [ ] Remove item not in inventory (should do nothing)
- [ ] Scene transition with `DontDestroyOnLoad`
- [ ] Multiple InventoryManager instances (should destroy duplicates)

### 8.7 Integration Tests

- [ ] Shop buy/sell integration
- [ ] Save/Load system integration
- [ ] UI binding updates correctly
- [ ] Player stats properly recalculated
- [ ] Visual equipment updates correctly

---

## Quick Reference

### Common Code Snippets

```csharp
// Get inventory manager
var inv = InventoryManager.Instance;

// Add item to inventory
bool added = inv.AddItem(itemSO);

// Equip item
inv.Equip(itemSO);

// Get equipped items
ItemSO weapon = inv.equippedWeapon;
ItemSO armor = inv.equippedArmor;

// Check set bonus
var setManager = SetBonusManager.Instance;
bool hasBonus = setManager.HasSetBonus(dragonSet, 2);
int pieceCount = setManager.GetPieceCount(dragonSet);

// Gold operations
inv.AddGold(100);
bool canAfford = inv.RemoveGold(50);
int gold = inv.Gold;
```

### File Locations

| File | Path |
|------|------|
| InventoryManager | `Assets/Scripts/Inventory/InventoryManager.cs` |
| SetBonusManager | `Assets/Scripts/Inventory/SetBonusManager.cs` |
| ItemSO | `Assets/Scripts/Data/ItemSO.cs` |
| EquipmentSetSO | `Assets/Scripts/Data/EquipmentSetSO.cs` |
| Sets Folder | `Assets/Resources/Sets/` |

---

*Last Updated: 2026-03-08*
*Version: 1.0*

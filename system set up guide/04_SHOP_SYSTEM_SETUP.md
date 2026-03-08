# Shop System Setup Guide

A comprehensive guide for setting up the Shop System, Gamble System, and their UI integration in CombatRun.

---

## Table of Contents

1. [Overview](#1-overview)
2. [ShopManager Setup](#2-shopmanager-setup)
3. [ShopUI Setup](#3-shopui-setup)
4. [Buy/Sell Flow](#4-buysell-flow)
5. [Gamble System](#5-gamble-system)
6. [Known Issues](#6-known-issues)
7. [UI Integration](#7-ui-integration)
8. [Testing Checklist](#8-testing-checklist)

---

## 1. Overview

### System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         SHOP SYSTEM ARCHITECTURE                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐       │
│  │  ShopManager │◄────►│    ShopUI    │◄────►│  UIManager   │       │
│  │  (Singleton) │      │  (UI Panel)  │      │              │       │
│  └──────┬───────┘      └──────┬───────┘      └──────────────┘       │
│         │                     │                                      │
│         ▼                     ▼                                      │
│  ┌──────────────┐      ┌──────────────┐                             │
│  │  ItemSO[]    │      │  Shop Slots  │                             │
│  │ Available    │      │  Preview     │                             │
│  │ CurrentStock │      │  Buy/Sell    │                             │
│  └──────────────┘      └──────────────┘                             │
│         ▲                                                            │
│         │                                                            │
│  ┌──────────────┐      ┌──────────────┐                             │
│  │GambleSystem  │◄────►│ Player Stats │                             │
│  │(Shop Addon)  │      │ Inventory    │                             │
│  └──────────────┘      └──────────────┘                             │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Features

| Feature | Description |
|---------|-------------|
| **Weighted Stock** | Shop stock generation uses rarity-based weighting (Common: 40, Uncommon: 30, Rare: 20, Epic: 8, Legendary: 2) |
| **Auto-Refresh** | Stock refreshes every 5 minutes or on shop open (configurable) |
| **Price Multiplier** | Regional/reputation-based price adjustments via `priceMultiplier` |
| **Stat Comparison** | Real-time preview showing stat changes before purchase |
| **Gamble Integration** | Optional gambling mini-games accessible from shop |
| **Event-Driven** | Uses C# events for loose coupling between systems |

---

## 2. ShopManager Setup

### 2.1 Creating the Singleton

Create an empty GameObject in your scene and attach the `ShopManager` script:

```csharp
// ShopManager.cs - Already implements singleton pattern
public class ShopManager : MonoBehaviour {
    public static ShopManager Instance { get; private set; }
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
}
```

**Setup Steps:**
1. Create empty GameObject: `GameObject > Create Empty`
2. Rename to "ShopManager"
3. Add component: `ShopManager`
4. Move to Managers scene or use `DontDestroyOnLoad`

### 2.2 Adding Items to AvailableItems

The `availableItems` list contains all items the shop CAN sell. Current stock is randomly selected from this pool.

```csharp
[Header("Stock")]
public List<ItemSO> availableItems = new List<ItemSO>();  // Pool of possible items
public List<ItemSO> currentStock = new List<ItemSO>();    // Currently displayed items
public int shopSlots = 6;                                  // Number of slots to fill
```

**Unity Inspector Setup:**

```
ShopManager (Script)
├── Stock
│   ├── Available Items
│   │   ├── Size: 10
│   │   ├── Element 0: [Iron Sword SO]
│   │   ├── Element 1: [Leather Armor SO]
│   │   ├── Element 2: [Health Potion SO]
│   │   └── ...
│   ├── Current Stock (auto-populated)
│   └── Shop Slots: 6
```

**Programmatic Population:**

```csharp
// Option 1: Add single item
ShopManager.Instance.AddToAvailableItems(itemSO);

// Option 2: Set entire list
ShopManager.Instance.SetAvailableItems(itemList);

// Option 3: Load from Resources
var allItems = Resources.LoadAll<ItemSO>("Items");
ShopManager.Instance.SetAvailableItems(new List<ItemSO>(allItems));
```

### 2.3 Configuring Stock Refresh

```csharp
[Header("Refresh")]
public bool autoRefreshOnOpen = true;   // Refresh when shop UI opens
public float refreshInterval = 300f;     // 5 minutes auto-refresh
```

**Refresh Modes:**

| Mode | Configuration | Behavior |
|------|--------------|----------|
| Manual Only | `autoRefreshOnOpen = false`, `refreshInterval = 0` | Only refreshes when `ForceRefresh()` called |
| Open Refresh | `autoRefreshOnOpen = true`, `refreshInterval = 0` | Refreshes each time shop opens |
| Timed Refresh | `autoRefreshOnOpen = false`, `refreshInterval > 0` | Refreshes on timer only |
| Both | `autoRefreshOnOpen = true`, `refreshInterval > 0` | Refreshes on both open and timer |

**Manual Refresh:**
```csharp
// From UI button
public void OnRefreshShop() {
    ShopManager.Instance.ForceRefresh();
}
```

### 2.4 Pricing Configuration

**Buy Price Formula:**
```
Buy Price = item.price × priceMultiplier
```

```csharp
[Header("Pricing")]
public float priceMultiplier = 1f;  // 1.0 = normal, 0.8 = 20% discount, 1.5 = 50% markup
```

**Regional Pricing Examples:**

| Region | priceMultiplier | Effect |
|--------|----------------|--------|
| Village | 0.8f | 20% cheaper |
| City | 1.0f | Standard prices |
| Dungeon | 1.5f | 50% expensive |
| Black Market | 0.6f | 40% cheaper but items are cursed |

**Dynamic Pricing by Reputation:**
```csharp
public void UpdatePriceByReputation(int reputation) {
    // Reputation: -100 to +100
    priceMultiplier = 1f - (reputation * 0.002f); // Max 20% discount at +100 rep
}
```

**Sell Price Formula:**
```
Sell Price = item.sellPrice × 0.5
```

> ⚠️ **Note:** The sell price uses `item.sellPrice` (not `item.price`), then applies 50%. This is intentional - shops buy items at half their sell value.

---

## 3. ShopUI Setup

### 3.1 Creating Shop Canvas

Create a new Canvas for the shop interface:

```
Hierarchy Structure:
├── ShopCanvas (Canvas)
│   └── ShopPanel (GameObject with UIPanel component)
│       └── ShopUI (Script component)
```

**Canvas Settings:**
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size (Reference: 1920x1080)
- Graphic Raycaster: Default

### 3.2 Shop Slot Prefab Structure

Create the shop slot prefab with this hierarchy:

```
ShopSlotPrefab (Prefab)
├── Button (Button component - root)
│   ├── Background (Image - slot background)
│   ├── Icon (Image - item icon)
│   ├── RarityBorder (Image - colored border based on rarity)
│   ├── Name (TextMeshProUGUI - item name)
│   └── Price (TextMeshProUGUI - "100 G")
```

**Prefab Visual Diagram:**

```
┌─────────────────────────────┐  ← Button (root)
│ ┌───────────────────────┐   │
│ │     ┌─────────┐       │   │  ← Background (Image)
│ │     │  ICON   │       │   │  ← Icon (Image)
│ │     └─────────┘       │   │
│ │     Item Name         │   │  ← Name (TextMeshProUGUI)
│ │     100 G             │   │  ← Price (TextMeshProUGUI)
│ └───────────────────────┘   │
└─────────────────────────────┘
```

**Required Components:**
- `Button` (root GameObject)
- `ShopSlotHover` (added dynamically by ShopUI)

**Script Reference in ShopUI:**
```csharp
[Header("Shop Grid")]
public Transform shopGrid;           // Parent for slots (GridLayoutGroup recommended)
public GameObject shopSlotPrefab;    // Prefab defined above
```

### 3.3 Preview Panel Setup

The preview panel displays detailed item information when a shop slot is clicked:

```
PreviewPanel (GameObject)
├── Background (Image)
├── ItemInfo
│   ├── Icon (Image)
│   ├── Name (TextMeshProUGUI)
│   └── Description (TextMeshProUGUI)
├── Price (TextMeshProUGUI) - "Price: 100 Gold"
├── StatsComparison
│   ├── CurrentStats
│   │   ├── Damage: [currentDamageText]
│   │   ├── Defense: [currentDefenseText]
│   │   └── Crit: [currentCritText]
│   └── PreviewStats
│       ├── Damage: [previewDamageText]
│       ├── Defense: [previewDefenseText]
│       └── Crit: [previewCritText]
└── BuyButton (Button)
```

**ShopUI Inspector Assignment:**

```csharp
[Header("Preview Panel")]
public GameObject previewPanel;
public Image previewIcon;
public TextMeshProUGUI previewName;
public TextMeshProUGUI previewDescription;
public TextMeshProUGUI previewPrice;
public Button buyButton;
```

### 3.4 Stat Comparison UI

The stat comparison shows current stats vs. stats after equipping:

```
┌─────────────────────────────────────┐
│         STAT COMPARISON             │
├─────────────────────────────────────┤
│           CURRENT   ►   AFTER       │
├─────────────────────────────────────┤
│ Damage:     25      ►     35  (green)│
│ Defense:    10      ►     10  (white)│
│ Crit:       5%      ►     8%  (green)│
└─────────────────────────────────────┘
```

**Color Coding:**
- **Green** = Stat increase
- **Red** = Stat decrease
- **White** = No change

**UI Assignment:**
```csharp
[Header("Stats Comparison")]
public TextMeshProUGUI currentDamageText;
public TextMeshProUGUI previewDamageText;
public TextMeshProUGUI currentDefenseText;
public TextMeshProUGUI previewDefenseText;
public TextMeshProUGUI currentCritText;
public TextMeshProUGUI previewCritText;
```

---

## 4. Buy/Sell Flow

### 4.1 Transaction Process

**Buy Flow Diagram:**

```
┌─────────┐    Click     ┌──────────┐    Validate     ┌───────────┐
│ Shop Slot│────────────►│ SelectItem│────────────────►│  Check    │
└─────────┘             └──────────┘                 │ Inventory │
                                                      └─────┬─────┘
                                                            │
                    ┌───────────────────────────────────────┼───────┐
                    │                                       ▼       │
                    │                              ┌─────────────┐  │
                    │                         No   │ Has Space?  │  │
                    │                    ┌─────────┤             │  │
                    │                    │         └─────────────┘  │
                    │                    │                │ Yes    │
                    │                    ▼                ▼        │
                    │         ┌─────────────┐      ┌───────────┐   │
                    │         │ Show "Full" │      │ Check Gold│   │
                    │         │  Message    │      └─────┬─────┘   │
                    │         └─────────────┘            │         │
                    │                              ┌─────┴─────┐   │
                    │                         No   │  Enough   │   │
                    │                    ┌─────────┤   Gold?   │   │
                    │                    │         └───────────┘   │
                    │                    │                │ Yes    │
                    │                    ▼                ▼        │
                    │         ┌─────────────┐      ┌───────────┐   │
                    │         │Show "Need    │      │PreviewEquip│  │
                    │         │More Gold"   │      │Update Stats│  │
                    │         └─────────────┘      └─────┬─────┘   │
                    │                                    │         │
                    │                                    ▼         │
                    │                           ┌──────────────┐   │
                    └──────────────────────────►│ Buy Button   │   │
                                                │  Clicked     │   │
                                                └──────┬───────┘   │
                                                       │           │
                                                       ▼           │
                                                ┌──────────────┐   │
                                                │ Remove Gold  │   │
                                                │ Add to Inv   │   │
                                                │ Clear Slot   │   │
                                                └──────────────┘   │
                                                                  │
```

### 4.2 Validation Checks

**BuyItem Validation:**
```csharp
public bool BuyItem(int slotIndex) {
    // 1. Slot index valid?
    if (slotIndex < 0 || slotIndex >= currentStock.Count) return false;
    
    // 2. Item exists?
    var item = currentStock[slotIndex];
    if (item == null) return false;
    
    // 3. InventoryManager available?
    if (InventoryManager.Instance == null) return false;
    
    // 4. Player has enough gold?
    if (InventoryManager.Instance.Gold < finalPrice) {
        UIManager.Instance?.ShowNotification("Not enough gold!");
        return false;
    }
    
    // 5. Inventory has space?
    if (!InventoryManager.Instance.HasInventorySpace()) {
        UIManager.Instance?.ShowNotification("Inventory full!");
        return false;
    }
    
    // Process purchase...
}
```

**SellItem Validation (CURRENTLY BROKEN - See Section 6):**
```csharp
public bool SellItem(ItemSO item) {
    // ⚠️ MISSING: Check if player actually owns the item!
    // ⚠️ MISSING: Check if item is sellable!
    
    if (item == null || InventoryManager.Instance == null) return false;
    
    // Directly removes item without validation!
    InventoryManager.Instance.RemoveItem(item);
    InventoryManager.Instance.AddGold(sellPrice);
}
```

### 4.3 Price Calculation

**Complete Pricing Formulas:**

```csharp
// BUY PRICE
public int GetBuyPrice(ItemSO item) {
    if (item == null) return 0;
    return Mathf.RoundToInt(item.price * priceMultiplier);
}

// SELL PRICE (with 50% shop markup)
public int GetSellPrice(ItemSO item) {
    if (item == null) return 0;
    return Mathf.RoundToInt(item.sellPrice * 0.5f);
}
```

**Price Example:**

| Item | item.price | item.sellPrice | Shop Buy Price (×1.0) | Player Sell Price (×0.5) |
|------|------------|----------------|----------------------|-------------------------|
| Iron Sword | 100 | 50 | 100 G | 25 G |
| Magic Staff | 500 | 250 | 500 G | 125 G |
| Legendary Blade | 5000 | 2500 | 5000 G | 1250 G |

---

## 5. Gamble System

### 5.1 Gamble Types and Mechanics

The `GambleSystem` provides risk/reward mini-games accessible from the shop:

```
┌─────────────────────────────────────────────────────────────────┐
│                     GAMBLE TYPE OVERVIEW                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │Double or Nothing│  │  Mystery Item   │  │  Health for Gold│ │
│  │   (50% / 50%)   │  │   (30% Rare+)   │  │  (Always Works) │ │
│  │                 │  │                 │  │                 │ │
│  │ Win: 2× Gold    │  │ Win: Rare Item  │  │ -30% HP         │ │
│  │ Loss: ½ Gold    │  │ Loss: Common    │  │ +200 Gold       │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│                                                                  │
│  ┌─────────────────┐  ┌─────────────────┐                       │
│  │  Dark Bargain   │  │  Chaos Reroll   │                       │
│  │  (Always Works) │  │  (Always Works) │                       │
│  │                 │  │                 │                       │
│  │ Cursed Item     │  │ Random Stats    │                       │
│  │ Negative Effect │  │ 5-30 Dmg        │                       │
│  │                 │  │ 0-15 Def        │                       │
│  └─────────────────┘  └─────────────────┘                       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 GambleOption Configuration

```csharp
[System.Serializable]
public class GambleOption {
    public string optionName;           // Display name
    public string description;          // Tooltip/description
    public int goldCost;                // Cost to attempt (0 = free)
    public GambleType type;             // Type enum
    public float successChance = 0.5f;  // 0.0 - 1.0 probability
    public int minReward;               // Min gold reward
    public int maxReward;               // Max gold reward
    public ItemRarity guaranteedRarityOnSuccess;
    public string failureDescription;   // Message on failure
}
```

**Default Gambles (Auto-initialized):**

| Name | Type | Cost | Success | Win | Loss |
|------|------|------|---------|-----|------|
| Double or Nothing | GoldDoubleOrNothing | 0 | 50% | Double gold | Lose half |
| Mystery Item | MysteryItem | 100 | 30% | Rare+ item | Common item |
| Blood Money | HealthForGold | 0 | 100% | 200 gold | -30% HP |
| Dark Bargain | CursedItem | 50 | 100% | Cursed item | N/A |
| Chaos Reroll | RerollStats | 25 | 100% | Random stats | N/A |

### 5.3 Integration with Shop

**Scene Setup:**

```
GambleManager (GameObject)
├── GambleSystem (Script)
│   ├── Available Gambles (auto-populated)
│   ├── Cursed Items (assign cursed ItemSOs)
│   ├── Player (auto-found or assign)
│   ├── Inventory (auto-found)
│   └── Shop (auto-found)
```

**UI Integration Flow:**

```
Shop UI
    │
    ├── [Normal Shop Tab] ──► Shop Slots
    │
    └── [Gamble Tab] ───────► GambleUI
                                    │
                                    ├── GambleOption List
                                    │       ├── Double or Nothing
                                    │       ├── Mystery Item
                                    │       └── ...
                                    │
                                    └── ExecuteGamble(option)
                                                │
                                                ▼
                                        GambleSystem.ResolveXxx()
```

**Creating Gamble UI:**

```csharp
public class GambleUI : MonoBehaviour {
    public Transform gambleList;
    public GameObject gambleOptionPrefab;
    
    void Start() {
        foreach (var option in GambleSystem.Instance.availableGambles) {
            CreateGambleOptionButton(option);
        }
    }
    
    void CreateGambleOptionButton(GambleOption option) {
        var button = Instantiate(gambleOptionPrefab, gambleList);
        button.GetComponentInChildren<TextMeshProUGUI>().text = 
            $"{option.optionName}\n{option.description}";
        
        button.GetComponent<Button>().onClick.AddListener(() => {
            GambleSystem.Instance.ExecuteGamble(option);
        });
    }
}
```

### 5.4 Risk/Reward Configuration

**Cursed Items Setup:**

```csharp
[Header("Cursed Items Pool")]
public List<ItemSO> cursedItems = new List<ItemSO>();
```

Create ItemSOs with IDs containing curse keywords:

| Item ID Contains | Curse Effect |
|------------------|--------------|
| "vampire" | LifeDrainCurse: +10 damage, -5 HP every 5s |
| "glass" | Double damage dealt AND taken, -10 defense |

**Curse Effects:**

```csharp
// LifeDrainCurse component (auto-added)
public class LifeDrainCurse : MonoBehaviour {
    public float drainInterval = 5f;
    public int drainAmount = 5;
    public int damageBonus = 10;
    
    // Applied on curse acquisition
    // Removed when RemoveAllCurses() called
}
```

**Stat Reroll Ranges:**

```csharp
// Chaos Reroll applies these ranges:
player.stats.baseDamage = Random.Range(5, 30);
player.stats.baseDefense = Random.Range(0, 15);
player.stats.baseCrit = Random.Range(0f, 0.3f);
player.stats.baseAttackSpeed = Random.Range(0.5f, 2f);
```

---

## 6. Known Issues

### 6.1 SellItem Validation Missing

**Issue:** `SellItem()` does not validate that the player actually owns the item before removing it.

**Current Code:**
```csharp
public bool SellItem(ItemSO item) {
    if (item == null || InventoryManager.Instance == null) return false;
    
    int sellPrice = GetSellPrice(item);
    
    // ⚠️ NO VALIDATION that item is in inventory!
    InventoryManager.Instance.RemoveItem(item);
    InventoryManager.Instance.AddGold(sellPrice);
    
    OnItemSold?.Invoke(item);
    return true;
}
```

**Fix:**
```csharp
public bool SellItem(ItemSO item) {
    if (item == null || InventoryManager.Instance == null) return false;
    
    // Check if player actually owns the item
    if (!InventoryManager.Instance.HasItem(item)) {
        UIManager.Instance?.ShowNotification("You don't own this item!");
        return false;
    }
    
    // Check if item is sellable
    if (!item.isSellable) {
        UIManager.Instance?.ShowNotification("This item cannot be sold!");
        return false;
    }
    
    int sellPrice = GetSellPrice(item);
    
    // Remove gold first (safer transaction order)
    InventoryManager.Instance.RemoveItem(item);
    InventoryManager.Instance.AddGold(sellPrice);
    
    OnItemSold?.Invoke(item);
    return true;
}
```

### 6.2 Sell Price Confusion

**Issue:** Documentation says "10% of buy price" but code implements "50% of item.sellPrice".

**Current Code:**
```csharp
public int GetSellPrice(ItemSO item) {
    if (item == null) return 0;
    return Mathf.RoundToInt(item.sellPrice * 0.5f); // 50% of sellPrice
}
```

**Clarification:**
- The sell price uses `item.sellPrice` field, NOT `item.price`
- Then applies 50% multiplier
- If you want 10% of buy price, use this formula instead:

```csharp
// Alternative: 10% of buy price
public int GetSellPrice(ItemSO item) {
    if (item == null) return 0;
    return Mathf.RoundToInt(GetBuyPrice(item) * 0.1f); // 10% of buy price
}
```

**Recommended Fix (document current behavior):**
```csharp
/// <summary>
/// Gets the sell price for an item.
/// Returns 50% of the item's sellPrice value.
/// Note: This uses item.sellPrice, not item.price
/// </summary>
public int GetSellPrice(ItemSO item) {
    if (item == null) return 0;
    return Mathf.RoundToInt(item.sellPrice * 0.5f);
}
```

### 6.3 GambleSystem AddGold Bug

**Issue:** `ResolveHealthForGold()` calls `player.AddGold(200)` instead of `inventory.AddGold(200)`.

**Current Code:**
```csharp
void ResolveHealthForGold() {
    int healthLoss = Mathf.RoundToInt(player.stats.MaxHP * 0.3f);
    player.TakeDamage(healthLoss);
    player.AddGold(200);  // ⚠️ PlayerController may not have AddGold!
    // ...
}
```

**Fix:**
```csharp
void ResolveHealthForGold() {
    int healthLoss = Mathf.RoundToInt(player.stats.MaxHP * 0.3f);
    player.TakeDamage(healthLoss);
    
    // Use InventoryManager for gold operations
    if (inventory != null) {
        inventory.AddGold(200);
    }
    
    if (UIManager.Instance != null) {
        UIManager.Instance.ShowNotification("Sacrificed HP for gold!");
    }
}
```

---

## 7. UI Integration

### 7.1 Event Subscriptions

**ShopUI Event Subscriptions:**

```csharp
void Start() {
    // Subscribe to ShopManager events
    if (shop != null) {
        shop.OnShopRefreshed += RefreshShopItems;
        shop.OnItemPurchased += OnItemPurchased;
    }
    
    // Subscribe to InventoryManager events
    if (inventory != null) {
        inventory.OnGoldChanged += UpdateGoldDisplay;
    }
}

void OnDestroy() {
    // CRITICAL: Always unsubscribe to prevent memory leaks
    if (shop != null) {
        shop.OnShopRefreshed -= RefreshShopItems;
        shop.OnItemPurchased -= OnItemPurchased;
    }
    
    if (inventory != null) {
        inventory.OnGoldChanged -= UpdateGoldDisplay;
    }
}
```

**Available Events:**

| Event | Source | Triggered When |
|-------|--------|----------------|
| `OnShopRefreshed` | ShopManager | Stock regenerates |
| `OnItemPurchased` | ShopManager | Successful purchase |
| `OnItemSold` | ShopManager | Successful sale |
| `OnGoldChanged` | InventoryManager | Gold amount changes |
| `OnGambleResolved` | GambleSystem | Gamble completes |

### 7.2 Gold Display Updates

```csharp
void UpdateGoldDisplay(int gold) {
    if (goldText != null) {
        goldText.text = $"Gold: {gold:N0}";  // Formatted with commas
    }
    
    // Update buy button state based on affordability
    if (selectedItem != null && buyButton != null && shop != null) {
        int price = shop.GetBuyPrice(selectedItem);
        buyButton.interactable = (inventory?.Gold ?? 0) >= price;
    }
}
```

**Gold Format Examples:**

| Raw Value | Display (N0 format) |
|-----------|---------------------|
| 100 | "Gold: 100" |
| 1500 | "Gold: 1,500" |
| 1000000 | "Gold: 1,000,000" |

### 7.3 Equipment Preview

The preview system temporarily applies item stats to show comparison:

```csharp
void SelectItem(int index) {
    selectedItem = shop.currentStock[index];
    
    // End any previous preview
    inventory?.EndPreview();
    
    // Apply preview stats
    inventory?.PreviewEquip(selectedItem);
    
    // Update UI with comparison
    UpdateStatComparison();
}

void UpdateStatComparison() {
    // Get base stats (without preview item)
    int currentDamage = player.stats.Damage - selectedItem.damageBonus;
    
    // Get preview stats (with preview item applied)
    int previewDamage = player.stats.Damage;
    
    // Display with color coding
    UpdateStatText(currentDamageText, currentDamage, previewDamage);
    UpdateStatText(previewDamageText, previewDamage, currentDamage);
}
```

**Preview Cleanup:**
```csharp
public void OnEndPreview() {
    inventory?.EndPreview();  // Restore actual equipment
    previewPanel?.SetActive(false);
    selectedItem = null;
    selectedSlotIndex = -1;
}
```

---

## 8. Testing Checklist

### 8.1 ShopManager Tests

| Test | Steps | Expected Result |
|------|-------|-----------------|
| Singleton | Open multiple scenes | Only one ShopManager exists |
| Stock Refresh | Open shop | 6 items displayed (matching shopSlots) |
| Weighted Rarity | Refresh 100 times | Common > Uncommon > Rare > Epic > Legendary frequency |
| Auto-Refresh Timer | Wait 5 minutes | Stock auto-refreshes |
| Force Refresh | Click refresh button | Stock immediately refreshes |
| Empty Pool | Clear availableItems | Warning logged, empty shop |

### 8.2 Buy Flow Tests

| Test | Steps | Expected Result |
|------|-------|-----------------|
| Successful Buy | Click item → Buy | Gold deducted, item added, slot cleared |
| Insufficient Gold | Set gold < price → Buy | "Not enough gold" notification |
| Full Inventory | Fill inventory → Buy | "Inventory full" notification |
| Buy Button State | Select expensive item | Button disabled if can't afford |
| Double Buy | Click buy rapidly | Only one purchase processed |

### 8.3 Sell Flow Tests

| Test | Steps | Expected Result |
|------|-------|-----------------|
| **BUG: Sell Without Item** | Call SellItem(itemNotInInventory) | Should fail (currently succeeds!) |
| **BUG: AddGold on Player** | Use Blood Money gamble | Gold added via InventoryManager (fix applied) |
| Normal Sell | Sell owned item | Gold added, item removed |

### 8.4 Gamble System Tests

| Test | Steps | Expected Result |
|------|-------|-----------------|
| Double or Nothing - Win | Execute with success | Gold doubled |
| Double or Nothing - Loss | Execute with failure | Gold halved |
| Mystery Item - Success | Execute with 30% luck | Rare+ item received |
| Mystery Item - Full Inv | Fill inventory → Execute | "Inventory full", gold refunded |
| Blood Money | Execute | -30% HP, +200 gold |
| Dark Bargain | Execute | Cursed item added, effect applied |
| Chaos Reroll | Execute | Stats randomized within ranges |
| Curse Cleanup | Call RemoveAllCurses() | All curse components destroyed |

### 8.5 UI Integration Tests

| Test | Steps | Expected Result |
|------|-------|-----------------|
| Gold Update | Buy/sell items | Gold text updates immediately |
| Stat Preview | Click item | Preview panel shows stat comparison |
| Stat Colors | Compare better/worse items | Green/Red/White coloring correct |
| Preview Cleanup | Close shop | Stats restored to actual equipment |
| Event Unsubscribe | Open/close shop multiple times | No duplicate event handlers |
| Memory Leak | Profile after 100 shop opens | No accumulated ShopSlot objects |

### 8.6 Edge Cases

| Test | Steps | Expected Result |
|------|-------|-----------------|
| Null Item in Stock | Manually set currentStock[0] = null | Slot skipped, no crash |
| Missing References | Delete InventoryManager | Error logged, graceful degradation |
| Zero Gold Double-or-Nothing | Execute with 0 gold | Notification shown, no change |
| Negative Price Multiplier | Set priceMultiplier = -1 | Price calculation handles gracefully |

---

## Quick Reference

### Price Formula Summary

```
BUY PRICE  = item.price × priceMultiplier
SELL PRICE = item.sellPrice × 0.5
```

### Key Events

```csharp
// Shop events
ShopManager.Instance.OnShopRefreshed += YourHandler;
ShopManager.Instance.OnItemPurchased += YourHandler;
ShopManager.Instance.OnItemSold += YourHandler;

// Inventory events  
InventoryManager.Instance.OnGoldChanged += YourHandler;

// Gamble events
GambleSystem.Instance.OnGambleResolved += YourHandler;
```

### Important Methods

```csharp
// Shop operations
ShopManager.Instance.RefreshStock();
ShopManager.Instance.ForceRefresh();
ShopManager.Instance.BuyItem(slotIndex);
ShopManager.Instance.SellItem(itemSO);  // ⚠️ Has validation issues

// Gamble operations
GambleSystem.Instance.ExecuteGamble(gambleOption);
GambleSystem.Instance.RemoveAllCurses();
GambleSystem.Instance.RestoreOriginalStats();

// UI operations
ShopUI.Instance.OnEndPreview();
ShopUI.Instance.OnRefreshShop();
ShopUI.Instance.OnCloseShop();
```

---

*Document Version: 1.0*
*Last Updated: 2026-03-08*
*Applies to: CombatRun Shop System v1.0*

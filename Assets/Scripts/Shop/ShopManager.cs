using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages shop stock, item purchasing/selling, and equipment preview.
/// </summary>
public class ShopManager : MonoBehaviour {
    public static ShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    public int shopSlotCount = 6;
    public float refreshCost = 50f;
    public ItemRarity[] rarityDistribution = { 
        ItemRarity.Common, ItemRarity.Common, ItemRarity.Common, 
        ItemRarity.Uncommon, ItemRarity.Uncommon, 
        ItemRarity.Rare 
    };

    [Header("Current Stock")]
    public List<ItemSO> currentStock = new List<ItemSO>();
    public List<int> currentPrices = new List<int>();

    [Header("All Items")]
    public List<ItemSO> allItems = new List<ItemSO>();

    [Header("References")]
    public InventoryManager inventory;
    public PlayerController player;

    [Header("Preview")]
    public ItemSO previewItem;
    private ItemSO previouslyEquippedItem;
    private EquipSlot previewSlot;
    private bool isPreviewing = false;
    
    // Cached items by rarity for performance
    private Dictionary<ItemRarity, List<ItemSO>> itemsByRarity;

    // Events
    public System.Action OnShopRefreshed;
    public System.Action<ItemSO> OnItemPreviewed;
    public System.Action<ItemSO> OnItemPurchased;
    public System.Action<ItemSO> OnItemSold;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        if (inventory == null) inventory = InventoryManager.Instance;
        if (player == null) {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) {
                Debug.LogError("[ShopManager] No PlayerController found!");
                enabled = false;
                return;
            }
        }
        
        LoadAllItems();
    }

    void LoadAllItems() {
        // Load all ItemSO from Resources
        ItemSO[] loadedItems = Resources.LoadAll<ItemSO>("Items");
        allItems.AddRange(loadedItems);
        
        // Build rarity cache for better performance
        BuildRarityCache();
    }
    
    /// <summary>
    /// Builds a cache of items organized by rarity for faster lookup.
    /// </summary>
    void BuildRarityCache() {
        itemsByRarity = new Dictionary<ItemRarity, List<ItemSO>>();
        
        foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity))) {
            itemsByRarity[rarity] = new List<ItemSO>();
        }
        
        foreach (var item in allItems) {
            if (item != null) {
                itemsByRarity[item.rarity].Add(item);
            }
        }
    }

    public void OpenShop() {
        if (currentStock.Count == 0) {
            RefreshStock();
        }
    }

    public void RefreshStock() {
        currentStock.Clear();
        currentPrices.Clear();

        for (int i = 0; i < shopSlotCount; i++) {
            ItemRarity rarity = rarityDistribution[Random.Range(0, rarityDistribution.Length)];
            ItemSO item = GetRandomItemOfRarity(rarity);
            
            if (item != null) {
                currentStock.Add(item);
                currentPrices.Add(item.price);
            }
        }

        OnShopRefreshed?.Invoke();
    }

    /// <summary>
    /// Gets a random item of the specified rarity using cached data.
    /// </summary>
    ItemSO GetRandomItemOfRarity(ItemRarity rarity) {
        if (itemsByRarity != null && itemsByRarity.TryGetValue(rarity, out var matchingItems)) {
            if (matchingItems.Count > 0) {
                return matchingItems[Random.Range(0, matchingItems.Count)];
            }
        }
        
        // Fallback to direct search if cache not available
        List<ItemSO> fallbackItems = allItems.FindAll(item => item != null && item.rarity == rarity);
        if (fallbackItems.Count > 0) {
            return fallbackItems[Random.Range(0, fallbackItems.Count)];
        }
        
        return null;
    }

    /// <summary>
    /// Previews an item on the character without purchasing.
    /// </summary>
    public void PreviewItem(ItemSO item) {
        if (item == null) return;
        
        if (isPreviewing) {
            EndPreview();
        }

        previewItem = item;
        isPreviewing = true;
        previewSlot = item.slot;

        // Store currently equipped item
        if (item.slot == EquipSlot.Weapon) {
            previouslyEquippedItem = inventory.equippedWeapon;
            player.SetWeaponVisual(item.itemSprite);
        } else if (item.slot == EquipSlot.Armor) {
            previouslyEquippedItem = inventory.equippedArmor;
            player.SetArmorVisual(item.itemSprite);
        }

        // Apply stats temporarily for preview
        inventory.PreviewEquip(item);

        OnItemPreviewed?.Invoke(item);
    }

    /// <summary>
    /// Ends the current preview and restores previous equipment.
    /// </summary>
    public void EndPreview() {
        if (!isPreviewing) return;

        // Restore previous equipment visual
        if (previewSlot == EquipSlot.Weapon) {
            player.SetWeaponVisual(previouslyEquippedItem?.itemSprite);
        } else if (previewSlot == EquipSlot.Armor) {
            player.SetArmorVisual(previouslyEquippedItem?.itemSprite);
        }

        // Restore stats
        inventory.EndPreview();

        previewItem = null;
        previouslyEquippedItem = null;
        isPreviewing = false;
    }

    /// <summary>
    /// Attempts to purchase an item from the shop.
    /// </summary>
    /// <param name="slotIndex">Index in the shop stock</param>
    /// <returns>True if purchase succeeded</returns>
    public bool PurchaseItem(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= currentStock.Count) return false;

        ItemSO item = currentStock[slotIndex];
        int price = currentPrices[slotIndex];

        if (player.gold >= price) {
            // Check if inventory has space
            if (inventory.items.Count >= inventory.maxInventorySlots) {
                if (UIManager.Instance != null) {
                    UIManager.Instance.ShowNotification("Inventory full!");
                }
                return false;
            }

            // Spend gold and add item
            player.SpendGold(price);
            inventory.AddItem(item);
            
            // Remove from shop
            currentStock.RemoveAt(slotIndex);
            currentPrices.RemoveAt(slotIndex);

            EndPreview();
            OnItemPurchased?.Invoke(item);
            return true;
        } else {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("Not enough gold!");
            }
            return false;
        }
    }

    public bool PurchaseItem(ItemSO item) {
        int index = currentStock.IndexOf(item);
        if (index >= 0) {
            return PurchaseItem(index);
        }
        return false;
    }

    /// <summary>
    /// Sells an item to the shop.
    /// </summary>
    public void SellItem(ItemSO item) {
        if (item == null) return;

        int sellPrice = inventory.GetSellPrice(item);
        
        // Check if equipped
        if (inventory.equippedWeapon == item || inventory.equippedArmor == item) {
            inventory.Unequip(item);
        } else {
            inventory.RemoveItem(item);
        }

        player.AddGold(sellPrice);
        OnItemSold?.Invoke(item);
    }

    public int GetPreviewDamage() => player?.stats?.Damage ?? 0;
    public int GetPreviewDefense() => player?.stats?.Defense ?? 0;
    public float GetPreviewCrit() => player?.stats?.Crit ?? 0f;

    public void CloseShop() {
        EndPreview();
    }
}

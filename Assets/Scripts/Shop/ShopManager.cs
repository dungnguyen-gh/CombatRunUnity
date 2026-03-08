using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages shop inventory, stock rotation, and purchase/sale logic.
/// Works with AutoBindingInventoryUI for automatic UI sync.
/// </summary>
public class ShopManager : MonoBehaviour {
    public static ShopManager Instance { get; private set; }
    
    [Header("Stock")]
    public List<ItemSO> availableItems = new List<ItemSO>();
    public List<ItemSO> currentStock = new List<ItemSO>();
    public int shopSlots = 6;
    
    [Header("Refresh")]
    public bool autoRefreshOnOpen = true;
    public float refreshInterval = 300f; // 5 minutes
    private float refreshTimer;
    
    [Header("Pricing")]
    public float priceMultiplier = 1f; // Can change based on reputation/region
    
    // Events
    public System.Action OnShopRefreshed;
    public System.Action<ItemSO> OnItemPurchased;
    public System.Action<ItemSO> OnItemSold;
    
    // References
    private InventoryManager inventory;
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    
    void Start() {
        inventory = InventoryManager.Instance;
        
        if (autoRefreshOnOpen) {
            RefreshStock();
        }
    }
    
    void OnDestroy() {
        // Cleanup any subscriptions if needed
    }
    
    void Update() {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval) {
            RefreshStock();
            refreshTimer = 0;
        }
    }
    
    /// <summary>
    /// Refreshes the shop stock with random items from available items.
    /// </summary>
    public void RefreshStock() {
        currentStock.Clear();
        
        if (availableItems.Count == 0) {
            Debug.LogWarning("[ShopManager] No available items to stock!");
            OnShopRefreshed?.Invoke();
            return;
        }
        
        // Create weighted pool based on rarity
        List<ItemSO> weightedPool = new List<ItemSO>();
        foreach (var item in availableItems) {
            int weight = item.rarity switch {
                ItemRarity.Common => 40,
                ItemRarity.Uncommon => 30,
                ItemRarity.Rare => 20,
                ItemRarity.Epic => 8,
                ItemRarity.Legendary => 2,
                _ => 10
            };
            
            for (int i = 0; i < weight; i++) {
                weightedPool.Add(item);
            }
        }
        
        // Fill stock slots
        for (int i = 0; i < shopSlots; i++) {
            if (weightedPool.Count > 0) {
                var randomItem = weightedPool[Random.Range(0, weightedPool.Count)];
                currentStock.Add(randomItem);
            }
        }
        
        OnShopRefreshed?.Invoke();
    }
    
    /// <summary>
    /// Buys an item from the shop.
    /// </summary>
    /// <param name="slotIndex">Index in currentStock</param>
    /// <returns>True if purchase succeeded</returns>
    public bool BuyItem(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= currentStock.Count) return false;
        
        var item = currentStock[slotIndex];
        if (item == null) return false;
        
        int finalPrice = GetBuyPrice(item);
        
        if (InventoryManager.Instance == null) {
            Debug.LogError("[ShopManager] No InventoryManager found!");
            return false;
        }
        
        if (InventoryManager.Instance.Gold < finalPrice) {
            UIManager.Instance?.ShowNotification("Not enough gold!");
            return false;
        }
        
        if (!InventoryManager.Instance.HasInventorySpace()) {
            UIManager.Instance?.ShowNotification("Inventory full!");
            return false;
        }
        
        // Process purchase
        InventoryManager.Instance.RemoveGold(finalPrice);
        InventoryManager.Instance.AddItem(item);
        currentStock[slotIndex] = null;
        
        OnItemPurchased?.Invoke(item);
        return true;
    }
    
    /// <summary>
    /// Sells an item to the shop.
    /// </summary>
    public bool SellItem(ItemSO item) {
        if (item == null || InventoryManager.Instance == null) return false;
        
        int sellPrice = GetSellPrice(item);
        
        InventoryManager.Instance.RemoveItem(item);
        InventoryManager.Instance.AddGold(sellPrice);
        
        OnItemSold?.Invoke(item);
        return true;
    }
    
    /// <summary>
    /// Gets the buy price for an item with multipliers applied.
    /// </summary>
    public int GetBuyPrice(ItemSO item) {
        if (item == null) return 0;
        return Mathf.RoundToInt(item.price * priceMultiplier);
    }
    
    /// <summary>
    /// Gets the sell price for an item.
    /// </summary>
    public int GetSellPrice(ItemSO item) {
        if (item == null) return 0;
        return Mathf.RoundToInt(item.sellPrice * 0.5f); // Shops buy at 50% of sell price
    }
    
    /// <summary>
    /// Adds an item to the available items pool.
    /// </summary>
    public void AddToAvailableItems(ItemSO item) {
        if (item != null && !availableItems.Contains(item)) {
            availableItems.Add(item);
        }
    }
    
    /// <summary>
    /// Clears and sets new available items.
    /// </summary>
    public void SetAvailableItems(List<ItemSO> items) {
        availableItems.Clear();
        if (items != null) {
            availableItems.AddRange(items);
        }
    }
    
    /// <summary>
    /// Forces a stock refresh.
    /// </summary>
    public void ForceRefresh() {
        refreshTimer = 0;
        RefreshStock();
    }
    
    /// <summary>
    /// Called when shop UI is opened.
    /// </summary>
    public void OpenShop() {
        if (autoRefreshOnOpen) {
            RefreshStock();
        }
    }
    
    /// <summary>
    /// Called when shop UI is closed.
    /// </summary>
    public void CloseShop() {
        // Could save state or trigger events here
    }
}

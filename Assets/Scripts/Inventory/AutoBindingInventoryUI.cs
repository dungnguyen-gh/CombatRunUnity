using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Inventory UI that automatically binds to components by naming convention.
/// No manual wiring required - just name your GameObjects correctly!
/// </summary>
public class AutoBindingInventoryUI : MonoBehaviour {
    [Header("Container References (Auto-discovered if null)")]
    public Transform itemSlotContainer;
    public Transform equipmentContainer;
    public Transform shopItemContainer;
    
    [Header("Prefabs")]
    public GameObject itemSlotPrefab;
    public GameObject shopItemPrefab;
    
    [Header("Dynamic Discovery Settings")]
    public bool autoDiscoverOnStart = true;
    public string slotContainerName = "ItemSlots";
    public string equipmentContainerName = "EquipmentSlots";
    public string shopContainerName = "ShopItems";
    
    // Auto-discovered components
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI capacityText;
    private Button closeButton;
    private Button sellButton;
    private Image[] equipmentSlots = new Image[2]; // Weapon, Armor
    private List<ItemSlotUI> itemSlots = new List<ItemSlotUI>();
    private List<ShopItemUI> shopItems = new List<ShopItemUI>();
    
    // State
    private ItemSO selectedItem;
    private InventoryManager inventory;
    private ShopManager shop;

    void Start() {
        if (autoDiscoverOnStart) {
            AutoDiscoverComponents();
        }
        
        inventory = InventoryManager.Instance;
        if (inventory != null) {
            inventory.OnInventoryChanged += RefreshInventoryUI;
            inventory.OnItemEquipped += OnItemEquipped;
            inventory.OnItemUnequipped += OnItemUnequipped;
        }
        
        shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) {
            shop.OnShopRefreshed += RefreshShopUI;
        }
        
        InitializeSlots();
        BindButtons();
        
        gameObject.SetActive(false);
    }

    void OnDestroy() {
        if (inventory != null) {
            inventory.OnInventoryChanged -= RefreshInventoryUI;
            inventory.OnItemEquipped -= OnItemEquipped;
            inventory.OnItemUnequipped -= OnItemUnequipped;
        }
        
        if (shop != null) {
            shop.OnShopRefreshed -= RefreshShopUI;
        }
    }

    #region Auto-Discovery

    void AutoDiscoverComponents() {
        // Find containers by name
        if (itemSlotContainer == null) {
            itemSlotContainer = FindDeepChild(transform, slotContainerName);
        }
        if (equipmentContainer == null) {
            equipmentContainer = FindDeepChild(transform, equipmentContainerName);
        }
        if (shopItemContainer == null) {
            shopItemContainer = FindDeepChild(transform, shopContainerName);
        }
        
        // Find common UI elements by name patterns
        goldText = FindComponentByName<TextMeshProUGUI>("Gold", "GoldText", "GoldAmount");
        capacityText = FindComponentByName<TextMeshProUGUI>("Capacity", "Slots", "InventoryCount");
        closeButton = FindComponentByName<Button>("Close", "CloseButton", "X", "Exit");
        sellButton = FindComponentByName<Button>("Sell", "SellButton", "SellItem");
        
        // Find equipment slots
        if (equipmentContainer != null) {
            var weaponSlot = FindDeepChild(equipmentContainer, "Weapon", "WeaponSlot");
            var armorSlot = FindDeepChild(equipmentContainer, "Armor", "ArmorSlot");
            
            if (weaponSlot != null) equipmentSlots[0] = weaponSlot.GetComponent<Image>();
            if (armorSlot != null) equipmentSlots[1] = armorSlot.GetComponent<Image>();
        }
        
        Debug.Log($"[AutoBindingInventoryUI] Discovered: " +
            $"SlotsContainer={itemSlotContainer != null}, " +
            $"GoldText={goldText != null}, " +
            $"CloseBtn={closeButton != null}");
    }

    T FindComponentByName<T>(params string[] names) where T : Component {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (var comp in components) {
            string compName = comp.gameObject.name.ToLower();
            foreach (var searchName in names) {
                if (compName.Contains(searchName.ToLower())) {
                    return comp;
                }
            }
        }
        return null;
    }

    Transform FindDeepChild(Transform parent, params string[] names) {
        if (parent == null) return null;
        
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) {
            string childName = child.name.ToLower();
            foreach (var searchName in names) {
                if (childName.Contains(searchName.ToLower())) {
                    return child;
                }
            }
        }
        return null;
    }

    #endregion

    #region Initialization

    void InitializeSlots() {
        // Create inventory slots
        if (itemSlotContainer != null && itemSlotPrefab != null && inventory != null) {
            // Clear existing
            foreach (Transform child in itemSlotContainer) {
                Destroy(child.gameObject);
            }
            itemSlots.Clear();
            
            // Create new slots
            for (int i = 0; i < inventory.maxInventorySlots; i++) {
                GameObject slotObj = Instantiate(itemSlotPrefab, itemSlotContainer);
                var slotUI = slotObj.GetComponent<ItemSlotUI>();
                if (slotUI == null) {
                    slotUI = slotObj.AddComponent<ItemSlotUI>();
                    slotUI.AutoBindComponents();
                }
                slotUI.SetSlotIndex(i);
                slotUI.OnSelected += OnSlotSelected;
                itemSlots.Add(slotUI);
            }
        }
        
        // Create shop slots
        if (shopItemContainer != null && shopItemPrefab != null && shop != null) {
            foreach (Transform child in shopItemContainer) {
                Destroy(child.gameObject);
            }
            shopItems.Clear();
            
            for (int i = 0; i < shop.shopSlots; i++) {
                GameObject itemObj = Instantiate(shopItemPrefab, shopItemContainer);
                var shopUI = itemObj.GetComponent<ShopItemUI>();
                if (shopUI == null) {
                    shopUI = itemObj.AddComponent<ShopItemUI>();
                    shopUI.AutoBindComponents();
                }
                shopUI.SetSlotIndex(i);
                shopUI.OnSelected += OnShopItemSelected;
                shopItems.Add(shopUI);
            }
        }
    }

    void BindButtons() {
        if (closeButton != null) {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
        
        if (sellButton != null) {
            sellButton.onClick.AddListener(SellSelectedItem);
        }
    }

    #endregion

    #region UI Refresh

    void RefreshInventoryUI() {
        if (inventory == null) return;
        
        // Update gold
        if (goldText != null) {
            goldText.text = $"{inventory.Gold:N0} G";
        }
        
        // Update capacity
        if (capacityText != null) {
            capacityText.text = $"{inventory.GetItemCount()}/{inventory.maxInventorySlots}";
        }
        
        // Update item slots
        for (int i = 0; i < itemSlots.Count; i++) {
            if (i < inventory.GetItemCount()) {
                itemSlots[i].SetItem(inventory.GetItem(i));
            } else {
                itemSlots[i].SetItem(null);
            }
        }
        
        // Update equipment display
        RefreshEquipmentDisplay();
    }

    void RefreshEquipmentDisplay() {
        if (inventory == null) return;
        
        if (equipmentSlots[0] != null) {
            equipmentSlots[0].sprite = inventory.equippedWeapon?.icon;
            equipmentSlots[0].color = inventory.equippedWeapon != null ? Color.white : Color.clear;
        }
        
        if (equipmentSlots[1] != null) {
            equipmentSlots[1].sprite = inventory.equippedArmor?.icon;
            equipmentSlots[1].color = inventory.equippedArmor != null ? Color.white : Color.clear;
        }
    }

    void RefreshShopUI() {
        if (shop == null) return;
        
        for (int i = 0; i < shopItems.Count; i++) {
            if (i < shop.currentStock.Count) {
                shopItems[i].SetItem(shop.currentStock[i]);
            } else {
                shopItems[i].SetItem(null);
            }
        }
    }

    void OnItemEquipped(ItemSO item) {
        RefreshEquipmentDisplay();
    }

    void OnItemUnequipped(ItemSO item) {
        RefreshEquipmentDisplay();
    }

    #endregion

    #region Interaction

    void OnSlotSelected(int index) {
        if (index < 0 || index >= inventory.GetItemCount()) {
            selectedItem = null;
            return;
        }
        
        selectedItem = inventory.GetItem(index);
        
        // Show item details, equip option, etc.
        ShowItemContextMenu(selectedItem, index);
    }

    void OnShopItemSelected(int index) {
        if (shop == null || index >= shop.currentStock.Count) return;
        
        var item = shop.currentStock[index];
        if (inventory.Gold >= item.price) {
            shop.BuyItem(index);
        } else {
            ShowNotification("Not enough gold!");
        }
    }

    void SellSelectedItem() {
        if (selectedItem == null) return;
        
        inventory.RemoveItem(selectedItem);
        inventory.AddGold(selectedItem.sellPrice);
        selectedItem = null;
    }

    void ShowItemContextMenu(ItemSO item, int index) {
        // Simple approach: equip/unequip on click
        if (item.isEquippable) {
            inventory.Equip(item);
        }
    }

    void ShowNotification(string message) {
        UIManager.Instance?.ShowNotification(message);
    }

    #endregion

    void OnEnable() {
        RefreshInventoryUI();
        RefreshShopUI();
    }
}

/// <summary>
/// Single inventory slot UI with auto-binding.
/// </summary>
public class ItemSlotUI : MonoBehaviour {
    private Image iconImage;
    private Image rarityBorder;
    private TextMeshProUGUI countText;
    private Button button;
    private ItemSO currentItem;
    private int slotIndex;
    
    public System.Action<int> OnSelected;

    void Awake() {
        AutoBindComponents();
    }

    public void AutoBindComponents() {
        iconImage = FindComponentByName<Image>("Icon", "ItemIcon");
        rarityBorder = FindComponentByName<Image>("Border", "Rarity", "Frame");
        countText = FindComponentByName<TextMeshProUGUI>("Count", "Amount", "Stack");
        button = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        
        if (button != null) {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    void OnButtonClick() {
        OnSelected?.Invoke(slotIndex);
    }
    
    void OnDestroy() {
        if (button != null) {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    T FindComponentByName<T>(params string[] names) where T : Component {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (var comp in components) {
            string compName = comp.gameObject.name.ToLower();
            foreach (var searchName in names) {
                if (compName.Contains(searchName.ToLower())) {
                    return comp;
                }
            }
        }
        return GetComponentInChildren<T>();
    }

    public void SetSlotIndex(int index) {
        slotIndex = index;
    }

    public void SetItem(ItemSO item) {
        currentItem = item;
        
        if (item != null) {
            if (iconImage != null) {
                iconImage.sprite = item.icon;
                iconImage.color = Color.white;
            }
            
            if (rarityBorder != null) {
                rarityBorder.color = item.GetRarityColor();
            }
            
            if (countText != null) {
                countText.text = item.isStackable ? item.stackCount.ToString() : "";
            }
        } else {
            if (iconImage != null) iconImage.color = Color.clear;
            if (rarityBorder != null) rarityBorder.color = Color.clear;
            if (countText != null) countText.text = "";
        }
    }
}

/// <summary>
/// Shop item UI with auto-binding.
/// </summary>
public class ShopItemUI : MonoBehaviour {
    private Image iconImage;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI priceText;
    private Button button;
    private ItemSO currentItem;
    private int slotIndex;
    
    public System.Action<int> OnSelected;

    void Awake() {
        AutoBindComponents();
    }

    public void AutoBindComponents() {
        iconImage = FindComponentByName<Image>("Icon", "ItemIcon");
        nameText = FindComponentByName<TextMeshProUGUI>("Name", "ItemName");
        priceText = FindComponentByName<TextMeshProUGUI>("Price", "Cost", "Gold");
        button = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        
        if (button != null) {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    void OnButtonClick() {
        OnSelected?.Invoke(slotIndex);
    }
    
    void OnDestroy() {
        if (button != null) {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    T FindComponentByName<T>(params string[] names) where T : Component {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (var comp in components) {
            string compName = comp.gameObject.name.ToLower();
            foreach (var searchName in names) {
                if (compName.Contains(searchName.ToLower())) {
                    return comp;
                }
            }
        }
        return GetComponentInChildren<T>();
    }

    public void SetSlotIndex(int index) {
        slotIndex = index;
    }

    public void SetItem(ItemSO item) {
        currentItem = item;
        gameObject.SetActive(item != null);
        
        if (item != null) {
            if (iconImage != null) iconImage.sprite = item.icon;
            if (nameText != null) nameText.text = item.itemName;
            if (priceText != null) priceText.text = $"{item.price}G";
        }
    }
}

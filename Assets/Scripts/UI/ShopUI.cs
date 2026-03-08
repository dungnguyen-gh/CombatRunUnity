using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shop interface with item browsing, preview, and stat comparison functionality.
/// Works with ShopManager for shop data and InventoryManager for player inventory.
/// 
/// <para><b>Setup Requirements:</b></para>
/// <list type="bullet">
///   <item><description>Attach to a panel GameObject with UIPanel component</description></item>
///   <item><description>Assign shopGrid (Transform) for shop item slots</description></item>
///   <item><description>Assign shopSlotPrefab (GameObject with Icon, Name, Price children)</description></item>
///   <item><description>Assign previewPanel and all its child UI elements</description></item>
///   <item><description>Assign stats comparison text elements</description></item>
/// </list>
/// 
/// <para><b>Events Subscribed:</b></para>
/// <list type="bullet">
///   <item><description>ShopManager.OnShopRefreshed - Refreshes shop items</description></item>
///   <item><description>ShopManager.OnItemPurchased - Shows purchase feedback</description></item>
///   <item><description>InventoryManager.OnGoldChanged - Updates gold display</description></item>
/// </list>
/// </summary>
[RequireComponent(typeof(UIPanel))]
public class ShopUI : MonoBehaviour {
    
    #region Inspector Fields - Shop Grid
    
    [Header("Shop Grid")]
    [Tooltip("Parent transform for instantiated shop slot objects")]
    public Transform shopGrid;
    
    [Tooltip("Prefab for shop slots (should have Icon, Name, Price children)")]
    public GameObject shopSlotPrefab;
    
    #endregion

    #region Inspector Fields - Preview Panel
    
    [Header("Preview Panel")]
    [Tooltip("Panel that shows item preview and buy option")]
    public GameObject previewPanel;
    
    [Tooltip("Image for item icon in preview panel")]
    public Image previewIcon;
    
    [Tooltip("Text for item name in preview panel")]
    public TextMeshProUGUI previewName;
    
    [Tooltip("Text for item description")]
    public TextMeshProUGUI previewDescription;
    
    [Tooltip("Text showing item price")]
    public TextMeshProUGUI previewPrice;
    
    [Tooltip("Button to buy the selected item")]
    public Button buyButton;
    
    #endregion

    #region Inspector Fields - Stats Comparison
    
    [Header("Stats Comparison")]
    [Tooltip("Text showing current damage stat")]
    public TextMeshProUGUI currentDamageText;
    
    [Tooltip("Text showing damage after equipping previewed item")]
    public TextMeshProUGUI previewDamageText;
    
    [Tooltip("Text showing current defense stat")]
    public TextMeshProUGUI currentDefenseText;
    
    [Tooltip("Text showing defense after equipping previewed item")]
    public TextMeshProUGUI previewDefenseText;
    
    [Tooltip("Text showing current crit chance")]
    public TextMeshProUGUI currentCritText;
    
    [Tooltip("Text showing crit after equipping previewed item")]
    public TextMeshProUGUI previewCritText;
    
    #endregion

    #region Inspector Fields - Player Gold
    
    [Header("Player Gold")]
    [Tooltip("Text showing player's current gold")]
    public TextMeshProUGUI goldText;
    
    #endregion

    #region Inspector Fields - Inventory Toggle
    
    [Header("Inventory Toggle")]
    [Tooltip("Button to open inventory from shop")]
    public Button openInventoryButton;
    
    [Tooltip("Button to refresh shop stock")]
    public Button refreshButton;
    
    [Tooltip("Button to close the shop")]
    public Button closeButton;
    
    #endregion

    #region Private Fields
    
    private ShopManager shop;
    private InventoryManager inventory;
    private PlayerController player;
    private ItemSO selectedItem;
    private int selectedSlotIndex = -1;
    private List<GameObject> slotObjects = new List<GameObject>();
    private UIPanel uiPanel;
    
    #endregion

    #region Properties
    
    /// <summary>
    /// Gets the currently selected shop item (null if none selected).
    /// </summary>
    public ItemSO SelectedItem => selectedItem;
    
    /// <summary>
    /// Returns true if a shop item is currently selected.
    /// </summary>
    public bool HasSelection => selectedItem != null;
    
    /// <summary>
    /// Gets the index of the selected slot (-1 if none selected).
    /// </summary>
    public int SelectedSlotIndex => selectedSlotIndex;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake() {
        uiPanel = GetComponent<UIPanel>();
        if (uiPanel == null) {
            Debug.LogError("[ShopUI] UIPanel component is required!");
        }
    }

    void Start() {
        // Get manager references
        shop = ShopManager.Instance;
        inventory = InventoryManager.Instance;
        player = FindFirstObjectByType<PlayerController>();
        
        // Subscribe to shop events
        if (shop != null) {
            shop.OnShopRefreshed += RefreshShopItems;
            shop.OnItemPurchased += OnItemPurchased;
        } else {
            Debug.LogWarning("[ShopUI] ShopManager instance not found!");
        }
        
        // Subscribe to inventory events
        // Subscribe to inventory events
        if (inventory != null) {
            inventory.OnGoldChanged += UpdateGoldDisplay;
        } else {
            Debug.LogWarning("[ShopUI] InventoryManager instance not found!");
        }
        
        // Find player for stat comparison
        if (player == null) {
            player = FindFirstObjectByType<PlayerController>();
        }

        // Hide preview panel initially
        if (previewPanel != null) {
            previewPanel.SetActive(false);
        }

        // Setup button listeners
        SetupButtonListeners();
    }
    
    void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks
        if (shop != null) {
            shop.OnShopRefreshed -= RefreshShopItems;
            shop.OnItemPurchased -= OnItemPurchased;
        }
        
        if (inventory != null) {
            inventory.OnGoldChanged -= UpdateGoldDisplay;
        }
        
        // Clean up slot objects
        ClearSlots();
    }

    void OnEnable() {
        RefreshShopItems();
        UpdateGoldDisplay(inventory?.Gold ?? 0);
    }
    
    void OnDisable() {
        // End preview when panel closes
        OnEndPreview();
    }
    
    #endregion

    #region Button Setup

    /// <summary>
    /// Sets up all button click listeners.
    /// </summary>
    void SetupButtonListeners() {
        if (openInventoryButton != null) {
            openInventoryButton.onClick.AddListener(() => {
                UIManager.Instance?.PlayButtonClickSound();
                UIManager.Instance?.ToggleInventory();
            });
        }
        
        if (refreshButton != null) {
            refreshButton.onClick.AddListener(() => {
                UIManager.Instance?.PlayButtonClickSound();
                OnRefreshShop();
            });
        }
        
        if (closeButton != null) {
            closeButton.onClick.AddListener(() => {
                UIManager.Instance?.PlayButtonClickSound();
                OnCloseShop();
            });
        }
    }
    
    #endregion

    #region Gold Display
    
    void UpdateGoldDisplay(int gold) {
        if (goldText != null) {
            goldText.text = $"Gold: {gold:N0}";
        }
        
        // Update buy button interactability if item selected
        if (selectedItem != null && buyButton != null && shop != null) {
            int price = shop.GetBuyPrice(selectedItem);
            buyButton.interactable = (inventory?.Gold ?? 0) >= price;
        }
    }
    
    #endregion

    #region Shop Item Display

    /// <summary>
    /// Refreshes the shop item display.
    /// Called when shop stock changes or panel opens.
    /// </summary>
    public void RefreshShopItems() {
        if (shop == null) return;

        ClearSlots();
        
        for (int i = 0; i < shop.currentStock.Count; i++) {
            var item = shop.currentStock[i];
            if (item != null) {
                int price = shop.GetBuyPrice(item);
                CreateShopSlot(item, price, i);
            }
        }
    }

    /// <summary>
    /// Clears all shop slot objects.
    /// </summary>
    void ClearSlots() {
        foreach (var slot in slotObjects) {
            if (slot != null) {
                // Remove button listeners before destroying
                var button = slot.GetComponent<Button>();
                if (button != null) {
                    button.onClick.RemoveAllListeners();
                }
                Destroy(slot);
            }
        }
        slotObjects.Clear();
    }

    /// <summary>
    /// Creates a shop slot for the given item.
    /// </summary>
    /// <param name="item">The item to display</param>
    /// <param name="price">The item's price</param>
    /// <param name="index">The slot index</param>
    void CreateShopSlot(ItemSO item, int price, int index) {
        if (shopGrid == null) {
            Debug.LogWarning("[ShopUI] Shop grid reference is missing!");
            return;
        }
        
        if (shopSlotPrefab == null) {
            Debug.LogWarning("[ShopUI] Shop slot prefab reference is missing!");
            return;
        }

        GameObject slot = Instantiate(shopSlotPrefab, shopGrid);
        slotObjects.Add(slot);

        // Set icon
        var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && item.icon != null) {
            icon.sprite = item.icon;
            icon.color = item.rarityColor;
        }

        // Set name
        var nameText = slot.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null) {
            nameText.text = item.itemName;
            nameText.color = item.rarityColor;
        }

        // Set price
        var priceText = slot.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        if (priceText != null) {
            priceText.text = $"{price} G";
        }

        // Set click handler
        var button = slot.GetComponent<Button>();
        if (button != null) {
            int capturedIndex = index; // Capture for closure
            button.onClick.AddListener(() => SelectItem(capturedIndex));
        }

        // Hover preview (basic implementation)
        var hover = slot.GetComponent<ShopSlotHover>();
        if (hover == null) {
            hover = slot.AddComponent<ShopSlotHover>();
        }
        hover.Initialize(item, this);
    }
    
    #endregion

    #region Item Selection

    /// <summary>
    /// Selects an item from the shop grid.
    /// </summary>
    /// <param name="index">The index in currentStock</param>
    void SelectItem(int index) {
        if (shop == null || index < 0 || index >= shop.currentStock.Count) return;

        selectedSlotIndex = index;
        selectedItem = shop.currentStock[index];
        if (selectedItem == null) return;

        // End any previous preview
        inventory?.EndPreview();

        // Show preview
        inventory?.PreviewEquip(selectedItem);
        UpdatePreviewPanel();
        
        // Play selection sound
        UIManager.Instance?.PlayButtonClickSound();
    }

    /// <summary>
    /// Updates the preview panel with selected item information.
    /// </summary>
    void UpdatePreviewPanel() {
        if (previewPanel == null || selectedItem == null) {
            Debug.LogWarning("[ShopUI] Preview panel or selected item is null!");
            return;
        }
        
        if (inventory == null || player == null) {
            Debug.LogWarning("[ShopUI] Inventory or Player reference is null!");
            return;
        }

        previewPanel.SetActive(true);

        // Item info
        if (previewIcon != null) {
            previewIcon.sprite = selectedItem.icon;
            previewIcon.color = selectedItem.rarityColor;
        }
        
        if (previewName != null) {
            previewName.text = selectedItem.itemName;
            previewName.color = selectedItem.rarityColor;
        }
        
        if (previewDescription != null) {
            previewDescription.text = selectedItem.description;
        }
        
        // Price
        if (previewPrice != null) {
            int price = shop?.GetBuyPrice(selectedItem) ?? 0;
            previewPrice.text = $"Price: {price} Gold";
        }

        // Stats comparison
        UpdateStatComparison();

        // Buy button
        if (buyButton != null) {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => {
                UIManager.Instance?.PlayButtonClickSound();
                BuySelectedItem();
            });
            
            // Check affordability
            int price = shop?.GetBuyPrice(selectedItem) ?? 0;
            bool canAfford = inventory != null && inventory.Gold >= price;
            buyButton.interactable = canAfford;
        }
    }

    /// <summary>
    /// Updates the stat comparison display (current vs preview).
    /// </summary>
    void UpdateStatComparison() {
        if (inventory == null || player == null || selectedItem == null) return;

        // Get base stats (before preview)
        int currentDamage = player.stats.Damage - selectedItem.damageBonus;
        int currentDefense = player.stats.Defense - selectedItem.defenseBonus;
        float currentCrit = player.stats.Crit - selectedItem.critBonus;

        // Get preview stats (currently applied by PreviewEquip)
        int previewDamage = player.stats.Damage;
        int previewDefense = player.stats.Defense;
        float previewCrit = player.stats.Crit;

        // Update UI
        UpdateStatText(currentDamageText, currentDamage, previewDamage);
        UpdateStatText(previewDamageText, previewDamage, currentDamage);
        UpdateStatText(currentDefenseText, currentDefense, previewDefense);
        UpdateStatText(previewDefenseText, previewDefense, currentDefense);
        UpdateStatText(currentCritText, currentCrit, previewCrit, true);
        UpdateStatText(previewCritText, previewCrit, currentCrit, true);
    }

    /// <summary>
    /// Updates a stat text with color-coded comparison.
    /// </summary>
    void UpdateStatText(TextMeshProUGUI text, float value, float compareValue, bool isPercent = false) {
        if (text == null) return;

        string format = isPercent ? "P0" : "0";
        text.text = value.ToString(format);

        // Color based on comparison
        if (value > compareValue) {
            text.color = Color.green;
        } else if (value < compareValue) {
            text.color = Color.red;
        } else {
            text.color = Color.white;
        }
    }
    
    #endregion

    #region Actions

    /// <summary>
    /// Buys the currently selected item.
    /// Called when the buy button is clicked.
    /// </summary>
    void BuySelectedItem() {
        if (shop == null || selectedSlotIndex < 0) {
            Debug.LogWarning("[ShopUI] Cannot buy - shop not available or no item selected!");
            return;
        }

        if (shop.BuyItem(selectedSlotIndex)) {
            previewPanel?.SetActive(false);
            selectedItem = null;
            selectedSlotIndex = -1;
            RefreshShopItems();
            UIManager.Instance?.ShowNotification("Item purchased!");
        }
    }

    void OnItemPurchased(ItemSO item) {
        // Could show purchase effect
        // UIManager.Instance?.ShowNotification($"Purchased {item.itemName}");
    }

    /// <summary>
    /// Ends the equipment preview and restores actual equipment.
    /// </summary>
    public void OnEndPreview() {
        inventory?.EndPreview();
        if (previewPanel != null) {
            previewPanel.SetActive(false);
        }
        selectedItem = null;
        selectedSlotIndex = -1;
    }

    /// <summary>
    /// Refreshes the shop stock.
    /// Called when the refresh button is clicked.
    /// </summary>
    public void OnRefreshShop() {
        shop?.ForceRefresh();
        OnEndPreview();
    }

    /// <summary>
    /// Closes the shop panel.
    /// Called when the close button is clicked.
    /// </summary>
    public void OnCloseShop() {
        OnEndPreview(); // End preview before closing
        UIManager.Instance?.ResumeGame();
    }
    
    #endregion
}

/// <summary>
/// Helper component for shop slot hover functionality.
/// Can be extended to implement hover preview.
/// </summary>
public class ShopSlotHover : MonoBehaviour {
    private ItemSO item;
    private ShopUI shopUI;

    /// <summary>
    /// Initializes the hover component with item and UI references.
    /// </summary>
    /// <param name="item">The item this slot represents</param>
    /// <param name="ui">Reference to the ShopUI</param>
    public void Initialize(ItemSO item, ShopUI ui) {
        this.item = item;
        this.shopUI = ui;
    }
    
    /// <summary>
    /// Gets the item associated with this slot.
    /// </summary>
    public ItemSO GetItem() => item;
}

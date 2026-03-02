using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shop interface with item browsing, preview, and stat comparison functionality.
/// </summary>
public class ShopUI : MonoBehaviour {
    [Header("Shop Grid")]
    public Transform shopGrid;
    public GameObject shopSlotPrefab;

    [Header("Preview Panel")]
    public GameObject previewPanel;
    public Image previewIcon;
    public TextMeshProUGUI previewName;
    public TextMeshProUGUI previewDescription;
    public TextMeshProUGUI previewPrice;
    public Button buyButton;

    [Header("Stats Comparison")]
    public TextMeshProUGUI currentDamageText;
    public TextMeshProUGUI previewDamageText;
    public TextMeshProUGUI currentDefenseText;
    public TextMeshProUGUI previewDefenseText;
    public TextMeshProUGUI currentCritText;
    public TextMeshProUGUI previewCritText;

    [Header("Player Gold")]
    public TextMeshProUGUI goldText;

    [Header("Inventory Toggle")]
    public Button openInventoryButton;

    private ShopManager shop;
    private ItemSO selectedItem;
    private int selectedSlotIndex = -1;
    private List<GameObject> slotObjects = new List<GameObject>();

    void Start() {
        shop = ShopManager.Instance;
        if (shop != null) {
            shop.OnShopRefreshed += RefreshShopItems;
            shop.OnItemPurchased += OnItemPurchased;
        }

        previewPanel?.SetActive(false);

        if (openInventoryButton != null) {
            openInventoryButton.onClick.AddListener(() => {
                UIManager.Instance?.ToggleInventory();
            });
        }
    }
    
    void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks
        if (shop != null) {
            shop.OnShopRefreshed -= RefreshShopItems;
            shop.OnItemPurchased -= OnItemPurchased;
        }
    }

    void OnEnable() {
        RefreshShopItems();
    }

    void Update() {
        // Update gold display with safe navigation
        int goldAmount = shop?.player?.gold ?? 0;
        if (goldText != null) {
            goldText.text = $"Gold: {goldAmount}";
        }
    }

    void RefreshShopItems() {
        if (shop == null) return;

        ClearSlots();
        
        for (int i = 0; i < shop.currentStock.Count; i++) {
            CreateShopSlot(shop.currentStock[i], shop.currentPrices[i], i);
        }
    }

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

    void CreateShopSlot(ItemSO item, int price, int index) {
        if (shopGrid == null || shopSlotPrefab == null) return;

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
        var hover = slot.AddComponent<ShopSlotHover>();
        hover.Initialize(item, this);
    }

    void SelectItem(int index) {
        if (shop == null || index < 0 || index >= shop.currentStock.Count) return;

        selectedSlotIndex = index;
        selectedItem = shop.currentStock[index];

        // End any previous preview
        shop.EndPreview();

        // Show preview
        shop.PreviewItem(selectedItem);
        UpdatePreviewPanel();
    }

    void UpdatePreviewPanel() {
        if (previewPanel == null || selectedItem == null || shop == null) return;

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
        if (previewDescription != null) previewDescription.text = selectedItem.description;
        
        // Price with bounds check
        if (previewPrice != null) {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < shop.currentPrices.Count) {
                previewPrice.text = $"Price: {shop.currentPrices[selectedSlotIndex]} Gold";
            } else {
                previewPrice.text = "Price: --";
            }
        }

        // Stats comparison
        UpdateStatComparison();

        // Buy button
        if (buyButton != null) {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(BuySelectedItem);
            
            // Check affordability with bounds check
            bool canAfford = false;
            if (selectedSlotIndex >= 0 && selectedSlotIndex < shop.currentPrices.Count && shop.player != null) {
                canAfford = shop.player.gold >= shop.currentPrices[selectedSlotIndex];
            }
            buyButton.interactable = canAfford;
        }
    }

    void UpdateStatComparison() {
        if (shop == null || shop.player == null || selectedItem == null) return;

        var baseStats = shop.player.stats;
        
        // Store current stats before preview
        int currentDamage = baseStats.Damage;
        int currentDefense = baseStats.Defense;
        float currentCrit = baseStats.Crit;

        // Get preview stats (already applied by PreviewItem)
        int previewDamage = shop.GetPreviewDamage();
        int previewDefense = shop.GetPreviewDefense();
        float previewCrit = shop.GetPreviewCrit();

        // Update UI
        UpdateStatText(currentDamageText, currentDamage, previewDamage);
        UpdateStatText(previewDamageText, previewDamage, currentDamage);
        UpdateStatText(currentDefenseText, currentDefense, previewDefense);
        UpdateStatText(previewDefenseText, previewDefense, currentDefense);
        UpdateStatText(currentCritText, currentCrit, previewCrit, true);
        UpdateStatText(previewCritText, previewCrit, currentCrit, true);
    }

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

    void BuySelectedItem() {
        if (shop == null || selectedSlotIndex < 0) return;

        if (shop.PurchaseItem(selectedSlotIndex)) {
            previewPanel.SetActive(false);
            selectedItem = null;
            selectedSlotIndex = -1;
            RefreshShopItems();
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("Item purchased!");
            }
        }
    }

    void OnItemPurchased(ItemSO item) {
        // Could show purchase effect
    }

    public void OnEndPreview() {
        if (shop != null) {
            shop.EndPreview();
        }
        previewPanel.SetActive(false);
        selectedItem = null;
        selectedSlotIndex = -1;
    }

    public void OnRefreshShop() {
        shop?.RefreshStock();
    }

    public void OnCloseShop() {
        UIManager.Instance?.ResumeGame();
    }
}

/// <summary>
/// Helper component for shop slot hover functionality.
/// Can be extended to implement hover preview.
/// </summary>
public class ShopSlotHover : MonoBehaviour {
    private ItemSO item;
    private ShopUI shopUI;

    public void Initialize(ItemSO item, ShopUI ui) {
        this.item = item;
        this.shopUI = ui;
    }
}

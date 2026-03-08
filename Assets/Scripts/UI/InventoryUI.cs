using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays inventory grid, equipment slots, and item details with equip/sell functionality.
/// Works with InventoryManager for data and UIManager for panel control.
/// 
/// <para><b>Setup Requirements:</b></para>
/// <list type="bullet">
///   <item><description>Attach to a panel GameObject with UIPanel component</description></item>
///   <item><description>Assign inventoryGrid (Transform) for item slots</description></item>
///   <item><description>Assign itemSlotPrefab (GameObject with Image and Button)</description></item>
///   <item><description>Assign equipment slot icons and stat texts</description></item>
///   <item><description>Assign itemDetailPanel and all its child UI elements</description></item>
/// </list>
/// 
/// <para><b>Events Subscribed:</b></para>
/// <list type="bullet">
///   <item><description>InventoryManager.OnInventoryChanged - Refreshes the UI</description></item>
///   <item><description>InventoryManager.OnItemEquipped - Updates equipment display</description></item>
///   <item><description>InventoryManager.OnItemUnequipped - Updates equipment display</description></item>
/// </list>
/// </summary>
[RequireComponent(typeof(UIPanel))]
public class InventoryUI : MonoBehaviour {
    
    #region Inspector Fields - Inventory Grid
    
    [Header("Inventory Grid")]
    [Tooltip("Parent transform for instantiated item slot objects")]
    public Transform inventoryGrid;
    
    [Tooltip("Prefab for inventory slots (should have Image for icon and Button)")]
    public GameObject itemSlotPrefab;
    
    #endregion

    #region Inspector Fields - Equipment Slots
    
    [Header("Equipment Slots")]
    [Tooltip("Image component for equipped weapon display")]
    public Image weaponSlotIcon;
    
    [Tooltip("Image component for equipped armor display")]
    public Image armorSlotIcon;
    
    [Tooltip("Text showing weapon stats")]
    public TextMeshProUGUI weaponStatText;
    
    [Tooltip("Text showing armor stats")]
    public TextMeshProUGUI armorStatText;
    
    #endregion

    #region Inspector Fields - Item Details
    
    [Header("Item Details")]
    [Tooltip("Panel that shows detailed item information")]
    public GameObject itemDetailPanel;
    
    [Tooltip("Image for item icon in detail panel")]
    public Image detailIcon;
    
    [Tooltip("Text for item name in detail panel")]
    public TextMeshProUGUI detailName;
    
    [Tooltip("Text for item description")]
    public TextMeshProUGUI detailDescription;
    
    [Tooltip("Text for item stats")]
    public TextMeshProUGUI detailStats;
    
    [Tooltip("Button to equip selected item")]
    public Button equipButton;
    
    [Tooltip("Button to unequip selected item")]
    public Button unequipButton;
    
    [Tooltip("Button to sell selected item")]
    public Button sellButton;
    
    #endregion

    #region Inspector Fields - Stats Display
    
    [Header("Stats Display")]
    [Tooltip("Text showing current damage stat")]
    public TextMeshProUGUI damageText;
    
    [Tooltip("Text showing current defense stat")]
    public TextMeshProUGUI defenseText;
    
    [Tooltip("Text showing current crit chance")]
    public TextMeshProUGUI critText;
    
    [Tooltip("Text showing current attack speed")]
    public TextMeshProUGUI attackSpeedText;
    
    #endregion

    #region Inspector Fields - Player Preview
    
    [Header("Player Preview")]
    [Tooltip("Image showing player character preview")]
    public Image playerPreviewImage;
    
    #endregion

    #region Private Fields
    
    private InventoryManager inventory;
    private ItemSO selectedItem;
    private List<GameObject> slotObjects = new List<GameObject>();
    private UIPanel uiPanel;

    // Cached component references for instantiated slot objects
    private class CachedSlotComponents {
        public Image icon;
        public Button button;
    }
    private List<CachedSlotComponents> cachedSlotComponents = new List<CachedSlotComponents>();
    
    #endregion

    #region Properties
    
    /// <summary>
    /// Gets the currently selected item (null if none selected).
    /// </summary>
    public ItemSO SelectedItem => selectedItem;
    
    /// <summary>
    /// Returns true if an item is currently selected.
    /// </summary>
    public bool HasSelection => selectedItem != null;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake() {
        uiPanel = GetComponent<UIPanel>();
        if (uiPanel == null) {
            Debug.LogError("[InventoryUI] UIPanel component is required!");
        }
    }

    void Start() {
        inventory = InventoryManager.Instance;
        if (inventory == null) {
            Debug.LogError("[InventoryUI] InventoryManager.Instance is null! Ensure InventoryManager exists in scene.");
            return;
        }
        
        // Subscribe to inventory events
        inventory.OnInventoryChanged += RefreshUI;
        inventory.OnItemEquipped += OnItemEquipped;
        inventory.OnItemUnequipped += OnItemUnequipped;

        // Hide detail panel initially
        if (itemDetailPanel != null) {
            itemDetailPanel.SetActive(false);
        }
        
        // Initial refresh
        RefreshUI();
    }

    void OnEnable() {
        RefreshUI();
        
        // Clear selection when panel opens
        ClearSelection();
    }
    
    void OnDisable() {
        // End any active preview when panel closes
        if (inventory != null) {
            inventory.EndPreview();
        }
        ClearSelection();
    }
    
    void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks
        if (inventory != null) {
            inventory.OnInventoryChanged -= RefreshUI;
            inventory.OnItemEquipped -= OnItemEquipped;
            inventory.OnItemUnequipped -= OnItemUnequipped;
        }
        
        // Clean up slot objects
        ClearSlots();
    }
    
    #endregion

    #region UI Refresh Methods

    /// <summary>
    /// Refreshes the entire inventory UI.
    /// Called automatically when inventory changes.
    /// </summary>
    public void RefreshUI() {
        if (inventory == null) return;
        
        ClearSlots();
        PopulateInventory();
        UpdateEquipmentSlots();
        UpdateStatsDisplay();
    }

    /// <summary>
    /// Clears all inventory slot objects and cached components.
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
        cachedSlotComponents.Clear();
    }

    /// <summary>
    /// Populates the inventory grid with item slots.
    /// </summary>
    void PopulateInventory() {
        if (inventoryGrid == null) {
            Debug.LogWarning("[InventoryUI] Inventory grid reference is missing!");
            return;
        }
        
        if (itemSlotPrefab == null) {
            Debug.LogWarning("[InventoryUI] Item slot prefab reference is missing!");
            return;
        }
        
        if (inventory == null) return;

        foreach (var item in inventory.Items) {
            if (item == null) continue;
            
            GameObject slot = Instantiate(itemSlotPrefab, inventoryGrid);
            slotObjects.Add(slot);

            // Cache components to avoid repeated GetComponent calls
            CachedSlotComponents cached = new CachedSlotComponents();
            cached.icon = slot.GetComponentInChildren<Image>();
            cached.button = slot.GetComponent<Button>();
            cachedSlotComponents.Add(cached);

            // Set icon using cached reference
            if (cached.icon != null && item.icon != null) {
                cached.icon.sprite = item.icon;
                cached.icon.color = item.rarityColor;
            }

            // Set click handler using cached reference
            if (cached.button != null) {
                var capturedItem = item; // Capture for closure
                cached.button.onClick.AddListener(() => SelectItem(capturedItem));
            }
        }
    }

    /// <summary>
    /// Updates the equipment slot displays.
    /// </summary>
    void UpdateEquipmentSlots() {
        if (inventory == null) return;
        
        // Weapon slot
        if (weaponSlotIcon != null) {
            if (inventory.equippedWeapon != null) {
                weaponSlotIcon.sprite = inventory.equippedWeapon.icon;
                weaponSlotIcon.color = inventory.equippedWeapon.rarityColor;
                if (weaponStatText != null) {
                    weaponStatText.text = $"+{inventory.equippedWeapon.damageBonus} DMG";
                }
            } else {
                weaponSlotIcon.sprite = null;
                weaponSlotIcon.color = Color.gray;
                if (weaponStatText != null) {
                    weaponStatText.text = "No Weapon";
                }
            }
        }

        // Armor slot
        if (armorSlotIcon != null) {
            if (inventory.equippedArmor != null) {
                armorSlotIcon.sprite = inventory.equippedArmor.icon;
                armorSlotIcon.color = inventory.equippedArmor.rarityColor;
                if (armorStatText != null) {
                    armorStatText.text = $"+{inventory.equippedArmor.defenseBonus} DEF";
                }
            } else {
                armorSlotIcon.sprite = null;
                armorSlotIcon.color = Color.gray;
                if (armorStatText != null) {
                    armorStatText.text = "No Armor";
                }
            }
        }
    }

    /// <summary>
    /// Updates the player stats display.
    /// </summary>
    void UpdateStatsDisplay() {
        if (inventory == null || inventory.player == null) return;

        var stats = inventory.player.stats;
        if (damageText != null) damageText.text = $"Damage: {stats.Damage}";
        if (defenseText != null) defenseText.text = $"Defense: {stats.Defense}";
        if (critText != null) critText.text = $"Crit: {stats.Crit:P0}";
        if (attackSpeedText != null) attackSpeedText.text = $"Attack Speed: {stats.AttackSpeed:F1}";
    }
    
    #endregion

    #region Item Selection

    /// <summary>
    /// Selects an item from the inventory grid.
    /// </summary>
    /// <param name="item">The item to select</param>
    void SelectItem(ItemSO item) {
        if (item == null) return;
        
        selectedItem = item;
        if (itemDetailPanel == null) {
            Debug.LogWarning("[InventoryUI] Item detail panel reference is missing!");
            return;
        }

        itemDetailPanel.SetActive(true);

        // Update icon
        if (detailIcon != null) {
            detailIcon.sprite = item.icon;
            detailIcon.color = item.rarityColor;
        }
        
        // Update name
        if (detailName != null) {
            detailName.text = item.itemName;
            detailName.color = item.rarityColor;
        }
        
        // Update description
        if (detailDescription != null) {
            detailDescription.text = item.description;
        }

        // Build stats text
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (item.damageBonus > 0) sb.AppendLine($"Damage: +{item.damageBonus}");
        if (item.defenseBonus > 0) sb.AppendLine($"Defense: +{item.defenseBonus}");
        if (item.critBonus > 0) sb.AppendLine($"Crit Chance: +{item.critBonus:P0}");
        if (item.attackSpeedBonus > 0) sb.AppendLine($"Attack Speed: +{item.attackSpeedBonus:F1}");
        if (item.maxHPBonus > 0) sb.AppendLine($"Max HP: +{item.maxHPBonus}");
        sb.AppendLine($"Sell Price: {inventory?.GetSellPrice(item) ?? 0} gold");
        
        if (detailStats != null) {
            detailStats.text = sb.ToString();
        }

        // Configure buttons
        ConfigureEquipButton();
        ConfigureUnequipButton(false);
        ConfigureSellButton();
    }

    /// <summary>
    /// Clears the current item selection and hides the detail panel.
    /// </summary>
    public void ClearSelection() {
        selectedItem = null;
        if (itemDetailPanel != null) {
            itemDetailPanel.SetActive(false);
        }
    }
    
    #endregion

    #region Button Configuration

    void ConfigureEquipButton() {
        if (equipButton == null) return;
        
        equipButton.gameObject.SetActive(true);
        equipButton.onClick.RemoveAllListeners();
        equipButton.onClick.AddListener(() => {
            UIManager.Instance?.PlayButtonClickSound();
            EquipSelectedItem();
        });
    }

    void ConfigureUnequipButton(bool show) {
        if (unequipButton == null) return;
        
        unequipButton.gameObject.SetActive(show);
    }

    void ConfigureSellButton() {
        if (sellButton == null) return;
        
        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(() => {
            UIManager.Instance?.PlayButtonClickSound();
            SellSelectedItem();
        });
    }
    
    #endregion

    #region Actions

    /// <summary>
    /// Equips the currently selected item.
    /// Called when the equip button is clicked.
    /// </summary>
    void EquipSelectedItem() {
        if (selectedItem == null || inventory == null) return;
        
        inventory.Equip(selectedItem);
        ClearSelection();
    }

    /// <summary>
    /// Sells the currently selected item.
    /// Called when the sell button is clicked.
    /// </summary>
    void SellSelectedItem() {
        if (selectedItem == null) return;
        
        ShopManager.Instance?.SellItem(selectedItem);
        ClearSelection();
    }

    void OnItemEquipped(ItemSO item) {
        // Could show equipped notification
        // UIManager.Instance?.ShowNotification($"Equipped {item.itemName}");
    }

    void OnItemUnequipped(ItemSO item) {
        // Could show unequipped notification
        // UIManager.Instance?.ShowNotification($"Unequipped {item.itemName}");
    }
    
    #endregion

    #region Equipment Slot Click Handlers

    /// <summary>
    /// Called when the weapon slot is clicked.
    /// Shows equipped weapon details with unequip option.
    /// </summary>
    public void OnWeaponSlotClicked() {
        if (inventory != null && inventory.equippedWeapon != null) {
            SelectEquippedItem(inventory.equippedWeapon, EquipSlot.Weapon);
        }
    }

    /// <summary>
    /// Called when the armor slot is clicked.
    /// Shows equipped armor details with unequip option.
    /// </summary>
    public void OnArmorSlotClicked() {
        if (inventory != null && inventory.equippedArmor != null) {
            SelectEquippedItem(inventory.equippedArmor, EquipSlot.Armor);
        }
    }

    void SelectEquippedItem(ItemSO item, EquipSlot slot) {
        selectedItem = item;
        if (itemDetailPanel == null) return;

        itemDetailPanel.SetActive(true);

        // Update icon
        if (detailIcon != null) {
            detailIcon.sprite = item.icon;
            detailIcon.color = item.rarityColor;
        }
        
        // Update name
        if (detailName != null) {
            detailName.text = $"{item.itemName} (Equipped)";
            detailName.color = item.rarityColor;
        }
        
        // Update description
        if (detailDescription != null) {
            detailDescription.text = item.description;
        }

        // Configure buttons for equipped item
        if (equipButton != null) {
            equipButton.gameObject.SetActive(false);
        }
        
        if (unequipButton != null) {
            unequipButton.gameObject.SetActive(true);
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(() => {
                UIManager.Instance?.PlayButtonClickSound();
                if (inventory != null) {
                    inventory.UnequipSlot(slot);
                }
                ClearSelection();
            });
        }
        
        if (sellButton != null) {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => SellSelectedItem());
        }
    }
    
    #endregion
}

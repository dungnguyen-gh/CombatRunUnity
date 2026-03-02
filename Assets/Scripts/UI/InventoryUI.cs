using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays inventory grid, equipment slots, and item details with equip/sell functionality.
/// </summary>
public class InventoryUI : MonoBehaviour {
    [Header("Inventory Grid")]
    public Transform inventoryGrid;
    public GameObject itemSlotPrefab;

    [Header("Equipment Slots")]
    public Image weaponSlotIcon;
    public Image armorSlotIcon;
    public TextMeshProUGUI weaponStatText;
    public TextMeshProUGUI armorStatText;

    [Header("Item Details")]
    public GameObject itemDetailPanel;
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI detailStats;
    public Button equipButton;
    public Button unequipButton;
    public Button sellButton;

    [Header("Stats Display")]
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI critText;
    public TextMeshProUGUI attackSpeedText;

    [Header("Player Preview")]
    public Image playerPreviewImage;

    private InventoryManager inventory;
    private ItemSO selectedItem;
    private List<GameObject> slotObjects = new List<GameObject>();

    // FIX: Cached component references for instantiated slot objects
    private class CachedSlotComponents {
        public Image icon;
        public Button button;
    }
    private List<CachedSlotComponents> cachedSlotComponents = new List<CachedSlotComponents>();

    void Start() {
        inventory = InventoryManager.Instance;
        if (inventory == null) {
            Debug.LogError("[InventoryUI] InventoryManager.Instance is null!");
            return;
        }
        
        // Subscribe to inventory events
        inventory.OnInventoryChanged += RefreshUI;
        inventory.OnItemEquipped += OnItemEquipped;
        inventory.OnItemUnequipped += OnItemUnequipped;

        itemDetailPanel?.SetActive(false);
        RefreshUI();
    }

    void OnEnable() {
        RefreshUI();
    }
    
    void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks
        if (inventory != null) {
            inventory.OnInventoryChanged -= RefreshUI;
            inventory.OnItemEquipped -= OnItemEquipped;
            inventory.OnItemUnequipped -= OnItemUnequipped;
        }
    }

    void RefreshUI() {
        if (inventory == null) return;
        
        ClearSlots();
        PopulateInventory();
        UpdateEquipmentSlots();
        UpdateStatsDisplay();
    }

    void ClearSlots() {
        foreach (var slot in slotObjects) {
            if (slot != null) {
                // FIX: Remove button listeners before destroying (use cached reference if available)
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

    void PopulateInventory() {
        if (inventoryGrid == null || itemSlotPrefab == null || inventory == null) return;

        foreach (var item in inventory.items) {
            GameObject slot = Instantiate(itemSlotPrefab, inventoryGrid);
            slotObjects.Add(slot);

            // FIX: Cache components to avoid repeated GetComponent calls
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

    void UpdateStatsDisplay() {
        if (inventory == null || inventory.player == null) return;

        var stats = inventory.player.stats;
        if (damageText != null) damageText.text = $"Damage: {stats.Damage}";
        if (defenseText != null) defenseText.text = $"Defense: {stats.Defense}";
        if (critText != null) critText.text = $"Crit: {stats.Crit:P0}";
        if (attackSpeedText != null) attackSpeedText.text = $"Attack Speed: {stats.AttackSpeed:F1}";
    }

    void SelectItem(ItemSO item) {
        if (item == null) return;
        
        selectedItem = item;
        if (itemDetailPanel == null) return;

        itemDetailPanel.SetActive(true);

        // Update details
        if (detailIcon != null) {
            detailIcon.sprite = item.icon;
            detailIcon.color = item.rarityColor;
        }
        if (detailName != null) {
            detailName.text = item.itemName;
            detailName.color = item.rarityColor;
        }
        if (detailDescription != null) detailDescription.text = item.description;

        // Build stats text
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (item.damageBonus > 0) sb.AppendLine($"Damage: +{item.damageBonus}");
        if (item.defenseBonus > 0) sb.AppendLine($"Defense: +{item.defenseBonus}");
        if (item.critBonus > 0) sb.AppendLine($"Crit Chance: +{item.critBonus:P0}");
        if (item.attackSpeedBonus > 0) sb.AppendLine($"Attack Speed: +{item.attackSpeedBonus:F1}");
        if (item.maxHPBonus > 0) sb.AppendLine($"Max HP: +{item.maxHPBonus}");
        sb.AppendLine($"Sell Price: {inventory.GetSellPrice(item)} gold");
        if (detailStats != null) detailStats.text = sb.ToString();

        // Configure buttons
        if (equipButton != null) {
            equipButton.gameObject.SetActive(true);
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(() => EquipSelectedItem());
        }
        if (unequipButton != null) unequipButton.gameObject.SetActive(false);
        if (sellButton != null) {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => SellSelectedItem());
        }
    }

    void EquipSelectedItem() {
        if (selectedItem == null || inventory == null) return;
        
        inventory.Equip(selectedItem);
        itemDetailPanel.SetActive(false);
        selectedItem = null;
    }

    void SellSelectedItem() {
        if (selectedItem == null) return;
        
        ShopManager.Instance?.SellItem(selectedItem);
        itemDetailPanel.SetActive(false);
        selectedItem = null;
    }

    void OnItemEquipped(ItemSO item) {
        // Could show equipped notification
    }

    void OnItemUnequipped(ItemSO item) {
        // Could show unequipped notification
    }

    public void OnWeaponSlotClicked() {
        if (inventory != null && inventory.equippedWeapon != null) {
            SelectEquippedItem(inventory.equippedWeapon, EquipSlot.Weapon);
        }
    }

    public void OnArmorSlotClicked() {
        if (inventory != null && inventory.equippedArmor != null) {
            SelectEquippedItem(inventory.equippedArmor, EquipSlot.Armor);
        }
    }

    void SelectEquippedItem(ItemSO item, EquipSlot slot) {
        selectedItem = item;
        if (itemDetailPanel == null) return;

        itemDetailPanel.SetActive(true);

        if (detailIcon != null) {
            detailIcon.sprite = item.icon;
            detailIcon.color = item.rarityColor;
        }
        if (detailName != null) {
            detailName.text = $"{item.itemName} (Equipped)";
            detailName.color = item.rarityColor;
        }
        if (detailDescription != null) detailDescription.text = item.description;

        // Configure buttons
        if (equipButton != null) equipButton.gameObject.SetActive(false);
        if (unequipButton != null) {
            unequipButton.gameObject.SetActive(true);
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(() => {
                if (inventory != null) {
                    inventory.UnequipSlot(slot);
                }
                itemDetailPanel.SetActive(false);
            });
        }
        if (sellButton != null) {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => SellSelectedItem());
        }
    }
}

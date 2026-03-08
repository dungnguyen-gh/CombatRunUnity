using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player inventory and equipment slots.
/// Handles item addition, removal, equipping, and stat application.
/// </summary>
public class InventoryManager : MonoBehaviour {
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory")]
    [SerializeField] private List<ItemSO> items = new List<ItemSO>();
    public int maxInventorySlots = 20;
    
    /// <summary>
    /// Read-only access to inventory items. Use AddItem/RemoveItem to modify.
    /// </summary>
    public IReadOnlyList<ItemSO> Items => items.AsReadOnly();
    
    [Header("Currency")]
    [SerializeField] private int gold = 0;
    public int Gold {
        get => gold;
        private set {
            gold = value;
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }
    }

    [Header("Equipment")]
    public ItemSO equippedWeapon;
    public ItemSO equippedArmor;

    [Header("References")]
    public PlayerController player;

    // Events
    public System.Action OnInventoryChanged;
    public System.Action<ItemSO> OnItemEquipped;
    public System.Action<ItemSO> OnItemUnequipped;
    public System.Action<int> OnGoldChanged;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        // FIX: Add null check for FindObjectOfType result
        if (player == null) {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) {
                Debug.LogWarning("[InventoryManager] PlayerController not found in scene!");
            }
        }
        
        // Apply initial equipment
        RefreshPlayerStats();
    }

    /// <summary>
    /// Adds an item to the inventory if space is available.
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns>True if item was added, false if inventory is full</returns>
    public bool AddItem(ItemSO item) {
        if (item == null) {
            Debug.LogWarning("InventoryManager: Attempted to add null item");
            return false;
        }
        
        if (items.Count >= maxInventorySlots) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("Inventory full!");
            }
            return false;
        }

        items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory.
    /// </summary>
    public void RemoveItem(ItemSO item) {
        if (item == null) return;
        
        if (items.Remove(item)) {
            OnInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// Equips an item and moves previously equipped item to inventory.
    /// </summary>
    public void Equip(ItemSO item) {
        if (item == null) return;

        if (item.slot == EquipSlot.Weapon) {
            if (equippedWeapon != null) UnequipSlot(EquipSlot.Weapon);
            equippedWeapon = item;
            
            // Update weapon type for mastery tracking
            if (player != null) {
                player.currentWeaponType = item.weaponType.ToString();
            }
            
            // Apply visual
            if (player != null) {
                player.SetWeaponVisual(item.itemSprite);
            }
        } else if (item.slot == EquipSlot.Armor) {
            if (equippedArmor != null) UnequipSlot(EquipSlot.Armor);
            equippedArmor = item;
            if (player != null) {
                player.SetArmorVisual(item.itemSprite);
            }
        }

        // Remove from inventory if it's there
        if (items.Contains(item)) {
            items.Remove(item);
        }

        RefreshPlayerStats();
        OnItemEquipped?.Invoke(item);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Unequips an item from the specified slot.
    /// If inventory is full, the item remains equipped and a warning is shown.
    /// </summary>
    public void UnequipSlot(EquipSlot slot) {
        ItemSO item = null;
        
        if (slot == EquipSlot.Weapon && equippedWeapon != null) {
            item = equippedWeapon;
            equippedWeapon = null;
            if (player != null) {
                player.SetWeaponVisual(null);
            }
        } else if (slot == EquipSlot.Armor && equippedArmor != null) {
            item = equippedArmor;
            equippedArmor = null;
            if (player != null) {
                player.SetArmorVisual(null);
            }
        }

        if (item != null) {
            // Check if inventory has space
            if (items.Count < maxInventorySlots) {
                items.Add(item);
                
                RefreshPlayerStats();
                OnItemUnequipped?.Invoke(item);
                OnInventoryChanged?.Invoke();
            } else {
                // Inventory full - re-equip the item and show warning
                if (slot == EquipSlot.Weapon) {
                    equippedWeapon = item;
                    if (player != null) player.SetWeaponVisual(item.itemSprite);
                } else if (slot == EquipSlot.Armor) {
                    equippedArmor = item;
                    if (player != null) player.SetArmorVisual(item.itemSprite);
                }
                
                if (UIManager.Instance != null) {
                    UIManager.Instance.ShowNotification("Cannot unequip - inventory full!");
                }
            }
        }
    }

    /// <summary>
    /// Unequips the specified item.
    /// </summary>
    public void Unequip(ItemSO item) {
        if (item == null) return;
        UnequipSlot(item.slot);
    }

    /// <summary>
    /// Recalculates player stats based on current equipment.
    /// </summary>
    public void RefreshPlayerStats() {
        if (player == null) return;
        
        player.stats.ResetMods();
        
        if (equippedWeapon != null) player.stats.ApplyItem(equippedWeapon);
        if (equippedArmor != null) player.stats.ApplyItem(equippedArmor);
        
        player.UpdateStatsFromEquipment();
    }

    /// <summary>
    /// For shop preview - temporarily apply stats without equipping.
    /// </summary>
    public void PreviewEquip(ItemSO item) {
        if (player == null || item == null) return;
        
        player.stats.ResetMods();
        
        // Apply current equipment
        if (equippedWeapon != null) player.stats.ApplyItem(equippedWeapon);
        if (equippedArmor != null) player.stats.ApplyItem(equippedArmor);
        
        // Add preview item
        player.stats.ApplyItem(item);

        // Apply visual preview for SPUM
        if (player.useSPUM && player.spumEquipment != null) {
            if (item.slot == EquipSlot.Weapon)
                player.spumEquipment.EquipWeapon(item.itemSprite, item.weaponType);
            else if (item.slot == EquipSlot.Armor)
                player.spumEquipment.EquipArmor(item.itemSprite);
        }
    }

    /// <summary>
    /// Ends equipment preview and restores actual equipment visuals.
    /// </summary>
    public void EndPreview() {
        RefreshPlayerStats();
        
        // Restore visual equipment
        if (player != null) {
            if (player.useSPUM && player.spumEquipment != null) {
                if (equippedWeapon != null)
                    player.spumEquipment.EquipWeapon(equippedWeapon.itemSprite, equippedWeapon.weaponType);
                else
                    player.spumEquipment.UnequipWeapon();
                    
                if (equippedArmor != null)
                    player.spumEquipment.EquipArmor(equippedArmor.itemSprite);
                else
                    player.spumEquipment.UnequipArmor();
            } else {
                player.SetWeaponVisual(equippedWeapon?.itemSprite);
                player.SetArmorVisual(equippedArmor?.itemSprite);
            }
        }
    }

    /// <summary>
    /// Adds gold to the player's inventory.
    /// </summary>
    public void AddGold(int amount) {
        if (amount > 0) {
            Gold += amount;
        }
    }
    
    /// <summary>
    /// Attempts to remove gold from the player's inventory.
    /// </summary>
    /// <returns>True if gold was successfully removed.</returns>
    public bool RemoveGold(int amount) {
        if (amount <= 0) return true;
        if (Gold >= amount) {
            Gold -= amount;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Calculates the sell price for an item (50% of buy price).
    /// </summary>
    public int GetSellPrice(ItemSO item) {
        if (item == null) return 0;
        return Mathf.RoundToInt(item.sellPrice > 0 ? item.sellPrice : item.price * 0.5f);
    }

    /// <summary>
    /// Checks if the inventory has room for more items.
    /// </summary>
    public bool HasInventorySpace() {
        return items.Count < maxInventorySlots;
    }

    /// <summary>
    /// Gets the number of empty inventory slots.
    /// </summary>
    public int GetEmptySlotCount() {
        return maxInventorySlots - items.Count;
    }
    
    /// <summary>
    /// Gets the number of items currently in inventory.
    /// </summary>
    public int GetItemCount() {
        return items.Count;
    }
    
    /// <summary>
    /// Gets an item at a specific index.
    /// </summary>
    /// <param name="index">Index in the inventory list</param>
    /// <returns>The item at that index, or null if invalid</returns>
    public ItemSO GetItem(int index) {
        if (index < 0 || index >= items.Count) return null;
        return items[index];
    }
    
    /// <summary>
    /// Checks if an item exists in the inventory.
    /// </summary>
    public bool ContainsItem(ItemSO item) {
        return items.Contains(item);
    }
}

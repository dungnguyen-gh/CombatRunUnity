using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active equipment set bonuses and applies their effects.
/// Prevents duplicate component additions through proper tracking.
/// Enhanced with events for set bonus changes and methods to get current set progress.
/// </summary>
public class SetBonusManager : MonoBehaviour {
    public static SetBonusManager Instance { get; private set; }

    [Header("All Sets")]
    public List<EquipmentSetSO> allSets = new List<EquipmentSetSO>();

    [Header("Current Bonuses")]
    public List<ActiveSetBonus> activeBonuses = new List<ActiveSetBonus>();

    [Header("References")]
    public InventoryManager inventory;
    public PlayerController player;

    // Track which special effects are currently active to prevent duplicates
    private HashSet<SetSpecialEffect> activeSpecialEffects = new HashSet<SetSpecialEffect>();

    // Events
    public System.Action<EquipmentSetSO, int> OnSetBonusActivated;  // Set, piece count (2 or 4)
    public System.Action<EquipmentSetSO> OnSetBonusLost;             // Set
    public System.Action<EquipmentSetSO, int> OnSetProgressChanged;  // Set, current piece count
    public System.Action OnAllSetBonusesUpdated;                     // Called when any set bonus changes

    // Store delegates as fields for proper unsubscription
    private System.Action<ItemSO> onItemEquippedDelegate;
    private System.Action<ItemSO> onItemUnequippedDelegate;
    
    // Track previous bonus states to detect changes
    private Dictionary<EquipmentSetSO, int> previousPieceCounts = new Dictionary<EquipmentSetSO, int>();
    private Dictionary<EquipmentSetSO, bool> previous2PieceState = new Dictionary<EquipmentSetSO, bool>();
    private Dictionary<EquipmentSetSO, bool> previous4PieceState = new Dictionary<EquipmentSetSO, bool>();

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
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        LoadAllSets();
        
        // Subscribe to equipment changes using stored delegates
        if (inventory != null) {
            onItemEquippedDelegate = _ => UpdateSetBonuses();
            onItemUnequippedDelegate = _ => UpdateSetBonuses();
            
            inventory.OnItemEquipped += onItemEquippedDelegate;
            inventory.OnItemUnequipped += onItemUnequippedDelegate;
        }

        UpdateSetBonuses();
    }

    void OnDestroy() {
        // Unsubscribe from events using stored delegates to prevent memory leaks
        if (inventory != null) {
            if (onItemEquippedDelegate != null)
                inventory.OnItemEquipped -= onItemEquippedDelegate;
            if (onItemUnequippedDelegate != null)
                inventory.OnItemUnequipped -= onItemUnequippedDelegate;
        }
    }

    void LoadAllSets() {
        EquipmentSetSO[] loadedSets = Resources.LoadAll<EquipmentSetSO>("Sets");
        allSets.AddRange(loadedSets);
    }

    /// <summary>
    /// Updates all active set bonuses based on currently equipped items.
    /// </summary>
    public void UpdateSetBonuses() {
        // Store previous states before clearing
        Dictionary<EquipmentSetSO, int> oldPieceCounts = new Dictionary<EquipmentSetSO, int>(previousPieceCounts);
        Dictionary<EquipmentSetSO, bool> old2PieceState = new Dictionary<EquipmentSetSO, bool>(previous2PieceState);
        Dictionary<EquipmentSetSO, bool> old4PieceState = new Dictionary<EquipmentSetSO, bool>(previous4PieceState);
        
        // Clear current tracking
        activeBonuses.Clear();
        previousPieceCounts.Clear();
        previous2PieceState.Clear();
        previous4PieceState.Clear();
        
        // Track which effects were active before
        HashSet<SetSpecialEffect> previousEffects = new HashSet<SetSpecialEffect>(activeSpecialEffects);
        activeSpecialEffects.Clear();

        // Count pieces for each set (only from equipped items, not inventory)
        Dictionary<EquipmentSetSO, int> setCounts = new Dictionary<EquipmentSetSO, int>();

        foreach (var set in allSets) {
            int count = CountSetPieces(set);
            if (count > 0) {
                setCounts[set] = count;
                
                bool has2Piece = count >= 2 && set.has2PieceBonus;
                bool has4Piece = count >= 4 && set.has4PieceBonus;
                
                ActiveSetBonus bonus = new ActiveSetBonus {
                    set = set,
                    pieceCount = count,
                    has2Piece = has2Piece,
                    has4Piece = has4Piece
                };
                activeBonuses.Add(bonus);

                // Track current state
                previousPieceCounts[set] = count;
                previous2PieceState[set] = has2Piece;
                previous4PieceState[set] = has4Piece;

                // Check for state changes and notify
                bool had2Piece = old2PieceState.ContainsKey(set) && old2PieceState[set];
                bool had4Piece = old4PieceState.ContainsKey(set) && old4PieceState[set];
                int oldCount = oldPieceCounts.ContainsKey(set) ? oldPieceCounts[set] : 0;

                // Notify if 2-piece threshold reached for the first time
                if (has2Piece && !had2Piece) {
                    OnSetBonusActivated?.Invoke(set, 2);
                    ShowSetBonusNotification(set, 2);
                }
                
                // Notify if 4-piece threshold reached for the first time
                if (has4Piece && !had4Piece) {
                    OnSetBonusActivated?.Invoke(set, 4);
                    ShowSetBonusNotification(set, 4);
                }
                
                // Notify if piece count changed
                if (count != oldCount) {
                    OnSetProgressChanged?.Invoke(set, count);
                }
                
                // Notify if bonus lost
                if (!has2Piece && had2Piece) {
                    OnSetBonusLost?.Invoke(set);
                }
                if (!has4Piece && had4Piece) {
                    // Only notify if still have 2-piece (otherwise already notified by has2Piece check)
                    if (has2Piece) {
                        OnSetBonusLost?.Invoke(set);
                    }
                }

                // Track special effects
                if (has4Piece) {
                    activeSpecialEffects.Add(set.specialEffect4);
                }
            } else if (oldPieceCounts.ContainsKey(set) && oldPieceCounts[set] > 0) {
                // Had pieces before, now have none
                OnSetBonusLost?.Invoke(set);
            }
        }

        // Apply stats
        ApplySetBonusStats();
        
        // Handle special effects (remove old ones, add new ones)
        UpdateSpecialEffects(previousEffects, activeSpecialEffects);
        
        // Notify that all bonuses have been updated
        OnAllSetBonusesUpdated?.Invoke();
    }

    /// <summary>
    /// Counts equipped pieces of a set (only from equipment slots, not inventory).
    /// </summary>
    int CountSetPieces(EquipmentSetSO set) {
        int count = 0;
        
        // Guard against null inventory
        if (inventory == null) return 0;
        
        // Check equipped weapon
        if (inventory.equippedWeapon != null) {
            if (set.setPieceIds.Contains(inventory.equippedWeapon.itemId)) {
                count++;
            }
        }
        
        // Check equipped armor
        if (inventory.equippedArmor != null) {
            if (set.setPieceIds.Contains(inventory.equippedArmor.itemId)) {
                count++;
            }
        }

        return count;
    }

    void ApplySetBonusStats() {
        if (player == null || player.stats == null) return;
        
        // Reset and reapply all set bonuses
        // Note: This assumes PlayerStats.ResetMods() will be called before this
        
        foreach (var bonus in activeBonuses) {
            if (bonus.has2Piece) {
                player.stats.damageMod += bonus.set.damageBonus2;
                player.stats.defenseMod += bonus.set.defenseBonus2;
                player.stats.critMod += bonus.set.critBonus2;
                player.stats.attackSpeedMod += bonus.set.attackSpeedBonus2;
                player.stats.maxHPMod += bonus.set.maxHPBonus2;
            }
            
            if (bonus.has4Piece) {
                player.stats.damageMod += bonus.set.damageBonus4;
                player.stats.defenseMod += bonus.set.defenseBonus4;
                player.stats.critMod += bonus.set.critBonus4;
                player.stats.attackSpeedMod += bonus.set.attackSpeedBonus4;
                player.stats.maxHPMod += bonus.set.maxHPBonus4;
            }
        }

        player.UpdateStatsFromEquipment();
    }

    /// <summary>
    /// Updates special effect components, removing old ones and adding new ones as needed.
    /// </summary>
    void UpdateSpecialEffects(HashSet<SetSpecialEffect> previous, HashSet<SetSpecialEffect> current) {
        // Remove effects that are no longer active
        foreach (var effect in previous) {
            if (!current.Contains(effect)) {
                RemoveSpecialEffect(effect);
            }
        }
        
        // Add effects that are newly active
        foreach (var effect in current) {
            if (!previous.Contains(effect)) {
                ApplySpecialEffect(effect);
            }
        }
    }

    void ApplySpecialEffect(SetSpecialEffect effect) {
        if (player == null) return;
        
        switch (effect) {
            case SetSpecialEffect.LifeSteal:
                // Only add if not present
                var ls = player.GetComponent<LifeStealEffect>();
                if (ls == null) {
                    player.gameObject.AddComponent<LifeStealEffect>().lifeStealPercent = 0.1f;
                }
                break;
            case SetSpecialEffect.BurnOnHit:
                var burn = player.GetComponent<BurnOnHitEffect>();
                if (burn == null) {
                    player.gameObject.AddComponent<BurnOnHitEffect>();
                }
                break;
            // Other effects handled elsewhere
        }
    }

    void RemoveSpecialEffect(SetSpecialEffect effect) {
        if (player == null) return;
        
        switch (effect) {
            case SetSpecialEffect.LifeSteal:
                var ls = player.GetComponent<LifeStealEffect>();
                if (ls != null) {
                    Destroy(ls);
                }
                break;
            case SetSpecialEffect.BurnOnHit:
                var burn = player.GetComponent<BurnOnHitEffect>();
                if (burn != null) {
                    Destroy(burn);
                }
                break;
        }
    }

    void ShowSetBonusNotification(EquipmentSetSO set, int pieceCount) {
        string bonusText = pieceCount == 2 ? set.bonusDescription2 : set.bonusDescription4;
        if (UIManager.Instance != null) {
            string setColorHex = ColorUtility.ToHtmlStringRGB(set.setColor);
            UIManager.Instance.ShowNotification($"<color=#{setColorHex}>{set.setName}</color> ({pieceCount}) Bonus: {bonusText}");
        }
    }

    /// <summary>
    /// Checks if a specific set bonus is active.
    /// </summary>
    public bool HasSetBonus(EquipmentSetSO set, int pieceCount) {
        foreach (var bonus in activeBonuses) {
            if (bonus.set == set && bonus.pieceCount >= pieceCount) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the current piece count for a set.
    /// </summary>
    public int GetPieceCount(EquipmentSetSO set) {
        foreach (var bonus in activeBonuses) {
            if (bonus.set == set) return bonus.pieceCount;
        }
        return 0;
    }
    
    /// <summary>
    /// Gets the full set progress information including which pieces are equipped.
    /// </summary>
    public SetProgressInfo GetSetProgress(EquipmentSetSO set) {
        SetProgressInfo info = new SetProgressInfo {
            set = set,
            equippedCount = GetPieceCount(set),
            totalPieces = set.setPieceIds.Count,
            has2PieceBonus = false,
            has4PieceBonus = false,
            equippedPieceIds = new List<string>()
        };
        
        // Check which specific pieces are equipped
        if (inventory != null) {
            if (inventory.equippedWeapon != null && set.setPieceIds.Contains(inventory.equippedWeapon.itemId)) {
                info.equippedPieceIds.Add(inventory.equippedWeapon.itemId);
            }
            if (inventory.equippedArmor != null && set.setPieceIds.Contains(inventory.equippedArmor.itemId)) {
                info.equippedPieceIds.Add(inventory.equippedArmor.itemId);
            }
        }
        
        // Check bonus states
        foreach (var bonus in activeBonuses) {
            if (bonus.set == set) {
                info.has2PieceBonus = bonus.has2Piece;
                info.has4PieceBonus = bonus.has4Piece;
                break;
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// Gets all active set bonuses.
    /// </summary>
    public List<ActiveSetBonus> GetAllActiveBonuses() {
        return new List<ActiveSetBonus>(activeBonuses);
    }
    
    /// <summary>
    /// Finds a set by its ID.
    /// </summary>
    public EquipmentSetSO GetSetById(string setId) {
        foreach (var set in allSets) {
            if (set.setId == setId) {
                return set;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Gets all sets that contain a specific item.
    /// </summary>
    public List<EquipmentSetSO> GetSetsForItem(ItemSO item) {
        List<EquipmentSetSO> sets = new List<EquipmentSetSO>();
        if (item == null) return sets;
        
        foreach (var set in allSets) {
            if (set.setPieceIds.Contains(item.itemId)) {
                sets.Add(set);
            }
        }
        return sets;
    }
}

/// <summary>
/// Helper class containing detailed set progress information.
/// </summary>
[System.Serializable]
public class SetProgressInfo {
    public EquipmentSetSO set;
    public int equippedCount;
    public int totalPieces;
    public bool has2PieceBonus;
    public bool has4PieceBonus;
    public List<string> equippedPieceIds;
    
    public float ProgressPercent => totalPieces > 0 ? (float)equippedCount / totalPieces : 0f;
    public bool IsComplete => equippedCount >= totalPieces;
}

// Helper component for LifeSteal
public class LifeStealEffect : MonoBehaviour {
    public float lifeStealPercent = 0.1f;
    private PlayerController player;

    void Awake() {
        player = GetComponent<PlayerController>();
    }

    public void OnDealDamage(int damage) {
        int heal = Mathf.RoundToInt(damage * lifeStealPercent);
        player?.Heal(heal);
    }
}

// Helper component for Burn on Hit
public class BurnOnHitEffect : MonoBehaviour {
    public StatusEffectData burnData;

    void Awake() {
        burnData = new StatusEffectData {
            type = StatusType.Burn,
            duration = 3f,
            tickRate = 0.5f,
            damagePerTick = 5,
            effectColor = new Color(1f, 0.3f, 0f)
        };
    }

    public void ApplyBurn(Enemy enemy) {
        if (enemy == null) return;
        
        var status = enemy.GetComponent<StatusEffect>();
        if (status != null) {
            status.ApplyStatus(burnData);
        }
    }
}

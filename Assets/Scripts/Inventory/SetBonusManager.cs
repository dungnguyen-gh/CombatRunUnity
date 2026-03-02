using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active equipment set bonuses and applies their effects.
/// Prevents duplicate component additions through proper tracking.
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
    public System.Action<EquipmentSetSO, int> OnSetBonusActivated;
    public System.Action<EquipmentSetSO> OnSetBonusLost;

    // FIX: Store delegates as fields for proper unsubscription
    private System.Action<ItemSO> onItemEquippedDelegate;
    private System.Action<ItemSO> onItemUnequippedDelegate;

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
        
        // FIX: Subscribe to equipment changes using stored delegates
        if (inventory != null) {
            onItemEquippedDelegate = _ => UpdateSetBonuses();
            onItemUnequippedDelegate = _ => UpdateSetBonuses();
            
            inventory.OnItemEquipped += onItemEquippedDelegate;
            inventory.OnItemUnequipped += onItemUnequippedDelegate;
        }

        UpdateSetBonuses();
    }

    void OnDestroy() {
        // FIX: Unsubscribe from events using stored delegates to prevent memory leaks
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
        // Clear current bonuses tracking
        activeBonuses.Clear();
        
        // Track which effects were active before
        HashSet<SetSpecialEffect> previousEffects = new HashSet<SetSpecialEffect>(activeSpecialEffects);
        activeSpecialEffects.Clear();

        // Count pieces for each set (only from equipped items, not inventory)
        Dictionary<EquipmentSetSO, int> setCounts = new Dictionary<EquipmentSetSO, int>();

        foreach (var set in allSets) {
            int count = CountSetPieces(set);
            if (count > 0) {
                setCounts[set] = count;
                
                ActiveSetBonus bonus = new ActiveSetBonus {
                    set = set,
                    pieceCount = count,
                    has2Piece = count >= 2 && set.has2PieceBonus,
                    has4Piece = count >= 4 && set.has4PieceBonus
                };
                activeBonuses.Add(bonus);

                // Notify if threshold reached
                if (count == 2 && set.has2PieceBonus) {
                    OnSetBonusActivated?.Invoke(set, 2);
                    ShowSetBonusNotification(set, 2);
                }
                if (count == 4 && set.has4PieceBonus) {
                    OnSetBonusActivated?.Invoke(set, 4);
                    ShowSetBonusNotification(set, 4);
                }

                // Track special effects
                if (bonus.has4Piece) {
                    activeSpecialEffects.Add(set.specialEffect4);
                }
            }
        }

        // Apply stats
        ApplySetBonusStats();
        
        // Handle special effects (remove old ones, add new ones)
        UpdateSpecialEffects(previousEffects, activeSpecialEffects);
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
            UIManager.Instance.ShowNotification($"{set.setName} ({pieceCount}) Bonus: {bonusText}");
        }
    }

    public bool HasSetBonus(EquipmentSetSO set, int pieceCount) {
        foreach (var bonus in activeBonuses) {
            if (bonus.set == set && bonus.pieceCount >= pieceCount) {
                return true;
            }
        }
        return false;
    }

    public int GetPieceCount(EquipmentSetSO set) {
        foreach (var bonus in activeBonuses) {
            if (bonus.set == set) return bonus.pieceCount;
        }
        return 0;
    }
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

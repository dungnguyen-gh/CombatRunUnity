using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a gambling option available to the player.
/// </summary>
[System.Serializable]
public class GambleOption {
    public string optionName;
    public string description;
    public int goldCost;
    public GambleType type;
    public float successChance = 0.5f;
    public int minReward;
    public int maxReward;
    public ItemRarity guaranteedRarityOnSuccess = ItemRarity.Common;
    public string failureDescription;
}

public enum GambleType {
    GoldDoubleOrNothing,    // 50% chance to double gold, 50% lose it
    MysteryItem,            // Pay for random rarity item
    HealthForGold,          // Lose HP, gain gold
    CursedItem,             // Get powerful item with negative effect
    RerollStats,            // Randomize current stats temporarily
    MysterySkill            // Get random skill effect
}

/// <summary>
/// Provides gambling mechanics (double-or-nothing, mystery items, cursed items, stat rerolls).
/// </summary>
public class GambleSystem : MonoBehaviour {
    public static GambleSystem Instance { get; private set; }

    [Header("Gamble Options")]
    public List<GambleOption> availableGambles = new List<GambleOption>();

    [Header("Cursed Items Pool")]
    public List<ItemSO> cursedItems = new List<ItemSO>();

    [Header("References")]
    public PlayerController player;
    public InventoryManager inventory;
    public ShopManager shop;

    // Track active curses for cleanup
    private List<MonoBehaviour> activeCurses = new List<MonoBehaviour>();
    private Dictionary<string, int> originalBaseStats = new Dictionary<string, int>();

    // Events
    public System.Action<GambleOption, bool> OnGambleResolved;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        // Validate references
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (inventory == null) inventory = InventoryManager.Instance;
        if (shop == null) shop = ShopManager.Instance;
        
        if (player == null || inventory == null) {
            Debug.LogError("[GambleSystem] Missing required references (player or inventory)!");
            enabled = false;
            return;
        }
        
        InitializeDefaultGambles();
    }

    void InitializeDefaultGambles() {
        // Double or Nothing
        availableGambles.Add(new GambleOption {
            optionName = "Double or Nothing",
            description = "50% chance to double your gold. 50% chance to lose half.",
            goldCost = 0,
            type = GambleType.GoldDoubleOrNothing,
            successChance = 0.5f,
            failureDescription = "Lost half your gold!"
        });

        // Mystery Item
        availableGambles.Add(new GambleOption {
            optionName = "Mystery Item",
            description = "Pay 100 gold for a random item. 30% chance for Rare or better!",
            goldCost = 100,
            type = GambleType.MysteryItem,
            successChance = 0.3f,
            minReward = 1,
            maxReward = 1,
            guaranteedRarityOnSuccess = ItemRarity.Rare,
            failureDescription = "Got a Common item."
        });

        // Health for Gold
        availableGambles.Add(new GambleOption {
            optionName = "Blood Money",
            description = "Lose 30% HP, gain 200 gold.",
            goldCost = 0,
            type = GambleType.HealthForGold,
            successChance = 1f, // Always succeeds
            minReward = 200,
            maxReward = 200
        });

        // Cursed Item
        availableGambles.Add(new GambleOption {
            optionName = "Dark Bargain",
            description = "Get a powerful cursed item... with a price.",
            goldCost = 50,
            type = GambleType.CursedItem,
            successChance = 1f
        });

        // Reroll Stats
        availableGambles.Add(new GambleOption {
            optionName = "Chaos Reroll",
            description = "Randomize all your stats for this run. Risky!",
            goldCost = 25,
            type = GambleType.RerollStats,
            successChance = 1f
        });
    }

    /// <summary>
    /// Executes a gamble option.
    /// </summary>
    public bool ExecuteGamble(GambleOption option) {
        if (player == null) return false;

        // Check gold cost
        if (option.goldCost > 0 && (inventory == null || !inventory.RemoveGold(option.goldCost))) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("Not enough gold!");
            }
            return false;
        }

        bool success = Random.value < option.successChance;
        
        switch (option.type) {
            case GambleType.GoldDoubleOrNothing:
                ResolveGoldGamble(success);
                break;
            case GambleType.MysteryItem:
                ResolveMysteryItem(success);
                break;
            case GambleType.HealthForGold:
                ResolveHealthForGold();
                break;
            case GambleType.CursedItem:
                ResolveCursedItem();
                break;
            case GambleType.RerollStats:
                ResolveRerollStats();
                break;
        }

        OnGambleResolved?.Invoke(option, success);
        return true;
    }

    void ResolveGoldGamble(bool success) {
        int currentGold = inventory?.Gold ?? 0;
        
        if (success) {
            int reward = currentGold;
            inventory?.AddGold(reward);
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification($"JACKPOT! Gained {reward} gold!");
            }
        } else {
            int loss = currentGold / 2;
            inventory?.RemoveGold(loss);
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification($"Unlucky... Lost {loss} gold.");
            }
        }
    }

    void ResolveMysteryItem(bool success) {
        ItemRarity rarity = success ? ItemRarity.Rare : ItemRarity.Common;
        
        // Check inventory space first
        if (!inventory.HasInventorySpace()) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("Inventory full! Cannot receive item.");
            }
            // Refund the gold cost
            inventory?.AddGold(100);
            return;
        }
        
        // Get random item of that rarity
        ItemSO reward = GetRandomItemOfRarity(rarity);

        if (reward != null) {
            inventory.AddItem(reward);
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification($"Got: {reward.itemName}!");
            }
        }
    }
    
    /// <summary>
    /// Gets a random item of the specified rarity from the shop.
    /// </summary>
    ItemSO GetRandomItemOfRarity(ItemRarity rarity) {
        if (shop == null || shop.availableItems == null || shop.availableItems.Count == 0) {
            return null;
        }
        
        List<ItemSO> matchingItems = new List<ItemSO>();
        foreach (var item in shop.availableItems) {
            if (item != null && item.rarity == rarity) {
                matchingItems.Add(item);
            }
        }

        if (matchingItems.Count > 0) {
            return matchingItems[Random.Range(0, matchingItems.Count)];
        }
        return null;
    }

    void ResolveHealthForGold() {
        int healthLoss = Mathf.RoundToInt(player.stats.MaxHP * 0.3f);
        player.TakeDamage(healthLoss);
        player.AddGold(200);
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification("Sacrificed HP for gold!");
        }
    }

    void ResolveCursedItem() {
        if (cursedItems.Count == 0) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("No cursed items available!");
            }
            return;
        }
        
        // Check inventory space
        if (!inventory.HasInventorySpace()) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification("Inventory full! Cannot receive cursed item.");
            }
            // Refund
            inventory?.AddGold(50);
            return;
        }

        ItemSO cursedItem = cursedItems[Random.Range(0, cursedItems.Count)];
        
        // Apply curse effect
        ApplyCurseEffect(cursedItem);
        
        inventory.AddItem(cursedItem);
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification($"Acquired cursed item: {cursedItem.itemName}!");
        }
    }

    void ApplyCurseEffect(ItemSO cursedItem) {
        // Guard against null cursedItem
        if (cursedItem == null) {
            Debug.LogWarning("[GambleSystem] ApplyCurseEffect called with null cursedItem");
            return;
        }
        if (string.IsNullOrEmpty(cursedItem.itemId)) return;
        
        // Different curses based on item name or ID
        if (cursedItem.itemId.Contains("vampire")) {
            // Take damage over time but deal more damage
            var curse = player.gameObject.AddComponent<LifeDrainCurse>();
            if (curse != null) {
                activeCurses.Add(curse);
            }
        } else if (cursedItem.itemId.Contains("glass")) {
            // Double damage dealt and taken
            // Store original values for potential restoration
            originalBaseStats["damage"] = player.stats.baseDamage;
            originalBaseStats["defense"] = player.stats.baseDefense;
            
            player.stats.baseDamage *= 2;
            player.stats.baseDefense = Mathf.Max(0, player.stats.baseDefense - 10);
        }
    }

    void ResolveRerollStats() {
        // Store original values
        originalBaseStats["damage"] = player.stats.baseDamage;
        originalBaseStats["defense"] = player.stats.baseDefense;
        
        // Randomize base stats temporarily
        player.stats.baseDamage = Random.Range(5, 30);
        player.stats.baseDefense = Random.Range(0, 15);
        player.stats.baseCrit = Random.Range(0f, 0.3f);
        player.stats.baseAttackSpeed = Random.Range(0.5f, 2f);
        
        player.UpdateStatsFromEquipment();
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification("Stats randomized!");
        }
    }
    
    /// <summary>
    /// Removes all active curse effects. Call when resetting player state.
    /// </summary>
    public void RemoveAllCurses() {
        foreach (var curse in activeCurses) {
            if (curse != null) {
                Destroy(curse);
            }
        }
        activeCurses.Clear();
    }
    
    /// <summary>
    /// Restores original base stats if they were stored.
    /// </summary>
    public void RestoreOriginalStats() {
        if (originalBaseStats.TryGetValue("damage", out int originalDamage)) {
            player.stats.baseDamage = originalDamage;
        }
        if (originalBaseStats.TryGetValue("defense", out int originalDefense)) {
            player.stats.baseDefense = originalDefense;
        }
        player.UpdateStatsFromEquipment();
    }

    public GambleOption GetGambleByName(string name) {
        return availableGambles.Find(g => g.optionName == name);
    }
}

/// <summary>
/// Curse component that drains health over time but boosts damage.
/// </summary>
public class LifeDrainCurse : MonoBehaviour {
    public float drainInterval = 5f;
    public int drainAmount = 5;
    public int damageBonus = 10;

    private PlayerController player;
    private float timer;
    private bool statsApplied = false;

    void Awake() {
        player = GetComponent<PlayerController>();
        if (player != null && player.stats != null) {
            player.stats.baseDamage += damageBonus;
            statsApplied = true;
        }
    }

    void Update() {
        timer -= Time.deltaTime;
        if (timer <= 0) {
            timer = drainInterval;
            player?.TakeDamage(drainAmount);
        }
    }

    void OnDestroy() {
        // Only remove stats if we added them
        if (statsApplied && player != null && player.stats != null) {
            player.stats.baseDamage -= damageBonus;
        }
    }
}

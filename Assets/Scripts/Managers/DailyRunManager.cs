using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class DailyModifier {
    public string modifierName;
    public string description;
    public ModifierType type;
    public float playerStatMultiplier = 1f;
    public float enemyStatMultiplier = 1f;
    public float goldMultiplier = 1f;
    public float xpMultiplier = 1f;
    public bool enableHardcore = false; // Permadeath
}

public enum ModifierType {
    DoubleDamage,       // Player deals 2x damage
    GlassCannon,        // Double damage both ways
    Tank,               // Double defense, half speed
    SpeedDemon,         // 2x speed, enemies faster too
    RichStart,          // Start with 500 gold
    PoorStart,          // Start with 0 gold, enemies drop 2x
    Hardcore,           // Permadeath, no continues
    RandomSkills,       // Skills randomized every wave
    EnemySwarm,         // 2x enemies, half HP
    BossRush           // Boss every 3 waves
}

public class DailyRunManager : MonoBehaviour {
    public static DailyRunManager Instance { get; private set; }

    [Header("Daily Run")]
    public string currentSeed;
    public DailyModifier[] currentModifiers;
    public bool isDailyRun = false;
    
    // FIX: DateTime is not serializable by Unity - use long (Unix timestamp) instead
    [SerializeField] private long runDateTimestamp;
    
    // Runtime accessor that converts to/from DateTime
    public DateTime RunDate {
        get => runDateTimestamp == 0 ? DateTime.Now : DateTime.FromBinary(runDateTimestamp);
        set => runDateTimestamp = value.ToBinary();
    }

    [Header("Modifier Pool")]
    public List<DailyModifier> allModifiers = new List<DailyModifier>();

    [Header("Leaderboard")]
    public List<DailyRunResult> todayResults = new List<DailyRunResult>();

    // Events
    public System.Action<DailyModifier[]> OnDailyRunStarted;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeModifiers();
        } else {
            Destroy(gameObject);
        }
    }

    void InitializeModifiers() {
        allModifiers.Add(new DailyModifier {
            modifierName = "Double Damage",
            description = "You deal double damage!",
            type = ModifierType.DoubleDamage,
            playerStatMultiplier = 2f,
            enemyStatMultiplier = 1f
        });

        allModifiers.Add(new DailyModifier {
            modifierName = "Glass Cannon",
            description = "Double damage dealt AND taken!",
            type = ModifierType.GlassCannon,
            playerStatMultiplier = 2f,
            enemyStatMultiplier = 2f
        });

        allModifiers.Add(new DailyModifier {
            modifierName = "Tank",
            description = "Double defense, but slower attacks.",
            type = ModifierType.Tank,
            playerStatMultiplier = 1f,
            enemyStatMultiplier = 1f
        });

        allModifiers.Add(new DailyModifier {
            modifierName = "Gold Rush",
            description = "Enemies drop 3x gold!",
            type = ModifierType.PoorStart,
            goldMultiplier = 3f
        });

        allModifiers.Add(new DailyModifier {
            modifierName = "Enemy Swarm",
            description = "More enemies, but they're weaker.",
            type = ModifierType.EnemySwarm,
            enemyStatMultiplier = 0.7f
        });

        allModifiers.Add(new DailyModifier {
            modifierName = "Hardcore",
            description = "One life. Make it count.",
            type = ModifierType.Hardcore,
            enableHardcore = true
        });
    }

    public void GenerateDailyRun() {
        // Generate seed from date
        RunDate = DateTime.Now;
        currentSeed = RunDate.ToString("yyyyMMdd");
        
        // Use seed for deterministic random
        System.Random rng = new System.Random(currentSeed.GetHashCode());
        
        // Pick 2-3 random modifiers
        List<DailyModifier> selected = new List<DailyModifier>();
        int count = rng.Next(2, 4);
        
        List<DailyModifier> pool = new List<DailyModifier>(allModifiers);
        for (int i = 0; i < count && pool.Count > 0; i++) {
            int index = rng.Next(0, pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        currentModifiers = selected.ToArray();
        isDailyRun = true;

        // Show daily run info
        ShowDailyRunInfo();
    }

    public void StartDailyRun() {
        if (!isDailyRun) {
            GenerateDailyRun();
        }

        ApplyModifiers();
        OnDailyRunStarted?.Invoke(currentModifiers);
    }

    void ApplyModifiers() {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        foreach (var mod in currentModifiers) {
            ApplyModifier(mod, player);
        }
    }

    void ApplyModifier(DailyModifier mod, PlayerController player) {
        switch (mod.type) {
            case ModifierType.DoubleDamage:
                player.stats.baseDamage = Mathf.RoundToInt(player.stats.baseDamage * 2);
                break;
            case ModifierType.GlassCannon:
                player.stats.baseDamage = Mathf.RoundToInt(player.stats.baseDamage * 2);
                player.stats.baseDefense = Mathf.Max(0, player.stats.baseDefense - 5);
                break;
            case ModifierType.Tank:
                player.stats.baseDefense *= 2;
                player.stats.baseAttackSpeed *= 0.5f;
                break;
            case ModifierType.RichStart:
                player.AddGold(500);
                break;
            case ModifierType.PoorStart:
                player.SpendGold(player.gold);
                break;
            case ModifierType.EnemySwarm:
                var gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null) {
                    gameManager.enemiesPerWave *= 2;
                }
                break;
        }

        player.UpdateStatsFromEquipment();
    }

    public void SubmitRunResult(int wave, int kills, int gold, bool victory) {
        DailyRunResult result = new DailyRunResult {
            dateTimestamp = runDateTimestamp,
            waveReached = wave,
            enemiesKilled = kills,
            goldCollected = gold,
            victory = victory,
            modifiers = currentModifiers
        };

        todayResults.Add(result);
        
        // Sort by wave, then kills
        todayResults.Sort((a, b) => {
            if (a.waveReached != b.waveReached) 
                return b.waveReached.CompareTo(a.waveReached);
            return b.enemiesKilled.CompareTo(a.enemiesKilled);
        });
    }

    void ShowDailyRunInfo() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DAILY RUN ===");
        sb.AppendLine($"Seed: {currentSeed}");
        sb.AppendLine("\nModifiers:");
        
        foreach (var mod in currentModifiers) {
            sb.AppendLine($"- {mod.modifierName}: {mod.description}");
        }

        Debug.Log(sb.ToString());
        
        // Also show in UI
        UIManager.Instance?.ShowNotification("Daily Run Generated!\nCheck console for details.");
    }

    public string GetModifierSummary() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var mod in currentModifiers) {
            sb.Append(mod.modifierName + ", ");
        }
        return sb.ToString().TrimEnd(',', ' ');
    }
}

[System.Serializable]
public class DailyRunResult {
    // FIX: DateTime is not serializable by Unity - use long (Unix timestamp) instead
    [SerializeField] public long dateTimestamp;
    
    // Runtime accessor for DateTime
    public DateTime Date {
        get => dateTimestamp == 0 ? DateTime.MinValue : DateTime.FromBinary(dateTimestamp);
        set => dateTimestamp = value.ToBinary();
    }
    
    public int waveReached;
    public int enemiesKilled;
    public int goldCollected;
    public bool victory;
    public DailyModifier[] modifiers;
}

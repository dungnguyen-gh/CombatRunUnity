using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a skill synergy - a powerful effect triggered by casting skills in a specific sequence.
/// </summary>
[System.Serializable]
public class SkillSynergy {
    public string synergyName;
    public string description;
    public int[] requiredSkillSequence; // Skill indices that need to be cast in order
    public float timeWindow = 3f; // Time to complete sequence
    public SynergyEffect effect;
    public float effectDuration = 5f;
    public float damageMultiplier = 1.5f;
}

public enum SynergyEffect {
    DamageBoost,        // Next skills do more damage
    CooldownReset,      // Reset all cooldowns
    EmpowerNext,        // Next skill is empowered
    ChainLightning,     // Skills chain to nearby enemies
    DamageReduction,    // Take less damage
    LifeStealAura,     // All damage heals
    InfiniteMana       // No cooldowns for duration
}

/// <summary>
/// Tracks skill cast sequences and activates powerful synergy effects when specific combos are performed.
/// </summary>
public class SkillSynergyManager : MonoBehaviour {
    public static SkillSynergyManager Instance { get; private set; }

    [Header("Synergies")]
    public List<SkillSynergy> synergies = new List<SkillSynergy>();

    [Header("Tracking")]
    private List<int> recentSkillCasts = new List<int>();
    private List<float> recentSkillTimes = new List<float>();
    private SkillCaster skillCaster;
    private PlayerController player;

    [Header("Active Effects")]
    private float activeSynergyTimer = 0f;
    private SkillSynergy activeSynergy;
    private bool synergyActive = false;
    private int activeDefenseBonuses = 0; // Track stacked defense bonuses

    // Events
    public System.Action<SkillSynergy> OnSynergyActivated;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else if (Instance != this) {
            Destroy(gameObject);
        }
    }

    void Start() {
        // Cache references
        skillCaster = FindFirstObjectByType<SkillCaster>();
        player = FindFirstObjectByType<PlayerController>();
        
        if (skillCaster == null) {
            Debug.LogWarning("[SkillSynergyManager] SkillCaster not found!");
        }
        if (player == null) {
            Debug.LogWarning("[SkillSynergyManager] PlayerController not found!");
        }
        
        InitializeDefaultSynergies();
        
        // Subscribe to skill casts
        if (player != null) {
            player.OnSkillCast += OnSkillCast;
        }
    }

    void OnDisable() {
        // Unsubscribe to prevent memory leaks
        if (player != null) {
            player.OnSkillCast -= OnSkillCast;
        }
    }

    void Update() {
        // Clean up old skill casts
        CleanupOldCasts();

        // Update active synergy
        if (synergyActive) {
            activeSynergyTimer -= Time.deltaTime;
            if (activeSynergyTimer <= 0) {
                EndSynergy();
            }
        }
    }

    void InitializeDefaultSynergies() {
        // 1-2 Combo: Fire + Ground = Inferno (damage boost)
        synergies.Add(new SkillSynergy {
            synergyName = "Inferno",
            description = "Fire + Meteor creates burning ground",
            requiredSkillSequence = new int[] { 0, 1 },
            timeWindow = 3f,
            effect = SynergyEffect.DamageBoost,
            damageMultiplier = 1.5f,
            effectDuration = 5f
        });

        // 2-3 Combo: Ground + Projectile = Shattered Earth (empowered projectile)
        synergies.Add(new SkillSynergy {
            synergyName = "Shattered Earth",
            description = "Meteor + Fireball creates explosive projectiles",
            requiredSkillSequence = new int[] { 1, 2 },
            timeWindow = 3f,
            effect = SynergyEffect.EmpowerNext,
            damageMultiplier = 2f,
            effectDuration = 3f
        });

        // 3-4 Combo: Projectile + Shield = Reflecting Shield (damage reduction + reflect)
        synergies.Add(new SkillSynergy {
            synergyName = "Reflecting Shield",
            description = "Fireball + Shield reflects damage",
            requiredSkillSequence = new int[] { 2, 3 },
            timeWindow = 2f,
            effect = SynergyEffect.DamageReduction,
            damageMultiplier = 0.5f,
            effectDuration = 4f
        });

        // 1-2-3 Combo: Full offensive combo
        synergies.Add(new SkillSynergy {
            synergyName = "Elemental Overload",
            description = "All three attack skills in sequence",
            requiredSkillSequence = new int[] { 0, 1, 2 },
            timeWindow = 4f,
            effect = SynergyEffect.ChainLightning,
            damageMultiplier = 1.3f,
            effectDuration = 6f
        });

        // 1-2-3-4 Combo: Ultimate combo
        synergies.Add(new SkillSynergy {
            synergyName = "Avatar of Power",
            description = "All skills in sequence - ULTIMATE POWER",
            requiredSkillSequence = new int[] { 0, 1, 2, 3 },
            timeWindow = 5f,
            effect = SynergyEffect.InfiniteMana,
            damageMultiplier = 2f,
            effectDuration = 3f
        });
    }

    void OnSkillCast(int skillIndex) {
        // Limit list size to prevent unbounded growth
        const int MAX_HISTORY = 50;
        if (recentSkillCasts.Count >= MAX_HISTORY) {
            recentSkillCasts.RemoveAt(0);
            recentSkillTimes.RemoveAt(0);
        }
        
        // Record the cast
        recentSkillCasts.Add(skillIndex);
        recentSkillTimes.Add(Time.time);

        // Only check for synergies if not already active
        if (!synergyActive) {
            CheckForSynergies();
        }
    }

    void CheckForSynergies() {
        foreach (var synergy in synergies) {
            if (CheckSynergyMatch(synergy)) {
                ActivateSynergy(synergy);
                return; // Only activate one synergy at a time
            }
        }
    }

    bool CheckSynergyMatch(SkillSynergy synergy) {
        int[] sequence = synergy.requiredSkillSequence;
        
        // Need enough casts
        if (recentSkillCasts.Count < sequence.Length) return false;

        // Check if last N casts match the sequence
        int startIndex = recentSkillCasts.Count - sequence.Length;
        
        for (int i = 0; i < sequence.Length; i++) {
            if (recentSkillCasts[startIndex + i] != sequence[i]) {
                return false;
            }
        }

        // Check timing
        float firstCastTime = recentSkillTimes[startIndex];
        float lastCastTime = recentSkillTimes[recentSkillTimes.Count - 1];
        if (lastCastTime - firstCastTime > synergy.timeWindow) {
            return false;
        }

        return true;
    }

    void ActivateSynergy(SkillSynergy synergy) {
        activeSynergy = synergy;
        synergyActive = true;
        activeSynergyTimer = synergy.effectDuration;

        // Clear used casts
        recentSkillCasts.Clear();
        recentSkillTimes.Clear();

        // Apply effect
        ApplySynergyEffect(synergy);

        OnSynergyActivated?.Invoke(synergy);
        ShowSynergyNotification(synergy);
    }

    void ApplySynergyEffect(SkillSynergy synergy) {
        switch (synergy.effect) {
            case SynergyEffect.DamageBoost:
                // Damage boost handled in damage calculation
                break;
            case SynergyEffect.CooldownReset:
                // Reset all cooldowns
                if (skillCaster != null) {
                    skillCaster.ResetAllCooldowns();
                }
                break;
            case SynergyEffect.EmpowerNext:
                // Next skill will check this flag
                break;
            case SynergyEffect.DamageReduction:
                if (player != null && player.stats != null) {
                    // Track stacking bonuses properly
                    if (activeDefenseBonuses == 0) {
                        player.stats.defenseMod += 20; // Flat defense boost
                    }
                    activeDefenseBonuses++;
                }
                break;
            case SynergyEffect.InfiniteMana:
                // Set all cooldowns to 0 for duration
                if (skillCaster != null) {
                    skillCaster.ResetAllCooldowns();
                }
                break;
        }
    }

    void EndSynergy() {
        if (activeSynergy == null) return;

        // Remove effects
        switch (activeSynergy.effect) {
            case SynergyEffect.DamageReduction:
                if (player != null && player.stats != null) {
                    activeDefenseBonuses--;
                    if (activeDefenseBonuses <= 0) {
                        player.stats.defenseMod -= 20;
                        activeDefenseBonuses = 0;
                    }
                }
                break;
        }

        synergyActive = false;
        activeSynergy = null;
    }

    void CleanupOldCasts() {
        float currentTime = Time.time;
        float maxWindow = 5f; // Maximum window to keep

        for (int i = recentSkillTimes.Count - 1; i >= 0; i--) {
            if (currentTime - recentSkillTimes[i] > maxWindow) {
                recentSkillTimes.RemoveAt(i);
                recentSkillCasts.RemoveAt(i);
            }
        }
    }

    void ShowSynergyNotification(SkillSynergy synergy) {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification(
                $"SYNERGY: {synergy.synergyName}!\n{synergy.description}"
            );
        }
    }

    // Public methods for other systems to check synergy state
    public bool IsSynergyActive() => synergyActive;
    public SkillSynergy GetActiveSynergy() => activeSynergy;
    
    public float GetSynergyDamageMultiplier() {
        if (!synergyActive || activeSynergy == null) return 1f;
        return activeSynergy.damageMultiplier;
    }

    public bool ShouldChainLightning() {
        return synergyActive && activeSynergy != null && activeSynergy.effect == SynergyEffect.ChainLightning;
    }

    public bool IsNextSkillEmpowered() {
        return synergyActive && activeSynergy != null && activeSynergy.effect == SynergyEffect.EmpowerNext;
    }
}

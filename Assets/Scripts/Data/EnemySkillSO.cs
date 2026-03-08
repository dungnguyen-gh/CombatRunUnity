using UnityEngine;

/// <summary>
/// Skill types available for enemy AI.
/// </summary>
public enum EnemySkillType {
    MeleeAttack,      // Close range physical attack
    RangedProjectile, // Fires a projectile
    DashAttack,       // Dashes toward player then attacks
    AOEAttack,        // Area of effect around enemy
    Summon,           // Summons minions
    SelfHeal,         // Restores own health
    Buff,             // Temporary stat boost
    Retreat,          // Move away from player
    ChargeAttack      // Wind up then powerful attack
}

/// <summary>
/// When the enemy is allowed to use this skill.
/// </summary>
public enum SkillUsageCondition {
    Anytime,          // No restrictions
    HealthAbove50,    // Only when health > 50%
    HealthBelow50,    // Only when health < 50%
    HealthBelow25,    // Only when health < 25%
    PlayerClose,      // Only when player is within close range
    PlayerFar,        // Only when player is beyond attack range
    OnCooldownOnly    // Use only when other skills are on cooldown
}

/// <summary>
/// ScriptableObject for enemy skills.
/// Simpler than player skills but supports various AI behaviors.
/// </summary>
[CreateAssetMenu(fileName = "EnemySkill_", menuName = "ARPG/Enemy Skill")]
public class EnemySkillSO : ScriptableObject {
    
    [Header("Basic Info")]
    public string skillId;
    public string skillName;
    public EnemySkillType skillType = EnemySkillType.MeleeAttack;
    
    [Header("Damage & Effects")]
    public int damage = 10;
    public float damageMultiplier = 1f;
    public float knockbackForce = 0f;
    public bool applyStatusEffect = false;
    public StatusType statusType = StatusType.None;
    public float statusDuration = 3f;
    
    [Header("Cooldown & Timing")]
    public float cooldownTime = 3f;
    public float castTime = 0f;           // Wind-up time before skill executes
    public float recoveryTime = 0.5f;     // Time after skill before enemy can act
    
    [Header("Range & Area")]
    public float minRange = 0f;           // Minimum distance to use skill
    public float maxRange = 2f;           // Maximum distance to use skill
    public float aoeRadius = 0f;          // For AOE skills
    
    [Header("Movement")]
    public float dashDistance = 0f;       // For dash attacks
    public float dashSpeed = 10f;
    public float retreatDistance = 0f;    // How far to retreat
    public float retreatDuration = 2f;    // How long to retreat for (Retreat skill type)
    
    [Header("Summoning")]
    public GameObject summonPrefab;       // For summon skills
    public int summonCount = 1;
    public float summonDuration = 10f;
    
    [Header("Visual & Audio")]
    public GameObject castEffectPrefab;   // Effect during wind-up
    public GameObject effectPrefab;       // Effect on impact
    public GameObject projectilePrefab;   // For ranged skills
    public AudioClip castSound;
    public AudioClip impactSound;
    
    [Header("AI Behavior")]
    public SkillUsageCondition usageCondition = SkillUsageCondition.Anytime;
    public float priority = 1f;           // Higher priority = more likely to be used
    public bool canBeInterrupted = true;  // Can be interrupted by player attacks
    public bool facePlayerDuringCast = true;
    
    [Header("Buff")]
    public float buffDuration = 5f;       // Duration of buff effects (Buff skill type)
    public float buffValue = 0.5f;        // Buff multiplier (0.5 = +50% to stat)
    
    [Header("Animation")]
    public string animationTrigger = "Attack";
    public int spumAnimationIndex = 2;    // Default to attack animation
    
    /// <summary>
    /// Calculates final damage based on enemy base damage.
    /// </summary>
    public int CalculateDamage(int baseDamage) {
        return Mathf.RoundToInt((baseDamage + damage) * damageMultiplier);
    }
    
    /// <summary>
    /// Checks if this skill can be used based on current conditions.
    /// </summary>
    public bool CanUse(float healthPercent, float distanceToPlayer, bool otherSkillsOnCooldown) {
        switch (usageCondition) {
            case SkillUsageCondition.Anytime:
                return true;
            case SkillUsageCondition.HealthAbove50:
                return healthPercent > 0.5f;
            case SkillUsageCondition.HealthBelow50:
                return healthPercent < 0.5f;
            case SkillUsageCondition.HealthBelow25:
                return healthPercent < 0.25f;
            case SkillUsageCondition.PlayerClose:
                return distanceToPlayer <= maxRange;
            case SkillUsageCondition.PlayerFar:
                return distanceToPlayer > maxRange;
            case SkillUsageCondition.OnCooldownOnly:
                return otherSkillsOnCooldown;
            default:
                return true;
        }
    }
    
    /// <summary>
    /// Validates the skill configuration.
    /// </summary>
    public bool Validate(out string error) {
        error = "";
        
        if (string.IsNullOrEmpty(skillId)) {
            error = "Skill ID is empty!";
            return false;
        }
        
        if (cooldownTime <= 0) {
            error = "Cooldown must be greater than 0!";
            return false;
        }
        
        if (skillType == EnemySkillType.RangedProjectile && projectilePrefab == null) {
            error = "Ranged projectile skill needs a projectile prefab!";
            return false;
        }
        
        if (skillType == EnemySkillType.Summon && summonPrefab == null) {
            error = "Summon skill needs a summon prefab!";
            return false;
        }
        
        if (maxRange < minRange) {
            error = "Max range must be greater than min range!";
            return false;
        }
        
        return true;
    }
}

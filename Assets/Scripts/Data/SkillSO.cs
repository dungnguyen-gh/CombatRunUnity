using UnityEngine;

/// <summary>
/// Enhanced Skill ScriptableObject with dynamic configuration.
/// Supports multiple skill types with advanced parameters.
/// </summary>
[CreateAssetMenu(fileName="Skill_", menuName="ARPG/Skill")]
public class SkillSO : ScriptableObject {
    [Header("Basic Info")]
    public string skillId;
    public string skillName;
    public string description;
    public Sprite icon;
    public int skillSlot; // 0-3 for keys 1-4
    public SkillRarity rarity = SkillRarity.Common;
    
    [Header("Targeting & Casting")]
    public SkillType skillType = SkillType.CircleAOE;
    public SkillTargeting targeting = SkillTargeting.Directional;
    public float castTime = 0f; // 0 = instant, >0 = charging/casting time
    public bool canMoveWhileCasting = true;
    public bool requiresLineOfSight = false;
    
    [Header("Cooldown & Cost")]
    public float cooldownTime = 5f;
    public int manaCost = 0;
    public int healthCost = 0; // For blood magic style skills
    
    [Header("Damage & Effects")]
    public float damageMultiplier = 1f;
    public int flatDamageBonus = 0;
    public float critChanceBonus = 0f;
    public float critDamageMultiplier = 1f;
    
    [Header("Area & Range")]
    public float range = 3f;
    public float radius = 2f;
    public float coneAngle = 360f; // For cone-shaped AOEs (360 = full circle)
    
    [Header("Duration & Ticks")]
    public float duration = 0f; // For DoT, buffs, shields
    public float tickRate = 1f; // Damage/effect interval
    public int maxStacks = 1; // For stacking debuffs/buffs
    
    [Header("Movement (Dash/Teleport)")]
    public float dashDistance = 0f;
    public float dashSpeed = 20f;
    public bool dashInvulnerable = false;
    public bool leaveTrail = false;
    
    [Header("Summon Settings")]
    public GameObject summonPrefab;
    public int summonCount = 1;
    public float summonDuration = 10f;
    public bool summonFollowPlayer = true;
    
    [Header("Chain Settings")]
    public int chainBounces = 3;
    public float chainRange = 5f;
    public float chainDamageFalloff = 0.8f; // Each bounce does 80% damage
    
    [Header("Visual & Audio")]
    public GameObject castEffectPrefab; // Effect at cast start
    public GameObject effectPrefab; // Effect at impact/location
    public GameObject projectilePrefab; // For projectile type
    public GameObject persistentEffectPrefab; // For lasting effects (shields, buffs)
    public AudioClip castSound;
    public AudioClip impactSound;
    public AudioClip loopSound; // For channeling/beam skills
    
    [Header("Animation")]
    public string animationTrigger = "Skill"; // Animator trigger name
    public int spumAnimationIndex = 0; // Index in SPUM animation list
    public float animationSpeedMultiplier = 1f;
    
    [Header("Screen Effects")]
    public bool useScreenShake = false;
    public float screenShakeIntensity = 0.3f;
    public float screenShakeDuration = 0.2f;
    public bool useSlowMotion = false;
    public float slowMotionScale = 0.5f;
    public float slowMotionDuration = 0.3f;
    
    [Header("Status Effects")]
    public bool applyBurn = false;
    public bool applyFreeze = false;
    public bool applyPoison = false;
    public bool applyShock = false;
    public bool applyStun = false;
    public float statusDuration = 3f;
    
    [Header("Advanced")]
    public bool pierceEnemies = false; // Projectile goes through enemies
    public bool explodeOnImpact = false;
    public float explosionRadius = 2f;
    public bool homing = false; // Projectile tracks enemies
    public float homingStrength = 5f;
    
    /// <summary>
    /// Gets the display color based on rarity.
    /// </summary>
    public Color GetRarityColor() {
        switch (rarity) {
            case SkillRarity.Common: return Color.white;
            case SkillRarity.Uncommon: return Color.green;
            case SkillRarity.Rare: return Color.cyan;
            case SkillRarity.Epic: return Color.magenta;
            case SkillRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Calculates final damage based on base damage.
    /// </summary>
    public int CalculateDamage(int baseDamage) {
        float damage = (baseDamage * damageMultiplier) + flatDamageBonus;
        return Mathf.RoundToInt(damage);
    }
    
    /// <summary>
    /// Validates if skill configuration is correct.
    /// </summary>
    public bool Validate(out string error) {
        error = "";
        
        if (string.IsNullOrEmpty(skillId)) {
            error = "Skill ID is empty!";
            return false;
        }
        
        if (skillType == SkillType.Projectile && projectilePrefab == null) {
            error = "Projectile skill needs projectilePrefab!";
            return false;
        }
        
        if (skillType == SkillType.Summon && summonPrefab == null) {
            error = "Summon skill needs summonPrefab!";
            return false;
        }
        
        if (cooldownTime <= 0) {
            error = "Cooldown must be greater than 0!";
            return false;
        }
        
        return true;
    }
}

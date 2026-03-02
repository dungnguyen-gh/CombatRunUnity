using UnityEngine;

/// <summary>
/// Serializable player stats class with base values, modifiers, and computed final stats.
/// Supports serialization for save/load systems.
/// </summary>
[System.Serializable]
public class PlayerStats {
    [Header("Base Stats")]
    public int baseDamage = 10;
    public int baseDefense = 0;
    public float baseCrit = 0.05f;
    public float baseAttackSpeed = 1f;
    public int baseMaxHP = 100;

    // Current HP (not a base stat)
    public int currentHP = 100;

    // Computed properties
    public int MaxHP => baseMaxHP + maxHPMod;
    public int Damage => Mathf.Max(1, baseDamage + damageMod); // Minimum 1 damage
    public int Defense => Mathf.Max(0, baseDefense + defenseMod); // Minimum 0 defense
    public float Crit => Mathf.Clamp01(baseCrit + critMod);
    
    // Attack speed with minimum to prevent divide-by-zero
    public float AttackSpeed {
        get {
            float speed = baseAttackSpeed + attackSpeedMod;
            return Mathf.Max(0.1f, speed); // Minimum 10% attack speed
        }
    }

    // Modifiers from equipment
    // Note: These are serialized for save/load compatibility
    public int damageMod = 0;
    public int defenseMod = 0;
    public float critMod = 0f;
    public float attackSpeedMod = 0f;
    public int maxHPMod = 0;

    /// <summary>
    /// Resets all equipment modifiers to zero.
    /// </summary>
    public void ResetMods() {
        damageMod = 0;
        defenseMod = 0;
        critMod = 0f;
        attackSpeedMod = 0f;
        maxHPMod = 0;
    }

    /// <summary>
    /// Applies item bonuses to stats.
    /// </summary>
    public void ApplyItem(ItemSO item) {
        if (item == null) return;
        damageMod += item.damageBonus;
        defenseMod += item.defenseBonus;
        critMod += item.critBonus;
        attackSpeedMod += item.attackSpeedBonus;
        maxHPMod += item.maxHPBonus;
    }

    /// <summary>
    /// Removes item bonuses from stats.
    /// </summary>
    public void RemoveItem(ItemSO item) {
        if (item == null) return;
        damageMod -= item.damageBonus;
        defenseMod -= item.defenseBonus;
        critMod -= item.critBonus;
        attackSpeedMod -= item.attackSpeedBonus;
        maxHPMod -= item.maxHPBonus;
    }

    /// <summary>
    /// Applies damage to HP, considering defense.
    /// </summary>
    public void TakeDamage(int damage) {
        int damageTaken = Mathf.Max(1, damage - Defense);
        currentHP = Mathf.Max(0, currentHP - damageTaken);
    }

    /// <summary>
    /// Heals HP up to max HP.
    /// </summary>
    public void Heal(int amount) {
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    /// <summary>
    /// Determines if an attack should be a critical hit.
    /// Uses Unity's Random.value - consider using a seedable RNG for deterministic gameplay.
    /// </summary>
    public bool IsCrit() {
        return Random.value < Crit;
    }

    /// <summary>
    /// Calculates critical hit damage.
    /// </summary>
    public int GetCritDamage(int baseDamage) {
        return Mathf.RoundToInt(baseDamage * 1.5f); // 50% crit damage bonus
    }

    /// <summary>
    /// Fully restores HP to max.
    /// </summary>
    public void FullHeal() {
        currentHP = MaxHP;
    }

    /// <summary>
    /// Gets current HP as a percentage (0-1).
    /// </summary>
    public float GetHealthPercent() {
        return (float)currentHP / MaxHP;
    }
}

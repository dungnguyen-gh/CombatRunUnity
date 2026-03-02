using UnityEngine;
using System.Collections;

public enum StatusType { None, Burn, Freeze, Poison, Shock, Bleed }

[System.Serializable]
public class StatusEffectData {
    public StatusType type;
    public float duration;
    public float tickRate = 1f; // Damage tick interval
    public int damagePerTick;
    public float slowAmount = 0f; // 0-1 for movement slow
    public float damageMultiplier = 1f; // Incoming damage modifier
    public Color effectColor = Color.white;
    public GameObject visualEffect;
}

/// <summary>
/// Manages status effects on enemies (Burn, Freeze, Poison, Shock, Bleed).
/// Includes elemental reactions and priority-based status overriding.
/// </summary>
public class StatusEffect : MonoBehaviour {
    [Header("Current Effects")]
    public StatusType currentStatus = StatusType.None;
    public float remainingDuration;
    
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public GameObject burnEffect;
    public GameObject freezeEffect;
    public GameObject poisonEffect;
    public GameObject shockEffect;
    public GameObject bleedEffect;

    private Enemy enemy;
    private float tickTimer;
    private StatusEffectData currentData;
    private GameObject activeEffect;
    private Color originalColor;
    private float originalSpeed = -1f; // Store original speed to avoid floating point errors
    private bool isStunned = false;

    void Awake() {
        enemy = GetComponent<Enemy>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    void Update() {
        if (currentStatus == StatusType.None) return;

        remainingDuration -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        // Apply tick damage
        if (tickTimer <= 0 && currentData.damagePerTick > 0) {
            // Guard: enemy may become null during tick damage
            if (enemy == null) {
                ClearStatus();
                return;
            }
            ApplyTickDamage();
            tickTimer = currentData.tickRate;
        }

        // End effect
        if (remainingDuration <= 0) {
            ClearStatus();
        }
    }

    void OnDisable() {
        // Clean up any active effects when disabled/destroyed
        ClearStatus();
    }

    /// <summary>
    /// Applies a status effect to the enemy. May override existing status based on priority.
    /// </summary>
    public void ApplyStatus(StatusEffectData data) {
        if (data == null) return;
        
        // Some statuses can override others
        if (currentStatus != StatusType.None && currentStatus != data.type) {
            // Priority: Freeze > Shock > Burn > Poison > Bleed
            if (!CanOverride(data.type)) return;
        }

        // Clear existing status before applying new one
        ClearStatus();

        // Refresh or apply new
        currentStatus = data.type;
        currentData = data;
        remainingDuration = data.duration;
        tickTimer = data.tickRate;

        // Apply visual
        ApplyVisualEffect();

        // Apply immediate effects
        ApplyImmediateEffects();
    }

    bool CanOverride(StatusType newType) {
        int GetPriority(StatusType type) => type switch {
            StatusType.Freeze => 5,
            StatusType.Shock => 4,
            StatusType.Burn => 3,
            StatusType.Poison => 2,
            StatusType.Bleed => 1,
            _ => 0
        };

        return GetPriority(newType) >= GetPriority(currentStatus);
    }

    void ApplyImmediateEffects() {
        if (enemy == null) return;

        switch (currentStatus) {
            case StatusType.Freeze:
                // Store original speed before modifying
                if (originalSpeed < 0) originalSpeed = enemy.moveSpeed;
                // Clamp slow amount to prevent negative or zero multiplier
                float slowMultiplier = Mathf.Clamp01(1f - currentData.slowAmount);
                if (slowMultiplier <= 0.01f) slowMultiplier = 0.01f; // Minimum 1% speed
                enemy.moveSpeed = originalSpeed * slowMultiplier;
                break;
            case StatusType.Shock:
                // Stun briefly
                StartCoroutine(ShockStun());
                break;
        }
    }

    void ApplyTickDamage() {
        if (enemy == null) return;
        enemy.TakeDamage(currentData.damagePerTick);
        
        // Show tick damage number
        if (DamageNumberManager.Instance != null) {
            DamageNumberManager.Instance.ShowDamage(
                currentData.damagePerTick, 
                transform.position + Vector3.up * 0.5f
            );
        }
    }

    void ApplyVisualEffect() {
        // Clear old effect
        if (activeEffect != null) {
            Destroy(activeEffect);
        }

        // Change sprite color
        if (spriteRenderer != null) {
            spriteRenderer.color = currentData.effectColor;
        }

        // Spawn particle effect
        GameObject effectPrefab = currentStatus switch {
            StatusType.Burn => burnEffect,
            StatusType.Freeze => freezeEffect,
            StatusType.Poison => poisonEffect,
            StatusType.Shock => shockEffect,
            StatusType.Bleed => bleedEffect,
            _ => null
        };

        if (effectPrefab != null) {
            activeEffect = Instantiate(effectPrefab, transform);
        }
    }

    /// <summary>
    /// Clears the current status effect and restores enemy state.
    /// </summary>
    public void ClearStatus() {
        if (enemy != null && currentStatus == StatusType.Freeze) {
            // Restore original speed
            if (originalSpeed >= 0) {
                enemy.moveSpeed = originalSpeed;
            }
        }

        currentStatus = StatusType.None;
        currentData = null;
        originalSpeed = -1f;

        if (spriteRenderer != null) {
            spriteRenderer.color = originalColor;
        }

        if (activeEffect != null) {
            Destroy(activeEffect);
            activeEffect = null;
        }
    }

    IEnumerator ShockStun() {
        if (enemy == null || isStunned) yield break;
        
        isStunned = true;
        // FIX: Variable shadowing - rename local variable to avoid shadowing class field
        float stunnedOriginalSpeed = enemy.moveSpeed;
        enemy.moveSpeed = 0;
        
        yield return new WaitForSeconds(0.5f);
        
        // Only restore speed if still stunned (not overridden by another status)
        if (enemy != null && isStunned && currentStatus == StatusType.Shock) {
            enemy.moveSpeed = stunnedOriginalSpeed;
        }
        isStunned = false;
    }

    // Elemental reaction: Burn + Poison = Explosion
    public bool TryElementalReaction(StatusType newStatus) {
        if (currentStatus == StatusType.Burn && newStatus == StatusType.Poison) {
            // Explosion reaction
            ExplosionReaction();
            return true;
        }
        if (currentStatus == StatusType.Freeze && newStatus == StatusType.Shock) {
            // Shatter reaction - double damage
            ShatterReaction();
            return true;
        }
        return false;
    }

    void ExplosionReaction() {
        // AOE damage around enemy
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f);
        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                var otherEnemy = hit.GetComponent<Enemy>();
                if (otherEnemy != null && otherEnemy != enemy) {
                    otherEnemy.TakeDamage(50);
                }
            }
        }
        
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification("EXPLOSION!");
        }
        ClearStatus();
    }

    void ShatterReaction() {
        if (enemy != null) {
            // Fixed: Removed redundant GetComponent call
            enemy.TakeDamage(enemy.maxHealth / 4); // 25% max HP damage
        }
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification("SHATTER!");
        }
        ClearStatus();
    }

    void OnDrawGizmosSelected() {
        // Show explosion radius for Burn+Poison reaction
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}

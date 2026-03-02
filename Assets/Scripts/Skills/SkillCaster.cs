using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Casts equipped skills with cooldown management.
/// Handles different skill types: CircleAOE, GroundAOE, Projectile, and Shield.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SkillCaster : MonoBehaviour {
    [Header("Skills")]
    public SkillSO[] skills = new SkillSO[4]; // 4 skill slots
    
    [Header("References")]
    public Transform castPoint;
    public LayerMask enemyLayer;
    public PlayerController player;

    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject shieldEffectPrefab;

    // Cooldown tracking
    private float[] cooldownTimers = new float[4];
    private bool[] isShieldActive = new bool[4];
    private List<GameObject> activeEffects = new List<GameObject>();
    private Camera mainCamera;

    void Awake() {
        if (player == null) player = GetComponent<PlayerController>();
        if (castPoint == null) castPoint = transform;
        
        // Cache main camera reference
        mainCamera = Camera.main;
        if (mainCamera == null) {
            Debug.LogWarning("[SkillCaster] No main camera found!");
        }
        
        // Ensure arrays match skill array length
        if (cooldownTimers.Length != skills.Length) {
            System.Array.Resize(ref cooldownTimers, skills.Length);
        }
        if (isShieldActive.Length != skills.Length) {
            System.Array.Resize(ref isShieldActive, skills.Length);
        }
    }

    void OnDisable() {
        // Clean up all active effects when disabled/destroyed
        CleanupActiveEffects();
    }

    /// <summary>
    /// Cleans up all active effects and resets shield state.
    /// </summary>
    void CleanupActiveEffects() {
        foreach (var effect in activeEffects) {
            if (effect != null) Destroy(effect);
        }
        activeEffects.Clear();
        
        // Reset player shield state
        if (player != null) {
            player.SetShieldActive(false);
        }
        
        // Reset shield flags
        for (int i = 0; i < isShieldActive.Length; i++) {
            isShieldActive[i] = false;
        }
    }

    void Update() {
        // Update cooldown timers
        for (int i = 0; i < cooldownTimers.Length; i++) {
            if (cooldownTimers[i] > 0) {
                cooldownTimers[i] -= Time.deltaTime;
            }
        }
    }

    public bool CanCastSkill(int index) {
        if (index < 0 || index >= skills.Length) return false;
        if (skills[index] == null) return false;
        return cooldownTimers[index] <= 0;
    }

    public float GetCooldownRemaining(int index) {
        if (index < 0 || index >= cooldownTimers.Length) return 0;
        return Mathf.Max(0, cooldownTimers[index]);
    }

    public float GetCooldownPercent(int index) {
        if (index < 0 || index >= skills.Length) return 1f;
        if (skills[index] == null) return 1f;
        return Mathf.Clamp01(1f - (cooldownTimers[index] / skills[index].cooldownTime));
    }

    /// <summary>
    /// Attempts to cast the skill at the specified index.
    /// </summary>
    public bool CastSkill(int index) {
        if (!CanCastSkill(index)) return false;
        
        SkillSO skill = skills[index];
        cooldownTimers[index] = skill.cooldownTime;

        // Cast based on skill type
        switch (skill.skillType) {
            case SkillType.CircleAOE:
                CastCircleAOE(skill);
                break;
            case SkillType.GroundAOE:
                CastGroundAOE(skill);
                break;
            case SkillType.Projectile:
                CastProjectile(skill);
                break;
            case SkillType.Shield:
                StartCoroutine(CastShield(skill, index));
                break;
            default:
                Debug.LogError($"[SkillCaster] Unhandled skill type: {skill.skillType}");
                return false;
        }

        // Play sound
        if (skill.castSound != null) {
            AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
        }

        return true;
    }

    void CastCircleAOE(SkillSO skill) {
        // Wide circle attack around player
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skill.radius, enemyLayer);
        
        ShowEffect(skill.effectPrefab, transform.position, skill.radius);
        
        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                ApplyDamage(hit.gameObject, skill.damageMultiplier);
            }
        }
    }

    void CastGroundAOE(SkillSO skill) {
        // AOE at mouse position or in front of player
        Vector3 targetPos = GetMouseWorldPosition();
        if (targetPos == Vector3.zero) {
            // Use forward direction if no mouse
            targetPos = transform.position + (Vector3)GetFacingDirection() * skill.range;
        }

        ShowEffect(skill.effectPrefab, targetPos, skill.radius);

        // Delay for ground effect
        StartCoroutine(DelayedAOE(targetPos, skill));
    }

    IEnumerator DelayedAOE(Vector3 position, SkillSO skill) {
        yield return new WaitForSeconds(0.3f);
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, skill.radius, enemyLayer);
        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                ApplyDamage(hit.gameObject, skill.damageMultiplier);
            }
        }
    }

    void CastProjectile(SkillSO skill) {
        if (projectilePrefab == null) return;
        
        // Validate player and stats
        if (player == null || player.stats == null) {
            Debug.LogError("[SkillCaster] Player or stats is null!");
            return;
        }

        Vector2 direction = GetFacingDirection();
        if (direction == Vector2.zero) direction = Vector2.right;

        GameObject proj = Instantiate(projectilePrefab, castPoint.position, Quaternion.identity);
        Projectile projectile = proj.GetComponent<Projectile>();
        
        // Guard: Ensure Projectile component exists
        if (projectile == null) {
            Debug.LogError("[SkillCaster] Projectile prefab missing Projectile component!");
            Destroy(proj);
            return;
        }
        
        projectile.Initialize(direction, skill.range, skill.damageMultiplier * player.stats.Damage, enemyLayer);
    }

    IEnumerator CastShield(SkillSO skill, int index) {
        // Temporary shield - reduce damage taken
        player.SetShieldActive(true);
        isShieldActive[index] = true;

        GameObject effect = null;
        if (shieldEffectPrefab != null) {
            effect = Instantiate(shieldEffectPrefab, transform);
            activeEffects.Add(effect);
        }

        yield return new WaitForSeconds(skill.duration > 0 ? skill.duration : 3f);

        player.SetShieldActive(false);
        isShieldActive[index] = false;
        
        if (effect != null) {
            activeEffects.Remove(effect);
            Destroy(effect);
        }
    }

    void ShowEffect(GameObject prefab, Vector3 position, float scale) {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * scale;
        Destroy(effect, 1f);
    }

    void ApplyDamage(GameObject target, float damageMultiplier) {
        var enemy = target.GetComponent<Enemy>();
        if (enemy != null) {
            // Validate player and stats
            if (player == null || player.stats == null) {
                Debug.LogError("[SkillCaster] Player or stats is null in ApplyDamage!");
                return;
            }
            
            int damage = Mathf.RoundToInt(player.stats.Damage * damageMultiplier);
            if (player.stats.IsCrit()) {
                damage = player.stats.GetCritDamage(damage);
            }
            enemy.TakeDamage(damage);
        }
    }

    Vector2 GetFacingDirection() {
        // Get direction from player input (already using new Input System)
        Vector2 input = player != null ? player.GetMoveInput() : Vector2.zero;
        if (input.magnitude > 0.1f) return input.normalized;
        
        // Use mouse position relative to player
        if (mainCamera == null) {
            Debug.LogWarning("[SkillCaster] No main camera found for facing direction!");
            return Vector2.right; // Default fallback
        }
        
        // New Input System mouse position
        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
        return ((Vector2)(mousePos - transform.position)).normalized;
    }

    Vector3 GetMouseWorldPosition() {
        if (mainCamera == null) return Vector3.zero;
        
        // New Input System mouse position
        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector3 mousePos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    void OnDrawGizmosSelected() {
        // Visualize skill ranges
        Gizmos.color = Color.yellow;
        if (skills != null && skills.Length > 0 && skills[0] != null) {
            Gizmos.DrawWireSphere(transform.position, skills[0].radius);
        }
    }
    
    /// <summary>
    /// Resets all skill cooldowns. Used by synergy effects.
    /// </summary>
    public void ResetAllCooldowns() {
        for (int i = 0; i < cooldownTimers.Length; i++) {
            cooldownTimers[i] = 0f;
        }
    }
}

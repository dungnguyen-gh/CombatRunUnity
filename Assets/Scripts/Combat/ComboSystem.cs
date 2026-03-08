using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Manages melee combo counting with time windows, damage/attack speed bonuses,
/// and a powerful finisher attack at max combo.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class ComboSystem : MonoBehaviour {
    [Header("Combo Settings")]
    public float comboWindow = 2f; // Time between attacks to maintain combo
    public int maxCombo = 5;
    public float damageBonusPerCombo = 0.2f; // 20% more damage per combo level
    public float attackSpeedBonusPerCombo = 0.1f;

    [Header("Visual")]
    public GameObject comboEffectPrefab;
    public Vector3 comboTextOffset = new Vector3(0, 1.5f, 0);

    [Header("Finisher")]
    public int finisherComboRequirement = 5;
    public float finisherDamageMultiplier = 3f;
    public float finisherKnockback = 5f;
    public GameObject finisherEffect;

    // State
    private int currentCombo = 0;
    private float comboTimer = 0f;
    private bool canFinisher = false;
    private PlayerController player;

    // Events
    public System.Action<int> OnComboChanged;
    public System.Action OnComboReset;
    public System.Action OnFinisherReady;
    public System.Action OnFinisherUsed;

    void Awake() {
        player = GetComponent<PlayerController>();
        if (player == null) {
            Debug.LogError("[ComboSystem] PlayerController not found on same GameObject!", this);
            enabled = false;
        }
    }

    void OnDestroy() {
        // Clear events to prevent memory leaks
        OnComboChanged = null;
        OnComboReset = null;
        OnFinisherReady = null;
        OnFinisherUsed = null;
    }

    void Update() {
        if (currentCombo > 0) {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0) {
                ResetCombo();
            }
        }
    }

    /// <summary>
    /// Registers a hit in the combo system.
    /// </summary>
    /// <param name="isFinisherInput">If true, attempts to execute finisher if available</param>
    public void RegisterHit(bool isFinisherInput = false) {
        if (isFinisherInput && canFinisher) {
            PerformFinisher();
            return;
        }

        currentCombo++;
        comboTimer = comboWindow;

        // Cap at max combo
        if (currentCombo > maxCombo) {
            currentCombo = maxCombo;
        }

        // Check for finisher availability
        if (currentCombo >= finisherComboRequirement && !canFinisher) {
            canFinisher = true;
            OnFinisherReady?.Invoke();
            ShowFinisherReadyEffect();
        }

        OnComboChanged?.Invoke(currentCombo);
        ShowComboEffect();
    }

    void PerformFinisher() {
        // Validate player reference
        if (player == null) {
            Debug.LogError("[ComboSystem] Player reference is null! Cannot perform finisher.");
            ResetCombo();
            return;
        }

        // Validate stats reference
        if (player.stats == null) {
            Debug.LogError("[ComboSystem] Player stats is null!");
            ResetCombo();
            return;
        }

        // Deal massive damage in AOE
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, 
            player.meleeRange * 1.5f, 
            player.enemyLayer
        );

        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null) {
                    int damage = Mathf.RoundToInt(
                        player.stats.Damage * finisherDamageMultiplier * (1 + currentCombo * damageBonusPerCombo)
                    );
                    enemy.TakeDamage(damage);
                    
                    // Apply knockback
                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null) {
                        Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                        enemyRb.AddForce(knockbackDir * finisherKnockback, ForceMode2D.Impulse);
                    }
                }
            }
        }

        // Effects
        if (finisherEffect != null) {
            Instantiate(finisherEffect, transform.position, Quaternion.identity);
        }

        OnFinisherUsed?.Invoke();
        ResetCombo();
    }

    public void ResetCombo() {
        if (currentCombo > 0) {
            currentCombo = 0;
            canFinisher = false;
            OnComboReset?.Invoke();
        }
    }

    public float GetCurrentDamageBonus() {
        return 1f + (currentCombo * damageBonusPerCombo);
    }

    public float GetCurrentAttackSpeedBonus() {
        return currentCombo * attackSpeedBonusPerCombo;
    }

    void ShowComboEffect() {
        if (comboEffectPrefab != null) {
            GameObject effect = Instantiate(comboEffectPrefab, transform.position + comboTextOffset, Quaternion.identity);
            
            // FIX: Add null check for GetComponentInChildren
            var text = effect.GetComponentInChildren<TMPro.TextMeshPro>();
            if (text != null) {
                text.text = $"x{currentCombo}";
                text.color = GetComboColor();
            }
            Destroy(effect, 1f);
        }

        // Show notification at certain milestones
        if (currentCombo == 3 || currentCombo == 5) {
            if (UIManager.Instance != null) {
                UIManager.Instance.ShowNotification($"Combo x{currentCombo}!");
            }
        }
    }

    void ShowFinisherReadyEffect() {
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification("FINISHER READY! (Hold Attack)");
        }
    }

    Color GetComboColor() {
        return currentCombo switch {
            1 => Color.white,
            2 => Color.yellow,
            3 => new Color(1f, 0.5f, 0f), // Orange
            4 => Color.red,
            5 => new Color(0.8f, 0f, 1f), // Purple
            _ => Color.white
        };
    }

    public int GetCurrentCombo() => currentCombo;
    public bool IsFinisherReady() => canFinisher;
}

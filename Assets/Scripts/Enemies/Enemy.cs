using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum EnemyState { Idle, Chase, Attack, Dead }

/// <summary>
/// Enhanced Enemy class with improved pooling support, skill system integration,
/// and auto-facing player on spawn.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StatusEffect))]
public class Enemy : MonoBehaviour {
    [Header("Stats")]
    public int maxHealth = 30;
    public int damage = 5;
    public float moveSpeed = 2f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;
    public int goldReward = 5;
    public float itemDropChance = 0.3f;

    [Header("AI")]
    public float detectionRange = 8f;
    public float stopDistance = 0.5f;
    public bool patrol = false;
    public Vector2[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Components - Legacy (for non-SPUM)")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    
    [Header("Sprite Facing")]
    [Tooltip("If true, sprite faces RIGHT by default (flipX=false). If false, sprite faces LEFT by default.")]
    public bool spriteFacesRight = true;

    [Header("SPUM Integration")]
    public bool useSPUM = false;
    public SPUM_Prefabs spumPrefabs;
    public int idleAnimationIndex = 0;
    public int moveAnimationIndex = 0;
    public int attackAnimationIndex = 0;
    public int hitAnimationIndex = 0;
    public int deathAnimationIndex = 0;

    [Header("Drops")]
    public GameObject goldPickupPrefab;
    public GameObject[] itemDropPrefabs;

    // State
    private int currentHealth;
    private EnemyState currentState = EnemyState.Idle;
    private Transform player;
    private float attackTimer;
    private bool isDead = false;
    private Vector2 startPosition;
    private Vector2 facingDirection = Vector2.right;

    // Skill System
    private EnemyAI enemyAI;
    private bool hasAI = false;

    // Status Effects
    private StatusEffect statusEffect;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    // Events
    public System.Action<Enemy> OnDeath;
    public static System.Action<Enemy> OnEnemyDeathGlobal;

    void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        // StatusEffect is required by [RequireComponent] attribute
        // It should be auto-added by Unity, but we check defensively
        if (statusEffect == null) {
            statusEffect = GetComponent<StatusEffect>();
            // Silent fallback - don't log warnings to avoid console spam
            // The EnemyPool should have added this during prefab setup
            if (statusEffect == null) {
                statusEffect = gameObject.AddComponent<StatusEffect>();
            }
        }
        
        // Get optional AI component
        enemyAI = GetComponent<EnemyAI>();
        hasAI = enemyAI != null;
        
        // Auto-find SPUM components if using SPUM
        if (useSPUM) {
            if (spumPrefabs == null)
                spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }
    }

    void OnEnable() {
        // Reset state when enabled (called when retrieved from pool)
        ResetFromPool();
    }

    void OnDisable() {
        // Clean up when returning to pool
        CleanupForPool();
    }

    /// <summary>
    /// Reset enemy state for object pooling.
    /// Called when enemy is retrieved from the pool.
    /// </summary>
    public void ResetFromPool() {
        // Find player and face them immediately
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        ResetState();
        
        // Face the player on spawn
        if (player != null) {
            FacePlayer();
        }
        
        // Re-initialize SPUM if needed
        if (useSPUM && spumPrefabs != null) {
            InitializeSPUM();
        }
        
        // Ensure collider is enabled
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        
        // Reset velocity
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // Reset AI if present
        if (hasAI && enemyAI != null) {
            enemyAI.ResetAI();
        }
    }

    /// <summary>
    /// Internal state reset - clears all runtime state.
    /// </summary>
    void ResetState() {
        currentHealth = maxHealth;
        isDead = false;
        currentState = EnemyState.Idle;
        attackTimer = 0f;
        currentPatrolIndex = 0;
        facingDirection = Vector2.right;
        
        // Clear status effects
        if (statusEffect != null) {
            statusEffect.ClearStatus();
        }
        
        // Stop all active coroutines
        StopAllActiveCoroutines();
        activeCoroutines.Clear();
        
        // Reset animation
        if (useSPUM && spumPrefabs != null) {
            PlayIdleAnimation();
        } else if (animator != null) {
            animator.SetBool("IsMoving", false);
            animator.ResetTrigger("Die");
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Attack");
        }
        
        // Reset sprite direction
        UpdateSpriteFacing(Vector2.right);
    }

    /// <summary>
    /// Cleanup when returning to pool - stops coroutines and clears effects.
    /// </summary>
    void CleanupForPool() {
        // Stop all coroutines
        StopAllActiveCoroutines();
        
        // Clear status effects
        if (statusEffect != null) {
            statusEffect.ClearStatus();
        }
        
        // Reset velocity
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // Stop any ongoing animations
        if (animator != null) {
            animator.SetBool("IsMoving", false);
        }
    }

    /// <summary>
    /// Stops all tracked coroutines safely.
    /// </summary>
    void StopAllActiveCoroutines() {
        foreach (var coroutine in activeCoroutines) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
    }

    /// <summary>
    /// Starts a coroutine and tracks it for cleanup.
    /// </summary>
    Coroutine StartTrackedCoroutine(IEnumerator routine) {
        Coroutine coroutine = StartCoroutine(routine);
        activeCoroutines.Add(coroutine);
        return coroutine;
    }



    void InitializeSPUM() {
        if (spumPrefabs == null) return;
        
        // Check if animator is properly configured before initializing
        if (spumPrefabs._anim == null) {
            Debug.LogWarning($"[Enemy] SPUM prefab on {gameObject.name} has no Animator assigned!");
            return;
        }
        
        // Check if runtimeAnimatorController is valid
        if (spumPrefabs._anim.runtimeAnimatorController == null) {
            Debug.LogWarning($"[Enemy] SPUM prefab on {gameObject.name} has no Animator Controller! " +
                           $"Please assign an AnimatorController to the SPUM prefab's animator.");
            return;
        }
        
        // CRITICAL FIX: Check if controller is already an OverrideController
        // If so, we can't call OverrideControllerInit() - it would try to nest override controllers
        if (spumPrefabs._anim.runtimeAnimatorController is AnimatorOverrideController) {
            // Already an override controller - skip initialization
            // The animations should still work
            Debug.Log($"[Enemy] SPUM on {gameObject.name} already has OverrideController - skipping initialization");
        } else {
            // Safe to initialize - it's a base controller
            try {
                spumPrefabs.OverrideControllerInit();
            }
            catch (System.Exception ex) {
                Debug.LogWarning($"[Enemy] SPUM OverrideControllerInit failed on {gameObject.name}: {ex.Message}");
            }
        }
        
        try {
            if (!spumPrefabs.allListsHaveItemsExist()) {
                spumPrefabs.PopulateAnimationLists();
            }
        }
        catch (System.Exception ex) {
            Debug.LogWarning($"[Enemy] SPUM PopulateAnimationLists failed on {gameObject.name}: {ex.Message}");
        }
        
        PlayIdleAnimation();
    }

    // Cached squared distances for efficient comparison
    private float sqrDetectionRange;
    private float sqrAttackRange;
    
    void Start() {
        // Cache squared distances for efficient comparison
        sqrDetectionRange = detectionRange * detectionRange;
        sqrAttackRange = attackRange * attackRange;
        
        // Try to find player, with fallback for spawn order issues
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) {
                // Player might not be spawned yet - will retry in Update when needed
                Debug.LogWarning("[Enemy] Player not found at Start, will retry when needed");
            }
        }

        // Initialize SPUM if using it
        if (useSPUM && spumPrefabs != null) {
            InitializeSPUM();
        }
    }
    
    void Update() {
        if (isDead) return;
        
        // Cache player reference once per frame
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // If AI is present, let it handle state
        if (hasAI && enemyAI != null && enemyAI.enabled) {
            // AI handles its own state, we just update animations
            UpdateAnimationFromAI();
        } else {
            // Legacy state machine
            UpdateState();
        }
        
        // Update animations
        if (useSPUM) {
            UpdateSPUMAnimation();
        } else {
            UpdateLegacyAnimation();
        }
    }

    void FixedUpdate() {
        if (isDead) return;
        
        // Only execute movement if AI is not present or disabled
        if (!hasAI || !enemyAI.enabled) {
            switch (currentState) {
                case EnemyState.Chase:
                    ChasePlayer();
                    break;
                case EnemyState.Idle:
                    if (patrol && patrolPoints.Length > 0) {
                        Patrol();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Updates animations based on AI state.
    /// </summary>
    void UpdateAnimationFromAI() {
        if (enemyAI == null) return;
        
        AIState aiState = enemyAI.GetCurrentState();
        
        // Map AI state to animation
        switch (aiState) {
            case AIState.Idle:
            case AIState.UseSkill: // Keep previous animation during skill cast
                // Keep current
                break;
            case AIState.Chase:
            case AIState.Reposition:
                // Moving
                break;
            case AIState.Attack:
                // Attacking
                break;
            case AIState.Retreat:
                // Moving away
                break;
        }
    }

    void UpdateState() {
        if (player == null) return;

        // Use squared distance for more efficient comparison (avoids sqrt)
        float sqrDistanceToPlayer = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;

        switch (currentState) {
            case EnemyState.Idle:
                if (sqrDistanceToPlayer <= sqrDetectionRange) {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                // 1.5f squared = 2.25f
                if (sqrDistanceToPlayer > sqrDetectionRange * 2.25f) {
                    currentState = EnemyState.Idle;
                } else if (sqrDistanceToPlayer <= sqrAttackRange) {
                    currentState = EnemyState.Attack;
                }
                break;

            case EnemyState.Attack:
                if (sqrDistanceToPlayer > sqrAttackRange) {
                    currentState = EnemyState.Chase;
                } else {
                    TryAttack();
                }
                break;
        }

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    void ChasePlayer() {
        if (player == null) return;
        
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);
        
        if (distance > stopDistance) {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
            
            facingDirection = direction;
            UpdateSpriteFacing(direction);
        }
    }

    void Patrol() {
        if (patrolPoints.Length == 0) return;
        
        Vector2 target = patrolPoints[currentPatrolIndex];
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target);

        if (distance < 0.1f) {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        } else {
            rb.MovePosition(rb.position + direction * moveSpeed * 0.5f * Time.fixedDeltaTime);
            
            facingDirection = direction;
            UpdateSpriteFacing(direction);
        }
    }

    /// <summary>
    /// Updates the sprite facing direction.
    /// </summary>
    void UpdateSpriteFacing(Vector2 direction) {
        if (useSPUM && spumPrefabs != null) {
            UpdateSPUMFacing(direction);
        } else if (spriteRenderer != null) {
            // Calculate flip based on sprite's default facing direction
            // If spriteFacesRight: moving right = no flip, moving left = flip
            // If !spriteFacesRight: moving right = flip, moving left = no flip
            if (direction.x > 0.1f) {
                spriteRenderer.flipX = !spriteFacesRight;
            } else if (direction.x < -0.1f) {
                spriteRenderer.flipX = spriteFacesRight;
            }
        }
    }

    void UpdateSPUMFacing(Vector2 direction) {
        if (spumPrefabs == null) return;
        
        // Use localScale for 2D sprite flipping
        // Supports both right-facing and left-facing default sprites
        Vector3 scale = spumPrefabs.transform.localScale;
        float absScaleX = Mathf.Abs(scale.x);
        
        if (direction.x > 0.1f) {
            // Facing right
            scale.x = spriteFacesRight ? absScaleX : -absScaleX;
        } else if (direction.x < -0.1f) {
            // Facing left
            scale.x = spriteFacesRight ? -absScaleX : absScaleX;
        }
        spumPrefabs.transform.localScale = scale;
    }

    /// <summary>
    /// Faces the player immediately. Called on spawn and when needed.
    /// </summary>
    public void FacePlayer() {
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }
        
        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        FaceDirection(toPlayer);
    }

    /// <summary>
    /// Faces a specific direction.
    /// </summary>
    public void FaceDirection(Vector2 direction) {
        if (direction == Vector2.zero) return;
        
        facingDirection = direction;
        UpdateSpriteFacing(direction);
    }

    void TryAttack() {
        if (attackTimer > 0) return;
        
        attackTimer = attackCooldown;
        PerformAttack();
    }

    void PerformAttack() {
        // Trigger animation
        if (useSPUM && spumPrefabs != null) {
            PlayAttackAnimation();
        } else if (animator != null) {
            animator.SetTrigger("Attack");
        }

        // Deal damage to player
        if (player != null) {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= attackRange) {
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null) {
                    playerController.TakeDamage(damage);
                }
            }
        }
    }

    #region Animation - Legacy
    void UpdateLegacyAnimation() {
        if (animator == null) return;
        
        animator.SetBool("IsMoving", currentState == EnemyState.Chase || 
            (currentState == EnemyState.Idle && patrol));
    }
    #endregion

    #region Animation - SPUM
    void UpdateSPUMAnimation() {
        if (spumPrefabs == null) return;
        
        // If AI is present, use its state for animation
        if (hasAI && enemyAI != null && enemyAI.enabled) {
            AIState aiState = enemyAI.GetCurrentState();
            bool isMoving = aiState == AIState.Chase || aiState == AIState.Reposition || 
                           (aiState == AIState.Retreat) ||
                           (aiState == AIState.Idle && patrol);
            
            if (isMoving) {
                PlayMoveAnimation();
            } else {
                PlayIdleAnimation();
            }
        } else {
            // Legacy behavior
            bool isMoving = currentState == EnemyState.Chase || 
                (currentState == EnemyState.Idle && patrol);
            
            if (isMoving) {
                PlayMoveAnimation();
            } else {
                PlayIdleAnimation();
            }
        }
    }

    void PlayIdleAnimation() {
        if (spumPrefabs == null) return;
        if (IsValidAnimationIndex(PlayerState.IDLE, idleAnimationIndex)) {
            spumPrefabs.PlayAnimation(PlayerState.IDLE, idleAnimationIndex);
        }
    }

    void PlayMoveAnimation() {
        if (spumPrefabs == null) return;
        if (IsValidAnimationIndex(PlayerState.MOVE, moveAnimationIndex)) {
            spumPrefabs.PlayAnimation(PlayerState.MOVE, moveAnimationIndex);
        }
    }

    void PlayAttackAnimation() {
        if (spumPrefabs == null) return;
        if (IsValidAnimationIndex(PlayerState.ATTACK, attackAnimationIndex)) {
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, attackAnimationIndex);
        }
    }

    void PlayHitAnimation() {
        if (spumPrefabs == null) return;
        if (IsValidAnimationIndex(PlayerState.DAMAGED, hitAnimationIndex)) {
            spumPrefabs.PlayAnimation(PlayerState.DAMAGED, hitAnimationIndex);
        }
    }

    void PlayDeathAnimation() {
        if (spumPrefabs == null) return;
        if (IsValidAnimationIndex(PlayerState.DEATH, deathAnimationIndex)) {
            spumPrefabs.PlayAnimation(PlayerState.DEATH, deathAnimationIndex);
        }
    }

    bool IsValidAnimationIndex(PlayerState state, int index) {
        if (spumPrefabs?.StateAnimationPairs == null) return false;
        if (!spumPrefabs.StateAnimationPairs.TryGetValue(state.ToString(), out var anims)) return false;
        return index >= 0 && index < anims.Count;
    }
    #endregion

    public void TakeDamage(int damage) {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // Show damage number
        DamageNumberManager.Instance?.ShowDamage(damage, transform.position);
        
        // Flash red
        StartTrackedCoroutine(DamageFlash());

        // Interrupt skill cast if AI is present
        if (hasAI && enemyAI != null) {
            enemyAI.InterruptSkill();
        }

        if (currentHealth <= 0) {
            Die();
        } else {
            // Hit animation
            if (useSPUM && spumPrefabs != null) {
                PlayHitAnimation();
            } else if (animator != null) {
                animator.SetTrigger("Hit");
            }
        }
    }

    /// <summary>
    /// Heals the enemy. Negative damage also works.
    /// </summary>
    public void Heal(int amount) {
        if (isDead || amount <= 0) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        // Show heal number (negative damage)
        DamageNumberManager.Instance?.ShowDamage(-amount, transform.position);
    }

    IEnumerator DamageFlash() {
        if (useSPUM && spumPrefabs != null) {
            // For SPUM, flash all sprite renderers
            SpriteRenderer[] renderers = spumPrefabs.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0) {
                Color[] originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) {
                    originalColors[i] = renderers[i].color;
                    renderers[i].color = Color.red;
                }
                yield return new WaitForSeconds(0.1f);
                for (int i = 0; i < renderers.Length; i++) {
                    if (renderers[i] != null) renderers[i].color = originalColors[i];
                }
            }
        } else if (spriteRenderer != null) {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }

    void Die() {
        isDead = true;
        currentState = EnemyState.Dead;
        
        if (useSPUM && spumPrefabs != null) {
            PlayDeathAnimation();
        } else if (animator != null) {
            animator.SetTrigger("Die");
        }

        // Disable collision
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Drop rewards
        DropRewards();

        // Invoke events
        OnDeath?.Invoke(this);
        OnEnemyDeathGlobal?.Invoke(this);
        
        // Return to pool after delay
        StartTrackedCoroutine(ReturnToPoolAfterDelay(3f));
    }

    IEnumerator ReturnToPoolAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        
        // Return to pool
        if (EnemyPool.Instance != null) {
            EnemyPool.Instance.ReturnToPool(gameObject);
        } else {
            // Fallback if no pool exists
            Destroy(gameObject);
        }
    }

    void DropRewards() {
        // Drop gold
        if (goldPickupPrefab != null) {
            var gold = Instantiate(goldPickupPrefab, transform.position, Quaternion.identity);
            var pickup = gold.GetComponent<GoldPickup>();
            if (pickup != null) {
                pickup.SetAmount(goldReward);
            } else {
                Debug.LogWarning("[Enemy] GoldPickup prefab missing GoldPickup component");
            }
        }

        // Chance to drop item
        if (Random.value < itemDropChance && itemDropPrefabs != null && itemDropPrefabs.Length > 0) {
            int index = Random.Range(0, itemDropPrefabs.Length);
            Instantiate(itemDropPrefabs[index], transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }

    #region Public API

    /// <summary>
    /// Sets the current enemy state.
    /// </summary>
    public void SetState(EnemyState newState) {
        currentState = newState;
    }

    /// <summary>
    /// Gets the current enemy state.
    /// </summary>
    public EnemyState GetCurrentState() {
        return currentState;
    }

    /// <summary>
    /// Gets the current health.
    /// </summary>
    public int GetCurrentHealth() {
        return currentHealth;
    }

    /// <summary>
    /// Gets health as a percentage (0-1).
    /// </summary>
    public float GetHealthPercent() {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Checks if the enemy is dead.
    /// </summary>
    public bool IsDead {
        get { return isDead; }
    }

    /// <summary>
    /// Gets the maximum health.
    /// </summary>
    public int MaxHealth {
        get { return maxHealth; }
    }

    /// <summary>
    /// Gets the player transform.
    /// </summary>
    public Transform GetPlayer() {
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        return player;
    }

    #endregion

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

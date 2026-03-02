using UnityEngine;
using System.Collections;

public enum EnemyState { Idle, Chase, Attack, Dead }

[RequireComponent(typeof(Rigidbody2D))]
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

    // Events
    public System.Action<Enemy> OnDeath;
    public static System.Action<Enemy> OnEnemyDeathGlobal;

    void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        startPosition = transform.position;

        // Auto-find SPUM components if using SPUM
        if (useSPUM) {
            if (spumPrefabs == null)
                spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }
    }

    void Start() {
        // Try to find player, with fallback for spawn order issues
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) {
            // Player might not be spawned yet - will retry in Update when needed
            Debug.LogWarning("[Enemy] Player not found at Start, will retry when needed");
        }

        // Initialize SPUM if using it
        if (useSPUM && spumPrefabs != null) {
            InitializeSPUM();
        }
    }

    void InitializeSPUM() {
        spumPrefabs.OverrideControllerInit();
        
        if (!spumPrefabs.allListsHaveItemsExist()) {
            spumPrefabs.PopulateAnimationLists();
        }
        
        PlayIdleAnimation();
    }

    void Update() {
        if (isDead) return;
        
        UpdateState();
        
        if (useSPUM) {
            UpdateSPUMAnimation();
        } else {
            UpdateLegacyAnimation();
        }
    }

    void FixedUpdate() {
        if (isDead) return;
        
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

    void UpdateState() {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState) {
            case EnemyState.Idle:
                if (distanceToPlayer <= detectionRange) {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                if (distanceToPlayer > detectionRange * 1.5f) {
                    currentState = EnemyState.Idle;
                } else if (distanceToPlayer <= attackRange) {
                    currentState = EnemyState.Attack;
                }
                break;

            case EnemyState.Attack:
                if (distanceToPlayer > attackRange) {
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
            
            // Update facing direction
            if (useSPUM && spumPrefabs != null) {
                UpdateSPUMFacing(direction);
            } else if (spriteRenderer != null) {
                if (direction.x > 0.1f) spriteRenderer.flipX = false;
                else if (direction.x < -0.1f) spriteRenderer.flipX = true;
            }
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
            
            if (useSPUM && spumPrefabs != null) {
                UpdateSPUMFacing(direction);
            } else if (spriteRenderer != null) {
                if (direction.x > 0.1f) spriteRenderer.flipX = false;
                else if (direction.x < -0.1f) spriteRenderer.flipX = true;
            }
        }
    }

    void UpdateSPUMFacing(Vector2 direction) {
        if (spumPrefabs == null) return;
        
        // Use rotation Y for facing (same as player)
        if (direction.x > 0.1f)
            spumPrefabs.transform.rotation = Quaternion.Euler(0, 180, 0); // Face Left
        else if (direction.x < -0.1f)
            spumPrefabs.transform.rotation = Quaternion.Euler(0, 0, 0);   // Face Right
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
        
        bool isMoving = currentState == EnemyState.Chase || 
            (currentState == EnemyState.Idle && patrol);
        
        if (isMoving) {
            PlayMoveAnimation();
        } else {
            PlayIdleAnimation();
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
        StartCoroutine(DamageFlash());

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

        OnDeath?.Invoke(this);
        OnEnemyDeathGlobal?.Invoke(this);

        // Destroy after animation
        Destroy(gameObject, 1f);
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

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

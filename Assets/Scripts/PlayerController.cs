using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// Main player controller for movement, combat, and stats.
/// Supports both regular single-sprite characters and SPUM multi-part characters.
/// Uses the NEW Input System (Unity Input System package).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SkillCaster))]
public class PlayerController : MonoBehaviour {
    [Header("Input")]
    [Tooltip("Input Action Asset for controls")]
    public InputActionAsset inputActions;
    private InputActionMap gameplayActions;
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction skill1Action;
    private InputAction skill2Action;
    private InputAction skill3Action;
    private InputAction skill4Action;
    private InputAction inventoryAction;
    private InputAction pauseAction;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;
    private bool isMoving = false;

    [Header("Stats")]
    public PlayerStats stats = new PlayerStats();
    public int gold = 0;

    [Header("Combat")]
    public float meleeRange = 1.5f;
    public float meleeCooldown = 0.5f;
    public LayerMask enemyLayer;
    public Transform attackPoint;
    private float meleeCooldownTimer;
    private bool isShieldActive = false;
    private float shieldDamageReduction = 0.5f;

    [Header("Components")]
    [Tooltip("Legacy: Animator for single-sprite characters. Not used for SPUM.")]
    public Animator animator;
    [Tooltip("Legacy: Single sprite renderer for flip and damage flash. Optional - will use flashAllSpriteRenderers if null.")]
    public SpriteRenderer spriteRenderer;
    private SkillCaster skillCaster;
    private ComboSystem comboSystem;
    
    [Header("Damage Flash")]
    [Tooltip("Use VFX for damage flash (recommended for SPUM)")]
    public bool useVFXDamageFlash = true;
    [Tooltip("VFX prefab for damage flash (instantiates at player position)")]
    public GameObject damageFlashVFX;
    [Tooltip("Duration of damage flash in seconds")]
    public float damageFlashDuration = 0.1f;
    [Tooltip("Legacy: Flash all sprite renderers (slow, not recommended)")]
    public bool flashAllSpriteRenderers = false;

    [Header("Advanced Combat")]
    public string currentWeaponType = "Sword"; // For mastery tracking
    public bool enableComboSystem = true;
    public bool enableWeaponMastery = true;

    [Header("Lives & Revive")]
    public int maxLives = 3;
    public int currentLives = 3;
    public float reviveDelay = 3f;
    public bool isDead = false;
    public bool isReviving = false;

    [Header("Invulnerability After Revive")]
    [Tooltip("Duration of invulnerability after revive (seconds)")]
    public float invulnerabilityDuration = 3f;
    [Tooltip("Flash interval during invulnerability (seconds)")]
    public float invulnerabilityFlashInterval = 0.15f;
    [Tooltip("VFX prefab for shield effect during invulnerability")]
    public GameObject invulnerabilityShieldVFX;
    
    // Private state
    private bool isInvulnerable = false;
    private GameObject activeShieldVFX;
    private Coroutine invulnerabilityCoroutine;
    private Coroutine reviveCountdownCoroutine;

    // Cached component references
    private BurnOnHitEffect cachedBurnOnHitEffect;

    [Header("SPUM Integration")]
    public bool useSPUM = false; // Set to true if using SPUM character
    public SPUMPlayerBridge spumBridge;
    public SPUMEquipmentManager spumEquipment;

    // Events
    public System.Action<int, int> OnHealthChanged;
    public System.Action<int> OnGoldChanged;
    public System.Action<float> OnMeleeAttack;
    public System.Action<int> OnSkillCast;
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRevived;
    public System.Action<bool> OnInvulnerabilityChanged;

    // FIX: Store lambda delegates to enable proper unsubscription
    private System.Action<InputAction.CallbackContext> skill1Delegate;
    private System.Action<InputAction.CallbackContext> skill2Delegate;
    private System.Action<InputAction.CallbackContext> skill3Delegate;
    private System.Action<InputAction.CallbackContext> skill4Delegate;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        skillCaster = GetComponent<SkillCaster>();
        comboSystem = GetComponent<ComboSystem>();
        if (attackPoint == null) attackPoint = transform;
        
        // Initialize HP
        stats.currentHP = stats.MaxHP;

        // Auto-find SPUM components if useSPUM is enabled
        if (useSPUM) {
            if (spumBridge == null)
                spumBridge = GetComponent<SPUMPlayerBridge>();
            if (spumEquipment == null)
                spumEquipment = GetComponent<SPUMEquipmentManager>();
        }

        // Setup Input Actions
        SetupInputActions();
    }

    void SetupInputActions() {
        // If no input asset assigned, create default
        if (inputActions == null) {
            Debug.LogWarning("[PlayerController] No Input Action Asset assigned! Please assign GameControls input actions.");
            return;
        }

        gameplayActions = inputActions.FindActionMap("Gameplay");
        if (gameplayActions == null) {
            Debug.LogError("[PlayerController] Could not find 'Gameplay' action map in Input Action Asset!");
            return;
        }

        moveAction = gameplayActions.FindAction("Move");
        attackAction = gameplayActions.FindAction("Attack");
        skill1Action = gameplayActions.FindAction("Skill1");
        skill2Action = gameplayActions.FindAction("Skill2");
        skill3Action = gameplayActions.FindAction("Skill3");
        skill4Action = gameplayActions.FindAction("Skill4");
        inventoryAction = gameplayActions.FindAction("Inventory");
        pauseAction = gameplayActions.FindAction("Pause");

        // Validate skill actions
        ValidateSkillAction(skill1Action, "Skill1");
        ValidateSkillAction(skill2Action, "Skill2");
        ValidateSkillAction(skill3Action, "Skill3");
        ValidateSkillAction(skill4Action, "Skill4");

        // Bind callbacks
        // FIX: Store lambda delegates for proper unsubscription
        skill1Delegate = ctx => OnSkillPerformed(ctx, 0);
        skill2Delegate = ctx => OnSkillPerformed(ctx, 1);
        skill3Delegate = ctx => OnSkillPerformed(ctx, 2);
        skill4Delegate = ctx => OnSkillPerformed(ctx, 3);

        if (attackAction != null)
            attackAction.performed += OnAttackPerformed;
        else
            Debug.LogWarning("[PlayerController] 'Attack' action not found in Input Action Asset!");
        
        if (skill1Action != null)
            skill1Action.performed += skill1Delegate;
        if (skill2Action != null)
            skill2Action.performed += skill2Delegate;
        if (skill3Action != null)
            skill3Action.performed += skill3Delegate;
        if (skill4Action != null)
            skill4Action.performed += skill4Delegate;

        if (inventoryAction != null)
            inventoryAction.performed += OnInventoryPerformed;
        else
            Debug.LogWarning("[PlayerController] 'Inventory' action not found in Input Action Asset!");
        
        if (pauseAction != null)
            pauseAction.performed += OnPausePerformed;
        else
            Debug.LogWarning("[PlayerController] 'Pause' action not found in Input Action Asset!");
    }
    
    /// <summary>
    /// Validates that a skill input action exists and logs helpful error if not.
    /// </summary>
    void ValidateSkillAction(InputAction action, string actionName) {
        if (action == null) {
            Debug.LogError($"[PlayerController] '{actionName}' action not found in Input Action Asset! " +
                          "Skill inputs won't work. Make sure your Input Action Asset has a 'Gameplay' action map " +
                          "with actions named Skill1, Skill2, Skill3, and Skill4.");
        }
    }

    void OnEnable() {
        // Subscribe to enemy deaths for mastery tracking
        Enemy.OnEnemyDeathGlobal += OnEnemyKilled;
        
        // Enable input actions
        gameplayActions?.Enable();
    }

    void OnDisable() {
        Enemy.OnEnemyDeathGlobal -= OnEnemyKilled;
        
        // Disable input actions
        gameplayActions?.Disable();
    }

    void OnDestroy() {
        // FIX: Unsubscribe using stored delegates (lambdas must be the same instance)
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;
        if (skill1Action != null && skill1Delegate != null)
            skill1Action.performed -= skill1Delegate;
        if (skill2Action != null && skill2Delegate != null)
            skill2Action.performed -= skill2Delegate;
        if (skill3Action != null && skill3Delegate != null)
            skill3Action.performed -= skill3Delegate;
        if (skill4Action != null && skill4Delegate != null)
            skill4Action.performed -= skill4Delegate;
        if (inventoryAction != null)
            inventoryAction.performed -= OnInventoryPerformed;
        if (pauseAction != null)
            pauseAction.performed -= OnPausePerformed;
    }

    void Start() {
        OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);
        OnGoldChanged?.Invoke(gold);
    }

    void Update() {
        // Don't process input if dead or reviving
        if (isDead || isReviving) return;
        
        HandleInput();
        HandleCooldowns();
        
        // Only update regular animator (non-SPUM)
        if (!useSPUM) {
            UpdateAnimation();
        }
    }

    void FixedUpdate() {
        // Don't move if dead or reviving
        if (isDead || isReviving) return;
        
        HandleMovement();
    }

    void HandleInput() {
        // Movement - Read from Input Action
        if (moveAction != null) {
            moveInput = moveAction.ReadValue<Vector2>();
        }
        
        if (moveInput.magnitude > 0.1f) {
            facingDirection = moveInput.normalized;
            isMoving = true;
        } else {
            isMoving = false;
        }
    }

    void OnAttackPerformed(InputAction.CallbackContext context) {
        TryMeleeAttack();
    }

    void OnSkillPerformed(InputAction.CallbackContext context, int skillIndex) {
        TryCastSkill(skillIndex);
    }

    void OnInventoryPerformed(InputAction.CallbackContext context) {
        UIManager.Instance?.ToggleInventory();
    }

    void OnPausePerformed(InputAction.CallbackContext context) {
        // Use HandleEscapeKey to properly manage panel closing then pause
        if (UIManager.Instance != null) {
            UIManager.Instance.HandleEscapeKey();
        }
    }

    void HandleMovement() {
        rb.MovePosition(rb.position + moveInput.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void HandleCooldowns() {
        if (meleeCooldownTimer > 0) {
            meleeCooldownTimer -= Time.deltaTime;
        }
    }

    void UpdateAnimation() {
        // Skip if using SPUM (handled by SPUMPlayerBridge)
        if (useSPUM) return;
        
        if (animator == null) return;
        
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("LastMoveX", facingDirection.x);
        animator.SetFloat("LastMoveY", facingDirection.y);

        // Flip sprite based on facing (legacy single sprite only)
        if (spriteRenderer != null) {
            if (facingDirection.x > 0.1f) spriteRenderer.flipX = false;
            else if (facingDirection.x < -0.1f) spriteRenderer.flipX = true;
        }
    }

    void TryMeleeAttack() {
        if (meleeCooldownTimer > 0) return;
        
        meleeCooldownTimer = meleeCooldown / stats.AttackSpeed;
        PerformMeleeAttack();
        OnMeleeAttack?.Invoke(meleeCooldown / stats.AttackSpeed);

        // Trigger SPUM attack animation if using SPUM
        if (useSPUM && spumBridge != null) {
            spumBridge.PlayAttackAnimation();
        }
        // Trigger regular animator if not using SPUM
        else if (animator != null) {
            animator.SetTrigger("Attack");
        }
    }

    void PerformMeleeAttack() {
        // Check for finisher input (hold attack button)
        bool isFinisher = attackAction != null && attackAction.IsPressed();
        
        // Determine attack position based on facing direction
        Vector2 attackPos = (Vector2)transform.position + facingDirection * meleeRange * 0.5f;
        
        // Show attack effect
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, meleeRange * 0.5f, enemyLayer);
        
        int hitCount = 0;
        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                int damage = stats.Damage;
                
                // Apply combo multiplier
                if (comboSystem != null && enableComboSystem) {
                    damage = Mathf.RoundToInt(damage * comboSystem.GetCurrentDamageBonus());
                }
                
                // Apply synergy multiplier
                if (SkillSynergyManager.Instance != null) {
                    damage = Mathf.RoundToInt(damage * SkillSynergyManager.Instance.GetSynergyDamageMultiplier());
                }
                
                if (stats.IsCrit()) {
                    damage = stats.GetCritDamage(damage);
                    // Show crit effect
                }
                
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null) {
                    enemy.TakeDamage(damage);
                    hitCount++;
                    
                    // Apply burn on hit if set bonus active (cached reference)
                    if (cachedBurnOnHitEffect == null) {
                        cachedBurnOnHitEffect = GetComponent<BurnOnHitEffect>();
                    }
                    cachedBurnOnHitEffect?.ApplyBurn(enemy);
                }
            }
        }

        // Register combo hit if we hit something
        if (hitCount > 0 && comboSystem != null && enableComboSystem) {
            comboSystem.RegisterHit(isFinisher && comboSystem.IsFinisherReady());
        }
    }

    void TryCastSkill(int index) {
        // Validate skill caster exists
        if (skillCaster == null) {
            skillCaster = GetComponent<SkillCaster>();
            if (skillCaster == null) {
                Debug.LogError("[PlayerController] No SkillCaster found! Cannot cast skills.");
                return;
            }
        }
        
        // Validate skill exists at index
        var skill = skillCaster.GetSkill(index);
        if (skill == null) {
            if (Application.isEditor) {
                Debug.LogWarning($"[PlayerController] No skill assigned to slot {index + 1}! Assign a SkillSO in the inspector.");
            }
            return;
        }
        
        // Try to cast the skill
        if (skillCaster.TryCastSkill(index)) {
            OnSkillCast?.Invoke(index);

            // Trigger SPUM skill animation if using SPUM
            if (useSPUM && spumBridge != null) {
                spumBridge.PlaySkillAnimation(index);
            }
        }
    }

    public void TakeDamage(int damage) {
        // Cannot take damage while dead, reviving, or invulnerable
        if (isDead || isReviving || isInvulnerable) {
            // Show invulnerability effect if trying to take damage while invulnerable
            if (isInvulnerable && gameObject.activeInHierarchy) {
                ShowInvulnerabilityEffect();
            }
            return;
        }

        if (isShieldActive) {
            damage = Mathf.RoundToInt(damage * (1f - shieldDamageReduction));
        }

        // Defense is already applied in stats.TakeDamage
        stats.TakeDamage(damage);
        
        OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);

        // Show damage flash - only if game object is active
        if (gameObject.activeInHierarchy) {
            StartCoroutine(DamageFlash());
        }

        // Play damaged animation
        if (useSPUM && spumBridge != null) {
            spumBridge.PlayDamagedAnimation();
        }

        if (stats.currentHP <= 0) {
            Die();
        }
    }

    /// <summary>
    /// Shows a brief visual effect when damage is blocked by invulnerability.
    /// </summary>
    void ShowInvulnerabilityEffect() {
        // Could instantiate a small VFX here to show "blocked" or "immune"
        // For now, we'll just do a quick flash
        if (activeShieldVFX != null) {
            // Pulse the shield
            StartCoroutine(PulseShieldEffect());
        }
    }

    IEnumerator PulseShieldEffect() {
        if (activeShieldVFX == null) yield break;
        
        Transform shieldTransform = activeShieldVFX.transform;
        Vector3 originalScale = shieldTransform.localScale;
        
        // Quick pulse animation
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            shieldTransform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }
        
        shieldTransform.localScale = originalScale;
    }

    IEnumerator DamageFlash() {
        // Method 1: VFX-based damage flash (recommended, most performant)
        if (useVFXDamageFlash && damageFlashVFX != null) {
            GameObject flash = Instantiate(damageFlashVFX, transform.position, Quaternion.identity);
            flash.transform.SetParent(transform);
            Destroy(flash, damageFlashDuration);
            yield return new WaitForSeconds(damageFlashDuration);
            yield break;
        }
        
        // Method 2: Flash all sprite renderers (legacy, slow)
        if (flashAllSpriteRenderers) {
            Transform searchRoot = transform;
            if (useSPUM && spumEquipment != null && spumEquipment.spumPrefabs != null) {
                searchRoot = spumEquipment.spumPrefabs.transform;
            }
            
            SpriteRenderer[] renderers = searchRoot.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0) {
                Color[] originalColors = new Color[renderers.Length];
                
                for (int i = 0; i < renderers.Length; i++) {
                    originalColors[i] = renderers[i].color;
                    renderers[i].color = Color.red;
                }
                
                yield return new WaitForSeconds(damageFlashDuration);
                
                for (int i = 0; i < renderers.Length; i++) {
                    if (renderers[i] != null) {
                        renderers[i].color = originalColors[i];
                    }
                }
                yield break;
            }
        }
        
        // Method 3: Single sprite renderer flash (legacy single-sprite)
        if (spriteRenderer != null) {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(damageFlashDuration);
            if (spriteRenderer != null) {
                spriteRenderer.color = original;
            }
        }
    }

    void Die() {
        if (isDead || isReviving) return;
        
        isDead = true;
        currentLives--;
        
        // Notify listeners
        OnPlayerDied?.Invoke();
        
        // Stop any active invulnerability
        if (invulnerabilityCoroutine != null) {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }
        RemoveInvulnerability();

        // Play death animation
        if (useSPUM && spumBridge != null) {
            spumBridge.PlayDeathAnimation();
        }
        else if (animator != null) {
            animator.SetTrigger("Die");
        }

        Debug.Log($"Player Died! Lives remaining: {currentLives}");
        
        if (currentLives > 0) {
            // Start revive process
            reviveCountdownCoroutine = StartCoroutine(ReviveAfterDelay());
        } else {
            // Game Over - no lives left
            StartCoroutine(GameOver());
        }
    }

    System.Collections.IEnumerator ReviveAfterDelay() {
        isReviving = true;
        
        // Show revive countdown UI
        UIManager.Instance?.ShowReviveCountdown(reviveDelay);
        
        // Wait for revive delay
        float countdown = reviveDelay;
        while (countdown > 0) {
            countdown -= Time.deltaTime;
            yield return null;
        }
        
        // Revive player
        Revive();
    }

    void Revive() {
        isDead = false;
        isReviving = false;
        
        // Reset HP
        stats.currentHP = stats.MaxHP;
        OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);
        
        // Play revive animation or effect
        if (useSPUM && spumBridge != null) {
            spumBridge.PlayIdleAnimation();
        }
        else if (animator != null) {
            animator.SetTrigger("Revive");
        }
        
        // Start invulnerability period
        StartInvulnerability();
        
        // Show revive notification
        UIManager.Instance?.ShowNotification($"Revived! {currentLives} lives remaining");
        
        // Notify listeners
        OnPlayerRevived?.Invoke();
        
        Debug.Log("Player Revived! Invulnerability active for " + invulnerabilityDuration + " seconds.");
    }

    #region Invulnerability System

    /// <summary>
    /// Starts the invulnerability period after revive.
    /// </summary>
    void StartInvulnerability() {
        isInvulnerable = true;
        OnInvulnerabilityChanged?.Invoke(true);
        
        // Create shield VFX if assigned
        if (invulnerabilityShieldVFX != null && activeShieldVFX == null) {
            activeShieldVFX = Instantiate(invulnerabilityShieldVFX, transform.position, Quaternion.identity);
            activeShieldVFX.transform.SetParent(transform);
        }
        
        // Start the invulnerability coroutine
        invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());
    }

    /// <summary>
    /// Coroutine that handles the invulnerability duration and visual effects.
    /// </summary>
    System.Collections.IEnumerator InvulnerabilityRoutine() {
        float remainingTime = invulnerabilityDuration;
        
        // Get all sprite renderers for flashing effect
        SpriteRenderer[] allRenderers = GetAllSpriteRenderers();
        
        while (remainingTime > 0) {
            // Flash effect - toggle transparency
            float alpha = Mathf.PingPong(Time.time * (1f / invulnerabilityFlashInterval), 1f) * 0.5f + 0.5f;
            SetSpritesAlpha(allRenderers, alpha);
            
            remainingTime -= Time.deltaTime;
            yield return null;
        }
        
        // Remove invulnerability
        RemoveInvulnerability();
        
        // Restore full opacity
        SetSpritesAlpha(allRenderers, 1f);
        
        invulnerabilityCoroutine = null;
    }

    /// <summary>
    /// Removes invulnerability state and cleans up effects.
    /// </summary>
    void RemoveInvulnerability() {
        isInvulnerable = false;
        OnInvulnerabilityChanged?.Invoke(false);
        
        // Remove shield VFX
        if (activeShieldVFX != null) {
            Destroy(activeShieldVFX);
            activeShieldVFX = null;
        }
        
        // Ensure sprites are fully visible
        SpriteRenderer[] allRenderers = GetAllSpriteRenderers();
        SetSpritesAlpha(allRenderers, 1f);
        
        Debug.Log("Invulnerability ended.");
    }

    /// <summary>
    /// Gets all sprite renderers on the player (for SPUM or regular).
    /// </summary>
    SpriteRenderer[] GetAllSpriteRenderers() {
        if (useSPUM && spumEquipment != null && spumEquipment.spumPrefabs != null) {
            return spumEquipment.spumPrefabs.GetComponentsInChildren<SpriteRenderer>(true);
        }
        return GetComponentsInChildren<SpriteRenderer>(true);
    }

    /// <summary>
    /// Sets alpha value for all sprite renderers.
    /// </summary>
    void SetSpritesAlpha(SpriteRenderer[] renderers, float alpha) {
        foreach (var renderer in renderers) {
            if (renderer != null) {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
    }

    /// <summary>
    /// Checks if the player is currently invulnerable.
    /// </summary>
    public bool IsInvulnerable() => isInvulnerable;

    /// <summary>
    /// Gets the remaining invulnerability time (0 if not invulnerable).
    /// </summary>
    public float GetRemainingInvulnerabilityTime() {
        // This is an approximation - the coroutine tracks the actual time
        return isInvulnerable ? invulnerabilityDuration : 0f;
    }

    #endregion

    System.Collections.IEnumerator GameOver() {
        // Notify GameManager
        GameManager.Instance?.OnPlayerGameOver();
        
        // Show game over UI
        UIManager.Instance?.ShowGameOver();
        
        yield return new WaitForSeconds(2f);
        
        // Destroy the player object instead of just disabling
        // This properly removes the player from the game
        Destroy(gameObject);
    }

    public void Heal(int amount) {
        // Cannot heal while dead
        if (isDead) return;
        
        stats.Heal(amount);
        OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);
    }

    public void AddGold(int amount) {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    public bool SpendGold(int amount) {
        if (gold >= amount) {
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            return true;
        }
        return false;
    }

    public void UpdateStatsFromEquipment() {
        // Recalculate stats when equipment changes
        stats.ResetMods();
        // Equipment bonuses will be applied by InventoryManager
        OnHealthChanged?.Invoke(stats.currentHP, stats.MaxHP);
    }

    // Visual updates for equipment - supports both regular and SPUM
    public void SetWeaponVisual(Sprite sprite) {
        if (useSPUM && spumEquipment != null) {
            spumEquipment.EquipWeapon(sprite);
        }
        else {
            // Legacy support
            SetLegacyWeaponVisual(sprite);
        }
    }

    public void SetArmorVisual(Sprite sprite) {
        if (useSPUM && spumEquipment != null) {
            spumEquipment.EquipArmor(sprite);
        }
        else {
            // Legacy support
            SetLegacyArmorVisual(sprite);
        }
    }

    void SetLegacyWeaponVisual(Sprite sprite) {
        // Find weapon visual child if not assigned
        if (transform.Find("Weapon") != null) {
            var sr = transform.Find("Weapon").GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.sprite = sprite;
                sr.enabled = sprite != null;
            }
        }
    }

    void SetLegacyArmorVisual(Sprite sprite) {
        if (transform.Find("Armor") != null) {
            var sr = transform.Find("Armor").GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.sprite = sprite;
                sr.enabled = sprite != null;
            }
        }
    }

    void OnEnemyKilled(Enemy enemy) {
        // Register kill for weapon mastery
        if (enableWeaponMastery && !string.IsNullOrEmpty(currentWeaponType)) {
            WeaponMasteryManager.Instance?.RegisterKill(currentWeaponType);
        }
    }

    // Public getters for SPUM bridge
    public Vector2 GetMoveInput() => moveInput;
    public Vector2 GetFacingDirection() => facingDirection;
    public bool IsMoving() => isMoving;
    
    // Shield system
    public void SetShieldActive(bool active) {
        isShieldActive = active;
        // Visual effect
        if (active) {
            // Could show shield VFX here
        }
    }
    
    public bool IsShieldActive() => isShieldActive;
    public float GetShieldDamageReduction() => isShieldActive ? shieldDamageReduction : 0f;

    // State getters
    public bool IsDead() => isDead;
    public bool IsReviving() => isReviving;
    public int GetCurrentLives() => currentLives;
    public int GetMaxLives() => maxLives;

    void OnDrawGizmosSelected() {
        // Show melee range
        Gizmos.color = Color.red;
        Vector2 attackPos = Application.isPlaying ? 
            (Vector2)transform.position + facingDirection * meleeRange * 0.5f : 
            (Vector2)transform.position;
        Gizmos.DrawWireSphere(attackPos, meleeRange * 0.5f);
    }
}

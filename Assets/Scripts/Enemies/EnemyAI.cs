using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AI personality types that determine behavior patterns.
/// </summary>
public enum AIPersonality {
    Aggressive,   // Charges player, uses skills often, never retreats
    Defensive,    // Keeps distance, blocks/parries, retreats when hurt
    Ranged,       // Maintains distance, kites player
    Tactical,     // Uses skills strategically, adapts to player behavior
    Berserker,    // Low health = more damage, never retreats
    Support       // Buffs allies, summons, avoids direct combat
}

/// <summary>
/// AI state machine states.
/// </summary>
public enum AIState {
    Idle,
    Chase,
    Attack,
    UseSkill,
    Retreat,
    Reposition,
    Dead
}

/// <summary>
/// Handles AI logic for enemies including skill usage, personality-based behaviors,
/// and state management. Separated from Enemy.cs for better code organization.
/// </summary>
public class EnemyAI : MonoBehaviour {
    
    [Header("AI Personality")]
    public AIPersonality personality = AIPersonality.Aggressive;
    
    [Header("Skills")]
    public EnemySkillSO[] skills = new EnemySkillSO[0];
    public bool useSkills = true;
    
    [Header("Skill Usage")]
    public float skillCheckInterval = 1f;      // How often to check for skill usage
    public float minTimeBetweenSkills = 2f;    // Minimum time between any skills
    public float randomSkillChance = 0.3f;     // Chance to use skill when available
    
    [Header("Retreat Settings")]
    public bool canRetreat = true;
    public float retreatHealthThreshold = 0.25f;
    public float retreatDuration = 3f;
    public float repositionMinDistance = 4f;
    
    [Header("Combat Timing")]
    public float reactionTime = 0.2f;          // Delay before reacting to player
    public float attackCommitment = 0.5f;      // How long to commit to an attack
    
    // References
    private Enemy enemy;
    private Transform player;
    private Rigidbody2D rb;
    
    // State
    private AIState currentState = AIState.Idle;
    private AIState previousState = AIState.Idle;
    private float[] skillCooldowns;
    private float lastSkillTime = -999f;
    private float skillCheckTimer;
    private float retreatTimer;
    private float stateTimer;
    private bool isCastingSkill = false;
    private int currentSkillIndex = -1;
    private Coroutine activeSkillCoroutine;
    private Coroutine stateCoroutine;
    
    // Runtime skill state
    private List<GameObject> activeEffects = new List<GameObject>();
    private List<GameObject> activeSummons = new List<GameObject>();
    
    // Events
    public System.Action<EnemySkillSO> OnSkillStarted;
    public System.Action<EnemySkillSO> OnSkillCompleted;
    public System.Action<AIState> OnStateChanged;
    
    void Awake() {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        
        InitializeSkillCooldowns();
    }
    
    void Start() {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void OnEnable() {
        ResetAI();
    }
    
    void OnDisable() {
        CleanupActiveEffects();
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Initializes the cooldown array for skills.
    /// </summary>
    void InitializeSkillCooldowns() {
        skillCooldowns = new float[skills.Length];
        for (int i = 0; i < skillCooldowns.Length; i++) {
            skillCooldowns[i] = 0f;
        }
    }
    
    /// <summary>
    /// Resets AI state for object pooling.
    /// </summary>
    public void ResetAI() {
        currentState = AIState.Idle;
        previousState = AIState.Idle;
        isCastingSkill = false;
        currentSkillIndex = -1;
        lastSkillTime = -999f;
        skillCheckTimer = 0f;
        retreatTimer = 0f;
        stateTimer = 0f;
        
        // Reset skill cooldowns
        if (skillCooldowns != null) {
            for (int i = 0; i < skillCooldowns.Length; i++) {
                skillCooldowns[i] = 0f;
            }
        }
        
        // Stop any running coroutines
        if (activeSkillCoroutine != null) {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }
        if (stateCoroutine != null) {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }
        
        CleanupActiveEffects();
    }
    
    void Update() {
        if (enemy == null || enemy.IsDead) return;
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }
        
        UpdateCooldowns();
        UpdateAI();
    }
    
    /// <summary>
    /// Updates all skill cooldowns.
    /// </summary>
    void UpdateCooldowns() {
        for (int i = 0; i < skillCooldowns.Length; i++) {
            if (skillCooldowns[i] > 0) {
                skillCooldowns[i] -= Time.deltaTime;
            }
        }
    }
    
    /// <summary>
    /// Main AI update loop.
    /// </summary>
    void UpdateAI() {
        float healthPercent = enemy.GetHealthPercent();
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Update state timer
        stateTimer += Time.deltaTime;
        skillCheckTimer += Time.deltaTime;
        
        // Check for state transitions based on personality and situation
        AIState newState = DetermineState(healthPercent, distanceToPlayer);
        
        if (newState != currentState) {
            ChangeState(newState);
        }
        
        // Check for skill usage
        if (useSkills && !isCastingSkill && skillCheckTimer >= skillCheckInterval) {
            skillCheckTimer = 0f;
            TryUseSkill(healthPercent, distanceToPlayer);
        }
        
        // Execute current state behavior
        ExecuteStateBehavior();
    }
    
    /// <summary>
    /// Determines the appropriate state based on current conditions and personality.
    /// </summary>
    AIState DetermineState(float healthPercent, float distanceToPlayer) {
        // If casting a skill, stay in skill state
        if (isCastingSkill) return AIState.UseSkill;
        
        // Check retreat conditions (only if not already retreating from skill)
        if (canRetreat && currentState != AIState.Retreat && ShouldRetreat(healthPercent, distanceToPlayer)) {
            retreatTimer = retreatDuration;
            return AIState.Retreat;
        }
        
        // Personality-based state selection
        switch (personality) {
            case AIPersonality.Aggressive:
                return DetermineAggressiveState(distanceToPlayer);
                
            case AIPersonality.Defensive:
                return DetermineDefensiveState(healthPercent, distanceToPlayer);
                
            case AIPersonality.Ranged:
                return DetermineRangedState(distanceToPlayer);
                
            case AIPersonality.Tactical:
                return DetermineTacticalState(healthPercent, distanceToPlayer);
                
            case AIPersonality.Berserker:
                return DetermineBerserkerState(distanceToPlayer);
                
            case AIPersonality.Support:
                return DetermineSupportState(healthPercent, distanceToPlayer);
                
            default:
                return DetermineAggressiveState(distanceToPlayer);
        }
    }
    
    AIState DetermineAggressiveState(float distanceToPlayer) {
        if (distanceToPlayer <= enemy.attackRange) {
            return AIState.Attack;
        }
        return AIState.Chase;
    }
    
    AIState DetermineDefensiveState(float healthPercent, float distanceToPlayer) {
        // Defensive enemies keep some distance and retreat when hurt
        if (healthPercent < retreatHealthThreshold) {
            return AIState.Retreat;
        }
        
        if (distanceToPlayer < enemy.attackRange * 0.5f) {
            return AIState.Reposition; // Too close, back up
        }
        
        if (distanceToPlayer <= enemy.attackRange) {
            return AIState.Attack;
        }
        
        return AIState.Chase;
    }
    
    AIState DetermineRangedState(float distanceToPlayer) {
        // Ranged enemies want to stay at optimal distance
        float optimalRange = enemy.attackRange * 0.8f;
        float minComfortDistance = enemy.attackRange * 0.3f;
        
        if (distanceToPlayer < minComfortDistance) {
            return AIState.Retreat; // Too close, kite back
        }
        
        if (distanceToPlayer <= enemy.attackRange) {
            return AIState.Attack;
        }
        
        // Reposition to optimal range rather than getting too close
        if (distanceToPlayer < optimalRange) {
            return AIState.Reposition;
        }
        
        return AIState.Chase;
    }
    
    AIState DetermineTacticalState(float healthPercent, float distanceToPlayer) {
        // Tactical enemies adapt their behavior
        if (healthPercent < 0.3f && Random.value < 0.5f) {
            return AIState.Retreat; // Sometimes retreat when low health
        }
        
        if (distanceToPlayer <= enemy.attackRange) {
            // Sometimes reposition instead of attacking
            if (Random.value < 0.2f && stateTimer > 2f) {
                return AIState.Reposition;
            }
            return AIState.Attack;
        }
        
        return AIState.Chase;
    }
    
    AIState DetermineBerserkerState(float distanceToPlayer) {
        // Berserkers never retreat, just chase and attack
        if (distanceToPlayer <= enemy.attackRange) {
            return AIState.Attack;
        }
        return AIState.Chase;
    }
    
    AIState DetermineSupportState(float healthPercent, float distanceToPlayer) {
        // Support types stay at range and use skills
        if (distanceToPlayer < repositionMinDistance) {
            return AIState.Retreat;
        }
        
        if (HasAvailableSummonSkill()) {
            return AIState.UseSkill;
        }
        
        // Stay at range
        if (distanceToPlayer > enemy.attackRange * 1.5f) {
            return AIState.Chase;
        }
        
        return AIState.Idle; // Wait for skill cooldowns
    }
    
    /// <summary>
    /// Checks if enemy should retreat based on personality and health.
    /// </summary>
    bool ShouldRetreat(float healthPercent, float distanceToPlayer) {
        switch (personality) {
            case AIPersonality.Aggressive:
            case AIPersonality.Berserker:
                return false; // Never retreat
                
            case AIPersonality.Defensive:
                return healthPercent < retreatHealthThreshold;
                
            case AIPersonality.Ranged:
                return distanceToPlayer < enemy.attackRange * 0.3f;
                
            case AIPersonality.Tactical:
                return healthPercent < retreatHealthThreshold * 0.5f;
                
            case AIPersonality.Support:
                return distanceToPlayer < repositionMinDistance;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Changes to a new AI state.
    /// </summary>
    void ChangeState(AIState newState) {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;
        
        OnStateChanged?.Invoke(newState);
        
        // Cancel any running state coroutine
        if (stateCoroutine != null) {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }
        
        // Start state-specific coroutine if needed
        if (newState == AIState.Retreat && retreatTimer > 0) {
            stateCoroutine = StartCoroutine(RetreatStateCoroutine());
        }
    }
    
    /// <summary>
    /// Coroutine that handles the retreat state timing.
    /// </summary>
    IEnumerator RetreatStateCoroutine() {
        while (retreatTimer > 0) {
            retreatTimer -= Time.deltaTime;
            yield return null;
        }
        
        // Return to previous state when retreat is done
        if (currentState == AIState.Retreat) {
            ChangeState(AIState.Chase);
        }
    }
    
    /// <summary>
    /// Executes behavior based on current state.
    /// </summary>
    void ExecuteStateBehavior() {
        switch (currentState) {
            case AIState.Chase:
                enemy.SetState(EnemyState.Chase);
                break;
                
            case AIState.Attack:
                enemy.SetState(EnemyState.Attack);
                break;
                
            case AIState.Retreat:
                RetreatFromPlayer();
                break;
                
            case AIState.Reposition:
                Reposition();
                break;
                
            case AIState.Idle:
                enemy.SetState(EnemyState.Idle);
                break;
        }
    }
    
    /// <summary>
    /// Moves away from the player.
    /// </summary>
    void RetreatFromPlayer() {
        if (player == null || rb == null) return;
        
        Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
        rb.MovePosition(rb.position + awayFromPlayer * enemy.moveSpeed * 0.8f * Time.fixedDeltaTime);
        
        // Face away from player
        enemy.FaceDirection(awayFromPlayer);
    }
    
    /// <summary>
    /// Repositions to maintain optimal distance.
    /// </summary>
    void Reposition() {
        if (player == null || rb == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        Vector2 direction;
        
        if (distanceToPlayer < enemy.attackRange * 0.5f) {
            // Too close, move away
            direction = ((Vector2)transform.position - (Vector2)player.position).normalized;
        } else {
            // Too far, move closer but not directly at player
            direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            // Add perpendicular movement for strafing
            direction = Quaternion.Euler(0, 0, Random.value > 0.5f ? 45f : -45f) * direction;
        }
        
        rb.MovePosition(rb.position + direction * enemy.moveSpeed * 0.7f * Time.fixedDeltaTime);
        enemy.FaceDirection(direction);
    }
    
    /// <summary>
    /// Attempts to use a skill based on conditions.
    /// </summary>
    void TryUseSkill(float healthPercent, float distanceToPlayer) {
        if (Time.time - lastSkillTime < minTimeBetweenSkills) return;
        if (Random.value > randomSkillChance) return;
        
        // Find available skills
        List<int> availableSkills = new List<int>();
        bool otherSkillsOnCooldown = true;
        
        for (int i = 0; i < skills.Length; i++) {
            if (skills[i] == null) continue;
            if (skillCooldowns[i] > 0) continue;
            
            // Check range
            if (distanceToPlayer < skills[i].minRange || distanceToPlayer > skills[i].maxRange) continue;
            
            // Check usage condition
            if (!skills[i].CanUse(healthPercent, distanceToPlayer, false)) continue;
            
            availableSkills.Add(i);
            otherSkillsOnCooldown = false;
        }
        
        if (availableSkills.Count == 0) return;
        
        // Re-check conditions with correct "other skills on cooldown" value
        availableSkills.Clear();
        for (int i = 0; i < skills.Length; i++) {
            if (skills[i] == null) continue;
            if (skillCooldowns[i] > 0) continue;
            if (distanceToPlayer < skills[i].minRange || distanceToPlayer > skills[i].maxRange) continue;
            if (!skills[i].CanUse(healthPercent, distanceToPlayer, otherSkillsOnCooldown)) continue;
            
            availableSkills.Add(i);
        }
        
        if (availableSkills.Count == 0) return;
        
        // Select skill based on priority
        int selectedSkill = SelectSkillByPriority(availableSkills);
        ExecuteSkill(selectedSkill);
    }
    
    /// <summary>
    /// Selects a skill from available options based on priority weights.
    /// </summary>
    int SelectSkillByPriority(List<int> availableSkills) {
        if (availableSkills.Count == 1) return availableSkills[0];
        
        float totalPriority = 0f;
        foreach (int index in availableSkills) {
            totalPriority += skills[index].priority;
        }
        
        float random = Random.value * totalPriority;
        float current = 0f;
        
        foreach (int index in availableSkills) {
            current += skills[index].priority;
            if (random <= current) {
                return index;
            }
        }
        
        return availableSkills[availableSkills.Count - 1];
    }
    
    /// <summary>
    /// Executes the selected skill.
    /// </summary>
    void ExecuteSkill(int skillIndex) {
        if (skillIndex < 0 || skillIndex >= skills.Length) return;
        if (skills[skillIndex] == null) return;
        
        EnemySkillSO skill = skills[skillIndex];
        
        // Validate
        if (!skill.Validate(out string error)) {
            Debug.LogWarning($"[EnemyAI] Skill validation failed: {error}");
            return;
        }
        
        currentSkillIndex = skillIndex;
        isCastingSkill = true;
        skillCooldowns[skillIndex] = skill.cooldownTime;
        lastSkillTime = Time.time;
        
        activeSkillCoroutine = StartCoroutine(SkillCastCoroutine(skill));
    }
    
    /// <summary>
    /// Coroutine for handling skill cast timing.
    /// </summary>
    IEnumerator SkillCastCoroutine(EnemySkillSO skill) {
        OnSkillStarted?.Invoke(skill);
        
        // Face player during cast if required
        if (skill.facePlayerDuringCast && player != null) {
            Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            enemy.FaceDirection(toPlayer);
        }
        
        // Cast/wind-up time
        if (skill.castTime > 0) {
            // Play cast effect
            if (skill.castEffectPrefab != null) {
                GameObject effect = Instantiate(skill.castEffectPrefab, transform.position, Quaternion.identity, transform);
                activeEffects.Add(effect);
            }
            
            // Play cast sound
            if (skill.castSound != null) {
                AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
            }
            
            // Wait for cast time
            yield return new WaitForSeconds(skill.castTime);
        }
        
        // Execute skill effect
        ExecuteSkillEffect(skill);
        
        // Recovery time
        if (skill.recoveryTime > 0) {
            yield return new WaitForSeconds(skill.recoveryTime);
        }
        
        isCastingSkill = false;
        currentSkillIndex = -1;
        OnSkillCompleted?.Invoke(skill);
    }
    
    /// <summary>
    /// Executes the actual skill effect based on skill type.
    /// </summary>
    void ExecuteSkillEffect(EnemySkillSO skill) {
        if (player == null) return;
        
        switch (skill.skillType) {
            case EnemySkillType.MeleeAttack:
                ExecuteMeleeSkill(skill);
                break;
                
            case EnemySkillType.RangedProjectile:
                ExecuteRangedSkill(skill);
                break;
                
            case EnemySkillType.DashAttack:
                StartCoroutine(ExecuteDashAttack(skill));
                break;
                
            case EnemySkillType.AOEAttack:
                ExecuteAOESkill(skill);
                break;
                
            case EnemySkillType.Summon:
                ExecuteSummonSkill(skill);
                break;
                
            case EnemySkillType.SelfHeal:
                ExecuteSelfHeal(skill);
                break;
                
            case EnemySkillType.Retreat:
                retreatTimer = skill.retreatDuration > 0 ? skill.retreatDuration : 2f;
                ChangeState(AIState.Retreat);
                break;
                
            case EnemySkillType.ChargeAttack:
                StartCoroutine(ExecuteChargeAttack(skill));
                break;
        }
        
        // Play impact sound
        if (skill.impactSound != null) {
            AudioSource.PlayClipAtPoint(skill.impactSound, transform.position);
        }
    }
    
    void ExecuteMeleeSkill(EnemySkillSO skill) {
        // Check if player is still in range
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > skill.maxRange) return;
        
        // Deal damage to player
        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null) {
            int damage = skill.CalculateDamage(enemy.damage);
            playerController.TakeDamage(damage);
            
            // Apply knockback
            if (skill.knockbackForce > 0) {
                Vector2 knockbackDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                // Apply knockback to player (if player controller supports it)
                // playerController.ApplyKnockback(knockbackDir * skill.knockbackForce);
            }
            
            // Apply status effect
            if (skill.applyStatusEffect) {
                // Apply to player if player has status effect system
            }
        }
        
        // Spawn effect
        if (skill.effectPrefab != null) {
            Instantiate(skill.effectPrefab, transform.position, Quaternion.identity);
        }
    }
    
    void ExecuteRangedSkill(EnemySkillSO skill) {
        if (skill.projectilePrefab == null) return;
        
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 spawnPos = transform.position;
        
        GameObject proj = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(Vector3.forward, direction));
        
        var projectile = proj.GetComponent<Projectile>();
        if (projectile != null) {
            int damage = skill.CalculateDamage(enemy.damage);
            // Initialize without SkillSO (enemy skills are simpler)
            projectile.Initialize(direction, skill.maxRange, damage, LayerMask.GetMask("Player"));
        }
    }
    
    IEnumerator ExecuteDashAttack(EnemySkillSO skill) {
        Vector2 dashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 targetPos = (Vector2)transform.position + dashDir * skill.dashDistance;
        Vector2 startPos = transform.position;
        float distance = Vector2.Distance(startPos, targetPos);
        float duration = distance / skill.dashSpeed;
        float elapsed = 0f;
        
        // Spawn dash effect
        if (skill.effectPrefab != null) {
            GameObject effect = Instantiate(skill.effectPrefab, transform.position, Quaternion.identity);
            activeEffects.Add(effect);
        }
        
        // Perform dash
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));
            yield return null;
        }
        
        // Deal damage at end of dash if player is nearby
        float finalDistance = Vector2.Distance(transform.position, player.position);
        if (finalDistance <= skill.aoeRadius) {
            var playerController = player.GetComponent<PlayerController>();
            if (playerController != null) {
                playerController.TakeDamage(skill.CalculateDamage(enemy.damage));
            }
        }
    }
    
    void ExecuteAOESkill(EnemySkillSO skill) {
        // Spawn AOE effect
        if (skill.effectPrefab != null) {
            GameObject effect = Instantiate(skill.effectPrefab, transform.position, Quaternion.identity);
            if (skill.aoeRadius > 0) {
                effect.transform.localScale = Vector3.one * skill.aoeRadius * 2f;
            }
            Destroy(effect, 1f);
        }
        
        // Damage all players in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skill.aoeRadius, LayerMask.GetMask("Player"));
        foreach (var hit in hits) {
            var playerController = hit.GetComponent<PlayerController>();
            if (playerController != null) {
                playerController.TakeDamage(skill.CalculateDamage(enemy.damage));
            }
        }
    }
    
    void ExecuteSummonSkill(EnemySkillSO skill) {
        if (skill.summonPrefab == null) return;
        
        for (int i = 0; i < skill.summonCount; i++) {
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector2 spawnPos = (Vector2)transform.position + offset;
            
            GameObject summon = Instantiate(skill.summonPrefab, spawnPos, Quaternion.identity);
            activeSummons.Add(summon);
            
            if (skill.summonDuration > 0) {
                Destroy(summon, skill.summonDuration);
            }
        }
    }
    
    void ExecuteSelfHeal(EnemySkillSO skill) {
        // Heal effect
        if (skill.effectPrefab != null) {
            GameObject effect = Instantiate(skill.effectPrefab, transform.position, Quaternion.identity, transform);
            Destroy(effect, 2f);
        }
        
        // Restore health (negative damage = heal)
        enemy.Heal(skill.damage);
    }
    
    IEnumerator ExecuteChargeAttack(EnemySkillSO skill) {
        // Brief pause to show wind-up
        yield return new WaitForSeconds(0.3f);
        
        // Dash toward player
        Vector2 dashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 targetPos = (Vector2)transform.position + dashDir * skill.dashDistance;
        
        float elapsed = 0f;
        float duration = skill.dashDistance / skill.dashSpeed;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rb.MovePosition(Vector2.Lerp(transform.position, targetPos, t));
            
            // Check for player collision during charge
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer < 1f) {
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null) {
                    playerController.TakeDamage(skill.CalculateDamage(enemy.damage * 2)); // Double damage for charge
                }
                break;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Checks if there's a summon skill available and off cooldown.
    /// </summary>
    bool HasAvailableSummonSkill() {
        for (int i = 0; i < skills.Length; i++) {
            if (skills[i] != null && 
                skills[i].skillType == EnemySkillType.Summon && 
                skillCooldowns[i] <= 0) {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Interrupts the current skill cast if it can be interrupted.
    /// </summary>
    public void InterruptSkill() {
        if (!isCastingSkill || currentSkillIndex < 0) return;
        if (currentSkillIndex >= skills.Length) return;
        if (!skills[currentSkillIndex].canBeInterrupted) return;
        
        if (activeSkillCoroutine != null) {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }
        
        isCastingSkill = false;
        currentSkillIndex = -1;
        
        // Clear cast effects
        CleanupActiveEffects();
    }
    
    /// <summary>
    /// Cleans up all active effects.
    /// </summary>
    void CleanupActiveEffects() {
        foreach (var effect in activeEffects) {
            if (effect != null) Destroy(effect);
        }
        activeEffects.Clear();
    }
    
    /// <summary>
    /// Gets the current AI state.
    /// </summary>
    public AIState GetCurrentState() {
        return currentState;
    }
    
    /// <summary>
    /// Checks if the AI is currently casting a skill.
    /// </summary>
    public bool IsCastingSkill() {
        return isCastingSkill;
    }
    
    /// <summary>
    /// Gets cooldown remaining for a specific skill.
    /// </summary>
    public float GetSkillCooldown(int index) {
        if (index < 0 || index >= skillCooldowns.Length) return 0f;
        return Mathf.Max(0f, skillCooldowns[index]);
    }
    
    /// <summary>
    /// Resets all skill cooldowns.
    /// </summary>
    public void ResetAllCooldowns() {
        for (int i = 0; i < skillCooldowns.Length; i++) {
            skillCooldowns[i] = 0f;
        }
    }
    
    /// <summary>
    /// Forces the AI to use a retreat behavior.
    /// </summary>
    public void ForceRetreat(float duration) {
        retreatTimer = duration;
        ChangeState(AIState.Retreat);
    }
    
    void OnDrawGizmosSelected() {
        // Draw skill ranges
        if (skills == null) return;
        
        Gizmos.color = Color.cyan;
        foreach (var skill in skills) {
            if (skill == null) continue;
            Gizmos.DrawWireSphere(transform.position, skill.maxRange);
        }
    }
}

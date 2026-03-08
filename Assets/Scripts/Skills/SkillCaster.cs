using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// SkillCaster - Handles all player skill casting, cooldowns, and skill execution.
/// 
/// === SETUP GUIDE ===
/// 
/// 1. SKILL ASSIGNMENT (Inspector):
///    - Assign this component to your Player GameObject (should be auto-added via RequireComponent)
///    - In the inspector, you'll see a "Skills" array with 4 slots (indices 0-3)
///    - Create SkillSO assets via: Right-click > Create > ARPG > Skill
///    - Drag SkillSO assets into the 4 skill slots (Slot 0 = Key 1, Slot 1 = Key 2, etc.)
/// 
/// 2. REQUIRED REFERENCES:
///    - castPoint: Transform where projectiles spawn (defaults to player transform)
///    - enemyLayer: LayerMask for enemies (assign "Enemy" layer)
///    - obstacleLayer: LayerMask for walls/obstacles (used by Dash)
///    - player: Auto-assigned from GetComponent<PlayerController>()
/// 
/// 3. SKILL TYPE REQUIREMENTS:
///    - Projectile: Must have projectilePrefab in SkillSO or defaultProjectilePrefab assigned
///    - Summon: Must have summonPrefab in SkillSO
///    - Shield: Uses defaultShieldEffectPrefab if no persistentEffectPrefab in SkillSO
///    - Teleport/Dash: Require player movement to be enabled
///    - Chain: Requires enemies on enemyLayer within range
/// 
/// 4. INPUT SYSTEM CONNECTION:
///    - PlayerController.SetupInputActions() binds Skill1-4 keys to TryCastSkill(0-3)
///    - Input is handled via Unity's NEW Input System (GameControls input action asset)
///    - Skills trigger when cooldown <= 0 and player has required resources
/// 
/// 5. TESTING WITHOUT SPUM:
///    - Set useSPUM = false in PlayerController
///    - Skills will work normally without SPUM animation integration
///    - Visual effects and damage will still function
/// 
/// 6. DEBUGGING:
///    - Check console for [SkillCaster] validation errors on start
///    - Enable "Verbose Logging" in PlayerController for detailed skill execution logs
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SkillCaster : MonoBehaviour {
    [Header("Skills")]
    [Tooltip("4 Skill slots - assign SkillSO assets here. Index 0 = Key 1, Index 1 = Key 2, etc.")]
    public SkillSO[] skills = new SkillSO[4];
    
    [Header("References")]
    [Tooltip("Transform where projectiles/effects spawn. If null, uses player transform.")]
    public Transform castPoint;
    [Tooltip("LayerMask for detecting enemies. REQUIRED for all combat skills.")]
    public LayerMask enemyLayer;
    [Tooltip("LayerMask for obstacle detection (used by Dash). If 0, uses all layers except enemyLayer.")]
    public LayerMask obstacleLayer;
    [Tooltip("Auto-assigned PlayerController reference")]
    public PlayerController player;

    [Header("Prefabs")]
    [Tooltip("Default projectile prefab used when SkillSO doesn't specify one")]
    public GameObject defaultProjectilePrefab;
    [Tooltip("Default shield effect prefab used when SkillSO doesn't specify one")]
    public GameObject defaultShieldEffectPrefab;
    
    [Header("Debug")]
    [Tooltip("Enable detailed logging for skill execution")]
    public bool verboseLogging = false;
    
    // Runtime state
    private float[] cooldownTimers = new float[4];
    private bool[] isCasting = new bool[4];
    private bool[] isChanneling = new bool[4];
    private List<GameObject> activeEffects = new List<GameObject>();
    private List<GameObject> activeSummons = new List<GameObject>();
    private Camera mainCamera;
    private bool setupValid = false;
    
    // Events for UI binding
    public System.Action<int, float> OnCooldownStarted; // slot, cooldown
    public System.Action<int, float> OnCooldownUpdated; // slot, remaining
    public System.Action<int> OnSkillCast; // slot
    public System.Action<int> OnSkillCharging; // slot
    public System.Action<int> OnSkillReleased; // slot
    public System.Action<int, string> OnSkillFailed; // slot, reason

    void Awake() {
        if (player == null) player = GetComponent<PlayerController>();
        if (castPoint == null) castPoint = transform;
        mainCamera = Camera.main;
        
        // Initialize arrays
        EnsureArraySize(ref cooldownTimers, skills.Length);
        EnsureArraySize(ref isCasting, skills.Length);
        EnsureArraySize(ref isChanneling, skills.Length);
        
        // Validate setup
        setupValid = ValidateSetup();
    }
    
    void Start() {
        if (!setupValid) {
            LogError("Setup validation failed! Skills may not work correctly. Check console for details.");
        }
    }

    void Update() {
        UpdateCooldowns();
        UpdateChanneledSkills();
    }

    void OnDisable() {
        CleanupAllEffects();
    }

    #region Setup Validation

    /// <summary>
    /// Validates the skill setup on Awake. Logs detailed errors for any issues found.
    /// Returns true if setup is valid, false otherwise.
    /// </summary>
    bool ValidateSetup() {
        bool isValid = true;
        
        // Check if we have 4 skill slots
        if (skills == null || skills.Length != 4) {
            LogError($"Skills array must have exactly 4 slots! Current: {(skills?.Length.ToString() ?? "null")}");
            isValid = false;
            
            // Fix array size
            if (skills == null) skills = new SkillSO[4];
            else EnsureArraySize(ref skills, 4);
        }
        
        // Validate each skill slot
        for (int i = 0; i < skills.Length; i++) {
            var skill = skills[i];
            
            if (skill == null) {
                LogWarning($"Skill slot {i + 1} (Key {i + 1}) is EMPTY! Assign a SkillSO asset in the inspector.");
                continue;
            }
            
            // Validate skill configuration
            if (!skill.Validate(out string error)) {
                LogError($"Skill in slot {i + 1} ({skill.skillName ?? "Unnamed"}) has invalid configuration: {error}");
                isValid = false;
            }
            
            // Check for required prefabs based on skill type
            switch (skill.skillType) {
                case SkillType.Projectile:
                    if (skill.projectilePrefab == null && defaultProjectilePrefab == null) {
                        LogError($"Skill '{skill.skillName}' (slot {i + 1}) is Projectile type but has no projectilePrefab! " +
                                "Either assign one in the SkillSO or set defaultProjectilePrefab on SkillCaster.");
                        isValid = false;
                    }
                    break;
                    
                case SkillType.Summon:
                    if (skill.summonPrefab == null) {
                        LogError($"Skill '{skill.skillName}' (slot {i + 1}) is Summon type but has no summonPrefab! " +
                                "Assign one in the SkillSO asset.");
                        isValid = false;
                    }
                    break;
                    
                case SkillType.Shield:
                case SkillType.Reflect:
                    if (skill.persistentEffectPrefab == null && defaultShieldEffectPrefab == null) {
                        LogWarning($"Skill '{skill.skillName}' (slot {i + 1}) has no visual effect. " +
                                  "Assign persistentEffectPrefab in SkillSO or defaultShieldEffectPrefab on SkillCaster.");
                    }
                    break;
            }
            
            // Check for icon (UI requirement)
            if (skill.icon == null) {
                LogWarning($"Skill '{skill.skillName}' (slot {i + 1}) has no icon assigned. UI will show empty slot.");
            }
        }
        
        // Check camera for targeting
        if (mainCamera == null) {
            LogError("No MainCamera found! Skill targeting (mouse-based skills) will fail.");
            isValid = false;
        }
        
        // Check enemy layer
        if (enemyLayer == 0) {
            LogWarning("enemyLayer is not set! Enemies won't be detected by skills.");
        }
        
        return isValid;
    }

    #endregion

    #region Core Skill System

    /// <summary>
    /// Attempts to cast or start charging a skill.
    /// Returns true if skill cast started successfully, false otherwise.
    /// </summary>
    public bool TryCastSkill(int index) {
        if (!CanCastSkill(index)) {
            if (verboseLogging) Log($"TryCastSkill({index}) - CanCastSkill returned false");
            return false;
        }
        
        SkillSO skill = skills[index];
        if (skill == null) {
            LogWarning($"TryCastSkill({index}) - No skill assigned to slot {index + 1}");
            OnSkillFailed?.Invoke(index, "No skill assigned");
            return false;
        }
        
        // Validate skill
        if (!skill.Validate(out string error)) {
            LogWarning($"Skill validation failed for '{skill.skillName}': {error}");
            OnSkillFailed?.Invoke(index, $"Invalid: {error}");
            return false;
        }
        
        // Check resource costs
        if (!HasResources(skill)) {
            ShowResourceWarning();
            OnSkillFailed?.Invoke(index, "Not enough resources");
            return false;
        }
        
        // Start casting/charging if needed
        if (skill.castTime > 0) {
            StartCoroutine(CastWithDelay(index, skill));
            return true;
        }
        
        // Instant cast
        return ExecuteSkill(index, skill);
    }

    /// <summary>
    /// Releases a channeled skill.
    /// </summary>
    public bool TryReleaseSkill(int index) {
        if (index < 0 || index >= skills.Length) {
            LogWarning($"TryReleaseSkill({index}) - Invalid index");
            return false;
        }
        if (!isChanneling[index]) {
            return false;
        }
        
        isChanneling[index] = false;
        OnSkillReleased?.Invoke(index);
        Log($"Skill {index + 1} released");
        return true;
    }

    bool ExecuteSkill(int index, SkillSO skill) {
        if (skill == null) {
            LogError($"ExecuteSkill({index}) - Skill is null!");
            return false;
        }
        
        // Consume resources first (fail-fast)
        if (!HasResources(skill)) {
            ShowResourceWarning();
            return false;
        }
        
        ConsumeResources(skill);
        
        // Set cooldown immediately so it starts even if skill fails
        cooldownTimers[index] = skill.cooldownTime;
        OnCooldownStarted?.Invoke(index, skill.cooldownTime);
        
        // Screen effects
        ApplyScreenEffects(skill);
        
        // Execute based on type
        bool success = false;
        try {
            success = skill.skillType switch {
                SkillType.CircleAOE => CastCircleAOE(skill),
                SkillType.GroundAOE => CastGroundAOE(skill),
                SkillType.Projectile => CastProjectile(skill),
                SkillType.Melee => CastMelee(skill),           // Added Melee support
                SkillType.Shield => CastShield(skill, index),
                SkillType.Dash => CastDash(skill),
                SkillType.Summon => CastSummon(skill),
                SkillType.Buff => CastBuff(skill, index),
                SkillType.Heal => CastHeal(skill),
                SkillType.Chain => CastChain(skill),
                SkillType.Beam => StartChanneling(index, skill),
                SkillType.Trap => CastTrap(skill),
                SkillType.Teleport => CastTeleport(skill),
                SkillType.Reflect => CastReflect(skill, index),
                _ => HandleUnknownSkillType(skill)
            };
        }
        catch (System.Exception ex) {
            LogError($"Exception during skill execution '{skill.skillName}': {ex.Message}\n{ex.StackTrace}");
            success = false;
        }
        
        if (success) {
            OnSkillCast?.Invoke(index);
            PlayCastEffects(skill);
            Log($"Skill '{skill.skillName}' (slot {index + 1}) executed successfully");
        } else {
            LogWarning($"Skill '{skill.skillName}' (slot {index + 1}) execution failed");
            OnSkillFailed?.Invoke(index, "Execution failed");
        }
        
        return success;
    }
    
    bool HandleUnknownSkillType(SkillSO skill) {
        LogError($"Unknown skill type: {skill.skillType}. Skill '{skill.skillName}' may need implementation.");
        return false;
    }

    #endregion

    #region Skill Type Implementations

    bool CastCircleAOE(SkillSO skill) {
        Vector2 castPos = transform.position;
        
        // Visual effect
        SpawnEffect(skill.castEffectPrefab, castPos, skill.radius * 2);
        
        // Delay for animation
        StartCoroutine(DelayedCircleDamage(castPos, skill, 0.2f));
        return true;
    }

    IEnumerator DelayedCircleDamage(Vector2 position, SkillSO skill, float delay) {
        yield return new WaitForSeconds(delay);
        
        SpawnEffect(skill.effectPrefab, position, skill.radius * 2);
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, skill.radius, enemyLayer);
        int hitCount = 0;
        foreach (var hit in hits) {
            if (hit != null && hit.CompareTag("Enemy")) {
                ApplyDamage(hit.gameObject, skill);
                ApplyStatusEffects(hit.gameObject, skill);
                hitCount++;
            }
        }
        
        if (verboseLogging) Log($"CircleAOE hit {hitCount} enemies");
    }

    bool CastGroundAOE(SkillSO skill) {
        Vector3 targetPos = GetTargetPosition(skill);
        if (targetPos == Vector3.zero) {
            LogWarning("GroundAOE: Invalid target position");
            return false;
        }
        
        // Show targeting indicator
        SpawnEffect(skill.castEffectPrefab, targetPos, skill.radius * 0.5f);
        
        // Delay for impact
        StartCoroutine(DelayedGroundImpact(targetPos, skill));
        return true;
    }

    IEnumerator DelayedGroundImpact(Vector3 position, SkillSO skill) {
        yield return new WaitForSeconds(0.4f);
        
        SpawnEffect(skill.effectPrefab, position, skill.radius * 2);
        
        // Explosion
        if (skill.explodeOnImpact) {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, skill.explosionRadius, enemyLayer);
            foreach (var hit in hits) {
                if (hit != null && hit.CompareTag("Enemy")) {
                    ApplyDamage(hit.gameObject, skill);
                }
            }
        } else {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, skill.radius, enemyLayer);
            foreach (var hit in hits) {
                if (hit != null && hit.CompareTag("Enemy")) {
                    ApplyDamage(hit.gameObject, skill);
                    ApplyStatusEffects(hit.gameObject, skill);
                }
            }
        }
    }

    bool CastProjectile(SkillSO skill) {
        // Get prefab - prioritize skill's prefab, fallback to default
        GameObject prefab = skill.projectilePrefab ?? defaultProjectilePrefab;
        if (prefab == null) {
            LogError($"Projectile skill '{skill.skillName}' has no projectilePrefab!");
            return false;
        }
        
        Vector2 direction = GetAimDirection(skill);
        if (direction == Vector2.zero) {
            direction = player?.GetFacingDirection() ?? Vector2.right;
        }
        
        Vector2 spawnPos = castPoint != null ? (Vector2)castPoint.position : (Vector2)transform.position;
        
        // Spawn projectile with rotation facing direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject proj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, angle));
        
        if (proj == null) {
            LogError("Failed to instantiate projectile!");
            return false;
        }
        
        // Configure projectile
        var projectile = proj.GetComponent<Projectile>();
        if (projectile != null) {
            int baseDamage = player?.stats?.Damage ?? 10;
            int damage = skill.CalculateDamage(baseDamage);
            projectile.Initialize(direction, skill.range, damage, enemyLayer, skill);
            projectile.pierce = skill.pierceEnemies;
            projectile.homing = skill.homing;
            projectile.homingStrength = skill.homingStrength;
            projectile.explodeOnImpact = skill.explodeOnImpact;
            projectile.explosionRadius = skill.explosionRadius;
            
            if (verboseLogging) {
                Log($"Projectile initialized: dmg={damage}, range={skill.range}, pierce={skill.pierceEnemies}, homing={skill.homing}");
            }
        } else {
            LogWarning($"Projectile prefab '{prefab.name}' has no Projectile component!");
        }
        
        // Cast effect
        SpawnEffect(skill.castEffectPrefab, spawnPos, 1f);
        
        return true;
    }

    bool CastMelee(SkillSO skill) {
        // Melee attack - single target in front of player
        Vector2 origin = castPoint != null ? (Vector2)castPoint.position : (Vector2)transform.position;
        Vector2 direction = player?.GetFacingDirection() ?? Vector2.right;
        
        if (direction == Vector2.zero) direction = Vector2.right;
        
        // Raycast to find enemy in melee range
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, skill.range, enemyLayer);
        
        if (hit.collider != null) {
            // Hit an enemy
            ApplyDamage(hit.collider.gameObject, skill);
            ApplyStatusEffects(hit.collider.gameObject, skill);
            
            // Spawn hit effect
            SpawnEffect(skill.effectPrefab, hit.point, 1f);
            
            if (verboseLogging) {
                Log($"Melee attack hit {hit.collider.name} for damage");
            }
        } else {
            // Missed - spawn effect at max range anyway for visual feedback
            Vector2 missPos = origin + direction * skill.range;
            SpawnEffect(skill.effectPrefab, missPos, 0.5f);
            
            if (verboseLogging) {
                Log("Melee attack missed - no enemy in range");
            }
        }
        
        // Cast effect at player position
        SpawnEffect(skill.castEffectPrefab, origin, 1f);
        
        return true;
    }

    bool CastShield(SkillSO skill, int index) {
        GameObject prefab = skill.persistentEffectPrefab ?? defaultShieldEffectPrefab;
        GameObject effect = null;
        
        if (prefab != null) {
            effect = SpawnEffect(prefab, transform.position, 1f, transform);
            if (effect != null) {
                activeEffects.Add(effect);
            }
        }
        
        if (player != null) {
            player.SetShieldActive(true);
        }
        
        StartCoroutine(ShieldDuration(skill, index, effect));
        return true;
    }

    IEnumerator ShieldDuration(SkillSO skill, int index, GameObject effect) {
        float timer = skill.duration > 0 ? skill.duration : 5f;
        
        while (timer > 0) {
            timer -= Time.deltaTime;
            yield return null;
        }
        
        if (player != null) {
            player.SetShieldActive(false);
        }
        
        if (effect != null) {
            activeEffects.Remove(effect);
            Destroy(effect);
        }
    }

    bool CastDash(SkillSO skill) {
        Vector2 dashDir = player?.GetFacingDirection() ?? Vector2.right;
        if (dashDir == Vector2.zero) dashDir = Vector2.right;
        
        float dashDistance = skill.dashDistance > 0 ? skill.dashDistance : 5f;
        Vector2 targetPos = (Vector2)transform.position + dashDir * dashDistance;
        
        // Check if dash would hit wall (use obstacle layer or default to everything except enemies)
        LayerMask dashCollisionMask = obstacleLayer != 0 ? obstacleLayer : ~enemyLayer;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDir, dashDistance, dashCollisionMask);
        if (hit.collider != null) {
            targetPos = hit.point - dashDir * 0.5f; // Stop before wall
        }
        
        // Start dash
        StartCoroutine(PerformDash(targetPos, skill));
        return true;
    }

    IEnumerator PerformDash(Vector2 targetPos, SkillSO skill) {
        Vector2 startPos = transform.position;
        float distance = Vector2.Distance(startPos, targetPos);
        float duration = distance / Mathf.Max(skill.dashSpeed, 1f);
        float elapsed = 0;
        
        // Spawn trail effect
        if (skill.leaveTrail) {
            SpawnEffect(skill.effectPrefab, transform.position, 1f);
        }
        
        // Make invulnerable during dash
        if (skill.dashInvulnerable) {
            // Could add invulnerability flag here
        }
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        transform.position = targetPos;
        
        // Spawn end effect
        SpawnEffect(skill.effectPrefab, targetPos, 1f);
    }

    bool CastSummon(SkillSO skill) {
        if (skill.summonPrefab == null) {
            LogError($"Summon skill '{skill.skillName}' has no summonPrefab!");
            return false;
        }
        
        int count = Mathf.Max(1, skill.summonCount);
        
        for (int i = 0; i < count; i++) {
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector2 spawnPos = (Vector2)transform.position + offset;
            
            GameObject summon = Instantiate(skill.summonPrefab, spawnPos, Quaternion.identity);
            if (summon != null) {
                activeSummons.Add(summon);
                
                // Configure summon lifetime
                float lifetime = skill.summonDuration > 0 ? skill.summonDuration : 10f;
                Destroy(summon, lifetime);
            }
        }
        
        if (verboseLogging) Log($"Summoned {count} units");
        return true;
    }

    bool CastBuff(SkillSO skill, int index) {
        if (player == null || player.stats == null) {
            LogError("Cannot cast buff - player or stats is null!");
            return false;
        }
        
        // Apply stat modifiers
        player.stats.damageMod += Mathf.RoundToInt(skill.damageMultiplier - 1f);
        player.stats.critMod += skill.critChanceBonus;
        
        // Visual effect
        GameObject effect = SpawnEffect(skill.persistentEffectPrefab, transform.position, 1f, transform);
        if (effect != null) activeEffects.Add(effect);
        
        // Duration
        if (skill.duration > 0) {
            StartCoroutine(BuffDuration(skill, effect));
        }
        
        return true;
    }

    IEnumerator BuffDuration(SkillSO skill, GameObject effect) {
        yield return new WaitForSeconds(skill.duration);
        
        // Revert stats
        if (player != null && player.stats != null) {
            player.stats.damageMod -= Mathf.RoundToInt(skill.damageMultiplier - 1f);
            player.stats.critMod -= skill.critChanceBonus;
        }
        
        if (effect != null) {
            activeEffects.Remove(effect);
            Destroy(effect);
        }
    }

    bool CastHeal(SkillSO skill) {
        if (player == null) {
            LogError("Cannot cast heal - player is null!");
            return false;
        }
        
        int baseHeal = Mathf.Max(1, player.stats?.MaxHP / 10 ?? 10); // Base on 10% max HP
        int healAmount = skill.CalculateDamage(baseHeal);
        player.Heal(healAmount);
        
        SpawnEffect(skill.effectPrefab, transform.position, 1f, transform);
        
        if (verboseLogging) Log($"Healed for {healAmount} HP");
        return true;
    }

    bool CastChain(SkillSO skill) {
        // Find initial target
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, skill.range, enemyLayer);
        if (enemies.Length == 0) {
            LogWarning("Chain: No enemies in range");
            return false;
        }
        
        // Find closest enemy
        Transform closest = null;
        float closestDist = float.MaxValue;
        foreach (var enemy in enemies) {
            if (enemy != null && enemy.CompareTag("Enemy")) {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist) {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
        }
        
        if (closest == null) {
            LogWarning("Chain: No valid enemy target found");
            return false;
        }
        
        int damage = skill.CalculateDamage(player?.stats?.Damage ?? 10);
        int bounces = Mathf.Max(0, skill.chainBounces);
        
        StartCoroutine(ChainLightning(closest, skill, damage, bounces));
        return true;
    }

    IEnumerator ChainLightning(Transform target, SkillSO skill, int damage, int bounces) {
        HashSet<Transform> hitTargets = new HashSet<Transform>();
        Transform current = target;
        Vector2 sourcePos = transform.position;
        int currentDamage = damage;
        
        for (int i = 0; i <= bounces; i++) {
            if (current == null) break;
            if (hitTargets.Contains(current)) break;
            
            hitTargets.Add(current);
            
            // Deal damage
            var enemy = current.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.TakeDamage(currentDamage);
                SpawnEffect(skill.effectPrefab, current.position, 1f);
            }
            
            // Visual beam
            Debug.DrawLine(sourcePos, current.position, Color.yellow, 0.1f);
            
            // Find next target
            Collider2D[] nearby = Physics2D.OverlapCircleAll(current.position, skill.chainRange, enemyLayer);
            Transform next = null;
            foreach (var col in nearby) {
                if (col != null && !hitTargets.Contains(col.transform) && col.CompareTag("Enemy")) {
                    next = col.transform;
                    break;
                }
            }
            
            sourcePos = current.position;
            current = next;
            
            // Damage falloff
            currentDamage = Mathf.RoundToInt(currentDamage * skill.chainDamageFalloff);
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    bool CastTrap(SkillSO skill) {
        Vector3 pos = GetMouseWorldPosition();
        if (pos == Vector3.zero) {
            pos = transform.position + (Vector3)(player?.GetFacingDirection() ?? Vector2.right) * 2f;
        }
        
        GameObject trap = SpawnEffect(skill.effectPrefab, pos, 1f);
        if (trap != null) {
            float lifetime = skill.duration > 0 ? skill.duration : 30f;
            Destroy(trap, lifetime);
        }
        
        return true;
    }

    bool CastTeleport(SkillSO skill) {
        Vector3 target = GetMouseWorldPosition();
        if (target == Vector3.zero) {
            LogWarning("Teleport: No valid target position");
            return false;
        }
        
        // Check line of sight
        if (skill.requiresLineOfSight) {
            if (Physics2D.Linecast(transform.position, target, ~enemyLayer)) {
                ShowNotification("Target blocked!");
                return false;
            }
        }
        
        // Spawn effects
        SpawnEffect(skill.castEffectPrefab, transform.position, 1f);
        
        // Teleport
        transform.position = target;
        
        // Destination effect
        SpawnEffect(skill.effectPrefab, target, 1f);
        
        return true;
    }

    bool CastReflect(SkillSO skill, int index) {
        // Similar to shield but reflects projectiles
        return CastShield(skill, index); // Extend for reflect logic
    }

    #endregion

    #region Helper Methods

    void UpdateCooldowns() {
        for (int i = 0; i < cooldownTimers.Length; i++) {
            if (cooldownTimers[i] > 0) {
                cooldownTimers[i] -= Time.deltaTime;
                OnCooldownUpdated?.Invoke(i, cooldownTimers[i]);
            }
        }
    }

    void UpdateChanneledSkills() {
        for (int i = 0; i < isChanneling.Length; i++) {
            if (isChanneling[i]) {
                // Update channeling logic if needed
            }
        }
    }

    IEnumerator CastWithDelay(int index, SkillSO skill) {
        isCasting[index] = true;
        OnSkillCharging?.Invoke(index);
        
        // Show charge effect
        float elapsed = 0;
        while (elapsed < skill.castTime) {
            elapsed += Time.deltaTime;
            
            // Can cancel by moving if required
            if (!skill.canMoveWhileCasting && player != null && player.IsMoving()) {
                isCasting[index] = false;
                OnSkillReleased?.Invoke(index);
                Log($"Skill {index + 1} cast cancelled due to movement");
                yield break;
            }
            
            yield return null;
        }
        
        isCasting[index] = false;
        ExecuteSkill(index, skill);
    }

    bool StartChanneling(int index, SkillSO skill) {
        isChanneling[index] = true;
        StartCoroutine(ChannelingLoop(index, skill));
        return true;
    }

    IEnumerator ChannelingLoop(int index, SkillSO skill) {
        float tickTimer = 0;
        float tickRate = skill.tickRate > 0 ? skill.tickRate : 0.5f;
        
        while (isChanneling[index] && cooldownTimers[index] > 0) {
            tickTimer += Time.deltaTime;
            
            if (tickTimer >= tickRate) {
                tickTimer = 0;
                ApplyChannelingEffect(skill);
            }
            
            yield return null;
        }
        
        isChanneling[index] = false;
    }

    void ApplyChannelingEffect(SkillSO skill) {
        Vector2 origin = castPoint != null ? (Vector2)castPoint.position : (Vector2)transform.position;
        Vector2 dir = player?.GetFacingDirection() ?? Vector2.right;
        
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, skill.range, enemyLayer);
        if (hit.collider != null) {
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null) {
                ApplyDamage(hit.collider.gameObject, skill);
            }
        }
    }

    Vector3 GetTargetPosition(SkillSO skill) {
        return skill.targeting switch {
            SkillTargeting.Self => transform.position,
            SkillTargeting.MousePosition => GetMouseWorldPosition(),
            SkillTargeting.Directional => transform.position + (Vector3)(player?.GetFacingDirection() ?? Vector2.right) * skill.range,
            _ => GetMouseWorldPosition()
        };
    }

    Vector2 GetAimDirection(SkillSO skill) {
        if (skill.targeting == SkillTargeting.MousePosition) {
            Vector3 mousePos = GetMouseWorldPosition();
            if (mousePos == Vector3.zero) return Vector2.zero;
            return ((Vector2)mousePos - (Vector2)transform.position).normalized;
        }
        return player?.GetFacingDirection() ?? Vector2.right;
    }

    void ApplyDamage(GameObject target, SkillSO skill) {
        if (target == null) return;
        
        var enemy = target.GetComponent<Enemy>();
        if (enemy != null) {
            int baseDamage = player?.stats?.Damage ?? 10;
            int damage = skill.CalculateDamage(baseDamage);
            
            // Apply synergy damage multiplier
            if (SkillSynergyManager.Instance != null && SkillSynergyManager.Instance.IsSynergyActive()) {
                float synergyMultiplier = SkillSynergyManager.Instance.GetSynergyDamageMultiplier();
                damage = Mathf.RoundToInt(damage * synergyMultiplier);
            }
            
            // Check crit
            float critChance = (player?.stats?.Crit ?? 0) + skill.critChanceBonus;
            bool isCrit = Random.value < critChance;
            if (isCrit) {
                damage = Mathf.RoundToInt(damage * skill.critDamageMultiplier);
            }
            
            enemy.TakeDamage(damage);
        }
    }

    void ApplyStatusEffects(GameObject target, SkillSO skill) {
        if (target == null) return;
        
        var status = target.GetComponent<StatusEffect>();
        if (status == null) return;
        
        // Apply burn status
        if (skill.applyBurn) {
            var burnData = new StatusEffectData {
                type = StatusType.Burn,
                duration = skill.statusDuration > 0 ? skill.statusDuration : 3f,
                tickRate = 0.5f,
                damagePerTick = Mathf.Max(1, Mathf.RoundToInt((player?.stats?.Damage ?? 10) * 0.2f)),
                effectColor = Color.red
            };
            status.ApplyStatus(burnData);
        }
        
        // Apply freeze status
        if (skill.applyFreeze) {
            var freezeData = new StatusEffectData {
                type = StatusType.Freeze,
                duration = skill.statusDuration > 0 ? skill.statusDuration : 4f,
                slowAmount = 0.5f,
                effectColor = Color.cyan
            };
            status.ApplyStatus(freezeData);
        }
        
        // Apply poison status
        if (skill.applyPoison) {
            var poisonData = new StatusEffectData {
                type = StatusType.Poison,
                duration = skill.statusDuration > 0 ? skill.statusDuration : 5f,
                tickRate = 1f,
                damagePerTick = Mathf.Max(1, Mathf.RoundToInt((player?.stats?.Damage ?? 10) * 0.15f)),
                effectColor = Color.green
            };
            status.ApplyStatus(poisonData);
        }
        
        // Apply shock status
        if (skill.applyShock) {
            var shockData = new StatusEffectData {
                type = StatusType.Shock,
                duration = skill.statusDuration > 0 ? skill.statusDuration : 0.5f,
                effectColor = Color.yellow
            };
            status.ApplyStatus(shockData);
        }
    }

    GameObject SpawnEffect(GameObject prefab, Vector3 position, float scale, Transform parent = null) {
        if (prefab == null) return null;
        
        GameObject effect = Instantiate(prefab, position, Quaternion.identity, parent);
        effect.transform.localScale = Vector3.one * scale;
        
        // Auto destroy after particle duration if it's a particle system
        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null) {
            float lifetime = ps.main.duration + ps.main.startLifetime.constant;
            Destroy(effect, lifetime);
        }
        
        return effect;
    }

    void PlayCastEffects(SkillSO skill) {
        if (skill.castSound != null) {
            AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
        }
    }

    void ApplyScreenEffects(SkillSO skill) {
        if (skill.useScreenShake && Camera.main != null) {
            var camFollow = Camera.main.GetComponent<CameraFollow>();
            camFollow?.Shake(skill.screenShakeDuration, skill.screenShakeIntensity);
        }
        
        if (skill.useSlowMotion) {
            Time.timeScale = skill.slowMotionScale;
            StartCoroutine(RestoreTimeScale(skill.slowMotionDuration));
        }
    }

    IEnumerator RestoreTimeScale(float delay) {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
    }

    bool HasResources(SkillSO skill) {
        // TODO: Implement mana/health resource system
        return true;
    }

    void ConsumeResources(SkillSO skill) {
        // TODO: Deduct mana/health
    }

    void ShowResourceWarning() {
        UIManager.Instance?.ShowNotification("Not enough resources!");
    }

    void ShowNotification(string message) {
        UIManager.Instance?.ShowNotification(message);
    }

    void CleanupAllEffects() {
        foreach (var effect in activeEffects) {
            if (effect != null) Destroy(effect);
        }
        activeEffects.Clear();
        
        foreach (var summon in activeSummons) {
            if (summon != null) Destroy(summon);
        }
        activeSummons.Clear();
    }

    void EnsureArraySize<T>(ref T[] array, int size) {
        if (array == null) {
            array = new T[size];
            return;
        }
        if (array.Length != size) {
            System.Array.Resize(ref array, size);
        }
    }

    #endregion

    #region Logging Helpers

    void Log(string message) {
        if (verboseLogging) {
            Debug.Log($"[SkillCaster] {message}");
        }
    }
    
    void LogWarning(string message) {
        Debug.LogWarning($"[SkillCaster] {message}");
    }
    
    void LogError(string message) {
        Debug.LogError($"[SkillCaster] {message}");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Checks if a skill can be cast (index valid, skill assigned, not on cooldown, not casting).
    /// </summary>
    public bool CanCastSkill(int index) {
        if (index < 0 || index >= skills.Length) return false;
        if (skills[index] == null) return false;
        return cooldownTimers[index] <= 0 && !isCasting[index];
    }

    /// <summary>
    /// Gets the remaining cooldown for a skill slot.
    /// </summary>
    public float GetCooldownRemaining(int index) {
        if (index < 0 || index >= cooldownTimers.Length) return 0;
        return Mathf.Max(0, cooldownTimers[index]);
    }

    /// <summary>
    /// Gets the cooldown progress as a percentage (0 = ready, 1 = full cooldown).
    /// </summary>
    public float GetCooldownPercent(int index) {
        if (index < 0 || index >= skills.Length) return 1f;
        if (skills[index] == null) return 1f;
        float cooldownTime = skills[index].cooldownTime;
        if (cooldownTime <= 0) return 1f;
        return Mathf.Clamp01(1f - (cooldownTimers[index] / cooldownTime));
    }

    /// <summary>
    /// Checks if a skill is currently charging/casting.
    /// </summary>
    public bool IsCharging(int index) {
        if (index < 0 || index >= isCasting.Length) return false;
        return isCasting[index];
    }

    /// <summary>
    /// Checks if a skill is currently channeling.
    /// </summary>
    public bool IsChanneling(int index) {
        if (index < 0 || index >= isChanneling.Length) return false;
        return isChanneling[index];
    }

    /// <summary>
    /// Resets all skill cooldowns (for debug/testing).
    /// </summary>
    public void ResetAllCooldowns() {
        for (int i = 0; i < cooldownTimers.Length; i++) {
            cooldownTimers[i] = 0f;
        }
        Log("All cooldowns reset");
    }
    
    /// <summary>
    /// Gets the SkillSO for a slot (null if empty).
    /// </summary>
    public SkillSO GetSkill(int index) {
        if (index < 0 || index >= skills.Length) return null;
        return skills[index];
    }
    
    /// <summary>
    /// Returns true if all 4 skill slots have skills assigned.
    /// </summary>
    public bool HasAllSkillsAssigned() {
        if (skills == null || skills.Length != 4) return false;
        for (int i = 0; i < 4; i++) {
            if (skills[i] == null) return false;
        }
        return true;
    }
    
    /// <summary>
    /// Returns the number of skills currently assigned.
    /// </summary>
    public int GetAssignedSkillCount() {
        if (skills == null) return 0;
        int count = 0;
        for (int i = 0; i < skills.Length; i++) {
            if (skills[i] != null) count++;
        }
        return count;
    }

    #endregion

    #region Input Helpers

    Vector2 GetMouseWorldPosition() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) return Vector2.zero;
        }
        
        Vector2 mouseScreenPos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
        return worldPos;
    }

    #endregion
}

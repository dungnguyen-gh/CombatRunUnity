using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Object pool for enemies to reduce Instantiate/Destroy overhead.
/// Pre-instantiates enemies and reuses them instead of destroying.
/// Enhanced with proper state reset, direction facing, and cleanup support.
/// </summary>
public class EnemyPool : MonoBehaviour {
    public static EnemyPool Instance { get; private set; }
    
    [System.Serializable]
    public class PoolConfig {
        public string poolId;
        public GameObject prefab;
        public int initialSize = 10;
        public bool allowGrowth = true;
        public int maxSize = 50;
    }
    
    [Header("Pool Configuration")]
    public PoolConfig[] enemyPools;
    public Transform poolContainer;
    
    [Header("Spawn Settings")]
    public bool autoFacePlayer = true;
    public float spawnInvulnerabilityTime = 0.5f;
    
    // Dictionary mapping prefab to its pool
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, PoolConfig> prefabToConfig = new Dictionary<GameObject, PoolConfig>();
    private Dictionary<GameObject, GameObject> activeToPrefab = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }
        
        InitializePools();
    }
    
    void InitializePools() {
        if (poolContainer == null) {
            poolContainer = transform;
        }
        
        foreach (var config in enemyPools) {
            if (config.prefab == null) continue;
            
            Queue<GameObject> pool = new Queue<GameObject>();
            pools[config.prefab] = pool;
            prefabToConfig[config.prefab] = config;
            
            // Pre-instantiate
            for (int i = 0; i < config.initialSize; i++) {
                GameObject obj = CreateNewObject(config.prefab);
                pool.Enqueue(obj);
            }
            
            Debug.Log($"[EnemyPool] Initialized pool '{config.poolId}' with {config.initialSize} objects");
        }
    }
    
    GameObject CreateNewObject(GameObject prefab) {
        // IMPORTANT: Add StatusEffect BEFORE instantiating if prefab has Enemy
        // This prevents the "Creating missing StatusEffect" warning
        #if UNITY_EDITOR
        if (prefab.GetComponent<Enemy>() != null && prefab.GetComponent<StatusEffect>() == null) {
            prefab.AddComponent<StatusEffect>();
            Debug.Log($"[EnemyPool] Added StatusEffect to prefab: {prefab.name}");
        }
        #endif
        
        GameObject obj = Instantiate(prefab, poolContainer);
        obj.SetActive(false);
        
        // Ensure enemy has required components
        EnsureEnemyComponents(obj);
        
        return obj;
    }
    
    /// <summary>
    /// Ensures the enemy object has all required components for pooling.
    /// Called during pool initialization to pre-configure enemy prefabs.
    /// </summary>
    void EnsureEnemyComponents(GameObject obj) {
        // Add components in dependency order: StatusEffect -> Rigidbody2D -> Enemy
        // This prevents [RequireComponent] warnings
        
        // 1. StatusEffect (required by Enemy)
        if (obj.GetComponent<StatusEffect>() == null) {
            obj.AddComponent<StatusEffect>();
        }
        
        // 2. Rigidbody2D (required by Enemy)
        if (obj.GetComponent<Rigidbody2D>() == null) {
            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            #if UNITY_EDITOR
            Debug.Log($"[EnemyPool] Added missing Rigidbody2D to {obj.name}");
            #endif
        }
        
        // 3. Enemy (last, so its dependencies are already present)
        if (obj.GetComponent<Enemy>() == null) {
            obj.AddComponent<Enemy>();
            #if UNITY_EDITOR
            Debug.Log($"[EnemyPool] Added missing Enemy component to {obj.name}");
            #endif
        }
        
        // 4. Collider2D
        if (obj.GetComponent<Collider2D>() == null) {
            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            #if UNITY_EDITOR
            Debug.Log($"[EnemyPool] Added missing Collider2D to {obj.name}");
            #endif
        }
    }
    
    /// <summary>
    /// Gets an enemy from the pool or creates a new one.
    /// </summary>
    public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation) {
        if (prefab == null) {
            Debug.LogError("[EnemyPool] Cannot get from pool - prefab is null!");
            return null;
        }
        
        // Create pool for this prefab if it doesn't exist
        if (!pools.ContainsKey(prefab)) {
            CreateDynamicPool(prefab);
        }
        
        Queue<GameObject> pool = pools[prefab];
        PoolConfig config = prefabToConfig[prefab];
        
        GameObject obj = null;
        
        // Try to get from pool
        while (pool.Count > 0) {
            obj = pool.Dequeue();
            if (obj != null) {
                break; // Valid object found
            }
            // Object was destroyed, skip it
        }
        
        // Pool empty - create new if allowed
        if (obj == null) {
            int currentCount = CountActiveObjects(prefab) + pool.Count;
            if (config.allowGrowth && currentCount < config.maxSize) {
                obj = CreateNewObject(prefab);
                Debug.Log($"[EnemyPool] Grew pool '{config.poolId}' to {currentCount + 1}");
            } else {
                Debug.LogWarning($"[EnemyPool] Pool '{config.poolId}' exhausted and at max size!");
                return null;
            }
        }
        
        // Activate and position
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        
        // Track as active
        activeToPrefab[obj] = prefab;
        
        // Reset enemy state with full cleanup
        ResetEnemyState(obj, position);
        
        return obj;
    }
    
    /// <summary>
    /// Gets an enemy from the pool and makes it face the player.
    /// </summary>
    public GameObject GetFromPoolFacingPlayer(GameObject prefab, Vector3 position) {
        // Calculate rotation to face player
        Quaternion rotation = CalculateRotationTowardsPlayer(position);
        GameObject obj = GetFromPool(prefab, position, rotation);
        
        if (obj != null && autoFacePlayer) {
            // Additional facing direction setup
            FacePlayer(obj, position);
        }
        
        return obj;
    }
    
    /// <summary>
    /// Gets an enemy from the pool with explicit facing direction.
    /// </summary>
    public GameObject GetFromPoolWithFacing(GameObject prefab, Vector3 position, Vector2 facingDirection) {
        float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        GameObject obj = GetFromPool(prefab, position, rotation);
        
        if (obj != null) {
            // Apply facing to the enemy component
            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.FaceDirection(facingDirection);
            }
        }
        
        return obj;
    }
    
    /// <summary>
    /// Calculates rotation to face the player from a spawn position.
    /// </summary>
    Quaternion CalculateRotationTowardsPlayer(Vector3 spawnPosition) {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return Quaternion.identity;
        
        Vector2 direction = ((Vector2)player.transform.position - (Vector2)spawnPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, 0, angle);
    }
    
    /// <summary>
    /// Makes the enemy face towards the player.
    /// </summary>
    void FacePlayer(GameObject obj, Vector3 spawnPosition) {
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null) {
            // The enemy will auto-face player in its ResetFromPool method
            // But we can also force it here
            enemy.FacePlayer();
        } else {
            // Fallback: manually flip sprite based on spawn position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) {
                bool shouldFaceRight = player.transform.position.x > spawnPosition.x;
                sr.flipX = !shouldFaceRight;
            }
        }
    }
    
    void CreateDynamicPool(GameObject prefab) {
        Debug.Log($"[EnemyPool] Creating dynamic pool for {prefab.name}");
        
        PoolConfig config = new PoolConfig {
            poolId = prefab.name,
            prefab = prefab,
            initialSize = 5,
            allowGrowth = true,
            maxSize = 30
        };
        
        Queue<GameObject> pool = new Queue<GameObject>();
        pools[prefab] = pool;
        prefabToConfig[prefab] = config;
        
        // Pre-instantiate a few
        for (int i = 0; i < config.initialSize; i++) {
            GameObject obj = CreateNewObject(prefab);
            pool.Enqueue(obj);
        }
    }
    
    /// <summary>
    /// Resets enemy state completely for object pooling.
    /// This includes health, status effects, physics, and AI state.
    /// </summary>
    void ResetEnemyState(GameObject obj, Vector3 spawnPosition) {
        if (obj == null) return;
        
        // Reset Enemy component
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null) {
            // ResetFromPool handles most reset logic
            enemy.ResetFromPool();
        }
        
        // Reset EnemyAI component if present
        EnemyAI ai = obj.GetComponent<EnemyAI>();
        if (ai != null) {
            ai.ResetAI();
        }
        
        // Reset StatusEffect component
        StatusEffect status = obj.GetComponent<StatusEffect>();
        if (status != null) {
            status.ClearStatus();
        }
        
        // Reset physics
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.simulated = true;
        }
        
        // Reset all colliders
        Collider2D[] colliders = obj.GetComponents<Collider2D>();
        foreach (var col in colliders) {
            if (col != null) {
                col.enabled = true;
            }
        }
        
        // Reset all renderers
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers) {
            if (renderer != null) {
                renderer.color = Color.white;
            }
        }
        
        // Stop all particle effects
        ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles) {
            if (ps != null) {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        // Ensure transform is clean
        obj.transform.localScale = Vector3.one;
        
        // Apply spawn invulnerability if configured
        if (spawnInvulnerabilityTime > 0 && enemy != null) {
            // Could add invulnerability logic here
        }
    }
    
    /// <summary>
    /// Returns an enemy to the pool.
    /// Properly cleans up all coroutines, effects, and state.
    /// </summary>
    public void ReturnToPool(GameObject obj) {
        if (obj == null) return;
        
        if (!activeToPrefab.TryGetValue(obj, out GameObject prefab)) {
            // Object wasn't from pool - destroy it
            Debug.LogWarning($"[EnemyPool] Object {obj.name} not from pool, destroying");
            Destroy(obj);
            return;
        }
        
        // Stop any running coroutines on the object
        StopObjectCoroutines(obj);
        
        // Cleanup enemy components before deactivation
        CleanupEnemyForPool(obj);
        
        // Deactivate
        obj.SetActive(false);
        obj.transform.SetParent(poolContainer);
        
        // Reset transform
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        
        // Return to pool
        if (pools.ContainsKey(prefab)) {
            pools[prefab].Enqueue(obj);
        }
        
        // Remove from active tracking
        activeToPrefab.Remove(obj);
    }
    
    /// <summary>
    /// Stops all coroutines running on an object and its components.
    /// </summary>
    void StopObjectCoroutines(GameObject obj) {
        // Stop coroutines on the object itself
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null) {
            // Enemy handles its own coroutine cleanup in CleanupForPool
        }
        
        EnemyAI ai = obj.GetComponent<EnemyAI>();
        if (ai != null) {
            ai.StopAllCoroutines();
        }
        
        StatusEffect status = obj.GetComponent<StatusEffect>();
        if (status != null) {
            status.StopAllCoroutines();
        }
    }
    
    /// <summary>
    /// Cleans up enemy components before returning to pool.
    /// </summary>
    void CleanupEnemyForPool(GameObject obj) {
        // Cleanup Enemy
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null) {
            // Disable the component to prevent Update calls
            enemy.enabled = false;
        }
        
        // Cleanup AI
        EnemyAI ai = obj.GetComponent<EnemyAI>();
        if (ai != null) {
            ai.enabled = false;
        }
        
        // Clear status effects
        StatusEffect status = obj.GetComponent<StatusEffect>();
        if (status != null) {
            status.ClearStatus();
        }
        
        // Reset physics
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.simulated = false; // Disable physics while in pool
        }
        
        // Disable colliders
        Collider2D[] colliders = obj.GetComponents<Collider2D>();
        foreach (var col in colliders) {
            if (col != null) col.enabled = false;
        }
        
        // Clear any active visual effects
        Transform effectsParent = obj.transform.Find("ActiveEffects");
        if (effectsParent != null) {
            Destroy(effectsParent.gameObject);
        }
        
        // Clear any active projectiles or summons owned by this enemy
        // (This would require tracking ownership, simplified here)
    }
    
    /// <summary>
    /// Returns an enemy to the pool after a delay.
    /// </summary>
    public void ReturnToPoolAfterDelay(GameObject obj, float delay) {
        if (obj == null) return;
        
        // Cancel any existing return coroutine for this object
        if (activeCoroutines.ContainsKey(obj)) {
            if (activeCoroutines[obj] != null) {
                StopCoroutine(activeCoroutines[obj]);
            }
            activeCoroutines.Remove(obj);
        }
        
        Coroutine coroutine = StartCoroutine(ReturnAfterDelayCoroutine(obj, delay));
        activeCoroutines[obj] = coroutine;
    }
    
    System.Collections.IEnumerator ReturnAfterDelayCoroutine(GameObject obj, float delay) {
        yield return new WaitForSeconds(delay);
        
        // Remove from tracking before returning
        if (activeCoroutines.ContainsKey(obj)) {
            activeCoroutines.Remove(obj);
        }
        
        ReturnToPool(obj);
    }
    
    int CountActiveObjects(GameObject prefab) {
        int count = 0;
        foreach (var kvp in activeToPrefab) {
            if (kvp.Value == prefab && kvp.Key != null && kvp.Key.activeInHierarchy) {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Pre-warms a pool with additional objects.
    /// </summary>
    public void PreWarmPool(GameObject prefab, int count) {
        if (!pools.ContainsKey(prefab)) {
            CreateDynamicPool(prefab);
        }
        
        Queue<GameObject> pool = pools[prefab];
        for (int i = 0; i < count; i++) {
            GameObject obj = CreateNewObject(prefab);
            pool.Enqueue(obj);
        }
    }
    
    /// <summary>
    /// Gets statistics about a pool.
    /// </summary>
    public void GetPoolStats(GameObject prefab, out int available, out int active, out int total) {
        available = pools.ContainsKey(prefab) ? pools[prefab].Count : 0;
        active = CountActiveObjects(prefab);
        total = available + active;
    }
    
    /// <summary>
    /// Clears all active coroutines and returns all objects to their pools.
    /// Call this when changing scenes or resetting the game.
    /// </summary>
    public void ClearAllPools() {
        // Stop all active return coroutines
        foreach (var kvp in activeCoroutines) {
            if (kvp.Value != null) {
                StopCoroutine(kvp.Value);
            }
        }
        activeCoroutines.Clear();
        
        // Return all active objects to pools
        List<GameObject> activeObjects = new List<GameObject>(activeToPrefab.Keys);
        foreach (var obj in activeObjects) {
            if (obj != null) {
                ReturnToPool(obj);
            }
        }
    }
    
    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called when a new scene is loaded.
    /// Clears pools when leaving game scene.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // Check if we're leaving the game scene
        if (!IsGameScene(scene.name)) {
            Debug.Log("[EnemyPool] Leaving game scene - clearing all pools");
            ClearAllPools();
        }
    }

    /// <summary>
    /// Checks if scene name indicates a game scene.
    /// </summary>
    bool IsGameScene(string sceneName) {
        return sceneName.Contains("Game") || sceneName.Contains("Level") || sceneName == "SampleScene";
    }

    void OnDestroy() {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Clear all pools
        ClearAllPools();
        
        pools.Clear();
        prefabToConfig.Clear();
        activeToPrefab.Clear();
        activeCoroutines.Clear();
    }
}

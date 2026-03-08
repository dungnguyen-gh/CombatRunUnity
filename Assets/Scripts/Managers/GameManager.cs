using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager that handles game state, wave spawning, and game over/restart logic.
/// Persistent singleton that survives scene loads.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameActive = false;
    public int currentWave = 0;
    public int enemiesKilled = 0;

    [Header("Spawning")]
    public Transform[] spawnPoints;
    public GameObject[] enemyPrefabs;
    public float timeBetweenWaves = 5f;
    public int enemiesPerWave = 5;
    public int maxEnemies = 20;

    [Header("Boss")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public int bossWave = 5;

    [Header("References")]
    public PlayerController player;
    public Transform enemyContainer;
    public EnemyPool enemyPool;

    [Header("Player Lives")]
    public int startingLives = 3;

    private float waveTimer;
    private int currentEnemyCount;
    private bool bossSpawned = false;
    private bool isGameOver = false;

    // Events
    public System.Action<int> OnWaveStarted;
    public System.Action OnBossSpawned;
    public System.Action OnGameWon;
    public System.Action OnGameOver;
    public System.Action OnGameRestarted;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        // Don't auto-start - wait for SceneTransitionManager to call StartGame
        // This allows proper multi-scene setup
        if (IsInGameScene()) {
            InitializeForGameScene();
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
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == SceneTransitionManager.Instance?.gameSceneName) {
            InitializeForGameScene();
        } else {
            // Not in game scene - cleanup game state
            CleanupGameState();
        }
    }

    /// <summary>
    /// Checks if currently in the game scene.
    /// </summary>
    bool IsInGameScene() {
        if (SceneTransitionManager.Instance != null) {
            return SceneTransitionManager.Instance.IsInGameScene();
        }
        // Fallback if STM not available
        return SceneManager.GetActiveScene().name.Contains("Game") || 
               SceneManager.GetActiveScene().name.Contains("Scene");
    }

    /// <summary>
    /// Initializes the game manager for the game scene.
    /// Called when entering the game scene.
    /// </summary>
    void InitializeForGameScene() {
        Debug.Log("[GameManager] Initializing for game scene");
        
        // Clean up any old references
        CleanupPlayerReferences();
        
        // Find new player reference
        FindPlayerReference();
        
        // Find scene-specific references
        FindSceneReferences();
        
        // Start the game
        StartGame();
    }

    /// <summary>
    /// Finds scene-specific references (spawn points, containers, etc.)
    /// </summary>
    void FindSceneReferences() {
        // Find spawn points if not assigned
        if (spawnPoints == null || spawnPoints.Length == 0) {
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints = new Transform[spawnPointObjects.Length];
            for (int i = 0; i < spawnPointObjects.Length; i++) {
                spawnPoints[i] = spawnPointObjects[i].transform;
            }
        }

        // Find enemy container if not assigned
        if (enemyContainer == null) {
            GameObject container = GameObject.FindWithTag("EnemyContainer");
            if (container != null) {
                enemyContainer = container.transform;
            }
        }

        // Find boss spawn point if not assigned
        if (bossSpawnPoint == null) {
            GameObject bossSpawn = GameObject.FindWithTag("BossSpawn");
            if (bossSpawn != null) {
                bossSpawnPoint = bossSpawn.transform;
            }
        }
    }

    /// <summary>
    /// Cleans up player event subscriptions.
    /// </summary>
    void CleanupPlayerReferences() {
        if (player != null) {
            player.OnPlayerDied -= OnPlayerDied;
            player.OnPlayerRevived -= OnPlayerRevived;
            player = null;
        }
    }

    /// <summary>
    /// Cleans up game state when leaving the game scene.
    /// </summary>
    void CleanupGameState() {
        Debug.Log("[GameManager] Cleaning up game state");
        
        isGameActive = false;
        isGameOver = false;
        
        CleanupPlayerReferences();
        
        // Return all enemies to pool
        if (enemyPool != null) {
            enemyPool.ClearAllPools();
        }
    }

    void Update() {
        if (!isGameActive || isGameOver) return;

        // Wave spawning
        if (currentEnemyCount <= 0 && !bossSpawned) {
            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0) {
                if (currentWave + 1 >= bossWave) {
                    SpawnBoss();
                } else {
                    StartNextWave();
                }
            }
        }
    }

    /// <summary>
    /// Finds the player reference in the scene.
    /// </summary>
    void FindPlayerReference() {
        if (player == null) {
            player = FindFirstObjectByType<PlayerController>();
        }
        
        // Subscribe to player events
        if (player != null) {
            player.OnPlayerDied += OnPlayerDied;
            player.OnPlayerRevived += OnPlayerRevived;
        }
    }

    void OnDestroy() {
        // Unsubscribe from player events
        if (player != null) {
            player.OnPlayerDied -= OnPlayerDied;
            player.OnPlayerRevived -= OnPlayerRevived;
        }
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void StartGame() {
        isGameActive = true;
        isGameOver = false;
        currentWave = 0;
        enemiesKilled = 0;
        bossSpawned = false;
        waveTimer = 2f; // Initial delay
        
        // Find player if not assigned
        FindPlayerReference();
        
        // Reset player lives if player exists
        if (player != null) {
            player.currentLives = startingLives;
            player.maxLives = startingLives;
        }
        
        Debug.Log("Game Started!");
    }

    /// <summary>
    /// Called when the player dies (but might still have lives left).
    /// </summary>
    void OnPlayerDied() {
        Debug.Log($"Player died! Lives remaining: {(player != null ? player.currentLives : 0)}");
        
        // If player has no lives left, game over will be handled by the player's GameOver coroutine
    }

    /// <summary>
    /// Called when the player is revived.
    /// </summary>
    void OnPlayerRevived() {
        Debug.Log("Player revived!");
        
        // Show notification
        UIManager.Instance?.ShowNotification($"Wave {currentWave} - Fight!");
    }

    /// <summary>
    /// Called when the player has no lives left and game is truly over.
    /// This is called from PlayerController's GameOver coroutine.
    /// </summary>
    public void OnPlayerGameOver() {
        if (isGameOver) return; // Prevent multiple calls
        
        isGameOver = true;
        isGameActive = false;
        
        Debug.Log("GAME OVER!");
        
        // Save progress
        SaveProgress();
        
        // Submit daily run result if applicable
        SubmitDailyRunResult(false);
        
        // Show game over UI with stats
        UIManager.Instance?.ShowGameOver(enemiesKilled, currentWave);
        
        // Notify listeners
        OnGameOver?.Invoke();
    }

    void StartNextWave() {
        currentWave++;
        OnWaveStarted?.Invoke(currentWave);

        int enemyCount = enemiesPerWave + (currentWave * 2);
        SpawnWave(enemyCount);
        
        waveTimer = timeBetweenWaves;
        
        // Show wave notification
        UIManager.Instance?.ShowNotification($"Wave {currentWave}");
        
        Debug.Log($"Wave {currentWave} started with {enemyCount} enemies!");
    }

    void SpawnWave(int count) {
        for (int i = 0; i < count; i++) {
            if (currentEnemyCount >= maxEnemies) break;
            
            SpawnEnemy();
        }
    }

    void SpawnEnemy() {
        if (spawnPoints.Length == 0 || enemyPrefabs.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = null;
        
        // Try to get from pool first
        if (enemyPool != null) {
            enemy = enemyPool.GetFromPool(enemyPrefab, spawnPoint.position, Quaternion.identity);
        }
        
        // Fallback to Instantiate if pool fails
        if (enemy == null) {
            enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, enemyContainer);
        }
        
        if (enemy != null) {
            currentEnemyCount++;

            // Subscribe to death
            var enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null) {
                enemyScript.OnDeath -= OnEnemyDeath; // Prevent double subscription
                enemyScript.OnDeath += OnEnemyDeath;
            }
        }
    }

    void SpawnBoss() {
        bossSpawned = true;
        OnBossSpawned?.Invoke();

        if (bossPrefab == null || bossSpawnPoint == null) return;

        GameObject boss = null;
        
        // Try to get from pool first
        if (enemyPool != null) {
            boss = enemyPool.GetFromPool(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
        }
        
        // Fallback to Instantiate
        if (boss == null) {
            boss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity, enemyContainer);
        }
        
        if (boss != null) {
            var bossScript = boss.GetComponent<Enemy>();
            if (bossScript != null) {
                bossScript.OnDeath -= OnBossDeath; // Prevent double subscription
                bossScript.OnDeath += OnBossDeath;
            }

            UIManager.Instance?.ShowNotification("BOSS APPEARS!");
            Debug.Log("Boss spawned!");
        }
    }

    void OnEnemyDeath(Enemy enemy) {
        currentEnemyCount--;
        enemiesKilled++;
        
        // FIX: Unsubscribe to prevent memory leak
        enemy.OnDeath -= OnEnemyDeath;
        
        // Return to pool after delay (allows death animation to play)
        if (enemyPool != null) {
            enemyPool.ReturnToPoolAfterDelay(enemy.gameObject, 1f);
        } else {
            Destroy(enemy.gameObject, 1f);
        }
    }

    void OnBossDeath(Enemy boss) {
        isGameOver = true;
        isGameActive = false;
        
        OnGameWon?.Invoke();
        UIManager.Instance?.ShowNotification("VICTORY!");
        
        // Save progress
        SaveProgress();
        
        // Submit daily run result if applicable (victory!)
        SubmitDailyRunResult(true);
        
        // FIX: Unsubscribe to prevent memory leak
        boss.OnDeath -= OnBossDeath;
        
        // Return to pool or destroy
        if (enemyPool != null) {
            enemyPool.ReturnToPoolAfterDelay(boss.gameObject, 2f);
        } else {
            Destroy(boss.gameObject, 2f);
        }
        
        // Show victory screen or return to menu
        Invoke(nameof(ReturnToMenu), 5f);
    }

    /// <summary>
    /// Legacy GameOver method - redirects to OnPlayerGameOver for consistency.
    /// </summary>
    public void GameOver() {
        OnPlayerGameOver();
    }

    /// <summary>
    /// Restarts the game by reloading the scene.
    /// Called by UIManager when Play Again button is clicked.
    /// </summary>
    public void RestartGame() {
        Debug.Log("[GameManager] Restarting game...");
        
        // Reset state
        isGameOver = false;
        isGameActive = false; // Will be set true after scene load
        currentWave = 0;
        enemiesKilled = 0;
        bossSpawned = false;
        waveTimer = 2f;
        
        // Notify listeners
        OnGameRestarted?.Invoke();
        
        // Use SceneTransitionManager for proper cleanup
        if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.RestartCurrentScene();
        } else {
            // Fallback
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMenu() {
        Debug.Log("[GameManager] Returning to main menu...");
        
        // Save progress if needed
        SaveProgress();
        
        // Use SceneTransitionManager
        if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.GoToMainMenu();
        }
    }

    /// <summary>
    /// Saves current run progress.
    /// Called when leaving game or on game over.
    /// </summary>
    void SaveProgress() {
        // Save cumulative stats via SaveLoadManager
        SaveLoadManager.Instance?.SaveCumulativeStats(enemiesKilled, currentWave, player?.gold ?? 0);

        // Save weapon mastery
        SaveLoadManager.Instance?.SaveAllWeaponMastery(WeaponMasteryManager.Instance);

        // Save active run state (for Continue functionality)
        if (isGameActive && !isGameOver && player != null && player.currentLives > 0) {
            string weaponId = InventoryManager.Instance?.equippedWeapon?.itemId ?? "";
            string armorId = InventoryManager.Instance?.equippedArmor?.itemId ?? "";
            
            SaveLoadManager.Instance?.SaveActiveRun(
                currentWave,
                enemiesKilled,
                player.gold,
                player.currentLives,
                weaponId,
                armorId
            );
        } else {
            SaveLoadManager.Instance?.ClearActiveRun();
        }
    }

    /// <summary>
    /// Submits daily run result to the leaderboard.
    /// </summary>
    void SubmitDailyRunResult(bool victory) {
        DailyRunManager dailyRun = DailyRunManager.Instance;
        if (dailyRun == null || !dailyRun.isDailyRun) return;

        var result = new DailyRunResult {
            dateTimestamp = System.DateTime.Now.ToBinary(),
            waveReached = currentWave,
            enemiesKilled = enemiesKilled,
            goldCollected = player?.gold ?? 0,
            victory = victory,
            modifiers = dailyRun.currentModifiers
        };

        // Add to DailyRunManager's in-memory list
        dailyRun.SubmitRunResult(currentWave, enemiesKilled, player?.gold ?? 0, victory);

        // Save to persistent storage
        SaveLoadManager.Instance?.SaveDailyRunResult(result);

        Debug.Log($"[GameManager] Submitted daily run result: Wave {currentWave}, Victory: {victory}");
    }

    /// <summary>
    /// Gets the current game over state.
    /// </summary>
    public bool IsGameOver() => isGameOver;

    /// <summary>
    /// Gets the current game active state.
    /// </summary>
    public bool IsGameActive() => isGameActive;

    /// <summary>
    /// Gets the number of enemies killed.
    /// </summary>
    public int GetEnemiesKilled() => enemiesKilled;

    /// <summary>
    /// Gets the current wave number.
    /// </summary>
    public int GetCurrentWave() => currentWave;
}

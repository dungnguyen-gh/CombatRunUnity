using UnityEngine;
using UnityEngine.SceneManagement;

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

    private float waveTimer;
    private int currentEnemyCount;
    private bool bossSpawned = false;

    // Events
    public System.Action<int> OnWaveStarted;
    public System.Action OnBossSpawned;
    public System.Action OnGameWon;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        
        StartGame();
    }

    void Update() {
        if (!isGameActive) return;

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

    public void StartGame() {
        isGameActive = true;
        currentWave = 0;
        enemiesKilled = 0;
        bossSpawned = false;
        waveTimer = 2f; // Initial delay
    }

    void StartNextWave() {
        currentWave++;
        OnWaveStarted?.Invoke(currentWave);

        int enemyCount = enemiesPerWave + (currentWave * 2);
        SpawnWave(enemyCount);
        
        waveTimer = timeBetweenWaves;
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

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, enemyContainer);
        currentEnemyCount++;

        // Subscribe to death
        var enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null) {
            enemyScript.OnDeath += OnEnemyDeath;
        }
    }

    void SpawnBoss() {
        bossSpawned = true;
        OnBossSpawned?.Invoke();

        if (bossPrefab == null || bossSpawnPoint == null) return;

        GameObject boss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity, enemyContainer);
        
        var bossScript = boss.GetComponent<Enemy>();
        if (bossScript != null) {
            bossScript.OnDeath += OnBossDeath;
        }

        UIManager.Instance?.ShowNotification("BOSS APPEARS!");
    }

    void OnEnemyDeath(Enemy enemy) {
        currentEnemyCount--;
        enemiesKilled++;
        // FIX: Unsubscribe to prevent memory leak
        enemy.OnDeath -= OnEnemyDeath;
    }

    void OnBossDeath(Enemy boss) {
        OnGameWon?.Invoke();
        UIManager.Instance?.ShowNotification("VICTORY!");
        // FIX: Unsubscribe to prevent memory leak
        boss.OnDeath -= OnBossDeath;
        
        // Show victory screen or return to menu
        Invoke(nameof(ReturnToMenu), 5f);
    }

    public void GameOver() {
        isGameActive = false;
        UIManager.Instance?.ShowNotification("GAME OVER");
        Invoke(nameof(RestartGame), 3f);
    }

    void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ReturnToMenu() {
        // Load main menu scene if available
        // SceneManager.LoadScene("MainMenu");
        RestartGame();
    }
}

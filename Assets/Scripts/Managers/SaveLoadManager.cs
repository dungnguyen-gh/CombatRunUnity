using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central save/load manager for all persistent game data.
/// Uses PlayerPrefs for simplicity (can be upgraded to JSON/Binary).
/// </summary>
public class SaveLoadManager : MonoBehaviour {
    public static SaveLoadManager Instance { get; private set; }

    [Header("Debug")]
    public bool logSaveLoad = false;

    // Keys
    private const string KEY_VERSION = "SaveVersion";
    private const string KEY_TOTAL_KILLS = "TotalKills";
    private const string KEY_HIGH_WAVE = "HighWave";
    private const string KEY_TOTAL_GOLD = "TotalGoldCollected";
    private const string KEY_HAS_ACTIVE_RUN = "HasActiveRun";
    private const string KEY_CURRENT_RUN_WAVE = "CurrentRunWave";
    private const string KEY_CURRENT_RUN_KILLS = "CurrentRunKills";
    private const string KEY_CURRENT_RUN_GOLD = "CurrentRunGold";
    private const string KEY_CURRENT_RUN_LIVES = "CurrentRunLives";
    private const string KEY_PLAYER_WEAPON = "PlayerWeapon";
    private const string KEY_PLAYER_ARMOR = "PlayerArmor";
    private const string KEY_MASTERY_PREFIX = "Mastery_";
    private const string KEY_OPTIONS_MUSIC = "MusicVolume";
    private const string KEY_OPTIONS_SFX = "SFXVolume";
    private const string KEY_OPTIONS_FULLSCREEN = "Fullscreen";

    private const int CURRENT_VERSION = 1;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CheckVersion();
        } else {
            Destroy(gameObject);
        }
    }

    #region Version Management

    void CheckVersion() {
        int savedVersion = PlayerPrefs.GetInt(KEY_VERSION, 0);
        if (savedVersion < CURRENT_VERSION) {
            MigrateSaveData(savedVersion);
            PlayerPrefs.SetInt(KEY_VERSION, CURRENT_VERSION);
        }
    }

    void MigrateSaveData(int oldVersion) {
        Debug.Log($"[SaveLoadManager] Migrating save data from version {oldVersion} to {CURRENT_VERSION}");
        // Add migration logic here if needed
    }

    #endregion

    #region Stats Persistence

    /// <summary>
    /// Saves cumulative stats (called on game over/exit).
    /// </summary>
    public void SaveCumulativeStats(int runKills, int runWave, int runGold) {
        // Add to totals
        int totalKills = PlayerPrefs.GetInt(KEY_TOTAL_KILLS, 0);
        PlayerPrefs.SetInt(KEY_TOTAL_KILLS, totalKills + runKills);

        int totalGold = PlayerPrefs.GetInt(KEY_TOTAL_GOLD, 0);
        PlayerPrefs.SetInt(KEY_TOTAL_GOLD, totalGold + runGold);

        // Update high score
        int highWave = PlayerPrefs.GetInt(KEY_HIGH_WAVE, 0);
        if (runWave > highWave) {
            PlayerPrefs.SetInt(KEY_HIGH_WAVE, runWave);
        }

        PlayerPrefs.Save();

        if (logSaveLoad) {
            Debug.Log($"[SaveLoadManager] Saved cumulative stats: +{runKills} kills, wave {runWave}");
        }
    }

    /// <summary>
    /// Gets cumulative stats for display.
    /// </summary>
    public CumulativeStats GetCumulativeStats() {
        return new CumulativeStats {
            totalKills = PlayerPrefs.GetInt(KEY_TOTAL_KILLS, 0),
            highWave = PlayerPrefs.GetInt(KEY_HIGH_WAVE, 0),
            totalGold = PlayerPrefs.GetInt(KEY_TOTAL_GOLD, 0)
        };
    }

    #endregion

    #region Active Run Persistence

    /// <summary>
    /// Saves current run state (for Continue functionality).
    /// </summary>
    public void SaveActiveRun(int wave, int kills, int gold, int lives, string weaponId, string armorId) {
        PlayerPrefs.SetInt(KEY_HAS_ACTIVE_RUN, 1);
        PlayerPrefs.SetInt(KEY_CURRENT_RUN_WAVE, wave);
        PlayerPrefs.SetInt(KEY_CURRENT_RUN_KILLS, kills);
        PlayerPrefs.SetInt(KEY_CURRENT_RUN_GOLD, gold);
        PlayerPrefs.SetInt(KEY_CURRENT_RUN_LIVES, lives);
        
        if (!string.IsNullOrEmpty(weaponId))
            PlayerPrefs.SetString(KEY_PLAYER_WEAPON, weaponId);
        if (!string.IsNullOrEmpty(armorId))
            PlayerPrefs.SetString(KEY_PLAYER_ARMOR, armorId);

        PlayerPrefs.Save();

        if (logSaveLoad) {
            Debug.Log($"[SaveLoadManager] Saved active run: Wave {wave}, {lives} lives");
        }
    }

    /// <summary>
    /// Loads active run state.
    /// </summary>
    public ActiveRunData LoadActiveRun() {
        if (!HasActiveRun()) return null;

        return new ActiveRunData {
            wave = PlayerPrefs.GetInt(KEY_CURRENT_RUN_WAVE, 1),
            kills = PlayerPrefs.GetInt(KEY_CURRENT_RUN_KILLS, 0),
            gold = PlayerPrefs.GetInt(KEY_CURRENT_RUN_GOLD, 0),
            lives = PlayerPrefs.GetInt(KEY_CURRENT_RUN_LIVES, 3),
            weaponId = PlayerPrefs.GetString(KEY_PLAYER_WEAPON, ""),
            armorId = PlayerPrefs.GetString(KEY_PLAYER_ARMOR, "")
        };
    }

    /// <summary>
    /// Checks if there's an active run to continue.
    /// </summary>
    public bool HasActiveRun() {
        return PlayerPrefs.GetInt(KEY_HAS_ACTIVE_RUN, 0) == 1;
    }

    /// <summary>
    /// Clears active run (called on game over or new game).
    /// </summary>
    public void ClearActiveRun() {
        PlayerPrefs.SetInt(KEY_HAS_ACTIVE_RUN, 0);
        PlayerPrefs.DeleteKey(KEY_CURRENT_RUN_WAVE);
        PlayerPrefs.DeleteKey(KEY_CURRENT_RUN_KILLS);
        PlayerPrefs.DeleteKey(KEY_CURRENT_RUN_GOLD);
        PlayerPrefs.DeleteKey(KEY_CURRENT_RUN_LIVES);
        PlayerPrefs.DeleteKey(KEY_PLAYER_WEAPON);
        PlayerPrefs.DeleteKey(KEY_PLAYER_ARMOR);
        PlayerPrefs.Save();

        if (logSaveLoad) {
            Debug.Log("[SaveLoadManager] Cleared active run");
        }
    }

    #endregion

    #region Weapon Mastery Persistence

    /// <summary>
    /// Saves weapon mastery data.
    /// </summary>
    public void SaveWeaponMastery(string weaponType, WeaponMasteryData data) {
        string key = KEY_MASTERY_PREFIX + weaponType;
        PlayerPrefs.SetInt(key + "_Level", data.masteryLevel);
        PlayerPrefs.SetInt(key + "_Kills", data.killCount);
        PlayerPrefs.Save();

        if (logSaveLoad) {
            Debug.Log($"[SaveLoadManager] Saved {weaponType} mastery: Level {data.masteryLevel}");
        }
    }

    /// <summary>
    /// Loads weapon mastery data.
    /// </summary>
    public WeaponMasteryData LoadWeaponMastery(string weaponType) {
        string key = KEY_MASTERY_PREFIX + weaponType;
        var data = new WeaponMasteryData {
            weaponType = weaponType,
            masteryLevel = PlayerPrefs.GetInt(key + "_Level", 0)
        };
        // Set kill count via reflection or add kills one by one
        int kills = PlayerPrefs.GetInt(key + "_Kills", 0);
        for (int i = 0; i < kills; i++) {
            data.AddKill();
        }
        return data;
    }

    /// <summary>
    /// Saves all weapon masteries.
    /// </summary>
    public void SaveAllWeaponMastery(WeaponMasteryManager manager) {
        if (manager == null) return;

        // Save all weapon types
        string[] weaponTypes = new string[] { "Sword", "Axe", "Spear", "Dagger", "Bow", "Staff" };
        foreach (string type in weaponTypes) {
            var data = manager.GetMastery(type);
            SaveWeaponMastery(type, data);
        }

        if (logSaveLoad) {
            Debug.Log("[SaveLoadManager] Saved all weapon masteries");
        }
    }

    /// <summary>
    /// Loads all weapon masteries into the manager.
    /// </summary>
    public void LoadAllWeaponMastery(WeaponMasteryManager manager) {
        if (manager == null) return;

        // Load all weapon types
        string[] weaponTypes = new string[] { "Sword", "Axe", "Spear", "Dagger", "Bow", "Staff" };
        foreach (string type in weaponTypes) {
            var data = LoadWeaponMastery(type);
            // Apply to manager (would need to add a method to WeaponMasteryManager)
        }

        if (logSaveLoad) {
            Debug.Log("[SaveLoadManager] Loaded all weapon masteries");
        }
    }

    #endregion

    #region Daily Run Leaderboard

    /// <summary>
    /// Saves a daily run result to the leaderboard.
    /// </summary>
    public void SaveDailyRunResult(DailyRunResult result) {
        // Get existing results
        List<DailyRunResult> results = LoadDailyRunLeaderboard();
        
        // Add new result
        results.Add(result);
        
        // Sort by wave, then kills
        results.Sort((a, b) => {
            if (a.waveReached != b.waveReached)
                return b.waveReached.CompareTo(a.waveReached);
            return b.enemiesKilled.CompareTo(a.enemiesKilled);
        });

        // Keep only top 10
        while (results.Count > 10) {
            results.RemoveAt(results.Count - 1);
        }

        // Save
        for (int i = 0; i < results.Count; i++) {
            string prefix = $"DailyRun_{i}_";
            PlayerPrefs.SetInt(prefix + "Wave", results[i].waveReached);
            PlayerPrefs.SetInt(prefix + "Kills", results[i].enemiesKilled);
            PlayerPrefs.SetInt(prefix + "Gold", results[i].goldCollected);
            PlayerPrefs.SetInt(prefix + "Victory", results[i].victory ? 1 : 0);
        }

        PlayerPrefs.SetInt("DailyRun_Count", results.Count);
        PlayerPrefs.Save();

        if (logSaveLoad) {
            Debug.Log($"[SaveLoadManager] Saved daily run result: Wave {result.waveReached}");
        }
    }

    /// <summary>
    /// Loads the daily run leaderboard.
    /// </summary>
    public List<DailyRunResult> LoadDailyRunLeaderboard() {
        List<DailyRunResult> results = new List<DailyRunResult>();
        int count = PlayerPrefs.GetInt("DailyRun_Count", 0);

        for (int i = 0; i < count; i++) {
            string prefix = $"DailyRun_{i}_";
            results.Add(new DailyRunResult {
                waveReached = PlayerPrefs.GetInt(prefix + "Wave", 0),
                enemiesKilled = PlayerPrefs.GetInt(prefix + "Kills", 0),
                goldCollected = PlayerPrefs.GetInt(prefix + "Gold", 0),
                victory = PlayerPrefs.GetInt(prefix + "Victory", 0) == 1
            });
        }

        return results;
    }

    #endregion

    #region Options Persistence

    public void SaveOptions(float musicVolume, float sfxVolume, bool fullscreen) {
        PlayerPrefs.SetFloat(KEY_OPTIONS_MUSIC, musicVolume);
        PlayerPrefs.SetFloat(KEY_OPTIONS_SFX, sfxVolume);
        PlayerPrefs.SetInt(KEY_OPTIONS_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public OptionsData LoadOptions() {
        return new OptionsData {
            musicVolume = PlayerPrefs.GetFloat(KEY_OPTIONS_MUSIC, 1f),
            sfxVolume = PlayerPrefs.GetFloat(KEY_OPTIONS_SFX, 1f),
            fullscreen = PlayerPrefs.GetInt(KEY_OPTIONS_FULLSCREEN, 1) == 1
        };
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Deletes all save data (for testing or player request).
    /// </summary>
    public void DeleteAllSaveData() {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt(KEY_VERSION, CURRENT_VERSION);
        PlayerPrefs.Save();
        Debug.Log("[SaveLoadManager] All save data deleted");
    }

    /// <summary>
    /// Forces immediate save to disk.
    /// </summary>
    public void ForceSave() {
        PlayerPrefs.Save();
    }

    #endregion
}

#region Data Structures

[Serializable]
public struct CumulativeStats {
    public int totalKills;
    public int highWave;
    public int totalGold;
}

[Serializable]
public class ActiveRunData {
    public int wave;
    public int kills;
    public int gold;
    public int lives;
    public string weaponId;
    public string armorId;
}

[Serializable]
public struct OptionsData {
    public float musicVolume;
    public float sfxVolume;
    public bool fullscreen;
}

#endregion

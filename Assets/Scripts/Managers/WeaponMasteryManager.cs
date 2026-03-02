using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Serializable entry for weapon mastery to support Unity serialization.
/// </summary>
[System.Serializable]
public class WeaponMasteryEntry {
    public string weaponType;
    public WeaponMasteryData data;
}

/// <summary>
/// Manages weapon mastery progression across all weapon types.
/// Uses serializable lists for save/load compatibility.
/// </summary>
public class WeaponMasteryManager : MonoBehaviour {
    public static WeaponMasteryManager Instance { get; private set; }

    [Header("Mastery Data")]
    [SerializeField]
    private List<WeaponMasteryEntry> masteryList = new List<WeaponMasteryEntry>();
    
    // Runtime cache for fast lookups
    private Dictionary<string, WeaponMasteryData> masteryCache;

    [Header("UI")]
    public bool showMasteryNotifications = true;

    // Events
    public System.Action<string, int> OnMasteryLevelUp;
    public System.Action<string> OnEnemyKilled;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCache();
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Builds the runtime cache from the serializable list.
    /// </summary>
    void BuildCache() {
        // Guard: Ensure masteryCache is initialized
        if (masteryCache == null) {
            masteryCache = new Dictionary<string, WeaponMasteryData>();
        } else {
            masteryCache.Clear();
        }
        foreach (var entry in masteryList) {
            if (entry != null && !string.IsNullOrEmpty(entry.weaponType)) {
                masteryCache[entry.weaponType] = entry.data;
            }
        }
    }

    /// <summary>
    /// Synchronizes the serializable list with the cache (call before saving).
    /// </summary>
    void SyncListFromCache() {
        masteryList.Clear();
        foreach (var kvp in masteryCache) {
            masteryList.Add(new WeaponMasteryEntry {
                weaponType = kvp.Key,
                data = kvp.Value
            });
        }
    }

    /// <summary>
    /// Registers a kill for the specified weapon type.
    /// </summary>
    public void RegisterKill(string weaponType) {
        if (string.IsNullOrEmpty(weaponType)) return;

        // Get or create mastery data
        if (!masteryCache.ContainsKey(weaponType)) {
            masteryCache[weaponType] = new WeaponMasteryData { 
                weaponType = weaponType 
            };
            // Sync to list for serialization
            SyncListFromCache();
        }

        var mastery = masteryCache[weaponType];
        int oldLevel = mastery.masteryLevel;
        mastery.AddKill();

        // Check for level up
        if (mastery.masteryLevel > oldLevel && showMasteryNotifications) {
            OnMasteryLevelUp?.Invoke(weaponType, mastery.masteryLevel);
            ShowLevelUpNotification(weaponType, mastery.masteryLevel);
        }

        OnEnemyKilled?.Invoke(weaponType);
    }

    public WeaponMasteryData GetMastery(string weaponType) {
        if (masteryCache != null && masteryCache.ContainsKey(weaponType)) {
            return masteryCache[weaponType];
        }
        return new WeaponMasteryData { weaponType = weaponType };
    }

    public void ApplyMasteryBonuses(PlayerStats stats, string weaponType) {
        var mastery = GetMastery(weaponType);
        stats.damageMod += mastery.bonusDamage;
        stats.critMod += mastery.bonusCrit;
        stats.attackSpeedMod += mastery.bonusAttackSpeed;
    }

    void ShowLevelUpNotification(string weaponType, int level) {
        var mastery = GetMastery(weaponType);
        if (UIManager.Instance != null) {
            UIManager.Instance.ShowNotification(
                $"{weaponType} Mastery Level {level}!\n" +
                $"+{mastery.bonusDamage} DMG, +{mastery.bonusCrit:P0} Crit"
            );
        }
    }

    #region Save/Load Support

    /// <summary>
    /// Loads mastery data from a saved list.
    /// </summary>
    public void LoadMasteryData(List<WeaponMasteryEntry> data) {
        masteryList = data ?? new List<WeaponMasteryEntry>();
        BuildCache();
    }

    /// <summary>
    /// Gets the mastery data list for saving.
    /// </summary>
    public List<WeaponMasteryEntry> GetMasteryDataForSave() {
        SyncListFromCache();
        return masteryList;
    }

    #endregion
}

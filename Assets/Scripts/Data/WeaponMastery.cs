using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Serializable data class for weapon mastery progress.
/// </summary>
[System.Serializable]
public class WeaponMasteryData {
    public string weaponType; // e.g., "Sword", "Axe", "Spear"
    public int killCount = 0;
    public int masteryLevel = 0;
    public float masteryProgress = 0f; // 0-1 to next level
    
    // Bonuses at current level
    public int bonusDamage = 0;
    public float bonusCrit = 0f;
    public float bonusAttackSpeed = 0f;

    [Header("Level Thresholds")]
    public int[] killsPerLevel = { 10, 25, 50, 100, 200 };

    public void AddKill() {
        if (masteryLevel >= killsPerLevel.Length) return;
        
        killCount++;
        CalculateLevel();
    }

    void CalculateLevel() {
        int newLevel = 0;
        for (int i = 0; i < killsPerLevel.Length; i++) {
            if (killCount >= killsPerLevel[i]) {
                newLevel = i + 1;
            }
        }

        if (newLevel > masteryLevel) {
            masteryLevel = newLevel;
            CalculateBonuses();
        }

        // Calculate progress
        if (masteryLevel < killsPerLevel.Length) {
            int prevThreshold = masteryLevel > 0 ? killsPerLevel[masteryLevel - 1] : 0;
            int nextThreshold = killsPerLevel[masteryLevel];
            masteryProgress = (float)(killCount - prevThreshold) / (nextThreshold - prevThreshold);
        } else {
            masteryProgress = 1f;
        }
    }

    void CalculateBonuses() {
        // Linear scaling with diminishing returns
        bonusDamage = masteryLevel * 2;
        bonusCrit = masteryLevel * 0.02f;
        bonusAttackSpeed = masteryLevel * 0.05f;
    }

    public int GetKillsForNextLevel() {
        if (masteryLevel >= killsPerLevel.Length) return -1;
        return killsPerLevel[masteryLevel];
    }

    /// <summary>
    /// Gets total kills (alias for killCount).
    /// </summary>
    public int totalKills => killCount;

    /// <summary>
    /// Gets progress to next level (0-1).
    /// </summary>
    public float GetProgressToNextLevel() {
        return masteryProgress;
    }
}

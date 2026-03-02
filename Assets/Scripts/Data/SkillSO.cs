using UnityEngine;

[CreateAssetMenu(fileName="Skill_", menuName="ARPG/Skill")]
public class SkillSO : ScriptableObject {
    [Header("Basic Info")]
    public string skillId;
    public string skillName;
    public string description;
    public Sprite icon;
    public int skillSlot; // 0-3 for keys 1-4

    [Header("Cooldown")]
    public float cooldownTime = 5f;

    [Header("Skill Type & Stats")]
    public SkillType skillType = SkillType.CircleAOE;
    public float damageMultiplier = 1f; // Multiplier of base damage
    public float range = 3f;
    public float radius = 2f;
    public float duration = 0f; // For shields or DoT
    public GameObject effectPrefab;
    public AudioClip castSound;
}

using UnityEngine;

[CreateAssetMenu(fileName="Item_", menuName="ARPG/Item")]
public class ItemSO : ScriptableObject {
    [Header("Basic Info")]
    public string itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    public ItemRarity rarity = ItemRarity.Common;
    public EquipSlot slot = EquipSlot.Weapon;
    public int price = 10;

    [Header("Stats")]
    public int damageBonus = 0;
    public int defenseBonus = 0;
    public float critBonus = 0f; // e.g., 0.15 = +15%
    public float attackSpeedBonus = 0f;
    public int maxHPBonus = 0;

    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.None; // For mastery tracking

    [Header("Visual")]
    public Sprite itemSprite; // For equipped visual
    public Color rarityColor => GetRarityColor();
    
    [Header("Set Bonus")]
    public string setId; // Leave empty if not part of a set

    private Color GetRarityColor() {
        return rarity switch {
            ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),    // Gray
            ItemRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),  // Green
            ItemRarity.Rare => new Color(0.2f, 0.5f, 1f),        // Blue
            ItemRarity.Epic => new Color(0.8f, 0.2f, 0.9f),      // Purple
            _ => Color.white
        };
    }
}

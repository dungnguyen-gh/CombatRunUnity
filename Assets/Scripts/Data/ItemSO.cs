using UnityEngine;
using System.Text;

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
    public int sellPrice => Mathf.Max(1, price / 5); // Auto-calculate sell price
    
    [Header("Item Type")]
    public bool isEquippable = true;
    public bool isStackable = false;
    public int stackCount = 1;
    public int maxStackSize = 99;

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
    public Sprite worldSprite; // For 3D world pickup display (fallback to icon if null)
    public Color rarityColor => GetRarityColor();
    
    [Header("Set Bonus")]
    public string setId; // Leave empty if not part of a set

    /// <summary>
    /// Gets the display color based on rarity.
    /// </summary>
    public Color GetRarityColor() {
        return rarity switch {
            ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),    // Gray
            ItemRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),  // Green
            ItemRarity.Rare => new Color(0.2f, 0.5f, 1f),        // Blue
            ItemRarity.Epic => new Color(0.8f, 0.2f, 0.9f),      // Purple
            ItemRarity.Legendary => new Color(1f, 0.8f, 0.2f),   // Gold
            _ => Color.white
        };
    }
    
    /// <summary>
    /// Gets the sprite to use for world display (falls back to icon if worldSprite is null).
    /// </summary>
    public Sprite GetWorldSprite() {
        return worldSprite != null ? worldSprite : icon;
    }
    
    /// <summary>
    /// Returns formatted description with stats.
    /// </summary>
    public string GetTooltipText() {
        string tooltip = $"<b><color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{itemName}</color></b>\n";
        tooltip += $"<size=10>{description}</size>\n\n";
        
        if (damageBonus > 0) tooltip += $"Damage: +{damageBonus}\n";
        if (defenseBonus > 0) tooltip += $"Defense: +{defenseBonus}\n";
        if (critBonus > 0) tooltip += $"Crit Chance: +{critBonus * 100:F0}%\n";
        if (attackSpeedBonus > 0) tooltip += $"Attack Speed: +{attackSpeedBonus * 100:F0}%\n";
        if (maxHPBonus > 0) tooltip += $"Max HP: +{maxHPBonus}\n";
        
        tooltip += $"\n<size=9>Sell: {sellPrice}G</size>";
        
        return tooltip;
    }
    
    /// <summary>
    /// Returns a formatted tooltip text including set bonus information.
    /// </summary>
    public string GetTooltipTextWithSetBonus(EquipmentSetSO set, int pieceCount) {
        StringBuilder sb = new StringBuilder();
        
        // Item name with rarity color
        sb.AppendLine($"<b><color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{itemName}</color></b>");
        sb.AppendLine($"<size=10>{description}</size>");
        sb.AppendLine();
        
        // Stats
        if (damageBonus > 0) sb.AppendLine($"Damage: +{damageBonus}");
        if (defenseBonus > 0) sb.AppendLine($"Defense: +{defenseBonus}");
        if (critBonus > 0) sb.AppendLine($"Crit Chance: +{critBonus * 100:F0}%");
        if (attackSpeedBonus > 0) sb.AppendLine($"Attack Speed: +{attackSpeedBonus * 100:F0}%");
        if (maxHPBonus > 0) sb.AppendLine($"Max HP: +{maxHPBonus}");
        
        // Set bonus info
        if (set != null && !string.IsNullOrEmpty(setId)) {
            sb.AppendLine();
            sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(set.setColor)}><b>{set.setName} Set</b></color>");
            
            // Progress indicator
            bool has2Piece = pieceCount >= 2;
            bool has4Piece = pieceCount >= 4;
            
            string piece2Color = has2Piece ? "#00FF00" : "#808080";
            string piece4Color = has4Piece ? "#00FF00" : "#808080";
            
            sb.AppendLine($"<color={piece2Color}>({Mathf.Min(pieceCount, 2)}/2) {set.bonusDescription2}</color>");
            
            if (set.has4PieceBonus) {
                sb.AppendLine($"<color={piece4Color}>({Mathf.Min(pieceCount, 4)}/4) {set.bonusDescription4}</color>");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine($"<size=9>Sell: {sellPrice}G</size>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Gets a formatted string showing all item stats for comparison.
    /// </summary>
    public string GetStatComparisonText(ItemSO compareTo = null) {
        StringBuilder sb = new StringBuilder();
        
        System.Func<int, int, string> formatIntStat = (value, compareValue) => {
            if (compareTo == null) return value > 0 ? $"+{value}" : value.ToString();
            int diff = value - compareValue;
            if (diff > 0) return $"+{value} <color=#00FF00>(+{diff})</color>";
            if (diff < 0) return $"+{value} <color=#FF0000>({diff})</color>";
            return $"+{value}";
        };
        
        System.Func<float, float, string> formatFloatStat = (value, compareValue) => {
            if (compareTo == null) return $"+{value * 100:F0}%";
            float diff = value - compareValue;
            if (diff > 0) return $"+{value * 100:F0}% <color=#00FF00>(+{diff * 100:F0}%)</color>";
            if (diff < 0) return $"+{value * 100:F0}% <color=#FF0000>({diff * 100:F0}%)</color>";
            return $"+{value * 100:F0}%";
        };
        
        if (damageBonus > 0 || (compareTo != null && compareTo.damageBonus > 0)) {
            sb.AppendLine($"Damage: {formatIntStat(damageBonus, compareTo?.damageBonus ?? 0)}");
        }
        if (defenseBonus > 0 || (compareTo != null && compareTo.defenseBonus > 0)) {
            sb.AppendLine($"Defense: {formatIntStat(defenseBonus, compareTo?.defenseBonus ?? 0)}");
        }
        if (critBonus > 0 || (compareTo != null && compareTo.critBonus > 0)) {
            sb.AppendLine($"Crit: {formatFloatStat(critBonus, compareTo?.critBonus ?? 0)}");
        }
        if (attackSpeedBonus > 0 || (compareTo != null && compareTo.attackSpeedBonus > 0)) {
            sb.AppendLine($"Attack Speed: {formatFloatStat(attackSpeedBonus, compareTo?.attackSpeedBonus ?? 0)}");
        }
        if (maxHPBonus > 0 || (compareTo != null && compareTo.maxHPBonus > 0)) {
            sb.AppendLine($"Max HP: {formatIntStat(maxHPBonus, compareTo?.maxHPBonus ?? 0)}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Creates a stack of this item.
    /// </summary>
    public ItemSO CreateStack(int count) {
        if (!isStackable || count <= 1) return this;
        
        ItemSO stack = Instantiate(this);
        stack.name = name; // Keep original name
        stack.stackCount = Mathf.Min(count, maxStackSize);
        return stack;
    }
}

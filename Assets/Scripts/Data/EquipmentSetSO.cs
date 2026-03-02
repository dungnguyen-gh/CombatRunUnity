using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName="Set_", menuName="ARPG/EquipmentSet")]
public class EquipmentSetSO : ScriptableObject {
    [Header("Set Info")]
    public string setId;
    public string setName;
    public string description;
    public Sprite setIcon;
    public Color setColor = Color.yellow;

    [Header("Set Pieces (Item IDs)")]
    public List<string> setPieceIds = new List<string>();

    [Header("2-Piece Bonus")]
    public bool has2PieceBonus = true;
    public int damageBonus2 = 0;
    public int defenseBonus2 = 0;
    public float critBonus2 = 0f;
    public float attackSpeedBonus2 = 0f;
    public int maxHPBonus2 = 0;
    public string bonusDescription2 = "+10 Damage";

    [Header("4-Piece Bonus")]
    public bool has4PieceBonus = true;
    public int damageBonus4 = 0;
    public int defenseBonus4 = 0;
    public float critBonus4 = 0f;
    public float attackSpeedBonus4 = 0f;
    public int maxHPBonus4 = 0;
    public string bonusDescription4 = "Special Set Effect";
    public SetSpecialEffect specialEffect4 = SetSpecialEffect.None;

    [Header("Full Set Bonus (if applicable)")]
    public string fullSetBonusDescription;
}

public enum SetSpecialEffect {
    None,
    LifeSteal,           // Heal on hit
    BurnOnHit,          // Apply burn to enemies
    DoubleGold,         // 2x gold drops
    ShieldOnHit,        // Chance to gain shield when hit
    CriticalOverload,   // Crits deal extra AOE
    VampireTouch        // Damage heals you
}

[System.Serializable]
public class ActiveSetBonus {
    public EquipmentSetSO set;
    public int pieceCount;
    public bool has2Piece;
    public bool has4Piece;
}

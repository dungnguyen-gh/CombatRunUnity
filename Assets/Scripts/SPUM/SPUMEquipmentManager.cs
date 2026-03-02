using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages equipment visual changes for SPUM characters.
/// Handles swapping sprites for weapons, armor, helmets, etc.
/// </summary>
public class SPUMEquipmentManager : MonoBehaviour
{
    [Header("SPUM Reference")]
    public SPUM_Prefabs spumPrefabs;
    
    [Header("Equipment Part Transforms")]
    // These will be auto-found based on naming conventions
    public Transform helmetTransform;
    public Transform armorTransform;
    public Transform leftWeaponTransform;
    public Transform rightWeaponTransform;
    public Transform shieldTransform;
    public Transform backTransform;
    
    [Header("Default Equipment")]
    public Sprite defaultHelmet;
    public Sprite defaultArmor;
    public Sprite defaultWeapon;
    
    // Dictionary to track current equipment sprites
    private Dictionary<EquipSlot, Sprite> currentEquipment = new Dictionary<EquipSlot, Sprite>();
    private Dictionary<string, SpriteRenderer> partRenderers = new Dictionary<string, SpriteRenderer>();
    
    // FIX: Cached SpriteRenderer references to avoid repeated GetComponent calls
    private SpriteRenderer helmetRenderer;
    private SpriteRenderer armorRenderer;
    private SpriteRenderer leftWeaponRenderer;
    private SpriteRenderer rightWeaponRenderer;
    private SpriteRenderer shieldRenderer;
    private SpriteRenderer backRenderer;
    
    void Awake()
    {
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        
        FindPartRenderers();
    }
    
    void Start()
    {
        // Cache default sprites
        CacheDefaultSprites();
    }
    
    void FindPartRenderers()
    {
        if (spumPrefabs == null) return;
        
        // Find all SpriteRenderers in the SPUM prefab hierarchy
        SpriteRenderer[] renderers = spumPrefabs.GetComponentsInChildren<SpriteRenderer>(true);
        
        foreach (var renderer in renderers)
        {
            string name = renderer.gameObject.name.ToLower();
            partRenderers[name] = renderer;
            
            // Assign to specific categories based on naming
            // FIX: Cache SpriteRenderer references to avoid repeated GetComponent calls
            if (name.Contains("helmet"))
            {
                helmetTransform = renderer.transform;
                helmetRenderer = renderer;
            }
            else if (name.Contains("armor") || name.Contains("body"))
            {
                armorTransform = renderer.transform;
                armorRenderer = renderer;
            }
            else if (name.Contains("weapon") && name.Contains("left"))
            {
                leftWeaponTransform = renderer.transform;
                leftWeaponRenderer = renderer;
            }
            else if (name.Contains("weapon") && name.Contains("right"))
            {
                rightWeaponTransform = renderer.transform;
                rightWeaponRenderer = renderer;
            }
            else if (name.Contains("shield"))
            {
                shieldTransform = renderer.transform;
                shieldRenderer = renderer;
            }
            else if (name.Contains("back"))
            {
                backTransform = renderer.transform;
                backRenderer = renderer;
            }
        }
    }
    
    void CacheDefaultSprites()
    {
        // FIX: Use cached SpriteRenderer references with null checks
        // Store current sprites as defaults
        if (helmetRenderer != null)
            defaultHelmet = helmetRenderer.sprite;
        else if (helmetTransform != null)
            defaultHelmet = helmetTransform.GetComponent<SpriteRenderer>()?.sprite;
            
        if (armorRenderer != null)
            defaultArmor = armorRenderer.sprite;
        else if (armorTransform != null)
            defaultArmor = armorTransform.GetComponent<SpriteRenderer>()?.sprite;
            
        if (rightWeaponRenderer != null)
            defaultWeapon = rightWeaponRenderer.sprite;
        else if (rightWeaponTransform != null)
            defaultWeapon = rightWeaponTransform.GetComponent<SpriteRenderer>()?.sprite;
    }
    
    /// <summary>
    /// Equip a weapon sprite (changes the weapon visual)
    /// </summary>
    public void EquipWeapon(Sprite weaponSprite, WeaponType weaponType = WeaponType.Sword)
    {
        if (weaponSprite == null)
        {
            UnequipWeapon();
            return;
        }
        
        // SPUM typically has weapons on both hands for certain animations
        // Right hand is usually the main weapon hand
        // FIX: Use cached SpriteRenderer reference with null check
        if (rightWeaponRenderer != null)
        {
            rightWeaponRenderer.sprite = weaponSprite;
            rightWeaponRenderer.enabled = true;
        }
        else if (rightWeaponTransform != null)
        {
            SpriteRenderer sr = rightWeaponTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = weaponSprite;
                sr.enabled = true;
            }
        }
        
        // For dual-wield or specific weapon types, also set left hand
        // FIX: Use cached SpriteRenderer reference with null check
        if (weaponType == WeaponType.Dagger || weaponType == WeaponType.Axe)
        {
            if (leftWeaponRenderer != null)
            {
                leftWeaponRenderer.sprite = weaponSprite;
                leftWeaponRenderer.enabled = true;
            }
            else if (leftWeaponTransform != null)
            {
                SpriteRenderer sr = leftWeaponTransform.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = weaponSprite;
                    sr.enabled = true;
                }
            }
        }
        
        currentEquipment[EquipSlot.Weapon] = weaponSprite;
        
        Debug.Log($"Equipped weapon: {weaponSprite.name}");
    }
    
    /// <summary>
    /// Unequip weapon (restore default or hide)
    /// </summary>
    public void UnequipWeapon()
    {
        // FIX: Use cached SpriteRenderer reference with null check
        if (rightWeaponRenderer != null)
        {
            rightWeaponRenderer.sprite = defaultWeapon;
            rightWeaponRenderer.enabled = defaultWeapon != null;
        }
        else if (rightWeaponTransform != null)
        {
            SpriteRenderer sr = rightWeaponTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = defaultWeapon;
                sr.enabled = defaultWeapon != null;
            }
        }
        
        if (leftWeaponRenderer != null)
        {
            leftWeaponRenderer.enabled = false;
        }
        else if (leftWeaponTransform != null)
        {
            SpriteRenderer sr = leftWeaponTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
        
        currentEquipment.Remove(EquipSlot.Weapon);
    }
    
    /// <summary>
    /// Equip armor (changes body/helmet sprites)
    /// </summary>
    public void EquipArmor(Sprite armorSprite, Sprite helmetSprite = null)
    {
        // Equip body armor
        // FIX: Use cached SpriteRenderer reference with null check
        if (armorSprite != null)
        {
            if (armorRenderer != null)
            {
                armorRenderer.sprite = armorSprite;
                armorRenderer.enabled = true;
            }
            else if (armorTransform != null)
            {
                SpriteRenderer sr = armorTransform.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = armorSprite;
                    sr.enabled = true;
                }
            }
        }
        
        // Equip helmet if provided
        if (helmetSprite != null)
        {
            if (helmetRenderer != null)
            {
                helmetRenderer.sprite = helmetSprite;
                helmetRenderer.enabled = true;
            }
            else if (helmetTransform != null)
            {
                SpriteRenderer sr = helmetTransform.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = helmetSprite;
                    sr.enabled = true;
                }
            }
        }
        
        currentEquipment[EquipSlot.Armor] = armorSprite;
        
        Debug.Log($"Equipped armor: {armorSprite?.name}");
    }
    
    /// <summary>
    /// Unequip armor (restore defaults)
    /// </summary>
    public void UnequipArmor()
    {
        // FIX: Use cached SpriteRenderer reference with null check
        if (armorRenderer != null)
        {
            armorRenderer.sprite = defaultArmor;
        }
        else if (armorTransform != null)
        {
            SpriteRenderer sr = armorTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = defaultArmor;
            }
        }
        
        if (helmetRenderer != null)
        {
            helmetRenderer.sprite = defaultHelmet;
        }
        else if (helmetTransform != null)
        {
            SpriteRenderer sr = helmetTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = defaultHelmet;
            }
        }
        
        currentEquipment.Remove(EquipSlot.Armor);
    }
    
    /// <summary>
    /// Equip a shield
    /// </summary>
    public void EquipShield(Sprite shieldSprite)
    {
        if (shieldSprite == null) return;
        
        // FIX: Use cached SpriteRenderer reference with null check
        if (shieldRenderer != null)
        {
            shieldRenderer.sprite = shieldSprite;
            shieldRenderer.enabled = true;
        }
        else if (shieldTransform != null)
        {
            SpriteRenderer sr = shieldTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = shieldSprite;
                sr.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// Unequip shield
    /// </summary>
    public void UnequipShield()
    {
        // FIX: Use cached SpriteRenderer reference with null check
        if (shieldRenderer != null)
        {
            shieldRenderer.enabled = false;
        }
        else if (shieldTransform != null)
        {
            SpriteRenderer sr = shieldTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
    }
    
    /// <summary>
    /// Equip back item (backpack, wings, etc.)
    /// </summary>
    public void EquipBack(Sprite backSprite)
    {
        if (backSprite == null) return;
        
        // FIX: Use cached SpriteRenderer reference with null check
        if (backRenderer != null)
        {
            backRenderer.sprite = backSprite;
            backRenderer.enabled = true;
        }
        else if (backTransform != null)
        {
            SpriteRenderer sr = backTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = backSprite;
                sr.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// Change equipment color (for dyeing system)
    /// </summary>
    public void SetEquipmentColor(EquipSlot slot, Color color)
    {
        SpriteRenderer targetRenderer = null;
        
        switch (slot)
        {
            case EquipSlot.Weapon:
                targetRenderer = rightWeaponRenderer;
                break;
            case EquipSlot.Armor:
                targetRenderer = armorRenderer;
                break;
        }
        
        // FIX: Use cached SpriteRenderer reference with null check
        if (targetRenderer != null)
        {
            targetRenderer.color = color;
        }
        else
        {
            // Fallback to transform-based lookup with null check
            Transform target = slot == EquipSlot.Weapon ? rightWeaponTransform : armorTransform;
            if (target != null)
            {
                SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = color;
                }
            }
        }
    }
    
    /// <summary>
    /// Reset equipment color to default (white)
    /// </summary>
    public void ResetEquipmentColor(EquipSlot slot)
    {
        SetEquipmentColor(slot, Color.white);
    }
    
    /// <summary>
    /// Preview equipment without actually equipping (for shop preview)
    /// </summary>
    public void PreviewEquipment(EquipSlot slot, Sprite sprite)
    {
        // Store current
        Sprite current = null;
        if (currentEquipment.ContainsKey(slot))
            current = currentEquipment[slot];
        
        // Apply preview
        switch (slot)
        {
            case EquipSlot.Weapon:
                EquipWeapon(sprite);
                break;
            case EquipSlot.Armor:
                EquipArmor(sprite);
                break;
        }
        
        // Note: You'll need to call EndPreview(slot, current) to restore
    }
    
    /// <summary>
    /// End preview and restore original equipment
    /// </summary>
    public void EndPreview(EquipSlot slot, Sprite originalSprite)
    {
        switch (slot)
        {
            case EquipSlot.Weapon:
                if (originalSprite != null)
                    EquipWeapon(originalSprite);
                else
                    UnequipWeapon();
                break;
            case EquipSlot.Armor:
                if (originalSprite != null)
                    EquipArmor(originalSprite);
                else
                    UnequipArmor();
                break;
        }
    }
    
    /// <summary>
    /// Get the SpriteRenderer for a specific part by name
    /// </summary>
    public SpriteRenderer GetPartRenderer(string partName)
    {
        if (partRenderers.ContainsKey(partName.ToLower()))
            return partRenderers[partName.ToLower()];
        
        return null;
    }
    
    /// <summary>
    /// Get currently equipped sprite for a slot
    /// </summary>
    public Sprite GetEquippedSprite(EquipSlot slot)
    {
        if (currentEquipment.ContainsKey(slot))
            return currentEquipment[slot];
        return null;
    }
    
    void OnValidate()
    {
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
    }
}

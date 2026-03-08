using UnityEngine;

/// <summary>
/// World pickup item with bobbing animation, magnet effect, spin animation, and auto-pickup when close.
/// </summary>
public class ItemPickup : MonoBehaviour {
    public ItemSO item;
    public float pickupRange = 1.5f;
    public float magnetSpeed = 5f;
    public float lifetime = 60f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    
    [Header("Spin Animation")]
    public bool spinEnabled = true;
    public float spinSpeed = 180f; // Degrees per second
    public Vector3 spinAxis = Vector3.forward;

    private Transform player;
    private bool isMagnetized = false;
    private Vector3 startPosition;
    private Vector3 bobOffset;
    private SpriteRenderer spriteRenderer;
    private float timeOffset; // For varied bobbing between items

    void Start() {
        // Try to find player, with fallback for spawn order issues
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) {
            // Player might not be spawned yet - will retry in Update
            Debug.LogWarning("[ItemPickup] Player not found at Start, will retry");
        }
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        timeOffset = Random.Range(0f, Mathf.PI * 2f); // Random starting phase
        
        // Set sprite from item (use worldSprite if available, fallback to icon)
        UpdateVisual();

        Destroy(gameObject, lifetime);
    }
    
    /// <summary>
    /// Updates the visual sprite based on item data.
    /// </summary>
    void UpdateVisual() {
        if (item != null && spriteRenderer != null) {
            Sprite spriteToUse = item.GetWorldSprite();
            if (spriteToUse != null) {
                spriteRenderer.sprite = spriteToUse;
                // Apply rarity color tint at reduced opacity for world items
                Color rarityTint = item.rarityColor;
                rarityTint.a = spriteRenderer.color.a; // Preserve original alpha
                spriteRenderer.color = rarityTint;
            }
        }
    }

    void Update() {
        // Spin animation
        if (spinEnabled && !isMagnetized) {
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime);
        }
        
        // Bobbing animation (using offset to not interfere with magnet movement)
        float bobY = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
        bobOffset = new Vector3(0, bobY, 0);

        if (player == null) {
            // Try to find player again
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Apply bobbing only when not magnetized
            if (!isMagnetized) {
                transform.position = startPosition + bobOffset;
            }
            return;
        }

        // Use 2D distance (ignoring Z)
        Vector2 pos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos2D = new Vector2(player.position.x, player.position.y);
        float distance = Vector2.Distance(pos2D, playerPos2D);

        // Magnet to player
        if (distance < pickupRange || isMagnetized) {
            isMagnetized = true;
            Vector2 direction = (playerPos2D - pos2D).normalized;
            transform.position += (Vector3)direction * magnetSpeed * Time.deltaTime;
            // Don't update startPosition during magnet - this prevents bobbing center from shifting
            
            // Stop spinning when magnetized
            if (spinEnabled) {
                // Reset rotation to upright when being pulled
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, 5f * Time.deltaTime);
            }
        } else {
            // Apply bobbing when not magnetized
            transform.position = startPosition + bobOffset;
        }

        // Pickup when close enough
        if (distance < 0.3f) {
            Pickup();
        }
    }

    /// <summary>
    /// Sets the item data and updates the visual.
    /// </summary>
    public void SetItem(ItemSO newItem) {
        item = newItem;
        UpdateVisual();
    }
    
    /// <summary>
    /// Configures the spin animation settings.
    /// </summary>
    public void SetSpinAnimation(bool enabled, float speed = 180f, Vector3? axis = null) {
        spinEnabled = enabled;
        if (enabled) {
            spinSpeed = speed;
            if (axis.HasValue) {
                spinAxis = axis.Value;
            }
        }
    }

    void Pickup() {
        if (item == null) return;
        
        // Guard: Ensure InventoryManager exists
        if (InventoryManager.Instance == null) {
            Debug.LogWarning("[ItemPickup] Cannot pickup - InventoryManager.Instance is null");
            return;
        }
        
        // Use singleton instance instead of FindObjectOfType for better performance
        if (InventoryManager.Instance != null) {
            if (InventoryManager.Instance.AddItem(item)) {
                // Show pickup notification with rarity color
                if (UIManager.Instance != null) {
                    string rarityHex = ColorUtility.ToHtmlStringRGB(item.rarityColor);
                    UIManager.Instance.ShowNotification($"Picked up: <color=#{rarityHex}>{item.itemName}</color>");
                }
                Destroy(gameObject);
            }
            // If AddItem returns false (inventory full), don't destroy - let player see it's still there
        }
    }
}

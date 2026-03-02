using UnityEngine;

/// <summary>
/// World pickup item with bobbing animation, magnet effect, and auto-pickup when close.
/// </summary>
public class ItemPickup : MonoBehaviour {
    public ItemSO item;
    public float pickupRange = 1.5f;
    public float magnetSpeed = 5f;
    public float lifetime = 60f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;

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
        
        // Set sprite from item
        if (item != null && spriteRenderer != null) {
            spriteRenderer.sprite = item.icon;
        }

        Destroy(gameObject, lifetime);
    }

    void Update() {
        // Bobbing animation (using offset to not interfere with magnet movement)
        float bobY = Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
        bobOffset = new Vector3(0, bobY, 0);

        if (player == null) {
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
        if (item != null && spriteRenderer != null) {
            spriteRenderer.sprite = item.icon;
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
                // Show pickup notification
                if (UIManager.Instance != null) {
                    UIManager.Instance.ShowNotification($"Picked up: {item.itemName}");
                }
                Destroy(gameObject);
            }
            // If AddItem returns false (inventory full), don't destroy - let player see it's still there
        }
    }
}

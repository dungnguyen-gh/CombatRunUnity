using UnityEngine;

/// <summary>
/// Gold currency pickup with magnet effect.
/// Automatically finds player and adds gold to inventory.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class GoldPickup : MonoBehaviour {
    public int goldAmount = 5;
    public float pickupRange = 1f;
    public float magnetSpeed = 5f;
    public float lifetime = 30f;
    public float initialBounce = 2f; // Upward force on spawn

    private Transform player;
    private bool isMagnetized = false;
    private float spawnTime;
    private PlayerController playerController;
    private Rigidbody2D rb;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null) {
            playerController = player.GetComponent<PlayerController>();
        }
        spawnTime = Time.time;
        
        // Setup physics to prevent falling through floor
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) {
            // No rigidbody - static pickup (recommended)
            // Make sure collider is trigger
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        } else {
            // Has rigidbody - configure for pickup physics
            rb.gravityScale = 0f; // NO GRAVITY
            rb.bodyType = RigidbodyType2D.Kinematic; // Don't fall
            
            // Optional: Add small bounce on spawn
            rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), initialBounce);
        }
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    void Update() {
        // Cached player reference - no retry logic in Update for performance
        // Player is cached in Start; if null, pickup will still work via direct collision
        if (player == null) return;

        // Use 2D distance
        Vector2 pos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos2D = new Vector2(player.position.x, player.position.y);
        float distance = Vector2.Distance(pos2D, playerPos2D);

        // Magnet to player when close
        if (distance < pickupRange || isMagnetized) {
            isMagnetized = true;
            Vector2 direction = (playerPos2D - pos2D).normalized;
            transform.position += (Vector3)direction * magnetSpeed * Time.deltaTime;
        }

        // Check for pickup
        if (distance < 0.3f) {
            Pickup();
        }
    }

    /// <summary>
    /// Sets the gold amount for this pickup.
    /// </summary>
    public void SetAmount(int amount) {
        goldAmount = Mathf.Max(0, amount);
    }

    void Pickup() {
        if (playerController != null) {
            playerController.AddGold(goldAmount);
            
            // Optional: Show floating text for gold amount
            if (DamageNumberManager.Instance != null) {
                DamageNumberManager.Instance.ShowDamage(
                    goldAmount, 
                    transform.position + Vector3.up * 0.5f
                );
            }
        }
        Destroy(gameObject);
    }
}

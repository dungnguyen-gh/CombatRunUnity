using UnityEngine;

/// <summary>
/// Simple projectile behavior - moves in direction, damages on hit, destroys at max distance or lifetime.
/// Includes safety checks for double-hit prevention and zero-direction handling.
/// </summary>
public class Projectile : MonoBehaviour {
    public float speed = 10f;
    public float lifetime = 3f;
    
    private Vector2 direction;
    private float damage;
    private LayerMask targetLayer;
    private float distanceTraveled;
    private float maxDistance;
    private Vector2 startPosition;
    private bool hasHit = false;

    /// <summary>
    /// Initializes the projectile with movement and damage parameters.
    /// </summary>
    /// <param name="dir">Direction vector (will be normalized)</param>
    /// <param name="range">Maximum travel distance</param>
    /// <param name="dmg">Damage dealt on hit</param>
    /// <param name="layer">Layer mask for valid targets</param>
    public void Initialize(Vector2 dir, float range, float dmg, LayerMask layer) {
        // Validate direction
        if (dir == Vector2.zero) {
            Debug.LogWarning("[Projectile] Initialize called with zero direction! Destroying projectile.");
            Destroy(gameObject);
            return;
        }
        
        direction = dir.normalized;
        maxDistance = range;
        damage = dmg;
        targetLayer = layer;
        startPosition = transform.position;
        
        // Rotate to face direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        Destroy(gameObject, lifetime);
    }

    void Update() {
        float moveDistance = speed * Time.deltaTime;
        transform.position += (Vector3)direction * moveDistance;
        
        // More accurate distance calculation using start position
        distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance) {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        // Prevent double hits
        if (hasHit) return;
        
        // Check if layer matches target layer
        if (((1 << other.gameObject.layer) & targetLayer) != 0) {
            // FIX: Add null check for GetComponent result
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null) {
                hasHit = true;
                enemy.TakeDamage(Mathf.RoundToInt(damage));
            }
            Destroy(gameObject);
        }
    }
    
    void OnDrawGizmosSelected() {
        // Visualize projectile path
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)direction * 2f);
    }
}

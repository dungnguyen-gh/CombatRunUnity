using UnityEngine;

/// <summary>
/// Enhanced projectile with homing, piercing, and skill integration.
/// </summary>
public class Projectile : MonoBehaviour {
    [Header("Movement")]
    public float speed = 10f;
    public float maxLifetime = 5f;
    
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public ParticleSystem trailParticles;
    
    // Runtime state
    private Vector2 direction;
    private float range;
    private int damage;
    private LayerMask hitLayers;
    private SkillSO sourceSkill;
    private float distanceTraveled;
    private Transform target; // For homing
    
    // Settings
    public bool pierce { get; set; }
    public bool homing { get; set; }
    public float homingStrength { get; set; }
    public bool explodeOnImpact { get; set; }
    public float explosionRadius { get; set; }
    
    private bool initialized = false;

    void Awake() {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        Destroy(gameObject, maxLifetime);
    }

    void Update() {
        if (!initialized) return;
        
        Move();
        CheckRange();
    }

    void Move() {
        if (homing && target != null) {
            // Homing behavior
            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
            direction = Vector2.Lerp(direction, toTarget, homingStrength * Time.deltaTime);
            
            // Rotate to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        Vector2 moveStep = direction * speed * Time.deltaTime;
        transform.position += (Vector3)moveStep;
        distanceTraveled += moveStep.magnitude;
        
        // Raycast for hit detection (more accurate than trigger)
        RaycastHit2D hit = Physics2D.Raycast(transform.position - (Vector3)moveStep, direction, moveStep.magnitude, hitLayers);
        if (hit.collider != null) {
            OnHit(hit.collider, hit.point);
        }
    }

    void CheckRange() {
        if (distanceTraveled >= range) {
            Expire();
        }
    }

    public void Initialize(Vector2 dir, float maxRange, int dmg, LayerMask layers, SkillSO skill = null) {
        direction = dir.normalized;
        range = maxRange;
        damage = dmg;
        hitLayers = layers;
        sourceSkill = skill;
        initialized = true;
        
        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Find homing target if enabled
        if (homing) {
            FindHomingTarget();
        }
    }

    void FindHomingTarget() {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 10f, hitLayers);
        float closestDist = float.MaxValue;
        
        foreach (var enemy in enemies) {
            if (enemy.CompareTag("Enemy")) {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist) {
                    closestDist = dist;
                    target = enemy.transform;
                }
            }
        }
    }

    void OnHit(Collider2D other, Vector2 hitPoint) {
        if (other.CompareTag("Enemy")) {
            // Deal damage
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.TakeDamage(damage);
            }
            
            // Spawn hit effect
            if (sourceSkill?.effectPrefab != null) {
                Instantiate(sourceSkill.effectPrefab, hitPoint, Quaternion.identity);
            }
            
            // Handle piercing
            if (!pierce) {
                if (explodeOnImpact) {
                    Explode(hitPoint);
                }
                Destroy(gameObject);
            }
        } else if (!other.isTrigger) {
            // Hit environment
            if (explodeOnImpact) {
                Explode(hitPoint);
            }
            Destroy(gameObject);
        }
    }

    void Explode(Vector2 position) {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, explosionRadius, hitLayers);
        foreach (var hit in hits) {
            if (hit.CompareTag("Enemy")) {
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null) {
                    // Explosion does 50% damage
                    enemy.TakeDamage(damage / 2);
                }
            }
        }
        
        // Spawn explosion effect
        if (sourceSkill?.effectPrefab != null) {
            var explosion = Instantiate(sourceSkill.effectPrefab, position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * explosionRadius;
        }
    }

    void Expire() {
        // Spawn expire effect
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) {
        // Backup hit detection
        if (initialized && !other.isTrigger) {
            OnHit(other, transform.position);
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

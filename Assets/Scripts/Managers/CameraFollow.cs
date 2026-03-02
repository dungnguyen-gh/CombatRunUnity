using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Bounds")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // FIX: Add timer to limit FindGameObjectWithTag calls to once per second when target is null
    private float lastFindAttemptTime = -999f;
    private const float FIND_COOLDOWN = 1f;

    void Start() {
        // Cache player reference at start
        if (target == null) {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    void LateUpdate() {
        // FIX: Add timer to limit FindGameObjectWithTag calls when target is null
        if (target == null) {
            if (Time.time - lastFindAttemptTime >= FIND_COOLDOWN) {
                lastFindAttemptTime = Time.time;
                target = GameObject.FindGameObjectWithTag("Player")?.transform;
            }
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;
        
        if (useBounds) {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}

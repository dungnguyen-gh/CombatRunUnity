using UnityEngine;
using System.Collections;

/// <summary>
/// Smooth camera follow with screen shake support.
/// Updated for snappier, more responsive following.
/// </summary>
public class CameraFollow : MonoBehaviour {
    [Header("Follow Settings")]
    public Transform target;
    [Tooltip("Higher = snappier (5-10 for smooth, 15-25 for responsive)")]
    public float smoothSpeed = 15f;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Look Ahead")]
    [Tooltip("Disable for less laggy camera")]
    public bool useLookAhead = false;
    [Tooltip("Keep small (0.5-1) to avoid delay")]
    public float lookAheadDistance = 0.5f;
    public float lookAheadSpeed = 5f;
    
    [Header("Bounds")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    
    [Header("Zoom")]
    public bool useDynamicZoom = false;
    public float minZoom = 4f;
    public float maxZoom = 8f;
    public float zoomSpeed = 2f;
    private Camera cam;
    private float baseZoom;
    
    // Shake state
    private Vector3 shakeOffset;
    private float shakeMagnitude;
    private float shakeDuration;
    private bool isShaking = false;
    
    // Look ahead
    private Vector3 lookAheadPos;
    private Vector3 velocity;
    
    // Cached target pos for smoother following
    private Vector3 currentTargetPos;

    void Start() {
        cam = GetComponent<Camera>();
        baseZoom = cam.orthographicSize;
        
        // Try to find player immediately
        TryFindPlayer();
        
        // Initialize position immediately to prevent initial lag
        if (target != null) {
            currentTargetPos = target.position + offset;
            transform.position = currentTargetPos;
        }
    }

    /// <summary>
    /// Attempts to find the player if target is null.
    /// Call this when player spawns or to reacquire target.
    /// </summary>
    public void TryFindPlayer() {
        if (target != null) return;
        
        // Try to find by tag first
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            target = player.transform;
            Debug.Log("[CameraFollow] Found player by tag: " + player.name);
            return;
        }
        
        // Fallback: try to find PlayerController component
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) {
            target = pc.transform;
            Debug.Log("[CameraFollow] Found player by PlayerController: " + pc.name);
            return;
        }
        
        // Try GameManager reference
        if (GameManager.Instance != null && GameManager.Instance.player != null) {
            target = GameManager.Instance.player.transform;
            Debug.Log("[CameraFollow] Found player via GameManager: " + target.name);
            return;
        }
    }

    void LateUpdate() {
        // Try to find player if target is null (handles spawn order issues)
        if (target == null) {
            TryFindPlayer();
            
            // Still no target? Skip this frame
            if (target == null) return;
        }
        
        // Verify target is still valid (not destroyed)
        if (target.gameObject == null || !target.gameObject.activeInHierarchy) {
            target = null;
            return;
        }
        
        // Calculate desired position
        Vector3 desiredPos = target.position + offset;
        
        // Look ahead (optional - can cause perceived lag)
        if (useLookAhead) {
            var playerController = target.GetComponent<PlayerController>();
            if (playerController != null) {
                Vector2 moveDir = playerController.GetFacingDirection();
                Vector3 desiredLookAhead = new Vector3(moveDir.x, moveDir.y, 0) * lookAheadDistance;
                lookAheadPos = Vector3.Lerp(lookAheadPos, desiredLookAhead, lookAheadSpeed * Time.deltaTime);
                desiredPos += lookAheadPos;
            }
        }
        
        // Apply bounds
        if (useBounds) {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
        }
        
        // Smooth follow using Lerp for snappier response
        // SmoothDamp can feel heavy, Lerp is more responsive
        float t = smoothSpeed * Time.deltaTime;
        t = Mathf.Clamp01(t); // Prevent overshoot
        currentTargetPos = Vector3.Lerp(transform.position, desiredPos, t);
        
        // Apply position
        transform.position = currentTargetPos;
        
        // Apply shake
        if (isShaking) {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0;
            transform.position += shakeOffset;
            
            shakeDuration -= Time.deltaTime;
            shakeMagnitude = Mathf.Lerp(shakeMagnitude, 0, Time.deltaTime * 5f);
            
            if (shakeDuration <= 0) {
                isShaking = false;
                shakeOffset = Vector3.zero;
            }
        }
        
        // Dynamic zoom
        if (useDynamicZoom) {
            float targetZoom = baseZoom;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Sets the target transform directly (useful for cutscenes or manual assignment).
    /// </summary>
    public void SetTarget(Transform newTarget) {
        target = newTarget;
        if (target != null) {
            Debug.Log("[CameraFollow] Target set to: " + target.name);
        }
    }

    /// <summary>
    /// Clears the current target.
    /// </summary>
    public void ClearTarget() {
        target = null;
    }

    /// <summary>
    /// Triggers a screen shake effect.
    /// </summary>
    public void Shake(float duration, float magnitude) {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
        isShaking = true;
    }

    /// <summary>
    /// Smoothly moves camera to target (for cutscenes).
    /// </summary>
    public void MoveTo(Vector3 position, float duration) {
        StartCoroutine(MoveToCoroutine(position, duration));
    }

    IEnumerator MoveToCoroutine(Vector3 position, float duration) {
        Vector3 startPos = transform.position;
        float elapsed = 0;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector3.Lerp(startPos, position + offset, t);
            yield return null;
        }
    }

    void OnDrawGizmosSelected() {
        if (useBounds) {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
    }
}

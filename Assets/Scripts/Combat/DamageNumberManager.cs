using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object pool for floating damage number UI elements to avoid GC pressure.
/// Supports critical hit visualization with proper pool cleanup.
/// </summary>
public class DamageNumberManager : MonoBehaviour {
    public static DamageNumberManager Instance { get; private set; }
    
    [Header("Settings")]
    public GameObject damageNumberPrefab;
    public int poolSize = 20;
    public float numberLifetime = 1f;
    public float floatSpeed = 1f;
    public float critFontSizeMultiplier = 1.5f;

    private Queue<GameObject> damageNumberPool = new Queue<GameObject>();
    private Dictionary<GameObject, float> originalFontSizes = new Dictionary<GameObject, float>();
    
    // FIX: Cached TextMeshPro references to avoid repeated GetComponent calls
    private Dictionary<GameObject, TMPro.TextMeshPro> cachedTextMeshes = new Dictionary<GameObject, TMPro.TextMeshPro>();

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        } else {
            Destroy(gameObject);
        }
    }

    void InitializePool() {
        if (damageNumberPrefab == null) {
            Debug.LogWarning("DamageNumberManager: No prefab assigned!");
            return;
        }
        
        for (int i = 0; i < poolSize; i++) {
            GameObject obj = Instantiate(damageNumberPrefab, transform);
            obj.SetActive(false);
            damageNumberPool.Enqueue(obj);
            
            // FIX: Cache TextMeshPro reference
            var textMesh = obj.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null) {
                cachedTextMeshes[obj] = textMesh;
                
                // Store original font size
                if (!originalFontSizes.ContainsKey(obj)) {
                    originalFontSizes[obj] = textMesh.fontSize;
                }
            }
        }
    }

    /// <summary>
    /// Shows a damage number at the specified position.
    /// </summary>
    /// <param name="damage">The damage value to display</param>
    /// <param name="position">World position to spawn the number</param>
    /// <param name="isCrit">Whether this is a critical hit (affects color and size)</param>
    public void ShowDamage(int damage, Vector3 position, bool isCrit = false) {
        GameObject obj = GetFromPool();
        if (obj == null) {
            Debug.LogWarning("DamageNumberManager: Pool exhausted!");
            return;
        }

        obj.transform.position = position + Vector3.up * 0.5f;
        
        // FIX: Use cached TextMeshPro reference
        TMPro.TextMeshPro textMesh = null;
        if (!cachedTextMeshes.TryGetValue(obj, out textMesh)) {
            textMesh = obj.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null) {
                cachedTextMeshes[obj] = textMesh;
            }
        }
        
        if (textMesh != null) {
            // Reset to original font size first
            if (originalFontSizes.TryGetValue(obj, out float originalSize)) {
                textMesh.fontSize = originalSize;
            }
            
            textMesh.text = damage.ToString();
            textMesh.color = isCrit ? Color.yellow : Color.white;
            
            // Apply crit font size multiplier
            if (isCrit) {
                textMesh.fontSize *= critFontSizeMultiplier;
            }
        }

        obj.SetActive(true);
        
        // Animate and return to pool
        StartCoroutine(AnimateAndReturn(obj, textMesh));
    }

    GameObject GetFromPool() {
        if (damageNumberPool.Count > 0) {
            return damageNumberPool.Dequeue();
        }
        return null;
    }

    System.Collections.IEnumerator AnimateAndReturn(GameObject obj, TMPro.TextMeshPro textMesh) {
        float timer = 0;
        Vector3 startPos = obj.transform.position;
        
        while (timer < numberLifetime) {
            if (obj == null) yield break;
            
            timer += Time.deltaTime;
            obj.transform.position = startPos + Vector3.up * floatSpeed * timer;
            
            yield return null;
        }

        if (obj != null) {
            // FIX: Use cached TextMeshPro reference if the passed one is null
            if (textMesh == null) {
                cachedTextMeshes.TryGetValue(obj, out textMesh);
            }
            
            // Reset font size before returning to pool
            if (textMesh != null && originalFontSizes.TryGetValue(obj, out float originalSize)) {
                textMesh.fontSize = originalSize;
            }
            
            obj.SetActive(false);
            damageNumberPool.Enqueue(obj);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Base component for all UI panels in the game.
/// Handles CanvasGroup management, open/close animations, and gamepad navigation setup.
/// Attach this to any panel GameObject that needs to work with the UIManager pause stack system.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour {
    
    [Header("Panel Settings")]
    [Tooltip("Unique identifier for this panel. Used by UIManager for reference.")]
    public string panelId;
    
    [Tooltip("If true, opening this panel will pause the game (add to pause stack)")]
    public bool pausesGame = true;
    
    [Tooltip("If true, this panel can be closed with the Escape/Back button")]
    public bool closeableByEscape = true;
    
    [Tooltip("If true, panel will start hidden")]
    public bool startHidden = true;
    
    [Header("Animation")]
    [Tooltip("Duration of open/close animation in seconds")]
    public float animationDuration = 0.15f;
    
    [Tooltip("Animation curve for panel transitions")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("Type of animation to play when opening/closing")]
    public PanelAnimationType animationType = PanelAnimationType.Fade;
    
    [Header("DOTween Animation (Optional)")]
    [Tooltip("If true, uses DOTween for smoother animations instead of coroutines")]
    public bool useDOTween = true;
    
    [Tooltip("Bounce effect intensity for scale animations")]
    public float bounceIntensity = 1.2f;
    
    [Tooltip("Slide distance for slide animations")]
    public float slideDistance = 500f;
    
    [Header("Gamepad Navigation")]
    [Tooltip("First selectable element when panel opens (for gamepad navigation)")]
    public Selectable firstSelected;
    
    [Tooltip("If true, will automatically find first selectable child if none assigned")]
    public bool autoFindFirstSelectable = true;
    
    [Header("Sound Effects")]
    [Tooltip("Sound effect to play when panel opens")]
    public AudioClip openSound;
    
    [Tooltip("Sound effect to play when panel closes")]
    public AudioClip closeSound;
    
    [Tooltip("Sound effect to play when navigation changes (gamepad/keyboard)")]
    public AudioClip navigationSound;
    
    // Cached component references
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine currentAnimation;
    private bool isOpen = false;
    
    // Events
    /// <summary>Called when panel starts opening</summary>
    public System.Action OnPanelOpen;
    
    /// <summary>Called when panel finishes opening</summary>
    public System.Action OnPanelOpened;
    
    /// <summary>Called when panel starts closing</summary>
    public System.Action OnPanelClose;
    
    /// <summary>Called when panel finishes closing</summary>
    public System.Action OnPanelClosed;
    
    /// <summary>Called when navigation sound should play</summary>
    public System.Action OnNavigationSound;

    #region Properties
    
    /// <summary>
    /// Returns true if the panel is currently open or animating open.
    /// </summary>
    public bool IsOpen => isOpen;
    
    /// <summary>
    /// Returns true if the panel is currently animating.
    /// </summary>
    public bool IsAnimating => currentAnimation != null;
    
    /// <summary>
    /// Gets the CanvasGroup component (creates one if needed).
    /// </summary>
    public CanvasGroup CanvasGroup {
        get {
            if (canvasGroup == null) {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            return canvasGroup;
        }
    }
    
    #endregion

    #region Unity Lifecycle
    
    void Awake() {
        // Cache components
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // Auto-assign panel ID if empty
        if (string.IsNullOrEmpty(panelId)) {
            panelId = gameObject.name;
        }
        
        // Ensure CanvasGroup exists
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    void Start() {
        if (startHidden) {
            HideInstant();
        } else {
            ShowInstant();
        }
    }
    
    void OnEnable() {
        // Subscribe to input events for navigation sound
        // This would need to be connected to your input system
    }
    
    void OnDisable() {
        // Stop any running animations
        if (currentAnimation != null) {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Opens the panel with animation.
    /// </summary>
    /// <param name="playSound">If true, plays the open sound effect</param>
    public void Open(bool playSound = true) {
        if (isOpen) return;
        
        isOpen = true;
        OnPanelOpen?.Invoke();
        
        if (playSound && openSound != null) {
            PlaySound(openSound);
        }
        
        // Stop any existing animation
        KillCurrentAnimation();
        
        if (useDOTween) {
            AnimateOpenDOTween();
        } else {
            currentAnimation = StartCoroutine(AnimateOpen());
        }
    }
    
    /// <summary>
    /// Closes the panel with animation.
    /// </summary>
    /// <param name="playSound">If true, plays the close sound effect</param>
    public void Close(bool playSound = true) {
        if (!isOpen) return;
        
        isOpen = false;
        OnPanelClose?.Invoke();
        
        if (playSound && closeSound != null) {
            PlaySound(closeSound);
        }
        
        // Stop any existing animation
        KillCurrentAnimation();
        
        if (useDOTween) {
            AnimateCloseDOTween();
        } else {
            currentAnimation = StartCoroutine(AnimateClose());
        }
    }
    
    void KillCurrentAnimation() {
        if (currentAnimation != null) {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        // Kill any DOTween animations
        DOTween.Kill(rectTransform);
        DOTween.Kill(canvasGroup);
    }
    
    /// <summary>
    /// Toggles the panel open/closed state.
    /// </summary>
    public void Toggle() {
        if (isOpen) {
            Close();
        } else {
            Open();
        }
    }
    
    /// <summary>
    /// Instantly shows the panel without animation.
    /// </summary>
    public void ShowInstant() {
        isOpen = true;
        gameObject.SetActive(true);
        CanvasGroup.alpha = 1f;
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        SetupNavigation();
    }
    
    /// <summary>
    /// Instantly hides the panel without animation.
    /// </summary>
    public void HideInstant() {
        isOpen = false;
        CanvasGroup.alpha = 0f;
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Sets up gamepad/keyboard navigation for this panel.
    /// Call this after dynamically adding content.
    /// </summary>
    public void SetupNavigation() {
        if (firstSelected == null && autoFindFirstSelectable) {
            firstSelected = GetComponentInChildren<Selectable>();
        }
        
        if (firstSelected != null) {
            // Only set if using gamepad/keyboard (not mouse)
            if (UnityEngine.EventSystems.EventSystem.current != null) {
                var current = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                // If nothing selected or selection is outside this panel, select first
                if (current == null || !current.transform.IsChildOf(transform)) {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// Clears the current selection (useful when closing panel).
    /// </summary>
    public void ClearSelection() {
        if (UnityEngine.EventSystems.EventSystem.current != null) {
            var current = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(transform)) {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
    
    /// <summary>
    /// Plays the navigation sound (call this when navigating UI with gamepad/keyboard).
    /// </summary>
    public void PlayNavigationSound() {
        OnNavigationSound?.Invoke();
        if (navigationSound != null) {
            PlaySound(navigationSound);
        }
    }
    
    #endregion

    #region Animation Coroutines
    
    IEnumerator AnimateOpen() {
        gameObject.SetActive(true);
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        
        float elapsed = 0f;
        Vector2 startScale = Vector2.zero;
        Vector2 endScale = Vector2.one;
        
        while (elapsed < animationDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = animationCurve.Evaluate(Mathf.Clamp01(elapsed / animationDuration));
            
            switch (animationType) {
                case PanelAnimationType.Fade:
                    CanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    break;
                    
                case PanelAnimationType.Scale:
                    CanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    if (rectTransform != null) {
                        rectTransform.localScale = Vector2.Lerp(startScale, endScale, t);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromBottom:
                    CanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    if (rectTransform != null) {
                        float yOffset = Mathf.Lerp(-100f, 0f, t);
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, yOffset);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromTop:
                    CanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    if (rectTransform != null) {
                        float yOffset = Mathf.Lerp(100f, 0f, t);
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, yOffset);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromLeft:
                    CanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    if (rectTransform != null) {
                        float xOffset = Mathf.Lerp(-100f, 0f, t);
                        rectTransform.anchoredPosition = new Vector2(xOffset, rectTransform.anchoredPosition.y);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromRight:
                    CanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    if (rectTransform != null) {
                        float xOffset = Mathf.Lerp(100f, 0f, t);
                        rectTransform.anchoredPosition = new Vector2(xOffset, rectTransform.anchoredPosition.y);
                    }
                    break;
            }
            
            yield return null;
        }
        
        // Finalize
        CanvasGroup.alpha = 1f;
        if (rectTransform != null) {
            rectTransform.localScale = Vector2.one;
            // Reset position based on animation type
            if (animationType != PanelAnimationType.Fade && animationType != PanelAnimationType.Scale) {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        SetupNavigation();
        currentAnimation = null;
        OnPanelOpened?.Invoke();
    }
    
    IEnumerator AnimateClose() {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        ClearSelection();
        
        float elapsed = 0f;
        Vector2 startScale = Vector2.one;
        Vector2 endScale = Vector2.zero;
        Vector2 startPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        
        while (elapsed < animationDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = animationCurve.Evaluate(Mathf.Clamp01(elapsed / animationDuration));
            
            switch (animationType) {
                case PanelAnimationType.Fade:
                    CanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    break;
                    
                case PanelAnimationType.Scale:
                    CanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    if (rectTransform != null) {
                        rectTransform.localScale = Vector2.Lerp(startScale, endScale, t);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromBottom:
                    CanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    if (rectTransform != null) {
                        float yOffset = Mathf.Lerp(0f, -100f, t);
                        rectTransform.anchoredPosition = new Vector2(startPosition.x, yOffset);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromTop:
                    CanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    if (rectTransform != null) {
                        float yOffset = Mathf.Lerp(0f, 100f, t);
                        rectTransform.anchoredPosition = new Vector2(startPosition.x, yOffset);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromLeft:
                    CanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    if (rectTransform != null) {
                        float xOffset = Mathf.Lerp(0f, -100f, t);
                        rectTransform.anchoredPosition = new Vector2(xOffset, startPosition.y);
                    }
                    break;
                    
                case PanelAnimationType.SlideFromRight:
                    CanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    if (rectTransform != null) {
                        float xOffset = Mathf.Lerp(0f, 100f, t);
                        rectTransform.anchoredPosition = new Vector2(xOffset, startPosition.y);
                    }
                    break;
            }
            
            yield return null;
        }
        
        // Finalize
        CanvasGroup.alpha = 0f;
        if (rectTransform != null) {
            rectTransform.localScale = Vector2.one;
            rectTransform.anchoredPosition = startPosition;
        }
        
        gameObject.SetActive(false);
        currentAnimation = null;
        OnPanelClosed?.Invoke();
    }
    
    #endregion
    
    #region DOTween Animation Methods
    
    void AnimateOpenDOTween() {
        gameObject.SetActive(true);
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        
        Sequence sequence = DOTween.Sequence();
        
        switch (animationType) {
            case PanelAnimationType.Fade:
                CanvasGroup.alpha = 0f;
                sequence.Append(CanvasGroup.DOFade(1f, animationDuration).SetEase(Ease.OutCubic));
                break;
                
            case PanelAnimationType.Scale:
                CanvasGroup.alpha = 0f;
                rectTransform.localScale = Vector3.zero;
                sequence.Append(rectTransform.DOScale(1f, animationDuration).SetEase(Ease.OutBack, bounceIntensity));
                sequence.Join(CanvasGroup.DOFade(1f, animationDuration * 0.5f).SetEase(Ease.OutCubic));
                break;
                
            case PanelAnimationType.SlideFromBottom:
                CanvasGroup.alpha = 0f;
                Vector2 bottomPos = new Vector2(rectTransform.anchoredPosition.x, -slideDistance);
                rectTransform.anchoredPosition = bottomPos;
                sequence.Append(rectTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(Ease.OutBack, bounceIntensity));
                sequence.Join(CanvasGroup.DOFade(1f, animationDuration * 0.5f).SetEase(Ease.OutCubic));
                break;
                
            case PanelAnimationType.SlideFromTop:
                CanvasGroup.alpha = 0f;
                Vector2 topPos = new Vector2(rectTransform.anchoredPosition.x, slideDistance);
                rectTransform.anchoredPosition = topPos;
                sequence.Append(rectTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(Ease.OutBack, bounceIntensity));
                sequence.Join(CanvasGroup.DOFade(1f, animationDuration * 0.5f).SetEase(Ease.OutCubic));
                break;
                
            case PanelAnimationType.SlideFromLeft:
                CanvasGroup.alpha = 0f;
                Vector2 leftPos = new Vector2(-slideDistance, rectTransform.anchoredPosition.y);
                rectTransform.anchoredPosition = leftPos;
                sequence.Append(rectTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(Ease.OutBack, bounceIntensity));
                sequence.Join(CanvasGroup.DOFade(1f, animationDuration * 0.5f).SetEase(Ease.OutCubic));
                break;
                
            case PanelAnimationType.SlideFromRight:
                CanvasGroup.alpha = 0f;
                Vector2 rightPos = new Vector2(slideDistance, rectTransform.anchoredPosition.y);
                rectTransform.anchoredPosition = rightPos;
                sequence.Append(rectTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(Ease.OutBack, bounceIntensity));
                sequence.Join(CanvasGroup.DOFade(1f, animationDuration * 0.5f).SetEase(Ease.OutCubic));
                break;
        }
        
        sequence.OnComplete(() => {
            SetupNavigation();
            OnPanelOpened?.Invoke();
        });
    }
    
    void AnimateCloseDOTween() {
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        
        Sequence sequence = DOTween.Sequence();
        
        switch (animationType) {
            case PanelAnimationType.Fade:
                sequence.Append(CanvasGroup.DOFade(0f, animationDuration).SetEase(Ease.InCubic));
                break;
                
            case PanelAnimationType.Scale:
                sequence.Append(rectTransform.DOScale(0.8f, animationDuration).SetEase(Ease.InCubic));
                sequence.Join(CanvasGroup.DOFade(0f, animationDuration).SetEase(Ease.InCubic));
                break;
                
            case PanelAnimationType.SlideFromBottom:
                Vector2 bottomPos = new Vector2(rectTransform.anchoredPosition.x, -slideDistance);
                sequence.Append(rectTransform.DOAnchorPos(bottomPos, animationDuration).SetEase(Ease.InCubic));
                sequence.Join(CanvasGroup.DOFade(0f, animationDuration).SetEase(Ease.InCubic));
                break;
                
            case PanelAnimationType.SlideFromTop:
                Vector2 topPos = new Vector2(rectTransform.anchoredPosition.x, slideDistance);
                sequence.Append(rectTransform.DOAnchorPos(topPos, animationDuration).SetEase(Ease.InCubic));
                sequence.Join(CanvasGroup.DOFade(0f, animationDuration).SetEase(Ease.InCubic));
                break;
                
            case PanelAnimationType.SlideFromLeft:
                Vector2 leftPos = new Vector2(-slideDistance, rectTransform.anchoredPosition.y);
                sequence.Append(rectTransform.DOAnchorPos(leftPos, animationDuration).SetEase(Ease.InCubic));
                sequence.Join(CanvasGroup.DOFade(0f, animationDuration).SetEase(Ease.InCubic));
                break;
                
            case PanelAnimationType.SlideFromRight:
                Vector2 rightPos = new Vector2(slideDistance, rectTransform.anchoredPosition.y);
                sequence.Append(rectTransform.DOAnchorPos(rightPos, animationDuration).SetEase(Ease.InCubic));
                sequence.Join(CanvasGroup.DOFade(0f, animationDuration).SetEase(Ease.InCubic));
                break;
        }
        
        sequence.OnComplete(() => {
            gameObject.SetActive(false);
            OnPanelClosed?.Invoke();
        });
    }
    
    #endregion

    #region Private Methods
    
    void PlaySound(AudioClip clip) {
        if (clip == null) return;
        
        // Try to use a global audio source or play one-shot
        // This is a simple implementation - you may want to use your own AudioManager
        if (Camera.main != null) {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
        
        // Alternative: notify UIManager to play sound
        // UIManager.Instance?.PlaySoundEffect(clip);
    }
    
    #endregion
}

/// <summary>
/// Available animation types for panel transitions.
/// </summary>
public enum PanelAnimationType {
    /// <summary>Simple fade in/out</summary>
    Fade,
    
    /// <summary>Scale from zero to full size</summary>
    Scale,
    
    /// <summary>Slide in from bottom of screen</summary>
    SlideFromBottom,
    
    /// <summary>Slide in from top of screen</summary>
    SlideFromTop,
    
    /// <summary>Slide in from left of screen</summary>
    SlideFromLeft,
    
    /// <summary>Slide in from right of screen</summary>
    SlideFromRight
}

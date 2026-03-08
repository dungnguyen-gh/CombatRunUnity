using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Generic UI element animator using DOTween.
/// Attach to any UI element for hover, click, and idle animations.
/// </summary>
public class UIAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
    
    [Header("Hover Animation")]
    public bool animateHover = true;
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.15f;
    public Ease hoverEase = Ease.OutBack;
    
    [Header("Click Animation")]
    public bool animateClick = true;
    public float clickScale = 0.9f;
    public float clickDuration = 0.1f;
    public Ease clickEase = Ease.OutQuad;
    
    [Header("Idle Animation (Optional)")]
    public bool animateIdle = false;
    public IdleAnimationType idleType = IdleAnimationType.Pulse;
    public float idleDuration = 1f;
    public float idleMagnitude = 0.1f;
    public Ease idleEase = Ease.InOutSine;
    
    [Header("Color Animation (Optional)")]
    public bool animateColor = false;
    public Graphic targetGraphic;
    public Color hoverColor = Color.white;
    public Color pressedColor = Color.gray;
    public float colorTransitionDuration = 0.1f;
    
    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Color originalColor;
    private Tween currentTween;
    private Tween idleTween;
    private bool isHovering = false;
    private bool isPressed = false;

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        
        if (targetGraphic == null) {
            targetGraphic = GetComponent<Graphic>();
        }
        
        if (targetGraphic != null) {
            originalColor = targetGraphic.color;
        }
    }
    
    void OnEnable() {
        if (animateIdle) {
            StartIdleAnimation();
        }
    }
    
    void OnDisable() {
        KillAllTweens();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        isHovering = true;
        
        if (animateHover) {
            KillTween();
            currentTween = rectTransform.DOScale(originalScale * hoverScale, hoverDuration)
                .SetEase(hoverEase);
        }
        
        if (animateColor && targetGraphic != null) {
            targetGraphic.DOColor(hoverColor, colorTransitionDuration);
        }
        
        if (hoverSound != null) {
            PlaySound(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        isHovering = false;
        isPressed = false;
        
        if (animateHover) {
            KillTween();
            currentTween = rectTransform.DOScale(originalScale, hoverDuration)
                .SetEase(Ease.OutCubic);
        }
        
        if (animateColor && targetGraphic != null) {
            targetGraphic.DOColor(originalColor, colorTransitionDuration);
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        isPressed = true;
        
        if (animateClick) {
            KillTween();
            currentTween = rectTransform.DOScale(originalScale * clickScale, clickDuration)
                .SetEase(clickEase);
        }
        
        if (animateColor && targetGraphic != null) {
            targetGraphic.DOColor(pressedColor, colorTransitionDuration);
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (!isPressed) return;
        isPressed = false;
        
        if (animateClick || animateHover) {
            KillTween();
            float targetScale = isHovering ? hoverScale : 1f;
            currentTween = rectTransform.DOScale(originalScale * targetScale, clickDuration)
                .SetEase(Ease.OutBack);
        }
        
        if (animateColor && targetGraphic != null) {
            targetGraphic.DOColor(isHovering ? hoverColor : originalColor, colorTransitionDuration);
        }
        
        if (clickSound != null) {
            PlaySound(clickSound);
        }
    }
    
    void StartIdleAnimation() {
        KillIdleTween();
        
        switch (idleType) {
            case IdleAnimationType.Pulse:
                idleTween = rectTransform.DOScale(originalScale * (1f + idleMagnitude), idleDuration)
                    .SetEase(idleEase)
                    .SetLoops(-1, LoopType.Yoyo);
                break;
                
            case IdleAnimationType.Float:
                idleTween = rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + idleMagnitude * 50f, idleDuration)
                    .SetEase(idleEase)
                    .SetLoops(-1, LoopType.Yoyo);
                break;
                
            case IdleAnimationType.Shake:
                idleTween = rectTransform.DOShakeAnchorPos(idleDuration, idleMagnitude * 10f, 10, 90, false, true)
                    .SetLoops(-1, LoopType.Restart);
                break;
                
            case IdleAnimationType.Rotate:
                idleTween = rectTransform.DORotate(new Vector3(0, 0, idleMagnitude * 10f), idleDuration)
                    .SetEase(idleEase)
                    .SetLoops(-1, LoopType.Yoyo);
                break;
        }
    }
    
    void KillTween() {
        currentTween?.Kill();
        currentTween = null;
    }
    
    void KillIdleTween() {
        idleTween?.Kill();
        idleTween = null;
    }
    
    void KillAllTweens() {
        KillTween();
        KillIdleTween();
        DOTween.Kill(rectTransform);
    }
    
    void PlaySound(AudioClip clip) {
        if (clip == null) return;
        
        if (Camera.main != null) {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }
    
    void OnDestroy() {
        KillAllTweens();
    }
}

public enum IdleAnimationType {
    Pulse,
    Float,
    Shake,
    Rotate
}

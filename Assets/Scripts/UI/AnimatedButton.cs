using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Button with hover, click, and press animations using DOTween.
/// Attach to any Button GameObject for instant animation.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class AnimatedButton : MonoBehaviour {
    
    [Header("Animation Settings")]
    public bool animateHover = true;
    public bool animateClick = true;
    public bool animatePress = true;
    
    [Header("Hover")]
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.15f;
    public Ease hoverEase = Ease.OutBack;
    
    [Header("Click")]
    public float clickPunch = 0.2f;
    public float clickDuration = 0.2f;
    
    [Header("Press")]
    public float pressScale = 0.9f;
    public float pressDuration = 0.1f;
    
    [Header("Colors")]
    public bool animateColor = false;
    public Color hoverColor = Color.white;
    public Color pressedColor = Color.gray;
    
    private RectTransform rectTransform;
    private Button button;
    private Image buttonImage;
    private Vector3 originalScale;
    private Color originalColor;
    private Tween currentTween;

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        originalScale = rectTransform.localScale;
        if (buttonImage != null) originalColor = buttonImage.color;
        
        SetupEvents();
    }

    void SetupEvents() {
        // Hover events
        var trigger = GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) {
            trigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // Pointer Enter
        var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) => OnPointerEnter());
        trigger.triggers.Add(entryEnter);
        
        // Pointer Exit
        var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((data) => OnPointerExit());
        trigger.triggers.Add(entryExit);
        
        // Pointer Down
        var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown
        };
        entryDown.callback.AddListener((data) => OnPointerDown());
        trigger.triggers.Add(entryDown);
        
        // Pointer Up
        var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp
        };
        entryUp.callback.AddListener((data) => OnPointerUp());
        trigger.triggers.Add(entryUp);
        
        // Click
        button.onClick.AddListener(OnClick);
    }

    void OnPointerEnter() {
        if (!animateHover) return;
        
        KillTween();
        currentTween = rectTransform.DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(hoverEase);
        
        if (animateColor && buttonImage != null) {
            buttonImage.DOColor(hoverColor, hoverDuration);
        }
    }

    void OnPointerExit() {
        if (!animateHover) return;
        
        KillTween();
        currentTween = rectTransform.DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutCubic);
        
        if (animateColor && buttonImage != null) {
            buttonImage.DOColor(originalColor, hoverDuration);
        }
    }

    void OnPointerDown() {
        if (!animatePress) return;
        
        KillTween();
        currentTween = rectTransform.DOScale(originalScale * pressScale, pressDuration)
            .SetEase(Ease.OutQuad);
        
        if (animateColor && buttonImage != null) {
            buttonImage.DOColor(pressedColor, pressDuration);
        }
    }

    void OnPointerUp() {
        if (!animatePress) return;
        
        KillTween();
        currentTween = rectTransform.DOScale(originalScale, pressDuration)
            .SetEase(Ease.OutBack);
        
        if (animateColor && buttonImage != null) {
            buttonImage.DOColor(originalColor, pressDuration);
        }
    }

    void OnClick() {
        if (!animateClick) return;
        
        KillTween();
        rectTransform.DOPunchScale(
            new Vector3(clickPunch, clickPunch, clickPunch),
            clickDuration,
            1,
            0.5f
        );
    }

    void KillTween() {
        currentTween?.Kill();
    }

    void OnDestroy() {
        KillTween();
    }
}

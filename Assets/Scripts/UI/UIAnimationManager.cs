using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Central manager for UI animations using DOTween.
/// Provides smooth transitions for panels, buttons, and notifications.
/// </summary>
public class UIAnimationManager : MonoBehaviour {
    public static UIAnimationManager Instance { get; private set; }

    [Header("Animation Settings")]
    public float defaultDuration = 0.3f;
    public Ease defaultEase = Ease.OutCubic;
    public Ease bounceEase = Ease.OutBack;
    public Ease elasticEase = Ease.OutElastic;

    [Header("Panel Animation")]
    public float panelFadeInDuration = 0.25f;
    public float panelFadeOutDuration = 0.2f;
    public float panelScaleInDuration = 0.3f;
    public float panelScaleOutDuration = 0.2f;
    public float panelSlideDistance = 500f;

    [Header("Button Animation")]
    public float buttonHoverScale = 1.1f;
    public float buttonClickScale = 0.9f;
    public float buttonAnimationDuration = 0.15f;

    [Header("Notification Animation")]
    public float notificationSlideInDuration = 0.4f;
    public float notificationSlideOutDuration = 0.3f;
    public float notificationSlideDistance = 300f;

    [Header("Damage Number Animation")]
    public float damageNumberFloatDuration = 1f;
    public float damageNumberFloatDistance = 100f;
    public float damageNumberFadeDelay = 0.5f;

    // Tween tracking for cleanup
    private Dictionary<Transform, Tween> activeTweens = new Dictionary<Transform, Tween>();

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }

        // Initialize DOTween
        DOTween.Init();
    }

    #region Panel Animations

    /// <summary>
    /// Animates panel opening with fade and scale.
    /// </summary>
    public Tween AnimatePanelOpen(RectTransform panel, CanvasGroup canvasGroup) {
        KillTween(panel);

        panel.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();

        // Fade in
        sequence.Join(canvasGroup.DOFade(1f, panelFadeInDuration).SetEase(defaultEase));

        // Scale in with bounce
        sequence.Join(panel.DOScale(1f, panelScaleInDuration).SetEase(bounceEase));

        RegisterTween(panel, sequence);
        return sequence;
    }

    /// <summary>
    /// Animates panel closing with fade and scale.
    /// </summary>
    public Tween AnimatePanelClose(RectTransform panel, CanvasGroup canvasGroup, System.Action onComplete = null) {
        KillTween(panel);

        Sequence sequence = DOTween.Sequence();

        // Scale out
        sequence.Join(panel.DOScale(0.8f, panelScaleOutDuration).SetEase(Ease.InCubic));

        // Fade out
        sequence.Join(canvasGroup.DOFade(0f, panelFadeOutDuration).SetEase(Ease.InCubic));

        if (onComplete != null) {
            sequence.OnComplete(() => onComplete());
        }

        RegisterTween(panel, sequence);
        return sequence;
    }

    /// <summary>
    /// Slides panel in from specified direction.
    /// </summary>
    public Tween AnimatePanelSlideIn(RectTransform panel, SlideDirection direction, CanvasGroup canvasGroup = null) {
        KillTween(panel);

        Vector2 startPos = GetSlideStartPosition(panel, direction);
        Vector2 endPos = panel.anchoredPosition;

        panel.anchoredPosition = startPos;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();

        // Slide
        sequence.Join(panel.DOAnchorPos(endPos, panelFadeInDuration).SetEase(bounceEase));

        // Fade
        if (canvasGroup != null) {
            sequence.Join(canvasGroup.DOFade(1f, panelFadeInDuration).SetEase(defaultEase));
        }

        RegisterTween(panel, sequence);
        return sequence;
    }

    /// <summary>
    /// Slides panel out to specified direction.
    /// </summary>
    public Tween AnimatePanelSlideOut(RectTransform panel, SlideDirection direction, CanvasGroup canvasGroup = null, System.Action onComplete = null) {
        KillTween(panel);

        Vector2 endPos = GetSlideEndPosition(panel, direction);

        Sequence sequence = DOTween.Sequence();

        // Slide out
        sequence.Join(panel.DOAnchorPos(endPos, panelFadeOutDuration).SetEase(Ease.InCubic));

        // Fade
        if (canvasGroup != null) {
            sequence.Join(canvasGroup.DOFade(0f, panelFadeOutDuration).SetEase(Ease.InCubic));
        }

        if (onComplete != null) {
            sequence.OnComplete(() => onComplete());
        }

        RegisterTween(panel, sequence);
        return sequence;
    }

    Vector2 GetSlideStartPosition(RectTransform panel, SlideDirection direction) {
        Vector2 pos = panel.anchoredPosition;
        switch (direction) {
            case SlideDirection.Left: return new Vector2(pos.x - panelSlideDistance, pos.y);
            case SlideDirection.Right: return new Vector2(pos.x + panelSlideDistance, pos.y);
            case SlideDirection.Top: return new Vector2(pos.x, pos.y + panelSlideDistance);
            case SlideDirection.Bottom: return new Vector2(pos.x, pos.y - panelSlideDistance);
            default: return pos;
        }
    }

    Vector2 GetSlideEndPosition(RectTransform panel, SlideDirection direction) {
        Vector2 pos = panel.anchoredPosition;
        switch (direction) {
            case SlideDirection.Left: return new Vector2(pos.x - panelSlideDistance, pos.y);
            case SlideDirection.Right: return new Vector2(pos.x + panelSlideDistance, pos.y);
            case SlideDirection.Top: return new Vector2(pos.x, pos.y + panelSlideDistance);
            case SlideDirection.Bottom: return new Vector2(pos.x, pos.y - panelSlideDistance);
            default: return pos;
        }
    }

    #endregion

    #region Button Animations

    /// <summary>
    /// Animates button hover (scale up).
    /// </summary>
    public Tween AnimateButtonHover(Transform button) {
        KillTween(button);

        Tween tween = button.DOScale(buttonHoverScale, buttonAnimationDuration)
            .SetEase(bounceEase);

        RegisterTween(button, tween);
        return tween;
    }

    /// <summary>
    /// Animates button unhover (scale back to 1).
    /// </summary>
    public Tween AnimateButtonUnhover(Transform button) {
        KillTween(button);

        Tween tween = button.DOScale(1f, buttonAnimationDuration)
            .SetEase(defaultEase);

        RegisterTween(button, tween);
        return tween;
    }

    /// <summary>
    /// Animates button click (punch scale).
    /// </summary>
    public Tween AnimateButtonClick(Transform button) {
        KillTween(button);

        Tween tween = button.DOPunchScale(
            new Vector3(0.2f, 0.2f, 0.2f),
            buttonAnimationDuration,
            1,
            0.5f
        );

        RegisterTween(button, tween);
        return tween;
    }

    /// <summary>
    /// Animates button press (scale down then up).
    /// </summary>
    public Tween AnimateButtonPress(Transform button) {
        KillTween(button);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(button.DOScale(buttonClickScale, buttonAnimationDuration * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(button.DOScale(1f, buttonAnimationDuration * 0.5f).SetEase(bounceEase));

        RegisterTween(button, sequence);
        return sequence;
    }

    #endregion

    #region Notification Animations

    /// <summary>
    /// Animates notification sliding in from right.
    /// </summary>
    public Tween AnimateNotificationShow(RectTransform notification, CanvasGroup canvasGroup = null) {
        KillTween(notification);

        Vector2 startPos = new Vector2(notificationSlideDistance, notification.anchoredPosition.y);
        Vector2 endPos = notification.anchoredPosition;

        notification.anchoredPosition = startPos;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(notification.DOAnchorPos(endPos, notificationSlideInDuration).SetEase(bounceEase));
        if (canvasGroup != null) {
            sequence.Join(canvasGroup.DOFade(1f, notificationSlideInDuration * 0.5f).SetEase(defaultEase));
        }

        RegisterTween(notification, sequence);
        return sequence;
    }

    /// <summary>
    /// Animates notification sliding out to right.
    /// </summary>
    public Tween AnimateNotificationHide(RectTransform notification, CanvasGroup canvasGroup = null, System.Action onComplete = null) {
        KillTween(notification);

        Vector2 endPos = new Vector2(notificationSlideDistance, notification.anchoredPosition.y);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(notification.DOAnchorPos(endPos, notificationSlideOutDuration).SetEase(Ease.InCubic));
        if (canvasGroup != null) {
            sequence.Join(canvasGroup.DOFade(0f, notificationSlideOutDuration).SetEase(Ease.InCubic));
        }

        if (onComplete != null) {
            sequence.OnComplete(() => onComplete());
        }

        RegisterTween(notification, sequence);
        return sequence;
    }

    #endregion

    #region Skill Slot Animations

    /// <summary>
    /// Animates skill slot cooldown complete (punch scale + glow).
    /// </summary>
    public Tween AnimateSkillReady(Transform skillSlot) {
        KillTween(skillSlot);

        Tween tween = skillSlot.DOPunchScale(
            new Vector3(0.3f, 0.3f, 0.3f),
            0.3f,
            2,
            0.5f
        );

        RegisterTween(skillSlot, tween);
        return tween;
    }

    /// <summary>
    /// Animates skill cast (quick scale down then up).
    /// </summary>
    public Tween AnimateSkillCast(Transform skillSlot) {
        KillTween(skillSlot);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(skillSlot.DOScale(0.7f, 0.1f).SetEase(Ease.OutQuad));
        sequence.Append(skillSlot.DOScale(1f, 0.2f).SetEase(bounceEase));

        RegisterTween(skillSlot, sequence);
        return sequence;
    }

    /// <summary>
    /// Pulsing animation for active/charging skills.
    /// </summary>
    public Tween AnimateSkillCharging(Transform skillSlot) {
        KillTween(skillSlot);

        Tween tween = skillSlot.DOScale(1.1f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        RegisterTween(skillSlot, tween);
        return tween;
    }

    #endregion

    #region Damage Number Animations

    /// <summary>
    /// Animates damage number floating up and fading.
    /// </summary>
    public Tween AnimateDamageNumber(RectTransform damageNumber, CanvasGroup canvasGroup) {
        KillTween(damageNumber);

        Vector2 startPos = damageNumber.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, damageNumberFloatDistance);

        Sequence sequence = DOTween.Sequence();

        // Float up
        sequence.Join(damageNumber.DOAnchorPos(endPos, damageNumberFloatDuration).SetEase(Ease.OutCubic));

        // Fade out after delay
        sequence.Append(canvasGroup.DOFade(0f, damageNumberFloatDuration * 0.5f).SetEase(Ease.InCubic));

        sequence.OnComplete(() => Destroy(damageNumber.gameObject));

        RegisterTween(damageNumber, sequence);
        return sequence;
    }

    /// <summary>
    /// Animates critical hit damage (scale punch + shake).
    /// </summary>
    public Tween AnimateCriticalDamage(Transform damageNumber) {
        KillTween(damageNumber);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(damageNumber.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad));
        sequence.Append(damageNumber.DOShakeScale(0.3f, 0.3f, 10, 90));
        sequence.Append(damageNumber.DOScale(1f, 0.2f).SetEase(defaultEase));

        RegisterTween(damageNumber, sequence);
        return sequence;
    }

    #endregion

    #region Combo & Score Animations

    /// <summary>
    /// Animates combo text (scale punch).
    /// </summary>
    public Tween AnimateComboUpdate(Transform comboText, int comboCount) {
        KillTween(comboText);

        // Scale based on combo
        float targetScale = 1f + (comboCount * 0.1f);
        targetScale = Mathf.Min(targetScale, 2f);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(comboText.DOScale(targetScale, 0.15f).SetEase(Ease.OutQuad));
        sequence.Append(comboText.DOScale(1f, 0.3f).SetEase(elasticEase));

        RegisterTween(comboText, sequence);
        return sequence;
    }

    /// <summary>
    /// Animates gold gain (float up + fade).
    /// </summary>
    public Tween AnimateGoldGain(RectTransform goldText) {
        KillTween(goldText);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(goldText.DOAnchorPosY(goldText.anchoredPosition.y + 30f, 0.5f).SetEase(Ease.OutCubic));
        sequence.Join(goldText.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).SetEase(Ease.InCubic));

        RegisterTween(goldText, sequence);
        return sequence;
    }

    #endregion

    #region Screen Effects

    /// <summary>
    /// Shakes the entire screen.
    /// </summary>
    public Tween ShakeScreen(Transform camera, float duration = 0.5f, float strength = 1f) {
        return camera.DOShakePosition(duration, strength, 10, 90, false, true);
    }

    /// <summary>
    /// Flash the screen white (for damage/impact).
    /// </summary>
    public Tween FlashScreen(CanvasGroup flashOverlay, float duration = 0.2f) {
        KillTween(flashOverlay.transform);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(flashOverlay.DOFade(0.5f, duration * 0.3f).SetEase(Ease.OutQuad));
        sequence.Append(flashOverlay.DOFade(0f, duration * 0.7f).SetEase(Ease.InQuad));

        RegisterTween(flashOverlay.transform, sequence);
        return sequence;
    }

    #endregion

    #region Utility Methods

    void RegisterTween(Transform transform, Tween tween) {
        if (activeTweens.ContainsKey(transform)) {
            activeTweens[transform]?.Kill();
        }
        activeTweens[transform] = tween;

        tween.OnComplete(() => {
            if (activeTweens.ContainsKey(transform) && activeTweens[transform] == tween) {
                activeTweens.Remove(transform);
            }
        });
    }

    void KillTween(Transform transform) {
        if (activeTweens.ContainsKey(transform)) {
            activeTweens[transform]?.Kill();
            activeTweens.Remove(transform);
        }
    }

    /// <summary>
    /// Kills all active tweens. Call when changing scenes.
    /// </summary>
    public void KillAllTweens() {
        foreach (var tween in activeTweens.Values) {
            tween?.Kill();
        }
        activeTweens.Clear();
    }

    #endregion
}

public enum SlideDirection {
    Left,
    Right,
    Top,
    Bottom
}

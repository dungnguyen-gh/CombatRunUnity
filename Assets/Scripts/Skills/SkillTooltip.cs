using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Tooltip that displays skill information on hover.
/// Auto-follows mouse with smooth positioning.
/// </summary>
public class SkillTooltip : MonoBehaviour {
    [Header("References")]
    public RectTransform tooltipPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statsText;
    public Image skillIcon;
    public Image rarityBorder;
    
    [Header("Settings")]
    public Vector2 offset = new Vector2(15, 15);
    public float followSpeed = 15f;
    public bool constrainToScreen = true;
    
    private RectTransform canvasRect;
    private Canvas canvas;
    private SkillSO currentSkill;
    private Camera mainCamera;

    void Awake() {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas?.GetComponent<RectTransform>();
        mainCamera = Camera.main;
        
        Hide();
    }

    void Update() {
        if (gameObject.activeSelf) {
            FollowMouse();
        }
    }

    public void Show(SkillSO skill) {
        if (skill == null) return;
        currentSkill = skill;
        
        // Update content
        if (nameText != null) {
            nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(skill.GetRarityColor())}>{skill.skillName}</color>";
        }
        
        if (descriptionText != null) {
            descriptionText.text = skill.description;
        }
        
        if (statsText != null) {
            statsText.text = BuildStatsText(skill);
        }
        
        if (skillIcon != null) {
            skillIcon.sprite = skill.icon;
        }
        
        if (rarityBorder != null) {
            rarityBorder.color = skill.GetRarityColor();
        }
        
        gameObject.SetActive(true);
        FollowMouse();
    }

    public void Hide() {
        gameObject.SetActive(false);
        currentSkill = null;
    }

    string BuildStatsText(SkillSO skill) {
        string text = "";
        
        // Cooldown
        text += $"Cooldown: {skill.cooldownTime:F1}s\n";
        
        // Damage
        if (skill.damageMultiplier != 1f) {
            text += $"Damage: {skill.damageMultiplier:P0}\n";
        }
        
        // Range
        if (skill.range > 0) {
            text += $"Range: {skill.range:F1}m\n";
        }
        
        // Duration
        if (skill.duration > 0) {
            text += $"Duration: {skill.duration:F1}s\n";
        }
        
        // Cost
        if (skill.manaCost > 0) {
            text += $"Mana Cost: {skill.manaCost}\n";
        }
        
        return text;
    }

    void FollowMouse() {
        Vector2 mousePos = Input.mousePosition;
        
        // Convert to canvas space
        Vector2 targetPos;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
            targetPos = mousePos + offset;
        } else {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mousePos, canvas.worldCamera, out targetPos);
            targetPos += offset;
        }
        
        // Constrain to screen
        if (constrainToScreen && tooltipPanel != null) {
            Vector2 size = tooltipPanel.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;
            
            // Keep within bounds
            targetPos.x = Mathf.Clamp(targetPos.x, -canvasSize.x/2 + size.x/2, canvasSize.x/2 - size.x/2);
            targetPos.y = Mathf.Clamp(targetPos.y, -canvasSize.y/2 + size.y/2, canvasSize.y/2 - size.y/2);
        }
        
        // Smooth follow
        if (tooltipPanel != null) {
            tooltipPanel.anchoredPosition = Vector2.Lerp(
                tooltipPanel.anchoredPosition, 
                targetPos, 
                followSpeed * Time.deltaTime
            );
        }
    }
}

/// <summary>
/// Attach to skill slot to show tooltip on hover.
/// </summary>
public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public SkillSO skill;
    public SkillTooltip tooltip;
    
    void Start() {
        if (tooltip == null) {
            tooltip = FindFirstObjectByType<SkillTooltip>();
        }
    }
    
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) {
        if (tooltip != null && skill != null) {
            tooltip.Show(skill);
        }
    }
    
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) {
        tooltip?.Hide();
    }
    
    public void SetSkill(SkillSO newSkill) {
        skill = newSkill;
    }
}

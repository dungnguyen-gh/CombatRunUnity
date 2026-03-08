using UnityEngine;

/// <summary>
/// Bridge between our PlayerController and SPUM animation system.
/// Handles animation state syncing and parameter updates.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SPUMPlayerBridge : MonoBehaviour
{
    [Header("SPUM Components")]
    public SPUM_Prefabs spumPrefabs;
    public Animator spumAnimator;
    
    [Header("Animation State Indices")]
    public int idleAnimationIndex = 0;
    public int moveAnimationIndex = 0;
    public int attackAnimationIndex = 0;
    public int damagedAnimationIndex = 0;
    public int debuffAnimationIndex = 0;
    public int deathAnimationIndex = 0;
    
    [Header("Skill Animation Indices")]
    public int[] skillAnimationIndices = new int[4] { 1, 1, 1, 1 }; // Default to skill animation
    
    // Internal state tracking
    private PlayerController playerController;
    private bool wasMoving = false;
    private Vector2 lastFacingDirection = Vector2.down;
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        
        // Auto-find SPUM components if not assigned
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        
        if (spumAnimator == null && spumPrefabs != null)
            spumAnimator = spumPrefabs._anim;
    }
    
    void Start()
    {
        InitializeSPUM();
    }
    
    void Update()
    {
        UpdateAnimationState();
        UpdateFacingDirection();
    }
    
    void InitializeSPUM()
    {
        if (spumPrefabs == null)
        {
            Debug.LogError("SPUMPlayerBridge: SPUM_Prefabs not found!");
            return;
        }
        
        // Check if animator has a valid controller
        if (spumPrefabs._anim == null)
        {
            Debug.LogError("SPUMPlayerBridge: No Animator found on SPUM_Prefabs!");
            return;
        }
        
        // CRITICAL FIX: Check if controller is already an OverrideController
        if (spumPrefabs._anim.runtimeAnimatorController is AnimatorOverrideController)
        {
            // Already an override controller - skip initialization
            Debug.Log("[SPUMPlayerBridge] Animator already has OverrideController - skipping initialization");
        }
        else if (spumPrefabs._anim.runtimeAnimatorController != null)
        {
            // Safe to initialize - it's a base controller
            spumPrefabs.OverrideControllerInit();
        }
        else
        {
            Debug.LogError("SPUMPlayerBridge: No RuntimeAnimatorController assigned!");
            return;
        }
        
        // Ensure animation lists are populated
        try
        {
            if (!spumPrefabs.allListsHaveItemsExist())
            {
                spumPrefabs.PopulateAnimationLists();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SPUMPlayerBridge] PopulateAnimationLists failed: {ex.Message}");
        }
        
        // Start with idle animation
        PlayIdleAnimation();
    }
    
    void UpdateAnimationState()
    {
        if (spumPrefabs == null) return;
        
        // Get current input from PlayerController
        Vector2 moveInput = playerController.GetMoveInput();
        bool isMoving = moveInput.magnitude > 0.1f;
        
        // Handle movement state changes
        if (isMoving && !wasMoving)
        {
            PlayMoveAnimation();
        }
        else if (!isMoving && wasMoving)
        {
            PlayIdleAnimation();
        }
        
        wasMoving = isMoving;
    }
    
    void UpdateFacingDirection()
    {
        if (spumPrefabs == null) return;
        
        Vector2 facing = playerController.GetFacingDirection();
        if (facing.magnitude > 0.1f)
        {
            lastFacingDirection = facing;
            
            // Use rotation Y to flip character (more reliable than scale)
            // SPUM sprites are drawn facing LEFT by default
            // Y=180 flips to face right, Y=0 keeps default left-facing
            if (facing.x > 0.1f)
                spumPrefabs.transform.rotation = Quaternion.Euler(0, 180, 0);    // Face Right
            else if (facing.x < -0.1f)
                spumPrefabs.transform.rotation = Quaternion.Euler(0, 0, 0);      // Face Left
        }
    }
    
    // Public methods to be called by PlayerController
    
    public void PlayIdleAnimation()
    {
        spumPrefabs?.PlayAnimation(PlayerState.IDLE, idleAnimationIndex);
    }
    
    public void PlayMoveAnimation()
    {
        spumPrefabs?.PlayAnimation(PlayerState.MOVE, moveAnimationIndex);
    }
    
    public void PlayAttackAnimation()
    {
        spumPrefabs?.PlayAnimation(PlayerState.ATTACK, attackAnimationIndex);
    }
    
    public void PlayDamagedAnimation()
    {
        spumPrefabs?.PlayAnimation(PlayerState.DAMAGED, damagedAnimationIndex);
    }
    
    public void PlayDeathAnimation()
    {
        spumPrefabs?.PlayAnimation(PlayerState.DEATH, deathAnimationIndex);
    }
    
    public void PlayDebuffAnimation()
    {
        spumPrefabs?.PlayAnimation(PlayerState.DEBUFF, debuffAnimationIndex);
    }
    
    public void PlaySkillAnimation(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < skillAnimationIndices.Length)
        {
            int animIndex = skillAnimationIndices[skillIndex];
            // Use ATTACK state with different animation index for skills
            // Or use OTHER state depending on your setup
            spumPrefabs?.PlayAnimation(PlayerState.ATTACK, animIndex);
        }
    }
    
    // Animation event callbacks - can be called from animation events
    public void OnAttackAnimationStart()
    {
        // Called when attack animation starts
    }
    
    public void OnAttackAnimationHit()
    {
        // Called at the "hit" point of attack animation (damage frame)
        // You can add animation events to your SPUM animations to call this
    }
    
    public void OnAttackAnimationEnd()
    {
        // Called when attack animation ends
        PlayIdleAnimation();
    }
    
    // Helper to set animation speed (for attack speed buffs)
    public void SetAnimationSpeed(float speedMultiplier)
    {
        if (spumAnimator != null)
        {
            spumAnimator.speed = speedMultiplier;
        }
    }
    
    // Get current facing direction for attack calculations
    public Vector2 GetFacingDirection()
    {
        return lastFacingDirection;
    }
    
    void OnValidate()
    {
        if (spumPrefabs == null)
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        if (spumAnimator == null && spumPrefabs != null)
            spumAnimator = spumPrefabs._anim;
    }
}

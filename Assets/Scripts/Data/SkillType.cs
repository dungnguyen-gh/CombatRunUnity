/// <summary>
/// Enhanced skill types supporting dynamic gameplay.
/// </summary>
public enum SkillType { 
    // Basic Attack Types
    CircleAOE,    // Spin attack around player
    GroundAOE,    // Meteor/delayed explosion
    Projectile,   // Fireball/arrows
    Melee,        // Single target melee attack
    
    // Defensive Types
    Shield,       // Damage reduction buff
    Reflect,      // Reflects projectiles
    Heal,         // Self or AoE healing
    
    // Mobility Types
    Dash,         // Quick movement in direction
    Teleport,     // Instant position change
    Blink,        // Short range teleport
    
    // Summon Types
    Summon,       // Spawn allied units
    Turret,       // Stationary defensive unit
    Totem,        // Buff/debuff area
    
    // Channeling Types
    Beam,         // Continuous laser/beam
    Channel,      // Hold to charge/effect
    
    // Utility Types
    Buff,         // Stat enhancement
    Trap,         // Deployable hazard
    Chain,        // Bouncing between targets
    AreaDenial,   // Persistent damaging zone
    
    // Special Types
    Transform,    // Change form/abilities
    TimeWarp      // Slow enemies/speed self
}

/// <summary>
/// How the skill determines its target location.
/// </summary>
public enum SkillTargeting {
    Self,           // Centered on player
    MousePosition,  // At cursor location
    Directional,    // In facing direction
    TargetEnemy,    // Requires enemy under cursor
    TargetAlly,     // Requires ally under cursor
    AreaSelect,     // Click and drag area
    AutoTarget      // Automatically targets nearest enemy
}

/// <summary>
/// Skill rarity for progression systems.
/// </summary>
public enum SkillRarity {
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

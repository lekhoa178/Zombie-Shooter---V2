// Scriptable Object for enemy configuration

using UnityEngine;
using Flyweight;
using MoreMountains.Feedbacks;

// Base EnemyData class
[CreateAssetMenu(fileName = "EnemyData", menuName = "AI/Enemy Data/Base Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Stats")]
    public string enemyName = "Enemy";
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;
    
    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    
    [Header("Telegraph Settings")]
    public float telegraphDuration = 0.2f;
    public VFXFlyweightSettings telegraphVfxSettings;
    
    [Header("Hit Reaction Settings")]
    [Tooltip("Reduces push back distance by this percentage (0-1)")]
    public float pushBackResistance = 0.3f;
    [Tooltip("Reduces stun duration by this percentage (0-1)")]
    public float stunDurationResistance = 0.2f;
    [Tooltip("If true, small enemies get pushed further on death")]
    public bool applyExtraDeathPushBack = true;
    [Tooltip("Multiplier for push back distance when this enemy dies")]
    public float deathPushBackMultiplier = 1.5f;
    
    [Header("Stun Recovery System")]
    [Tooltip("Whether this enemy builds up stun resistance")]
    public bool enableStunResistance = true;
    [Tooltip("How much 'stun stress' the enemy can take before becoming temporarily immune")]
    public float stunResistanceThreshold = 3f;
    [Tooltip("How fast stun stress accumulates per second while stunned")]
    public float stunStressAccumulationRate = 2f;
    [Tooltip("How fast stun stress decays per second when not stunned")]
    public float stunStressDecayRate = 0.5f;
    [Tooltip("How long the enemy is immune to stuns after recovery")]
    public float stunRecoveryDuration = 5f;
    [Tooltip("How long until stun stress fully resets if not stunned")]
    public float stunStressResetDelay = 10f;
    [Tooltip("VFX to play when enemy enters stun recovery mode")]
    public VFXFlyweightSettings stunRecoveryVfxSettings;
    [Tooltip("Empower the enemy during stun recovery (increase damage/speed)")]
    public bool empowerDuringRecovery = false;
    [Tooltip("Damage multiplier during recovery state")]
    public float recoveryDamageMultiplier = 1.2f;
    [Tooltip("Speed multiplier during recovery state")]
    public float recoverySpeedMultiplier = 1.2f;
    
    [Header("Pushback Settings")]
    [Tooltip("If true, enemy will be pushed back when hit")]
    public bool attackCanPushBack = false;
    [Tooltip("How far the enemy is pushed back when hit")]
    public float attackPushbackDistance = 1f;
    [Tooltip("How long the enemy is pushed back when hit")]
    public float attackPushbackDuration = 0.5f;
    
    [Header("Knockback Settings")]
    [Tooltip("If true, enemy will be knocked back when hit")]
    public bool attackCanStun = false;
    [Tooltip("Threshold damage for knockback to occur")]
    public float attackStunThreshold = 5f;
    [Tooltip("How long the enemy is stunned when hit")]
    public float attackStunDuration = 1f;
    
    [Header("Attack VFX/SFX")]
    // Hit VFX Configuration
    public VFXFlyweightSettings hitVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool overrideHitVfxScale = false;
    [Tooltip("Only used if overrideHitVfxScale is enabled")]
    public Vector3 hitVfxScale = Vector3.one;

    [Header("Additional Attack VFX")]
    public bool enableAdditionalVfx = false;
    public VFXFlyweightSettings additionalVfxSettings;
    [Tooltip("If true, will spawn VFX for each hit. If false, spawns only once per attack")]
    public bool spawnPerHit = false;
    [Tooltip("Where to spawn the additional VFX")]
    public bool spawnAtHitLocation = true;
    public bool spawnAtEnemyLocation = false;
    [Tooltip("Additional offset from spawn location")]
    public Vector3 additionalVfxOffset = Vector3.zero;
    [Tooltip("Rotation offset for the additional VFX")]
    public Vector3 additionalVfxRotation = Vector3.zero;
    [Tooltip("If true, VFX will face the hit direction")]
    public bool faceHitDirection = false;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool overrideAdditionalVfxScale = false;
    [Tooltip("Only used if overrideAdditionalVfxScale is enabled")]
    public Vector3 additionalVfxScale = Vector3.one;
    [Tooltip("Delay before spawning the additional VFX")]
    public float additionalVfxDelay = 0f;

    [Header("Hit Reaction VFX")]
    public VFXFlyweightSettings hitReactionVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool overrideHitReactionVfxScale = false;
    [Tooltip("Only used if overrideHitReactionVfxScale is enabled")]
    public Vector3 hitReactionVfxScale = Vector3.one;
    
    [Header("Additional Hit Reaction VFX")]
    public bool enableAdditionalHitReactionVfx = false;
    public VFXFlyweightSettings additionalHitReactionVfxSettings;
    [Tooltip("If true, will spawn VFX for each hit")]
    public bool spawnHitReactionPerHit = true;
    [Tooltip("Where to spawn the additional VFX")]
    public bool spawnHitReactionAtHitLocation = true;
    [Tooltip("Additional offset from spawn location")]
    public Vector3 additionalHitReactionVfxOffset = Vector3.zero;
    [Tooltip("Rotation offset for the additional VFX")]
    public Vector3 additionalHitReactionVfxRotation = Vector3.zero;
    [Tooltip("If true, VFX will face the hit direction")]
    public bool hitReactionFaceHitDirection = true;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool overrideAdditionalHitReactionVfxScale = false;
    [Tooltip("Only used if overrideAdditionalHitReactionVfxScale is enabled")]
    public Vector3 additionalHitReactionVfxScale = Vector3.one;
    [Tooltip("Delay before spawning the additional VFX")]
    public float additionalHitReactionVfxDelay = 0f;

    [Header("Movement Start VFX")]
    [Tooltip("Enable VFX when the enemy starts moving")]
    public bool enableStartMovingVfx = false;
    [Tooltip("The VFX to spawn when enemy begins movement")]
    public VFXFlyweightSettings startMovingVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool overrideStartMovingVfxScale = false;
    [Tooltip("Only used if overrideStartMovingVfxScale is enabled")]
    public Vector3 startMovingVfxScale = Vector3.one;
    [Tooltip("Offset from the enemy position where the start movement VFX should appear")]
    public Vector3 startMovingVfxOffset = new Vector3(0, 0.1f, 0);
    [Tooltip("Rotation of the start movement VFX")]
    public Vector3 startMovingVfxRotation = new Vector3(-90, 0, 0);
    [Tooltip("Minimum speed required to spawn start movement VFX")]
    public float minSpeedForStartMovingVfx = 0.5f;
    [Tooltip("Minimum time between spawning start movement VFX (prevents spam when stopping/starting)")]
    public float startMovingVfxCooldown = 1.0f;

    [Header("Movement Step VFX")]
    [Tooltip("Enable step VFX when the enemy moves")]
    public bool enableStepVfx = false;
    [Tooltip("The VFX to spawn when enemy takes a step")]
    public VFXFlyweightSettings stepVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool overrideStepVfxScale = false;
    [Tooltip("Only used if overrideStepVfxScale is enabled")]
    public Vector3 stepVfxScale = Vector3.one;
    [Tooltip("How frequently to spawn step VFX in seconds")]
    public float stepVfxInterval = 0.3f;
    [Tooltip("Offset from the foot position where the step VFX should appear")]
    public Vector3 stepVfxOffset = new Vector3(0, 0.05f, 0);
    [Tooltip("Rotation of the step VFX")]
    public Vector3 stepVfxRotation = new Vector3(-90, 0, 0);
    [Tooltip("Should step VFX alternate between left and right feet")]
    public bool alternateStepVfx = true;
    [Tooltip("Offset for left foot (only used if alternateStepVfx is true)")]
    public Vector3 leftFootOffset = new Vector3(-0.2f, 0.05f, 0);
    [Tooltip("Offset for right foot (only used if alternateStepVfx is true)")]
    public Vector3 rightFootOffset = new Vector3(0.2f, 0.05f, 0);
    [Tooltip("Scale step VFX intensity based on movement speed")]
    public bool scaleStepVfxWithSpeed = true;
    [Tooltip("Minimum speed required to spawn step VFX")]
    public float minSpeedForStepVfx = 0.5f;

    public GameObject deathVFX;
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;

    [Tooltip("If true, enemy will rotate to face away from the hit direction when damaged")]
    public bool rotateToFaceHitDirection = true;
    [Tooltip("How quickly the enemy rotates to face the hit direction (degrees per second)")]
    public float hitDirectionRotationSpeed = 10f;
    [Tooltip("If true, enemy only faces hit direction when stunned")]
    public bool onlyRotateWhenStunned = false;
    [Tooltip("If true, enemy only faces hit direction when pushed back")]
    public bool onlyRotateWhenPushedBack = false;

    // Helper methods to get the appropriate scale for each VFX type
    public Vector3 GetHitVfxScale()
    {
        return overrideHitVfxScale ? hitVfxScale : Vector3.zero; // Zero signals to use prefab scale
    }
    
    public Vector3 GetAdditionalVfxScale()
    {
        return overrideAdditionalVfxScale ? additionalVfxScale : Vector3.zero;
    }
    
    public Vector3 GetHitReactionVfxScale()
    {
        return overrideHitReactionVfxScale ? hitReactionVfxScale : Vector3.zero;
    }
    
    public Vector3 GetAdditionalHitReactionVfxScale()
    {
        return overrideAdditionalHitReactionVfxScale ? additionalHitReactionVfxScale : Vector3.zero;
    }
    
    public Vector3 GetStepVfxScale()
    {
        return overrideStepVfxScale ? stepVfxScale : Vector3.zero;
    }
    
    public Vector3 GetStartMovingVfxScale()
    {
        return overrideStartMovingVfxScale ? startMovingVfxScale : Vector3.zero;
    }
}

// Hit reaction types
public enum HitReactionType
{
    Small,
    Medium,
    Large,
    Stagger,
    Knockback,
    Knockdown
}
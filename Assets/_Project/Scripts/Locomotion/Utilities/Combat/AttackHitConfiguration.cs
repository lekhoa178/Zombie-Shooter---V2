using Flyweight;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack Hit Config", menuName = "Combat/Attack Hit Configuration")]
public class AttackHitConfiguration : ScriptableObject
{
    public enum DetectionShape
    {
        Circle,
        HalfCircle,
        Cone,
        Box
    }

    [Header("Shape Configuration")] public DetectionShape Shape = DetectionShape.Circle;

    public bool FallbackToDefaultOnNoHits = true;

    // Circle/HalfCircle/Cone parameters
    public float Radius = 0.5f;

    // HalfCircle/Cone parameters
    [Range(0, 360)] public float ArcAngle = 120f;

    // Box parameters
    public Vector3 BoxSize = new Vector3(1f, 0.5f, 1f);

    [Header("Position Configuration")] public Vector3 Offset = new Vector3(0, 0, 1f);
    public bool OffsetFromColliderCenter = true;

    [Header("Hit Effects")] public float Damage = 10f;

    // Hit Stop configuration
    [Header("Hit Stop")]
    public bool EnableHitStop = true;
    public float HitStopDuration = 0.1f;
    [Tooltip("How much to slow down time during hit stop")]
    [Range(0.01f, 1f)]
    public float HitStopTimeScale = 0.1f;
    [Tooltip("Apply hit stop to player only, enemies only, or both")]
    public enum HitStopTarget { Player, Enemy, Both }
    public HitStopTarget HitStopAffects = HitStopTarget.Both;

    // Camera shake configuration
    [Header("Camera Effects")]
    public bool EnableCameraShake = true;
    public float CameraShakeIntensity = 0.1f;
    public float CameraShakeDuration = 0.1f;

    public VFXFlyweightSettings HitVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool OverrideHitVfxScale = false;
    [Tooltip("Only used if OverrideHitVfxScale is enabled")]
    public Vector3 HitVfxScale = Vector3.one;

    [Header("Additional VFX")]
    public bool EnableAdditionalVfx = false;
    public VFXFlyweightSettings AdditionalVfxSettings;
    [Tooltip("If true, will spawn VFX even when attack doesn't hit anything")]
    public bool SpawnEvenWithoutHit = false;
    [Tooltip("If true, will spawn VFX for each hit collision. If false, spawns only once per attack")]
    public bool SpawnPerCollision = false;
    [Tooltip("Where to spawn the additional VFX")]
    public bool SpawnAtHitLocation = true;
    public bool SpawnAtPlayerLocation = false;
    [Tooltip("Additional offset from spawn location")]
    public Vector3 AdditionalVfxOffset = Vector3.zero;
    [Tooltip("Rotation offset for the additional VFX")]
    public Vector3 AdditionalVfxRotation = Vector3.zero;
    [Tooltip("If true, VFX will face the hit direction")]
    public bool FaceHitDirection = false;
    [Tooltip("If true, VFX will inherit player's rotation")]
    public bool InheritPlayerRotation = false;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool OverrideAdditionalVfxScale = true;
    [Tooltip("Only used if OverrideAdditionalVfxScale is enabled")]
    public Vector3 AdditionalVfxScale = Vector3.one;
    [Tooltip("Delay before spawning the additional VFX")]
    public float AdditionalVfxDelay = 0f;

    [Header("Critical Hit")]
    public bool EnableCriticalHit = false;
    [Range(0, 100)]
    public float CriticalHitChance = 10f;
    public float CriticalHitDamageMultiplier = 2f;
    public VFXFlyweightSettings CriticalHitVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool OverrideCriticalVfxScale = true;
    [Tooltip("Only used if OverrideCriticalVfxScale is enabled")]
    public Vector3 CriticalVfxScale = Vector3.one;
    public Color CriticalHitColor = Color.red;

    [Header("Area Effect")]
    public bool EnableAreaEffect = false;
    public float AreaEffectRadius = 2f;
    public VFXFlyweightSettings AreaEffectVfxSettings;
    [Tooltip("Enable to override the prefab's original scale")]
    public bool OverrideAreaEffectVfxScale = false;
    [Tooltip("Only used if OverrideAreaEffectVfxScale is enabled")]
    public Vector3 AreaEffectVfxScale = Vector3.one;
    public float AreaEffectDamage = 5f;

    // Area effect hit stop configuration
    public bool EnableAreaHitStop = true;
    public float AreaHitStopDuration = 0.05f;
    [Range(0.01f, 1f)]
    public float AreaHitStopTimeScale = 0.2f;
    public HitStopTarget AreaHitStopAffects = HitStopTarget.Both;

    // Area effect camera shake
    public bool EnableAreaCameraShake = true;
    public float AreaCameraShakeIntensity = 0.08f;
    public float AreaCameraShakeDuration = 0.08f;

    [Header("Debug")] public bool DebugEnabled = true;
    public Color DebugColor = Color.red;
    public float DebugDuration = 1f;

    [Header("Animation")]
    public bool UseCustomAnimation = false;
    [Tooltip("Animation clip to play for this attack. Leave empty to use the default animation system.")]
    public AnimationClip AnimationClip;
    [Tooltip("Name of animator state to override (leave empty to use 'NormalAttack' state)")]
    public string AnimatorStateName = "NormalAttack";
    [Tooltip("Override animation speed")]
    public bool OverrideAnimationSpeed = false;
    [Tooltip("Animation playback speed multiplier")]
    [Range(0.1f, 2f)]
    public float AnimationSpeedMultiplier = 1f;
    [Tooltip("Root motion settings for this attack")]
    public bool UseRootMotion = true;
    [Tooltip("Time when hit detection should occur (0-1 normalized time)")]
    [Range(0f, 1f)]
    public float HitTiming = 0.5f;

    [Header("Attack Invincibility Frames")]
    public bool EnableAttackIFrames = false;
    public float AttackIFrameDuration = 0.2f;

    [Header("Weapon Configuration")]
    public bool UseWeapon = false;
    [Tooltip("The weapon prefab to spawn")]
    public GameObject WeaponPrefab;
    public enum WeaponHand
    {
        None,
        Left,
        Right,
        Both
    }
    [Tooltip("Which hand(s) should hold the weapon")]
    public WeaponHand WeaponSlot = WeaponHand.Right;
    [Tooltip("Should the weapon be removed after the attack")]
    public bool RemoveWeaponAfterAttack = true;
    [Tooltip("Local position offset for the weapon")]
    public Vector3 WeaponPositionOffset = Vector3.zero;
    [Tooltip("Local rotation offset for the weapon")]
    public Vector3 WeaponRotationOffset = Vector3.zero;

    [Header("Targeting Settings")]
    [Tooltip("Override global target detection settings for this attack")]
    public bool OverrideTargetDetectionSettings = false;
    
    [Tooltip("Detection range for this specific attack")]
    [Range(0f, 15f)]
    public float DetectionRange = 10f;
    
    [Tooltip("Dot product threshold for this attack (higher is more strict alignment)")]
    [Range(0f, 1f)]
    public float DotProductThreshold = 0.15f;

    // Helper methods to get the appropriate scale for each VFX type
    public Vector3 GetHitVfxScale()
    {
        return OverrideHitVfxScale ? HitVfxScale : Vector3.zero; // Zero signals to use prefab scale
    }
    
    public Vector3 GetAdditionalVfxScale()
    {
        return OverrideAdditionalVfxScale ? AdditionalVfxScale : Vector3.zero;
    }
    
    public Vector3 GetCriticalVfxScale()
    {
        return OverrideCriticalVfxScale ? CriticalVfxScale : Vector3.zero;
    }
    
    public Vector3 GetAreaEffectVfxScale()
    {
        return OverrideAreaEffectVfxScale ? AreaEffectVfxScale : Vector3.zero;
    }

    // Helper methods to get the targeting settings (either override or default)
    public float GetDetectionRange(ComboConfiguration comboConfig)
    {
        return OverrideTargetDetectionSettings ? 
            DetectionRange : comboConfig.DefaultDetectionRange;
    }
    
    public float GetDotProductThreshold(ComboConfiguration comboConfig)
    {
        return OverrideTargetDetectionSettings ? 
            DotProductThreshold : comboConfig.DefaultDotProductThreshold;
    }
}
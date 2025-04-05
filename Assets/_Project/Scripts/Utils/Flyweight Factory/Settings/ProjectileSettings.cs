using Unity.VisualScripting;
using UnityEngine;

namespace Flyweight
{
    [CreateAssetMenu(menuName = "Flyweight/Projectile Settings")]
    public class ProjectileSettings : FlyweightSettings
    {
        [Header("Basic Settings")]
        [Tooltip("Base damage dealt by this projectile")]
        public float damage = 10f;
        [Tooltip("Base speed of the projectile in units per second")]
        public float speed = 10f;
        [Tooltip("Time in seconds before the projectile automatically despawns")]
        public float despawnDelay = 5f;
        [Tooltip("Maximum distance the projectile can travel before despawning")]
        public float maxTravelDistance = 50f;
        
        [Header("Movement Type")]
        [Tooltip("Whether this projectile uses parabolic (arc) trajectory by default")]
        public bool useParabolicTrajectory = false;
        [Tooltip("For straight projectiles only: constrains movement to the horizontal plane (zeros out Y component)")]
        public bool flatTrajectory = false;
        [Tooltip("Maximum angle for direction variance (prevents exceeding spread angle)")]
        public float maxDirectionVariance = 5f;
        
        [Header("Parabolic Trajectory Settings")]
        [Tooltip("Default height of the parabolic arc")]
        public float parabolicHeight = 3f;
        [Tooltip("For parabolic trajectories: time in seconds to reach target (consistent regardless of distance)")]
        public float timeToTarget = 1.2f;
        [Tooltip("Whether to use random height variation for parabolic trajectory")]
        public bool useRandomHeight = false;
        [Tooltip("Minimum height for random variation (only used if useRandomHeight is true)")]
        public float minRandomHeight = 2f;
        [Tooltip("Maximum height for random variation (only used if useRandomHeight is true)")]
        public float maxRandomHeight = 5f;
        
        [Header("Homing Settings")]
        [Tooltip("Should the projectile seek targets?")]
        public bool isHoming = false;
        [Tooltip("How strongly the projectile homes in on its target (higher values = faster turning)")]
        [Range(0.0f, 1.0f)]
        public float homingStrength = 0.5f;
        [Tooltip("Maximum angle in degrees that the projectile can turn to acquire targets")]
        public float homingAngle = 45f;
        
        [Header("Parabolic Homing Settings")]
        [Tooltip("Enable homing for parabolic projectiles")]
        public bool enableHomingForParabolic = false;
        [Tooltip("How aggressively parabolic projectiles adjust their trajectory (higher = more aggressive)")]
        [Range(0.0f, 1.0f)]
        public float parabolicHomingFactor = 0.5f;
        [Tooltip("When enabled, the projectile stops tracking after a certain point in its trajectory")]
        public bool useCutoffHoming = false;
        [Tooltip("Normalized time (0-1) when tracking stops and projectile maintains last trajectory")]
        [Range(0.0f, 1.0f)]
        public float homingCutoffTime = 0.7f;
        [Tooltip("How quickly homing fades as the projectile approaches cutoff (lower = more abrupt)")]
        [Range(0.01f, 0.5f)]
        public float homingCutoffFade = 0.1f;
        
        [Header("Collision Settings")]
        [Tooltip("Layers this projectile will collide with")]
        public LayerMask hitLayers = -1;
        [Tooltip("Whether this projectile can hit multiple targets before despawning")]
        public bool canPierceTargets = false;
        [Tooltip("Maximum number of targets that can be hit if piercing is enabled")]
        public int maxPierceCount = 3;
        
        [Header("Area Damage Settings")]
        [Tooltip("Radius of area damage for parabolic projectiles")]
        public float areaRadius = 2f;
        [Tooltip("Use direct collision instead of area damage for parabolic projectiles")]
        public bool useDirectCollisionForParabolic = false;
        [Tooltip("Show prediction zone for parabolic projectiles")]
        public bool showPredictionZone = true;
        [Tooltip("Custom settings for the prediction zone")]
        public FlyweightSettings predictionZoneSettings;
        
        [Header("Visual Settings")]
        [Tooltip("Override scale of the projectile (1,1,1 = default)")]
        public Vector3 projectileScale = Vector3.one;
        [Tooltip("Y-axis offset from spawn position")]
        public float yPositionOffset = 0f;
        [Tooltip("Scale modifier for hit VFX relative to area radius (1.0 = same size as radius)")]
        public float vfxScaleModifier = 1.0f;
        
        [Header("Effects")]
        [Tooltip("Trail effect for the projectile")]
        public GameObject trailPrefab;
        [Tooltip("VFX spawned when hitting something")]
        public VFXFlyweightSettings hitVFXSettings;
        [Tooltip("Audio clip played when projectile is fired")]
        public AudioClip fireSound;
        [Tooltip("Audio clip played when projectile hits something")]
        public AudioClip hitSound;
        
        [Header("Status Effects")]
        [Tooltip("Whether this projectile can apply status effects")]
        public bool appliesStatusEffects = false;
        [Tooltip("Type of status effect to apply")]
        public string statusEffectType;
        [Tooltip("Duration of the status effect in seconds")]
        public float statusEffectDuration = 3f;
        
        [Header("Debug")]
        [Tooltip("Show debug information for this projectile in the console and scene view")]
        public bool showDebugInfo = false;

        public override Flyweight Create()
        {
            GameObject go = Instantiate(prefab);
            go.SetActive(false);
            go.name = prefab.name;

            Projectile flyweight = go.AddComponent<Projectile>();
            flyweight.settings = this;
            
            // Apply custom scale if not default
            if (projectileScale != Vector3.one)
            {
                go.transform.localScale = projectileScale;
            }
            
            // Add trail if specified
            if (trailPrefab)
            {
                GameObject trail = Instantiate(trailPrefab, go.transform);
                trail.transform.localPosition = Vector3.zero;
            }
            
            flyweight.trailRenderers = flyweight.GetComponentsInChildren<TrailRenderer>(true);

            // Set up collision layers
            Collider[] colliders = go.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                foreach (Collider collider in colliders)
                {
                    collider.includeLayers = hitLayers;
                    collider.excludeLayers = ~hitLayers;
                    collider.gameObject.layer = LayerMask.NameToLayer("Projectile");
                }
            }

            return flyweight;
        }
        
        public override void OnGet(Flyweight flyweight)
        {
            base.OnGet(flyweight);
            
            // Reset projectile state
            flyweight.gameObject.SetActive(true);
            
            // Play fire sound if available
            if (fireSound != null)
            {
                AudioSource.PlayClipAtPoint(fireSound, flyweight.transform.position, 0.5f);
            }
        }
        
        public override void OnRelease(Flyweight flyweight)
        {
            base.OnRelease(flyweight);
            
            // Ensure the object is disabled
            flyweight.gameObject.SetActive(false);
        }
    }
}
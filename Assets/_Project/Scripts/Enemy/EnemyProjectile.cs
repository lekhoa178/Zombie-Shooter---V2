using LocomotionSystem;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private bool useGravity = false;
    
    [Header("Trajectory Settings")]
    [SerializeField] private bool useParabolicTrajectory = false;
    [SerializeField] private float parabolicHeight = 3f;
    
    // Common properties
    private Vector3 direction;
    private float damage;
    private GameObject owner;
    private Rigidbody rb;
    
    // Parabolic trajectory properties
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float journeyLength;
    private float startTime;
    private bool isInitialized = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null && !useParabolicTrajectory)
        {
            rb.useGravity = useGravity;
        }
    }
    
    /// <summary>
    /// Initialize a straight-line projectile
    /// </summary>
    public void Initialize(Vector3 dir, float dmg, GameObject projectileOwner)
    {
        direction = dir.normalized;
        damage = dmg;
        owner = projectileOwner;
        
        isInitialized = true;
        useParabolicTrajectory = false;
        
        // Start lifetime countdown
        Destroy(gameObject, lifetime);
    }
    
    /// <summary>
    /// Initialize a parabolic trajectory projectile
    /// </summary>
    public void InitializeParabolic(Vector3 start, Vector3 target, float dmg, GameObject projectileOwner, float arcHeight = -1, float projectileSpeed = -1)
    {
        startPosition = start;
        targetPosition = target;
        damage = dmg;
        owner = projectileOwner;
        
        // Use provided values or fallback to serialized values
        parabolicHeight = arcHeight > 0 ? arcHeight : parabolicHeight;
        speed = projectileSpeed > 0 ? projectileSpeed : speed;
        
        useParabolicTrajectory = true;
        isInitialized = true;
        
        // Calculate journey metrics
        journeyLength = Vector3.Distance(start, target);
        startTime = Time.time;
        
        // Disable rigidbody physics since we'll manually position
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Start lifetime countdown with a buffer in case journey calculation is off
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        if (useParabolicTrajectory)
        {
            UpdateParabolicTrajectory();
        }
    }
    
    private void FixedUpdate()
    {
        if (!isInitialized || useParabolicTrajectory) return;
        
        // Only update straight projectiles here
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }
    
    private void UpdateParabolicTrajectory()
    {
        // Calculate how far we've traveled
        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / journeyLength;
        
        // Stop if we've reached or passed the target
        if (fractionOfJourney >= 1.0f)
        {
            // Destroy at end of arc if didn't hit anything
            Destroy(gameObject);
            return;
        }
        
        // Calculate current position using parabolic arc
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
        
        // Add height offset based on parabolic curve
        // This creates a nice arc that peaks in the middle of the journey
        currentPos.y += parabolicHeight * Mathf.Sin(fractionOfJourney * Mathf.PI);
        
        // Update position
        transform.position = currentPos;
        
        // Update rotation to face travel direction
        if (fractionOfJourney < 0.99f)
        {
            // Calculate next position to determine direction
            float nextFraction = Mathf.Min(fractionOfJourney + 0.01f, 1.0f);
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, nextFraction);
            nextPos.y += parabolicHeight * Mathf.Sin(nextFraction * Mathf.PI);
            
            // Calculate and apply rotation
            Vector3 direction = (nextPos - currentPos).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject, collision.contacts[0].point);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject, transform.position);
    }
    
    private void HandleHit(GameObject hitObject, Vector3 hitPoint)
    {
        OnHitObject(hitObject, hitPoint, useParabolicTrajectory ? 
            (hitPoint - transform.position).normalized : direction);
    }
    
    private void OnHitObject(GameObject hitObject, Vector3 hitPoint, Vector3 direction)
    {
        // Ignore owner
        if (hitObject == owner) return;
        
        // Check if hit player
        var player = hitObject.GetComponent<Player>();
        if (player != null)
        {
            // Create hit context
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;
            
            var hitContext = HitContext.Create(
                hitDirection, 
                hitPoint, 
                damage, 
                owner
            );
            
            // Use the new TriggerOnHit method instead of directly calling OnHit
            hitContext.TriggerOnHit(hitObject);
        }
        
        // Spawn hit VFX
        if (hitVFX != null)
        {
            Instantiate(hitVFX, hitPoint, Quaternion.LookRotation(direction));
        }
        
        // Destroy projectile
        Destroy(gameObject);
    }
    
    // Helper methods for configuration
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    public void SetParabolicHeight(float height)
    {
        parabolicHeight = height;
    }
}
using Flyweight;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedEnemyData", menuName = "AI/Enemy Data/Ranged Enemy Data")]
public class RangedEnemyData : EnemyData
{
    [Header("Attack Behavior")]
    [Tooltip("When true, getting hit during attack will interrupt the attack sequence")]
    public bool attackInterruptible = false;
    
    [Header("Projectile Settings")]
    [Tooltip("Flyweight Settings for the projectile - controls projectile behavior (trajectory, speed, parabolic settings, etc.)")]
    public ProjectileSettings projectileSettings;
    [Tooltip("Minimum distance to maintain from target (for ranged enemies)")]
    public float minAttackDistance = 2f;
    
    [Header("Fire Pattern")]
    [Tooltip("Number of consecutive shots")]
    public int consecutiveCount = 1;
    [Tooltip("Delay between consecutive shots")]
    public float consecutiveDelay = 0.2f;
    [Tooltip("Number of simultaneous projectiles per shot")]
    public int simultaneousCount = 1;
    [Tooltip("Spread angle for multiple projectiles")]
    public float spreadAngle = 15f;
    [Tooltip("Whether to use random spread")]
    public bool useRandomSpread = false;
    [Tooltip("Factor for random spread")]
    public float randomSpreadFactor = 0.5f;
    
    [Header("Aim Settings")]
    [Tooltip("Whether to lead the target")]
    public bool leadTarget = true;
    [Tooltip("Factor for leading the target")]
    public float leadFactor = 0.5f;
    [Tooltip("Random variation applied to each bullet's direction (higher = less accurate shots)")]
    public float directionVariance = 0f;
}
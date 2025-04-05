using System.Collections;
using UnityEngine;

namespace Flyweight
{
    public static class ProjectileFlyweightHelper
    {
        // Debug settings
        private static bool s_DrawDebugLines = false;
        private static float s_DebugLineDuration = 1.0f;
        private static Color s_TargetLineColor = Color.red;
        private static Color s_DirectionLineColor = Color.green;
        private static Color s_ParabolicLineColor = Color.yellow;
        
        // Enable or disable debug visualization
        public static void SetDebugVisualization(bool enabled, float duration = 1.0f)
        {
            s_DrawDebugLines = enabled;
            s_DebugLineDuration = duration;
        }
        
        // Spawn a basic projectile with position and rotation
        public static Projectile SpawnProjectile(ProjectileSettings settings, Vector3 position, Quaternion rotation)
        {
            if (settings == null) return null;
            
            Projectile projectile = FlyweightFactory.Spawn(settings) as Projectile;
            if (projectile != null)
            {
                // Apply Y offset if configured
                if (settings.yPositionOffset != 0f)
                {
                    position.y += settings.yPositionOffset;
                }
                
                projectile.transform.position = position;
                projectile.transform.rotation = rotation;
            }
            return projectile;
        }
        
        // Apply random directional variance to a direction within constraints
        private static Vector3 ApplyDirectionVariance(Vector3 direction, Vector3 originalDirection, float variance, float maxAngle = 180f)
        {
            if (Mathf.Abs(variance) < 0.01f) return direction;
            
            // Calculate current angle from original direction
            float currentAngle = Vector3.Angle(originalDirection, direction);
            
            // For variance application, treat the spread cone as a circle
            // Generate a random direction perpendicular to the original direction
            Vector3 perp = Vector3.Cross(direction, Vector3.up).normalized;
            if (perp.magnitude < 0.01f)
            {
                perp = Vector3.Cross(direction, Vector3.forward).normalized;
            }
            
            // Create a random rotation around the reference direction (the original base direction)
            // Random angle around the circle (direction of variance)
            float randomRotAngle = Random.Range(0f, 360f);
            
            // First, get the perpendicular direction based on the random angle
            Quaternion aroundAxis = Quaternion.AngleAxis(randomRotAngle, originalDirection);
            Vector3 randomPerp = aroundAxis * perp;
            
            // Amount of variance (distance from current point)
            float amountOfVariance = Random.Range(0f, variance);
            
            // Then apply the variance
            Quaternion varianceRot = Quaternion.AngleAxis(amountOfVariance, randomPerp);
            
            // Apply the rotation to get our new direction
            Vector3 result = varianceRot * direction;
            
            // Calculate the resulting angle from the original direction
            float resultAngle = Vector3.Angle(originalDirection, result);
            
            // If we've exceeded the max angle, wrap around rather than clamping
            if (resultAngle > maxAngle)
            {
                // Get the plane defined by the original direction
                Vector3 planeNormal = originalDirection;
                
                // Project the result onto the plane perpendicular to the original direction
                Vector3 projectedResult = Vector3.ProjectOnPlane(result, planeNormal).normalized;
                
                // Determine the current angle on the plane
                Vector3 referenceOnPlane = Vector3.ProjectOnPlane(Vector3.up, planeNormal).normalized;
                if (referenceOnPlane.magnitude < 0.01f)
                    referenceOnPlane = Vector3.ProjectOnPlane(Vector3.forward, planeNormal).normalized;
                
                // Get the signed angle on the plane
                float angleOnPlane = Vector3.SignedAngle(referenceOnPlane, projectedResult, planeNormal);
                
                // Normalize the angle to 0-360
                angleOnPlane = (angleOnPlane + 360f) % 360f;
                
                // Calculate what the angle should be after wrapping
                // This keeps the same angle magnitude but reflected across the cone boundary
                float wrappedAngle;
                
                // Calculate how much we've exceeded the boundary
                float excessAngle = resultAngle - maxAngle;
                
                // Wrap to the opposite side by the same excess amount
                wrappedAngle = maxAngle - excessAngle;
                
                // Create a new direction at the wrapped angle
                Quaternion wrapRotation = Quaternion.AngleAxis(angleOnPlane, planeNormal);
                Vector3 dirOnPlane = wrapRotation * referenceOnPlane;
                
                // Apply the wrapped angle from the original direction
                Quaternion coneRotation = Quaternion.AngleAxis(wrappedAngle, Vector3.Cross(planeNormal, dirOnPlane).normalized);
                result = coneRotation * planeNormal;
                
                if (s_DrawDebugLines)
                {
                    // Draw a line showing the wrapping behavior for debugging
                    Debug.DrawRay(Vector3.zero, dirOnPlane * 3, Color.yellow, s_DebugLineDuration);
                    Debug.DrawRay(Vector3.zero, result * 3, Color.magenta, s_DebugLineDuration);
                }
            }
            
            return result.normalized;
        }
        
        // Spawn a straight projectile (single)
        public static Projectile SpawnStraightProjectile(
            ProjectileSettings settings, 
            Vector3 position, 
            Vector3 direction, 
            float damage, 
            GameObject owner, 
            float speed = -1f,
            float directionVariance = 0f,
            Vector3 originalDirection = default)
        {
            if (settings == null) return null;
            
            // If original direction not specified, use the provided direction
            if (originalDirection == default)
            {
                originalDirection = direction;
            }
            
            // Apply direction variance within constraints
            float maxAngle = Vector3.Angle(originalDirection, direction) + settings.maxDirectionVariance;
            Vector3 variedDirection = ApplyDirectionVariance(direction, originalDirection, directionVariance, maxAngle);
            
            if (s_DrawDebugLines)
            {
                // Draw debug ray showing the intended direction
                float debugDistance = 10f; // How far to draw the line
                Debug.DrawRay(position, direction * debugDistance, s_DirectionLineColor, s_DebugLineDuration);
                
                // If direction variance is applied, show the actual direction
                if (directionVariance > 0.01f)
                {
                    Debug.DrawRay(position, variedDirection * debugDistance, Color.red, s_DebugLineDuration);
                }
            }
            
            Projectile projectile = SpawnProjectile(settings, position, Quaternion.LookRotation(variedDirection));
            if (projectile != null)
            {
                projectile.Initialize(variedDirection, damage, owner);
                if (speed > 0) projectile.SetSpeed(speed);
            }
            return projectile;
        }
        
        // Spawn a parabolic projectile
        public static Projectile SpawnParabolicProjectile(
            ProjectileSettings settings, 
            Vector3 startPosition, 
            Vector3 targetPosition, 
            float damage, 
            GameObject owner, 
            float arcHeight = -1f,
            float speed = -1f,
            float directionVariance = 0f,
            Vector3 originalDirection = default)
        {
            if (settings == null) return null;
            
            Vector3 direction = (targetPosition - startPosition).normalized;
            
            // If original direction not specified, use the calculated direction
            if (originalDirection == default)
            {
                originalDirection = direction;
            }
            
            // Apply direction variance to target position with constraints
            if (Mathf.Abs(directionVariance) > 0.01f)
            {
                // Calculate the distance to target
                float distance = Vector3.Distance(startPosition, targetPosition);
                
                // Apply variance to direction
                float maxAngle = Vector3.Angle(originalDirection, direction) + settings.maxDirectionVariance;
                Vector3 variedDirection = ApplyDirectionVariance(
                    direction, 
                    originalDirection, 
                    directionVariance, 
                    maxAngle
                );
                
                // Calculate new target position
                targetPosition = startPosition + variedDirection * distance;
            }
            
            if (s_DrawDebugLines)
            {
                // Draw debug line from start to target
                Debug.DrawLine(startPosition, targetPosition, s_TargetLineColor, s_DebugLineDuration);
                
                // Draw parabolic path approximation
                float actualHeight = arcHeight > 0 ? arcHeight : settings.parabolicHeight;
                DrawParabolicPath(startPosition, targetPosition, actualHeight, s_ParabolicLineColor, s_DebugLineDuration);
            }
            
            // Direction needs to be recalculated after variance
            direction = (targetPosition - startPosition).normalized;
            Projectile projectile = SpawnProjectile(settings, startPosition, Quaternion.LookRotation(direction));
            if (projectile != null)
            {
                float height = arcHeight > 0 ? arcHeight : settings.parabolicHeight;
                
                // Only pass speed if specifically requested to override timeToTarget calculation
                // Otherwise let the projectile use settings.timeToTarget directly
                if (speed > 0)
                {
                    projectile.InitializeParabolic(startPosition, targetPosition, damage, owner, height, speed);
                }
                else
                {
                    // Pass -1 for speed to signal using timeToTarget from settings
                    projectile.InitializeParabolic(startPosition, targetPosition, damage, owner, height, -1f);
                }
            }
            return projectile;
        }
        
        // Draw an approximation of a parabolic path with multiple line segments
        private static void DrawParabolicPath(Vector3 start, Vector3 end, float height, Color color, float duration)
        {
            int segments = 20;
            Vector3 prevPoint = start;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 currentPoint = CalculateParabolicPoint(start, end, height, t);
                Debug.DrawLine(prevPoint, currentPoint, color, duration);
                prevPoint = currentPoint;
            }
        }
        
        // Calculate a point on a parabolic path
        private static Vector3 CalculateParabolicPoint(Vector3 start, Vector3 end, float height, float t)
        {
            // Quadratic Bezier curve
            Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * height;
            Vector3 p0 = Vector3.Lerp(start, mid, t);
            Vector3 p1 = Vector3.Lerp(mid, end, t);
            return Vector3.Lerp(p0, p1, t);
        }
        
        // Optimized batched projectile spawning for patterns
        public static void SpawnProjectilePattern(
            ProjectileSettings settings,
            Vector3 position,
            Vector3 targetPosition,
            float damage,
            GameObject owner,
            int count,
            float spreadAngle,
            bool useRandomSpread = false,
            float randomSpreadFactor = 0.5f,
            float directionVariance = 0f)
        {
            if (settings == null || count <= 0) return;
            
            Vector3 baseDirection = (targetPosition - position).normalized;
            Vector3 upRef = Vector3.up;
            
            // Use settings to determine if we should use parabolic trajectory
            bool useParabolic = settings.useParabolicTrajectory;
            float parabolicHeight = settings.parabolicHeight;
            
            // Handle the single projectile case without spread
            if (count == 1)
            {
                if (useParabolic)
                {
                    SpawnParabolicProjectile(
                        settings,
                        position,
                        targetPosition,
                        damage,
                        owner,
                        parabolicHeight, // Use height from settings
                        settings.speed,
                        directionVariance
                    );
                }
                else
                {
                    SpawnStraightProjectile(
                        settings,
                        position,
                        baseDirection,
                        damage,
                        owner,
                        settings.speed,
                        directionVariance
                    );
                }
                return;
            }
            
            // Calculate total angle and angle increment
            float totalAngle = Mathf.Min(spreadAngle, 360f);
            float angleIncrement = totalAngle / (count - 1);
            float startAngle = -totalAngle / 2f;
            
            for (int i = 0; i < count; i++)
            {
                float currentAngle;
                
                if (useRandomSpread)
                {
                    // Random angle within the spread range, scaled by randomSpreadFactor
                    currentAngle = Random.Range(-totalAngle/2, totalAngle/2) * randomSpreadFactor;
                }
                else
                {
                    // Evenly distributed angles
                    currentAngle = startAngle + (i * angleIncrement);
                }
                
                // Create rotation around up vector
                Quaternion rot = Quaternion.AngleAxis(currentAngle, upRef);
                Vector3 direction = rot * baseDirection;
                
                if (useParabolic)
                {
                    // Calculate the target point by extending the direction
                    float distanceToTarget = Vector3.Distance(position, targetPosition);
                    Vector3 projectileTarget = position + direction * distanceToTarget;
                    
                    SpawnParabolicProjectile(
                        settings,
                        position,
                        projectileTarget,
                        damage,
                        owner,
                        parabolicHeight, // Use height from settings
                        settings.speed,
                        directionVariance,
                        baseDirection // Pass original direction for variance constraints
                    );
                }
                else
                {
                    SpawnStraightProjectile(
                        settings,
                        position,
                        direction,
                        damage,
                        owner,
                        settings.speed,
                        directionVariance,
                        baseDirection // Pass original direction for variance constraints
                    );
                }
            }
        }
        
        // Utility method to determine spread direction with custom up vector
        public static Vector3 GetSpreadDirection(Vector3 baseDirection, float angle, Vector3 upVector)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, upVector);
            return rotation * baseDirection;
        }
        
        // Spawn multiple projectiles in an arc pattern
        public static void SpawnArcProjectiles(
            ProjectileSettings settings,
            Vector3 position,
            Vector3 targetPosition,
            float damage,
            GameObject owner,
            int count,
            float arcAngle,
            Vector3 upVector,
            float directionVariance = 0f)
        {
            if (settings == null || count <= 0) return;
            
            Vector3 baseDirection = (targetPosition - position).normalized;
            
            if (s_DrawDebugLines)
            {
                // Draw a line to the target
                Debug.DrawLine(position, targetPosition, s_TargetLineColor, s_DebugLineDuration);
                
                // Draw arc visualization
                if (count > 1)
                {
                    float angleStep = arcAngle / (count - 1);
                    float startAngle = -arcAngle / 2;
                    
                    for (int i = 0; i < count; i++)
                    {
                        float angle = startAngle + (angleStep * i);
                        Vector3 spreadDirection = GetSpreadDirection(baseDirection, angle, upVector);
                        Debug.DrawRay(position, spreadDirection * 5f, Color.cyan, s_DebugLineDuration);
                    }
                }
            }
            
            if (count == 1)
            {
                SpawnStraightProjectile(settings, position, baseDirection, damage, owner, -1f, directionVariance, baseDirection);
                return;
            }
            
            float realAngleStep = arcAngle / (count - 1);
            float realStartAngle = -arcAngle / 2;
            
            for (int i = 0; i < count; i++)
            {
                float angle = realStartAngle + (realAngleStep * i);
                Vector3 spreadDirection = GetSpreadDirection(baseDirection, angle, upVector);
                
                // Each bullet gets individual direction variance confined to spread angle
                SpawnStraightProjectile(
                    settings, 
                    position, 
                    spreadDirection, 
                    damage, 
                    owner, 
                    -1f, 
                    directionVariance,
                    baseDirection  // Pass original direction for constraint
                );
            }
        }
        
        // Spawn projectiles in a circle pattern
        public static void SpawnCircleProjectiles(
            ProjectileSettings settings,
            Vector3 position,
            float damage,
            GameObject owner,
            int count,
            Vector3 upVector = default,
            float directionVariance = 0f)
        {
            if (settings == null || count <= 0) return;
            
            // Default to world up if not specified
            if (upVector == default) upVector = Vector3.up;
            
            float angleStep = 360f / count;
            
            if (s_DrawDebugLines)
            {
                // Visualize the circle pattern
                for (int i = 0; i < count; i++)
                {
                    float angle = i * angleStep;
                    Quaternion rotation = Quaternion.AngleAxis(angle, upVector);
                    Vector3 forward = Vector3.Cross(upVector, Vector3.right).normalized;
                    if (forward.magnitude < 0.01f) forward = Vector3.Cross(upVector, Vector3.forward).normalized;
                    Vector3 direction = rotation * forward;
                    
                    Debug.DrawRay(position, direction * 5f, Color.magenta, s_DebugLineDuration);
                }
            }
            
            // For circle patterns, use a reference direction perpendicular to up
            Vector3 referenceDirection = Vector3.Cross(upVector, Vector3.right).normalized;
            if (referenceDirection.magnitude < 0.01f) 
                referenceDirection = Vector3.Cross(upVector, Vector3.forward).normalized;
            
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                
                // Create rotation around the up vector
                Quaternion rotation = Quaternion.AngleAxis(angle, upVector);
                
                // Get the forward direction (perpendicular to up)
                Vector3 forward = Vector3.Cross(upVector, Vector3.right).normalized;
                if (forward.magnitude < 0.01f) forward = Vector3.Cross(upVector, Vector3.forward).normalized;
                
                // Apply rotation to forward direction
                Vector3 direction = rotation * forward;
                
                // Each bullet gets individual direction variance
                SpawnStraightProjectile(settings, position, direction, damage, owner, -1f, directionVariance, referenceDirection);
            }
        }
        
        // Spawn projectiles in a spiral pattern over time
        public static IEnumerator SpawnSpiralProjectiles(
            ProjectileSettings settings,
            Transform origin,
            float damage,
            GameObject owner,
            int count,
            float duration,
            float rotationsPerSecond = 1f,
            bool clockwise = true,
            Vector3 upVector = default,
            float directionVariance = 0f)
        {
            if (settings == null || count <= 0 || duration <= 0) yield return null;
            
            // Default to world up if not specified
            if (upVector == default) upVector = Vector3.up;
            
            float delayBetweenShots = duration / count;
            float angleStep = 360f * rotationsPerSecond * delayBetweenShots;
            if (!clockwise) angleStep = -angleStep;
            
            float currentAngle = 0f;
            
            // For spiral patterns, use a reference direction perpendicular to up
            Vector3 referenceDirection = Vector3.Cross(upVector, Vector3.right).normalized;
            if (referenceDirection.magnitude < 0.01f) 
                referenceDirection = Vector3.Cross(upVector, Vector3.forward).normalized;
            
            for (int i = 0; i < count; i++)
            {
                if (origin == null) break;
                
                // Create rotation around the up vector
                Quaternion rotation = Quaternion.AngleAxis(currentAngle, upVector);
                
                // Get the forward direction (perpendicular to up)
                Vector3 forward = Vector3.Cross(upVector, Vector3.right).normalized;
                if (forward.magnitude < 0.01f) forward = Vector3.Cross(upVector, Vector3.forward).normalized;
                
                // Apply rotation to forward direction
                Vector3 direction = rotation * forward;
                
                if (s_DrawDebugLines)
                {
                    Debug.DrawRay(origin.position, direction * 5f, Color.cyan, s_DebugLineDuration);
                }
                
                // Each bullet gets individual direction variance
                SpawnStraightProjectile(settings, origin.position, direction, damage, owner, -1f, directionVariance, referenceDirection);
                
                currentAngle += angleStep;
                yield return new WaitForSeconds(delayBetweenShots);
            }
        }
    }
} 
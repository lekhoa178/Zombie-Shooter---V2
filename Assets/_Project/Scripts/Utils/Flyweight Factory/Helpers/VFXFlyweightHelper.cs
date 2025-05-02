using System.Collections;
using UnityEngine;

namespace Flyweight
{
    public static class VFXFlyweightHelper
    {
        // Helper method with explicit scale parameter
        public static VFXFlyweight SpawnVfxAtPosition(VFXFlyweightSettings settings, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!settings) return null;
            
            var vfxFlyweight = FlyweightFactory.Spawn(settings) as VFXFlyweight;
            if (vfxFlyweight)
            {
                vfxFlyweight.SetPosition(position);
                vfxFlyweight.SetRotation(rotation);
                
                // Only set scale if not using prefab scale (checking if it's not zero)
                if (scale != Vector3.zero)
                {
                    vfxFlyweight.SetScale(scale);
                }
                // Otherwise the prefab's original scale will be preserved
            }
            
            return vfxFlyweight;
        }
        
        // Overload that uses prefab scale by default
        public static VFXFlyweight SpawnVfxAtPosition(VFXFlyweightSettings settings, Vector3 position, Quaternion rotation = default)
        {
            return SpawnVfxAtPosition(settings, position, rotation, Vector3.zero); // Zero tells the method to preserve prefab scale
        }
        
        public static void ReturnVfxToPool(VFXFlyweight vfx)
        {
            if (vfx)
            {
                FlyweightFactory.ReturnToPool(vfx);
            }
        }
        
        // Helper coroutine with explicit scale parameter
        public static IEnumerator SpawnDelayedVfx(VFXFlyweightSettings settings, Vector3 position, Vector3 scale, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnVfxAtPosition(settings, position, Quaternion.identity, scale);
        }
        
        // Overload that uses prefab scale by default
        public static IEnumerator SpawnDelayedVfx(VFXFlyweightSettings settings, Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnVfxAtPosition(settings, position);
        }
        
        public static IEnumerator SpawnDelayedVfx(
            VFXFlyweightSettings settings,
            Vector3 position,
            Vector3 scale,
            float delay,
            Quaternion rotation)
        {
            yield return new WaitForSeconds(delay);
            SpawnVfxAtPosition(settings, position, rotation, scale);
        }
    }
}


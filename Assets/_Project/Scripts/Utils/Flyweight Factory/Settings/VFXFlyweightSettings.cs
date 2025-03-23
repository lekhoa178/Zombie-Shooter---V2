using Flyweight;
using System;
using UnityEngine;

namespace Flyweight
{
    [CreateAssetMenu(fileName = "New VFX Settings", menuName = "Flyweight/VFX Settings")]
    public class VFXFlyweightSettings : FlyweightSettings
    {

        [Tooltip("Duration after which VFX will automatically return to pool")]
        public float autoReturnDuration = 2f;

        public override Flyweight Create()
        {
            if (!prefab)
            {
                Debug.LogError($"VFX prefab is null for {name}");
                return null;
            }

            var instance = Instantiate(prefab);
            var vfxFlyweight = instance.AddComponent<VFXFlyweight>();
            vfxFlyweight.settings = this;

            // Disable by default
            instance.SetActive(false);

            return vfxFlyweight;
        }

        public override void OnGet(Flyweight flyweight)
        {
            if (flyweight is VFXFlyweight vfxFlyweight)
            {
                vfxFlyweight.gameObject.SetActive(true);
                vfxFlyweight.StartReturnTimer();
            }
        }

        public override void OnRelease(Flyweight flyweight)
        {
            if (flyweight is VFXFlyweight vfxFlyweight)
            {
                vfxFlyweight.StopReturnTimer();
                vfxFlyweight.gameObject.SetActive(false);
            }
        }
    }
}
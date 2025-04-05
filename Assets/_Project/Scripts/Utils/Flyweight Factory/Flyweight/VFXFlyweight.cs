using Flyweight;
using System.Collections;
using UnityEngine;

namespace Flyweight
{
    public class VFXFlyweight : Flyweight
    {
        private new VFXFlyweightSettings settings => (VFXFlyweightSettings)base.settings;
        
        private Coroutine _autoReturnCoroutine;

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
        }

        public void StartReturnTimer()
        {
            if (settings is VFXFlyweightSettings vfxSettings)
            {
                StopReturnTimer();
                _autoReturnCoroutine = StartCoroutine(ReturnAfterDelay(vfxSettings.autoReturnDuration));
            }
        }

        public void StopReturnTimer()
        {
            if (_autoReturnCoroutine != null)
            {
                StopCoroutine(_autoReturnCoroutine);
                _autoReturnCoroutine = null;
            }
        }

        private IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            FlyweightFactory.ReturnToPool(this);
        }
    }

}
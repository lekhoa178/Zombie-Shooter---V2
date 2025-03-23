using System.Collections;
using UnityEngine;
using UnityUtils;

namespace Flyweight
{
    public class Projectile : Flyweight
    {
        private new ProjectileSettings settings => (ProjectileSettings) base.settings;

        private void OnEnable()
        {
            StartCoroutine(DespawnAfterDelay(settings.despawnDelay));
        }

        private void FixedUpdate()
        {
            transform.Translate(Vector3.forward * (settings.speed * Time.deltaTime));
        }

        IEnumerator DespawnAfterDelay(float delay)
        {
            yield return Helpers.GetWaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}
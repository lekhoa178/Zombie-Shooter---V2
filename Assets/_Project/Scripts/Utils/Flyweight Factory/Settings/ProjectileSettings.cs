using UnityEngine;

namespace Flyweight
{
    [CreateAssetMenu(menuName = "Flyweight/Projectile Settings")]
    public class ProjectileSettings : FlyweightSettings
    {
        public float despawnDelay = 5f;
        public float speed = 10f;
        public float damage = 10f;

        public override Flyweight Create()
        {
            GameObject go = Instantiate(prefab);
            go.SetActive(false);
            go.name = prefab.name;

            Flyweight flyweight = go.AddComponent<Projectile>();
            flyweight.settings = this;

            return flyweight;
        }
    }
}
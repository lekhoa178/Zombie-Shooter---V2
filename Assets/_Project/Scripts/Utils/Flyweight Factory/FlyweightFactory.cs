using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityUtils;

namespace Flyweight
{
    public class FlyweightFactory : PersistentSingleton<FlyweightFactory>
    {
        [SerializeField] bool collectionCheck = true;
        [SerializeField] int defaultCapacity = 10;
        [SerializeField] int maxPoolSize = 100;

        readonly Dictionary<string, IObjectPool<Flyweight>> pools = new();

        public static Flyweight Spawn(FlyweightSettings s) => instance.GetPoolFor(s).Get();
        public static void ReturnToPool(Flyweight f) => instance.GetPoolFor(f.settings).Release(f);

        private IObjectPool<Flyweight> GetPoolFor(FlyweightSettings settings)
        {
            if (pools.TryGetValue(settings.id, out var pool)) return pool;

            pool = new ObjectPool<Flyweight>(
                settings.Create,
                settings.OnGet,
                settings.OnRelease,
                settings.OnDestroyPoolObject,
                collectionCheck,
                defaultCapacity,
                maxPoolSize
            );


            pools.Add(settings.id, pool);
            return pool;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityUtils;

namespace Flyweight
{
    public class FlyweightFactory : PersistentSingleton<FlyweightFactory>
    {
        [SerializeField] bool m_CollectionCheck = true;
        [SerializeField] int m_DefaultCapacity = 10;
        [SerializeField] int m_MaxPoolSize = 100;

        private readonly Dictionary<string, IObjectPool<Flyweight>> m_Pools = new();
        private readonly HashSet<Flyweight> m_ActiveFlyweights = new();

        #region Public Methods

        public static Flyweight Spawn(FlyweightSettings s)
        {
            var flyweight = instance.GetPoolFor(s).Get();
            instance.m_ActiveFlyweights.Add(flyweight);
            return flyweight;
        }

        public static void ReturnToPool(Flyweight f)
        {
            if (f == null) return;

            try
            {
                instance.m_ActiveFlyweights.Remove(f);
                instance.GetPoolFor(f.settings).Release(f);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogWarning($"Already returned to pool: {f.settings.id} - {f.name}");
            }
        }

        public static void ReturnAllToPool()
        {
            instance.ReturnAllActiveToPool();
        }
        
        public static void ClearAllPools()
        {
            foreach (var pool in instance.m_Pools.Values)
            {
                pool.Clear();
            }
            
            instance.m_Pools.Clear();
        }

        #endregion

        #region Private Methods

        private IObjectPool<Flyweight> GetPoolFor(FlyweightSettings settings)
        {
            if (m_Pools.TryGetValue(settings.id, out var pool)) return pool;

            pool = new ObjectPool<Flyweight>(
                settings.Create,
                settings.OnGet,
                settings.OnRelease,
                settings.OnDestroyPoolObject,
                m_CollectionCheck,
                m_DefaultCapacity,
                m_MaxPoolSize
            );

            m_Pools.Add(settings.id, pool);
            return pool;
        }

        private void ReturnAllActiveToPool()
        {
            // Create a copy to avoid collection modification during iteration
            var activeFlyweightsCopy = new HashSet<Flyweight>(m_ActiveFlyweights);
            
            foreach (var flyweight in activeFlyweightsCopy)
            {
                if (flyweight != null)
                {
                    ReturnToPool(flyweight);
                }
            }
            
            m_ActiveFlyweights.Clear();
        }
        

        #endregion
    }
}
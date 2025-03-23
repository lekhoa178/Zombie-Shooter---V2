using System;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerBoostDashData
    {
        [field: SerializeField] [field: Range(0.1f, 10f)] public float MaxDistance { get; private set; } = 5f;
        [field: SerializeField] public PlayerRotationData RotationData { get; private set; }

        [field: SerializeField] [field: Tooltip("An estimate amount of time using for dash animation")]
        public float DashTrailMaxTime { get; private set; } = 0.06f;
        // [field: SerializeField] public float DashTrailInterval { get; private set; } = 0.008f;
        [field: SerializeField] [field: Range(1, 20)] public int MaxDashTrailCount { get; private set; } = 12;
        [field: SerializeField] public GameObject DashTrailPrefab { get; private set; }
        [field: SerializeField] public ParticleSystem DashTrailParticleSystem { get; private set; }
    }
}
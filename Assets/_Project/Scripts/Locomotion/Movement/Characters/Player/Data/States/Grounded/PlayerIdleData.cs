using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerIdleData
    {
        [field: SerializeField] public List<PlayerCameraRecenteringData> BackwardsCameraRecenteringData { get; private set; }
        [field: SerializeField] [Range(0, 5)] public float DistanceToGround { get; private set; } = 0.1f;
    }
}
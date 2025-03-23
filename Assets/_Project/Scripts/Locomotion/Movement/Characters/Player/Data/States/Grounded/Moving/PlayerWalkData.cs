using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerWalkData
    {
        [field: SerializeField] [field: Range(0f, 1f)] public float SpeedModifier { get; private set; } = 0.225f;
        [field: SerializeField] public List<PlayerCameraRecenteringData> BackwardsCameraRecenteringData { get; private set; }
        [field: SerializeField] public bool HasTurningState { get; private set; } = false;
    }
}
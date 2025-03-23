using System;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerRunData
    {
        [field: SerializeField] [field: Range(1f, 2f)] public float SpeedModifier { get; private set; } = 1f;
        [field: SerializeField] public bool HasTurningState { get; private set; } = false;
    }
}
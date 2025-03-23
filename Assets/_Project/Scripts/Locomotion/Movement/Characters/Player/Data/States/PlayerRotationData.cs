using System;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerRotationData
    {
        [field: SerializeField] public Vector3 TargetRotationReachTime { get; private set; }
        
        public PlayerRotationData(Vector3 targetRotationReachTime)
        {
            TargetRotationReachTime = targetRotationReachTime;
        }
    }
}
using System;
using Unity.Cinemachine;
using UnityEngine;

namespace LocomotionSystem
{
    [Serializable]
    public class PlayerCameraRecenteringUtility
    {
        [field: SerializeField] public CinemachineCamera VirtualCamera { get; private set; }
        [field: SerializeField] public float DefaultHorizontalWaitTime { get; private set; } = 0f;
        [field: SerializeField] public float DefaultHorizontalRecenteringTime { get; private set; } = 4f;

        private CinemachinePanTilt panTilt;

        public void Initialize()
        {
            panTilt = VirtualCamera.GetComponent<CinemachinePanTilt>();
        }

        public void EnableRecentering(float waitTime = -1f, float recenteringTime = -1f, float baseMovementSpeed = 1f, float movementSpeed = 1f)
        {
            panTilt.PanAxis.Recentering.Enabled = true;

            panTilt.PanAxis.CancelRecentering();

            if (Mathf.Approximately(waitTime, -1f))
            {
                waitTime = DefaultHorizontalWaitTime;
            }

            if (Mathf.Approximately(recenteringTime, -1f))
            {
                recenteringTime = DefaultHorizontalRecenteringTime;
            }

            recenteringTime = recenteringTime * baseMovementSpeed / movementSpeed;

            panTilt.PanAxis.Recentering.Wait = waitTime;
            panTilt.PanAxis.Recentering.Time = recenteringTime;
        }

        public void DisableRecentering()
        {
            panTilt.PanAxis.Recentering.Enabled = false;
        }
    }
}
using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private InputActionReference zoomAction;
        
        [SerializeField] [Range(0f, 12f)] private float defaultDistance = 6f;
        [SerializeField] [Range(0f, 12f)] private float minimumDistance = 1f;
        [SerializeField] [Range(0f, 12f)] private float maximumDistance = 6f;

        [SerializeField] [Range(0f, 20f)] private float smoothing = 4f;
        [SerializeField] [Range(0f, 20f)] private float zoomSensitivity = 1f;

        private CinemachinePositionComposer positionComposer;
        
        private float currentTargetDistance;

        private void Awake()
        {
            positionComposer = GetComponent<CinemachinePositionComposer>();
            
            zoomAction.action.started += OnZoom;

            currentTargetDistance = defaultDistance;
        }

        private void OnDestroy()
        {
            zoomAction.action.started -= OnZoom;
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            var  zoomValue = ctx.ReadValue<float>() * zoomSensitivity;
            
            Zoom(ctx.ReadValue<float>());
        }

        private void Zoom(float zoomValue)
        {
            currentTargetDistance = Mathf.Clamp(currentTargetDistance + zoomValue, minimumDistance, maximumDistance);

            var currentDistance = positionComposer.CameraDistance;

            if (Mathf.Approximately(currentDistance, currentTargetDistance))
            {
                return;
            }

            var lerpZoomValue = Mathf.Lerp(currentDistance, currentTargetDistance, smoothing * Time.deltaTime);

            positionComposer.CameraDistance = lerpZoomValue;
        }
    }
}
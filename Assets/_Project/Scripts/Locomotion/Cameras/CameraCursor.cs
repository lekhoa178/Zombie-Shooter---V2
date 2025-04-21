using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LocomotionSystem
{
    public class CameraCursor : MonoBehaviour
    {
        [SerializeField] private InputActionReference cameraToggleInputAction;
        [SerializeField] private bool startHidden;

        [SerializeField] private CinemachineInputAxisController inputAxisController;
        [SerializeField] private bool disableCameraLookOnCursorVisible;
        [SerializeField] private bool disableCameraZoomOnCursorVisible;

        [Tooltip("If you're using Cinemachine 2.8.4 or under, untick this option.\nIf unticked, both Look and Zoom will be disabled.")]
        [SerializeField] private bool fixedCinemachineVersion;
        
        private bool isUICursorVisible;

        private void Awake()
        {
            cameraToggleInputAction.action.started += OnCameraCursorToggled;
            cameraToggleInputAction.action.canceled += OnCameraCursorToggled;

            if (startHidden)
            {
                ToggleCursor();
            }
        }

        private void OnEnable()
        {
            cameraToggleInputAction.action.Enable();
        }

        private void OnDisable()
        {
            cameraToggleInputAction.action.Disable();
        }

        private void OnCameraCursorToggled(InputAction.CallbackContext context)
        {
            ToggleCursor();
        }
        
        public void UIToggleCursor(bool show)
        {
            isUICursorVisible = show;
            
            if (show)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (!fixedCinemachineVersion)
                {
                    inputAxisController.enabled = false;
                }
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                if (!fixedCinemachineVersion)
                {
                    inputAxisController.enabled = true;
                }
            }
        }

        private void ToggleCursor()
        {
            if (isUICursorVisible)
            {
                return;
            }
            
            Cursor.visible = !Cursor.visible;

            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;

                if (!fixedCinemachineVersion)
                {
                    inputAxisController.enabled = true;

                    return;
                }

                //inputAxisController.XYAxis.action.Enable();
                //inputAxisController.ZAxis.action.Enable();

                return;
            }

            Cursor.lockState = CursorLockMode.None;

            if (!fixedCinemachineVersion)
            {
                inputAxisController.enabled = false;

                return;
            }

            //if (disableCameraLookOnCursorVisible)
            //{
            //    inputProvider.XYAxis.action.Disable();
            //}

            //if (disableCameraZoomOnCursorVisible)
            //{
            //    inputProvider.ZAxis.action.Disable();
            //}
        }
    }
}
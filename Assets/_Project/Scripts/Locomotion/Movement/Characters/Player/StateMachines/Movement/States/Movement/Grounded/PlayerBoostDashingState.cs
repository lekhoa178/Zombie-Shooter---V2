using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUtils;

namespace LocomotionSystem
{
    public class PlayerBoostDashingState : PlayerGroundedState
    {
        private PlayerBoostDashData BoostDashData => GroundedData.BoostDashData;
        
        private int consecutiveDashesUsed;
        private bool shouldKeepRotating;
        
        private int dashTrailAmount = 0;
        private int dashTrailIndex = 0;
        
        private Vector3 startPoint = Vector3.zero;   // P0
        private Vector3 endPoint = new Vector3(1, 0, 0);  // P3
        private Vector3 controlPoint1 = new Vector3(0.3f, 0, 0.5f);  // P1
        private Vector3 controlPoint2 = new Vector3(0.7f, 0, -0.5f);  // P2
        
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        
        private readonly List<GameObject> dashTrailObjects;

        private readonly CountdownTimer dashTimer;

        public PlayerBoostDashingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
            dashTrailObjects = new List<GameObject>();
            
            for (var i = 0; i <= BoostDashData.MaxDashTrailCount; i++)
            {
                var go = Object.Instantiate(BoostDashData.DashTrailPrefab);
                go.SetActive(false);
                dashTrailObjects.Add(go);
            }
            
            dashTimer = new CountdownTimer(BoostDashData.DashTrailMaxTime + 0.1f);
        }

        public override void Enter()
        {
            base.Enter();

            // StartAnimation(StateMachine.Player.AnimationData.DashParameterHash);

            StateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;

            StateMachine.ReusableData.RotationData = BoostDashData.RotationData;
                
            var excludeMask = ((1 << enemyLayer) | (1 << obstacleLayer));
            StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.excludeLayers = excludeMask;

            shouldKeepRotating = StateMachine.ReusableData.MovementInput != Vector2.zero;

            Dash();
            
            StateMachine.Player.TargetAvatar.SetActive(false);
            
            StateMachine.Player.StartCoroutine(DashTrailEffect());
            
            dashTimer.Reset();
            dashTimer.Start();

            // Object.Instantiate(StateMachine.Player.ShadowDashEffect, StateMachine.Player.transform.position, StateMachine.Player.transform.rotation);
        }

        public override void Exit()
        {
            base.Exit();

            SetBaseRotationData();

            StateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.excludeLayers = 0;

            StartAnimation(StateMachine.Player.AnimationData.DashParameterHash);
            StopAnimation(StateMachine.Player.AnimationData.DashParameterHash);
            
            StateMachine.Player.TargetAvatar.SetActive(true);
        }


        private IEnumerator DashTrailEffect()
        {
            var vfxTimePoints = new float[] { 0.2f, 0.5f, 0.8f, 0.95f };
            var vfxIndex = 0;

            dashTrailIndex = 0;
            dashTrailAmount = BoostDashData.MaxDashTrailCount;

            float dashTrailInterval = BoostDashData.DashTrailMaxTime / BoostDashData.MaxDashTrailCount;

            while (dashTrailIndex <= dashTrailAmount)
            {
                // For the animator, you can use normalized time if needed.
                float normalizedTime = dashTrailIndex * 2 /
                    (30 * StateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0).length) % 1;
                StateMachine.Player.Animator.Play("Mvm_Boost", 0, normalizedTime);

                // Wait for the computed interval before showing the next trail.
                yield return new WaitForSeconds(dashTrailInterval);

                var obj = dashTrailObjects[dashTrailIndex];
                obj.SetActive(false);
                obj.SetActive(true);

                // Compute normalized time along the dash.
                // Since dashTrailInterval = DashTrailMaxTime / MaxDashTrailCount,
                // t simplifies to: dashTrailIndex / MaxDashTrailCount.
                float t = (float)dashTrailIndex / BoostDashData.MaxDashTrailCount;

                // Optionally apply any easing to t (for instance, uncomment the line below if desired).
                // t = Mathf.SmoothStep(0f, 1f, t * t);

                // For an offset effect – this can be adjusted as needed.
                float sineOffset = Mathf.Sin(t * Mathf.PI * 2) * 2f;
                obj.transform.position = StateMachine.Player.transform.position +
                                         sineOffset * StateMachine.Player.transform.right;
                obj.transform.rotation = Quaternion.LookRotation(EvaluateTangent(t)) * Quaternion.Euler(-90f, 0, 0);

                dashTrailIndex++;

                // Update the mesh for the trail object.
                var mf = obj.GetComponent<MeshFilter>();
                var mesh = new Mesh();
                StateMachine.Player.TargetAvatarRenderer.BakeMesh(mesh);
                mf.mesh = mesh;

                // Optionally instantiate VFX at specific time points.
                if (vfxIndex < vfxTimePoints.Length && t >= vfxTimePoints[vfxIndex])
                {
                    vfxIndex++;
                    var vfxGo = Object.Instantiate(BoostDashData.DashTrailParticleSystem,
                        obj.transform.position + Vector3.up * 0.88f, Quaternion.identity);
                    vfxGo.Play();
                }
                
            }
            
            StateMachine.Player.DashEndFeedback.PlayFeedbacks();
        }

        public override void Update()
        {
            base.Update();

            dashTimer.Tick(Time.deltaTime);
            if (!dashTimer.IsFinished) return;
            
            if (StateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                StateMachine.ChangeState(StateMachine.IdlingState);

                return;
            }

            StateMachine.ChangeState(StateMachine.SprintingState);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            
            if (!shouldKeepRotating)
            {
                return;
            }
            
            RotateTowardsTargetRotation();
        }
        
        private void Dash()
        {
            Vector3 dashDirection = StateMachine.Player.transform.forward;
            dashDirection.y = 0f;

            UpdateTargetRotation(dashDirection, false);
        
            dashDirection = StateMachine.Player.transform.forward;
            
            float dashSpeed = BoostDashData.MaxDistance / BoostDashData.DashTrailMaxTime;
            Vector3 dashForce = dashSpeed * dashDirection;

            StateMachine.Player.Rigidbody.AddForce(dashForce, ForceMode.VelocityChange);
        
            CalculateDashTrajectory(dashForce);
        }
        
        private void CalculateDashTrajectory(Vector3 dashForce)
        {
            startPoint = StateMachine.Player.transform.position;
        
            endPoint = startPoint + BoostDashData.DashTrailMaxTime * dashForce;
            
            controlPoint1 = Vector3.Lerp(startPoint, endPoint, 0.3f) + StateMachine.Player.transform.right * 5f;
            controlPoint2 = Vector3.Lerp(startPoint, endPoint, 0.7f) + StateMachine.Player.transform.right * -5f;
        
            Debug.DrawLine(startPoint, endPoint, Color.red, 3f);
            Debug.DrawLine(startPoint, controlPoint1, Color.green, 3f);
            Debug.DrawLine(controlPoint1, controlPoint2, Color.green, 3f);
            Debug.DrawLine(controlPoint2, endPoint, Color.green, 3f);
        
            for (int i = 0; i < 10; i++)
            {
                float t = i / 10f;
                Debug.DrawLine(Evaluate(t), Evaluate(t + 0.1f), Color.blue, 3f);
            }
        }

        private Vector3 Evaluate(float t)
        {
            t = Mathf.Clamp01(t);

            var u = 1 - t;
            return (u * u * u * startPoint) +
                   (3 * u * u * t * controlPoint1) +
                   (3 * u * t * t * controlPoint2) +
                   (t * t * t * endPoint);
        }

        private Vector3 EvaluateTangent(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1 - t;

            Vector3 tangent = (3 * u * u * (controlPoint1 - startPoint)) +
                              (6 * u * t * (controlPoint2 - controlPoint1)) +
                              (3 * t * t * (endPoint - controlPoint2));

            tangent.y = 0;
            return tangent.normalized;
        }
        
        
        protected override void Move()
        {
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();

            StateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;

        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();

            StateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            base.OnMovementPerformed(context);

            shouldKeepRotating = true;
        }
        
        protected override void OnMovementCanceled(InputAction.CallbackContext context)
        {
        }

        protected override void OnDashStarted(InputAction.CallbackContext context)
        {
        }
    }
}
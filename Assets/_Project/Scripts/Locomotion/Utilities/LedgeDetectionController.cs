using System.Collections;
using System.Collections.Generic;
using Climbing;
using UnityEngine;
using UnityEngine.Serialization;

namespace LocomotionSystem
{
    public class LedgeDetectionController : MonoBehaviour
    {
        
        [Header("Layers")]
        public LayerMask ledgeLayer;
        
        public RaycastHit HitSurface { get; private set; }
        
        public Player Player { get; private set; }
        public PlayerInput Input { get; private set; }
        
        
        private void Awake()
        {
            Player = FindAnyObjectByType<Player>();
            
            Input = Player.GetComponent<PlayerInput>();
        }

        // private void Start()
        // {
        //     // StartCoroutine(RunEveryXms());
        // }

        private IEnumerator RunEveryXms()
        {
            while (true)
            {
                yield return new WaitForSeconds(.1f);
                // yield return null;
                
                DetectLedge();
            }
        }
        
        public bool DetectLedge()
        {
            if (Player.MovementStateMachine.ReusableData.IsGrounded)
            {
                return false;
            }
            
            Vector3 lineDownStart = Player.transform.position + GetPlayerLocalSpaceUpperBounds() + Player.transform.forward * 0.4f - Player.transform.up * 0.3f;
            Vector3 lineDownEnd = lineDownStart + Vector3.down * 0.2f;
            Debug.DrawLine(lineDownStart, lineDownEnd, Color.red);
            
            if (Physics.Linecast(lineDownStart, lineDownEnd, out RaycastHit hit, LayerMask.GetMask("Ground")))
            {
                Vector3 lineFwdStart = new Vector3(Player.transform.position.x, hit.point.y - 0.1f, Player.transform.position.z) - Player.transform.forward * 0.5f;
                Vector3 lineFwdEnd = lineFwdStart + Player.transform.forward * 1f;
                Debug.DrawLine(lineFwdStart, lineFwdEnd, Color.yellow, 3f);
                
                if (Physics.Linecast(lineFwdStart, lineFwdEnd, out RaycastHit fwdHit, LayerMask.GetMask("Ground")))
                {
                    if (fwdHit.collider)
                    {
                        var angle = Vector3.Angle(-fwdHit.normal, Player.transform.forward);
                        Debug.Log($"Angle: {angle}");
                        if (angle is < 35f or > 325f)
                        {
                            HitSurface = fwdHit;
                            Debug.Log("Ledge Detected");
                            return true;
                        }
                        else
                        {
                            Debug.DrawLine(fwdHit.point, fwdHit.point + fwdHit.normal, Color.blue, 5f);
                        }
                    }
                }
            }

            return false;
        }
        
        private Vector3 GetPlayerLocalSpaceUpperBounds()
        {
            return Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderCenterInLocalSpace 
                   + Vector3.up * Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderVerticalExtents.y;
        }

        private Vector3 GetPlayerLocalSpaceCenter()
        {
            return Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderCenterInLocalSpace;
        }
    }
}
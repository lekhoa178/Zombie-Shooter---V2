using System;
using LocomotionSystem;
using UnityEngine;

public class IKFootPlacement : MonoBehaviour
{
    [Range(0, 5)] public float DistanceToGround = 1;
    
    private Animator animator;
    private Player player;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<Player>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (player.MovementStateMachine.CurrentState is PlayerMovingState)
        {
            return;
        }

        if (!animator) return;
        
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        RaycastHit hit;
        if (Physics.Raycast(animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 2f + Vector3.forward * 0.1f, Vector3.down, out hit, DistanceToGround + 2f, LayerMask.GetMask("Ground")))
        {
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + new Vector3(0, DistanceToGround, -0.1f));
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal) * Quaternion.Euler(0, 0, 0));
        }

        if (Physics.Raycast(animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 2f + Vector3.forward * 0.1f, Vector3.down, out hit, DistanceToGround + 2f, LayerMask.GetMask("Ground")))
        {
            animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + new Vector3(0, DistanceToGround, -0.1f));
            animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal) * Quaternion.Euler(0, 30, 0));
        }
    }
}

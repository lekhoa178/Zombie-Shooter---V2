using System;
using LocomotionSystem;
using UnityEngine;

public class PlayerResultPose : MonoBehaviour
{
    private Animator _animator;
    
    private int _idleStateHash;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _idleStateHash = Animator.StringToHash("isIdling");
        if (_animator != null)
        {
            _animator.SetBool(_idleStateHash, false);
        }
    }
}

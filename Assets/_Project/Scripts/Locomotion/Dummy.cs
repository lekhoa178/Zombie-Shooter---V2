using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Dummy : MonoBehaviour, IHittable
{
    [SerializeField] private AnimationClip hitAnimation;
    [SerializeField] private MMF_Player hitFeedback;
    
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void OnHit(Vector3 hitDirection, float damage)
    {
        hitDirection.y = 0;
        
        transform.rotation = Quaternion.LookRotation(-hitDirection);
        
        // transform.DOMove(transform.position + hitDirection.normalized * 0.3f, 0.1f);
        
        animator.Play(hitAnimation.name, -1, 0f);

        if (hitFeedback)
        {
            hitFeedback.PlayFeedbacks();
        }
    }
}

public interface IHittable
{
    public void OnHit(Vector3 hitDirection, float damage);
}

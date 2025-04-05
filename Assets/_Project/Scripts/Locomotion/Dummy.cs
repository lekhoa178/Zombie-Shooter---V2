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

    public event Action<IHittable, float, Vector3> OnDamaged;

    public void OnHit(Vector3 direction, float damage)
    {
        direction.y = 0;

        transform.rotation = Quaternion.LookRotation(-direction);

        // transform.DOMove(transform.position + hitDirection.normalized * 0.3f, 0.1f);

        animator.Play(hitAnimation.name, -1, 0f);

        if (hitFeedback)
        {
            hitFeedback.PlayFeedbacks();
        }
    }

    public void OnHitWithContext(HitContext hitContext)
    {
        throw new NotImplementedException();
    }

    public void OnHitFromDirection(Vector3 hitPosition, Vector3 hitDirection, float damage)
    {
        OnHit(hitDirection, damage);
    }

    public void ReactToHit(HitReactionType reactionType, Vector3 direction)
    {
        throw new NotImplementedException();
    }
}
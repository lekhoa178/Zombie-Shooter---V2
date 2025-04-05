using System;
using System.Collections;
using UnityEngine;
using LocomotionSystem;

/// <summary>
/// Handles knockback and pushback effects for the player
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerKnockbackHandler : MonoBehaviour, IKnockbackable
{
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackResistance = 0.3f;
    [SerializeField] private bool enableVisualFeedback = true;
    
    // Private state tracking
    private bool _isPushedBack = false;
    private Coroutine _activeKnockbackCoroutine;
    
    // Required components
    private Player _player;
    private Animator _animator;
    private Rigidbody _rigidbody;
    private PlayerInput _input;
    
    // Events
    public event Action<GameObject> OnKnockbackStart;
    public event Action<GameObject> OnKnockbackEnd;
    
    // IKnockbackable implementation
    public bool IsPushedBack => _isPushedBack;
    
    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = _player.Animator;
        _rigidbody = _player.Rigidbody;
        _input = _player.Input;
    }
    
    private void OnEnable()
    {
        CombatEventManager.OnKnockbackApplied += HandleKnockbackEvent;
    }
    
    private void OnDisable()
    {
        CombatEventManager.OnKnockbackApplied -= HandleKnockbackEvent;
    }
    
    /// <summary>
    /// Apply knockback to the player
    /// </summary>
    public bool ApplyKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        // Apply knockback resistance
        float adjustedDistance = distance * (1f - knockbackResistance);
        Debug.Log($"Player knocked back: {adjustedDistance} units (original: {distance}), duration: {duration}s");
        
        if (_activeKnockbackCoroutine != null)
        {
            StopCoroutine(_activeKnockbackCoroutine);
        }
        
        _activeKnockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, adjustedDistance, duration, curve));
        return true;
    }
    
    /// <summary>
    /// Apply death knockback to the player - enhanced effect
    /// </summary>
    public bool ApplyDeathKnockback(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        // In this case, we ignore resistance and apply the full knockback for dramatic effect
        Debug.Log($"Player death knockback: {distance} units, duration: {duration}s");
        
        if (_activeKnockbackCoroutine != null)
        {
            StopCoroutine(_activeKnockbackCoroutine);
        }
        
        _activeKnockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, distance, duration, curve));
        return true;
    }
    
    /// <summary>
    /// Handle knockback from the combat event system
    /// </summary>
    public void HandleKnockbackEvent(CombatEventManager.KnockbackEventContext context)
    {
        // Only process if this is the target
        if (context.Target != gameObject) return;
        
        Debug.Log($"Player received knockback event with distance {context.Distance}");
        
        if (context.IsDeath)
        {
            ApplyDeathKnockback(context.Direction, context.Distance, context.Duration, context.Curve);
        }
        else
        {
            ApplyKnockback(context.Direction, context.Distance, context.Duration, context.Curve);
        }
    }
    
    /// <summary>
    /// Coroutine to handle the knockback effect
    /// </summary>
    private IEnumerator KnockbackCoroutine(Vector3 direction, float distance, float duration, AnimationCurve curve = null)
    {
        // Default curve if none provided
        curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // Set flags and notify listeners
        _isPushedBack = true;
        OnKnockbackStart?.Invoke(gameObject);
        
        // Disable player input during knockback
        bool originalInputState = _input.enabled;
        _input.enabled = false;
        
        // Get current player state for restoration afterward
        var currentState = _player.MovementStateMachine.CurrentState;
        
        // Play hit reaction animation
        if (_animator != null)
        {
            _animator.SetTrigger("Damage");
        }
        
        // Pause player movement state machine
        // currentState.OnPauseState();
        
        // Calculate the end position
        var startPosition = transform.position;
        var normalizedDirection = new Vector3(direction.x, 0, direction.z).normalized;
        var targetPosition = startPosition + normalizedDirection * distance;
        
        // Perform a raycast to check if the target position is valid
        RaycastHit hit;
        if (Physics.Raycast(startPosition, normalizedDirection, out hit, distance, _player.LayerData.GroundLayer))
        {
            // Adjust target position to avoid going through walls
            targetPosition = hit.point - normalizedDirection * 0.5f; // Stay slightly away from the wall
        }
        
        // Store the actual push distance (might be less than requested if obstacles are in the way)
        var actualDistance = Vector3.Distance(startPosition, targetPosition);
        
        // If we can't push back significantly, just skip the movement
        if (actualDistance < 0.2f)
        {
            _isPushedBack = false;
            _input.enabled = originalInputState;
            // currentState.OnResumeState();
            OnKnockbackEnd?.Invoke(gameObject);
            yield break;
        }
        
        // Save original rigidbody settings
        var wasKinematic = _rigidbody.isKinematic;
        var wasGravity = _rigidbody.useGravity;
        
        // Set physics settings for smooth knockback
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        
        // Execute the knockback movement
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            var normalizedTime = elapsedTime / duration;
            var curveValue = curve.Evaluate(normalizedTime);
            
            // Move directly without physics
            transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the target position
        transform.position = targetPosition;
        
        // Restore original rigidbody settings
        _rigidbody.isKinematic = wasKinematic;
        _rigidbody.useGravity = wasGravity;
        
        // Reset state
        _isPushedBack = false;
        
        // Re-enable player input
        _input.enabled = originalInputState;
        
        // Resume player movement state machine
        // currentState.OnResumeState();
        
        // Broadcast that knockback has ended
        CombatEventManager.TriggerKnockbackEnded(null, gameObject);
        
        // Notify listeners
        OnKnockbackEnd?.Invoke(gameObject);
    }
}
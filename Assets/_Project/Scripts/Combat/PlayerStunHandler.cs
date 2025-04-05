using System;
using System.Collections;
using UnityEngine;
using LocomotionSystem;

/// <summary>
/// Handles stunning and stun recovery for the player
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerStunHandler : MonoBehaviour, IStunnable
{
    [Header("Stun Settings")]
    [SerializeField] private float stunResistance = 0.2f;
    [SerializeField] private float recoveryDuration = 3f;
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private ParticleSystem stunVFX;
    
    // Private state tracking
    private bool _isStunned = false;
    private bool _isInStunRecovery = false;
    private float _stunRecoveryEndTime = 0f;
    private Coroutine _activeStunCoroutine;
    
    // Required components
    private Player _player;
    private Animator _animator;
    private Rigidbody _rigidbody;
    private PlayerInput _input;
    
    // Events
    public event Action<GameObject> OnStunBegin;
    public event Action<GameObject> OnStunEnd;
    
    // IStunnable implementation
    public bool IsStunned => _isStunned;
    public bool IsInStunRecovery => _isInStunRecovery;
    public bool CanBeStunned => !_isStunned && !_isInStunRecovery;
    
    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = _player.Animator;
        _rigidbody = _player.Rigidbody;
        _input = _player.Input;
    }
    
    private void OnEnable()
    {
        CombatEventManager.OnStunApplied += HandleStunEvent;
    }
    
    private void OnDisable()
    {
        CombatEventManager.OnStunApplied -= HandleStunEvent;
    }
    
    /// <summary>
    /// Apply stun effect to the player
    /// </summary>
    public bool ApplyStun(float duration)
    {
        // Can't stun if in recovery or already stunned
        if (_isInStunRecovery || _isStunned)
        {
            Debug.Log($"Player stun resisted: already stunned={_isStunned}, in recovery={_isInStunRecovery}");
            return false;
        }
        
        // Apply stun resistance
        float adjustedDuration = duration * (1f - stunResistance);
        Debug.Log($"Player stunned for {adjustedDuration}s (original: {duration}s)");
        
        if (_activeStunCoroutine != null)
        {
            StopCoroutine(_activeStunCoroutine);
        }
        
        _activeStunCoroutine = StartCoroutine(StunCoroutine(adjustedDuration));
        return true;
    }
    
    /// <summary>
    /// Handle stun from the combat event system
    /// </summary>
    public void HandleStunEvent(CombatEventManager.StunEventContext context)
    {
        // Only process if this is the target
        if (context.Target != gameObject) return;
        
        Debug.Log($"Player received stun event with duration {context.Duration}s");
        
        if (ApplyStun(context.Duration))
        {
            // Stun was applied successfully
        }
        else
        {
            // Stun was resisted
            CombatEventManager.TriggerStunResisted(context.Source, gameObject, context.Duration, context.Damage);
        }
    }
    
    /// <summary>
    /// Coroutine to handle the stun duration and effects
    /// </summary>
    private IEnumerator StunCoroutine(float duration)
    {
        // Set flags and notify listeners
        _isStunned = true;
        OnStunBegin?.Invoke(gameObject);
        
        // Play stun animation and effects
        if (_animator != null)
        {
            _animator.SetBool("Stunned", true);
        }
        
        // Play stun VFX
        if (enableVisualFeedback && stunVFX != null)
        {
            stunVFX.Play();
        }
        
        // Disable player input
        if (_input != null)
        {
            _input.enabled = false;
        }
        
        // Pause player movement state machine
        // _player.MovementStateMachine.CurrentState.OnPauseState();
        
        // Wait for the stun duration
        yield return new WaitForSeconds(duration);
        
        // End the stun state
        EndStun();
        
        // Start recovery phase
        StartCoroutine(RecoveryCoroutine(recoveryDuration));
        
        // Notify the event system
        CombatEventManager.TriggerStunEnded(null, gameObject);
    }
    
    /// <summary>
    /// End the stunned state
    /// </summary>
    public void EndStun()
    {
        if (!_isStunned) return;
        
        // Reset flags and notify listeners
        _isStunned = false;
        OnStunEnd?.Invoke(gameObject);
        
        // Stop stun animation and effects
        if (_animator != null)
        {
            _animator.SetBool("Stunned", false);
        }
        
        // Stop stun VFX
        if (enableVisualFeedback && stunVFX != null)
        {
            stunVFX.Stop();
        }
        
        // Re-enable player input
        if (_input != null)
        {
            _input.enabled = true;
        }
        
        // Resume player movement state machine
        // _player.MovementStateMachine.CurrentState.OnResumeState();
    }
    
    /// <summary>
    /// Coroutine to handle the recovery period after stun
    /// </summary>
    private IEnumerator RecoveryCoroutine(float duration)
    {
        _isInStunRecovery = true;
        _stunRecoveryEndTime = Time.time + duration;
        
        CombatEventManager.TriggerRecoveryStarted(null, gameObject, "Stun", duration);
        
        yield return new WaitForSeconds(duration);
        
        _isInStunRecovery = false;
        
        CombatEventManager.TriggerRecoveryEnded(null, gameObject, "Stun");
    }
}
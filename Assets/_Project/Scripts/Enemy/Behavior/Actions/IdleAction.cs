using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Idle", story: "[Agent] Idles for random duration", category: "Action", id: "064f06357b5d105f46f711bf4e570732")]
public partial class IdleAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> MinIdleTime;
    [SerializeReference] public BlackboardVariable<float> MaxIdleTime;
    
    private float idleDuration;
    private float startTime;
    private bool isInitialized;

    protected override Status OnStart()
    {
        if (!Agent.Value)
        {
            Debug.LogWarning("IdleAction: Agent is null");
            return Status.Failure;
        }

        if (MaxIdleTime.Value <= 0)
        {
            Debug.LogWarning("IdleAction: MaxIdleTime must be greater than 0");
            return Status.Failure;
        }

        // Generate random idle duration when starting
        idleDuration = UnityEngine.Random.Range(MinIdleTime.Value, MaxIdleTime.Value);
        startTime = Time.time;
        isInitialized = true;

        // Stop any movement if the agent has a movement component
        var movement = Agent.Value.GetComponent<IAIMovement>();
        if (movement != null)
        {
            movement.StopMovement();
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!isInitialized || !Agent.Value)
        {
            return Status.Failure;
        }

        // Check if we've been idling long enough
        float elapsedTime = Time.time - startTime;
        if (elapsedTime >= idleDuration)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        isInitialized = false;
    }
}


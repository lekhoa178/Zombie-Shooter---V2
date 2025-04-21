using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PowerTree
{
    #region Evasion Branch Nodes

    /// <summary>
    /// 1. Ghost Steps: Increase movement speed and temporary boost when dodging
    /// </summary>
    [CreateAssetMenu(fileName = "GhostSteps", menuName = "PowerTree/Nodes/Evasion/GhostSteps")]
    public class GhostStepsNode : PowerNode
    {
        [SerializeField] private float[] moveSpeedBoost = new float[] { 0.1f, 0.15f, 0.2f }; // 10%, 15%, 20% permanent
        [SerializeField] private float[] dodgeBoost = new float[] { 0.1f, 0.15f, 0.2f }; // +10%, +15%, +20% temporary
        [SerializeField] private float dodgeBoostDuration = 2f; // 2 seconds

        private float currentMoveBoost = 0f;

        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Increase movement speed by {moveSpeedBoost[0]} and gain an additional {dodgeBoost[0]} for {dodgeBoostDuration} seconds after dodging",
                "Movement speed increased to {moveSpeedBoost[1]} and dodge boost to {dodgeBoost[1]}",
                "Movement speed increased to {moveSpeedBoost[2]} and dodge boost to {dodgeBoost[2]}"
            };
        }

        private UnityAction _onSuccessfulDodgeAction;

        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > moveSpeedBoost.Length) return;

            // Remove previous effect if any
            if (currentMoveBoost > 0)
            {
                player.ModifyMoveSpeedMultiplier(-currentMoveBoost);
            }

            // Apply permanent movement speed boost
            currentMoveBoost = moveSpeedBoost[level - 1];
            player.ModifyMoveSpeedMultiplier(currentMoveBoost);

            // Define the action
            _onSuccessfulDodgeAction = () => OnSuccessfulDodge(player, level);

            // Subscribe to dodge event
            DodgeSystem.OnSuccessfulDodge.AddListener(_onSuccessfulDodgeAction);
        }

        public override void RemoveEffect(PlayerStats player)
        {
            // Remove permanent boost
            player.ModifyMoveSpeedMultiplier(-currentMoveBoost);

            // Unsubscribe from dodge event
            if (_onSuccessfulDodgeAction != null)
            {
                DodgeSystem.OnSuccessfulDodge.RemoveListener(_onSuccessfulDodgeAction);
            }
            
            ResetState();
        }

        public override void ResetState()
        {
            currentMoveBoost = 0f;
        }

        private void OnSuccessfulDodge(PlayerStats player, int level)
        {
            // Apply temporary movement speed boost after dodge
            float tempBoost = dodgeBoost[level - 1];
            player.ModifyMoveSpeedMultiplier(tempBoost, dodgeBoostDuration);
        }
    }

    /// <summary>
    /// 2. Wind Evasion: Chance to dodge attacks
    /// </summary>
    [CreateAssetMenu(fileName = "WindEvasion", menuName = "PowerTree/Nodes/Evasion/WindEvasion")]
    public class WindEvasionNode : PowerNode
    {
        [SerializeField] private float[] dodgeChance = new float[] { 0.05f, 0.1f, 0.15f }; // 5%, 10%, 15%

        private float currentDodgeBoost = 0f;

        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "Gain {dodgeChance[0]} chance to dodge incoming attacks",
                "Dodge chance increased to {dodgeChance[1]}",
                "Dodge chance increased to {dodgeChance[2]}"
            };
        }

        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > dodgeChance.Length) return;

            // Remove previous effect if any
            if (currentDodgeBoost > 0)
            {
                player.ModifyDodgeChance(-currentDodgeBoost);
            }

            // Apply new dodge chance
            currentDodgeBoost = dodgeChance[level - 1];
            player.ModifyDodgeChance(currentDodgeBoost);
        }

        public override void RemoveEffect(PlayerStats player)
        {
            // Remove dodge chance boost
            player.ModifyDodgeChance(-currentDodgeBoost);
            
            ResetState();
        }

        public override void ResetState()
        {
            currentDodgeBoost = 0f;
        }
    }

    /// <summary>
    /// 3. Agility: Movement speed and dodge when at low health
    /// </summary>
    [CreateAssetMenu(fileName = "Agility", menuName = "PowerTree/Nodes/Evasion/Agility")]
    public class AgilityNode : PowerNode
    {
        [SerializeField] private float[] moveSpeedBoost = new float[] { 0.1f, 0.15f, 0.2f }; // 10%, 15%, 20%
        [SerializeField] private float[] dodgeChanceBoost = new float[] { 0f, 0f, 0.05f }; // 0%, 0%, 5%
        [SerializeField] private float lowHealthThreshold = 1f; // 1 heart or less

        private float currentMoveBoost = 0f;
        private float currentDodgeBoost = 0f;
        private bool effectsApplied = false;

        private void OnValidate()
        {
            // Auto-populate descriptions with field values
            descriptions = new string[]
            {
                "While at {lowHealthThreshold} hearts or below, gain {moveSpeedBoost[0]} movement speed",
                "Movement speed boost increased to {moveSpeedBoost[1]}",
                "Movement speed boost increased to {moveSpeedBoost[2]} and gain {dodgeChanceBoost[2]} dodge chance"
            };
        }

        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > moveSpeedBoost.Length) return;

            currentMoveBoost = moveSpeedBoost[level - 1];
            currentDodgeBoost = dodgeChanceBoost[level - 1];

            // Save node-specific configuration
            player.SetNodeProperty(nodeID, "lowHealthThreshold", lowHealthThreshold);

            // Check and apply initial state
            CheckAndApplyEffectsIfNeeded(player);

            // Subscribe to heart changes
            player.OnRedHeartLost.AddListener((_) => CheckAndApplyEffectsIfNeeded(player));
            player.OnRedHeartGained.AddListener((_) => CheckAndApplyEffectsIfNeeded(player));
        }

        public override void RemoveEffect(PlayerStats player)
        {
            // Unsubscribe from events
            player.OnRedHeartLost.RemoveListener((_) => CheckAndApplyEffectsIfNeeded(player));
            player.OnRedHeartGained.RemoveListener((_) => CheckAndApplyEffectsIfNeeded(player));

            // Remove effects if applied
            if (effectsApplied)
            {
                player.ModifyMoveSpeedMultiplier(-currentMoveBoost);
                if (currentDodgeBoost > 0)
                {
                    player.ModifyDodgeChance(-currentDodgeBoost);
                }

                effectsApplied = false;
            }
            
            ResetState();
        }

        public override void ResetState()
        {
            currentMoveBoost = 0f;
            currentDodgeBoost = 0f;
            effectsApplied = false;
        }


        private void CheckAndApplyEffectsIfNeeded(PlayerStats player)
        {
            // Get the configured threshold for this node
            float threshold = player.GetNodeProperty(nodeID, "lowHealthThreshold", lowHealthThreshold);
            bool isLowHealth = player.IsLowHealth(threshold);

            if (isLowHealth && !effectsApplied)
            {
                // Apply bonuses
                player.ModifyMoveSpeedMultiplier(currentMoveBoost);
                if (currentDodgeBoost > 0)
                {
                    player.ModifyDodgeChance(currentDodgeBoost);
                }

                effectsApplied = true;
            }
            else if (!isLowHealth && effectsApplied)
            {
                // Remove bonuses
                player.ModifyMoveSpeedMultiplier(-currentMoveBoost);
                if (currentDodgeBoost > 0)
                {
                    player.ModifyDodgeChance(-currentDodgeBoost);
                }

                effectsApplied = false;
            }
        }
    }

    #endregion
}

// Helper stubs to make the code compile - these would be implemented in your game
public static class DodgeSystem
{
    public static UnityEngine.Events.UnityEvent OnSuccessfulDodge = new UnityEngine.Events.UnityEvent();
} 
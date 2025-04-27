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
        [SerializeField] private float dodgeEffectDuration = 2f; // 2 seconds

        private float _currentMoveBoost;

        private void OnValidate()
        {
            descriptions = new[]
            {
                "Increase movement speed by {moveSpeedBoost[0]} and gain an additional {dodgeBoost[0]} for {dodgeEffectDuration} seconds after dodging",
                "Movement speed increased to {moveSpeedBoost[1]} and dodge boost to {dodgeBoost[1]}",
                "Movement speed increased to {moveSpeedBoost[2]} and dodge boost to {dodgeBoost[2]}"
            };
        }

        private UnityAction _onSuccessfulDodgeAction;

        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > moveSpeedBoost.Length) return;

            // Remove previous effect if any
            if (_currentMoveBoost > 0)
            {
                player.ModifyMoveSpeedMultiplier(-_currentMoveBoost);
            }

            // Apply permanent movement speed boost
            _currentMoveBoost = moveSpeedBoost[level - 1];
            player.ModifyMoveSpeedMultiplier(_currentMoveBoost);

            // Define the action
            _onSuccessfulDodgeAction = () => OnSuccessfulDodge(player, level);

            // Subscribe to dodge event
            player.OnDodgeSuccessful.AddListener(_onSuccessfulDodgeAction);
        }

        public override void RemoveEffect(PlayerStats player)
        {
            // Remove permanent boost
            player.ModifyMoveSpeedMultiplier(-_currentMoveBoost);

            // Unsubscribe from dodge event
            if (_onSuccessfulDodgeAction != null)
            {
                player.OnDodgeSuccessful.RemoveListener(_onSuccessfulDodgeAction);
            }
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentMoveBoost = 0f;
        }

        private void OnSuccessfulDodge(PlayerStats player, int level)
        {
            // Apply temporary movement speed boost after dodge
            float tempBoost = dodgeBoost[level - 1];
            player.ModifyMoveSpeedMultiplier(tempBoost, dodgeEffectDuration);
        }
    }

    /// <summary>
    /// 2. Wind Evasion: Chance to dodge attacks
    /// </summary>
    [CreateAssetMenu(fileName = "WindEvasion", menuName = "PowerTree/Nodes/Evasion/WindEvasion")]
    public class WindEvasionNode : PowerNode
    {
        [SerializeField] private float[] dodgeChance = new[] { 0.05f, 0.1f, 0.15f }; // 5%, 10%, 15%

        private float _currentDodgeBoost;

        private void OnValidate()
        {
            descriptions = new[]
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
            if (_currentDodgeBoost > 0)
            {
                player.ModifyDodgeChance(-_currentDodgeBoost);
            }

            // Apply new dodge chance
            _currentDodgeBoost = dodgeChance[level - 1];
            player.ModifyDodgeChance(_currentDodgeBoost);
        }

        public override void RemoveEffect(PlayerStats player)
        {
            // Remove dodge chance boost
            player.ModifyDodgeChance(-_currentDodgeBoost);
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentDodgeBoost = 0f;
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

        private float _currentMoveBoost;
        private float _currentDodgeBoost;
        private bool _effectsApplied;

        private void OnValidate()
        {
            descriptions = new[]
            {
                "While at {lowHealthThreshold} hearts or below, gain {moveSpeedBoost[0]} movement speed",
                "Movement speed boost increased to {moveSpeedBoost[1]}",
                "Movement speed boost increased to {moveSpeedBoost[2]} and gain {dodgeChanceBoost[2]} dodge chance"
            };
        }

        public override void ApplyEffect(PlayerStats player, int level)
        {
            if (level <= 0 || level > moveSpeedBoost.Length) return;

            _currentMoveBoost = moveSpeedBoost[level - 1];
            _currentDodgeBoost = dodgeChanceBoost[level - 1];

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
            if (_effectsApplied)
            {
                player.ModifyMoveSpeedMultiplier(-_currentMoveBoost);
                if (_currentDodgeBoost > 0)
                {
                    player.ModifyDodgeChance(-_currentDodgeBoost);
                }

                _effectsApplied = false;
            }
            
            ResetState();
        }

        public override void ResetState()
        {
            _currentMoveBoost = 0f;
            _currentDodgeBoost = 0f;
            _effectsApplied = false;
        }


        private void CheckAndApplyEffectsIfNeeded(PlayerStats player)
        {
            // Get the configured threshold for this node
            float threshold = player.GetNodeProperty(nodeID, "lowHealthThreshold", lowHealthThreshold);
            bool isLowHealth = player.IsLowHealth(threshold);

            if (isLowHealth && !_effectsApplied)
            {
                // Apply bonuses
                player.ModifyMoveSpeedMultiplier(_currentMoveBoost);
                if (_currentDodgeBoost > 0)
                {
                    player.ModifyDodgeChance(_currentDodgeBoost);
                }

                _effectsApplied = true;
            }
            else if (!isLowHealth && _effectsApplied)
            {
                // Remove bonuses
                player.ModifyMoveSpeedMultiplier(-_currentMoveBoost);
                if (_currentDodgeBoost > 0)
                {
                    player.ModifyDodgeChance(-_currentDodgeBoost);
                }

                _effectsApplied = false;
            }
        }
    }

    #endregion
}
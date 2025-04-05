using UnityEngine;

[CreateAssetMenu(fileName = "MeleeEnemyData", menuName = "AI/Melee Enemy Data")]
public class MeleeEnemyData : EnemyData
{
    [Header("Melee Attack Settings")]
    [Tooltip("Should the enemy use a telegraph before attacking")]
    public bool useTelegraph = true;
    [Tooltip("Whether this enemy can combo attacks")]
    public bool canComboAttacks = false;
    [Tooltip("Maximum number of combo hits")]
    public int maxComboCount = 3;
    [Tooltip("Delay between combo attacks")]
    public float comboDelay = 0.5f;
    [Tooltip("Chance to perform a combo attack")]
    public float comboChance = 0.7f;
}
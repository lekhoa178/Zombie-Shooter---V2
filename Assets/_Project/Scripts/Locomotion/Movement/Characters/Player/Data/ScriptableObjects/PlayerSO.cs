using Flyweight;
using UnityEngine;

namespace LocomotionSystem
{
    [CreateAssetMenu(fileName = "Player", menuName = "Genshin Impact Movement System/Characters/Player")]
    public class PlayerSO : ScriptableObject
    {
        [field: SerializeField] public PlayerGroundedData GroundedData { get; private set; }
        [field: SerializeField] public PlayerAirborneData AirborneData { get; private set; }

        [field: Header("Debug Combat Configuration")]
        [Tooltip("If assigned, this will override the WeaponManager's ComboConfiguration (for debugging purposes)")]
        [field: SerializeField] public ComboConfiguration DebugComboConfigOverride { get; private set; }

        [field: Header("Default VFX Settings")]
        [field: SerializeField] public VFXFlyweightSettings DefaultHitVfxSettings { get; private set; }

        [field: SerializeField] public GameObject LastHitVfx { get; private set; }
    }
}
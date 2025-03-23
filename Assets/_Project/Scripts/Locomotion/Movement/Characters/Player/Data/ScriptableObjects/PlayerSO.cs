using Flyweight;
using UnityEngine;

namespace LocomotionSystem
{
    [CreateAssetMenu(fileName = "Player", menuName = "Genshin Impact Movement System/Characters/Player")]
    public class PlayerSO : ScriptableObject
    {
        [field: SerializeField] public PlayerGroundedData GroundedData { get; private set; }
        [field: SerializeField] public PlayerAirborneData AirborneData { get; private set; }

        [field: Header("Combat Configuration")]
        [field: SerializeField]
        public ComboConfiguration ComboConfig { get; private set; }
        
        // Add this to your existing PlayerSO class:
        [field: Header("Default VFX Settings")]
        [field: SerializeField] public VFXFlyweightSettings DefaultHitVfxSettings { get; private set; }

        [field: SerializeField] public GameObject LastHitVfx { get; private set; }
    }
}
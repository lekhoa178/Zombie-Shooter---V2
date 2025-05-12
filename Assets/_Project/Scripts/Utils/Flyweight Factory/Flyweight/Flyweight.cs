using UnityEngine;

namespace Flyweight
{
    public class Flyweight : MonoBehaviour
    {
        public FlyweightSettings settings; // Intrinsic state
        
        void OnDestroy()
        {
            Debug.LogError("Flyweight destroyed: " + name);
        }
    }
}
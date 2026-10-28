using UnityEngine;

namespace SpiritPrototype
{
    [DisallowMultipleComponent]
    public sealed class SpiritEncounter : MonoBehaviour
    {
        public string species;
        public Sprite portrait;
        [Min(1)] public int maxHealth = 18;
    }
}

using UnityEngine;

namespace SpiritPrototype
{
    [CreateAssetMenu(menuName = "Espiritus/Ficha de espiritu", fileName = "NuevoEspiritu")]
    public sealed class SpiritDefinition : ScriptableObject
    {
        [Header("Identidad - no cambiar un ID que ya se uso en guardados")]
        public string id;
        public string displayName;
        public Sprite sprite;
        public Color tint = Color.white;
        public Sprite[] idleFrames = new Sprite[0];
        [Min(.1f)] public float idleFramesPerSecond = 5;
        public Sprite FrameAt(float time)
        {
            if (idleFrames == null || idleFrames.Length == 0) return sprite;
            int index = (int)((System.Math.Max(0, time) * idleFramesPerSecond) % idleFrames.Length);
            return idleFrames[index] != null ? idleFrames[index] : sprite;
        }
        public string rarity = "Por definir";
        [Min(.1f)] public float mapSize = .9f;
        [Header("Aliado")]
        public bool canBeAlly = true;
        [Min(1)] public int allyMaxHealth = 20;
        [Min(1)] public int allyDamage = 5;
        [Min(0)] public int attackRupture = 2;
        [Min(0)] public int containment = 5;
        public bool usesBrasa;
        [Header("Enemigo")]
        public bool spawnInDungeon = true;
        public bool capturable = true;
        [Min(1)] public int enemyMaxHealth = 18;
        [Min(1)] public int enemyAttack = 3;
        [Min(1)] public int enemyStability = 10;
    }
}

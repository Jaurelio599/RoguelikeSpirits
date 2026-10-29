using UnityEngine;

namespace SpiritPrototype
{
    [DisallowMultipleComponent]
    public sealed class SpiritEncounter : MonoBehaviour
    {
        public SpiritDefinition definition;
        public string species;
        public Sprite portrait;
        [Min(1)] public int maxHealth = 18;
        [Min(1)] public int attackDamage = 3;
        [Min(1)] public int maxStability = 10;
        public bool capturable = true;
        public void ApplyDefinition(SpiritDefinition value)
        {
            definition = value; species = value.id;
            if (value.sprite != null) portrait = value.sprite;
            maxHealth = value.enemyMaxHealth; attackDamage = value.enemyAttack;
            maxStability = value.enemyStability; capturable = value.capturable;
            var visual = GetComponent<SpriteRenderer>();
            if (visual != null && value.sprite != null)
            {
                visual.sprite = value.sprite; visual.color = value.tint;
                var size = value.sprite.bounds.size;
                transform.localScale = Vector3.one * (value.mapSize / Mathf.Max(.001f, Mathf.Max(size.x, size.y)));
            }
        }
    }
}

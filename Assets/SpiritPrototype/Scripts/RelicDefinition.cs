using System;
using UnityEngine;

namespace SpiritPrototype
{
    public enum RelicEffectKind { AddAttack, MultiplyAttack, AmplifyNextAddition, ReduceIncomingDamage, AddContainment }
    public enum RelicCondition { Always, BrasaOnly, EnemyBelowHalf, EnemyFullHealth }

    [Serializable]
    public struct PrototypeRelicRule
    {
        public int id, targetId;
        public string name;
        public RelicEffectKind effect;
        public RelicCondition condition;
        public float amount;
        public bool Matches(bool brasa, int health, int maxHealth)
        {
            switch (condition)
            {
                case RelicCondition.BrasaOnly: return brasa;
                case RelicCondition.EnemyBelowHalf: return (long)health * 2 < maxHealth;
                case RelicCondition.EnemyFullHealth: return health == maxHealth;
                default: return true;
            }
        }
    }

    [CreateAssetMenu(menuName = "Espiritus/Ficha de reliquia", fileName = "NuevaReliquia")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [Header("Identidad - conservar los numeros de IDs existentes")]
        [Min(0)] public int id;
        public string displayName;
        public Sprite icon;
        public string rarity = "Por definir";
        public bool inChests = true;
        [Header("Efecto reutilizable")]
        public RelicEffectKind effect = RelicEffectKind.AddAttack;
        public RelicCondition condition = RelicCondition.Always;
        [Min(.01f)] public float amount = 2;
        [Tooltip("Solo para AmplifyNextAddition: ID de la suma situada inmediatamente a la derecha.")]
        public int targetRelicId;
        [TextArea] public string flavorText;

        public PrototypeRelicRule Rule => new PrototypeRelicRule {id = id, targetId = targetRelicId, name = displayName,
            effect = effect, condition = condition, amount = amount};
        public string RuleText
        {
            get
            {
                string value = amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                string text;
                switch (effect)
                {
                    case RelicEffectKind.MultiplyAttack: text = "Multiplica el ataque x" + value; break;
                    case RelicEffectKind.AmplifyNextAddition: text = "Multiplica x" + value + " la suma de la reliquia ID " + targetRelicId + " inmediatamente a su derecha"; break;
                    case RelicEffectKind.ReduceIncomingDamage: text = "Reduce en " + value + " el dano recibido; minimo 1"; break;
                    case RelicEffectKind.AddContainment: text = "Contener elimina " + value + " puntos adicionales de estabilidad"; break;
                    default: text = "Suma " + value + " al ataque"; break;
                }
                switch (condition)
                {
                    case RelicCondition.BrasaOnly: text += " (solo Brasa)"; break;
                    case RelicCondition.EnemyBelowHalf: text += " (enemigo por debajo de media vida)"; break;
                    case RelicCondition.EnemyFullHealth: text += " (enemigo con toda su vida)"; break;
                }
                return text + ".";
            }
        }
    }
}

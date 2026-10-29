using System;
using System.Collections.Generic;
using System.Text;

namespace SpiritPrototype
{
    // Retain the original numeric aliases for compatibility; new IDs come from assets.
    public enum PrototypeRelic { Carbon = 0, Lens = 1, Prism = 2, Edge = 3, Shell = 4, Knot = 5, Dawn = 6 }
    public sealed class PrototypeRelicLine
    {
        private readonly List<PrototypeRelic> slots;
        private Dictionary<int, PrototypeRelicRule> frozen;
        public int Count => slots.Count;
        public PrototypeRelic this[int index] => slots[index];
        public PrototypeRelicLine() : this(new[] {PrototypeRelic.Carbon, PrototypeRelic.Lens, PrototypeRelic.Prism}) { }
        public PrototypeRelicLine(IEnumerable<PrototypeRelic> values)
        {
            slots = new List<PrototypeRelic>(values);
            if (slots.Count > 5 || new HashSet<PrototypeRelic>(slots).Count != slots.Count) throw new ArgumentException("La fila admite cinco reliquias diferentes.");
            foreach (var id in slots) if (!PrototypeContent.KnownRelic((int)id)) throw new ArgumentException("Reliquia desconocida: " + (int)id);
        }
        public PrototypeRelicRule Rule(int index) => frozen != null ? frozen[(int)slots[index]] : PrototypeContent.Relic((int)slots[index]).Rule;
        public double AmountAt(int index)
        {
            var rule = Rule(index); double amount = rule.amount;
            if (index > 0)
            {
                var left = Rule(index - 1);
                if (left.effect == RelicEffectKind.AmplifyNextAddition && RelicDefinition.IsAdditive(rule.effect) && (left.anyAdditive || left.targetId == rule.id)) amount *= left.amount;
            }
            return amount;
        }
        public int IntegerAmountAt(int index) => (int)Math.Min(1000000, Math.Floor(AmountAt(index)));
        public int Sum(RelicEffectKind kind)
        {
            long total = 0; for (int i = 0; i < Count; i++) if (Rule(i).effect == kind) total += IntegerAmountAt(i);
            return (int)Math.Min(1000000, total);
        }
        public bool Contains(PrototypeRelic value) => slots.Contains(value);
        public bool Acquire(PrototypeRelic value, int replaceIndex = -1)
        {
            if (frozen != null || !PrototypeContent.KnownRelic((int)value) || Contains(value)) return false;
            if (Count < 5) { slots.Add(value); return true; }
            if (replaceIndex < 0 || replaceIndex >= Count) return false;
            slots[replaceIndex] = value; return true;
        }
        public int ContainmentBonus => Sum(RelicEffectKind.AddContainment);
        public int IncomingDamage(int damage) => Math.Max(1, damage - Sum(RelicEffectKind.ReduceIncomingDamage));
        public PrototypeRelicLine Snapshot()
        {
            var copy = new PrototypeRelicLine(slots) {frozen = new Dictionary<int, PrototypeRelicRule>()};
            for (int i = 0; i < Count; i++) copy.frozen.Add((int)slots[i], Rule(i));
            return copy;
        }
        public bool Move(int index, int direction)
        {
            if (frozen != null || Math.Abs(direction) != 1 || index < 0 || index >= Count) return false;
            int next = index + direction; if (next < 0 || next >= Count) return false;
            var value = slots[index]; slots[index] = slots[next]; slots[next] = value; return true;
        }
        public int Damage(int baseDamage, bool brasa, int enemyHealth, int enemyMaxHealth, out string trace)
        {
            double value = Math.Max(1, baseDamage); var log = new StringBuilder("Base " + baseDamage);
            for (int i = 0; i < Count; i++)
            {
                var rule = Rule(i); if (!rule.Matches(brasa, enemyHealth, enemyMaxHealth)) continue;
                if (rule.effect == RelicEffectKind.AddAttack)
                {
                    double bonus = AmountAt(i);
                    value += bonus; log.Append(" > " + rule.name + " +" + bonus);
                }
                else if (rule.effect == RelicEffectKind.MultiplyAttack) { value *= rule.amount; log.Append(" > " + rule.name + " x" + rule.amount); }
            }
            int result = (int)Math.Max(1, Math.Min(int.MaxValue, Math.Floor(value)));
            trace = log.Append(" = " + result).ToString(); return result;
        }
        public static string Name(PrototypeRelic value) => PrototypeContent.Relic((int)value).displayName;
        public static string Effect(PrototypeRelic value)
        {
            var definition = PrototypeContent.Relic((int)value);
            return definition.RuleText + (string.IsNullOrWhiteSpace(definition.flavorText) ? "" : " " + definition.flavorText);
        }
        public string Description(int index)
        {
            var rule = Rule(index);
            if (rule.effect != RelicEffectKind.AmplifyNextAddition) return Effect(slots[index]);
            return (index + 1 < Count && (rule.anyAdditive ? RelicDefinition.IsAdditive(Rule(index + 1).effect) : Rule(index + 1).id == rule.targetId) ? "ACTIVO: " : "INACTIVO: ") + Effect(slots[index]);
        }
    }
}

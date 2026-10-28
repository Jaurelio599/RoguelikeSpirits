using System;
using System.Collections.Generic;
using System.Text;

namespace SpiritPrototype
{
    public enum PrototypeRelic { Carbon, Lens, Prism }

    // The ordered line and damage arithmetic are independent of Unity.
    public sealed class PrototypeRelicLine
    {
        private readonly PrototypeRelic[] slots;
        public int Count => slots.Length;
        public PrototypeRelic this[int index] => slots[index];

        public PrototypeRelicLine() : this(new[] {PrototypeRelic.Carbon, PrototypeRelic.Lens, PrototypeRelic.Prism}) { }
        public PrototypeRelicLine(IEnumerable<PrototypeRelic> values)
        {
            var list = new List<PrototypeRelic>(values);
            if (list.Count > 5 || new HashSet<PrototypeRelic>(list).Count != list.Count)
                throw new ArgumentException("La fila admite cinco reliquias diferentes.");
            slots = list.ToArray();
        }

        public PrototypeRelicLine Snapshot() { return new PrototypeRelicLine(slots); }

        public bool Move(int index, int direction)
        {
            if (Math.Abs(direction) != 1 || index < 0 || index >= Count) return false;
            int next = index + direction;
            if (next < 0 || next >= Count) return false;
            PrototypeRelic value = slots[index]; slots[index] = slots[next]; slots[next] = value;
            return true;
        }

        public int Damage(int baseDamage, bool brasa, int enemyHealth, int enemyMaxHealth, out string trace)
        {
            double value = Math.Max(1, baseDamage);
            var log = new StringBuilder("Base " + baseDamage);
            for (int i = 0; i < Count; i++)
            {
                if (slots[i] == PrototypeRelic.Carbon && brasa)
                {
                    int bonus = i > 0 && slots[i - 1] == PrototypeRelic.Prism ? 8 : 4;
                    value += bonus;
                    log.Append(" > Carbon +" + bonus);
                }
                else if (slots[i] == PrototypeRelic.Lens && enemyHealth * 2 < enemyMaxHealth)
                { value *= 1.5; log.Append(" > Lente x1.5"); }
            }
            int result = Math.Max(1, (int)Math.Floor(value));
            trace = log.Append(" = " + result).ToString();
            return result;
        }

        public static string Name(PrototypeRelic value)
        {
            switch (value)
            {
                case PrototypeRelic.Carbon: return "Carbon tallado";
                case PrototypeRelic.Lens: return "Lente voraz";
                default: return "Prisma vecino";
            }
        }

        public string Description(int index)
        {
            switch (slots[index])
            {
                case PrototypeRelic.Carbon:
                    return "Suma 4 al ataque de Brasa. Ascua y Farol usan Brasa.";
                case PrototypeRelic.Lens:
                    return "Multiplica x1.5 si el enemigo esta por debajo de media vida.";
                default:
                    return index + 1 < Count && slots[index + 1] == PrototypeRelic.Carbon
                        ? "ACTIVO: duplica el +4 del Carbon inmediatamente a su derecha."
                        : "INACTIVO: necesita Carbon inmediatamente a su derecha.";
            }
        }
    }
}

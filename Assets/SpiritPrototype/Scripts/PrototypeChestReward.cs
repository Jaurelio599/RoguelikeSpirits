using System;
using System.Collections.Generic;

namespace SpiritPrototype
{
    // One offer per room, kept unchanged when its window is closed.
    public sealed class PrototypeChestReward
    {
        private readonly List<PrototypeRelic> options = new List<PrototypeRelic>();
        public IReadOnlyList<PrototypeRelic> Options => options.AsReadOnly();
        public bool Opened { get; private set; }
        public bool Consumed { get; private set; }

        public void Open(PrototypeRelicLine owned, Random random)
        {
            if (Opened || Consumed) return;
            var pool = new List<PrototypeRelic>();
            foreach (PrototypeRelic value in PrototypeContent.RelicIds)
                if (PrototypeContent.Relic((int)value).inChests && !owned.Contains(value)) pool.Add(value);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var value = pool[i]; pool[i] = pool[j]; pool[j] = value;
            }
            options.AddRange(pool.GetRange(0, Math.Min(3, pool.Count)));
            Opened = true;
        }

        public bool Claim(PrototypeRelicLine owned, int choice, int replaceIndex = -1)
        {
            if (!Opened || Consumed || choice < 0 || choice >= options.Count) return false;
            if (!owned.Acquire(options[choice], replaceIndex)) return false;
            Consumed = true;
            return true;
        }
        public void WriteTo(PrototypeRunSave state)
        {
            state.chestOpened = Opened; state.chestConsumed = Consumed;
            state.chestOptions.Clear();
            foreach (var option in options) state.chestOptions.Add((int)option);
        }
        public void Restore(PrototypeRunSave state)
        {
            options.Clear();
            foreach (int option in state.chestOptions) options.Add((PrototypeRelic)option);
            Opened = state.chestOpened; Consumed = state.chestConsumed;
        }
        public void Decline() { if (Opened) Consumed = true; }
    }
}

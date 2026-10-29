using System;
using System.Collections.Generic;

namespace SpiritPrototype
{
    [Serializable]
    public sealed class PrototypeMemberSave
    {
        public string species;
        public int health;
    }

    [Serializable]
    public sealed class PrototypeCodexEntry
    {
        public string species;
        public int seen, captures, bestRoom, completions;
        public int bestFloor, bestFloorRoom;
    }

    [Serializable]
    public sealed class PrototypeRunSave
    {
        public bool generatedFloor;
        public PrototypeFloorPlan floor;
        public int room, victories, captures, seals = 6, activeIndex;
        public float playerX = -2, playerY = -2;
        public bool sanctuaryUsed, rewarded;
        public List<PrototypeMemberSave> team = new List<PrototypeMemberSave>();
        public List<int> relics = new List<int>();
        public List<string> defeated = new List<string>();
        public List<string> seenThisRun = new List<string>();
        public bool chestOpened, chestConsumed;
        public List<int> chestOptions = new List<int>();
    }

    [Serializable]
    public sealed class PrototypeProfileData
    {
        public int schemaVersion = 1;
        public string name;
        public int expeditions, victories, defeats, bestRoom;
        public int bestFloor, bestFloorRoom;
        public List<PrototypeCodexEntry> codex = new List<PrototypeCodexEntry>();
        public PrototypeRunSave run;

        public static PrototypeProfileData Create(string name)
        {
            var profile = new PrototypeProfileData {name = string.IsNullOrWhiteSpace(name) ? "Explorador" : name.Trim()};
            profile.Entry("Ascua").seen = 1; // Granted companion, not an invented capture.
            return profile;
        }

        public PrototypeCodexEntry Entry(string species)
        {
            foreach (var entry in codex) if (entry.species == species) return entry;
            var created = new PrototypeCodexEntry {species = species};
            codex.Add(created);
            return created;
        }

        public bool RecordSeen(string species, string encounterKey = null)
        {
            string key = encounterKey ?? species;
            if (run == null || run.seenThisRun.Contains(key)) return false;
            run.seenThisRun.Add(key);
            Entry(species).seen++;
            return true;
        }

        public void RecordProgress(PrototypeRunSave state, bool won = false)
        {
            bestRoom = Math.Max(bestRoom, state.room + 1);
            if (state.generatedFloor && (bestFloor < 1 || bestFloorRoom < state.room + 1))
            { bestFloor = 1; bestFloorRoom = state.room + 1; }
            foreach (var member in state.team)
            {
                var entry = Entry(member.species);
                entry.bestRoom = Math.Max(entry.bestRoom, state.room + 1);
                if (state.generatedFloor && (entry.bestFloor < 1 || entry.bestFloorRoom < state.room + 1))
                { entry.bestFloor = 1; entry.bestFloorRoom = state.room + 1; }
                if (won) entry.completions++;
            }
        }

        public bool IsValid()
        {
            if (schemaVersion != 1 || string.IsNullOrWhiteSpace(name) || name.Length > 24 || codex == null ||
                expeditions < 0 || victories < 0 || defeats < 0 || bestRoom < 0 || bestRoom > 5 || bestFloor < 0 || bestFloor > 1 || bestFloorRoom < 0 || bestFloorRoom > 5) return false;
            var ids = new HashSet<string>();
            foreach (var entry in codex)
                if (entry == null || !PrototypeSpeciesCatalog.Known(entry.species) || !ids.Add(entry.species) ||
                    entry.seen < 0 || entry.captures < 0 || entry.bestRoom < 0 || entry.bestRoom > 5 || entry.completions < 0 || entry.bestFloor < 0 || entry.bestFloor > 1 || entry.bestFloorRoom < 0 || entry.bestFloorRoom > 5) return false;
            if (run == null) return true;
            if (run.room < 0 || run.room > (run.generatedFloor ? 4 : 3) || run.seals < 0 || run.victories < 0 || run.captures < 0 ||
                run.team == null || run.team.Count < 1 || run.team.Count > 4 ||
                run.activeIndex < 0 || run.activeIndex >= run.team.Count || run.team[run.activeIndex] == null ||
                run.team[run.activeIndex].health <= 0 || run.relics == null || run.relics.Count > 5 ||
                run.defeated == null || run.seenThisRun == null || run.chestOptions == null || run.chestOptions.Count > 3 ||
                float.IsNaN(run.playerX) || float.IsNaN(run.playerY) || float.IsInfinity(run.playerX) || float.IsInfinity(run.playerY) ||
                run.playerX < -6.6f || run.playerX > 5.6f || run.playerY < -4.6f || run.playerY > 3.6f) return false;
            if (run.generatedFloor && (run.floor == null || !run.floor.IsValid() || run.floor.Current.depth != run.room)) return false;
            foreach (var member in run.team)
            {
                if (member == null || !PrototypeSpeciesCatalog.Known(member.species) || !PrototypeSpeciesCatalog.CanBeAlly(member.species)) return false;
                if (member.health < 0 || member.health > 1000000) return false;
            }
            var relicIds = new HashSet<int>();
            foreach (int relic in run.relics)
                if (!PrototypeContent.KnownRelic(relic) || !relicIds.Add(relic)) return false;
            var offered = new HashSet<int>();
            foreach (int relic in run.chestOptions)
                if (!PrototypeContent.KnownRelic(relic) || !offered.Add(relic)) return false;
            foreach (string id in run.defeated) if (!PrototypeSpeciesCatalog.Known(id) || run.generatedFloor && !run.floor.Current.enemies.Contains(id)) return false;
            foreach (string id in run.seenThisRun) if (run.generatedFloor ? !run.floor.KnownEncounterKey(id) : !PrototypeSpeciesCatalog.Known(id)) return false;
            return true;
        }
    }

    public static class PrototypeSpeciesCatalog
    {
        public static string[] Names => PrototypeContent.SpeciesIds;
        public static bool Known(string species) => PrototypeContent.KnownSpirit(species);
        public static bool CanBeAlly(string species) => PrototypeContent.Spirit(species).canBeAlly;
        public static string Name(string species) => PrototypeContent.Spirit(species).displayName;
        public static string Rarity(string species) => PrototypeContent.Spirit(species).rarity;
    }
}

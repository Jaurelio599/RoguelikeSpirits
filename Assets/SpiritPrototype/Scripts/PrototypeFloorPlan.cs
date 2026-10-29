using System;
using System.Collections.Generic;

namespace SpiritPrototype
{
    public enum PrototypeRoomKind { Combat, Sanctuary, Boss }

    [Serializable]
    public struct PrototypeGridCell
    {
        public int x, y;
        public PrototypeGridCell(int x, int y) { this.x = x; this.y = y; }
        public int Key => (x + 6) + (y + 4) * 12;
    }

    [Serializable]
    public sealed class PrototypeFloorRoom
    {
        public int id, depth, layout, rewardSeed;
        public PrototypeRoomKind kind;
        public List<string> enemies = new List<string>();
        public List<PrototypeGridCell> blocks = new List<PrototypeGridCell>();
        public List<int> exits = new List<int>();
        public string Title => kind == PrototypeRoomKind.Sanctuary ? "Santuario" : kind == PrototypeRoomKind.Boss ? "Guardian del Umbral" : "Camara de ecos";
        public string Preview => kind == PrototypeRoomKind.Sanctuary ? "Recupera y reanima al equipo. Sin cofre."
            : kind == PrototypeRoomKind.Boss ? "Enfrenta al jefe para completar el piso." : "Dos encuentros, dos sellos y un cofre.";
    }

    [Serializable]
    public sealed class PrototypeFloorPlan
    {
        public int generatorVersion = 1, floorNumber = 1, seed;
        public List<PrototypeFloorRoom> rooms = new List<PrototypeFloorRoom>();
        public List<int> path = new List<int>();
        public PrototypeFloorRoom Current => rooms[path[path.Count - 1]];
        public string EncounterKey(string species) { return Current.id + "|" + species; }
        public bool MoveTo(int id)
        {
            if (!Current.exits.Contains(id)) return false;
            path.Add(id); return true;
        }
        public bool KnownEncounterKey(string key)
        {
            foreach (var room in rooms)
                foreach (string species in room.enemies)
                    if (key == room.id + "|" + species) return true;
            return false;
        }
        public static PrototypeFloorPlan Generate(int seed, IEnumerable<string> available)
        {
            var pool = new List<string>();
            foreach (string species in available)
                if (species != "Guardian del Umbral" && PrototypeSpeciesCatalog.Known(species) && !pool.Contains(species)) pool.Add(species);
            if (pool.Count < 2) throw new ArgumentException("El piso necesita al menos dos especies configuradas.");
            var random = new Random(seed);
            var plan = new PrototypeFloorPlan {seed = seed};
            int[] depths = {0, 1, 1, 2, 3, 3, 4};
            for (int id = 0; id < 7; id++)
            {
                var node = new PrototypeFloorRoom {id = id, depth = depths[id], layout = random.Next(4), rewardSeed = random.Next(),
                    kind = id == 6 ? PrototypeRoomKind.Boss : id == 2 || id == 5 ? PrototypeRoomKind.Sanctuary : PrototypeRoomKind.Combat};
                node.blocks = Layout(node.layout);
                if (node.kind == PrototypeRoomKind.Combat)
                {
                    int first = random.Next(pool.Count), second = random.Next(pool.Count - 1);
                    if (second >= first) second++;
                    node.enemies.Add(pool[first]); node.enemies.Add(pool[second]);
                }
                else if (node.kind == PrototypeRoomKind.Boss) node.enemies.Add("Guardian del Umbral");
                if (id == 0) node.exits.AddRange(new[] {1, 2});
                else if (id == 1 || id == 2) node.exits.Add(3);
                else if (id == 3) node.exits.AddRange(new[] {4, 5});
                else if (id == 4 || id == 5) node.exits.Add(6);
                if (node.exits.Count == 2 && random.Next(2) == 0) node.exits.Reverse();
                plan.rooms.Add(node);
            }
            plan.path.Add(0);
            if (!plan.IsValid()) throw new InvalidOperationException("El piso generado no paso la validacion de rutas.");
            return plan;
        }

        // Templates supply an initial layout; the actual cells are persisted in the save.
        private static List<PrototypeGridCell> Layout(int id)
        {
            var cells = new List<PrototypeGridCell>();
            if (id == 0)
            {
                cells.Add(new PrototypeGridCell(-3, -1)); cells.Add(new PrototypeGridCell(-3, 0));
                cells.Add(new PrototypeGridCell(2, -1)); cells.Add(new PrototypeGridCell(2, 0));
            }
            else if (id == 1)
                for (int x = -5; x <= 5; x++) { if (x >= -1 && x <= 1) continue; cells.Add(new PrototypeGridCell(x, 0)); }
            else if (id == 2)
            {
                for (int y = -2; y <= 1; y++) cells.Add(new PrototypeGridCell(-2, y));
                cells.Add(new PrototypeGridCell(2, -1)); cells.Add(new PrototypeGridCell(2, 0));
            }
            else
            {
                cells.Add(new PrototypeGridCell(-4, -2)); cells.Add(new PrototypeGridCell(-3, -2));
                cells.Add(new PrototypeGridCell(3, 0)); cells.Add(new PrototypeGridCell(4, 0));
                cells.Add(new PrototypeGridCell(1, 2)); cells.Add(new PrototypeGridCell(2, 2));
            }
            return cells;
        }

        public static bool Walkable(PrototypeFloorRoom room)
        {
            if (room == null || room.blocks == null || room.blocks.Count > 30) return false;
            var blocked = new HashSet<int>();
            foreach (var cell in room.blocks)
            {
                if (cell.x < -6 || cell.x > 5 || cell.y < -4 || cell.y > 3 || !blocked.Add(cell.Key)) return false;
            }
            var anchors = new[] {new PrototypeGridCell(0, -3), new PrototypeGridCell(0, 3),
                new PrototypeGridCell(4, 2), new PrototypeGridCell(-4, 1), new PrototypeGridCell(2, 1),
                new PrototypeGridCell(3, -2), new PrototypeGridCell(1, 1)};
            foreach (var anchor in anchors) if (blocked.Contains(anchor.Key)) return false;
            var seen = new HashSet<int>(); var queue = new Queue<PrototypeGridCell>();
            queue.Enqueue(anchors[0]); seen.Add(anchors[0].Key);
            int[] dx = {1, -1, 0, 0}, dy = {0, 0, 1, -1};
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    var next = new PrototypeGridCell(p.x + dx[i], p.y + dy[i]);
                    if (next.x < -6 || next.x > 5 || next.y < -4 || next.y > 3 || blocked.Contains(next.Key) || !seen.Add(next.Key)) continue;
                    queue.Enqueue(next);
                }
            }
            return seen.Count == 96 - blocked.Count; // Every free cell is reachable.
        }

        public bool IsValid()
        {
            if (generatorVersion != 1 || floorNumber != 1 || rooms == null || rooms.Count != 7 || path == null || path.Count < 1 || path.Count > 5 || path[0] != 0) return false;
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room == null || room.id != i || room.depth < 0 || room.depth > 4 || room.layout < 0 || room.layout > 3 ||
                    !Enum.IsDefined(typeof(PrototypeRoomKind), room.kind) || room.exits == null || room.enemies == null || !Walkable(room)) return false;
                if (room.depth == 4 ? room.kind != PrototypeRoomKind.Boss || room.exits.Count != 0 : room.kind == PrototypeRoomKind.Boss || room.exits.Count < 1 || room.exits.Count > 2) return false;
                if (room.kind == PrototypeRoomKind.Sanctuary && room.enemies.Count != 0) return false;
                if (room.kind == PrototypeRoomKind.Combat && room.enemies.Count != 2) return false;
                if (room.kind == PrototypeRoomKind.Boss && (room.enemies.Count != 1 || room.enemies[0] != "Guardian del Umbral")) return false;
                var species = new HashSet<string>();
                foreach (string enemy in room.enemies)
                    if (!PrototypeSpeciesCatalog.Known(enemy) || !species.Add(enemy) || room.kind == PrototypeRoomKind.Combat && enemy == "Guardian del Umbral") return false;
                var exits = new HashSet<int>();
                foreach (int exit in room.exits)
                    if (exit < 0 || exit >= rooms.Count || rooms[exit] == null || rooms[exit].depth != room.depth + 1 || !exits.Add(exit)) return false;
            }
            if (rooms[0].depth != 0 || rooms[0].kind != PrototypeRoomKind.Combat) return false;
            var reachable = new HashSet<int>(); var pending = new Queue<int>(); pending.Enqueue(0);
            while (pending.Count > 0) { int id = pending.Dequeue(); if (!reachable.Add(id)) continue; foreach (int next in rooms[id].exits) pending.Enqueue(next); }
            if (reachable.Count != rooms.Count) return false;
            for (int i = 0; i < path.Count; i++)
                if (path[i] < 0 || path[i] >= rooms.Count || rooms[path[i]].depth != i || i > 0 && !rooms[path[i - 1]].exits.Contains(path[i])) return false;
            return true;
        }
    }
}

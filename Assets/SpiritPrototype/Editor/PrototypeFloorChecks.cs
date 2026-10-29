using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeFloorChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar piso variable")]
        public static void Run()
        {
            string[] pool = {"Velo", "Raicero", "Campanilla", "Luto", "Farol"};
            var layouts = new HashSet<int>(); var pairs = new HashSet<string>();
            for (int seed = 0; seed < 1000; seed++)
            {
                var plan = PrototypeFloorPlan.Generate(seed, pool);
                Check(plan.IsValid(), "Piso valido.");
                foreach (var node in plan.rooms)
                {
                    Check(PrototypeFloorPlan.Walkable(node), "Puerta, objetos y encuentros alcanzables.");
                    layouts.Add(node.layout);
                    if (node.enemies.Count == 2) pairs.Add(node.enemies[0] + "/" + node.enemies[1]);
                }
                Check(JsonUtility.ToJson(plan) == JsonUtility.ToJson(PrototypeFloorPlan.Generate(seed, pool)), "Generacion determinista.");
                for (int i = 0; i < 4; i++) Check(plan.MoveTo(plan.Current.exits[0]), "Ruta valida de cinco salas.");
                Check(plan.path.Count == 5 && plan.Current.kind == PrototypeRoomKind.Boss && plan.IsValid(), "Toda ruta termina en el jefe.");
                Check(!plan.MoveTo(0), "No hay regreso ni ciclos.");
            }
            Check(layouts.Count == 4 && pairs.Count > 5, "Variedad de obstaculos y encuentros.");
            string directory = Path.Combine(Path.GetTempPath(), "SpiritFloorChecks-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profile = PrototypeProfileData.Create("Piso de prueba");
                var plan = PrototypeFloorPlan.Generate(12345, pool);
                profile.run = new PrototypeRunSave {generatedFloor = true, floor = plan, playerX = 0, playerY = -3};
                profile.run.team.Add(new PrototypeMemberSave {species = "Ascua", health = 14});
                profile.run.relics.Add((int)PrototypeRelic.Carbon);
                string species = plan.Current.enemies[0];
                Check(profile.RecordSeen(species, plan.EncounterKey(species)), "Cuenta el primer encuentro.");
                Check(!profile.RecordSeen(species, plan.EncounterKey(species)), "Cargar no duplica avistamientos.");
                var reward = new PrototypeChestReward(); reward.Open(new PrototypeRelicLine(), new System.Random(plan.Current.rewardSeed)); reward.WriteTo(profile.run);
                var store = new PrototypeProfileStore(directory); store.Save(0, profile);
                var loaded = store.Load(0, out _, out bool blocked);
                Check(!blocked && loaded != null && loaded.run.generatedFloor && loaded.run.floor.IsValid(), "Carga el piso sin regenerarlo.");
                Check(JsonUtility.ToJson(loaded.run.floor) == JsonUtility.ToJson(plan), "Conserva obstaculos, semillas, enemigos y bifurcaciones.");
                var restored = new PrototypeChestReward(); restored.Restore(loaded.run);
                Check(restored.Options[0] == reward.Options[0], "Conserva las opciones del cofre.");
                for (int i = 0; i < 4; i++) plan.MoveTo(plan.Current.exits[0]);
                profile.run.room = 4; profile.run.seenThisRun.Clear();
                profile.run.chestOpened = false; profile.run.chestOptions.Clear();
                profile.RecordProgress(profile.run, true);
                Check(profile.Entry("Ascua").bestFloor == 1 && profile.Entry("Ascua").bestFloorRoom == 5 && profile.IsValid(), "Museo distingue piso y sala.");
                plan.rooms[0].blocks.Add(new PrototypeGridCell(0, 3));
                Check(!plan.IsValid(), "Rechaza obstaculos sobre el acceso a la puerta.");
                Debug.Log("Espiritus: 1000 pisos validos, rutas, conectividad, guardado, cofres y museo correctos.");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Piso: " + message); }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeProfileChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar perfiles y guardado")]
        public static void Run()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SpiritProfilesChecks-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new PrototypeProfileStore(directory);
                CheckInlineNullBehavior(Path.Combine(directory, "InlineNullRegression"));
                var profile = Sample();
                Check(profile.IsValid(), "Estado valido de prueba.");
                Check(!profile.RecordSeen("Velo"), "Reabrir el encuentro no duplica avistamientos de la misma expedicion.");
                profile.RecordProgress(profile.run);
                Check(profile.Entry("Velo").bestRoom == 2, "El record pertenece a los miembros del equipo.");
                store.Save(0, profile);
                store.Save(1, PrototypeProfileData.Create("Otro perfil"));
                var loaded = store.Load(0, out string notice, out bool blocked);
                Check(!blocked && notice == "" && loaded != null, "Lectura del perfil.");
                Check(loaded.run.room == 1 && loaded.run.seals == 3 && loaded.run.team[1].health == 0 && loaded.run.activeIndex == 0,
                    "Conserva sala, sellos, lider y aliados incapacitados.");
                Check(loaded.run.playerX == 2.5f && loaded.run.playerY == -1.5f && loaded.run.defeated.Contains("Raicero"),
                    "Conserva posicion y encuentros resueltos.");
                Check(loaded.run.relics[3] == (int)PrototypeRelic.Shell && loaded.run.sanctuaryUsed,
                    "Conserva orden de reliquias y estado del santuario.");
                var chest = new PrototypeChestReward(); chest.Restore(loaded.run);
                chest.Open(new PrototypeRelicLine(), new System.Random(999));
                Check(chest.Options.Count == 3 && chest.Options[0] == PrototypeRelic.Edge, "Conserva las opciones sin sortear de nuevo.");
                Check(store.Load(1, out _, out _).name == "Otro perfil", "Los perfiles estan separados.");
                loaded.name = "Guardado posterior"; store.Save(0, loaded);
                File.WriteAllText(store.PathFor(0), "{corrupto");
                var recovered = store.Load(0, out notice, out blocked);
                Check(!blocked && recovered.name == "Prueba" && notice.Length > 0, "Recupera el respaldo anterior.");
                File.WriteAllText(store.PathFor(0), "{\"schemaVersion\":99,\"name\":\"Incompatible\"}");
                recovered = store.Load(0, out notice, out blocked);
                Check(!blocked && recovered.name == "Prueba", "Un formato incompatible tambien recupera el respaldo.");
                store.Save(0, recovered);
                Check(Directory.GetFiles(directory, "*.corrupt-*").Length == 1, "Conserva el archivo danado al recuperar.");
                string before = File.ReadAllText(store.PathFor(0));
                recovered.schemaVersion = 99;
                bool refused = false;
                try { store.Save(0, recovered); } catch (InvalidDataException) { refused = true; }
                Check(refused && File.ReadAllText(store.PathFor(0)) == before, "No sobrescribe con un formato no compatible.");
                File.WriteAllText(store.PathFor(0), "corrupto");
                File.WriteAllText(store.PathFor(0) + ".bak", "corrupto tambien");
                Check(store.Load(0, out _, out blocked) == null && blocked, "Bloquea el perfil ilegible sin recrearlo.");
                Check(File.ReadAllText(store.PathFor(0)) == "corrupto", "No destruye un perfil ilegible.");
                var ended = Sample(); ended.RecordProgress(ended.run, true); ended.victories++; ended.run = null;
                store.Save(2, ended);
                var won = store.Load(2, out _, out _);
                Check(won.run == null && won.Entry("Velo").captures == 1 && won.Entry("Velo").completions == 1,
                    "Terminar la expedicion conserva el museo y elimina solo la partida en curso.");
                Debug.Log("Espiritus: perfiles separados, museo, guardado, respaldo, validacion y opciones persistentes correctos.");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void CheckInlineNullBehavior(string directory)
        {
            Directory.CreateDirectory(directory);
            var store = new PrototypeProfileStore(directory);
            var empty = PrototypeProfileData.Create("Sin expedicion");
            store.Save(0, empty);
            var fresh = store.Load(0, out _, out bool blocked);
            Check(!blocked && fresh != null && fresh.run == null, "Perfil nuevo sin expedicion tras serializar.");

            // Reproduce Unity's inline null expansion explicitly, even in other C# runtimes.
            empty.run = new PrototypeRunSave(); // Empty team: invalid as an active expedition.
            string payload = "{\"fileFormat\":2,\"hasRun\":false,\"profile\":" + JsonUtility.ToJson(empty) + "}";
            File.WriteAllText(store.PathFor(1), payload);
            var expanded = store.Load(1, out _, out blocked);
            Check(!blocked && expanded != null && expanded.run == null,
                "Una clase expandida por Unity no crea una expedicion fantasma.");
            File.WriteAllText(store.PathFor(2), payload.Replace("\"hasRun\":false", "\"hasRun\":true"));
            Check(store.Load(2, out _, out blocked) == null && blocked,
                "Una expedicion marcada como activa sigue requiriendo un equipo valido.");
        }

        private static PrototypeProfileData Sample()
        {
            var profile = PrototypeProfileData.Create("Prueba");
            profile.expeditions = 1;
            var entry = profile.Entry("Velo"); entry.seen = 1; entry.captures = 1;
            profile.run = new PrototypeRunSave {room = 1, seals = 3, playerX = 2.5f, playerY = -1.5f,
                sanctuaryUsed = true, rewarded = true, chestOpened = true};
            profile.run.team.Add(new PrototypeMemberSave {species = "Ascua", health = 9});
            profile.run.team.Add(new PrototypeMemberSave {species = "Velo", health = 0});
            profile.run.relics.AddRange(new[] {(int)PrototypeRelic.Carbon, (int)PrototypeRelic.Lens, (int)PrototypeRelic.Prism, (int)PrototypeRelic.Shell});
            profile.run.chestOptions.AddRange(new[] {(int)PrototypeRelic.Edge, (int)PrototypeRelic.Knot, (int)PrototypeRelic.Dawn});
            profile.run.seenThisRun.Add("Velo"); profile.run.defeated.Add("Raicero");
            return profile;
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Perfiles: " + message); }
    }
}

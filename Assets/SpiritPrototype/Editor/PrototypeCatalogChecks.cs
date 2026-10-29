using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeCatalogChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar catalogos editables")]
        public static void Run()
        {
            var previous = PrototypeContent.Catalog;
            var test = PrototypeContentDefaults.CreateCatalog();
            SpiritDefinition addedSpirit = null; RelicDefinition addedRelic = null;
            string directory = Path.Combine(Path.GetTempPath(), "SpiritCatalogChecks-" + Guid.NewGuid().ToString("N"));
            try
            {
                addedSpirit = ScriptableObject.CreateInstance<SpiritDefinition>();
                addedSpirit.id = "catalog_test_spirit"; addedSpirit.displayName = "Espiritu de prueba";
                addedSpirit.allyMaxHealth = 23; addedSpirit.allyDamage = 8; addedSpirit.usesBrasa = true; addedSpirit.rarity = "Prueba";
                addedRelic = ScriptableObject.CreateInstance<RelicDefinition>();
                addedRelic.id = 42; addedRelic.displayName = "Reliquia de prueba"; addedRelic.amount = 3;
                test.spirits = new List<SpiritDefinition>(test.spirits) {addedSpirit}.ToArray();
                test.relics = new List<RelicDefinition>(test.relics) {addedRelic}.ToArray();
                PrototypeContent.UseCatalog(test);
                Check(PrototypeSpeciesCatalog.Known(addedSpirit.id) && PrototypeContent.KnownRelic(42), "Alta por datos, sin extender enum ni switch.");
                var member = PrototypeTeamMember.Create(addedSpirit.id, 21);
                Check(member.MaxHealth == 23 && member.Damage == 8 && member.IsBrasa && member.Health == 21, "Atributos del aliado desde ficha.");
                var line = new PrototypeRelicLine(new[] {(PrototypeRelic)42, PrototypeRelic.Lens});
                Check(line.Damage(10, false, 4, 20, out _) == 19, "Nuevo efecto reutilizado respeta el orden y el redondeo.");
                var snapshot = line.Snapshot(); addedRelic.amount = 9;
                Check(snapshot.Damage(10, false, 4, 20, out _) == 19 && line.Damage(10, false, 4, 20, out _) == 28, "Combate conserva los valores copiados al entrar.");
                addedRelic.amount = 3;
                bool offered = false;
                for (int seed = 0; seed < 100; seed++)
                {
                    var chest = new PrototypeChestReward(); chest.Open(new PrototypeRelicLine(), new System.Random(seed));
                    foreach (var option in chest.Options) if ((int)option == 42) offered = true;
                }
                Check(offered, "La reliquia nueva aparece en cofres.");
                addedRelic.inChests = false;
                for (int seed = 0; seed < 20; seed++)
                {
                    var chest = new PrototypeChestReward(); chest.Open(new PrototypeRelicLine(), new System.Random(seed));
                    foreach (var option in chest.Options) Check((int)option != 42, "Retirar de cofres no elimina el ID.");
                }
                var floor = PrototypeFloorPlan.Generate(5, new[] {addedSpirit.id, "Velo"});
                Check(floor.IsValid() && floor.Current.enemies.Contains(addedSpirit.id), "El piso acepta especies nuevas.");
                var profile = PrototypeProfileData.Create("Catalogo"); profile.Entry(addedSpirit.id).seen = 1;
                profile.run = new PrototypeRunSave(); profile.run.team.Add(new PrototypeMemberSave {species = addedSpirit.id, health = 21}); profile.run.relics.Add(42);
                var store = new PrototypeProfileStore(directory); store.Save(0, profile);
                addedSpirit.displayName = "Nombre cambiado"; addedRelic.displayName = "Nombre de reliquia cambiado";
                addedSpirit.allyMaxHealth = 10;
                var loaded = store.Load(0, out _, out bool blocked);
                Check(!blocked && loaded != null && loaded.run.team[0].species == addedSpirit.id && loaded.run.relics[0] == 42, "Los IDs sobreviven a nombres y balance nuevos.");
                Check(PrototypeTeamMember.Create(addedSpirit.id, loaded.run.team[0].health).Health == 10, "La vida se limita al nuevo maximo al reconstruir.");
                addedRelic.id = 0;
                Check(test.ValidateCatalog().Length > 0, "Rechaza IDs duplicados.");
                addedRelic.id = 42;
                Check(test.ValidateCatalog().Length == 0, "Catalogo reparado.");
                Debug.Log("Espiritus: fichas nuevas, efectos, orden, cofres, pisos, guardado, IDs y copia de combate correctos.");
            }
            finally
            {
                PrototypeContent.UseCatalog(previous);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
                foreach (var s in test.spirits) UnityEngine.Object.DestroyImmediate(s);
                foreach (var r in test.relics) UnityEngine.Object.DestroyImmediate(r);
                UnityEngine.Object.DestroyImmediate(test);
            }
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Catalogos: " + message); }
    }
}

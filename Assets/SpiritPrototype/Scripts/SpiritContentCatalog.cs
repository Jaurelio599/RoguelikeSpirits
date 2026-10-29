using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritPrototype
{
    [CreateAssetMenu(menuName = "Espiritus/Catalogo de contenido", fileName = "ContentCatalog")]
    public sealed class SpiritContentCatalog : ScriptableObject
    {
        public SpiritDefinition[] spirits = new SpiritDefinition[0];
        public RelicDefinition[] relics = new RelicDefinition[0];
        public string ValidateCatalog()
        {
            if (spirits == null || relics == null) return "Faltan las listas del catalogo.";
            var species = new HashSet<string>(StringComparer.Ordinal);
            var items = new Dictionary<int, RelicDefinition>();
            foreach (var s in spirits)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.id) || s.id != s.id.Trim() || s.id.Contains("|") || !species.Add(s.id)) return "ID de espiritu vacio, duplicado o con el caracter |.";
                if (s.idleFrames != null && s.idleFrames.Length > 0 && (s.idleFramesPerSecond <= 0 || float.IsNaN(s.idleFramesPerSecond) || float.IsInfinity(s.idleFramesPerSecond))) return "Velocidad de animacion invalida en " + s.id;
                if (s.idleFrames != null) foreach (var frame in s.idleFrames) if (frame == null) return "Hay un fotograma vacio en " + s.id;
                if (string.IsNullOrWhiteSpace(s.displayName) || s.mapSize < .1f || float.IsNaN(s.mapSize) || float.IsInfinity(s.mapSize) || s.enemyMaxHealth < 1 || s.enemyMaxHealth > 1000000 || s.enemyAttack < 1 || s.enemyAttack > 1000000 || s.enemyStability < 1 || s.enemyStability > 1000000 ||
                    s.canBeAlly && (s.allyMaxHealth < 1 || s.allyMaxHealth > 1000000 || s.allyDamage < 1 || s.allyDamage > 1000000 || s.attackRupture < 0 || s.attackRupture > 1000000 || s.containment < 0 || s.containment > 1000000) || s.capturable && !s.canBeAlly) return "Atributos invalidos en " + s.id;
            }
            foreach (string core in PrototypeContentDefaults.CoreSpecies) if (!species.Contains(core)) return "Falta el ID original " + core + ". Conserva la ficha y desactiva su aparicion si quieres retirarlo.";
            int spawns = 0;
            foreach (var s in spirits)
            {
                if (s.spawnInDungeon && s.id != "Guardian del Umbral") spawns++;
                if (Array.IndexOf(PrototypeContentDefaults.CoreSpecies, s.id) >= 0 && s.id != "Guardian del Umbral" && !s.canBeAlly) return "Conserva Can Be Ally en las especies originales para cargar equipos anteriores: " + s.id;
                if (s.id == "Guardian del Umbral" && (s.canBeAlly || s.capturable || s.spawnInDungeon)) return "El Guardian original debe conservar su rol de jefe no capturable.";
            }
            if (spawns < 2) return "El piso necesita al menos dos especies con Spawn In Dungeon.";
            var trialKeys = new HashSet<string>();
            foreach (var r in relics)
            {
                if (r == null || r.id < 0 || items.ContainsKey(r.id) || string.IsNullOrWhiteSpace(r.displayName)) return "ID de reliquia invalido o duplicado.";
                if (!string.IsNullOrEmpty(r.prototypeKey) && !trialKeys.Add(r.prototypeKey)) return "Clave de reliquia de prueba duplicada: " + r.prototypeKey;
                if (!Enum.IsDefined(typeof(RelicEffectKind), r.effect) || !Enum.IsDefined(typeof(RelicCondition), r.condition) || r.amount <= 0 || r.amount > 1000000 || float.IsNaN(r.amount) || float.IsInfinity(r.amount)) return "Efecto invalido en reliquia " + r.id;
                if ((r.effect == RelicEffectKind.ReduceIncomingDamage || r.effect == RelicEffectKind.AddContainment || r.effect == RelicEffectKind.AmplifyNextAddition || r.effect == RelicEffectKind.CaptureBonus || r.effect == RelicEffectKind.OpeningShield || r.effect == RelicEffectKind.EndRoundHeal || r.effect == RelicEffectKind.SwitchShield || r.effect == RelicEffectKind.VictoryEssence) && r.condition != RelicCondition.Always) return "El efecto de soporte o vecindad requiere Always: reliquia " + r.id;
                if ((r.effect == RelicEffectKind.ReduceIncomingDamage || r.effect == RelicEffectKind.AddContainment || r.effect == RelicEffectKind.RuptureBurnedAfterAttack || r.effect == RelicEffectKind.CaptureBonus || r.effect == RelicEffectKind.OpeningShield || r.effect == RelicEffectKind.EndRoundHeal || r.effect == RelicEffectKind.SwitchShield || r.effect == RelicEffectKind.VictoryEssence) && r.amount != (int)r.amount) return "La defensa y contencion usan valores enteros: reliquia " + r.id;
                items.Add(r.id, r);
            }
            for (int id = 0; id < 7; id++) if (!items.ContainsKey(id)) return "Falta la reliquia original ID " + id;
            foreach (var r in relics)
                if (r.effect == RelicEffectKind.AmplifyNextAddition && !r.amplifyAnyAdditive && (!items.TryGetValue(r.targetRelicId, out var target) || !RelicDefinition.IsAdditive(target.effect))) return "La vecindad debe apuntar a una reliquia AddAttack: ID " + r.id;
            return "";
        }
    }

    public static class PrototypeContent
    {
        private static SpiritContentCatalog catalog;
        private static Dictionary<string, SpiritDefinition> spirits;
        private static Dictionary<int, RelicDefinition> relics;
        private static string[] names;
        private static PrototypeRelic[] relicIds;
        public static SpiritContentCatalog Catalog { get { Ensure(); return catalog; } }
        public static string[] SpeciesIds { get { Ensure(); return names; } }
        public static PrototypeRelic[] RelicIds { get { Ensure(); return relicIds; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reload() { catalog = null; spirits = null; relics = null; names = null; relicIds = null; }
        private static void Ensure()
        {
            if (catalog != null) return;
            var asset = Resources.Load<SpiritContentCatalog>("SpiritPrototype/ContentCatalog");
            UseCatalog(asset != null ? asset : PrototypeContentDefaults.CreateCatalog());
        }
        public static void UseCatalog(SpiritContentCatalog value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            string error = value.ValidateCatalog();
            if (error.Length > 0) throw new InvalidOperationException("Catalogo: " + error);
            var nextSpirits = new Dictionary<string, SpiritDefinition>(StringComparer.Ordinal);
            var nextRelics = new Dictionary<int, RelicDefinition>();
            var nextNames = new List<string>(); var nextIds = new List<PrototypeRelic>();
            foreach (var s in value.spirits) { nextSpirits.Add(s.id, s); nextNames.Add(s.id); }
            foreach (var r in value.relics) { nextRelics.Add(r.id, r); nextIds.Add((PrototypeRelic)r.id); }
            spirits = nextSpirits; relics = nextRelics; names = nextNames.ToArray(); relicIds = nextIds.ToArray(); catalog = value;
        }
        public static bool KnownSpirit(string id) { Ensure(); return id != null && spirits.ContainsKey(id); }
        public static bool KnownRelic(int id) { Ensure(); return relics.ContainsKey(id); }
        public static SpiritDefinition Spirit(string id)
        { Ensure(); if (id != null && spirits.TryGetValue(id, out var s)) return s; throw new ArgumentException("Especie no registrada: " + id); }
        public static RelicDefinition Relic(int id)
        { Ensure(); if (relics.TryGetValue(id, out var r)) return r; throw new ArgumentException("Reliquia no registrada: " + id); }
    }
}

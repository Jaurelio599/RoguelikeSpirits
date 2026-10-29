using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace SpiritPrototype.Editor
{
    public static class SpiritTrialChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar reliquias y espiritus de prueba")]
        public static void Run()
        {
            var previous = PrototypeContent.Catalog;
            var catalog = PrototypeContentDefaults.CreateCatalog();
            var relics = new List<RelicDefinition>(catalog.relics);
            for (int i = 0; i < 7; i++) relics.Add(PrototypeContentDefaults.TrialRelic(i, 7 + i));
            catalog.relics = relics.ToArray(); catalog.relics[2].amplifyAnyAdditive = true;
            try
            {
                PrototypeContent.UseCatalog(catalog);
                var forward = Battle(7, 8); forward.Act(PrototypeAction.Attack, true, 1);
                var reverse = Battle(8, 7); reverse.Act(PrototypeAction.Attack, true, 1);
                Check(forward.Stability == 5 && reverse.Stability == 8 && forward.Burning && forward.EnemyHealth == 97, "Mecha antes de Campana y dano de Quemadura.");
                var amplified = Battle(7, 2, 8); amplified.Act(PrototypeAction.Attack, true, 1);
                Check(amplified.Stability == 2, "Prisma amplifica la ruptura de Campana.");
                var seal = Battle(2, 9); seal.DebugExpose();
                Check(seal.CaptureChance == 80, "Sello +20 con Prisma.");
                seal.Act(PrototypeAction.Capture, true, .79); Check(seal.Outcome == PrototypeOutcome.Captured, "La probabilidad se usa al capturar.");
                var bastion = Battle(10); Check(bastion.AllyShield == 4, "Bastion abre con escudo.");
                bastion.Act(PrototypeAction.Attack, true, 1); Check(bastion.AllyShield == 1 && bastion.AllyHealth == 20, "Escudo absorbe antes de vida.");
                Check(Battle(2, 10).AllyShield == 0, "Bastion no se activa fuera del primer espacio.");
                var heal = new PrototypeBattle(10, 100, 6, line: Line(2, 11)); heal.Act(PrototypeAction.Defend, true, 1);
                Check(heal.AllyHealth == 12, "Farol final cura cuatro con Prisma.");
                var notLast = new PrototypeBattle(10, 100, 6, line: Line(11, 0)); notLast.Act(PrototypeAction.Defend, true, 1);
                Check(notLast.AllyHealth == 8, "Farol solo funciona al final.");
                var relay = Battle(12); relay.SwitchAlly(7, 20, 5, 2, 5);
                Check(relay.AllyHealth == 7 && relay.AllyShield == 1, "Cambio recibe escudo antes de la respuesta.");
                var forced = new PrototypeBattle(1, 100, 6, line: Line(12)); forced.Act(PrototypeAction.Defend, true, 1);
                Check(forced.ReplaceFallenAlly(10, 20, 5, 2, 5) && forced.AllyShield == 0, "Reemplazo por baja no activa Cuerda.");
                var coin = new PrototypeBattle(20, 1, 6, line: Line(2, 13)); coin.Act(PrototypeAction.Attack, true, 1);
                Check(coin.EarnedEssence == 6 && !coin.Act(PrototypeAction.Attack, true, 1), "Moneda entrega esencia una sola vez.");
                var dot = new PrototypeBattle(20, 3, 6, attackDamage: 1, line: Line(7, 13)); dot.Act(PrototypeAction.Attack, true, 1);
                Check(dot.Outcome == PrototypeOutcome.Victory && dot.EarnedEssence == 0, "Una baja por Quemadura no activa Moneda.");
                var duration = Battle(7); duration.Act(PrototypeAction.Attack, true, 1); duration.SwitchAlly(20, 20, 1, 2, 5, false);
                duration.Act(PrototypeAction.Defend, true, 1); duration.Act(PrototypeAction.Defend, true, 1);
                Check(!duration.Burning && duration.EnemyHealth == 93, "Quemadura dura la ronda actual y dos posteriores, sin acumular intensidad.");
                Debug.Log("Espiritus: orden, vecindad, Quemadura, captura, escudos, curacion, cambios y esencia correctos.");
            }
            finally
            {
                PrototypeContent.UseCatalog(previous);
                foreach (var s in catalog.spirits) UnityEngine.Object.DestroyImmediate(s);
                foreach (var r in catalog.relics) UnityEngine.Object.DestroyImmediate(r);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
        private static PrototypeRelicLine Line(params int[] ids)
        { var values = new List<PrototypeRelic>(); foreach (int id in ids) values.Add((PrototypeRelic)id); return new PrototypeRelicLine(values); }
        private static PrototypeBattle Battle(params int[] ids) => new PrototypeBattle(20, 100, 6, attackDamage: 1, line: Line(ids));
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Pruebas: " + message); }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeDebugChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar controles debug")]
        public static void Run()
        {
            var battle = new PrototypeBattle(8, 18, 0);
            Check(battle.DebugResolve(true, true), "Captura sin sellos ni exposicion.");
            Check(battle.Outcome == PrototypeOutcome.Captured && battle.Seals == 0 && battle.Round == 1 && battle.AllyHealth == 8,
                "Capturar no gasta turno ni provoca respuesta.");
            Check(!battle.DebugResolve(false, true), "No resuelve dos veces el mismo combate.");
            var boss = new PrototypeBattle(20, 32, 6, capturable: false);
            Check(!boss.DebugResolve(true, true), "El jefe sigue sin ser capturable.");
            Check(boss.DebugResolve(false, true) && boss.EnemyHealth == 0 && boss.Outcome == PrototypeOutcome.Victory, "Victoria inmediata sobre jefe.");
            var full = new PrototypeBattle(20, 18, 6);
            Check(!full.DebugResolve(true, false) && full.Outcome == PrototypeOutcome.Ongoing, "No sobrepasa cuatro aliados.");
            full.DebugEnemy(true, true);
            Check(full.EnemyHealth == 7 && full.Exposed && full.Round == 1, "Condiciones de prueba sin avanzar la ronda.");
            full.DebugEnemy(false, false);
            Check(full.EnemyHealth == 18, "Restaurar vida del enemigo.");
            full.DebugSeals(25); Check(full.Seals == 31, "Sellos sincronizados.");
            var wounded = new PrototypeBattle(1, 18, 6);
            wounded.Act(PrototypeAction.Defend, true, 1);
            Check(wounded.Outcome == PrototypeOutcome.Defeat, "Fixture de baja.");
            wounded.DebugHeal();
            Check(wounded.Outcome == PrototypeOutcome.Ongoing && wounded.AllyHealth == 20, "Reanimacion de prueba.");
            Debug.Log("Espiritus: resolucion inmediata, limites, vida, estabilidad, reanimacion y sellos DEBUG correctos.");
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Debug: " + message); }
    }
}

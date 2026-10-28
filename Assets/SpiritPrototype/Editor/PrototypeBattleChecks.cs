using System;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeBattleChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar reglas de combate")]
        public static void Run()
        {
            var victory = new PrototypeBattle(20, 18, 6);
            for (int i = 0; i < 4; i++) victory.Act(PrototypeAction.Attack, true, 1);
            Check(victory.Outcome == PrototypeOutcome.Victory && victory.AllyHealth == 11,
                "La victoria debe impedir el contraataque del enemigo derrotado.");
            Check(!victory.Act(PrototypeAction.Attack, true, 1), "No actuar tras terminar.");

            var capture = Exposed();
            Check(capture.CaptureChance == 60, "Captura base visible.");
            Check(capture.Act(PrototypeAction.Capture, true, 0), "Captura valida.");
            Check(capture.Outcome == PrototypeOutcome.Captured && capture.Seals == 5 && capture.AllyHealth == 14,
                "Una captura exitosa consume un sello e impide el contraataque.");

            var failure = Exposed();
            failure.Act(PrototypeAction.Capture, true, 1);
            Check(failure.Outcome == PrototypeOutcome.Ongoing && failure.Seals == 5 &&
                failure.AllyHealth == 11 && failure.Stability == 5 && !failure.Exposed,
                "Un fallo consume turno y la exposicion vence al final de la ronda siguiente.");

            var full = Exposed();
            Check(!full.Act(PrototypeAction.Capture, false, 0) && full.Seals == 6 && full.Round == 3,
                "Equipo lleno no debe gastar sellos ni turnos.");
            var noSeals = new PrototypeBattle(20, 18, 0);
            noSeals.Act(PrototypeAction.Contain, true, 1); noSeals.Act(PrototypeAction.Contain, true, 1);
            Check(!noSeals.Act(PrototypeAction.Capture, true, 0) && noSeals.Round == 3,
                "Sin sellos no debe consumirse turno.");

            var unopened = new PrototypeBattle(20, 18, 6);
            Check(!unopened.Act(PrototypeAction.Capture, true, 0) && unopened.Round == 1 && unopened.Seals == 6,
                "No capturar sin exposicion.");
            var low = new PrototypeBattle(20, 8, 6);
            low.Act(PrototypeAction.Attack, true, 1);
            Check(low.CaptureChance == 75, "La mitad de vida anade 15 puntos porcentuales.");
            var defeat = new PrototypeBattle(2, 18, 6);
            defeat.Act(PrototypeAction.Defend, true, 1);
            Check(defeat.Outcome == PrototypeOutcome.Defeat && defeat.AllyHealth == 0,
                "Defender reduce dano sin permitir vida negativa.");
            Debug.Log("Espiritus: comprobaciones de victoria, derrota, captura, recursos y exposicion correctas.");
        }

        private static PrototypeBattle Exposed()
        {
            var battle = new PrototypeBattle(20, 18, 6);
            battle.Act(PrototypeAction.Contain, true, 1);
            battle.Act(PrototypeAction.Contain, true, 1);
            Check(battle.Exposed && battle.Round == 3, "Dos contenciones deben preparar captura.");
            return battle;
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Reglas de combate: " + message); }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeTeamChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar cambios de equipo")]
        public static void Run()
        {
            var battle = new PrototypeBattle(20, 18, 6);
            battle.Act(PrototypeAction.Contain, true, 1);
            int enemyHealth = battle.EnemyHealth, stability = battle.Stability;
            Check(battle.SwitchAlly(10, 26, 4, 2, 4), "Cambio voluntario aceptado.");
            Check(battle.AllyHealth == 7 && battle.ActiveMaxHealth == 26,
                "El cambio conserva la herida y aplica un solo golpe al entrante.");
            Check(battle.EnemyHealth == enemyHealth && battle.Stability == stability && battle.Round == 3,
                "Cambiar conserva al enemigo y consume una ronda.");
            Check(battle.AttackDamage == 4 && battle.ContainRupture == 4,
                "El nuevo aliado usa sus propios valores.");

            var exposed = new PrototypeBattle(20, 18, 6);
            exposed.Act(PrototypeAction.Contain, true, 1);
            exposed.Act(PrototypeAction.Contain, true, 1);
            exposed.SwitchAlly(18, 18, 4, 3, 6);
            Check(!exposed.Exposed && exposed.Stability == 5, "Cambiar tambien hace avanzar la exposicion.");

            var fallen = new PrototypeBattle(2, 18, 6);
            fallen.Act(PrototypeAction.Defend, true, 1);
            int round = fallen.Round;
            Check(fallen.Outcome == PrototypeOutcome.Defeat && fallen.AllyHealth == 0, "Baja del aliado.");
            Check(!fallen.ReplaceFallenAlly(0, 18, 4, 3, 6), "No usar un reemplazo incapacitado.");
            Check(fallen.ReplaceFallenAlly(9, 18, 4, 3, 6), "Reemplazo por baja.");
            Check(fallen.AllyHealth == 9 && fallen.Round == round && fallen.Outcome == PrototypeOutcome.Ongoing,
                "Un reemplazo forzoso no recibe otro golpe ni consume una ronda.");
            Check(!fallen.ReplaceFallenAlly(18, 18, 4, 3, 6), "No reemplazar por baja a un aliado vivo.");

            var captured = PrototypeTeamMember.Create("Raicero", 8);
            Check(captured.Health == 8 && captured.MaxHealth == 26, "Capturar no cura al nuevo aliado.");
            Debug.Log("Espiritus: comprobaciones de cambio, reemplazo, vida, estabilidad y captura correctas.");
        }

        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Equipo: " + message); }
    }
}

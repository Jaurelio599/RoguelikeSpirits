using System;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeExpeditionChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar reglas del jefe")]
        public static void Run()
        {
            var boss = new PrototypeBattle(20, 32, 6, enemyDamage: 4, capturable: false);
            Check(!boss.Act(PrototypeAction.Capture, true, 0) && boss.Seals == 6 && boss.Round == 1,
                "El jefe no puede capturarse ni consumir recursos por un intento invalido.");
            boss.Act(PrototypeAction.Contain, true, 1);
            Check(boss.AllyHealth == 16, "El jefe usa su propio dano.");
            boss.Act(PrototypeAction.Contain, true, 1);
            Check(boss.AllyHealth == 16 && boss.Exposed, "Romper estabilidad cancela exactamente una respuesta.");
            boss.Act(PrototypeAction.Defend, true, 1);
            Check(boss.AllyHealth == 14 && boss.Stability == 5, "La respuesta vuelve y defender divide su dano.");
            var large = new PrototypeBattle(20, 32, 6, enemyDamage: 4, capturable: false, enemyMaxStability: 20);
            Check(large.Stability == 20 && large.EnemyMaxStability == 20, "Estabilidad configurable del jefe.");
            large.Act(PrototypeAction.Contain, true, 1);
            Check(large.Stability == 15 && large.AllyHealth == 16, "No romper guardia antes del umbral.");
            Debug.Log("Espiritus: jefe no capturable, dano, defensa y ruptura de guardia correctos.");
        }

        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Jefe: " + message); }
    }
}

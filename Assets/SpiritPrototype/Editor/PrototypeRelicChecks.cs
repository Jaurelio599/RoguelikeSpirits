using System;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeRelicChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar orden de reliquias")]
        public static void Run()
        {
            var sumFirst = new PrototypeRelicLine(new[] {PrototypeRelic.Carbon, PrototypeRelic.Lens});
            var multiplyFirst = new PrototypeRelicLine(new[] {PrototypeRelic.Lens, PrototypeRelic.Carbon});
            Check(sumFirst.Damage(10, true, 4, 20, out _) == 21, "Sumar antes de multiplicar.");
            Check(multiplyFirst.Damage(10, true, 4, 20, out _) == 19, "Multiplicar antes de sumar.");
            var amplified = new PrototypeRelicLine(new[] {PrototypeRelic.Prism, PrototypeRelic.Carbon, PrototypeRelic.Lens});
            Check(amplified.Damage(10, true, 4, 20, out _) == 27, "Prisma solo duplica el valor del vecino derecho.");
            Check(amplified.Damage(10, false, 4, 20, out _) == 15, "Carbon no mejora ataques que no son Brasa.");
            Check(sumFirst.Damage(10, true, 10, 20, out _) == 14, "Media vida exacta no activa Lente.");
            Check(sumFirst.Damage(5, true, 4, 20, out _) == 13, "Redondeo hacia abajo al finalizar.");
            Check(!sumFirst.Move(0, -1), "No salir del extremo izquierdo.");
            Check(!sumFirst.Move(1, 1), "No salir del extremo derecho.");

            var source = new PrototypeRelicLine(new[] {PrototypeRelic.Carbon, PrototypeRelic.Lens});
            var battle = new PrototypeBattle(20, 18, 6, line: source);
            Check(battle.PredictedAttackDamage == 9, "Prevision inicial.");
            source.Move(0, 1);
            battle.Act(PrototypeAction.Contain, true, 1);
            battle.Act(PrototypeAction.Contain, true, 1);
            battle.Act(PrototypeAction.Attack, true, 1);
            Check(battle.EnemyHealth == 7 && battle.PredictedAttackDamage == 13,
                "La fila de combate conserva una copia del orden al entrar.");
            battle.SwitchAlly(18, 18, 4, 3, 6, false);
            Check(battle.PredictedAttackDamage == 6, "Cambiar a Niebla desactiva Carbon sin perder Lente.");
            Debug.Log("Espiritus: orden, vecindad, condiciones, redondeo y bloqueo de reliquias correctos.");
        }

        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Reliquias: " + message); }
    }
}

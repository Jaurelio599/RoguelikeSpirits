using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PrototypeRewardChecks
    {
        [MenuItem("Tools/Espiritus/Diagnostico/Verificar cofres y recompensas")]
        public static void Run()
        {
            // Each seed traverses two rooms and tests stable offers and capacity.
            for (int seed = 0; seed < 100; seed++)
            {
                var line = new PrototypeRelicLine();
                var random = new System.Random(seed);
                for (int room = 0; room < 2; room++)
                {
                    var chest = new PrototypeChestReward();
                    chest.Open(line, random);
                    Check(chest.Options.Count == 3, "Tres opciones en cada sala.");
                    var distinct = new HashSet<PrototypeRelic>();
                    foreach (var option in chest.Options)
                        Check(!line.Contains(option) && distinct.Add(option), "Sin duplicadas ni equipadas.");
                    var first = chest.Options[0];
                    chest.Open(line, random);
                    Check(chest.Options[0] == first, "Reabrir conserva las opciones.");
                    Check(!chest.Claim(line, -1), "No acepta opciones invalidas.");
                    Check(chest.Claim(line, 0), "Entrega una reliquia.");
                    Check(!chest.Claim(line, 1), "No entrega dos recompensas.");
                }
                Check(line.Count == 5, "Dos salas completan los cinco espacios.");
                var fullChest = new PrototypeChestReward();
                fullChest.Open(line, random);
                Check(!fullChest.Claim(line, 0), "No sobrepasa cinco espacios.");
                var oldLast = line[4];
                Check(fullChest.Claim(line, 0, 2) && line.Count == 5 && line[4] == oldLast,
                    "Reemplaza solo el espacio seleccionado.");
                var declined = new PrototypeChestReward();
                declined.Open(line, random); declined.Decline();
                Check(declined.Consumed && !declined.Claim(line, 0, 1), "Rechazo definitivo.");
            }
            var attack = new PrototypeRelicLine(new[] {PrototypeRelic.Edge, PrototypeRelic.Lens});
            Check(attack.Damage(10, false, 4, 20, out _) == 18, "Colmillo antes de Lente.");
            attack.Move(0, 1);
            Check(attack.Damage(10, false, 4, 20, out _) == 17, "Colmillo despues de Lente.");
            var dawn = new PrototypeRelicLine(new[] {PrototypeRelic.Dawn});
            Check(dawn.Damage(5, false, 20, 20, out _) == 8 &&
                dawn.Damage(5, false, 19, 20, out _) == 5, "Alba solo con vida completa.");
            var support = new PrototypeRelicLine(new[] {PrototypeRelic.Knot, PrototypeRelic.Shell});
            var battle = new PrototypeBattle(20, 30, 6, line: support);
            Check(battle.PredictedContainment == 7, "Nudo aumenta la contencion prevista.");
            battle.Act(PrototypeAction.Contain, true, 1);
            Check(battle.Stability == 3 && battle.AllyHealth == 18, "Nudo y Piedra afectan al combate.");
            battle.Act(PrototypeAction.Defend, true, 1);
            Check(battle.AllyHealth == 17, "Piedra se aplica despues de defender.");
            Check(support.IncomingDamage(1) == 1, "Minimo de un punto de dano.");
            var source = new PrototypeRelicLine();
            var snapshotBattle = new PrototypeBattle(20, 30, 6, line: source);
            source.Acquire(PrototypeRelic.Edge);
            Check(snapshotBattle.PredictedAttackDamage == 9, "La fila queda bloqueada al iniciar combate.");
            Debug.Log("Espiritus: cofres, opciones, reemplazo, rechazo y nuevas reliquias correctos (100 semillas).");
        }
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException("Recompensas: " + message); }
    }
}

using System;

namespace SpiritPrototype
{
    public enum PrototypeAction { Attack, Contain, Defend, Capture }
    public enum PrototypeOutcome { Ongoing, Victory, Captured, Defeat }

    // Rules have no dependency on Unity scenes, input, animation or UI.
    // This is a deliberately small 1v1 slice of the design manual.
    public sealed class PrototypeBattle
    {
        public const int AllyMaxHealth = 20;
        public const int MaxStability = 10;
        public int AllyHealth { get; private set; }
        public int EnemyHealth { get; private set; }
        public int EnemyMaxHealth { get; }
        public int ActiveMaxHealth { get; private set; }
        public int AttackDamage { get; private set; }
        public int AttackRupture { get; private set; }
        public int ContainRupture { get; private set; }
        public int Stability { get; private set; } = MaxStability;
        public int Seals { get; private set; }
        public int Round { get; private set; } = 1;
        public string Message { get; private set; } = "Elige una accion.";
        public PrototypeOutcome Outcome { get; private set; }
        private int exposedThroughRound;
        private readonly PrototypeRelicLine relics;
        private bool brasa;
        public string LastCalculation { get; private set; } = "";
        public int PredictedAttackDamage => relics.Damage(AttackDamage, brasa, EnemyHealth, EnemyMaxHealth, out _);
        public string PredictedAttackTrace
        {
            get { relics.Damage(AttackDamage, brasa, EnemyHealth, EnemyMaxHealth, out string trace); return trace; }
        }

        public bool Exposed => Stability == 0 && Round <= exposedThroughRound;
        public int CaptureChance => Math.Min(95, 60 + (EnemyHealth * 2 <= EnemyMaxHealth ? 15 : 0));

        public PrototypeBattle(int health, int enemyMaxHealth, int seals,
            int allyMaxHealth = 20, int attackDamage = 5, int attackRupture = 2, int containRupture = 5,
            PrototypeRelicLine line = null, bool isBrasa = true)
        {
            relics = line == null ? new PrototypeRelicLine(new PrototypeRelic[0]) : line.Snapshot();
            brasa = isBrasa;
            SetAlly(health, allyMaxHealth, attackDamage, attackRupture, containRupture);
            EnemyMaxHealth = Math.Max(1, enemyMaxHealth);
            EnemyHealth = EnemyMaxHealth;
            Seals = Math.Max(0, seals);
        }

        public bool Act(PrototypeAction action, bool teamHasRoom, double captureRoll)
        {
            if (Outcome != PrototypeOutcome.Ongoing) return false;
            if (action == PrototypeAction.Capture)
            {
                if (!Exposed) { Message = "Rompe la estabilidad antes de vincular."; return false; }
                if (Seals == 0) { Message = "No quedan sellos. Puedes derrotar al espiritu."; return false; }
                if (!teamHasRoom) { Message = "Equipo lleno. En esta prueba caben cuatro espiritus."; return false; }
                Seals--;
                if (captureRoll < CaptureChance / 100.0)
                {
                    Outcome = PrototypeOutcome.Captured;
                    Message = "Vinculo completado. El espiritu se une al equipo.";
                    return true;
                }
                Message = "El vinculo fallo. Se consumio un sello.";
            }
            else if (action == PrototypeAction.Defend) Message = "Tu espiritu se protege.";
            else
            {
                int damage = 1;
                if (action == PrototypeAction.Attack)
                {
                    damage = relics.Damage(AttackDamage, brasa, EnemyHealth, EnemyMaxHealth, out string calculation);
                    LastCalculation = calculation;
                }
                int rupture = action == PrototypeAction.Attack ? AttackRupture : ContainRupture;
                EnemyHealth = Math.Max(0, EnemyHealth - damage);
                if (Stability > 0)
                {
                    Stability = Math.Max(0, Stability - rupture);
                    if (Stability == 0) exposedThroughRound = Round + 1;
                }
                Message = "Causas " + damage + " de dano y " + rupture + " de ruptura.";
                if (EnemyHealth == 0)
                {
                    Outcome = PrototypeOutcome.Victory;
                    Message += " Espiritu derrotado.";
                    return true;
                }
            }
            ResolveEnemy(action == PrototypeAction.Defend);
            return true;
        }

        public bool SwitchAlly(int health, int maxHealth, int damage, int rupture, int contain, bool isBrasa = true)
        {
            if (Outcome != PrototypeOutcome.Ongoing || health <= 0) return false;
            SetAlly(health, maxHealth, damage, rupture, contain);
            brasa = isBrasa;
            Message = "Cambias de espiritu.";
            ResolveEnemy(false);
            return true;
        }

        public bool ReplaceFallenAlly(int health, int maxHealth, int damage, int rupture, int contain, bool isBrasa = true)
        {
            if (Outcome != PrototypeOutcome.Defeat || health <= 0) return false;
            SetAlly(health, maxHealth, damage, rupture, contain);
            brasa = isBrasa;
            Outcome = PrototypeOutcome.Ongoing;
            Message = "Tu aliado entra al combate. Elige una accion.";
            return true;
        }

        private void SetAlly(int health, int maxHealth, int damage, int rupture, int contain)
        {
            ActiveMaxHealth = Math.Max(1, maxHealth);
            AllyHealth = Math.Max(1, Math.Min(ActiveMaxHealth, health));
            AttackDamage = Math.Max(1, damage);
            AttackRupture = Math.Max(0, rupture);
            ContainRupture = Math.Max(0, contain);
        }

        private void ResolveEnemy(bool defend)
        {
            int incoming = defend ? 2 : 3;
            AllyHealth = Math.Max(0, AllyHealth - incoming);
            Message += " El enemigo causa " + incoming + " de dano.";
            if (AllyHealth == 0) Outcome = PrototypeOutcome.Defeat;
            if (Stability == 0 && Round >= exposedThroughRound)
            {
                Stability = MaxStability / 2;
                Message += " Recupera estabilidad.";
            }
            Round++;
        }
    }
}

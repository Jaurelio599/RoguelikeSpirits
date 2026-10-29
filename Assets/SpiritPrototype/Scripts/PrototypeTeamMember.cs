using System;

namespace SpiritPrototype
{
    public sealed class PrototypeTeamMember
    {
        public string Species { get; }
        public string DisplayName => PrototypeSpeciesCatalog.Name(Species);
        public int MaxHealth { get; }
        public int Health { get; set; }
        public int Damage { get; }
        public int Rupture { get; }
        public int Containment { get; }
        public bool Alive => Health > 0;
        public bool IsBrasa { get; }
        private PrototypeTeamMember(SpiritDefinition definition, int health)
        {
            Species = definition.id; MaxHealth = definition.allyMaxHealth; Damage = definition.allyDamage;
            Rupture = definition.attackRupture; Containment = definition.containment; IsBrasa = definition.usesBrasa;
            Health = Math.Max(1, Math.Min(MaxHealth, health));
        }
        public static PrototypeTeamMember Create(string species, int health)
        {
            var definition = PrototypeContent.Spirit(species);
            if (!definition.canBeAlly) throw new ArgumentException("La especie no admite aliados: " + species);
            return new PrototypeTeamMember(definition, health);
        }
    }
}

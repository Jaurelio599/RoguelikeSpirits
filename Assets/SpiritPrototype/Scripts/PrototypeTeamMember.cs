using System;

namespace SpiritPrototype
{
    public sealed class PrototypeTeamMember
    {
        public string Species { get; }
        public int MaxHealth { get; }
        public int Health { get; set; }
        public int Damage { get; }
        public int Rupture { get; }
        public int Containment { get; }
        public bool Alive => Health > 0;
        public bool IsBrasa => Species == "Ascua" || Species == "Farol";

        private PrototypeTeamMember(string species, int maxHealth, int damage, int rupture, int containment, int health)
        {
            Species = species; MaxHealth = maxHealth;
            Damage = damage; Rupture = rupture; Containment = containment;
            Health = Math.Max(1, Math.Min(maxHealth, health));
        }

        public static PrototypeTeamMember Create(string species, int health)
        {
            switch (species)
            {
                case "Velo": return new PrototypeTeamMember(species, 18, 4, 3, 6, health);
                case "Raicero": return new PrototypeTeamMember(species, 26, 4, 2, 4, health);
                case "Campanilla": return new PrototypeTeamMember(species, 18, 3, 2, 5, health);
                case "Luto": return new PrototypeTeamMember(species, 18, 7, 1, 4, health);
                case "Farol": return new PrototypeTeamMember(species, 20, 4, 3, 5, health);
                default: return new PrototypeTeamMember("Ascua", 20, 5, 2, 5, health);
            }
        }
    }
}

using UnityEngine;

namespace SpiritPrototype
{
    // Bootstrap only: after menu 8 the assets, not this table, own the editable data.
    public static class PrototypeContentDefaults
    {
        public static readonly string[] CoreSpecies = {"Ascua", "Velo", "Raicero", "Campanilla", "Luto", "Farol", "Guardian del Umbral"};
        public static SpiritDefinition Spirit(string id)
        {
            var s = ScriptableObject.CreateInstance<SpiritDefinition>(); s.id = id; s.displayName = id;
            switch (id)
            {
                case "Velo": s.allyMaxHealth = 18; s.allyDamage = 4; s.attackRupture = 3; s.containment = 6; break;
                case "Raicero": s.allyMaxHealth = 26; s.allyDamage = 4; s.attackRupture = 2; s.containment = 4; break;
                case "Campanilla": s.allyMaxHealth = 18; s.allyDamage = 3; s.attackRupture = 2; s.containment = 5; break;
                case "Luto": s.allyMaxHealth = 18; s.allyDamage = 7; s.attackRupture = 1; s.containment = 4; break;
                case "Farol": s.allyMaxHealth = 20; s.allyDamage = 4; s.attackRupture = 3; s.containment = 5; s.usesBrasa = true; break;
                case "Guardian del Umbral": s.canBeAlly = false; s.capturable = false; s.spawnInDungeon = false;
                    s.tint = new Color(1f, .65f, .65f); s.enemyMaxHealth = 32; s.enemyAttack = 4; s.enemyStability = 20; s.mapSize = 1.2f; s.rarity = "Jefe (provisional)"; break;
                default: s.usesBrasa = true; s.spawnInDungeon = false; s.enemyMaxHealth = 20; s.enemyAttack = 5; break;
            }
            return s;
        }
        public static RelicDefinition Relic(int id)
        {
            var r = ScriptableObject.CreateInstance<RelicDefinition>(); r.id = id;
            switch (id)
            {
                case 0: r.displayName = "Carbon tallado"; r.effect = RelicEffectKind.AddAttack; r.condition = RelicCondition.BrasaOnly; r.amount = 4; break;
                case 1: r.displayName = "Lente voraz"; r.effect = RelicEffectKind.MultiplyAttack; r.condition = RelicCondition.EnemyBelowHalf; r.amount = 1.5f; break;
                case 2: r.displayName = "Prisma vecino"; r.effect = RelicEffectKind.AmplifyNextAddition; r.targetRelicId = 0; r.amount = 2; break;
                case 3: r.displayName = "Colmillo pulido"; r.effect = RelicEffectKind.AddAttack; r.amount = 2; break;
                case 4: r.displayName = "Piedra refugio"; r.effect = RelicEffectKind.ReduceIncomingDamage; r.amount = 1; break;
                case 5: r.displayName = "Nudo de vinculo"; r.effect = RelicEffectKind.AddContainment; r.amount = 2; break;
                default: r.displayName = "Medallon del alba"; r.effect = RelicEffectKind.AddAttack; r.condition = RelicCondition.EnemyFullHealth; r.amount = 3; break;
            }
            return r;
        }
        public static readonly string[] TrialKeys = {"mecha", "campana", "sello", "bastion", "ultimo_farol", "cuerda", "moneda"};
        public static readonly string[] TrialIcons = {"MechaErrante", "CampanaCeniza", "SelloAgrietado", "BastionAntiguo", "UltimoFarol", "CuerdaRelevo", "MonedaDuelo"};
        public static RelicDefinition TrialRelic(int index, int id)
        {
            var r = ScriptableObject.CreateInstance<RelicDefinition>(); r.id = id; r.prototypeKey = "prototype." + TrialKeys[index];
            switch (index)
            {
                case 0: r.displayName = "Mecha errante"; r.effect = RelicEffectKind.BurnOnAttack; r.condition = RelicCondition.BrasaOnly; r.amount = 1; break;
                case 1: r.displayName = "Campana de ceniza"; r.effect = RelicEffectKind.RuptureBurnedAfterAttack; r.condition = RelicCondition.BrasaOnly; r.amount = 3; break;
                case 2: r.displayName = "Sello agrietado"; r.effect = RelicEffectKind.CaptureBonus; r.amount = 10; break;
                case 3: r.displayName = "Bastion antiguo"; r.effect = RelicEffectKind.OpeningShield; r.amount = 4; break;
                case 4: r.displayName = "Ultimo farol"; r.effect = RelicEffectKind.EndRoundHeal; r.amount = 2; break;
                case 5: r.displayName = "Cuerda de relevo"; r.effect = RelicEffectKind.SwitchShield; r.amount = 4; break;
                default: r.displayName = "Moneda de duelo"; r.effect = RelicEffectKind.VictoryEssence; r.amount = 3; break;
            }
            return r;
        }
        public static SpiritContentCatalog CreateCatalog()
        {
            var c = ScriptableObject.CreateInstance<SpiritContentCatalog>();
            c.spirits = new SpiritDefinition[CoreSpecies.Length]; c.relics = new RelicDefinition[7];
            for (int i = 0; i < c.spirits.Length; i++) c.spirits[i] = Spirit(CoreSpecies[i]);
            for (int i = 0; i < c.relics.Length; i++) c.relics[i] = Relic(i);
            return c;
        }
    }
}

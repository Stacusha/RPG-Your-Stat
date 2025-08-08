using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace RPGYourStat
{
    public static class StatModifierSystem
    {
        // Mapping des stats RPG vers les StatDefs de RimWorld avec leurs bonus par niveau
        private static readonly Dictionary<StatType, Dictionary<StatDef, float>> StatBonusMapping = 
            new Dictionary<StatType, Dictionary<StatDef, float>>
            {
                [StatType.STR] = new Dictionary<StatDef, float>
                {
                    { StatDefOf.WorkSpeedGlobal, 0.02f },            // +2% par niveau
                    { StatDefOf.ConstructionSpeed, 0.03f },          // +3% par niveau
                    { StatDefOf.MiningSpeed, 0.03f },                // +3% par niveau
                    { StatDefOf.MiningYield, 0.02f },                // +2% par niveau
                    { StatDefOf.ConstructSuccessChance, 0.01f },     // +1% par niveau
                    { StatDefOf.SmoothingSpeed, 0.03f },             // +3% par niveau
                    { StatDefOf.MeleeDamageFactor, 0.025f },         // +2.5% par niveau
                    { StatDefOf.CarryingCapacity, 0.05f },           // +5% par niveau
                    { StatDefOf.PlantWorkSpeed, 0.025f },            // +2.5% par niveau
                    { StatDefOf.DeepDrillingSpeed, 0.03f }           // +3% par niveau
                },
                
                [StatType.DEX] = new Dictionary<StatDef, float>
                {
                    { StatDefOf.ShootingAccuracyPawn, 0.02f },       // +2% par niveau
                    { StatDefOf.MeleeHitChance, 0.02f },             // +2% par niveau
                    { StatDefOf.MedicalTendSpeed, 0.025f },          // +2.5% par niveau
                    { StatDefOf.SurgerySuccessChanceFactor, 0.015f }, // +1.5% par niveau
                    { StatDefOf.FoodPoisonChance, -0.01f }           // -1% par niveau (réduction)
                },
                
                [StatType.AGL] = new Dictionary<StatDef, float>
                {
                    { StatDefOf.MoveSpeed, 0.03f },                  // +3% par niveau
                    { StatDefOf.MeleeDodgeChance, 0.02f },           // +2% par niveau
                    { StatDefOf.AimingDelayFactor, -0.015f },        // -1.5% par niveau (négatif = mieux)
                    { StatDefOf.HuntingStealth, 0.02f },             // +2% par niveau
                    { StatDefOf.RestRateMultiplier, 0.02f },         // +2% par niveau
                    { StatDefOf.MentalBreakThreshold, -0.01f },      // -1% par niveau (négatif = mieux)
                    { StatDefOf.PlantHarvestYield, 0.02f },          // +2% par niveau
                    { StatDefOf.FilthRate, -0.03f },                 // -3% par niveau (négatif = mieux)
                    { StatDefOf.EatingSpeed, 0.03f }                 // +3% par niveau
                },
                
                [StatType.CON] = new Dictionary<StatDef, float>
                {
                    { StatDefOf.ImmunityGainSpeed, 0.03f },          // +3% par niveau
                    { StatDefOf.ComfyTemperatureMin, -0.1f },        // -10%°C par niveau (résistance froid)
                    { StatDefOf.ComfyTemperatureMax, 0.1f },         // +10%°C par niveau (résistance chaud)
                    { StatDefOf.ToxicResistance, 0.02f },            // +2% par niveau
                    { StatDefOf.PainShockThreshold, 0.03f }          // +3% par niveau
                },
                
                [StatType.INT] = new Dictionary<StatDef, float>
                {
                    { StatDefOf.ResearchSpeed, 0.04f },              // +4% par niveau
                    { StatDefOf.GlobalLearningFactor, 0.03f },       // +3% par niveau
                    { StatDefOf.MedicalTendQuality, 0.025f },        // +2.5% par niveau
                    { StatDefOf.MedicalSurgerySuccessChance, 0.02f }, // +2% par niveau
                    { StatDefOf.TrapSpringChance, -0.02f },          // -2% par niveau (négatif = mieux)
                    { StatDefOf.NegotiationAbility, 0.02f },         // +2% par niveau
                    { StatDefOf.PsychicSensitivity, 0.015f }         // +1.5% par niveau
                },
                
                [StatType.CHA] = new Dictionary<StatDef, float>
                {
                    { StatDefOf.SocialImpact, 0.04f },               // +4% par niveau
                    { StatDefOf.TradePriceImprovement, 0.02f },      // +2% par niveau
                    { StatDefOf.TameAnimalChance, 0.03f },           // +3% par niveau
                    { StatDefOf.TrainAnimalChance, 0.025f },         // +2.5% par niveau
                    { StatDefOf.AnimalGatherYield, 0.02f },          // +2% par niveau
                    { StatDefOf.Beauty, 0.02f },                     // +2% par niveau
                    { StatDefOf.ArrestSuccessChance, 0.02f }         // +2% par niveau pour arrêter
                }
            };

        public static float GetStatModifier(Pawn pawn, StatDef stat)
        {
            var comp = pawn.GetComp<CompRPGStats>();
            if (comp == null) return 0f;

            float totalModifier = 0f;

            foreach (var statTypeMapping in StatBonusMapping)
            {
                StatType statType = statTypeMapping.Key;
                var bonusMapping = statTypeMapping.Value;

                if (bonusMapping.ContainsKey(stat))
                {
                    int level = comp.GetStatLevel(statType);
                    float bonusPerLevel = bonusMapping[stat];
                    
                    // Calculer le bonus total pour cette stat RPG
                    // (niveau - 1) car le niveau 1 ne donne pas de bonus
                    float statBonus = bonusPerLevel * (level - 1);
                    totalModifier += statBonus;
                }
            }

            return totalModifier;
        }

        // Méthode pour obtenir une description des bonus pour l'interface
        public static string GetStatBonusDescription(Pawn pawn, StatType statType)
        {
            var comp = pawn.GetComp<CompRPGStats>();
            if (comp == null) return "";

            int level = comp.GetStatLevel(statType);
            if (level <= 1) return "Aucun bonus";

            var bonuses = new List<string>();
            
            if (StatBonusMapping.ContainsKey(statType))
            {
                foreach (var bonus in StatBonusMapping[statType])
                {
                    StatDef statDef = bonus.Key;
                    float bonusPerLevel = bonus.Value;
                    float totalBonus = bonusPerLevel * (level - 1);
                    
                    string sign = totalBonus >= 0 ? "+" : "";
                    string percentage = (totalBonus * 100f).ToString("F1");
                    
                    bonuses.Add($"{statDef.label}: {sign}{percentage}%");
                }
            }

            return string.Join("\n", bonuses);
        }

        // Méthode pour obtenir les stats affectées par un type de stat RPG
        public static List<StatDef> GetAffectedStats(StatType statType)
        {
            if (StatBonusMapping.ContainsKey(statType))
            {
                return StatBonusMapping[statType].Keys.ToList();
            }
            return new List<StatDef>();
        }
    }
}
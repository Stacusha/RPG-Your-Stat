using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace RPGYourStat
{
    public static class AnimalStatModifierSystem
    {
        // Mapping des stats RPG vers les StatDefs spécialement pour les animaux
        private static readonly Dictionary<StatType, Dictionary<StatDef, string>> AnimalStatBonusMapping = 
            new Dictionary<StatType, Dictionary<StatDef, string>>
            {
                [StatType.STR] = new Dictionary<StatDef, string>
                {
                    // FORCE pour animaux : Capacité de charge, dégâts, vitesse de travail
                    { StatDefOf.CarryingCapacity, "ANIMAL_STR_CarryingCapacity" },
                    { StatDefOf.MeleeDamageFactor, "ANIMAL_STR_MeleeDamageFactor" },
                    { StatDefOf.WorkSpeedGlobal, "ANIMAL_STR_WorkSpeedGlobal" },
                    { StatDefOf.MiningSpeed, "ANIMAL_STR_MiningSpeed" },       // Pour animaux de minage
                    { StatDefOf.MiningYield, "ANIMAL_STR_MiningYield" }
                },
                
                [StatType.DEX] = new Dictionary<StatDef, string>
                {
                    // DEXTÉRITÉ pour animaux : Précision des attaques, manipulation
                    { StatDefOf.MeleeHitChance, "ANIMAL_DEX_MeleeHitChance" },
                    { StatDefOf.ShootingAccuracyPawn, "ANIMAL_DEX_ShootingAccuracyPawn" },  // Pour animaux dressés au tir
                    { StatDefOf.WorkSpeedGlobal, "ANIMAL_DEX_WorkSpeedGlobal" }
                },
                
                [StatType.AGL] = new Dictionary<StatDef, string>
                {
                    // AGILITÉ pour animaux : Vitesse, esquive, discrétion
                    { StatDefOf.MoveSpeed, "ANIMAL_AGL_MoveSpeed" },
                    { StatDefOf.MeleeDodgeChance, "ANIMAL_AGL_MeleeDodgeChance" },
                    { StatDefOf.HuntingStealth, "ANIMAL_AGL_HuntingStealth" },
                    { StatDefOf.AimingDelayFactor, "ANIMAL_AGL_AimingDelayFactor" },
                    { StatDefOf.FilthRate, "ANIMAL_AGL_FilthRate" }              // Animaux agiles salissent moins
                },
                
                [StatType.CON] = new Dictionary<StatDef, string>
                {
                    // CONSTITUTION pour animaux : Santé, résistance, récupération
                    { StatDefOf.ImmunityGainSpeed, "ANIMAL_CON_ImmunityGainSpeed" },
                    { StatDefOf.RestRateMultiplier, "ANIMAL_CON_RestRateMultiplier" },
                    { StatDefOf.ComfyTemperatureMin, "ANIMAL_CON_ComfyTemperatureMin" },
                    { StatDefOf.ComfyTemperatureMax, "ANIMAL_CON_ComfyTemperatureMax" },
                    { StatDefOf.ToxicResistance, "ANIMAL_CON_ToxicResistance" },
                    { StatDefOf.PainShockThreshold, "ANIMAL_CON_PainShockThreshold" },
                    { StatDefOf.FoodPoisonChance, "ANIMAL_CON_FoodPoisonChance" },
                    { StatDefOf.CarryingCapacity, "ANIMAL_CON_CarryingCapacity" }    // Endurance physique
                },
                
                [StatType.INT] = new Dictionary<StatDef, string>
                {
                    // INTELLIGENCE pour animaux : Apprentissage, dressage
                    { StatDefOf.GlobalLearningFactor, "ANIMAL_INT_GlobalLearningFactor" },
                    { StatDefOf.TrapSpringChance, "ANIMAL_INT_TrapSpringChance" },
                    { StatDefOf.HuntingStealth, "ANIMAL_INT_HuntingStealth" },      // Intelligence de chasse
                    { StatDefOf.WorkSpeedGlobal, "ANIMAL_INT_WorkSpeedGlobal" }      // Efficacité des tâches apprises
                },
                
                [StatType.CHA] = new Dictionary<StatDef, string>
                {
                    // CHARISME pour animaux : Relations sociales, dressage
                    { StatDefOf.SocialImpact, "ANIMAL_CHA_SocialImpact" },
                    { StatDefOf.TameAnimalChance, "ANIMAL_CHA_TameAnimalChance" },   // Animaux charismatiques aident au dressage
                    { StatDefOf.TrainAnimalChance, "ANIMAL_CHA_TrainAnimalChance" },
                    { StatDefOf.Beauty, "ANIMAL_CHA_Beauty" },                       // Animaux plus beaux
                    { StatDefOf.AnimalGatherYield, "ANIMAL_CHA_AnimalGatherYield" }  // Meilleure production
                }
            };

        // Valeurs par défaut pour les animaux (généralement plus faibles que les humains)
        private static readonly Dictionary<string, float> AnimalDefaultValues = new Dictionary<string, float>
        {
            // STR - FORCE pour animaux
            ["ANIMAL_STR_CarryingCapacity"] = 0.08f,        // +8% par niveau (plus que humains)
            ["ANIMAL_STR_MeleeDamageFactor"] = 0.04f,       // +4% par niveau (plus que humains)
            ["ANIMAL_STR_WorkSpeedGlobal"] = 0.03f,         // +3% par niveau
            ["ANIMAL_STR_MiningSpeed"] = 0.05f,             // +5% par niveau (pour animaux mineurs)
            ["ANIMAL_STR_MiningYield"] = 0.03f,             // +3% par niveau
            
            // DEX - DEXTÉRITÉ pour animaux
            ["ANIMAL_DEX_MeleeHitChance"] = 0.03f,          // +3% par niveau
            ["ANIMAL_DEX_ShootingAccuracyPawn"] = 0.015f,   // +1.5% par niveau (rare)
            ["ANIMAL_DEX_WorkSpeedGlobal"] = 0.025f,        // +2.5% par niveau
            
            // AGL - AGILITÉ pour animaux (leur point fort)
            ["ANIMAL_AGL_MoveSpeed"] = 0.05f,               // +5% par niveau (plus que humains)
            ["ANIMAL_AGL_MeleeDodgeChance"] = 0.04f,        // +4% par niveau (plus que humains)
            ["ANIMAL_AGL_HuntingStealth"] = 0.04f,          // +4% par niveau
            ["ANIMAL_AGL_AimingDelayFactor"] = -0.025f,     // -2.5% par niveau
            ["ANIMAL_AGL_FilthRate"] = -0.04f,              // -4% par niveau
            
            // CON - CONSTITUTION pour animaux (leur autre point fort)
            ["ANIMAL_CON_ImmunityGainSpeed"] = 0.05f,       // +5% par niveau (plus que humains)
            ["ANIMAL_CON_RestRateMultiplier"] = 0.04f,      // +4% par niveau
            ["ANIMAL_CON_ComfyTemperatureMin"] = -0.15f,    // -15% par niveau (plus que humains)
            ["ANIMAL_CON_ComfyTemperatureMax"] = 0.15f,     // +15% par niveau (plus que humains)
            ["ANIMAL_CON_ToxicResistance"] = 0.03f,         // +3% par niveau
            ["ANIMAL_CON_PainShockThreshold"] = 0.05f,      // +5% par niveau (plus que humains)
            ["ANIMAL_CON_FoodPoisonChance"] = -0.02f,       // -2% par niveau
            ["ANIMAL_CON_CarryingCapacity"] = 0.04f,        // +4% par niveau (endurance)
            
            // INT - INTELLIGENCE pour animaux (leur point faible)
            ["ANIMAL_INT_GlobalLearningFactor"] = 0.02f,    // +2% par niveau (moins que humains)
            ["ANIMAL_INT_TrapSpringChance"] = -0.015f,      // -1.5% par niveau (moins que humains)
            ["ANIMAL_INT_HuntingStealth"] = 0.025f,         // +2.5% par niveau
            ["ANIMAL_INT_WorkSpeedGlobal"] = 0.015f,        // +1.5% par niveau (moins que humains)
            
            // CHA - CHARISME pour animaux
            ["ANIMAL_CHA_SocialImpact"] = 0.03f,            // +3% par niveau
            ["ANIMAL_CHA_TameAnimalChance"] = 0.04f,        // +4% par niveau
            ["ANIMAL_CHA_TrainAnimalChance"] = 0.035f,      // +3.5% par niveau
            ["ANIMAL_CHA_Beauty"] = 0.03f,                  // +3% par niveau
            ["ANIMAL_CHA_AnimalGatherYield"] = 0.04f        // +4% par niveau
        };

        public static float GetAnimalStatModifier(Pawn animal, StatDef stat)
        {
            if (!animal.RaceProps.Animal) return 0f;
            
            var comp = animal.GetComp<CompRPGStats>();
            if (comp == null) return 0f;

            float totalModifier = 0f;

            foreach (var statTypeMapping in AnimalStatBonusMapping)
            {
                StatType statType = statTypeMapping.Key;
                var bonusMapping = statTypeMapping.Value;

                if (bonusMapping.ContainsKey(stat))
                {
                    int level = comp.GetStatLevel(statType);
                    string multiplierKey = bonusMapping[stat];
                    
                    float bonusPerLevel = GetAnimalMultiplierValue(multiplierKey);
                    float statBonus = bonusPerLevel * (level - 1);
                    totalModifier += statBonus;
                }
            }

            return totalModifier;
        }

        private static float GetAnimalMultiplierValue(string key)
        {
            if (RPGYourStat_Mod.settings?.statMultipliers != null && 
                RPGYourStat_Mod.settings.statMultipliers.ContainsKey(key))
            {
                return RPGYourStat_Mod.settings.statMultipliers[key];
            }
            
            return AnimalDefaultValues.TryGetValue(key, out float defaultValue) ? defaultValue : 0f;
        }

        public static string GetAnimalStatBonusDescription(Pawn animal, StatType statType)
        {
            var comp = animal.GetComp<CompRPGStats>();
            if (comp == null) return "";

            int level = comp.GetStatLevel(statType);
            if (level <= 1) return "Aucun bonus";

            var bonuses = new List<string>();
            
            if (AnimalStatBonusMapping.ContainsKey(statType))
            {
                foreach (var bonus in AnimalStatBonusMapping[statType])
                {
                    StatDef statDef = bonus.Key;
                    string multiplierKey = bonus.Value;
                    float bonusPerLevel = GetAnimalMultiplierValue(multiplierKey);
                    float totalBonus = bonusPerLevel * (level - 1);
                    
                    string sign = totalBonus >= 0 ? "+" : "";
                    string percentage = (totalBonus * 100f).ToString("F1");
                    
                    bonuses.Add($"{statDef.label}: {sign}{percentage}%");
                }
            }

            return string.Join("\n", bonuses);
        }

        public static List<StatDef> GetAnimalAffectedStats(StatType statType)
        {
            if (AnimalStatBonusMapping.ContainsKey(statType))
            {
                return AnimalStatBonusMapping[statType].Keys.ToList();
            }
            return new List<StatDef>();
        }

        public static Dictionary<StatDef, float> GetAnimalStatBonusMapping(StatType statType)
        {
            var result = new Dictionary<StatDef, float>();
            
            if (AnimalStatBonusMapping.ContainsKey(statType))
            {
                foreach (var kvp in AnimalStatBonusMapping[statType])
                {
                    StatDef statDef = kvp.Key;
                    string multiplierKey = kvp.Value;
                    float multiplierValue = GetAnimalMultiplierValue(multiplierKey);
                    
                    result[statDef] = multiplierValue;
                }
            }
            
            return result;
        }
    }
}
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace RPGYourStat
{
    public static class StatModifierSystem
    {
        // Mapping des stats RPG vers les StatDefs de RimWorld avec leurs clés de multiplicateur
        private static readonly Dictionary<StatType, Dictionary<StatDef, string>> StatBonusMapping = 
            new Dictionary<StatType, Dictionary<StatDef, string>>
            {
                [StatType.STR] = new Dictionary<StatDef, string>
                {
                    // FORCE : Travaux physiques, force brute, porter des charges
                    { StatDefOf.WorkSpeedGlobal, "STR_WorkSpeedGlobal" },
                    { StatDefOf.ConstructionSpeed, "STR_ConstructionSpeed" },
                    { StatDefOf.MiningSpeed, "STR_MiningSpeed" },
                    { StatDefOf.MiningYield, "STR_MiningYield" },
                    { StatDefOf.ConstructSuccessChance, "STR_ConstructSuccessChance" },
                    { StatDefOf.SmoothingSpeed, "STR_SmoothingSpeed" },
                    { StatDefOf.MeleeDamageFactor, "STR_MeleeDamageFactor" },
                    { StatDefOf.CarryingCapacity, "STR_CarryingCapacity" },
                    { StatDefOf.PlantWorkSpeed, "STR_PlantWorkSpeed" },
                    { StatDefOf.DeepDrillingSpeed, "STR_DeepDrillingSpeed" }
                },
                
                [StatType.DEX] = new Dictionary<StatDef, string>
                {
                    // DEXTÉRITÉ : Précision, manipulation fine, coordination main-œil
                    { StatDefOf.ShootingAccuracyPawn, "DEX_ShootingAccuracyPawn" },
                    { StatDefOf.MeleeHitChance, "DEX_MeleeHitChance" },
                    { StatDefOf.WorkSpeedGlobal, "DEX_WorkSpeedGlobal" },
                    { StatDefOf.MedicalTendSpeed, "DEX_MedicalTendSpeed" },
                    { StatDefOf.MedicalTendQuality, "DEX_MedicalTendQuality" },
                    { StatDefOf.SurgerySuccessChanceFactor, "DEX_SurgerySuccessChanceFactor" },
                    { StatDefOf.FoodPoisonChance, "DEX_FoodPoisonChance" }
                },
                
                [StatType.AGL] = new Dictionary<StatDef, string>
                {
                    // AGILITÉ : Vitesse, esquive, réflexes, mobilité
                    { StatDefOf.MoveSpeed, "AGL_MoveSpeed" },
                    { StatDefOf.MeleeDodgeChance, "AGL_MeleeDodgeChance" },
                    { StatDefOf.AimingDelayFactor, "AGL_AimingDelayFactor" },
                    { StatDefOf.HuntingStealth, "AGL_HuntingStealth" },
                    { StatDefOf.RestRateMultiplier, "AGL_RestRateMultiplier" },
                    { StatDefOf.PlantHarvestYield, "AGL_PlantHarvestYield" },
                    { StatDefOf.FilthRate, "AGL_FilthRate" },
                    { StatDefOf.EatingSpeed, "AGL_EatingSpeed" }
                },
                
                [StatType.CON] = new Dictionary<StatDef, string>
                {
                    // CONSTITUTION : Endurance, résistance, santé, récupération
                    { StatDefOf.CarryingCapacity, "CON_CarryingCapacity" },
                    { StatDefOf.WorkSpeedGlobal, "CON_WorkSpeedGlobal" },
                    { StatDefOf.ImmunityGainSpeed, "CON_ImmunityGainSpeed" },
                    { StatDefOf.MentalBreakThreshold, "CON_MentalBreakThreshold" },
                    { StatDefOf.RestRateMultiplier, "CON_RestRateMultiplier" },
                    { StatDefOf.ComfyTemperatureMin, "CON_ComfyTemperatureMin" },
                    { StatDefOf.ComfyTemperatureMax, "CON_ComfyTemperatureMax" },
                    { StatDefOf.ToxicResistance, "CON_ToxicResistance" },
                    { StatDefOf.FoodPoisonChance, "CON_FoodPoisonChance" },
                    { StatDefOf.PainShockThreshold, "CON_PainShockThreshold" }
                },
                
                [StatType.INT] = new Dictionary<StatDef, string>
                {
                    // INTELLIGENCE : Recherche, apprentissage, résolution de problèmes
                    { StatDefOf.ResearchSpeed, "INT_ResearchSpeed" },
                    { StatDefOf.GlobalLearningFactor, "INT_GlobalLearningFactor" },
                    { StatDefOf.MedicalTendQuality, "INT_MedicalTendQuality" },
                    { StatDefOf.MedicalSurgerySuccessChance, "INT_MedicalSurgerySuccessChance" },
                    { StatDefOf.PlantWorkSpeed, "INT_PlantWorkSpeed" },
                    { StatDefOf.TrapSpringChance, "INT_TrapSpringChance" },
                    { StatDefOf.NegotiationAbility, "INT_NegotiationAbility" },
                    { StatDefOf.PsychicSensitivity, "INT_PsychicSensitivity" }
                },
                
                [StatType.CHA] = new Dictionary<StatDef, string>
                {
                    // CHARISME : Relations sociales, négociation, leadership, beauté
                    { StatDefOf.SocialImpact, "CHA_SocialImpact" },
                    { StatDefOf.NegotiationAbility, "CHA_NegotiationAbility" },
                    { StatDefOf.TradePriceImprovement, "CHA_TradePriceImprovement" },
                    { StatDefOf.TameAnimalChance, "CHA_TameAnimalChance" },
                    { StatDefOf.TrainAnimalChance, "CHA_TrainAnimalChance" },
                    { StatDefOf.AnimalGatherYield, "CHA_AnimalGatherYield" },
                    { StatDefOf.Beauty, "CHA_Beauty" },
                    { StatDefOf.ArrestSuccessChance, "CHA_ArrestSuccessChance" },
                    { StatDefOf.MentalBreakThreshold, "CHA_MentalBreakThreshold" }
                }
            };

        // Valeurs par défaut mises à jour avec les stats existantes
        private static readonly Dictionary<string, float> DefaultValues = new Dictionary<string, float>
        {
            // STR - FORCE
            ["STR_WorkSpeedGlobal"] = 0.02f,
            ["STR_ConstructionSpeed"] = 0.03f,
            ["STR_MiningSpeed"] = 0.03f,
            ["STR_MiningYield"] = 0.02f,
            ["STR_ConstructSuccessChance"] = 0.01f,
            ["STR_SmoothingSpeed"] = 0.03f,
            ["STR_MeleeDamageFactor"] = 0.025f,
            ["STR_CarryingCapacity"] = 0.05f,
            ["STR_PlantWorkSpeed"] = 0.025f,
            ["STR_DeepDrillingSpeed"] = 0.03f,
            
            // DEX - DEXTÉRITÉ
            ["DEX_ShootingAccuracyPawn"] = 0.02f,
            ["DEX_MeleeHitChance"] = 0.02f,
            ["DEX_WorkSpeedGlobal"] = 0.02f,
            ["DEX_MedicalTendSpeed"] = 0.025f,
            ["DEX_MedicalTendQuality"] = 0.02f,
            ["DEX_SurgerySuccessChanceFactor"] = 0.015f,
            ["DEX_FoodPoisonChance"] = -0.01f,
            
            // AGL - AGILITÉ
            ["AGL_MoveSpeed"] = 0.03f,
            ["AGL_MeleeDodgeChance"] = 0.02f,
            ["AGL_AimingDelayFactor"] = -0.015f,
            ["AGL_HuntingStealth"] = 0.02f,
            ["AGL_RestRateMultiplier"] = 0.02f,
            ["AGL_PlantHarvestYield"] = 0.02f,
            ["AGL_FilthRate"] = -0.03f,
            ["AGL_EatingSpeed"] = 0.03f,
            
            // CON - CONSTITUTION
            ["CON_CarryingCapacity"] = 0.03f,
            ["CON_WorkSpeedGlobal"] = 0.015f,
            ["CON_ImmunityGainSpeed"] = 0.03f,
            ["CON_MentalBreakThreshold"] = -0.01f,
            ["CON_RestRateMultiplier"] = 0.025f,
            ["CON_ComfyTemperatureMin"] = -0.1f,
            ["CON_ComfyTemperatureMax"] = 0.1f,
            ["CON_ToxicResistance"] = 0.02f,
            ["CON_FoodPoisonChance"] = -0.015f,
            ["CON_PainShockThreshold"] = 0.03f,
            
            // INT - INTELLIGENCE
            ["INT_ResearchSpeed"] = 0.04f,
            ["INT_GlobalLearningFactor"] = 0.03f,
            ["INT_MedicalTendQuality"] = 0.025f,
            ["INT_MedicalSurgerySuccessChance"] = 0.02f,
            ["INT_PlantWorkSpeed"] = 0.02f,
            ["INT_TrapSpringChance"] = -0.02f,
            ["INT_NegotiationAbility"] = 0.015f,
            ["INT_PsychicSensitivity"] = 0.015f,
            
            // CHA - CHARISME
            ["CHA_SocialImpact"] = 0.04f,
            ["CHA_NegotiationAbility"] = 0.025f,
            ["CHA_TradePriceImprovement"] = 0.02f,
            ["CHA_TameAnimalChance"] = 0.03f,
            ["CHA_TrainAnimalChance"] = 0.025f,
            ["CHA_AnimalGatherYield"] = 0.02f,
            ["CHA_Beauty"] = 0.02f,
            ["CHA_ArrestSuccessChance"] = 0.02f,
            ["CHA_MentalBreakThreshold"] = -0.015f
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
                    string multiplierKey = bonusMapping[stat];
                    
                    float bonusPerLevel = GetMultiplierValue(multiplierKey);
                    float statBonus = bonusPerLevel * (level - 1);
                    totalModifier += statBonus;
                }
            }

            return totalModifier;
        }

        private static float GetMultiplierValue(string key)
        {
            if (RPGYourStat_Mod.settings?.statMultipliers != null && 
                RPGYourStat_Mod.settings.statMultipliers.ContainsKey(key))
            {
                return RPGYourStat_Mod.settings.statMultipliers[key];
            }
            
            return DefaultValues.TryGetValue(key, out float defaultValue) ? defaultValue : 0f;
        }

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
                    string multiplierKey = bonus.Value;
                    float bonusPerLevel = GetMultiplierValue(multiplierKey);
                    float totalBonus = bonusPerLevel * (level - 1);
                    
                    string sign = totalBonus >= 0 ? "+" : "";
                    string percentage = (totalBonus * 100f).ToString("F1");
                    
                    bonuses.Add($"{statDef.label}: {sign}{percentage}%");
                }
            }

            return string.Join("\n", bonuses);
        }

        public static List<StatDef> GetAffectedStats(StatType statType)
        {
            if (StatBonusMapping.ContainsKey(statType))
            {
                return StatBonusMapping[statType].Keys.ToList();
            }
            return new List<StatDef>();
        }

        public static Dictionary<StatDef, float> GetStatBonusMapping(StatType statType)
        {
            var result = new Dictionary<StatDef, float>();
            
            if (StatBonusMapping.ContainsKey(statType))
            {
                foreach (var kvp in StatBonusMapping[statType])
                {
                    StatDef statDef = kvp.Key;
                    string multiplierKey = kvp.Value;
                    float multiplierValue = GetMultiplierValue(multiplierKey);
                    
                    result[statDef] = multiplierValue;
                }
            }
            
            return result;
        }
    }
}
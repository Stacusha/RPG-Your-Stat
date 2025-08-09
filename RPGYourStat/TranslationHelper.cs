using Verse;
using RimWorld; // AJOUTÉ : Pour StatDef et DefDatabase

namespace RPGYourStat
{
    public static class TranslationHelper
    {
        // Méthodes pour les noms de stats
        public static string GetStatDisplayName(StatType statType)
        {
            string key = $"RPGStats.StatName.{statType}";
            return key.Translate();
        }
        
        public static string GetAnimalStatGroupName(StatType statType)
        {
            string key = $"RPGStats.StatGroup.Animal{statType}";
            return key.Translate();
        }
        
        // Méthodes pour l'interface utilisateur
        public static string GetUIText(string key)
        {
            return $"RPGStats.UI.{key}".Translate();
        }
        
        public static string GetSettingsText(string key)
        {
            return $"RPGStats.Settings.{key}".Translate();
        }
        
        public static string GetTooltipText(string key)
        {
            return $"RPGStats.Tooltip.{key}".Translate();
        }
        
        public static string GetActivityText(string key)
        {
            return $"RPGStats.Activity.{key}".Translate();
        }
        
        public static string GetRankingText(int rank)
        {
            string key = rank switch
            {
                1 => "RPGStats.Ranking.First",
                2 => "RPGStats.Ranking.Second", 
                3 => "RPGStats.Ranking.Third",
                4 => "RPGStats.Ranking.Fourth",
                5 => "RPGStats.Ranking.Fifth",
                6 => "RPGStats.Ranking.Sixth",
                _ => "RPGStats.Ranking.Other"
            };
            
            if (rank <= 6)
            {
                return key.Translate();
            }
            else
            {
                return key.Translate(rank);
            }
        }
        
        // Méthodes pour les messages
        public static string GetMessageText(string key)
        {
            return $"RPGStats.Message.{key}".Translate();
        }
        
        public static string GetBalanceText(string key)
        {
            return $"RPGStats.Balance.{key}".Translate();
        }
        
        // Méthode pour les messages de level up
        public static string GetLevelUpMessage(string pawnName, string statName, int newLevel)
        {
            return "RPGStats.Message.LevelUp".Translate(pawnName, statName, newLevel);
        }
        
        // Méthode pour les tooltips avec paramètres
        public static string GetStatImprovementsTooltip(string statName, string improvements)
        {
            return "RPGStats.Tooltip.StatImprovements".Translate(statName, improvements);
        }
        
        // Méthode pour les activités avec paramètres
        public static string GetHaulingActivity(string itemName)
        {
            return "RPGStats.Activity.Hauling".Translate(itemName);
        }
        
        // NOUVEAU : Méthode pour les noms de statistiques avec traduction selon la langue
        public static string GetStatName(string statKey)
        {
            // Utiliser directement le nom anglais du StatDef car RimWorld gère automatiquement les traductions
            var statDef = DefDatabase<StatDef>.GetNamedSilentFail(statKey);
            if (statDef != null)
            {
                return statDef.label.CapitalizeFirst();
            }
            
            // Fallback si le StatDef n'est pas trouvé
            string translationKey = $"RPGStats.StatNames.{statKey}";
            if (translationKey.CanTranslate())
            {
                return translationKey.Translate();
            }
            
            // Dernier fallback avec traductions manuelles
            return GetFriendlyStatName(statKey);
        }
        
        // Méthode privée pour les traductions de fallback
        private static string GetFriendlyStatName(string statKey)
        {
            return statKey switch
            {
                // Utiliser les clés telles qu'elles sont - RimWorld les traduira automatiquement
                "WorkSpeedGlobal" => "WorkSpeedGlobal".Translate(),
                "ConstructionSpeed" => "ConstructionSpeed".Translate(),
                "MiningSpeed" => "MiningSpeed".Translate(),
                "MiningYield" => "MiningYield".Translate(),
                "ConstructSuccessChance" => "ConstructSuccessChance".Translate(),
                "SmoothingSpeed" => "SmoothingSpeed".Translate(),
                "MeleeDamageFactor" => "MeleeDamageFactor".Translate(),
                "CarryingCapacity" => "CarryingCapacity".Translate(),
                "PlantWorkSpeed" => "PlantWorkSpeed".Translate(),
                "DeepDrillingSpeed" => "DeepDrillingSpeed".Translate(),
                "ShootingAccuracyPawn" => "ShootingAccuracyPawn".Translate(),
                "MeleeHitChance" => "MeleeHitChance".Translate(),
                "MedicalTendSpeed" => "MedicalTendSpeed".Translate(),
                "MedicalTendQuality" => "MedicalTendQuality".Translate(),
                "SurgerySuccessChanceFactor" => "SurgerySuccessChanceFactor".Translate(),
                "FoodPoisonChance" => "FoodPoisonChance".Translate(),
                "MoveSpeed" => "MoveSpeed".Translate(),
                "MeleeDodgeChance" => "MeleeDodgeChance".Translate(),
                "AimingDelayFactor" => "AimingDelayFactor".Translate(),
                "HuntingStealth" => "HuntingStealth".Translate(),
                "RestRateMultiplier" => "RestRateMultiplier".Translate(),
                "PlantHarvestYield" => "PlantHarvestYield".Translate(),
                "FilthRate" => "FilthRate".Translate(),
                "EatingSpeed" => "EatingSpeed".Translate(),
                "ImmunityGainSpeed" => "ImmunityGainSpeed".Translate(),
                "ComfyTemperatureMin" => "ComfyTemperatureMin".Translate(),
                "ComfyTemperatureMax" => "ComfyTemperatureMax".Translate(),
                "ToxicResistance" => "ToxicResistance".Translate(),
                "PainShockThreshold" => "PainShockThreshold".Translate(),
                "MentalBreakThreshold" => "MentalBreakThreshold".Translate(),
                "ResearchSpeed" => "ResearchSpeed".Translate(),
                "GlobalLearningFactor" => "GlobalLearningFactor".Translate(),
                "MedicalSurgerySuccessChance" => "MedicalSurgerySuccessChance".Translate(),
                "TrapSpringChance" => "TrapSpringChance".Translate(),
                "NegotiationAbility" => "NegotiationAbility".Translate(),
                "PsychicSensitivity" => "PsychicSensitivity".Translate(),
                "SocialImpact" => "SocialImpact".Translate(),
                "TradePriceImprovement" => "TradePriceImprovement".Translate(),
                "TameAnimalChance" => "TameAnimalChance".Translate(),
                "TrainAnimalChance" => "TrainAnimalChance".Translate(),
                "AnimalGatherYield" => "AnimalGatherYield".Translate(),
                "Beauty" => "Beauty".Translate(),
                "ArrestSuccessChance" => "ArrestSuccessChance".Translate(),
                _ => statKey // Utiliser la clé originale si pas de traduction
            };
        }
    }
}
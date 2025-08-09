using Verse;
using System.Collections.Generic;
using RimWorld;

namespace RPGYourStat
{
    public class  RPGYourStat_Settings : ModSettings
    {
        public bool debugMode = false;
        public float experienceMultiplier = 1.0f;
        public bool enableCombatExperience = true;
        public bool enableWorkExperience = true;
        public bool enableSocialExperience = true;

        // Paramètres avancés pour les multiplicateurs de bonus
        public Dictionary<string, float> statMultipliers = new Dictionary<string, float>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref debugMode, "debugMode", false);
            Scribe_Values.Look(ref experienceMultiplier, "experienceMultiplier", 1.0f);
            Scribe_Values.Look(ref enableCombatExperience, "enableCombatExperience", true);
            Scribe_Values.Look(ref enableWorkExperience, "enableWorkExperience", true);
            Scribe_Values.Look(ref enableSocialExperience, "enableSocialExperience", true);

            // Sauvegarder les multiplicateurs personnalisés
            Scribe_Collections.Look(ref statMultipliers, "statMultipliers", LookMode.Value, LookMode.Value);
            
            // MODIFIÉ : Toujours initialiser/mettre à jour les multiplicateurs
            if (statMultipliers == null)
            {
                statMultipliers = new Dictionary<string, float>();
            }
            
            // NOUVEAU : Mettre à jour avec les nouvelles valeurs après le chargement
            UpdateMultipliers();
        }

        // NOUVELLE MÉTHODE : Mise à jour des multiplicateurs avec les nouvelles valeurs
        private void UpdateMultipliers()
        {
            var newMappings = GetDefaultMappings();
            
            // Ajouter les nouvelles clés manquantes
            foreach (var kvp in newMappings)
            {
                if (!statMultipliers.ContainsKey(kvp.Key))
                {
                    statMultipliers[kvp.Key] = kvp.Value;
                }
            }
            
            // NOUVEAU : Supprimer les anciennes clés obsolètes
            var obsoleteKeys = new List<string>();
            foreach (var key in statMultipliers.Keys)
            {
                if (!newMappings.ContainsKey(key))
                {
                    obsoleteKeys.Add(key);
                }
            }
            
            foreach (var key in obsoleteKeys)
            {
                statMultipliers.Remove(key);
            }
        }

        // MODIFIÉ : Méthode pour obtenir les mappings par défaut avec les stats existantes
        private Dictionary<string, float> GetDefaultMappings()
        {
            return new Dictionary<string, float>
            {
                // STR - FORCE : Travaux physiques, force brute, porter des charges
                ["STR_WorkSpeedGlobal"] = 0.02f,              // Force dans tous les travaux
                ["STR_ConstructionSpeed"] = 0.03f,            // +3% par niveau
                ["STR_MiningSpeed"] = 0.03f,                  // +3% par niveau
                ["STR_MiningYield"] = 0.02f,                  // +2% par niveau
                ["STR_ConstructSuccessChance"] = 0.01f,       // +1% par niveau
                ["STR_SmoothingSpeed"] = 0.03f,               // +3% par niveau
                ["STR_MeleeDamageFactor"] = 0.025f,           // +2.5% par niveau
                ["STR_CarryingCapacity"] = 0.05f,             // +5% par niveau
                ["STR_PlantWorkSpeed"] = 0.025f,              // +2.5% par niveau (travail physique agricole)
                ["STR_DeepDrillingSpeed"] = 0.03f,            // +3% par niveau
                
                // DEX - DEXTÉRITÉ : Précision, manipulation fine, coordination main-œil
                ["DEX_ShootingAccuracyPawn"] = 0.02f,         // +2% par niveau
                ["DEX_MeleeHitChance"] = 0.02f,               // +2% par niveau
                ["DEX_WorkSpeedGlobal"] = 0.02f,              // +2% par niveau (dextérité dans les tâches)
                ["DEX_MedicalTendSpeed"] = 0.025f,            // +2.5% par niveau
                ["DEX_MedicalTendQuality"] = 0.02f,           // +2% par niveau (précision médicale)
                ["DEX_SurgerySuccessChanceFactor"] = 0.015f,  // +1.5% par niveau
                ["DEX_FoodPoisonChance"] = -0.01f,            // -1% par niveau (précision en cuisine)
                
                // AGL - AGILITÉ : Vitesse, esquive, réflexes, mobilité
                ["AGL_MoveSpeed"] = 0.03f,                    // +3% par niveau
                ["AGL_MeleeDodgeChance"] = 0.02f,             // +2% par niveau
                ["AGL_AimingDelayFactor"] = -0.015f,          // -1.5% par niveau (réflexes de visée)
                ["AGL_HuntingStealth"] = 0.02f,               // +2% par niveau (discrétion/agilité)
                ["AGL_RestRateMultiplier"] = 0.02f,           // +2% par niveau (récupération rapide)
                ["AGL_PlantHarvestYield"] = 0.02f,            // +2% par niveau (agilité pour récolter)
                ["AGL_FilthRate"] = -0.03f,                   // -3% par niveau (éviter de salir)
                ["AGL_EatingSpeed"] = 0.03f,                  // +3% par niveau (rapidité d'action)
                
                // CON - CONSTITUTION : Endurance, résistance, santé, récupération
                ["CON_CarryingCapacity"] = 0.03f,             // +3% par niveau (endurance physique)
                ["CON_WorkSpeedGlobal"] = 0.015f,             // +1.5% par niveau (endurance au travail)
                ["CON_ImmunityGainSpeed"] = 0.03f,            // +3% par niveau
                ["CON_MentalBreakThreshold"] = -0.01f,        // -1% par niveau (endurance mentale)
                ["CON_RestRateMultiplier"] = 0.025f,          // +2.5% par niveau (récupération physique)
                ["CON_ComfyTemperatureMin"] = -0.1f,          // -10%°C par niveau (résistance froid)
                ["CON_ComfyTemperatureMax"] = 0.1f,           // +10%°C par niveau (résistance chaud)
                ["CON_ToxicResistance"] = 0.02f,              // +2% par niveau
                ["CON_FoodPoisonChance"] = -0.015f,           // -1.5% par niveau (résistance empoisonnement)
                ["CON_PainShockThreshold"] = 0.03f,           // +3% par niveau
                
                // INT - INTELLIGENCE : Recherche, apprentissage, résolution de problèmes
                ["INT_ResearchSpeed"] = 0.04f,                // +4% par niveau
                ["INT_GlobalLearningFactor"] = 0.03f,         // +3% par niveau
                ["INT_MedicalTendQuality"] = 0.025f,          // +2.5% par niveau (connaissance médicale)
                ["INT_MedicalSurgerySuccessChance"] = 0.02f,  // +2% par niveau (connaissance médicale)
                ["INT_PlantWorkSpeed"] = 0.02f,               // +2% par niveau (connaissance botanique)
                ["INT_TrapSpringChance"] = -0.02f,            // -2% par niveau (intelligence pour éviter pièges)
                ["INT_NegotiationAbility"] = 0.015f,          // +1.5% par niveau (intelligence émotionnelle)
                ["INT_PsychicSensitivity"] = 0.015f,          // +1.5% par niveau (sensibilité psychique)
                
                // CHA - CHARISME : Relations sociales, négociation, leadership, beauté
                ["CHA_SocialImpact"] = 0.04f,                 // +4% par niveau
                ["CHA_NegotiationAbility"] = 0.025f,          // +2.5% par niveau (charisme principal)
                ["CHA_TradePriceImprovement"] = 0.02f,        // +2% par niveau
                ["CHA_TameAnimalChance"] = 0.03f,             // +3% par niveau
                ["CHA_TrainAnimalChance"] = 0.025f,           // +2.5% par niveau
                ["CHA_AnimalGatherYield"] = 0.02f,            // +2% par niveau (relation avec animaux)
                ["CHA_Beauty"] = 0.02f,                       // +2% par niveau (charisme = beauté)
                ["CHA_ArrestSuccessChance"] = 0.02f,          // +2% par niveau (persuasion/autorité)
                ["CHA_MentalBreakThreshold"] = -0.015f        // -1.5% par niveau (résistance au stress)
            };
        }

        // MODIFIÉ : Méthode publique pour l'initialisation avec les nouvelles améliorations
        public void InitializeDefaultMultipliers()
        {
            statMultipliers = new Dictionary<string, float>();
            var defaultMappings = GetDefaultMappings();

            foreach (var kvp in defaultMappings)
            {
                statMultipliers[kvp.Key] = kvp.Value;
            }
        }

        public float GetStatMultiplier(string key, float defaultValue)
        {
            if (statMultipliers == null)
            {
                InitializeDefaultMultipliers();
            }
            
            return statMultipliers.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public void SetStatMultiplier(string key, float value)
        {
            if (statMultipliers == null)
            {
                InitializeDefaultMultipliers();
            }
            
            statMultipliers[key] = value;
        }
    }
}
using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace RPGYourStat
{
    public class RPGYourStat_Mod : Mod
    {
        public static RPGYourStat_Settings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private bool showAdvancedSettings = false;

        public RPGYourStat_Mod(ModContentPack content) : base(content)
        {
            Log.Message("Le mod RPG Your Stat a été chargé avec succès !");
            settings = GetSettings<RPGYourStat_Settings>();
            
            LongEventHandler.QueueLongEvent(AddRPGComponentToPawns, "Initialisation RPG Stats", false, null);
        }

        private void AddRPGComponentToPawns()
        {
            var humanDef = DefDatabase<ThingDef>.GetNamed("Human", false);
            if (humanDef != null && !humanDef.comps.Any(c => c.compClass == typeof(CompRPGStats)))
            {
                humanDef.comps.Add(new CompPropertiesRPGStats());
                Log.Message("[RPGYourStat] Composant RPG ajouté aux humains");
            }

            var animalDefs = DefDatabase<ThingDef>.AllDefs.Where(def => 
                def.category == ThingCategory.Pawn && 
                def.race?.Animal == true);

            foreach (var animalDef in animalDefs)
            {
                if (!animalDef.comps.Any(c => c.compClass == typeof(CompRPGStats)))
                {
                    animalDef.comps.Add(new CompPropertiesRPGStats());
                }
            }

            Log.Message("[RPGYourStat] Composants RPG ajoutés aux animaux");
        }

        public override string SettingsCategory()
        {
            return "RPG Your Stat";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // MODIFIÉ : Forcer la mise à jour des multiplicateurs à chaque ouverture
            if (settings.statMultipliers == null)
            {
                settings.statMultipliers = new Dictionary<string, float>();
            }
            
            // NOUVEAU : Vérifier et ajouter les nouvelles clés manquantes
            var newMappings = new Dictionary<string, float>
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

            // Ajouter les nouvelles clés manquantes
            foreach (var kvp in newMappings)
            {
                if (!settings.statMultipliers.ContainsKey(kvp.Key))
                {
                    settings.statMultipliers[kvp.Key] = kvp.Value;
                }
            }

            // NOUVEAU : Supprimer les anciennes clés obsolètes
            var obsoleteKeys = new List<string>();
            foreach (var key in settings.statMultipliers.Keys.ToList())
            {
                if (!newMappings.ContainsKey(key))
                {
                    obsoleteKeys.Add(key);
                }
            }
            
            foreach (var key in obsoleteKeys)
            {
                settings.statMultipliers.Remove(key);
            }

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Paramètres de base
            listing.CheckboxLabeled("Activer le mode de débogage", ref settings.debugMode, 
                "Affiche les messages de débogage dans la console.");

            listing.Gap();
            listing.Label($"Multiplicateur d'expérience: {settings.experienceMultiplier:F1}");
            settings.experienceMultiplier = listing.Slider(settings.experienceMultiplier, 0.1f, 5.0f);

            listing.Gap();
            listing.CheckboxLabeled("Expérience de combat", ref settings.enableCombatExperience,
                "Les pawns gagnent de l'XP en combattant.");

            listing.CheckboxLabeled("Expérience de travail", ref settings.enableWorkExperience,
                "Les pawns gagnent de l'XP en travaillant.");

            listing.CheckboxLabeled("Expérience sociale", ref settings.enableSocialExperience,
                "Les pawns gagnent de l'XP en interagissant socialement.");

            listing.Gap();

            // Bouton pour les paramètres avancés
            if (listing.ButtonTextLabeled("Paramètres avancés:", showAdvancedSettings ? "Masquer les coefficients" : "Afficher les coefficients"))
            {
                showAdvancedSettings = !showAdvancedSettings;
            }

            listing.End();

            // Interface des paramètres avancés
            if (showAdvancedSettings)
            {
                // Calculer la zone pour les paramètres avancés
                Rect advancedRect = new Rect(inRect.x, inRect.y + 200f, inRect.width, inRect.height - 200f);
                DrawAdvancedSettings(advancedRect);
            }

            base.DoSettingsWindowContents(inRect);
        }

        private void DrawAdvancedSettings(Rect inRect)
        {
            // En-tête
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            GUI.color = Color.cyan;
            Widgets.Label(headerRect, "=== MULTIPLICATEURS DES BONUS RPG ===");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect descRect = new Rect(inRect.x, inRect.y + 35f, inRect.width, 40f);
            Widgets.Label(descRect, "Modifiez les bonus par niveau pour chaque amélioration (de -10% à +10%):");

            // Zone de scroll pour les paramètres avancés
            Rect scrollRect = new Rect(inRect.x, inRect.y + 80f, inRect.width, inRect.height - 120f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, GetAdvancedSettingsHeight());
            
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            
            float currentY = 0f;
            
            // Grouper par stat RPG
            var statGroups = new[]
            {
                new { Type = "STR", Name = "FORCE", Color = new Color(1.0f, 0.5f, 0.5f) },
                new { Type = "DEX", Name = "DEXTÉRITÉ", Color = new Color(0.5f, 1.0f, 0.5f) },
                new { Type = "AGL", Name = "AGILITÉ", Color = new Color(0.5f, 0.5f, 1.0f) },
                new { Type = "CON", Name = "CONSTITUTION", Color = new Color(1.0f, 0.8f, 0.3f) },
                new { Type = "INT", Name = "INTELLIGENCE", Color = new Color(0.8f, 0.3f, 1.0f) },
                new { Type = "CHA", Name = "CHARISME", Color = new Color(1.0f, 0.3f, 0.8f) }
            };
            
            for (int i = 0; i < statGroups.Length; i++)
            {
                var statGroup = statGroups[i];
                
                // En-tête de la stat avec couleur
                Rect statHeaderRect = new Rect(0f, currentY, viewRect.width, 30f);
                GUI.color = statGroup.Color;
                Text.Font = GameFont.Medium;
                Widgets.Label(statHeaderRect, $"=== {statGroup.Name} ===");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                currentY += 35f;
                
                // Multiplicateurs pour cette stat
                currentY = DrawStatMultipliers(statGroup.Type, currentY, viewRect.width);
                currentY += 20f; // Espacement entre les sections
            }
            
            Widgets.EndScrollView();
            
            // Bouton de réinitialisation
            Rect resetButtonRect = new Rect(inRect.x, inRect.y + inRect.height - 35f, 200f, 30f);
            if (Widgets.ButtonText(resetButtonRect, "Réinitialiser aux valeurs par défaut"))
            {
                ResetToDefaults();
            }
        }

        private float DrawStatMultipliers(string statType, float startY, float width)
        {
            float currentY = startY;
            
            // Obtenir les multiplicateurs pour cette stat
            var multiplierKeys = settings.statMultipliers.Keys.Where(k => k.StartsWith(statType + "_")).OrderBy(k => k).ToList();
            
            foreach (string key in multiplierKeys)
            {
                string statName = GetFriendlyStatName(key.Substring(statType.Length + 1));
                float currentValue = settings.GetStatMultiplier(key, 0f);
                
                // Layout en colonnes
                Rect labelRect = new Rect(20f, currentY, width * 0.45f, 24f);
                Rect sliderRect = new Rect(width * 0.5f, currentY, width * 0.35f, 24f);
                Rect valueRect = new Rect(width * 0.87f, currentY, width * 0.13f, 24f);
                
                // Nom de la stat avec tooltip
                Widgets.Label(labelRect, statName);
                if (Mouse.IsOver(labelRect))
                {
                    TooltipHandler.TipRegion(labelRect, $"Clé interne: {key}");
                }
                
                // Slider
                float newValue = Widgets.HorizontalSlider(sliderRect, currentValue, -0.1f, 0.1f, true);
                
                // Affichage de la valeur en pourcentage
                string percentageText = $"{(newValue * 100f):F1}%";
                GUI.color = newValue >= 0 ? Color.green : Color.red;
                Widgets.Label(valueRect, percentageText);
                GUI.color = Color.white;
                
                // Mettre à jour la valeur si elle a changé
                if (System.Math.Abs(newValue - currentValue) > 0.001f)
                {
                    settings.SetStatMultiplier(key, newValue);
                }
                
                currentY += 28f;
            }
            
            return currentY;
        }

        private string GetFriendlyStatName(string statKey)
        {
            // Convertir les clés internes en noms plus lisibles - CORRECTION DES DOUBLONS
            return statKey switch
            {
                // STR - FORCE
                "WorkSpeedGlobal" => "Vitesse de travail globale",
                "ConstructionSpeed" => "Vitesse de construction",
                "MiningSpeed" => "Vitesse de minage",
                "MiningYield" => "Rendement de minage",
                "ConstructSuccessChance" => "Chance de réussite construction",
                "SmoothingSpeed" => "Vitesse de lissage",
                "MeleeDamageFactor" => "Facteur de dégâts mêlée",
                "CarryingCapacity" => "Capacité de port",
                "PlantWorkSpeed" => "Vitesse de travail des plantes",
                "DeepDrillingSpeed" => "Vitesse de forage profond",
                
                // DEX - DEXTÉRITÉ
                "ShootingAccuracyPawn" => "Précision de tir",
                "MeleeHitChance" => "Chance de toucher en mêlée",
                "MedicalTendSpeed" => "Vitesse de soins médicaux",
                "MedicalTendQuality" => "Qualité soins médicaux",
                "SurgerySuccessChanceFactor" => "Facteur réussite chirurgie",
                "FoodPoisonChance" => "Chance d'empoisonnement alimentaire",
                
                // AGL - AGILITÉ
                "MoveSpeed" => "Vitesse de déplacement",
                "MeleeDodgeChance" => "Chance d'esquive mêlée",
                "AimingDelayFactor" => "Facteur délai de visée",
                "HuntingStealth" => "Discrétion de chasse",
                "RestRateMultiplier" => "Multiplicateur de repos",
                "PlantHarvestYield" => "Rendement de récolte",
                "FilthRate" => "Taux de saleté",
                "EatingSpeed" => "Vitesse d'alimentation",
                
                // CON - CONSTITUTION
                "ImmunityGainSpeed" => "Vitesse gain d'immunité",
                "ComfyTemperatureMin" => "Température confort minimale",
                "ComfyTemperatureMax" => "Température confort maximale",
                "ToxicResistance" => "Résistance toxique",
                "PainShockThreshold" => "Seuil de choc douloureux",
                "MentalBreakThreshold" => "Seuil de crise mentale",
                
                // INT - INTELLIGENCE
                "ResearchSpeed" => "Vitesse de recherche",
                "GlobalLearningFactor" => "Facteur d'apprentissage global",
                "MedicalSurgerySuccessChance" => "Chance de réussite chirurgie",
                "TrapSpringChance" => "Chance de déclencher un piège",
                "NegotiationAbility" => "Capacité de négociation",
                "PsychicSensitivity" => "Sensibilité psychique",
                
                // CHA - CHARISME
                "SocialImpact" => "Impact social",
                "TradePriceImprovement" => "Amélioration prix de commerce",
                "TameAnimalChance" => "Chance d'apprivoiser animal",
                "TrainAnimalChance" => "Chance de dresser animal",
                "AnimalGatherYield" => "Rendement collecte animal",
                "Beauty" => "Beauté",
                "ArrestSuccessChance" => "Chance de réussite d'arrestation",
                
                _ => statKey // Utiliser la clé originale si pas de traduction
            };
        }

        private float GetAdvancedSettingsHeight()
        {
            // Calculer la hauteur nécessaire pour tous les paramètres avancés
            int totalMultipliers = settings.statMultipliers?.Count ?? 0;
            int statSections = 6; // STR, DEX, AGL, CON, INT, CHA
            
            return (totalMultipliers * 28f) + (statSections * 55f) + 100f; // +100f pour les marges
        }

        private void ResetToDefaults()
        {
            settings.statMultipliers.Clear();
            settings.InitializeDefaultMultipliers();
            Messages.Message("Multiplicateurs réinitialisés aux valeurs par défaut", MessageTypeDefOf.PositiveEvent);
        }
    }
}
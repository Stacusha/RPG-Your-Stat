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
            
            // NOUVEAU : Paramètre pour les animaux avec callback pour mettre à jour les multiplicateurs
            bool previousAnimalSetting = settings.enableAnimalRPGStats;
            listing.CheckboxLabeled("Activer les stats RPG pour les animaux", ref settings.enableAnimalRPGStats,
                "Les animaux peuvent gagner de l'XP et améliorer leurs statistiques.");
            
            // Si le paramètre a changé, mettre à jour les multiplicateurs
            if (previousAnimalSetting != settings.enableAnimalRPGStats)
            {
                UpdateMultipliersForAnimalSetting();
            }

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
                Rect advancedRect = new Rect(inRect.x, inRect.y + 240f, inRect.width, inRect.height - 240f);
                DrawAdvancedSettings(advancedRect);
            }

            base.DoSettingsWindowContents(inRect);
        }

        // NOUVELLE MÉTHODE : Mettre à jour les multiplicateurs selon le paramètre des animaux
        private void UpdateMultipliersForAnimalSetting()
        {
            if (settings.enableAnimalRPGStats)
            {
                // Ajouter les multiplicateurs des animaux
                var animalMappings = new Dictionary<string, float>
                {
                    // Multiplicateurs pour animaux (comme défini dans AnimalStatModifierSystem)
                    ["ANIMAL_STR_CarryingCapacity"] = 0.08f,
                    ["ANIMAL_STR_MeleeDamageFactor"] = 0.04f,
                    ["ANIMAL_STR_WorkSpeedGlobal"] = 0.03f,
                    ["ANIMAL_STR_MiningSpeed"] = 0.05f,
                    ["ANIMAL_STR_MiningYield"] = 0.03f,
                    
                    ["ANIMAL_DEX_MeleeHitChance"] = 0.03f,
                    ["ANIMAL_DEX_ShootingAccuracyPawn"] = 0.015f,
                    ["ANIMAL_DEX_WorkSpeedGlobal"] = 0.025f,
                    
                    ["ANIMAL_AGL_MoveSpeed"] = 0.05f,
                    ["ANIMAL_AGL_MeleeDodgeChance"] = 0.04f,
                    ["ANIMAL_AGL_HuntingStealth"] = 0.04f,
                    ["ANIMAL_AGL_AimingDelayFactor"] = -0.025f,
                    ["ANIMAL_AGL_FilthRate"] = -0.04f,
                    
                    ["ANIMAL_CON_ImmunityGainSpeed"] = 0.05f,
                    ["ANIMAL_CON_RestRateMultiplier"] = 0.04f,
                    ["ANIMAL_CON_ComfyTemperatureMin"] = -0.15f,
                    ["ANIMAL_CON_ComfyTemperatureMax"] = 0.15f,
                    ["ANIMAL_CON_ToxicResistance"] = 0.03f,
                    ["ANIMAL_CON_PainShockThreshold"] = 0.05f,
                    ["ANIMAL_CON_FoodPoisonChance"] = -0.02f,
                    ["ANIMAL_CON_CarryingCapacity"] = 0.04f,
                    
                    ["ANIMAL_INT_GlobalLearningFactor"] = 0.02f,
                    ["ANIMAL_INT_TrapSpringChance"] = -0.015f,
                    ["ANIMAL_INT_HuntingStealth"] = 0.025f,
                    ["ANIMAL_INT_WorkSpeedGlobal"] = 0.015f,
                    
                    ["ANIMAL_CHA_SocialImpact"] = 0.03f,
                    ["ANIMAL_CHA_TameAnimalChance"] = 0.04f,
                    ["ANIMAL_CHA_TrainAnimalChance"] = 0.035f,
                    ["ANIMAL_CHA_Beauty"] = 0.03f,
                    ["ANIMAL_CHA_AnimalGatherYield"] = 0.04f
                };

                foreach (var kvp in animalMappings)
                {
                    if (!settings.statMultipliers.ContainsKey(kvp.Key))
                    {
                        settings.statMultipliers[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                // Supprimer les multiplicateurs des animaux
                var animalKeys = settings.statMultipliers.Keys.Where(k => k.StartsWith("ANIMAL_")).ToList();
                foreach (var key in animalKeys)
                {
                    settings.statMultipliers.Remove(key);
                }
            }
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
            
            // Grouper par type (humains puis animaux si activés)
            var humanStatGroups = new[]
            {
                new { Type = "STR", Name = "FORCE (Humains)", Color = new Color(1.0f, 0.5f, 0.5f), Prefix = "" },
                new { Type = "DEX", Name = "DEXTÉRITÉ (Humains)", Color = new Color(0.5f, 1.0f, 0.5f), Prefix = "" },
                new { Type = "AGL", Name = "AGILITÉ (Humains)", Color = new Color(0.5f, 0.5f, 1.0f), Prefix = "" },
                new { Type = "CON", Name = "CONSTITUTION (Humains)", Color = new Color(1.0f, 0.8f, 0.3f), Prefix = "" },
                new { Type = "INT", Name = "INTELLIGENCE (Humains)", Color = new Color(0.8f, 0.3f, 1.0f), Prefix = "" },
                new { Type = "CHA", Name = "CHARISME (Humains)", Color = new Color(1.0f, 0.3f, 0.8f), Prefix = "" }
            };

            // Dessiner les stats des humains
            for (int i = 0; i < humanStatGroups.Length; i++)
            {
                var statGroup = humanStatGroups[i];
                currentY = DrawStatGroupHeader(statGroup.Name, statGroup.Color, currentY, viewRect.width);
                currentY = DrawStatMultipliers(statGroup.Type, currentY, viewRect.width);
                currentY += 20f;
            }

            // NOUVEAU : Dessiner les stats des animaux si activées
            if (settings.enableAnimalRPGStats)
            {
                var animalStatGroups = new[]
                {
                    new { Type = "ANIMAL_STR", Name = "FORCE (Animaux)", Color = new Color(1.0f, 0.3f, 0.3f), Prefix = "ANIMAL_" },
                    new { Type = "ANIMAL_DEX", Name = "DEXTÉRITÉ (Animaux)", Color = new Color(0.3f, 1.0f, 0.3f), Prefix = "ANIMAL_" },
                    new { Type = "ANIMAL_AGL", Name = "AGILITÉ (Animaux)", Color = new Color(0.3f, 0.3f, 1.0f), Prefix = "ANIMAL_" },
                    new { Type = "ANIMAL_CON", Name = "CONSTITUTION (Animaux)", Color = new Color(1.0f, 0.6f, 0.1f), Prefix = "ANIMAL_" },
                    new { Type = "ANIMAL_INT", Name = "INTELLIGENCE (Animaux)", Color = new Color(0.6f, 0.1f, 1.0f), Prefix = "ANIMAL_" },
                    new { Type = "ANIMAL_CHA", Name = "CHARISME (Animaux)", Color = new Color(1.0f, 0.1f, 0.6f), Prefix = "ANIMAL_" }
                };

                // Séparateur
                currentY += 20f;
                Rect separatorRect = new Rect(viewRect.width * 0.1f, currentY, viewRect.width * 0.8f, 2f);
                GUI.color = Color.gray;
                Widgets.DrawBoxSolid(separatorRect, GUI.color);
                GUI.color = Color.white;
                currentY += 30f;

                for (int i = 0; i < animalStatGroups.Length; i++)
                {
                    var statGroup = animalStatGroups[i];
                    currentY = DrawStatGroupHeader(statGroup.Name, statGroup.Color, currentY, viewRect.width);
                    currentY = DrawStatMultipliers(statGroup.Type, currentY, viewRect.width);
                    currentY += 20f;
                }
            }
            else
            {
                // Afficher un message si les stats des animaux sont désactivées
                currentY += 20f;
                Rect disabledRect = new Rect(20f, currentY, viewRect.width - 40f, 30f);
                GUI.color = Color.gray;
                Text.Font = GameFont.Medium;
                Widgets.Label(disabledRect, "=== STATS ANIMAUX DÉSACTIVÉES ===");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                currentY += 50f;
            }
            
            Widgets.EndScrollView();
            
            // Bouton de réinitialisation
            Rect resetButtonRect = new Rect(inRect.x, inRect.y + inRect.height - 35f, 200f, 30f);
            if (Widgets.ButtonText(resetButtonRect, "Réinitialiser aux valeurs par défaut"))
            {
                ResetToDefaults();
            }
        }

        // NOUVELLE MÉTHODE : Dessiner l'en-tête d'un groupe de stats
        private float DrawStatGroupHeader(string title, Color color, float startY, float width)
        {
            Rect statHeaderRect = new Rect(0f, startY, width, 30f);
            GUI.color = color;
            Text.Font = GameFont.Medium;
            Widgets.Label(statHeaderRect, $"=== {title} ===");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return startY + 35f;
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
            int humanSections = 6; // STR, DEX, AGL, CON, INT, CHA pour humains
            int animalSections = settings.enableAnimalRPGStats ? 6 : 0; // Même chose pour animaux si activé
            
            float baseHeight = (totalMultipliers * 28f) + ((humanSections + animalSections) * 55f) + 200f;
            
            if (!settings.enableAnimalRPGStats)
            {
                baseHeight += 80f; // Espace pour le message "désactivé"
            }
            
            return baseHeight;
        }

        private void ResetToDefaults()
        {
            settings.statMultipliers.Clear();
            settings.InitializeDefaultMultipliers();
            Messages.Message("Multiplicateurs réinitialisés aux valeurs par défaut", MessageTypeDefOf.PositiveEvent);
        }
    }
}
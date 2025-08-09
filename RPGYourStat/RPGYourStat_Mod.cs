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
            try
            {
                // MODIFIÉ : Simplifier l'ajout du composant
                if (Find.Maps != null)
                {
                    foreach (var map in Find.Maps)
                    {
                        if (map?.mapPawns?.AllPawns != null)
                        {
                            foreach (var pawn in map.mapPawns.AllPawns)
                            {
                                if (pawn?.def?.comps != null)
                                {
                                    // Vérifier si le composant existe déjà
                                    var existingComp = pawn.GetComp<CompRPGStats>();
                                    if (existingComp == null)
                                    {
                                        // Ajouter le composant manuellement
                                        var newComp = new CompRPGStats();
                                        newComp.parent = pawn;
                                        pawn.AllComps.Add(newComp);
                                        newComp.PostSpawnSetup(false);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RPGYourStat] Erreur lors de l'ajout du composant RPG: {ex}");
            }
        }

        public override string SettingsCategory()
        {
            return "RPG Your Stat";
        }

        // MODIFIÉ : Forcer la mise à jour des multiplicateurs à chaque ouverture
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // MODIFIÉ : Forcer la mise à jour des multiplicateurs à chaque ouverture
            if (settings.statMultipliers == null)
            {
                settings.statMultipliers = new Dictionary<string, float>();
            }
            
            // NOUVEAU : Agrandir la fenêtre automatiquement
            var originalWindowSize = UI.screenWidth * 0.8f; // 80% de la largeur d'écran
            var newWindowHeight = UI.screenHeight * 0.9f;   // 90% de la hauteur d'écran
            
            // MODIFIÉ : Calculer l'espace disponible selon si les paramètres avancés sont ouverts
            float buttonHeight = 35f;
            float advancedButtonSpacing = 15f;
            float basicSettingsHeight = showAdvancedSettings ? 
                220f : // Plus petit si les paramètres avancés sont ouverts
                inRect.height - buttonHeight - advancedButtonSpacing - 20f; // Presque toute la fenêtre sinon
            
            Rect basicSettingsRect = new Rect(inRect.x, inRect.y, inRect.width, basicSettingsHeight);
            
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(basicSettingsRect);

            // Paramètres de base avec plus d'espacement
            listing.CheckboxLabeled(TranslationHelper.GetSettingsText("DebugMode"), ref settings.debugMode, 
                TranslationHelper.GetSettingsText("DebugModeTooltip"));

            listing.Gap(12f);
            listing.Label(TranslationHelper.GetSettingsText("ExperienceMultiplier").Translate(settings.experienceMultiplier));
            settings.experienceMultiplier = listing.Slider(settings.experienceMultiplier, 0.1f, 5.0f);

            listing.Gap(12f);
            listing.CheckboxLabeled(TranslationHelper.GetSettingsText("EnableCombatExperience"), ref settings.enableCombatExperience,
                TranslationHelper.GetSettingsText("EnableCombatExperienceTooltip"));

            listing.CheckboxLabeled(TranslationHelper.GetSettingsText("EnableWorkExperience"), ref settings.enableWorkExperience,
                TranslationHelper.GetSettingsText("EnableWorkExperienceTooltip"));

            listing.CheckboxLabeled(TranslationHelper.GetSettingsText("EnableSocialExperience"), ref settings.enableSocialExperience,
                TranslationHelper.GetSettingsText("EnableSocialExperienceTooltip"));

            listing.Gap(12f);
            
            // Paramètre pour les animaux avec callback pour mettre à jour les multiplicateurs
            bool previousAnimalSetting = settings.enableAnimalRPGStats;
            listing.CheckboxLabeled(TranslationHelper.GetSettingsText("EnableAnimalRPGStats"), ref settings.enableAnimalRPGStats,
                TranslationHelper.GetSettingsText("EnableAnimalRPGStatsTooltip"));
                
            // Si le paramètre a changé, mettre à jour les multiplicateurs
            if (previousAnimalSetting != settings.enableAnimalRPGStats)
            {
                UpdateMultipliersForAnimalSetting();
            }

            listing.Gap(12f);
            
            // Paramètres d'équilibrage automatique
            listing.CheckboxLabeled(TranslationHelper.GetSettingsText("EnableAutoBalance"), ref settings.enableAutoBalance,
                TranslationHelper.GetSettingsText("EnableAutoBalanceTooltip"));
    
            if (settings.enableAutoBalance)
            {
                listing.Gap(8f);
                listing.Label(TranslationHelper.GetSettingsText("EnemyBalanceMultiplier").Translate(settings.enemyBalanceMultiplier));
                settings.enemyBalanceMultiplier = listing.Slider(settings.enemyBalanceMultiplier, 0.5f, 2.0f);
                
                listing.Label(TranslationHelper.GetSettingsText("AllyBalanceMultiplier").Translate(settings.allyBalanceMultiplier));
                settings.allyBalanceMultiplier = listing.Slider(settings.allyBalanceMultiplier, 0.5f, 2.0f);
            }

            listing.End();

            // CORRIGÉ : Position du bouton selon l'état des paramètres avancés
            float buttonY;
            if (showAdvancedSettings)
            {
                // Si les paramètres avancés sont ouverts, le bouton va en bas de la fenêtre
                buttonY = inRect.y + inRect.height - buttonHeight - 10f;
            }
            else
            {
                // Si fermés, le bouton va juste après les paramètres de base
                buttonY = basicSettingsRect.y + basicSettingsRect.height + advancedButtonSpacing;
            }

            // CORRIGÉ : Bouton pour les paramètres avancés avec position dynamique
            Rect advancedButtonRect = new Rect(inRect.x + 10f, buttonY, 350f, buttonHeight);
            string advancedButtonText = showAdvancedSettings ? 
                TranslationHelper.GetSettingsText("HideCoefficients") : 
                TranslationHelper.GetSettingsText("ShowCoefficients");
            
            // NOUVEAU : Couleur différente pour indiquer l'état
            GUI.color = showAdvancedSettings ? new Color(1f, 0.7f, 0.7f) : new Color(0.7f, 1f, 0.7f);
            if (Widgets.ButtonText(advancedButtonRect, $"{TranslationHelper.GetSettingsText("AdvancedSettings")}: {advancedButtonText}"))
            {
                showAdvancedSettings = !showAdvancedSettings;
            }
            GUI.color = Color.white;

            // CORRIGÉ : Interface des paramètres avancés SEULEMENT si ouverts
            if (showAdvancedSettings)
            {
                // MODIFIÉ : Position entre les paramètres de base et le bouton
                float advancedY = basicSettingsRect.y + basicSettingsRect.height + 10f;
                float advancedHeight = buttonY - advancedY - 10f; // Espace entre les paramètres et le bouton
                
                if (advancedHeight > 150f) // S'assurer qu'il y a assez d'espace
                {
                    Rect advancedRect = new Rect(inRect.x, advancedY, inRect.width, advancedHeight);
                    DrawAdvancedSettings(advancedRect);
                }
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

        // CORRIGÉ : Méthode DrawAdvancedSettings avec beaucoup plus d'espacement
        private void DrawAdvancedSettings(Rect inRect)
        {
            // MODIFIÉ : En-tête plus compact
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 25f); // Plus petit
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            Widgets.DrawBoxSolid(headerRect, GUI.color);
            GUI.color = Color.cyan;
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, TranslationHelper.GetSettingsText("AdvancedSettingsHeader"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // MODIFIÉ : Description plus compacte
            Rect descRect = new Rect(inRect.x, inRect.y + 30f, inRect.width, 20f); // Plus petit
            GUI.color = Color.gray;
            Widgets.Label(descRect, TranslationHelper.GetSettingsText("AdvancedSettingsDescription"));
            GUI.color = Color.white;

            // MODIFIÉ : Zone de scroll utilisant TOUT l'espace disponible
            Rect scrollRect = new Rect(inRect.x, inRect.y + 55f, inRect.width, inRect.height - 95f); // Utilise tout l'espace
            float totalHeight = GetAdvancedSettingsHeight();
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 20f, totalHeight);
            
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            
            float currentY = 10f; // Commencer avec un peu de marge
            
            // Dessiner les stats des humains avec plus d'espacement
            var humanStatGroups = new[]
            {
                new { Type = "STR", Name = TranslationHelper.GetStatDisplayName(StatType.STR), Color = new Color(1.0f, 0.5f, 0.5f) },
                new { Type = "DEX", Name = TranslationHelper.GetStatDisplayName(StatType.DEX), Color = new Color(0.5f, 1.0f, 0.5f) },
                new { Type = "AGL", Name = TranslationHelper.GetStatDisplayName(StatType.AGL), Color = new Color(0.5f, 0.5f, 1.0f) },
                new { Type = "CON", Name = TranslationHelper.GetStatDisplayName(StatType.CON), Color = new Color(1.0f, 0.8f, 0.3f) },
                new { Type = "INT", Name = TranslationHelper.GetStatDisplayName(StatType.INT), Color = new Color(0.8f, 0.3f, 1.0f) },
                new { Type = "CHA", Name = TranslationHelper.GetStatDisplayName(StatType.CHA), Color = new Color(1.0f, 0.3f, 0.8f) }
            };
            
            // Dessiner les stats des humains
            for (int i = 0; i < humanStatGroups.Length; i++)
            {
                var statGroup = humanStatGroups[i];
                currentY = DrawStatGroupHeader(statGroup.Name, statGroup.Color, currentY, viewRect.width);
                currentY = DrawStatMultipliers(statGroup.Type, currentY, viewRect.width);
                currentY += 25f;
            }

            // Dessiner les stats des animaux si activées
            if (settings.enableAnimalRPGStats)
            {
                currentY += 15f; // Espacement supplémentaire avant les animaux
                
                var animalStatGroups = new[]
                {
                    new { Type = "ANIMAL_STR", Name = TranslationHelper.GetAnimalStatGroupName(StatType.STR), Color = new Color(1.0f, 0.3f, 0.3f) },
                    new { Type = "ANIMAL_DEX", Name = TranslationHelper.GetAnimalStatGroupName(StatType.DEX), Color = new Color(0.3f, 1.0f, 0.3f) },
                    new { Type = "ANIMAL_AGL", Name = TranslationHelper.GetAnimalStatGroupName(StatType.AGL), Color = new Color(0.3f, 0.3f, 1.0f) },
                    new { Type = "ANIMAL_CON", Name = TranslationHelper.GetAnimalStatGroupName(StatType.CON), Color = new Color(1.0f, 0.6f, 0.1f) },
                    new { Type = "ANIMAL_INT", Name = TranslationHelper.GetAnimalStatGroupName(StatType.INT), Color = new Color(0.6f, 0.1f, 1.0f) },
                    new { Type = "ANIMAL_CHA", Name = TranslationHelper.GetAnimalStatGroupName(StatType.CHA), Color = new Color(1.0f, 0.1f, 0.6f) }
                };
                
                for (int i = 0; i < animalStatGroups.Length; i++)
                {
                    var statGroup = animalStatGroups[i];
                    currentY = DrawStatGroupHeader(statGroup.Name, statGroup.Color, currentY, viewRect.width);
                    currentY = DrawStatMultipliers(statGroup.Type, currentY, viewRect.width);
                    currentY += 25f;
                }
            }
            else
            {
                // Message indiquant que les stats animaux sont désactivées
                Rect disabledRect = new Rect(10f, currentY, viewRect.width - 20f, 30f);
                GUI.color = Color.gray;
                Text.Font = GameFont.Medium;
                Widgets.Label(disabledRect, TranslationHelper.GetMessageText("AnimalStatsDisabledSettings"));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                currentY += 40f;
            }
            
            // NOUVEAU : Bouton de réinitialisation DANS la zone de scroll, en bas
            currentY += 20f; // Espacement
            Rect resetButtonRect = new Rect(10f, currentY, 200f, 30f);
            GUI.color = new Color(0.7f, 0.2f, 0.2f);
            if (Widgets.ButtonText(resetButtonRect, TranslationHelper.GetSettingsText("ResetToDefaults")))
            {
                ResetToDefaults();
            }
            GUI.color = Color.white;
            
            currentY += 40f; // Marge finale
            
            Widgets.EndScrollView();
        }

        // MODIFIÉ : Dessiner l'en-tête d'un groupe de stats avec plus d'espacement
        private float DrawStatGroupHeader(string title, Color color, float startY, float width)
        {
            Rect statHeaderRect = new Rect(0f, startY, width, 30f); // MODIFIÉ : Plus de hauteur
            GUI.color = color;
            Text.Font = GameFont.Medium;
            Widgets.Label(statHeaderRect, $"=== {title} ===");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return startY + 35f; // MODIFIÉ : Plus d'espacement
        }

        // MODIFIÉ : Plus d'espacement entre les lignes de multiplicateurs
        private float DrawStatMultipliers(string statType, float startY, float width)
        {
            float currentY = startY;
            
            // Obtenir les multiplicateurs pour cette stat
            var multiplierKeys = settings.statMultipliers.Keys.Where(k => k.StartsWith(statType + "_")).OrderBy(k => k).ToList();
            
            foreach (string key in multiplierKeys)
            {
                // CORRIGÉ : Extraire le nom de la stat et utiliser les vraies traductions RimWorld
                string statDefName = key.Substring(statType.Length + 1);
                string statName = GetFriendlyStatName(statDefName);
                float currentValue = settings.GetStatMultiplier(key, 0f);
                
                // Layout en colonnes avec plus de hauteur
                Rect labelRect = new Rect(20f, currentY, width * 0.45f, 25f); // MODIFIÉ : Plus de hauteur
                Rect sliderRect = new Rect(width * 0.5f, currentY + 2f, width * 0.35f, 25f);
                Rect valueRect = new Rect(width * 0.87f, currentY, width * 0.13f, 25f);
                
                // Nom de la stat avec tooltip
                Widgets.Label(labelRect, statName);
                if (Mouse.IsOver(labelRect))
                {
                    TooltipHandler.TipRegion(labelRect, $"Internal key: {key}\nStatDef: {statDefName}");
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
                
                currentY += 28f; // MODIFIÉ : Plus d'espacement entre lignes
            }
            
            return currentY;
        }

        // CORRIGÉ : Ajuster la hauteur calculée avec les nouveaux espacements
        private float GetAdvancedSettingsHeight()
        {
            // Calculer la hauteur nécessaire pour tous les paramètres avancés
            int totalMultipliers = settings.statMultipliers?.Count ?? 0;
            int humanSections = 6; // STR, DEX, AGL, CON, INT, CHA pour humains
            int animalSections = settings.enableAnimalRPGStats ? 6 : 0; // Même chose pour animaux si activé
            
            // MODIFIÉ : Ajuster les calculs avec les nouveaux espacements plus généreux
            float baseHeight = (totalMultipliers * 28f) + ((humanSections + animalSections) * 60f) + 200f;
            
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

        // CORRIGÉ : Méthode GetFriendlyStatName avec traductions
        private string GetFriendlyStatName(string statKey)
        {
            // NOUVEAU : Utiliser directement les StatDefs de RimWorld pour les traductions automatiques
            var statDef = DefDatabase<StatDef>.GetNamedSilentFail(statKey);
            if (statDef != null)
            {
                return statDef.label.CapitalizeFirst();
            }
            
            // Fallback pour les clés personnalisées
            return TranslationHelper.GetStatName(statKey);
        }
    }
}
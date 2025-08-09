using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RPGYourStat
{
    public class MainTabWindow_RPGStats : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private const float RowHeight = 25f;
        private const float StatColumnWidth = 80f;
        private const float NameColumnWidth = 150f;

        public MainTabWindow_RPGStats()
        {
            doCloseButton = true;
            doCloseX = true;
            forcePause = false;
            preventCameraMotion = false;
        }

        public override Vector2 RequestedTabSize => new Vector2(900f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            
            // En-tête
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, TranslationHelper.GetUIText("WindowTitle"));
            Text.Font = GameFont.Small;

            // Zone de contenu avec scroll
            Rect contentRect = new Rect(inRect.x, inRect.y + 50f, inRect.width, inRect.height - 50f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, GetTotalContentHeight());
            
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
            
            float currentY = 0f;
            
            // Dessiner les colons
            var colonists = Find.CurrentMap?.mapPawns?.FreeColonists?.ToList() ?? new List<Pawn>();
            currentY = DrawPawnsSection(TranslationHelper.GetUIText("Colonists"), colonists, currentY, viewRect.width);
            
            currentY += 30f;
            
            // Dessiner les animaux si les stats RPG sont activées pour eux
            if (RPGYourStat_Mod.settings?.enableAnimalRPGStats == true)
            {
                var animals = Find.CurrentMap?.mapPawns?.AllPawns?.Where(p => 
                    p.RaceProps.Animal && 
                    p.Faction == Faction.OfPlayer && 
                    p.GetComp<CompRPGStats>() != null).ToList() ?? new List<Pawn>();
                
                currentY = DrawPawnsSection(TranslationHelper.GetUIText("Animals"), animals, currentY, viewRect.width);
            }
            
            Widgets.EndScrollView();
        }

        private float DrawPawnsSection(string title, List<Pawn> pawns, float startY, float width)
        {
            float currentY = startY;
            
            // Titre de la section
            Rect titleRect = new Rect(10f, currentY, width - 20f, 30f);
            Text.Font = GameFont.Medium;
            GUI.color = Color.cyan;
            Widgets.Label(titleRect, title);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            currentY += 35f;
            
            if (!pawns.Any())
            {
                // MODIFIÉ : Utiliser les traductions
                Rect noDataRect = new Rect(20f, currentY, width - 40f, RowHeight);
                GUI.color = Color.gray;
                
                string message;
                if (title == TranslationHelper.GetUIText("Animals"))
                {
                    if (RPGYourStat_Mod.settings?.enableAnimalRPGStats == true)
                    {
                        message = TranslationHelper.GetUIText("NoAnimals");
                    }
                    else
                    {
                        message = TranslationHelper.GetMessageText("AnimalStatsDisabled");
                    }
                }
                else
                {
                    message = TranslationHelper.GetUIText("NoColonists");
                }
                
                Widgets.Label(noDataRect, message);
                GUI.color = Color.white;
                currentY += RowHeight + 10f;
                return currentY;
            }

            // En-têtes des colonnes
            currentY = DrawColumnHeaders(currentY, width);
            
            // Ligne de séparation sous les en-têtes
            Widgets.DrawLineHorizontal(0f, currentY, width);
            currentY += 5f;
            
            // Dessiner chaque pawn
            foreach (var pawn in pawns)
            {
                currentY = DrawPawnRow(pawn, currentY, width);
                currentY += 2f; // Petit espacement entre les lignes
            }
            
            return currentY;
        }

        private float DrawColumnHeaders(float y, float width)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.yellow;
            
            float currentX = 10f;
            
            // Nom
            Rect nameRect = new Rect(currentX, y, NameColumnWidth, RowHeight);
            Widgets.Label(nameRect, TranslationHelper.GetUIText("Name")); // MODIFIÉ : Utiliser la traduction
            currentX += NameColumnWidth + 10f;
            
            // En-têtes des statistiques avec tooltips des améliorations
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                Rect statRect = new Rect(currentX, y, StatColumnWidth, RowHeight);
                Widgets.Label(statRect, CompRPGStats.GetStatDisplayName(statType));
                
                // Tooltip pour afficher les améliorations de cette stat
                if (Mouse.IsOver(statRect))
                {
                    string tooltip = GetStatImprovementsTooltip(statType);
                    TooltipHandler.TipRegion(statRect, tooltip);
                }
                
                currentX += StatColumnWidth + 5f;
            }
            
            GUI.color = Color.white;
            return y + RowHeight + 5f;
        }

        private float DrawPawnRow(Pawn pawn, float y, float width)
        {
            float currentX = 10f;
            
            // Nom du pawn avec indication du type
            Rect nameRect = new Rect(currentX, y, NameColumnWidth, RowHeight);
            string pawnName = pawn.Name?.ToStringShort ?? TranslationHelper.GetUIText("Unknown"); // MODIFIÉ : Utiliser la traduction
            
            // NOUVEAU : Ajouter un indicateur pour les animaux
            if (pawn.RaceProps.Animal)
            {
                GUI.color = new Color(0.8f, 0.8f, 1.0f); // Bleu clair pour les animaux
                pawnName = $"[{TranslationHelper.GetUIText("AnimalIndicator")}] {pawnName}"; // MODIFIÉ : Utiliser la traduction
            }
            
            Widgets.Label(nameRect, pawnName);
            GUI.color = Color.white;
            currentX += NameColumnWidth + 10f;
            
            var stats = pawn.GetComp<CompRPGStats>();
            if (stats == null)
            {
                // MODIFIÉ : Utiliser les traductions
                Rect noStatsRect = new Rect(currentX, y, StatColumnWidth * 6f, RowHeight);
                GUI.color = Color.red;
                Widgets.Label(noStatsRect, TranslationHelper.GetUIText("MissingRPGComponent"));
                GUI.color = Color.white;
                return y + RowHeight;
            }

            // NOUVEAU : Vérifier si les stats sont activées pour ce type de pawn
            if (pawn.RaceProps.Animal && RPGYourStat_Mod.settings?.enableAnimalRPGStats != true)
            {
                Rect disabledRect = new Rect(currentX, y, StatColumnWidth * 6f, RowHeight);
                GUI.color = Color.yellow;
                Widgets.Label(disabledRect, TranslationHelper.GetMessageText("AnimalStatsDisabled"));
                GUI.color = Color.white;
                return y + RowHeight;
            }

            // Obtenir le classement des stats pour ce pawn
            var statRankings = GetStatRankings(stats);

            // Afficher chaque statistique
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                Rect statRect = new Rect(currentX, y, StatColumnWidth, RowHeight);
                
                var stat = stats.GetStat(statType);
                if (stat != null)
                {
                    int nextLevelExp = stats.GetRequiredExperienceForLevel(stat.level + 1);
                    string levelText = TranslationHelper.GetUIText("Level"); // MODIFIÉ : Utiliser la traduction
                    string statText = $"{levelText}{stat.level}\n({stat.experience:F1}/{nextLevelExp})";
                    
                    // Utiliser la couleur basée sur le classement
                    GUI.color = GetColorByRanking(statRankings, statType);
                    
                    // Affichage avec tooltip
                    Widgets.Label(statRect, statText);
                    
                    // Tooltip avec détails du pawn ET améliorations
                    if (Mouse.IsOver(statRect))
                    {
                        var tooltip = new System.Text.StringBuilder();
                        
                        // Info du pawn et stat
                        tooltip.AppendLine($"{pawn.Name?.ToStringShort ?? TranslationHelper.GetUIText("Unknown")} - {CompRPGStats.GetStatDisplayName(statType)}");
                        tooltip.AppendLine($"{levelText} {stat.level} ({stat.experience:F1}/{nextLevelExp} XP)");
                        tooltip.AppendLine(GetRankingText(statRankings, statType));
                        tooltip.AppendLine();
                        
                        // Bonus actuels
                        tooltip.AppendLine(TranslationHelper.GetTooltipText("CurrentBonuses"));
                        string currentBonuses = StatModifierSystem.GetStatBonusDescription(pawn, statType);
                        if (!string.IsNullOrEmpty(currentBonuses) && currentBonuses != TranslationHelper.GetTooltipText("NoBonuses"))
                        {
                            tooltip.AppendLine(currentBonuses);
                        }
                        else
                        {
                            tooltip.AppendLine(TranslationHelper.GetTooltipText("NoBonuses"));
                        }
                        
                        // Ajouter les améliorations possibles
                        tooltip.AppendLine();
                        tooltip.AppendLine(TranslationHelper.GetTooltipText("PossibleImprovements"));
                        var affectedStats = StatModifierSystem.GetAffectedStats(statType);
                        var bonusMapping = StatModifierSystem.GetStatBonusMapping(statType);
                        
                        foreach (var statDef in affectedStats.Take(5)) // Limiter à 5 pour éviter un tooltip trop long
                        {
                            if (bonusMapping.ContainsKey(statDef))
                            {
                                float bonusPerLevel = bonusMapping[statDef];
                                string sign = bonusPerLevel >= 0 ? "+" : "";
                                string percentage = (bonusPerLevel * 100f).ToString("F1");
                                tooltip.AppendLine($"• {statDef.label}: {sign}{percentage}%/{TranslationHelper.GetTooltipText("PerLevel")}");
                            }
                        }
                        
                        if (affectedStats.Count > 5)
                        {
                            tooltip.AppendLine(TranslationHelper.GetUIText("AndMore").Translate(affectedStats.Count - 5));
                        }
                        
                        TooltipHandler.TipRegion(statRect, tooltip.ToString().TrimEnd());
                    }
                }
                else
                {
                    GUI.color = Color.red;
                    Widgets.Label(statRect, TranslationHelper.GetUIText("Error"));
                }
                
                GUI.color = Color.white;
                currentX += StatColumnWidth + 5f;
            }

            return y + RowHeight;
        }

        // NOUVELLE MÉTHODE : Obtenir le classement des stats pour un pawn
        private Dictionary<StatType, int> GetStatRankings(CompRPGStats stats)
        {
            var rankings = new Dictionary<StatType, int>();
            var statLevels = new Dictionary<StatType, int>();
            
            // Collecter tous les niveaux
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                var stat = stats.GetStat(statType);
                if (stat != null)
                {
                    statLevels[statType] = stat.level;
                }
            }
            
            // Calculer les classements (du plus haut au plus bas)
            var sortedStats = statLevels.OrderByDescending(kvp => kvp.Value).ToList();
            
            for (int i = 0; i < sortedStats.Count; i++)
            {
                rankings[sortedStats[i].Key] = i + 1;
            }
            
            return rankings;
        }

        // NOUVELLE MÉTHODE : Obtenir la couleur selon le classement
        private Color GetColorByRanking(Dictionary<StatType, int> rankings, StatType statType)
        {
            if (!rankings.ContainsKey(statType))
                return Color.white;
                
            int rank = rankings[statType];
            return rank switch
            {
                1 => Color.green,      // 1er = Vert (meilleur)
                2 => Color.cyan,       // 2ème = Cyan
                3 => Color.yellow,     // 3ème = Jaune
                4 => new Color(1f, 0.5f, 0f), // 4ème = Orange
                5 => Color.red,        // 5ème = Rouge
                6 => Color.gray,       // 6ème = Gris (plus faible)
                _ => Color.white
            };
        }

        // Obtenir le tooltip des améliorations pour une stat
        private string GetStatImprovementsTooltip(StatType statType)
        {
            var affectedStats = StatModifierSystem.GetAffectedStats(statType);
            
            if (!affectedStats.Any())
            {
                return TranslationHelper.GetTooltipText("StatImprovements").Translate(
                    CompRPGStats.GetStatDisplayName(statType), 
                    TranslationHelper.GetTooltipText("NoImprovements")
                );
            }

            var improvements = new System.Text.StringBuilder();
            
            // Obtenir les bonus par niveau pour cette stat
            var bonusMapping = StatModifierSystem.GetStatBonusMapping(statType);
            
            foreach (var statDef in affectedStats.OrderBy(s => s.label))
            {
                if (bonusMapping.ContainsKey(statDef))
                {
                    float bonusPerLevel = bonusMapping[statDef];
                    string sign = bonusPerLevel >= 0 ? "+" : "";
                    string percentage = (bonusPerLevel * 100f).ToString("F1");
                    
                    improvements.AppendLine($"• {statDef.label}: {sign}{percentage}% {TranslationHelper.GetTooltipText("PerLevel")}");
                }
            }

            return TranslationHelper.GetTooltipText("StatImprovements").Translate(
                CompRPGStats.GetStatDisplayName(statType), 
                improvements.ToString().TrimEnd()
            );
        }

        // Obtenir le texte de classement pour le tooltip
        private string GetRankingText(Dictionary<StatType, int> rankings, StatType statType)
        {
            if (!rankings.ContainsKey(statType))
                return "";
            
            int rank = rankings[statType];
            return TranslationHelper.GetRankingText(rank);
        }

        private float DrawSeparator(float y, float width)
        {
            y += 10f; // Espacement avant
            
            Rect separatorRect = new Rect(width * 0.1f, y + 10f, width * 0.8f, 2f);
            GUI.color = Color.gray;
            Widgets.DrawBoxSolid(separatorRect, GUI.color);
            GUI.color = Color.white;
            
            return y + 30f; // Espacement après
        }

        private List<Pawn> GetColonists()
        {
            try
            {
                return Find.CurrentMap?.mapPawns?.FreeColonists?.ToList() ?? new List<Pawn>();
            }
            catch
            {
                return new List<Pawn>();
            }
        }

        private List<Pawn> GetAnimals()
        {
            try
            {
                // MODIFIÉ : Ne retourner des animaux que si les stats RPG sont activées pour eux
                if (RPGYourStat_Mod.settings?.enableAnimalRPGStats == true)
                {
                    return Find.CurrentMap?.mapPawns?.PawnsInFaction(Faction.OfPlayer)
                        ?.Where(p => p.RaceProps.Animal)?.ToList() ?? new List<Pawn>();
                }
                else
                {
                    return new List<Pawn>();
                }
            }
            catch
            {
                return new List<Pawn>();
            }
        }

        private float GetTotalContentHeight()
        {
            // Calculer la hauteur approximative nécessaire
            var colonists = GetColonists();
            var animals = GetAnimals();
            
            float baseHeight = 100f; // Headers et espacement
            float colonistHeight = (colonists.Count + 1) * (RowHeight + 2f) + 50f; // +1 pour header
            float animalHeight = RPGYourStat_Mod.settings?.enableAnimalRPGStats == true ? 
                (animals.Count + 1) * (RowHeight + 2f) + 50f : 0f;
            
            return baseHeight + colonistHeight + animalHeight;
        }
    }
}
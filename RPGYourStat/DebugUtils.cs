using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace RPGYourStat
{
    public static class DebugUtils
    {
        public static void LogMessage(string message)
        {
            // Vérifier si le mode de débogage est activé avant d'afficher le message
            if (RPGYourStat_Mod.settings?.debugMode == true)
            {
                Log.Message($"[RPGYourStat] {message}");
            }
        }

        // Nouvelle méthode pour les messages de level up (toujours affichés)
        public static void LogLevelUp(string message)
        {
            Log.Message($"[RPGYourStat] {message}");
        }

        // SUPPRIMÉ : Les commandes de debug qui causaient des erreurs de compilation
        // Ces méthodes peuvent être appelées manuellement si nécessaire

        // Méthode pour afficher le rapport d'équilibrage (accessible via les paramètres du mod)
        public static string GetBalanceReportString()
        {
            return EnemyStatsBalancer.GetBalanceReport();
        }

        // Méthode pour forcer l'équilibrage des ennemis (peut être appelée manuellement)
        public static void ForceBalanceAllEnemies()
        {
            try
            {
                var enemies = Find.CurrentMap?.mapPawns?.AllPawns?
                    .Where(p => p.Faction != null && 
                               p.Faction.HostileTo(Faction.OfPlayer) &&
                               p.GetComp<CompRPGStats>() != null)
                    .ToList() ?? new List<Pawn>();
                
                foreach (var enemy in enemies)
                {
                    EnemyStatsBalancer.BalanceNewPawnStats(enemy);
                }
                
                Messages.Message($"Équilibrage forcé pour {enemies.Count} ennemis", MessageTypeDefOf.PositiveEvent);
                LogMessage($"Équilibrage forcé effectué pour {enemies.Count} ennemis");
            }
            catch (System.Exception ex)
            {
                LogMessage($"Erreur lors de l'équilibrage forcé: {ex.Message}");
            }
        }

        // Méthode pour créer un ennemi équilibré (peut être appelée manuellement)
        public static void CreateBalancedEnemy()
        {
            try
            {
                // Trouver une faction hostile
                var hostileFaction = Find.FactionManager.AllFactions
                    .FirstOrDefault(f => f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction);
                
                if (hostileFaction == null)
                {
                    Messages.Message("Aucune faction hostile trouvée", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                // Générer un pawn ennemi
                var request = new PawnGenerationRequest(
                    PawnKindDefOf.Villager,
                    hostileFaction,
                    PawnGenerationContext.NonPlayer);
                    
                var enemy = PawnGenerator.GeneratePawn(request);
                
                // Le placer dans une zone libre proche
                IntVec3 spawnCell;
                if (CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(Find.CurrentMap) && !c.Fogged(Find.CurrentMap), 
                    Find.CurrentMap, CellFinder.EdgeRoadChance_Neutral, out spawnCell))
                {
                    GenSpawn.Spawn(enemy, spawnCell, Find.CurrentMap);
                    
                    // L'équilibrage se fera automatiquement via le patch PawnGenerator
                    Messages.Message($"Ennemi équilibré généré: {enemy.Name?.ToStringShort}", MessageTypeDefOf.PositiveEvent);
                    LogMessage($"Ennemi équilibré généré: {enemy.Name?.ToStringShort}");
                }
                else
                {
                    Messages.Message("Impossible de trouver une zone de spawn", MessageTypeDefOf.RejectInput);
                }
            }
            catch (System.Exception ex)
            {
                LogMessage($"Erreur lors de la création d'ennemi: {ex.Message}");
                Messages.Message($"Erreur lors de la création d'ennemi: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        // Méthode pour donner de l'XP de test aux colons
        public static void GiveTestExperience()
        {
            try
            {
                var colonists = Find.CurrentMap?.mapPawns?.FreeColonists?.ToList();
                if (colonists == null || !colonists.Any())
                {
                    Messages.Message("Aucun colon trouvé", MessageTypeDefOf.RejectInput);
                    return;
                }

                foreach (var colonist in colonists)
                {
                    var comp = colonist.GetComp<CompRPGStats>();
                    if (comp != null)
                    {
                        comp.GiveTestExperience();
                    }
                }

                Messages.Message($"XP de test donnée à {colonists.Count} colons", MessageTypeDefOf.PositiveEvent);
                LogMessage($"XP de test donnée à {colonists.Count} colons");
            }
            catch (System.Exception ex)
            {
                LogMessage($"Erreur lors du don d'XP de test: {ex.Message}");
            }
        }

        // Méthode pour afficher les statistiques détaillées d'un pawn
        public static void ShowPawnStats(Pawn pawn)
        {
            if (pawn == null) return;

            var comp = pawn.GetComp<CompRPGStats>();
            if (comp == null)
            {
                Messages.Message($"{pawn.Name?.ToStringShort ?? "Pawn"} n'a pas de composant RPG", MessageTypeDefOf.RejectInput);
                return;
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== STATS RPG DE {pawn.Name?.ToStringShort?.ToUpper() ?? "UNKNOWN"} ===");
            
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                var stat = comp.GetStat(statType);
                if (stat != null)
                {
                    int nextLevelExp = comp.GetRequiredExperienceForLevel(stat.level + 1);
                    report.AppendLine($"{statType}: Niveau {stat.level} ({stat.experience:F1}/{nextLevelExp} XP)");
                }
            }

            // Afficher les bonus actuels
            report.AppendLine();
            report.AppendLine("=== BONUS ACTUELS ===");
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                string bonusDesc = StatModifierSystem.GetStatBonusDescription(pawn, statType);
                if (!string.IsNullOrEmpty(bonusDesc) && bonusDesc != "Aucun bonus")
                {
                    report.AppendLine($"{statType}:");
                    report.AppendLine(bonusDesc);
                    report.AppendLine();
                }
            }

            Log.Message(report.ToString());
            
            if (Find.WindowStack != null)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(report.ToString(), "Fermer"));
            }
        }
    }
}
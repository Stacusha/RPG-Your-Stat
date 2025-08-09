using System;
using System.Collections.Generic;
using System.Linq; // AJOUTÉ : Pour les méthodes LINQ
using RimWorld;
using Verse;
using UnityEngine;

namespace RPGYourStat
{
    public static class EnemyStatsBalancer
    {
        // Cache pour éviter de recalculer constamment
        private static float cachedColonyPower = 0f;
        private static int lastUpdateTick = 0;
        private const int UpdateInterval = 2500; // Mettre à jour toutes les ~42 secondes

        public static void BalanceNewPawnStats(Pawn pawn)
        {
            if (pawn?.Faction == null) return;
            
            // CORRIGÉ : Utiliser une vérification similaire
            if (!IsGameReady()) return;
            
            // Vérifier si l'équilibrage automatique est activé
            if (RPGYourStat_Mod.settings?.enableAutoBalance != true) return;
            
            var playerFaction = Faction.OfPlayer;
            if (playerFaction == null) return;
            
            // Ne pas équilibrer les colons du joueur
            if (pawn.Faction == playerFaction) return;
            
            var comp = pawn.GetComp<CompRPGStats>();
            if (comp == null) return;

            try
            {
                if (pawn.Faction.HostileTo(playerFaction))
                {
                    // Ennemi : équilibrer selon la puissance de la colonie
                    BalanceEnemyStats(comp, pawn);
                }
                else if (pawn.Faction.PlayerRelationKind == FactionRelationKind.Ally)
                {
                    // Allié : équilibrer selon la richesse de leur faction
                    BalanceAllyStats(comp, pawn);
                }
                
                DebugUtils.LogMessage($"Stats équilibrées pour {pawn.Name?.ToStringShort ?? "Unknown"} ({pawn.Faction?.Name ?? "No Faction"})");
            }
            catch (Exception ex)
            {
                DebugUtils.LogMessage($"Erreur lors de l'équilibrage des stats pour {pawn.Name?.ToStringShort ?? "Unknown"}: {ex.Message}");
            }
        }

        // NOUVELLE MÉTHODE : Vérifier si le jeu est prêt
        private static bool IsGameReady()
        {
            try
            {
                if (Current.Game == null) return false;
                if (Find.FactionManager == null) return false;
                
                // Vérifier que la faction du joueur existe sans lever d'exception
                var factionManager = Find.FactionManager;
                if (factionManager.AllFactions == null) return false;
                
                var playerFaction = factionManager.AllFactions.FirstOrDefault(f => f != null && f.IsPlayer);
                return playerFaction != null;
            }
            catch
            {
                return false;
            }
        }

        private static void BalanceEnemyStats(CompRPGStats comp, Pawn enemy)
        {
            float colonyPower = GetColonyAverageStatLevel();
            if (colonyPower <= 0f) return;

            // Calculer la puissance cible pour cet ennemi (avec variation)
            float difficultyMultiplier = GetDifficultyMultiplier();
            float threatMultiplier = GetThreatMultiplier(enemy);
            float settingsMultiplier = RPGYourStat_Mod.settings?.enemyBalanceMultiplier ?? 1.0f;
            float targetPower = colonyPower * difficultyMultiplier * threatMultiplier * settingsMultiplier;

            // Ajouter de la variation aléatoire (±20%)
            float randomVariation = Rand.Range(0.8f, 1.2f);
            targetPower *= randomVariation;

            // Distribuer la puissance sur les 6 stats
            DistributeStatsEvenly(comp, targetPower, enemy);
        }

        private static void BalanceAllyStats(CompRPGStats comp, Pawn ally)
        {
            float factionWealth = GetFactionWealth(ally.Faction);
            float baseLevel = Mathf.Clamp(factionWealth / 10000f, 1f, 10f); // 1-10 niveaux selon richesse
            float settingsMultiplier = RPGYourStat_Mod.settings?.allyBalanceMultiplier ?? 1.0f;
            
            // Variation aléatoire pour les alliés (±30% pour plus de diversité)
            float randomVariation = Rand.Range(0.7f, 1.3f);
            float targetPower = baseLevel * 6f * randomVariation * settingsMultiplier; // 6 stats

            DistributeStatsEvenly(comp, targetPower, ally);
        }

        private static void DistributeStatsEvenly(CompRPGStats comp, float totalTargetPower, Pawn pawn)
        {
            var statTypes = Enum.GetValues(typeof(StatType)).Cast<StatType>().ToArray();
            
            // Créer des poids selon le type de pawn
            var weights = GetStatWeights(pawn);
            float totalWeight = weights.Values.Sum();
            
            foreach (var statType in statTypes)
            {
                // Calculer le niveau cible pour cette stat
                // CORRIGÉ : Utiliser TryGetValue au lieu de GetValueOrDefault
                float statWeight = 1f;
                if (weights.TryGetValue(statType, out float value))
                {
                    statWeight = value;
                }
                
                float statPortion = statWeight / totalWeight;
                float targetStatPower = totalTargetPower * statPortion;
                
                // Convertir en niveau (minimum 1)
                int targetLevel = Mathf.Max(1, Mathf.RoundToInt(targetStatPower));
                
                // Appliquer le niveau
                SetStatToLevel(comp, statType, targetLevel);
            }
        }

        private static Dictionary<StatType, float> GetStatWeights(Pawn pawn)
        {
            var weights = new Dictionary<StatType, float>();
            
            if (pawn.RaceProps.Animal)
            {
                // Animaux : privilégier AGI et CON
                weights[StatType.STR] = 0.8f;
                weights[StatType.DEX] = 0.6f;
                weights[StatType.AGL] = 1.4f;
                weights[StatType.CON] = 1.3f;
                weights[StatType.INT] = 0.4f;
                weights[StatType.CHA] = 0.5f;
            }
            else
            {
                // Humains : analyser leur rôle
                if (IsCombatPawn(pawn))
                {
                    // Combattants : STR, DEX, AGL
                    weights[StatType.STR] = 1.3f;
                    weights[StatType.DEX] = 1.2f;
                    weights[StatType.AGL] = 1.1f;
                    weights[StatType.CON] = 1.0f;
                    weights[StatType.INT] = 0.7f;
                    weights[StatType.CHA] = 0.7f;
                }
                else if (IsIntellectualPawn(pawn))
                {
                    // Intellectuels : INT, CHA
                    weights[StatType.STR] = 0.7f;
                    weights[StatType.DEX] = 0.8f;
                    weights[StatType.AGL] = 0.8f;
                    weights[StatType.CON] = 0.9f;
                    weights[StatType.INT] = 1.4f;
                    weights[StatType.CHA] = 1.4f;
                }
                else
                {
                    // Équilibré
                    weights[StatType.STR] = 1.0f;
                    weights[StatType.DEX] = 1.0f;
                    weights[StatType.AGL] = 1.0f;
                    weights[StatType.CON] = 1.0f;
                    weights[StatType.INT] = 1.0f;
                    weights[StatType.CHA] = 1.0f;
                }
            }
            
            return weights;
        }

        private static bool IsCombatPawn(Pawn pawn)
        {
            // Vérifier si le pawn a des compétences de combat élevées
            if (pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level >= 8) return true;
            if (pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level >= 8) return true;
            
            // Vérifier l'équipement
            if (pawn.equipment?.Primary?.def.IsWeapon == true) return true;
            
            return false;
        }

        private static bool IsIntellectualPawn(Pawn pawn)
        {
            if (pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level >= 8) return true;
            if (pawn.skills?.GetSkill(SkillDefOf.Medicine)?.Level >= 8) return true;
            if (pawn.skills?.GetSkill(SkillDefOf.Social)?.Level >= 8) return true;
            
            return false;
        }

        private static void SetStatToLevel(CompRPGStats comp, StatType statType, int targetLevel)
        {
            var stat = comp.GetStat(statType);
            if (stat == null) return;

            if (targetLevel > stat.level)
            {
                // Augmenter le niveau
                stat.level = targetLevel;
                stat.experience = 0f; // Remettre l'XP à 0 pour ce niveau
            }
        }

        private static float GetColonyAverageStatLevel()
        {
            // CORRIGÉ : Utiliser la même vérification
            if (!IsGameReady() || Find.CurrentMap == null) 
            {
                return 1f; // Valeur par défaut
            }
            
            // Utiliser le cache si récent
            if (Find.TickManager.TicksGame - lastUpdateTick < UpdateInterval && cachedColonyPower > 0f)
            {
                return cachedColonyPower;
            }

            var colonists = Find.CurrentMap?.mapPawns?.FreeColonists?.ToList();
            if (colonists == null || !colonists.Any())
            {
                cachedColonyPower = 1f;
                return cachedColonyPower;
            }

            float totalLevels = 0f;
            int totalStats = 0;

            foreach (var colonist in colonists)
            {
                var comp = colonist.GetComp<CompRPGStats>();
                if (comp == null) continue;

                foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                {
                    var stat = comp.GetStat(statType);
                    if (stat != null)
                    {
                        totalLevels += stat.level;
                        totalStats++;
                    }
                }
            }

            cachedColonyPower = totalStats > 0 ? totalLevels / totalStats : 1f;
            lastUpdateTick = Find.TickManager.TicksGame;
            
            DebugUtils.LogMessage($"Puissance moyenne de la colonie calculée: {cachedColonyPower:F1}");
            return cachedColonyPower;
        }

        private static float GetDifficultyMultiplier()
        {
            // Ajuster selon la difficulté
            var storytellerDifficulty = Find.Storyteller?.difficulty;
            if (storytellerDifficulty == null) return 1f;

            // CORRIGÉ : Utiliser threatScale au lieu de difficulty
            return storytellerDifficulty.threatScale switch
            {
                <= 0.5f => 0.8f,  // Facile
                <= 1.0f => 1.0f,  // Normal
                <= 1.5f => 1.2f,  // Difficile
                <= 2.0f => 1.4f,  // Très difficile
                _ => 1.6f         // Extrême
            };
        }

        private static float GetThreatMultiplier(Pawn pawn)
        {
            // Multiplicateur selon le type d'ennemi
            if (pawn.RaceProps.Animal)
            {
                float bodySize = pawn.RaceProps.baseBodySize;
                return Mathf.Clamp(bodySize, 0.5f, 2.0f);
            }

            // Humains
            if (pawn.equipment?.Primary?.def.IsRangedWeapon == true)
                return 1.1f; // Légèrement plus fort s'ils ont des armes à distance
            
            if (pawn.apparel?.WornApparel?.Any(a => a.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso)) == true)
                return 1.05f; // Légèrement plus fort s'ils ont une armure

            return 1f;
        }

        private static float GetFactionWealth(Faction faction)
        {
            if (faction == null) return 5000f; // Valeur par défaut

            try
            {
                // Approximation basée sur les relations et la taille de la faction
                float baseWealth = 5000f;
                
                // Bonus selon le goodwill
                if (faction.PlayerGoodwill > 50)
                    baseWealth += faction.PlayerGoodwill * 50f;
                
                // Bonus selon le type de faction (si accessible)
                if (faction.def?.techLevel != null)
                {
                    baseWealth *= faction.def.techLevel switch
                    {
                        TechLevel.Neolithic => 0.5f,
                        TechLevel.Medieval => 0.7f,
                        TechLevel.Industrial => 1.0f,
                        TechLevel.Spacer => 1.5f,
                        TechLevel.Ultra => 2.0f,
                        _ => 1.0f
                    };
                }

                return Mathf.Clamp(baseWealth, 1000f, 50000f);
            }
            catch
            {
                return 5000f;
            }
        }

        // NOUVELLE MÉTHODE : Pour les tests et le debug
        public static string GetBalanceReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine(TranslationHelper.GetBalanceText("ReportTitle"));
            
            var playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                report.AppendLine(TranslationHelper.GetBalanceText("PlayerFactionNotFound"));
                return report.ToString();
            }
            
            float colonyPower = GetColonyAverageStatLevel();
            report.AppendLine(TranslationHelper.GetBalanceText("ColonyPower").Translate(colonyPower));
            
            var difficulty = GetDifficultyMultiplier();
            report.AppendLine(TranslationHelper.GetBalanceText("DifficultyMultiplier").Translate(difficulty));
            
            // Statistiques des ennemis récents
            var allPawns = Find.CurrentMap?.mapPawns?.AllPawns?.Where(p => 
                p.Faction != playerFaction && 
                p.GetComp<CompRPGStats>() != null).ToList() ?? new List<Pawn>();
                
            if (allPawns.Any())
            {
                float enemyAverage = 0f;
                int count = 0;
                
                foreach (var pawn in allPawns.Take(10)) // Limiter à 10 pour éviter le spam
                {
                    var comp = pawn.GetComp<CompRPGStats>();
                    if (comp != null)
                    {
                        float pawnPower = 0f;
                        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                        {
                            var stat = comp.GetStat(statType);
                            if (stat != null) pawnPower += stat.level;
                        }
                        enemyAverage += pawnPower / 6f; // Moyenne sur 6 stats
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    enemyAverage /= count;
                    report.AppendLine($"Puissance moyenne ennemis: {enemyAverage:F1}");
                    report.AppendLine($"Ratio colonie/ennemis: {(colonyPower / enemyAverage):F2}");
                }
            }
            
            return report.ToString();
        }
    }
}
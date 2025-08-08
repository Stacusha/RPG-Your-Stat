using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace RPGYourStat
{
    public enum StatType
    {
        STR, // Force
        DEX, // Dextérité
        AGL, // Agilité
        CON, // Constitution
        INT, // Intelligence
        CHA  // Charisme
    }

    public class RPGStat
    {
        public int level = 1;
        public int experience = 0;
        public StatType type;

        public RPGStat(StatType statType)
        {
            type = statType;
        }

        public void ExposeData(string prefix)
        {
            Scribe_Values.Look(ref level, $"{prefix}_level", 1);
            Scribe_Values.Look(ref experience, $"{prefix}_experience", 0);
        }
    }

    public class CompRPGStats : ThingComp
    {
        private Dictionary<StatType, RPGStat> stats = new Dictionary<StatType, RPGStat>();
        
        private const int BaseExperienceRequired = 1000;

        public CompPropertiesRPGStats Props => (CompPropertiesRPGStats)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeStats();
        }

        private void InitializeStats()
        {
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                if (!stats.ContainsKey(statType))
                {
                    stats[statType] = new RPGStat(statType);
                }
            }
        }

        public RPGStat GetStat(StatType statType)
        {
            InitializeStats(); // S'assurer que les stats sont initialisées
            return stats.TryGetValue(statType, out RPGStat stat) ? stat : null;
        }

        public int GetStatLevel(StatType statType)
        {
            var stat = GetStat(statType);
            return stat?.level ?? 1;
        }

        public int GetStatExperience(StatType statType)
        {
            var stat = GetStat(statType);
            return stat?.experience ?? 0;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            
            // Initialiser les stats si nécessaire
            if (Scribe.mode == LoadSaveMode.LoadingVars || stats == null)
            {
                if (stats == null)
                    stats = new Dictionary<StatType, RPGStat>();
                InitializeStats();
            }

            // Sauvegarder/charger chaque statistique individuellement
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                if (!stats.ContainsKey(statType))
                    stats[statType] = new RPGStat(statType);
                
                stats[statType].ExposeData(statType.ToString());
            }
        }

        public void AddExperience(StatType statType, int amount)
        {
            if (amount <= 0) return;

            var stat = GetStat(statType);
            if (stat == null) return;

            stat.experience += amount;
            DebugUtils.LogMessage($"{parent.Label} gagne {amount} XP en {statType} (Total: {stat.experience})");
            
            CheckLevelUp(statType);
        }

        public int GetRequiredExperienceForLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0;

            // Système cumulatif :
            // Niveau 2 : 1000 XP total
            // Niveau 3 : 1000 + (1000 * 2) = 3000 XP total
            // Niveau 4 : 3000 + (1000 * 3) = 6000 XP total
            // Niveau 5 : 6000 + (1000 * 4) = 10000 XP total
            // etc.
            
            int totalExp = 0;
            for (int level = 2; level <= targetLevel; level++)
            {
                if (level == 2)
                {
                    totalExp += BaseExperienceRequired; // 1000 pour le niveau 2
                }
                else
                {
                    totalExp += BaseExperienceRequired * (level - 1); // 1000 * (niveau - 1)
                }
            }
            
            return totalExp;
        }

        private void CheckLevelUp(StatType statType)
        {
            var stat = GetStat(statType);
            if (stat == null) return;

            int requiredExp = GetRequiredExperienceForLevel(stat.level + 1);
            if (stat.experience >= requiredExp)
            {
                stat.level++;
                DebugUtils.LogMessage($"{parent.Label} monte au niveau {stat.level} en {statType}!");
                
                // Notification de level up
                if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer)
                {
                    Messages.Message($"{pawn.Name?.ToStringShort ?? "Pawn"} monte au niveau {stat.level} en {GetStatDisplayName(statType)}!", 
                        MessageTypeDefOf.PositiveEvent);
                }
                
                // Vérifier si un autre level up est possible
                CheckLevelUp(statType);
            }
        }

        public static string GetStatDisplayName(StatType statType)
        {
            return statType switch
            {
                StatType.STR => "Force",
                StatType.DEX => "Dextérité",
                StatType.AGL => "Agilité",
                StatType.CON => "Constitution",
                StatType.INT => "Intelligence",
                StatType.CHA => "Charisme",
                _ => statType.ToString()
            };
        }

        public override string CompInspectStringExtra()
        {
            if (stats == null || stats.Count == 0)
            {
                InitializeStats();
            }

            var result = new System.Text.StringBuilder();
            result.AppendLine("=== Statistiques RPG ===");
            
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                var stat = GetStat(statType);
                if (stat != null)
                {
                    int nextLevelExp = GetRequiredExperienceForLevel(stat.level + 1);
                    int expNeeded = nextLevelExp - stat.experience;
                    
                    result.AppendLine($"{GetStatDisplayName(statType)}: Niv.{stat.level} ({stat.experience}/{nextLevelExp} XP)");
                }
            }
            
            return result.ToString().TrimEnd();
        }

        // Ajouter cette méthode pour tester le gain d'expérience
        public void GiveTestExperience()
        {
            // Donner plus d'expérience pour tester le nouveau système
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                AddExperience(statType, UnityEngine.Random.Range(100, 500)); // Plus d'XP pour le test
            }
        }
    }

    public class CompPropertiesRPGStats : CompProperties
    {
        public CompPropertiesRPGStats()
        {
            compClass = typeof(CompRPGStats);
        }
    }
}
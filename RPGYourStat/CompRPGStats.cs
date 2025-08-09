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

    public class RPGStat : IExposable
    {
        public StatType type;
        public int level = 1;
        public float experience = 0f;

        public RPGStat() { }

        public RPGStat(StatType statType)
        {
            type = statType;
            level = 1;
            experience = 0f;
        }

        // CORRIGÉ : Implémentation correcte d'IExposable
        public void ExposeData()
        {
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref experience, "experience", 0f);
            Scribe_Values.Look(ref type, "type", StatType.STR);
        }

        // NOUVELLE MÉTHODE : Pour sauvegarder avec un label spécifique
        public void ExposeDataWithLabel(string label)
        {
            Scribe_Values.Look(ref level, $"{label}_level", 1);
            Scribe_Values.Look(ref experience, $"{label}_experience", 0f);
        }
    }

    public class CompRPGStats : ThingComp
    {
        private Dictionary<StatType, RPGStat> stats;
        private const int BaseExperienceRequired = 1000;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeStats();
            ApplyRPGBonusHediff();
        }

        private void ApplyRPGBonusHediff()
        {
            if (parent is Pawn pawn)
            {
                // Vérifier si le hediff existe déjà
                var existingHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("RPGStatBonus", false));
                if (existingHediff == null)
                {
                    // Ajouter le hediff de bonus RPG
                    var hediffDef = DefDatabase<HediffDef>.GetNamed("RPGStatBonus", false);
                    if (hediffDef != null)
                    {
                        var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                        pawn.health.AddHediff(hediff);
                    }
                }
            }
        }

        private void InitializeStats()
        {
            if (stats == null)
                stats = new Dictionary<StatType, RPGStat>();

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

        public float GetStatExperience(StatType statType)
        {
            var stat = GetStat(statType);
            return stat?.experience ?? 0f;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            
            // MODIFIÉ : Nouvelle approche pour la sauvegarde
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Mode sauvegarde : s'assurer que les stats existent
                InitializeStats();
            }
            
            if (stats == null)
            {
                stats = new Dictionary<StatType, RPGStat>();
                InitializeStats();
            }

            // CORRIGÉ : Utiliser la nouvelle méthode avec label
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                if (!stats.ContainsKey(statType))
                    stats[statType] = new RPGStat(statType);
                
                stats[statType].ExposeDataWithLabel(statType.ToString());
            }

            // Réappliquer le hediff après le chargement
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ApplyRPGBonusHediff();
            }
        }

        public void AddExperience(StatType statType, float amount)
        {
            var stat = GetStat(statType);
            if (stat != null)
            {
                stat.experience += amount;
                CheckLevelUp(statType);
            }
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
                stat.experience = 0f;
                stat.level++;
                
                // CORRIGÉ : Utiliser la nouvelle signature avec 3 paramètres et traductions
                string pawnName = parent?.Label ?? TranslationHelper.GetUIText("Unknown");
                string statName = GetStatDisplayName(statType);
                DebugUtils.LogLevelUp(pawnName, statName, stat.level);
                
                // MODIFIÉ : Notification de level up avec traductions
                if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer)
                {
                    string message = TranslationHelper.GetLevelUpMessage(
                        pawn.Name?.ToStringShort ?? TranslationHelper.GetUIText("Unknown"),
                        GetStatDisplayName(statType),
                        stat.level
                    );
                    Messages.Message(message, MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        // Remplacer la méthode GetStatDisplayName dans CompRPGStats
        public static string GetStatDisplayName(StatType statType)
        {
            return TranslationHelper.GetStatDisplayName(statType);
        }

        // Modifier la méthode CompInspectStringExtra
        public override string CompInspectStringExtra()
        {
            if (stats == null || stats.Count == 0)
            {
                InitializeStats();
            }

            var lines = new System.Collections.Generic.List<string>();
            
            // NOUVEAU : Afficher l'activité actuelle en premier si c'est un pawn
            if (parent is Pawn pawn)
            {
                string currentActivity = GetCurrentActivity(pawn);
                if (!string.IsNullOrEmpty(currentActivity))
                {
                    lines.Add(TranslationHelper.GetUIText("CurrentActivity"));
                    lines.Add(currentActivity);
                }
            }
            
            // Ajouter les statistiques RPG
            lines.Add(TranslationHelper.GetUIText("RPGStatsHeader"));
            
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                var stat = GetStat(statType);
                if (stat != null)
                {
                    int nextLevelExp = GetRequiredExperienceForLevel(stat.level + 1);
                    string levelText = TranslationHelper.GetUIText("Level");
                    lines.Add($"{GetStatDisplayName(statType)}: {levelText}{stat.level} ({stat.experience:F1}/{nextLevelExp} XP)");
                }
            }
            
            return string.Join("\n", lines);
        }

        // Ajouter cette méthode pour tester le gain d'expérience
        public void GiveTestExperience()
        {
            // Donner plus d'expérience pour tester le nouveau système
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                AddExperience(statType, UnityEngine.Random.Range(100f, 500f));
            }
        }

        // NOUVELLE MÉTHODE : Détecter l'activité actuelle du pawn
        private string GetCurrentActivity(Pawn pawn)
        {
            try
            {
                if (pawn?.CurJob?.def == null)
                    return TranslationHelper.GetActivityText("Wait");

                string jobDefName = pawn.CurJob.def.defName;
                
                // Traduire les activités principales
                return jobDefName switch
                {
                    "Wait" or "Wait_Downed" or "Wait_MaintainPosture" => TranslationHelper.GetActivityText("Wait"),
                    "Hunt" => TranslationHelper.GetActivityText("Hunt"),
                    "Mine" => TranslationHelper.GetActivityText("Mine"),
                    "Construct" or "ConstructRoof" or "PlaceBlueprint" => TranslationHelper.GetActivityText("Construct"),
                    "Cook" or "CookMeal" or "DoBill" when pawn.CurJob.bill?.recipe?.defName?.Contains("Cook") == true => TranslationHelper.GetActivityText("Cook"),
                    "Research" => TranslationHelper.GetActivityText("Research"),
                    "TendPatient" or "DeliverFood" when pawn.CurJob.targetA.Thing is Pawn => TranslationHelper.GetActivityText("Medical"),
                    "SocialRelax" or "Chitchat" or "DeepTalk" => TranslationHelper.GetActivityText("Social"),
                    // CORRIGÉ : Supprimer la référence à pawn.CurJob.verb qui n'existe pas
                    "AttackMelee" or "AttackStatic" or "UseVerbOnThing" => TranslationHelper.GetActivityText("Combat"),
                    "HaulToCell" or "HaulToContainer" => GetHaulingDescription(pawn),
                    _ => GetFriendlyJobName(jobDefName)
                };
            }
            catch
            {
                return TranslationHelper.GetActivityText("Wait");
            }
        }

        // NOUVELLE MÉTHODE : Description détaillée pour le transport
        private string GetHaulingDescription(Pawn pawn)
        {
            try
            {
                if (pawn?.CurJob?.targetA.Thing != null)
                {
                    var item = pawn.CurJob.targetA.Thing;
                    float weight = item.GetStatValue(StatDefOf.Mass);
                    return TranslationHelper.GetActivityText("Hauling").Translate($"{item.def.label} ({weight:F1}kg)");
                }
                return TranslationHelper.GetActivityText("Hauling").Translate(TranslationHelper.GetUIText("UnknownItem"));
            }
            catch
            {
                return TranslationHelper.GetActivityText("Hauling").Translate(TranslationHelper.GetUIText("UnknownItem"));
            }
        }

        // NOUVELLE MÉTHODE : Convertir les noms de jobs en traductions
        private string GetFriendlyJobName(string jobDefName)
        {
            string translationKey = $"RPGStats.Job.{jobDefName}";
            
            // Essayer la traduction spécifique
            if (translationKey.CanTranslate())
            {
                return translationKey.Translate();
            }
            
            // Sinon utiliser une traduction générique basée sur les patterns courants
            return jobDefName switch
            {
                var name when name.Contains("Plant") => TranslationHelper.GetActivityText("Farming"),
                var name when name.Contains("Clean") => TranslationHelper.GetActivityText("Cleaning"),
                var name when name.Contains("Repair") => TranslationHelper.GetActivityText("Repairing"),
                var name when name.Contains("Rescue") => TranslationHelper.GetActivityText("Rescuing"),
                var name when name.Contains("Capture") => TranslationHelper.GetActivityText("Capturing"),
                var name when name.Contains("Strip") => TranslationHelper.GetActivityText("Stripping"),
                var name when name.Contains("Equip") => TranslationHelper.GetActivityText("Equipping"),
                var name when name.Contains("Eat") => TranslationHelper.GetActivityText("Eating"),
                var name when name.Contains("Sleep") => TranslationHelper.GetActivityText("Sleeping"),
                var name when name.Contains("Recreation") => TranslationHelper.GetActivityText("Recreation"),
                _ => TranslationHelper.GetActivityText("Working") // Activité générique
            };
        }
    }
}
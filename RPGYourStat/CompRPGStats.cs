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
        public float experience = 0f;
        public StatType type;

        public RPGStat(StatType statType)
        {
            type = statType;
        }

        public void ExposeData(string prefix)
        {
            Scribe_Values.Look(ref level, $"{prefix}_level", 1);
            Scribe_Values.Look(ref experience, $"{prefix}_experience", 0f);
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
                        // Supprimé le message de debug
                    }
                }
            }
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

        public float GetStatExperience(StatType statType)
        {
            var stat = GetStat(statType);
            return stat?.experience ?? 0f;
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

            // Réappliquer le hediff après le chargement
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ApplyRPGBonusHediff();
            }
        }

        public void AddExperience(StatType statType, float amount)
        {
            if (amount <= 0) return;

            var stat = GetStat(statType);
            if (stat == null) return;

            stat.experience += amount;
            
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
                stat.experience = 0f;
                stat.level++;
                // GARDÉ : Message de level up (toujours affiché)
                DebugUtils.LogLevelUp($"{parent.Label} monte au niveau {stat.level} en {statType}!");
                
                // MODIFIÉ : Notification de level up simplifiée (sans description des bonus)
                if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer)
                {
                    Messages.Message($"{pawn.Name?.ToStringShort ?? "Pawn"} monte au niveau {stat.level} en {GetStatDisplayName(statType)}!", 
                        MessageTypeDefOf.PositiveEvent);
                }
                
                // Ne plus vérifier de level up supplémentaire car l'XP est remise à 0
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

            var lines = new System.Collections.Generic.List<string>();
            
            // NOUVEAU : Afficher l'activité actuelle en premier si c'est un pawn
            if (parent is Pawn pawn)
            {
                string currentActivity = GetCurrentActivity(pawn);
                if (!string.IsNullOrEmpty(currentActivity))
                {
                    lines.Add("=== ACTIVITÉ ACTUELLE ===");
                    lines.Add(currentActivity);
                }
            }
            
            // Ajouter les statistiques RPG
            lines.Add("=== Statistiques RPG ===");
            
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                var stat = GetStat(statType);
                if (stat != null)
                {
                    // MODIFIÉ : Utiliser la méthode existante pour afficher l'XP requise pour le niveau suivant
                    int nextLevelExp = GetRequiredExperienceForLevel(stat.level + 1);
                    
                    // MODIFIÉ : Affichage simplifié sans les bonus
                    lines.Add($"{GetStatDisplayName(statType)}: Niv.{stat.level} ({stat.experience:F1}/{nextLevelExp} XP)");
                }
            }
            
            // Joindre toutes les lignes sans lignes vides
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
                {
                    if (pawn?.mindState?.IsIdle == true)
                        return "🏃 Inactif";
                    return "🤔 Activité inconnue";
                }

                var jobDef = pawn.CurJob.def;
                string jobDefName = jobDef.defName;
                
                // NOUVEAU : Détection des activités avec icônes et descriptions
                return jobDefName switch
                {
                    // === TRAVAIL ET CONSTRUCTION ===
                    var job when job.Contains("Construct") => "🔨 Construction",
                    var job when job.Contains("Build") => "🔧 Construction",
                    var job when job.Contains("Repair") => "🔧 Réparation",
                    var job when job.Contains("Mine") => "⛏️ Minage",
                    var job when job.Contains("Smooth") => "🏗️ Lissage",
                    var job when job.Contains("CleanFilth") => "🧹 Nettoyage",
                    
                    // === AGRICULTURE ===
                    var job when job.Contains("Plant") => "🌱 Plantation",
                    var job when job.Contains("Harvest") => "🌾 Récolte",
                    var job when job.Contains("Cut") => "🪓 Coupage",
                    var job when job.Contains("Sow") => "🌱 Semence",
                    
                    // === COMBAT ET CHASSE ===
                    var job when job.Contains("Hunt") => "🏹 Chasse",
                    var job when job.Contains("Attack") => "⚔️ Combat",
                    var job when job.Contains("Fight") => "⚔️ Combat",
                    var job when job.Contains("Flee") => "🏃 Fuite",
                    
                    // === SOINS MÉDICAUX ===
                    var job when job.Contains("TendPatient") => "🏥 Soins médicaux",
                    var job when job.Contains("Surgery") => "🔬 Chirurgie",
                    var job when job.Contains("Rescue") => "🚑 Sauvetage",
                    
                    // === TRANSPORT ===
                    var job when job.Contains("Haul") => GetHaulingDescription(pawn),
                    var job when job.Contains("Carry") => "📦 Transport",
                    var job when job.Contains("TakeInventory") => "📦 Collecte",
                    
                    // === CRAFTING ET CUISINE ===
                    var job when job.Contains("Cook") => "🍳 Cuisine",
                    var job when job.Contains("DoBill") => GetCraftingDescription(pawn),
                    var job when job.Contains("Make") => "🔨 Fabrication",
                    
                    // === SOCIAL ===
                    var job when job.Contains("Social") => "💬 Interaction sociale",
                    var job when job.Contains("Chat") => "💬 Discussion",
                    var job when job.Contains("Recruit") => "🤝 Recrutement",
                    
                    // === ANIMAUX SPÉCIFIQUES ===
                    var job when job.Contains("Train") => GetTrainingDescription(pawn),
                    var job when job.Contains("Tame") => "🐕 Apprivoisement",
                    var job when job.Contains("Milk") => "🥛 Traite",
                    var job when job.Contains("Shear") => "✂️ Tonte",
                    
                    // === GARDE ET SÉCURITÉ ===
                    var job when job.Contains("Guard") => "🛡️ Garde",
                    var job when job.Contains("Wait") && jobDefName.Contains("Combat") => "⚔️ En position de combat",
                    
                    // === RECHERCHE ET ÉTUDE ===
                    var job when job.Contains("Research") => "🔬 Recherche",
                    var job when job.Contains("Study") => "📚 Étude",
                    
                    // === DIVERTISSEMENT ET REPOS ===
                    var job when job.Contains("Joy") => "🎉 Divertissement",
                    var job when job.Contains("Sleep") => "😴 Sommeil",
                    var job when job.Contains("Rest") => "🛏️ Repos",
                    var job when job.Contains("Meditate") => "🧘 Méditation",
                    
                    // === BESOINS BASIQUES ===
                    var job when job.Contains("Ingest") => "🍽️ Alimentation",
                    var job when job.Contains("Eat") => "🍽️ Alimentation",
                    
                    // === ACTIVITÉS SPÉCIALES ===
                    var job when job.Contains("Warden") => "🔒 Gardiennage",
                    var job when job.Contains("Trade") => "💰 Commerce",
                    var job when job.Contains("Lovin") => "💕 Romance",
                    
                    // === DÉPLACEMENT ===
                    var job when job.Contains("Goto") => "🚶 Déplacement",
                    var job when job.Contains("Follow") => "👥 Suivre",
                    
                    // Par défaut
                    _ => $"🔄 {GetFriendlyJobName(jobDefName)}"
                };
            }
            catch (System.Exception ex)
            {
                DebugUtils.LogMessage($"Erreur lors de la détection d'activité: {ex.Message}");
                return "❓ Erreur de détection";
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
                    return $"📦 Transport de {item.def.label} ({weight:F1}kg)";
                }
                return "📦 Transport";
            }
            catch
            {
                return "📦 Transport";
            }
        }

        // NOUVELLE MÉTHODE : Description détaillée pour le crafting
        private string GetCraftingDescription(Pawn pawn)
        {
            try
            {
                // Essayer de détecter le type de fabrication selon la position
                if (pawn?.CurJob?.targetA.Thing != null)
                {
                    var workbench = pawn.CurJob.targetA.Thing;
                    string workbenchName = workbench.def.defName.ToLower();
                    
                    return workbenchName switch
                    {
                        var name when name.Contains("stove") => "🍳 Cuisine",
                        var name when name.Contains("smithy") => "🔨 Forge",
                        var name when name.Contains("tailor") => "🧵 Couture",
                        var name when name.Contains("craft") => "🔧 Artisanat",
                        var name when name.Contains("drug") => "💊 Pharmacie",
                        var name when name.Contains("art") => "🎨 Art",
                        _ => "🔨 Fabrication"
                    };
                }
                return "🔨 Fabrication";
            }
            catch
            {
                return "🔨 Fabrication";
            }
        }

        // NOUVELLE MÉTHODE : Description détaillée pour le dressage
        private string GetTrainingDescription(Pawn pawn)
        {
            try
            {
                if (pawn?.CurJob?.targetA.Pawn != null)
                {
                    var animal = pawn.CurJob.targetA.Pawn;
                    return $"🎓 Dresse {animal.Name?.ToStringShort ?? "animal"}";
                }
                return "🎓 Dressage";
            }
            catch
            {
                return "🎓 Dressage";
            }
        }

        // NOUVELLE MÉTHODE : Convertir les noms de jobs en français
        private string GetFriendlyJobName(string jobDefName)
        {
            return jobDefName switch
            {
                "Wait" => "Attendre",
                "Wait_Downed" => "Inconscient",
                "Wait_MaintainPosture" => "Maintenir position",
                "GotoWander" => "Déambulation",
                "GotoSafeTemperature" => "Chercher température sûre",
                "LayDown" => "Se coucher",
                "Standby" => "En attente",
                "FleeAndCower" => "Fuite et protection",
                "ManTurret" => "Opérer tourelle",
                "BeatFire" => "Éteindre feu",
                "ExtinguishSelf" => "S'éteindre",
                "Vomit" => "Vomir",
                "Job_Stumble" => "Tituber",
                "Strip" => "Déshabiller",
                "Wear" => "S'habiller",
                "RemoveApparel" => "Enlever vêtement",
                "DropEquipment" => "Lâcher équipement",
                "Equip" => "Équiper",
                "UnloadInventory" => "Vider inventaire",
                "TakeFromInventory" => "Prendre inventaire",
                "UseVerbOnThing" => "Utiliser objet",
                _ => jobDefName // Utiliser le nom original si pas de traduction
            };
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
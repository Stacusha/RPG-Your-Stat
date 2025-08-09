using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RPGYourStat
{
    public static class ExperienceManager
    {
        // Mapping des compétences vers les stats RPG avec leurs pourcentages (pour humains)
        private static readonly Dictionary<SkillDef, Dictionary<StatType, float>> SkillToStatMapping = 
            new Dictionary<SkillDef, Dictionary<StatType, float>>
            {
                [SkillDefOf.Shooting] = new Dictionary<StatType, float>
                {
                    { StatType.DEX, 0.50f },
                    { StatType.AGL, 0.40f },
                    { StatType.CON, 0.10f }
                },
                [SkillDefOf.Melee] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.60f },
                    { StatType.DEX, 0.20f },
                    { StatType.AGL, 0.10f },
                    { StatType.CON, 0.10f }
                },
                [SkillDefOf.Construction] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.30f },
                    { StatType.DEX, 0.50f },
                    { StatType.AGL, 0.10f },
                    { StatType.CON, 0.10f }
                },
                [SkillDefOf.Mining] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.50f },
                    { StatType.DEX, 0.10f },
                    { StatType.AGL, 0.10f },
                    { StatType.CON, 0.30f }
                },
                [SkillDefOf.Cooking] = new Dictionary<StatType, float>
                {
                    { StatType.DEX, 0.60f },
                    { StatType.AGL, 0.20f },
                    { StatType.INT, 0.20f }
                },
                [SkillDefOf.Plants] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.30f },
                    { StatType.DEX, 0.10f },
                    { StatType.AGL, 0.30f },
                    { StatType.CON, 0.20f },
                    { StatType.INT, 0.10f }
                },
                [SkillDefOf.Animals] = new Dictionary<StatType, float>
                {
                    { StatType.DEX, 0.10f },
                    { StatType.AGL, 0.20f },
                    { StatType.INT, 0.10f },
                    { StatType.CHA, 0.60f }
                },
                [SkillDefOf.Crafting] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.10f },
                    { StatType.DEX, 0.60f },
                    { StatType.CON, 0.10f },
                    { StatType.INT, 0.20f }
                },
                [SkillDefOf.Artistic] = new Dictionary<StatType, float>
                {
                    { StatType.DEX, 0.20f },
                    { StatType.AGL, 0.10f },
                    { StatType.INT, 0.20f },
                    { StatType.CHA, 0.50f }
                },
                [SkillDefOf.Medicine] = new Dictionary<StatType, float>
                {
                    { StatType.DEX, 0.30f },
                    { StatType.AGL, 0.10f },
                    { StatType.CON, 0.10f },
                    { StatType.INT, 0.40f },
                    { StatType.CHA, 0.10f }
                },
                [SkillDefOf.Social] = new Dictionary<StatType, float>
                {
                    { StatType.DEX, 0.10f },
                    { StatType.CHA, 0.90f }
                },
                [SkillDefOf.Intellectual] = new Dictionary<StatType, float>
                {
                    { StatType.CON, 0.10f },
                    { StatType.INT, 0.90f }
                }
            };

        // NOUVEAU : Mapping des activités pour animaux basé sur leur taille et type
        private static readonly Dictionary<string, Dictionary<StatType, float>> AnimalActivityMapping = 
            new Dictionary<string, Dictionary<StatType, float>>
            {
                // Activités de transport (haul, carry)
                ["hauling"] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.40f },
                    { StatType.AGL, 0.30f },
                    { StatType.CON, 0.30f }
                },
                
                // Activités de combat
                ["combat"] = new Dictionary<StatType, float>
                {
                    { StatType.STR, 0.50f },
                    { StatType.AGL, 0.30f },
                    { StatType.CON, 0.20f }
                },
                
                // Activités de reproduction
                ["reproduction"] = new Dictionary<StatType, float>
                {
                    { StatType.CON, 0.60f },
                    { StatType.CHA, 0.40f }
                },
                
                // Activités de production (lait, laine, etc.)
                ["production"] = new Dictionary<StatType, float>
                {
                    { StatType.CON, 0.50f },
                    { StatType.STR, 0.30f },
                    { StatType.AGL, 0.20f }
                },
                
                // Activités de dressage/apprentissage
                ["training"] = new Dictionary<StatType, float>
                {
                    { StatType.INT, 0.50f },
                    { StatType.CHA, 0.30f },
                    { StatType.AGL, 0.20f }
                },
                
                // Activités de garde/vigilance
                ["guarding"] = new Dictionary<StatType, float>
                {
                    { StatType.AGL, 0.40f },
                    { StatType.INT, 0.30f },
                    { StatType.CON, 0.30f }
                },
                
                // Activités de chasse
                ["hunting"] = new Dictionary<StatType, float>
                {
                    { StatType.AGL, 0.40f },
                    { StatType.STR, 0.30f },
                    { StatType.DEX, 0.20f },
                    { StatType.INT, 0.10f }
                }
            };

        public static void GiveExperienceForSkill(Pawn pawn, SkillDef skill, float baseExperience)
        {
            if (pawn?.GetComp<CompRPGStats>() == null) return;
            if (!SkillToStatMapping.ContainsKey(skill)) return;

            var comp = pawn.GetComp<CompRPGStats>();
            var mapping = SkillToStatMapping[skill];

            foreach (var kvp in mapping)
            {
                StatType statType = kvp.Key;
                float percentage = kvp.Value;
                
                float expToGive = baseExperience * percentage;
                
                if (expToGive > 0f)
                {
                    comp.AddExperience(statType, expToGive);
                }
            }
        }

        public static void GiveCombatExperience(Pawn pawn, bool isRanged, float baseExperience)
        {
            if (pawn?.GetComp<CompRPGStats>() == null) return;

            var comp = pawn.GetComp<CompRPGStats>();
            
            if (isRanged)
            {
                // Combat à distance (Shooting)
                comp.AddExperience(StatType.DEX, baseExperience * 0.50f);
                comp.AddExperience(StatType.AGL, baseExperience * 0.40f);
                comp.AddExperience(StatType.CON, baseExperience * 0.10f);
            }
            else
            {
                // Combat au corps à corps (Melee) - pour humains ET animaux
                comp.AddExperience(StatType.STR, baseExperience * 0.60f);
                comp.AddExperience(StatType.DEX, baseExperience * 0.20f);
                comp.AddExperience(StatType.AGL, baseExperience * 0.10f);
                comp.AddExperience(StatType.CON, baseExperience * 0.10f);
            }
        }

        public static void GiveSocialExperience(Pawn pawn, float baseExperience)
        {
            var comp = pawn?.GetComp<CompRPGStats>();
            if (comp != null)
            {
                comp.AddExperience(StatType.DEX, baseExperience * 0.10f);
                comp.AddExperience(StatType.CHA, baseExperience * 0.90f);
            }
        }

        // NOUVEAU : Méthodes spécialisées pour les animaux
        public static void GiveAnimalActivityExperience(Pawn animal, string activityType, float baseExperience)
        {
            if (animal?.GetComp<CompRPGStats>() == null) return;
            if (!animal.RaceProps.Animal) return;
            if (!AnimalActivityMapping.ContainsKey(activityType)) return;

            var comp = animal.GetComp<CompRPGStats>();
            var mapping = AnimalActivityMapping[activityType];

            // Modifier l'expérience selon la taille de l'animal
            float sizeMultiplier = GetAnimalSizeMultiplier(animal);
            float adjustedExperience = baseExperience * sizeMultiplier;

            foreach (var kvp in mapping)
            {
                StatType statType = kvp.Key;
                float percentage = kvp.Value;
                
                float expToGive = adjustedExperience * percentage;
                
                if (expToGive > 0f)
                {
                    comp.AddExperience(statType, expToGive);
                }
            }

            DebugUtils.LogMessage($"Animal {animal.Name?.ToStringShort ?? "Unknown"} gagne {adjustedExperience:F1} XP pour activité: {activityType}");
        }

        // NOUVEAU : Calculer le multiplicateur basé sur la taille de l'animal
        private static float GetAnimalSizeMultiplier(Pawn animal)
        {
            if (!animal.RaceProps.Animal) return 1f;

            float bodySize = animal.RaceProps.baseBodySize;

            // Animaux plus gros = progression plus lente mais plus de potentiel
            // Animaux plus petits = progression plus rapide mais moins de potentiel
            if (bodySize >= 2.0f) // Gros animaux (éléphants, etc.)
                return 0.7f;
            else if (bodySize >= 1.0f) // Animaux moyens (chevaux, vaches, etc.)
                return 0.85f;
            else if (bodySize >= 0.5f) // Petits animaux (chiens, chats, etc.)
                return 1.0f;
            else // Très petits animaux (écureuils, etc.)
                return 1.2f;
        }

        // NOUVEAU : Expérience pour les activités de transport des animaux
        public static void GiveAnimalHaulingExperience(Pawn animal, float weight)
        {
            if (!animal.RaceProps.Animal) return;
            
            // Expérience basée sur le poids transporté
            float baseExp = Mathf.Min(weight * 0.1f, 50f); // Maximum 50 XP par transport
            GiveAnimalActivityExperience(animal, "hauling", baseExp);
        }

        // MODIFIÉ : Expérience pour les animaux de production
        public static void GiveAnimalProductionExperience(Pawn animal, ThingDef productType, int quantity)
        {
            if (!animal.RaceProps.Animal) return;
            
            float baseExp = quantity * 5f; // 5 XP par unité produite
            
            // Bonus selon le type de production (si disponible)
            if (productType != null)
            {
                if (productType.defName.Contains("Milk"))
                    baseExp *= 1.2f; // Bonus pour le lait
                else if (productType.defName.Contains("Wool") || productType.defName.Contains("Fur"))
                    baseExp *= 1.1f; // Bonus pour la laine/fourrure
            }
            else
            {
                // NOUVEAU : Si le type de produit est inconnu, utiliser un multiplicateur neutre
                baseExp *= 1.0f; // Pas de bonus ni de malus
            }
            
            GiveAnimalActivityExperience(animal, "production", baseExp);
        }

        // NOUVEAU : Expérience pour le dressage des animaux
        public static void GiveAnimalTrainingExperience(Pawn animal, TrainableDef trainable, bool success)
        {
            if (!animal.RaceProps.Animal) return;
            
            float baseExp = success ? 30f : 10f; // Plus d'XP si le dressage réussit
            
            // Bonus selon le type de dressage (si disponible)
            if (trainable != null)
            {
                if (trainable.defName.Contains("Guard") || trainable.defName.Contains("Attack"))
                    baseExp *= 1.3f; // Bonus pour les entraînements de combat
                else if (trainable.defName.Contains("Haul") || trainable.defName.Contains("Rescue"))
                    baseExp *= 1.1f; // Bonus pour les entraînements utilitaires
            }
            else
            {
                // NOUVEAU : Bonus par défaut si on ne connaît pas le type de dressage
                baseExp *= 1.0f; // Pas de modificateur particulier
            }
            
            GiveAnimalActivityExperience(animal, "training", baseExp);
        }

        // NOUVEAU : Expérience pour les animaux de garde
        public static void GiveAnimalGuardingExperience(Pawn animal)
        {
            if (!animal.RaceProps.Animal) return;
            
            // Expérience passive pour la garde (appelée périodiquement)
            float baseExp = 2f; // Petite quantité d'XP passive
            GiveAnimalActivityExperience(animal, "guarding", baseExp);
        }

        // NOUVEAU : Expérience pour la chasse des animaux
        public static void GiveAnimalHuntingExperience(Pawn animal, Pawn prey)
        {
            if (!animal.RaceProps.Animal) return;
            
            float baseExp = 25f;
            
            // Bonus selon la taille de la proie
            if (prey != null)
            {
                float preySize = prey.RaceProps.baseBodySize;
                baseExp *= Mathf.Clamp(preySize, 0.5f, 2.0f); // Proies plus grosses = plus d'XP
            }
            
            GiveAnimalActivityExperience(animal, "hunting", baseExp);
        }

        // NOUVEAU : Expérience pour la reproduction des animaux
        public static void GiveAnimalReproductionExperience(Pawn animal)
        {
            if (!animal.RaceProps.Animal) return;
            
            float baseExp = 100f; // Grosse récompense pour la reproduction
            GiveAnimalActivityExperience(animal, "reproduction", baseExp);
        }

        // Méthode spécialisée pour les activités de Growing si nécessaire
        public static void GiveGrowingExperience(Pawn pawn, float baseExperience)
        {
            var comp = pawn?.GetComp<CompRPGStats>();
            if (comp != null)
            {
                comp.AddExperience(StatType.STR, baseExperience * 0.10f);
                comp.AddExperience(StatType.DEX, baseExperience * 0.20f);
                comp.AddExperience(StatType.AGL, baseExperience * 0.20f);
                comp.AddExperience(StatType.CON, baseExperience * 0.10f);
                comp.AddExperience(StatType.INT, baseExperience * 0.40f);
                comp.AddExperience(StatType.CHA, baseExperience * 0.10f);
            }
        }
    }
}
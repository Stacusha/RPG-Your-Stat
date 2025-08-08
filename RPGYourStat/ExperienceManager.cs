using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RPGYourStat
{
    public static class ExperienceManager
    {
        // Mapping des compétences vers les stats RPG avec leurs pourcentages
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

        // Mapping pour les activités de Growing
        private static readonly Dictionary<StatType, float> GrowingMapping = new Dictionary<StatType, float>
        {
            { StatType.STR, 0.10f },
            { StatType.DEX, 0.20f },
            { StatType.AGL, 0.20f },
            { StatType.CON, 0.10f },
            { StatType.INT, 0.40f },
            { StatType.CHA, 0.10f }
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
                
                // Calcul direct en float - plus de conversions !
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
                // Combat au corps à corps (Melee)
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

        // Méthode spécialisée pour les activités de Growing si nécessaire
        public static void GiveGrowingExperience(Pawn pawn, float baseExperience)
        {
            var comp = pawn?.GetComp<CompRPGStats>();
            if (comp != null)
            {
                foreach (var kvp in GrowingMapping)
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
        }
    }
}
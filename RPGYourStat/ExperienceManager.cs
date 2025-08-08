using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RPGYourStat
{
    public static class ExperienceManager
    {
        // Mapping des compétences vers les stats RPG avec leurs pourcentages mis à jour
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

        // Ajouter le mapping pour Growing (qui n'existe pas en tant que SkillDef séparé)
        // En supposant que Growing fait partie de Plants dans RimWorld
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
                
                // Utiliser Mathf.CeilToInt au lieu de RoundToInt pour s'assurer qu'on obtient au moins 1
                float calculatedExp = baseExperience * percentage;
                int expToGive = calculatedExp < 1f ? Mathf.CeilToInt(calculatedExp) : Mathf.RoundToInt(calculatedExp);
                
                if (expToGive > 0)
                {
                    comp.AddExperience(statType, expToGive);
                    DebugUtils.LogMessage($"  → {statType}: +{expToGive} XP (calc: {calculatedExp:F2})");
                }
            }

            DebugUtils.LogMessage($"{pawn.Name} gagne de l'expérience en {skill.defName} (base: {baseExperience:F2})");
        }

        public static void GiveCombatExperience(Pawn pawn, bool isRanged, float baseExperience)
        {
            if (pawn?.GetComp<CompRPGStats>() == null) return;

            var comp = pawn.GetComp<CompRPGStats>();
            
            if (isRanged)
            {
                // Combat à distance (Shooting)
                int dexExp = Mathf.CeilToInt(baseExperience * 0.50f);
                int aglExp = Mathf.CeilToInt(baseExperience * 0.40f);
                int conExp = Mathf.CeilToInt(baseExperience * 0.10f);
                
                comp.AddExperience(StatType.DEX, dexExp);
                comp.AddExperience(StatType.AGL, aglExp);
                comp.AddExperience(StatType.CON, conExp);
                
                DebugUtils.LogMessage($"Combat à distance: DEX +{dexExp}, AGL +{aglExp}, CON +{conExp}");
            }
            else
            {
                // Combat au corps à corps (Melee)
                int strExp = Mathf.CeilToInt(baseExperience * 0.60f);
                int dexExp = Mathf.CeilToInt(baseExperience * 0.20f);
                int aglExp = Mathf.CeilToInt(baseExperience * 0.10f);
                int conExp = Mathf.CeilToInt(baseExperience * 0.10f);
                
                comp.AddExperience(StatType.STR, strExp);
                comp.AddExperience(StatType.DEX, dexExp);
                comp.AddExperience(StatType.AGL, aglExp);
                comp.AddExperience(StatType.CON, conExp);
                
                DebugUtils.LogMessage($"Combat corps à corps: STR +{strExp}, DEX +{dexExp}, AGL +{aglExp}, CON +{conExp}");
            }
        }

        public static void GiveSocialExperience(Pawn pawn, float baseExperience)
        {
            var comp = pawn?.GetComp<CompRPGStats>();
            if (comp != null)
            {
                int dexExp = Mathf.CeilToInt(baseExperience * 0.10f);
                int chaExp = Mathf.CeilToInt(baseExperience * 0.90f);
                
                comp.AddExperience(StatType.DEX, dexExp);
                comp.AddExperience(StatType.CHA, chaExp);
                
                DebugUtils.LogMessage($"Expérience sociale: DEX +{dexExp}, CHA +{chaExp}");
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
                    
                    float calculatedExp = baseExperience * percentage;
                    int expToGive = calculatedExp < 1f ? Mathf.CeilToInt(calculatedExp) : Mathf.RoundToInt(calculatedExp);
                    
                    if (expToGive > 0)
                    {
                        comp.AddExperience(statType, expToGive);
                        DebugUtils.LogMessage($"  → {statType}: +{expToGive} XP (calc: {calculatedExp:F2})");
                    }
                }
                
                DebugUtils.LogMessage($"{pawn.Name} gagne de l'expérience en Growing (base: {baseExperience:F2})");
            }
        }
    }
}
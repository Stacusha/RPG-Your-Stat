using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace RPGYourStat
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("Stacusha.RPGYourStat");
            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RPGYourStat] Erreur lors de l'application des patches Harmony: {ex}");
            }
        }
    }

    // Patch pour intercepter les gains d'expérience des compétences
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    public static class SkillRecord_Learn_Patch
    {
        public static void Postfix(SkillRecord __instance, float xp, bool direct)
        {
            if (__instance?.Pawn == null || xp <= 0) return;
            
            // Vérifier si les settings existent et si l'expérience de travail est activée
            if (RPGYourStat_Mod.settings?.enableWorkExperience != true) return;
            
            // Convertir l'XP de compétence en XP RPG (multiplier par 10 pour éviter l'arrondi à 0)
            float rpgXp = xp * RPGYourStat_Mod.settings.experienceMultiplier * 10f;
            ExperienceManager.GiveExperienceForSkill(__instance.Pawn, __instance.def, rpgXp);
        }
    }

    // Patch pour les attaques réussies
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Verb_LaunchProjectile_TryCastShot_Patch
    {
        public static void Postfix(Verb_LaunchProjectile __instance, bool __result)
        {
            if (!__result || __instance?.CasterPawn == null) return;
            if (RPGYourStat_Mod.settings?.enableCombatExperience != true) return;
            
            float baseExp = 20f * RPGYourStat_Mod.settings.experienceMultiplier;
            ExperienceManager.GiveCombatExperience(__instance.CasterPawn, true, baseExp);
        }
    }

    // Patch pour les attaques au corps à corps
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Verb_MeleeAttack_TryCastShot_Patch
    {
        public static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            if (!__result || __instance?.CasterPawn == null) return;
            if (RPGYourStat_Mod.settings?.enableCombatExperience != true) return;
            
            float baseExp = 20f * RPGYourStat_Mod.settings.experienceMultiplier;
            ExperienceManager.GiveCombatExperience(__instance.CasterPawn, false, baseExp);
        }
    }

    // Patch pour les interactions sociales - CORRIGÉ
    [HarmonyPatch(typeof(InteractionWorker), "Interacted")]
    public static class InteractionWorker_Interacted_Patch
    {
        public static void Postfix(InteractionWorker __instance, Pawn initiator, Pawn recipient)
        {
            if (initiator == null) return;
            if (RPGYourStat_Mod.settings?.enableSocialExperience != true) return;
            
            float baseExp = 10f * RPGYourStat_Mod.settings.experienceMultiplier;
            ExperienceManager.GiveSocialExperience(initiator, baseExp);
        }
    }

    // Patch alternatif pour les interactions sociales via PlayLogEntry_Interaction
    [HarmonyPatch(typeof(PlayLogEntry_Interaction), MethodType.Constructor)]
    [HarmonyPatch(new System.Type[] { typeof(InteractionDef), typeof(Pawn), typeof(Pawn), typeof(System.Collections.Generic.List<RulePackDef>) })]
    public static class PlayLogEntry_Interaction_Constructor_Patch
    {
        public static void Postfix(PlayLogEntry_Interaction __instance, InteractionDef intDef, Pawn initiator, Pawn recipient)
        {
            if (initiator == null || intDef == null) return;
            if (RPGYourStat_Mod.settings?.enableSocialExperience != true) return;
            
            float baseExp = 5f * RPGYourStat_Mod.settings.experienceMultiplier;
            ExperienceManager.GiveSocialExperience(initiator, baseExp);
        }
    }

    // Patch pour appliquer les modificateurs de stats RPG - CORRIGÉ avec signature spécifique
    [HarmonyPatch(typeof(StatWorker), "GetValue", new System.Type[] { typeof(StatRequest), typeof(bool) })]
    public static class StatWorker_GetValue_Patch
    {
        public static void Postfix(StatWorker __instance, StatRequest req, bool applyPostProcess, ref float __result)
        {
            if (req.Thing is Pawn pawn)
            {
                // Utiliser la réflexion pour accéder au champ stat
                var statField = typeof(StatWorker).GetField("stat", BindingFlags.NonPublic | BindingFlags.Instance);
                if (statField != null)
                {
                    StatDef stat = (StatDef)statField.GetValue(__instance);
                    if (stat != null)
                    {
                        float modifier = StatModifierSystem.GetStatModifier(pawn, stat);
                        if (modifier != 0f)
                        {
                            // Appliquer le modificateur comme un facteur multiplicatif
                            if (stat.formatString != null && stat.formatString.Contains("%"))
                            {
                                // Pour les pourcentages, ajouter directement
                                __result += modifier;
                            }
                            else
                            {
                                // Pour les autres stats, appliquer comme multiplicateur
                                __result *= (1f + modifier);
                            }
                        }
                    }
                }
            }
        }
    }
}
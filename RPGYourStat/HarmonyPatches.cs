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
                DebugUtils.LogMessage("Patches Harmony appliqués avec succès!");
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
            
            DebugUtils.LogMessage($"Pawn {__instance.Pawn.Name} gagne {rpgXp} XP RPG pour {__instance.def.defName} (XP skill: {xp})");
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
            
            float baseExp = 20f * RPGYourStat_Mod.settings.experienceMultiplier; // Augmenté de 2f à 20f
            ExperienceManager.GiveCombatExperience(__instance.CasterPawn, true, baseExp);
            
            DebugUtils.LogMessage($"{__instance.CasterPawn.Name} gagne de l'XP de combat à distance ({baseExp})");
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
            
            float baseExp = 20f * RPGYourStat_Mod.settings.experienceMultiplier; // Augmenté de 2f à 20f
            ExperienceManager.GiveCombatExperience(__instance.CasterPawn, false, baseExp);
            
            DebugUtils.LogMessage($"{__instance.CasterPawn.Name} gagne de l'XP de combat au corps à corps ({baseExp})");
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
            
            float baseExp = 10f * RPGYourStat_Mod.settings.experienceMultiplier; // Augmenté de 1f à 10f
            ExperienceManager.GiveSocialExperience(initiator, baseExp);
            
            DebugUtils.LogMessage($"{initiator.Name} gagne de l'XP sociale pour interaction avec {recipient?.Name} ({baseExp})");
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
            
            DebugUtils.LogMessage($"{initiator.Name} gagne de l'XP sociale pour {intDef.defName} avec {recipient?.Name} ({baseExp})");
        }
    }
}
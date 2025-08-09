using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
using Verse.AI;

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
            
            // NOUVEAU : Gestion spéciale pour la chasse par les animaux
            if (__instance.CasterPawn.RaceProps.Animal && __instance.CasterPawn.CurJob?.def == JobDefOf.Hunt)
            {
                var target = __instance.CasterPawn.CurJob.targetA.Pawn;
                ExperienceManager.GiveAnimalHuntingExperience(__instance.CasterPawn, target);
            }
        }
    }

    // Patch pour les interactions sociales
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

    // MODIFIÉ : Patch pour le transport d'objets (humains ET animaux)
    [HarmonyPatch(typeof(JobDriver_HaulToCell), "MakeNewToils")]
    public static class JobDriver_HaulToCell_MakeNewToils_Patch
    {
        public static void Postfix(JobDriver_HaulToCell __instance)
        {
            if (__instance?.pawn == null) return;
            if (RPGYourStat_Mod.settings?.enableWorkExperience != true) return;
            
            // Ajouter un callback à la fin du travail de transport
            var toils = __instance.GetType().GetField("toils", BindingFlags.NonPublic | BindingFlags.Instance);
            if (toils?.GetValue(__instance) is System.Collections.Generic.List<Toil> toilList && toilList.Count > 0)
            {
                var lastToil = toilList[toilList.Count - 1];
                lastToil.AddFinishAction(() =>
                {
                    if (__instance.job?.targetA.Thing != null)
                    {
                        float weight = __instance.job.targetA.Thing.GetStatValue(StatDefOf.Mass);
                        
                        if (__instance.pawn.RaceProps.Animal)
                        {
                            // Expérience spéciale pour les animaux
                            ExperienceManager.GiveAnimalHaulingExperience(__instance.pawn, weight);
                        }
                        else
                        {
                            // Expérience normale pour les humains via le système de compétences existant
                            // (déjà géré par le patch SkillRecord_Learn)
                        }
                    }
                });
            }
        }
    }

    // NOUVEAU : Patch pour l'expérience passive des animaux et détection des naissances
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Pawn_Tick_ExperienceSystem_Patch
    {
        private static int tickCounter = 0;
        private static readonly System.Collections.Generic.Dictionary<Pawn, bool> wasPregnant = 
            new System.Collections.Generic.Dictionary<Pawn, bool>();
        
        public static void Postfix(Pawn __instance)
        {
            if (!__instance.RaceProps.Animal) return;
            if (RPGYourStat_Mod.settings?.enableWorkExperience != true) return;
            
            // Vérifier toutes les 250 ticks (environ 4 secondes)
            tickCounter++;
            if (tickCounter >= 250)
            {
                tickCounter = 0;
                
                // Vérifier l'état de grossesse pour détecter les naissances
                CheckForBirth(__instance);
                
                // Vérifier si l'animal est en train de garder (toutes les 2500 ticks)
                if (tickCounter % 10 == 0) // Diviser encore par 10 pour arriver à 2500 ticks
                {
                    if (__instance.CurJob?.def != null && 
                        (__instance.CurJob.def.defName.Contains("Guard") || 
                         __instance.CurJob.def == JobDefOf.Wait_Combat))
                    {
                        ExperienceManager.GiveAnimalGuardingExperience(__instance);
                    }
                }
                
                // NOUVEAU : Donner de l'XP de dressage passif aux animaux qui sont en cours de dressage
                CheckForTrainingActivity(__instance);
            }
        }
        
        private static void CheckForBirth(Pawn animal)
        {
            try
            {
                // Vérifier si l'animal était enceinte au tick précédent
                bool currentlyPregnant = animal.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Pregnant) != null;
                
                if (wasPregnant.TryGetValue(animal, out bool previouslyPregnant))
                {
                    // Si l'animal était enceinte mais ne l'est plus, il a donné naissance
                    if (previouslyPregnant && !currentlyPregnant)
                    {
                        ExperienceManager.GiveAnimalReproductionExperience(animal);
                        DebugUtils.LogMessage($"Animal {animal.Name?.ToStringShort ?? "Unknown"} a donné naissance et gagne de l'XP de reproduction");
                    }
                }
                
                // Mettre à jour l'état
                wasPregnant[animal] = currentlyPregnant;
                
                // Nettoyer les entrées des animaux qui n'existent plus
                if (tickCounter % 50 == 0) // Nettoyage périodique
                {
                    var toRemove = new System.Collections.Generic.List<Pawn>();
                    foreach (var pawn in wasPregnant.Keys)
                    {
                        if (pawn?.Destroyed == true || pawn?.Map == null)
                        {
                            toRemove.Add(pawn);
                        }
                    }
                    foreach (var pawn in toRemove)
                    {
                        wasPregnant.Remove(pawn);
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Éviter les erreurs qui pourraient crasher le jeu
                DebugUtils.LogMessage($"Erreur lors de la vérification de naissance: {ex.Message}");
            }
        }

        // NOUVEAU : Vérifier si l'animal est en cours de dressage
        private static void CheckForTrainingActivity(Pawn animal)
        {
            try
            {
                // Vérifier si quelqu'un est en train de dresser cet animal
                if (animal.Map?.mapPawns?.FreeColonists != null)
                {
                    foreach (var colonist in animal.Map.mapPawns.FreeColonists)
                    {
                        if (colonist.CurJob?.def?.defName?.Contains("Train") == true && 
                            colonist.CurJob.targetA.Pawn == animal)
                        {
                            // L'animal est en cours de dressage, donner de l'XP
                            ExperienceManager.GiveAnimalTrainingExperience(animal, null, true);
                            
                            // Donner aussi de l'XP sociale au dresseur
                            ExperienceManager.GiveSocialExperience(colonist, 10f * RPGYourStat_Mod.settings.experienceMultiplier);
                            
                            DebugUtils.LogMessage($"Animal {animal.Name?.ToStringShort ?? "Unknown"} en cours de dressage par {colonist.Name?.ToStringShort ?? "Unknown"}");
                            break; // Un seul dresseur à la fois
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugUtils.LogMessage($"Erreur lors de la vérification de dressage: {ex.Message}");
            }
        }
    }

    // NOUVEAU : Patch générique pour les animaux de production (version simplifiée)
    [HarmonyPatch(typeof(CompHasGatherableBodyResource), "Gathered")]
    public static class CompHasGatherableBodyResource_Gathered_Patch
    {
        public static void Postfix(CompHasGatherableBodyResource __instance, Pawn doer)
        {
            if (__instance?.parent is Pawn animal && animal.RaceProps.Animal)
            {
                if (RPGYourStat_Mod.settings?.enableWorkExperience == true)
                {
                    // SIMPLIFIÉ : Donner une quantité d'XP fixe selon le type de composant
                    float baseExp = 25f; // XP de base pour la production
                    string productType = "generic";
                    
                    // Identifier le type de production selon le composant
                    if (__instance is CompMilkable)
                    {
                        baseExp = 30f; // Plus d'XP pour le lait
                        productType = "milk";
                    }
                    else if (__instance.GetType().Name.Contains("Shearable"))
                    {
                        baseExp = 25f; // XP normale pour la laine
                        productType = "wool";
                    }
                    
                    // Donner l'expérience de production avec un type générique
                    ExperienceManager.GiveAnimalActivityExperience(animal, "production", baseExp);
                    
                    DebugUtils.LogMessage($"Animal {animal.Name?.ToStringShort ?? "Unknown"} produit {productType} et gagne {baseExp} XP");
                }
            }
        }
    }

    // Patch pour appliquer les modificateurs de stats RPG
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
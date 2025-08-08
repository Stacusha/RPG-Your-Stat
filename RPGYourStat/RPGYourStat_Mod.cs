using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;

namespace RPGYourStat
{
    public class RPGYourStat_Mod : Mod
    {
        public static RPGYourStat_Settings settings;

        public RPGYourStat_Mod(ModContentPack content) : base(content)
        {
            Log.Message("Le mod RPG Your Stat a été chargé avec succès !");
            settings = GetSettings<RPGYourStat_Settings>();
            
            // Ajouter le composant aux pawns existants
            LongEventHandler.QueueLongEvent(AddRPGComponentToPawns, "Initialisation RPG Stats", false, null);
        }

        private void AddRPGComponentToPawns()
        {
            // Ajouter le composant aux définitions de pawns
            var humanDef = DefDatabase<ThingDef>.GetNamed("Human", false);
            if (humanDef != null && !humanDef.comps.Any(c => c.compClass == typeof(CompRPGStats)))
            {
                humanDef.comps.Add(new CompPropertiesRPGStats());
                Log.Message("[RPGYourStat] Composant RPG ajouté aux humains");
            }

            // Ajouter aux animaux
            var animalDefs = DefDatabase<ThingDef>.AllDefs.Where(def => 
                def.category == ThingCategory.Pawn && 
                def.race?.Animal == true);

            foreach (var animalDef in animalDefs)
            {
                if (!animalDef.comps.Any(c => c.compClass == typeof(CompRPGStats)))
                {
                    animalDef.comps.Add(new CompPropertiesRPGStats());
                }
            }

            Log.Message("[RPGYourStat] Composants RPG ajoutés aux animaux");
        }

        public override string SettingsCategory()
        {
            return "RPG Your Stat";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("Activer le mode de débogage", ref settings.debugMode, 
                "Affiche les messages de débogage dans la console.");

            listing.Gap();
            listing.Label($"Multiplicateur d'expérience: {settings.experienceMultiplier:F1}");
            settings.experienceMultiplier = listing.Slider(settings.experienceMultiplier, 0.1f, 5.0f);

            listing.Gap();
            listing.CheckboxLabeled("Expérience de combat", ref settings.enableCombatExperience,
                "Les pawns gagnent de l'XP en combattant.");

            listing.CheckboxLabeled("Expérience de travail", ref settings.enableWorkExperience,
                "Les pawns gagnent de l'XP en travaillant.");

            listing.CheckboxLabeled("Expérience sociale", ref settings.enableSocialExperience,
                "Les pawns gagnent de l'XP en interagissant socialement.");

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
using UnityEngine;
using Verse;

namespace RPGYourStat
{
    public class RPGYourStat_Mod : Mod
    {
        public static RPGYourStat_Settings settings;

        public RPGYourStat_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RPGYourStat_Settings>();
        }

        public override string SettingsCategory()
        {
            return "RPG Your Stat";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("Activer le mode de débogage", ref settings.debugMode, "Affiche les messages de débogage dans la console.");

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
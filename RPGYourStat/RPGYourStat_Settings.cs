using Verse;

namespace RPGYourStat
{
    public class RPGYourStat_Settings : ModSettings
    {
        public bool debugMode = false;

        public override void ExposeData()
        {
            base.ExposeData();
            // L'API ExposeData permet de sauvegarder et charger les données du mod
            Scribe_Values.Look(ref debugMode, "debugMode", false);
        }
    }
}
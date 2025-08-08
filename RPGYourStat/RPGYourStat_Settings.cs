using Verse;

namespace RPGYourStat
{
    public class RPGYourStat_Settings : ModSettings
    {
        public bool debugMode = true; // Activé par défaut pour le debug
        public float experienceMultiplier = 2.0f; // Augmenté pour compenser l'arrondi
        public bool enableCombatExperience = true;
        public bool enableWorkExperience = true;
        public bool enableSocialExperience = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref debugMode, "debugMode", true);
            Scribe_Values.Look(ref experienceMultiplier, "experienceMultiplier", 2.0f);
            Scribe_Values.Look(ref enableCombatExperience, "enableCombatExperience", true);
            Scribe_Values.Look(ref enableWorkExperience, "enableWorkExperience", true);
            Scribe_Values.Look(ref enableSocialExperience, "enableSocialExperience", true);
        }
    }
}
using Verse;

namespace RPGYourStat
{
    public class RPGYourStat_Settings : ModSettings
    {
        public bool debugMode = false; // Désactivé par défaut
        public float experienceMultiplier = 1.0f; // Réduit à 1.0f
        public bool enableCombatExperience = true;
        public bool enableWorkExperience = true;
        public bool enableSocialExperience = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref debugMode, "debugMode", false); // Changé la valeur par défaut
            Scribe_Values.Look(ref experienceMultiplier, "experienceMultiplier", 1.0f);
            Scribe_Values.Look(ref enableCombatExperience, "enableCombatExperience", true);
            Scribe_Values.Look(ref enableWorkExperience, "enableWorkExperience", true);
            Scribe_Values.Look(ref enableSocialExperience, "enableSocialExperience", true);
        }
    }
}
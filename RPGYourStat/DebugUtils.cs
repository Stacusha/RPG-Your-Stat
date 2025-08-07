using Verse;

namespace RPGYourStat
{
    public static class DebugUtils
    {
        public static void LogMessage(string message)
        {
            // Vérifier si le mode de débogage est activé avant d'afficher le message
            if (RPGYourStat_Mod.settings?.debugMode == true)
            {
                Log.Message($"[RPGYourStat] {message}");
            }
        }
    }
}
using RimWorld;
using UnityEngine;
using Verse;

namespace RPGYourStat
{
    public class MainTabWindow_RPGStats : MainTabWindow
    {
        public MainTabWindow_RPGStats()
        {
            // Configuration appropriée pour un MainTabWindow
            this.forcePause = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = false; // Les onglets principaux ne sont généralement pas draggables
            // Retirer doCloseButton et doCloseX qui ne sont pas appropriés pour MainTabWindow
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Ici, vous dessinez le contenu de votre fenêtre
            Text.Font = GameFont.Medium;
            Rect rect = new Rect(inRect.x + 10, inRect.y + 10, inRect.width - 20, 40);
            Widgets.Label(rect, "Onglet des statistiques RPG (Fonctionnel) !");
        }

        public override void PreOpen()
        {
            base.PreOpen();
            DebugUtils.LogMessage("L'onglet RPG Stats s'ouvre !");
        }

        public override void PostClose()
        {
            base.PostClose();
            DebugUtils.LogMessage("L'onglet RPG Stats se ferme.");
        }
    }
}
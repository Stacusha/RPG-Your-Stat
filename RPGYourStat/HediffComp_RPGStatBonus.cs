using RimWorld;
using Verse;
using System.Collections.Generic;

namespace RPGYourStat
{
    public class HediffComp_RPGStatBonus : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            
            // Mettre à jour les modificateurs toutes les 250 ticks (environ 4 secondes)
            if (Pawn.IsHashIntervalTick(250))
            {
                UpdateStatModifiers();
            }
        }

        private void UpdateStatModifiers()
        {
            var comp = Pawn.GetComp<CompRPGStats>();
            if (comp == null) return;

            // Cette méthode sera appelée pour maintenir les modificateurs à jour
            // Les modificateurs réels sont appliqués via StatWorker_GetValue patches
        }

        // MODIFIÉ : Supprimer l'affichage des bonus dans le label
        public override string CompLabelInBracketsExtra => null;

        // MODIFIÉ : Supprimer complètement le tooltip détaillé
        public override string CompTipStringExtra => null;
    }

    public class HediffCompProperties_RPGStatBonus : HediffCompProperties
    {
        public HediffCompProperties_RPGStatBonus()
        {
            compClass = typeof(HediffComp_RPGStatBonus);
        }
    }
}
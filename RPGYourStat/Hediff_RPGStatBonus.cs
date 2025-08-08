using RimWorld;
using Verse;

namespace RPGYourStat
{
    public class Hediff_RPGStatBonus : HediffWithComps // Changé de Hediff vers HediffWithComps
    {
        public override bool ShouldRemove => false; // Ne jamais retirer automatiquement
        public override bool Visible => true;
        public override string Label => "Bonus de statistiques RPG";

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            // Initialiser si nécessaire
        }

        public override void Tick()
        {
            base.Tick();
            // Maintenir le hediff actif
        }
    }
}
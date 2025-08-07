using UnityEngine;
using Verse;
using RimWorld;

namespace RPGYourStat
{
    public class CompRPGStats : ThingComp
    {
        private int level = 1;
        private int experience = 0;
        private const int BaseExperienceRequired = 100;
        private const float ExperienceMultiplier = 1.5f;

        public int Level
        {
            get => level;
            set
            {
                level = Mathf.Max(1, value);
                DebugUtils.LogMessage($"{parent.Label} niveau mis à jour: {level}");
            }
        }

        public int Experience
        {
            get => experience;
            set
            {
                experience = Mathf.Max(0, value);
                CheckLevelUp();
            }
        }

        public CompPropertiesRPGStats Props => (CompPropertiesRPGStats)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref experience, "experience", 0);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;

            Experience += amount;
            DebugUtils.LogMessage($"{parent.Label} gagne {amount} XP (Total: {Experience})");
        }

        public int GetRequiredExperienceForLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0;
            
            int totalRequired = 0;
            for (int i = 1; i < targetLevel; i++)
            {
                totalRequired += Mathf.RoundToInt(BaseExperienceRequired * Mathf.Pow(ExperienceMultiplier, i - 1));
            }
            return totalRequired;
        }

        private void CheckLevelUp()
        {
            int requiredExp = GetRequiredExperienceForLevel(level + 1);
            if (experience >= requiredExp)
            {
                level++;
                DebugUtils.LogMessage($"{parent.Label} monte au niveau {level}!");
                
                // Notification de level up
                if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer)
                {
                    Messages.Message($"{pawn.Name?.ToStringShort ?? "Pawn"} monte au niveau {level}!", 
                        MessageTypeDefOf.PositiveEvent);
                }
                
                // Vérifier si un autre level up est possible
                CheckLevelUp();
            }
        }

        public override string CompInspectStringExtra()
        {
            int nextLevelExp = GetRequiredExperienceForLevel(level + 1);
            int expNeeded = nextLevelExp - experience;
            
            return $"Niveau: {level}\nXP: {experience}/{nextLevelExp}\nXP restante: {expNeeded}";
        }
    }

    public class CompPropertiesRPGStats : CompProperties
    {
        public CompPropertiesRPGStats()
        {
            compClass = typeof(CompRPGStats);
        }
    }
}
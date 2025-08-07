using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RPGYourStat
{
    public class MainTabWindow_RPGStats : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private const float RowHeight = 25f;
        private const float StatColumnWidth = 80f;
        private const float NameColumnWidth = 150f;

        public MainTabWindow_RPGStats()
        {
            this.forcePause = false;
            this.closeOnClickedOutside = false;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
        }

        public override Vector2 RequestedTabSize => new Vector2(1000f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            
            // En-tête
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "Statistiques RPG des Colons et Animaux");
            Text.Font = GameFont.Small;

            // Bouton de test (temporaire)
            Rect testButtonRect = new Rect(inRect.x + inRect.width - 150f, inRect.y, 140f, 30f);
            if (Widgets.ButtonText(testButtonRect, "Test XP"))
            {
                foreach (var pawn in GetColonists())
                {
                    var comp = pawn.GetComp<CompRPGStats>();
                    comp?.GiveTestExperience();
                }
            }

            // Zone de contenu avec scroll
            Rect contentRect = new Rect(inRect.x, inRect.y + 50f, inRect.width, inRect.height - 50f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, GetTotalContentHeight());
            
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
            
            float currentY = 0f;
            
            // Dessiner les colons
            currentY = DrawPawnsSection("COLONS", GetColonists(), currentY, viewRect.width);
            
            // Séparateur
            currentY = DrawSeparator(currentY, viewRect.width);
            
            // Dessiner les animaux
            currentY = DrawPawnsSection("ANIMAUX", GetAnimals(), currentY, viewRect.width);
            
            Widgets.EndScrollView();
        }

        private float DrawPawnsSection(string title, List<Pawn> pawns, float startY, float width)
        {
            float currentY = startY;
            
            // Titre de la section
            Rect titleRect = new Rect(0f, currentY, width, 30f);
            Text.Font = GameFont.Medium;
            GUI.color = Color.cyan;
            Widgets.Label(titleRect, title);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            currentY += 35f;
            
            if (!pawns.Any())
            {
                Rect noDataRect = new Rect(20f, currentY, width - 20f, RowHeight);
                GUI.color = Color.gray;
                Widgets.Label(noDataRect, "Aucun " + (title.ToLower() == "colons" ? "colon" : "animal") + " trouvé");
                GUI.color = Color.white;
                currentY += RowHeight + 10f;
                return currentY;
            }

            // En-têtes des colonnes
            currentY = DrawColumnHeaders(currentY, width);
            
            // Ligne de séparation sous les en-têtes
            Widgets.DrawLineHorizontal(0f, currentY, width);
            currentY += 5f;

            // Données des pawns
            foreach (Pawn pawn in pawns)
            {
                currentY = DrawPawnRow(pawn, currentY, width);
            }
            
            currentY += 10f; // Espacement après la section
            return currentY;
        }

        private float DrawColumnHeaders(float y, float width)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.yellow;
            
            float currentX = 10f;
            
            // Nom
            Rect nameRect = new Rect(currentX, y, NameColumnWidth, RowHeight);
            Widgets.Label(nameRect, "Nom");
            currentX += NameColumnWidth + 10f;
            
            // En-têtes des statistiques
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                Rect statRect = new Rect(currentX, y, StatColumnWidth, RowHeight);
                Widgets.Label(statRect, CompRPGStats.GetStatDisplayName(statType));
                currentX += StatColumnWidth + 5f;
            }
            
            // Statut
            Rect statusRect = new Rect(currentX, y, 100f, RowHeight);
            Widgets.Label(statusRect, "Statut");
            
            GUI.color = Color.white;
            
            return y + RowHeight;
        }

        private float DrawPawnRow(Pawn pawn, float y, float width)
        {
            // Alternance de couleur de fond
            if (((int)(y / RowHeight)) % 2 == 0)
            {
                Rect bgRect = new Rect(0f, y, width, RowHeight);
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
                Widgets.DrawBoxSolid(bgRect, GUI.color);
                GUI.color = Color.white;
            }

            var stats = pawn.GetComp<CompRPGStats>();
            float currentX = 10f;
            
            // Nom du pawn
            Rect nameRect = new Rect(currentX, y, NameColumnWidth, RowHeight);
            Widgets.Label(nameRect, pawn.Name?.ToStringShort ?? "Inconnu");
            currentX += NameColumnWidth + 10f;
            
            if (stats == null)
            {
                // Afficher un message si le composant n'existe pas
                Rect noStatsRect = new Rect(currentX, y, StatColumnWidth * 6f, RowHeight);
                GUI.color = Color.red;
                Widgets.Label(noStatsRect, "Composant RPG manquant");
                GUI.color = Color.white;
                return y + RowHeight;
            }

            // Afficher chaque statistique
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                Rect statRect = new Rect(currentX, y, StatColumnWidth, RowHeight);
                
                var stat = stats.GetStat(statType);
                if (stat != null)
                {
                    int nextLevelExp = stats.GetRequiredExperienceForLevel(stat.level + 1);
                    string statText = $"Niv.{stat.level}\n({stat.experience}/{nextLevelExp})";
                    
                    // Couleur basée sur le niveau
                    GUI.color = GetLevelColor(stat.level);
                    
                    // Affichage avec tooltip
                    Widgets.Label(statRect, statText);
                    
                    // Tooltip avec détails
                    if (Mouse.IsOver(statRect))
                    {
                        int expNeeded = nextLevelExp - stat.experience;
                        string tooltip = $"{CompRPGStats.GetStatDisplayName(statType)}\n" +
                                       $"Niveau: {stat.level}\n" +
                                       $"Expérience: {stat.experience}/{nextLevelExp}\n" +
                                       $"XP restante: {expNeeded}";
                        TooltipHandler.TipRegion(statRect, tooltip);
                    }
                }
                else
                {
                    GUI.color = Color.red;
                    Widgets.Label(statRect, "ERR");
                }
                
                GUI.color = Color.white;
                currentX += StatColumnWidth + 5f;
            }
            
            // Statut
            Rect statusRect = new Rect(currentX, y, 100f, RowHeight);
            string status = GetPawnStatus(pawn);
            Color statusColor = GetStatusColor(pawn);
            GUI.color = statusColor;
            Widgets.Label(statusRect, status);
            GUI.color = Color.white;

            return y + RowHeight;
        }

        private Color GetLevelColor(int level)
        {
            if (level >= 20) return Color.magenta;      // Légendaire
            if (level >= 15) return Color.red;         // Épique
            if (level >= 10) return Color.blue;        // Rare
            if (level >= 5) return Color.green;        // Bon
            return Color.white;                        // Commun
        }

        private float DrawSeparator(float y, float width)
        {
            y += 10f; // Espacement avant
            
            Rect separatorRect = new Rect(width * 0.1f, y + 10f, width * 0.8f, 2f);
            GUI.color = Color.gray;
            Widgets.DrawBoxSolid(separatorRect, GUI.color);
            GUI.color = Color.white;
            
            return y + 30f; // Espacement après
        }

        private string GetPawnStatus(Pawn pawn)
        {
            if (pawn.Dead) return "Mort";
            if (pawn.Downed) return "À terre";
            if (pawn.InMentalState) return "État mental";
            if (pawn.health.HasHediffsNeedingTend()) return "Blessé";
            return "Actif";
        }

        private Color GetStatusColor(Pawn pawn)
        {
            if (pawn.Dead) return Color.red;
            if (pawn.Downed) return Color.yellow;
            if (pawn.InMentalState) return Color.magenta;
            if (pawn.health.HasHediffsNeedingTend()) return new Color(1f, 0.65f, 0f);
            return Color.green;
        }

        private List<Pawn> GetColonists()
        {
            try
            {
                return Find.CurrentMap?.mapPawns?.FreeColonists?.ToList() ?? new List<Pawn>();
            }
            catch
            {
                return new List<Pawn>();
            }
        }

        private List<Pawn> GetAnimals()
        {
            try
            {
                return Find.CurrentMap?.mapPawns?.PawnsInFaction(Faction.OfPlayer)
                    ?.Where(p => p.RaceProps.Animal)?.ToList() ?? new List<Pawn>();
            }
            catch
            {
                return new List<Pawn>();
            }
        }

        private float GetTotalContentHeight()
        {
            var colonists = GetColonists();
            var animals = GetAnimals();
            
            float height = 0f;
            
            // Section colons
            height += 35f; // Titre
            height += RowHeight + 5f; // En-têtes + ligne
            height += colonists.Count * RowHeight + 10f; // Données + espacement
            
            // Séparateur
            height += 40f;
            
            // Section animaux
            height += 35f; // Titre
            height += RowHeight + 5f; // En-têtes + ligne
            height += animals.Count * RowHeight + 10f; // Données + espacement
            
            // Si pas de données, ajouter de l'espace pour les messages "Aucun..."
            if (!colonists.Any()) height += RowHeight;
            if (!animals.Any()) height += RowHeight;
            
            return height + 50f; // Marge de sécurité
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
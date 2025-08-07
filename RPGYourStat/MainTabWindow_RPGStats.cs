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
        private const float RowHeight = 30f;
        private const float ColumnWidth = 120f;
        private const float SeparatorHeight = 20f;

        public MainTabWindow_RPGStats()
        {
            this.forcePause = false;
            this.closeOnClickedOutside = false;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
        }

        public override Vector2 RequestedTabSize => new Vector2(800f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            
            // En-tête
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "Statistiques RPG des Colons et Animaux");
            Text.Font = GameFont.Small;

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
            Rect headerRect = new Rect(0f, y, width, RowHeight);
            
            Text.Font = GameFont.Small;
            GUI.color = Color.yellow;
            
            // Nom
            Rect nameRect = new Rect(10f, y, ColumnWidth * 1.5f, RowHeight);
            Widgets.Label(nameRect, "Nom");
            
            // Niveau
            Rect levelRect = new Rect(ColumnWidth * 1.5f + 20f, y, ColumnWidth, RowHeight);
            Widgets.Label(levelRect, "Niveau");
            
            // XP
            Rect xpRect = new Rect(ColumnWidth * 2.5f + 30f, y, ColumnWidth, RowHeight);
            Widgets.Label(xpRect, "Expérience");
            
            // XP pour niveau suivant
            Rect nextLevelRect = new Rect(ColumnWidth * 3.5f + 40f, y, ColumnWidth * 1.2f, RowHeight);
            Widgets.Label(nextLevelRect, "XP Niveau Suivant");
            
            // Statut
            Rect statusRect = new Rect(ColumnWidth * 4.7f + 50f, y, ColumnWidth, RowHeight);
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
            
            // Nom du pawn
            Rect nameRect = new Rect(10f, y, ColumnWidth * 1.5f, RowHeight);
            Widgets.Label(nameRect, pawn.Name?.ToStringShort ?? "Inconnu");
            
            if (stats == null)
            {
                // Afficher un message si le composant n'existe pas
                Rect noStatsRect = new Rect(ColumnWidth * 1.5f + 20f, y, ColumnWidth * 3f, RowHeight);
                GUI.color = Color.red;
                Widgets.Label(noStatsRect, "Composant RPG manquant");
                GUI.color = Color.white;
                return y + RowHeight;
            }

            // Niveau
            Rect levelRect = new Rect(ColumnWidth * 1.5f + 20f, y, ColumnWidth, RowHeight);
            GUI.color = Color.green;
            Widgets.Label(levelRect, stats.Level.ToString());
            GUI.color = Color.white;
            
            // XP actuelle
            Rect xpRect = new Rect(ColumnWidth * 2.5f + 30f, y, ColumnWidth, RowHeight);
            Widgets.Label(xpRect, stats.Experience.ToString("F0"));
            
            // XP pour niveau suivant
            Rect nextLevelRect = new Rect(ColumnWidth * 3.5f + 40f, y, ColumnWidth * 1.2f, RowHeight);
            int xpNeeded = Mathf.Max(0, stats.GetRequiredExperienceForLevel(stats.Level + 1) - stats.Experience);
            GUI.color = Color.cyan;
            Widgets.Label(nextLevelRect, xpNeeded.ToString());
            GUI.color = Color.white;
            
            // Statut
            Rect statusRect = new Rect(ColumnWidth * 4.7f + 50f, y, ColumnWidth, RowHeight);
            string status = GetPawnStatus(pawn);
            Color statusColor = GetStatusColor(pawn);
            GUI.color = statusColor;
            Widgets.Label(statusRect, status);
            GUI.color = Color.white;

            return y + RowHeight;
        }

        private float DrawSeparator(float y, float width)
        {
            y += 10f; // Espacement avant
            
            Rect separatorRect = new Rect(width * 0.1f, y + SeparatorHeight / 2f, width * 0.8f, 2f);
            GUI.color = Color.gray;
            Widgets.DrawBoxSolid(separatorRect, GUI.color);
            GUI.color = Color.white;
            
            return y + SeparatorHeight + 10f; // Espacement après
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
            return Find.CurrentMap?.mapPawns?.FreeColonists?.ToList() ?? new List<Pawn>();
        }

        private List<Pawn> GetAnimals()
        {
            return Find.CurrentMap?.mapPawns?.PawnsInFaction(Faction.OfPlayer)
                .Where(p => p.RaceProps.Animal)?.ToList() ?? new List<Pawn>();
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
            height += SeparatorHeight + 20f;
            
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
            Log.Message("L'onglet RPG Stats s'ouvre !");
        }

        public override void PostClose()
        {
            base.PostClose();
            Log.Message("L'onglet RPG Stats se ferme.");
        }
    }
}
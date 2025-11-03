// Fichier: DonTroc/Models/UserProfile.cs

using System;
using System.Collections.Generic;

namespace DonTroc.Models
{
    public class UserProfile // Modèle de données pour le profil utilisateur
    { 
        public string Id { get; set; } = null!;
        public required string? Name { get; set; }
        // Alias pour compatibilité XAML existante (SocialView.xaml lie "Nom")
        public string? Nom { get => Name; set => Name = value; }
        // Alias pour compatibilité avec ReportService
        public string? DisplayName { get => Name; set => Name = value; }
        public string Email { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        // Alias pour compatibilité XAML existante (SocialView.xaml lie "PhotoProfil")
        public string? PhotoProfil { get => ProfilePictureUrl; set => ProfilePictureUrl = value; }

        // === SYSTÈME DE NOTATION ===
        
        /// <summary>
        /// Note moyenne de l'utilisateur (0.0 à 5.0)
        /// </summary>
        public double NoteMoyenne { get; set; } = 0.0;

        /// <summary>
        /// Nombre total d'évaluations reçues
        /// </summary>
        public int NombreEvaluations { get; set; } = 0;

        /// <summary>
        /// Nombre d'échanges terminés avec succès
        /// </summary>
        public int NombreEchangesReussis { get; set; } = 0;

        /// <summary>
        /// Badge de confiance basé sur les évaluations
        /// </summary>
        public BadgeConfiance Badge { get; set; } = BadgeConfiance.Nouveau;

        /// <summary>
        /// Date de membre depuis
        /// </summary>
        public DateTime DateInscription { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Dernières évaluations reçues (pour affichage)
        /// </summary>
        public List<Rating> DernieresEvaluations { get; set; } = new();

        /// <summary>
        /// Points de gamification de l'utilisateur
        /// </summary>
        public int Points { get; set; } = 0;

        /// <summary>
        /// Jeton pour les notifications push (Firebase Cloud Messaging)
        /// </summary>
        public string? FcmToken { get; set; }

        // === MÉTHODES CALCULÉES ===

        /// <summary>
        /// Calcule le badge de confiance basé sur les statistiques
        /// </summary>
        public void CalculerBadgeConfiance()
        {
            if (NombreEvaluations == 0)
            {
                Badge = BadgeConfiance.Nouveau;
                return;
            }

            if (NombreEvaluations >= 50 && NoteMoyenne >= 4.8)
                Badge = BadgeConfiance.Excellence;
            else if (NombreEvaluations >= 25 && NoteMoyenne >= 4.5)
                Badge = BadgeConfiance.Expert;
            else if (NombreEvaluations >= 10 && NoteMoyenne >= 4.0)
                Badge = BadgeConfiance.Confirme;
            else if (NombreEvaluations >= 3 && NoteMoyenne >= 3.5)
                Badge = BadgeConfiance.Fiable;
            else
                Badge = BadgeConfiance.Nouveau;
        }

        /// <summary>
        /// Met à jour les statistiques après une nouvelle évaluation
        /// </summary>
        public void MettreAJourStatistiques(double nouvelleNote)
        {
            var totalPoints = NoteMoyenne * NombreEvaluations + nouvelleNote;
            NombreEvaluations++;
            NoteMoyenne = Math.Round(totalPoints / NombreEvaluations, 1);
            CalculerBadgeConfiance();
        }
    }

    /// <summary>
    /// Énumération des badges de confiance
    /// </summary>
    public enum BadgeConfiance
    {
        Nouveau,        // Utilisateur nouveau ou peu d'évaluations
        Fiable,         // 3+ évaluations, moyenne ≥ 3.5
        Confirme,       // 10+ évaluations, moyenne ≥ 4.0
        Expert,         // 25+ évaluations, moyenne ≥ 4.5
        Excellence      // 50+ évaluations, moyenne ≥ 4.8
        ,
        Bronze,        // 5+ évaluations, moyenne ≥ 3.0
        Argent,        // 15+ évaluations, moyenne ≥ 3.5
        Or,            // 30+ évaluations, moyenne ≥ 4.0
        Platine,       // 50+ évaluations, moyenne ≥ 4.5
        Diamant        // 100+ évaluations, moyenne ≥ 4.8
    }
}

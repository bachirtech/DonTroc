// Fichier: DonTroc/Models/UserProfile.cs

using System;
using System.Collections.Generic;

namespace DonTroc.Models
{
    /// <summary>
    /// Énumération des rôles utilisateur
    /// </summary>
    public enum UserRole
    {
        Standard = 0,    // Utilisateur normal
        Moderator = 1,   // Modérateur (peut gérer les signalements et suspendre temporairement)
        Admin = 2        // Administrateur (tous les droits)
    }

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

        // === SYSTÈME DE RÔLES ET PERMISSIONS ===
        
        /// <summary>
        /// Rôle de l'utilisateur (Standard, Moderator, Admin)
        /// </summary>
        public UserRole Role { get; set; } = UserRole.Standard;

        /// <summary>
        /// Indique si l'utilisateur est suspendu
        /// </summary>
        public bool IsSuspended { get; set; } = false;

        /// <summary>
        /// Raison de la suspension (si applicable)
        /// </summary>
        public string? SuspensionReason { get; set; }

        /// <summary>
        /// Date de début de la suspension
        /// </summary>
        public DateTime? SuspensionDate { get; set; }

        /// <summary>
        /// Date de fin de la suspension (null = indéfinie)
        /// </summary>
        public DateTime? SuspensionEndDate { get; set; }

        /// <summary>
        /// ID de l'admin qui a effectué la suspension
        /// </summary>
        public string? SuspendedBy { get; set; }

        /// <summary>
        /// Vérifie si l'utilisateur est admin
        /// </summary>
        public bool IsAdmin => Role == UserRole.Admin;

        /// <summary>
        /// Vérifie si l'utilisateur est modérateur ou admin
        /// </summary>
        public bool IsModerator => Role == UserRole.Moderator || Role == UserRole.Admin;

        /// <summary>
        /// Vérifie si l'utilisateur peut accéder au panneau d'administration
        /// </summary>
        public bool CanAccessAdminPanel => IsModerator && !IsSuspended;

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

        // === LOCALISATION POUR NOTIFICATIONS DE PROXIMITÉ ===

        /// <summary>
        /// Latitude de la dernière position connue de l'utilisateur
        /// </summary>
        public double? LastLatitude { get; set; }

        /// <summary>
        /// Longitude de la dernière position connue de l'utilisateur
        /// </summary>
        public double? LastLongitude { get; set; }

        /// <summary>
        /// Date de la dernière mise à jour de la position
        /// </summary>
        public DateTime? LastLocationUpdate { get; set; }

        /// <summary>
        /// GeoHash encodé de la dernière position (précision 4 = ~39km × 20km).
        /// Utilisé pour les requêtes géospatiales efficaces sur Firebase.
        /// </summary>
        public string? GeoHash { get; set; }

        /// <summary>
        /// Rayon de notification préféré (en km) - par défaut 50 km
        /// </summary>
        public double NotificationRadius { get; set; } = 50.0;

        /// <summary>
        /// Activer/désactiver les notifications de proximité
        /// </summary>
        public bool ProximityNotificationsEnabled { get; set; } = true;

        // === TRACKING ACTIVITÉ (pour rappels push serveur) ===

        /// <summary>
        /// Timestamp (epoch ms) de la dernière activité de l'utilisateur.
        /// Mis à jour à chaque ouverture/reprise de l'app.
        /// Utilisé par les Cloud Functions de rétention.
        /// </summary>
        public long? LastActiveAt { get; set; }

        /// <summary>
        /// Préférences de notification push par type.
        /// Clés : "reminder_j1", "reminder_j3", "reminder_j7", "reminder_j14"
        /// Valeur : true (opt-in, défaut) ou false (opt-out)
        /// </summary>
        public Dictionary<string, bool> NotificationPreferences { get; set; } = new();

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

        /// <summary>
        /// Vérifie si la suspension est expirée et la lève automatiquement
        /// </summary>
        public bool CheckAndClearExpiredSuspension()
        {
            if (IsSuspended && SuspensionEndDate.HasValue && SuspensionEndDate.Value <= DateTime.UtcNow)
            {
                IsSuspended = false;
                SuspensionReason = null;
                SuspensionDate = null;
                SuspensionEndDate = null;
                SuspendedBy = null;
                return true;
            }
            return false;
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

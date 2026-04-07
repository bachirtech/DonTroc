// Fichier: DonTroc/Models/AdminModels.cs
// Modèles pour le panneau d'administration

using System;

namespace DonTroc.Models
{
    /// <summary>
    /// Types d'actions administratives pour le journal d'audit
    /// </summary>
    public enum AdminActionType
    {
        // Actions sur les utilisateurs
        UserSuspended,
        UserUnsuspended,
        UserPromotedToModerator,
        UserPromotedToAdmin,
        UserDemoted,
        UserDeleted,
        
        // Actions sur les annonces
        AnnonceApproved,
        AnnonceRejected,
        AnnonceDeleted,
        AnnonceFlagged,
        
        // Actions sur les signalements
        ReportReviewed,
        ReportResolved,
        ReportDismissed,
        
        // Actions système
        SettingsChanged,
        BulkAction,
        Other
    }

    /// <summary>
    /// Journal des actions administratives pour l'audit
    /// </summary>
    public class AdminLog
    {
        /// <summary>
        /// Identifiant unique du log
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type d'action effectuée
        /// </summary>
        public AdminActionType ActionType { get; set; }

        /// <summary>
        /// ID de l'administrateur qui a effectué l'action
        /// </summary>
        public string AdminId { get; set; } = null!;

        /// <summary>
        /// Nom de l'administrateur (pour affichage)
        /// </summary>
        public string? AdminName { get; set; }

        /// <summary>
        /// ID de l'élément ciblé (utilisateur, annonce, signalement)
        /// </summary>
        public string? TargetId { get; set; }

        /// <summary>
        /// Type de la cible (User, Annonce, Report)
        /// </summary>
        public string? TargetType { get; set; }

        /// <summary>
        /// Description détaillée de l'action
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Date et heure de l'action
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Adresse IP (optionnel, pour sécurité avancée)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Description formatée pour l'affichage
        /// </summary>
        public string DisplayDescription => ActionType switch
        {
            AdminActionType.UserSuspended => $"🚫 Utilisateur suspendu",
            AdminActionType.UserUnsuspended => $"✅ Suspension levée",
            AdminActionType.UserPromotedToModerator => $"⬆️ Promu modérateur",
            AdminActionType.UserPromotedToAdmin => $"👑 Promu admin",
            AdminActionType.UserDemoted => $"⬇️ Rôle rétrogradé",
            AdminActionType.UserDeleted => $"🗑️ Compte supprimé",
            AdminActionType.AnnonceApproved => $"✅ Annonce approuvée",
            AdminActionType.AnnonceRejected => $"❌ Annonce rejetée",
            AdminActionType.AnnonceDeleted => $"🗑️ Annonce supprimée",
            AdminActionType.AnnonceFlagged => $"🚩 Annonce signalée",
            AdminActionType.ReportReviewed => $"👁️ Signalement examiné",
            AdminActionType.ReportResolved => $"✅ Signalement résolu",
            AdminActionType.ReportDismissed => $"❌ Signalement rejeté",
            AdminActionType.SettingsChanged => $"⚙️ Paramètres modifiés",
            AdminActionType.BulkAction => $"📦 Action en masse",
            _ => $"📝 Action admin"
        };
    }

    /// <summary>
    /// Statistiques globales pour le dashboard admin
    /// </summary>
    public class AdminStats
    {
        /// <summary>
        /// Nombre total d'utilisateurs
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Nombre d'utilisateurs actifs (connectés dans les 30 derniers jours)
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// Nombre d'utilisateurs suspendus
        /// </summary>
        public int SuspendedUsers { get; set; }

        /// <summary>
        /// Nombre total d'annonces
        /// </summary>
        public int TotalAnnonces { get; set; }

        /// <summary>
        /// Nombre d'annonces actives
        /// </summary>
        public int ActiveAnnonces { get; set; }

        /// <summary>
        /// Nombre d'annonces en attente de modération
        /// </summary>
        public int PendingAnnonces { get; set; }

        /// <summary>
        /// Nombre total de signalements
        /// </summary>
        public int TotalReports { get; set; }

        /// <summary>
        /// Nombre de signalements en attente
        /// </summary>
        public int PendingReports { get; set; }

        /// <summary>
        /// Nombre de signalements résolus
        /// </summary>
        public int ResolvedReports { get; set; }

        /// <summary>
        /// Nombre total de transactions
        /// </summary>
        public int TotalTransactions { get; set; }

        /// <summary>
        /// Nombre de nouveaux utilisateurs aujourd'hui
        /// </summary>
        public int NewUsersToday { get; set; }

        /// <summary>
        /// Nombre de nouvelles annonces aujourd'hui
        /// </summary>
        public int NewAnnoncesToday { get; set; }

        /// <summary>
        /// Nombre de modérateurs
        /// </summary>
        public int ModeratorCount { get; set; }

        /// <summary>
        /// Nombre d'administrateurs
        /// </summary>
        public int AdminCount { get; set; }

        /// <summary>
        /// Date de la dernière mise à jour des stats
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Filtre pour la recherche d'utilisateurs dans l'admin
    /// </summary>
    public class UserSearchFilter
    {
        public string? SearchQuery { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsSuspended { get; set; }
        public DateTime? RegisteredAfter { get; set; }
        public DateTime? RegisteredBefore { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }

    /// <summary>
    /// Filtre pour la recherche de signalements
    /// </summary>
    public class ReportSearchFilter
    {
        public string? Status { get; set; }
        public string? ReportType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }
}


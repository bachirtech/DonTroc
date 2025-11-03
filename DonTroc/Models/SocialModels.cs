using System;
using System.Collections.Generic;

namespace DonTroc.Models
{
    /// <summary>
    /// Interface pour les documents Firebase qui ont besoin d'un ID
    /// </summary>
    public interface IFirebaseDocument
    {
        string Id { get; set; }
    }

    /// <summary>
    /// Modèle pour le système de parrainage
    /// </summary>
    public class ReferralCode : IFirebaseDocument
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public int NbUtilisations { get; set; }
        public int MaxUtilisations { get; set; } = 10;
        public bool IsActive { get; set; } = true;
        public List<string> UsersReferred { get; set; } = new();
    }

    /// <summary>
    /// Modèle pour suivre les amitiés
    /// </summary>
    public class Friendship : IFirebaseDocument
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public FriendshipStatus Status { get; set; }
        public string? ReferralCodeUsed { get; set; }
    }

    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    /// <summary>
    /// Modèle pour l'activité des amis
    /// </summary>
    public class FriendActivity : IFirebaseDocument
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserPhotoUrl { get; set; } = string.Empty;
        public ActivityType Type { get; set; }
        public DateTime DateActivite { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? AnnonceId { get; set; }
        public string? AnnonceTitle { get; set; }
        public string? AnnoncePhotoUrl { get; set; }
        public int Points { get; set; }
    }

    public enum ActivityType
    {
        AnnonceCreated,
        TransactionCompleted,
        RatingGiven,
        BadgeEarned,
        MilestoneReached
    }

    /// <summary>
    /// Modèle pour le partage sur les réseaux sociaux
    /// </summary>
    public class SocialShare : IFirebaseDocument
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AnnonceId { get; set; } = string.Empty;
        public SocialPlatform Platform { get; set; }
        public DateTime DatePartage { get; set; }
        public string ShareUrl { get; set; } = string.Empty;
    }

    public enum SocialPlatform
    {
        Facebook,
        Twitter,
        Instagram,
        WhatsApp,
        Telegram,
        Email,
        SMS,
        Native
    }

    /// <summary>
    /// Modèle pour les statistiques sociales
    /// </summary>
    public class SocialStats
    {
        public string UserId { get; set; } = string.Empty;
        public int NombreAmis { get; set; }
        public int NombreParrainages { get; set; }
        public int NombrePartages { get; set; }
        public int PointsGagnesParrainage { get; set; }
        public int PointsGagnesPartage { get; set; }
        public DateTime DerniereActivite { get; set; }
    }
}

using System;

namespace DonTroc.Models
{
    /// <summary>
    /// Représente un conseil ou une astuce pour l'utilisateur
    /// </summary>
    public class Tip
    {
        /// <summary>
        /// Identifiant unique du conseil
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Clé de fonctionnalité associée (ex: "creation_annonce", "favoris", etc.)
        /// </summary>
        public string FeatureKey { get; set; } = string.Empty;

        /// <summary>
        /// Titre du conseil
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Message du conseil
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Icône emoji à afficher
        /// </summary>
        public string Icon { get; set; } = "💡";

        /// <summary>
        /// Ordre d'affichage (si plusieurs conseils pour une même fonctionnalité)
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Position du conseil sur l'écran
        /// </summary>
        public TipPosition Position { get; set; } = TipPosition.Bottom;

        /// <summary>
        /// Indique si le conseil peut être ignoré définitivement
        /// </summary>
        public bool CanBeDismissedPermanently { get; set; } = true;
    }

    /// <summary>
    /// Position d'affichage du conseil
    /// </summary>
    public enum TipPosition
    {
        Top,
        Center,
        Bottom
    }

    /// <summary>
    /// État des conseils vus par l'utilisateur
    /// </summary>
    public class TipState
    {
        /// <summary>
        /// Dictionnaire des conseils vus (Id -> date de vue)
        /// </summary>
        public Dictionary<string, DateTime> SeenTips { get; set; } = new();

        /// <summary>
        /// Dictionnaire des conseils ignorés définitivement
        /// </summary>
        public HashSet<string> DismissedTips { get; set; } = new();

        /// <summary>
        /// Indique si l'utilisateur a désactivé tous les conseils
        /// </summary>
        public bool AllTipsDisabled { get; set; } = false;

        /// <summary>
        /// Date de dernière mise à jour
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration d'un conseil à afficher
    /// </summary>
    public class TipDisplayConfig
    {
        /// <summary>
        /// Le conseil à afficher
        /// </summary>
        public Tip Tip { get; set; } = new();

        /// <summary>
        /// Indique s'il y a d'autres conseils à afficher
        /// </summary>
        public bool HasMoreTips { get; set; } = false;

        /// <summary>
        /// Numéro du conseil actuel (ex: 1/3)
        /// </summary>
        public int CurrentIndex { get; set; } = 1;

        /// <summary>
        /// Nombre total de conseils pour cette fonctionnalité
        /// </summary>
        public int TotalTips { get; set; } = 1;
    }
}


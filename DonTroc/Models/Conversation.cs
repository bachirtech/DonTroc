using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DonTroc.Models
{
    public class Conversation : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string AnnonceId { get; set; } = string.Empty;
        public string AnnonceTitre { get; set; } = string.Empty;
        public string AnnonceImageUrl { get; set; } = string.Empty;
        
        // Ajout des champs manquants
        public string SellerId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public Dictionary<string, bool> ParticipantIds { get; set; } = new();
        public string LastMessage { get; set; } = string.Empty;
        
        // Correction : Utiliser explicitement long pour éviter les conflits de type
        // Firebase stocke les timestamps comme des valeurs long (Unix timestamp en millisecondes)
        public DateTime LastMessageTimestamp { get; set; } = new DateTime(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        
        // Propriété auxiliaire pour le type de message
        public string? LastMessageType { get; set; }
        
        // --- Propriétés pour l'UI, non stockées dans Firebase ---
        public string OtherUserName { get; set; } = string.Empty;
        
        /// <summary>
        /// Propriété calculée qui convertit le timestamp Unix en DateTime
        /// </summary>
        public DateTime LastMessageDateTime 
        {
            get
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(0).DateTime;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
        }

        private int _unreadCount;
        /// <summary>
        /// Nombre de messages non lus dans cette conversation
        /// </summary>
        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                if (_unreadCount != value)
                {
                    _unreadCount = value;
                    OnPropertyChanged(nameof(UnreadCount));
                    OnPropertyChanged(nameof(HasUnreadMessages));
                }
            }
        }

        /// <summary>
        /// Indique s'il y a des messages non lus dans cette conversation
        /// </summary>
        public bool HasUnreadMessages => UnreadCount > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

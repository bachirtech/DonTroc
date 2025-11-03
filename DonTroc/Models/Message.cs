using System;
using System.Collections.Generic;

namespace DonTroc.Models
{
    public class Message
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime SentDateTime { get; set; } // AJOUT: propriété manquante
        public bool IsRead { get; set; }
        
        // Propriétés pour les images
        public string? ImageUrl { get; set; }
        
        // Propriétés pour la géolocalisation
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationLatitude { get; set; } // proprieté 
        
        // Propriétés pour les messages vocaux
        public string? VoiceMessageUrl { get; set; }
        public int VoiceMessageDuration { get; set; } // en secondes
        
        // Propriété calculée côté client
        public bool IsSentByUser { get; set; }
        
        // Propriétés pour les statuts de message
        public MessageStatus Status { get; set; } = MessageStatus.Sent; // AJOUT: propriété manquante
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        
        // Propriétés pour les accusés de réception
        public List<string> ReadByUserIds { get; set; } = new List<string>(); // AJOUT: propriété manquante
        
        // Type de message pour faciliter le rendu
        public MessageType Type => GetMessageType();
        
        private MessageType GetMessageType()
        {
            if (!string.IsNullOrEmpty(VoiceMessageUrl))
                return MessageType.Voice;
            if (!string.IsNullOrEmpty(ImageUrl))
                return MessageType.Image;
            if (Latitude.HasValue && Longitude.HasValue)
                return MessageType.Location;
            return MessageType.Text;
        }
        
        // AJOUT: méthode manquante MarkAsRead
        public void MarkAsRead(string userId)
        {
            if (!ReadByUserIds.Contains(userId))
            {
                ReadByUserIds.Add(userId);
            }
            if (SenderId != userId)
            {
                IsRead = true;
                ReadAt = DateTime.UtcNow;
            }
        }
    }
    
    public enum MessageType
    {
        Text,
        Image,
        Voice,
        Location
    }
    
    // AJOUT: énumération manquante MessageStatus
    public enum MessageStatus
    {
        Sending,
        Sent,
        Delivered,
        Read,
        Failed
    }
}

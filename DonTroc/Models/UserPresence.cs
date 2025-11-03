using System;

namespace DonTroc.Models
{
    /// <summary>
    /// Modèle pour représenter la présence/statut d'un utilisateur
    /// </summary>
    public class UserPresence
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsOnline { get; set; } = false;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Hors ligne"; // "En ligne", "Absent", "Hors ligne"
        
        // Propriétés calculées pour l'affichage
        public string FormattedLastSeen
        {
            get
            {
                if (IsOnline) return "En ligne";
                
                var timeSpan = DateTime.UtcNow - LastSeen;
                
                if (timeSpan.TotalMinutes < 1)
                    return "À l'instant";
                if (timeSpan.TotalMinutes < 60)
                    return $"Il y a {(int)timeSpan.TotalMinutes} min";
                if (timeSpan.TotalHours < 24)
                    return $"Il y a {(int)timeSpan.TotalHours}h";
                if (timeSpan.TotalDays < 7)
                    return $"Il y a {(int)timeSpan.TotalDays} jour{((int)timeSpan.TotalDays > 1 ? "s" : "")}";
                
                return LastSeen.ToString("dd/MM/yyyy");
            }
        }
        
        public string StatusColor => IsOnline ? "#00FF00" : "#808080";
        public string StatusIcon => IsOnline ? "🟢" : "⚫";
    }
}

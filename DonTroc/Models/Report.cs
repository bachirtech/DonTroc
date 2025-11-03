using System;

namespace DonTroc.Models
{
    public class Report
    {
        public string Id { get; set; } = string.Empty;
        public string ReportedItemId { get; set; } = string.Empty; // ID of the Annonce, User, etc.
        public string ReportedItemType { get; set; } = string.Empty; // "Annonce", "User", etc.
        public string ReporterId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // e.g., "Pending", "Reviewed", "ActionTaken"
        public string Notes { get; set; } = string.Empty; // Moderator notes
    }
}


using System;
using System.Collections.Generic;

namespace DonTroc.Models;

/// <summary>
/// Modèle pour représenter une alerte d'annonce similaire
/// </summary>
public class AnnonceAlert
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Nom de l'alerte
    public string? Description { get; set; }
    
    // Critères de recherche
    public List<string> Keywords { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? Location { get; set; }
    public double? MaxDistance { get; set; } // En km
    public string AnnonceType { get; set; } = "both"; // "don", "troc", "both"
    
    // Configuration de l'alerte
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastTriggered { get; set; } = DateTime.MinValue;
    public int TriggerCount { get; set; } = 0;
    
    // Paramètres de notification
    public bool NotifyInApp { get; set; } = true;
    public bool NotifyEmail { get; set; } = false;
    public string NotificationFrequency { get; set; } = "immediate"; // "immediate", "daily", "weekly"
    
    // Métadonnées
    public List<string> MatchedAnnonceIds { get; set; } = new(); // Historique des annonces matchées
}

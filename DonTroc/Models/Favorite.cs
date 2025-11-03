using System;

namespace DonTroc.Models;

/// <summary>
/// Modèle pour représenter un favori d'annonce
/// </summary>
public class Favorite
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AnnonceId { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.Now;
    public string? ListName { get; set; } // Liste personnalisée (optionnel)
    public string? Notes { get; set; } // Notes personnelles de l'utilisateur
    
    // Propriétés pour les données de l'annonce (pour éviter les requêtes multiples)
    public string AnnonceTitle { get; set; } = string.Empty;
    public string AnnonceImageUrl { get; set; } = string.Empty;
    public string AnnonceLocation { get; set; } = string.Empty;
    public string AnnonceAuthorId { get; set; } = string.Empty;
    public string AnnonceAuthorName { get; set; } = string.Empty;
    public DateTime AnnonceCreatedAt { get; set; }
    public string AnnonceType { get; set; } = string.Empty; // "don" ou "troc"
    public string AnnonceCategory { get; set; } = string.Empty;
}

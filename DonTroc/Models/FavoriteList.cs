using System;
using System.Collections.Generic;

namespace DonTroc.Models;

/// <summary>
/// Modèle pour représenter une liste personnalisée de favoris
/// </summary>
public class FavoriteList
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#FF6B6B"; // Couleur pour l'interface
    public string Icon { get; set; } = "heart"; // Icône pour l'interface
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int ItemCount { get; set; } = 0; // Nombre d'éléments dans la liste
    public bool IsDefault { get; set; } = false; // Liste par défaut du système
    
    // Listes prédéfinies
    public static readonly Dictionary<string, FavoriteList> DefaultLists = new()
    {
        {
            "à_récupérer",
            new FavoriteList
            {
                Name = "À récupérer",
                Description = "Annonces que je veux récupérer bientôt",
                Color = "#4CAF50",
                Icon = "package",
                IsDefault = true
            }
        },
        {
            "intéressant",
            new FavoriteList
            {
                Name = "Intéressant",
                Description = "Annonces qui m'intéressent",
                Color = "#2196F3",
                Icon = "star",
                IsDefault = true
            }
        },
        {
            "plus_tard",
            new FavoriteList
            {
                Name = "Plus tard",
                Description = "À regarder plus tard",
                Color = "#FF9800",
                Icon = "clock",
                IsDefault = true
            }
        }
    };
}

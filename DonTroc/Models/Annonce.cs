using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DonTroc.Models;

// Énumération pour les statuts d'annonce
public enum StatutAnnonce
{
    Disponible,
    Reservee,
    Echangee,
    Archivee
}

// Modèle de données pour une annonce
public class Annonce : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    // Identifiant unique de l'annonce (généré par Firebase)
    public string Id { get; set; } = string.Empty;

    // Titre de l'annonce
    public string Titre { get; set; } = string.Empty;

    // Description détaillée de l'objet
    public string Description { get; set; } = string.Empty;

    // Type d'annonce : "Don" ou "Troc"
    public string Type { get; set; } = string.Empty;

    // Catégorie de l'objet (ex: Vêtements, Maison, Livres...)
    public string Categorie { get; set; } = string.Empty;

    // Liste des URLs des photos (jusqu'à 5)
    public List<string> PhotosUrls { get; set; } = new List<string>();

    // Compatibilité avec l'ancienne propriété ImageUrls
    public List<string> ImageUrls => PhotosUrls;

    // Propriété qui retourne la première URL de la liste, ou null si la liste est vide.
    public string? FirstPhotoUrl => PhotosUrls?.Count > 0 ? PhotosUrls[0] : null;

    // Propriété pour compter les photos de manière sécurisée
    public int PhotosCount => PhotosUrls?.Count ?? 0;

    // Identifiant de l'utilisateur qui a posté l'annonce
    public string UtilisateurId { get; set; } = string.Empty;

    // Date de création de l'annonce (pour l'application)
    [Newtonsoft.Json.JsonIgnore]
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Timestamp de création pour la sérialisation Firebase
    public long DateCreationTimestamp { get; set; }

    // Date de modification de l'annonce (ajoutée pour corriger l'erreur)
    public DateTime? DateModification { get; set; }

    // === PROPRIÉTÉS DE GÉOLOCALISATION ===
    
    // Localisation de l'annonce (ajoutée pour corriger l'erreur)
    public string? Localisation { get; set; }

    // Latitude pour la géolocalisation
    public double? Latitude { get; set; }

    // Longitude pour la géolocalisation
    public double? Longitude { get; set; }

    // Distance calculée par rapport à l'utilisateur (en km) - pour le tri et l'affichage
    public double? DistanceFromUser { get; set; }

    // Propriété calculée pour formater la distance d'affichage
    public string DistanceFormatted
    {
        get
        {
            if (!DistanceFromUser.HasValue || DistanceFromUser == double.MaxValue)
                return "inconnue";
                
            if (DistanceFromUser < 1)
                return $"{(int)(DistanceFromUser * 1000)} m"; // Afficher en mètres si < 1km
                
            return $"{DistanceFromUser:F1} km";
        }
    }

    // Adresse complète (pour affichage)
    public string? AdresseComplete { get; set; }

    // Ville
    public string? Ville { get; set; }

    // Code postal
    public string? CodePostal { get; set; }

    // === PROPRIÉTÉS DE BOOST ===
    
    // Date d'expiration du boost (si l'annonce est boostée)
    public DateTime? BoostExpirationDate { get; set; }

    // Propriété calculée pour savoir si l'annonce est actuellement boostée
    public bool IsBoosted => BoostExpirationDate.HasValue && BoostExpirationDate.Value > DateTime.UtcNow;

    // === PROPRIÉTÉS DE STATUT ===
    
    // Statut de l'annonce
    public StatutAnnonce Statut { get; set; } = StatutAnnonce.Disponible;
    
    // Propriété pour indiquer si l'annonce est en favoris (pour l'UI)
    private bool _isFavorite = false;
    public bool IsFavorite 
    { 
        get => _isFavorite;
        set
        {
            if (_isFavorite != value)
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }
    }

    // Nombre de vues de l'annonce
    public int NombreVues { get; set; } = 0;

    // Nombre de favoris
    public int NombreFavoris { get; set; } = 0;

    // === PROPRIÉTÉS POUR LE TROC ===
    
    // Pour les trocs : liste des catégories recherchées
    public List<string>? CategoriesRecherchees { get; set; }

    // Pour les trocs : description de ce qui est recherché
    public string? DescriptionRecherche { get; set; }

    // === MÉTHODES UTILITAIRES ===

    // Méthode pour vérifier si l'annonce peut être modifiée
    public bool PeutEtreModifiee()
    {
        return Statut == StatutAnnonce.Disponible || Statut == StatutAnnonce.Reservee;
    }

    // Méthode pour calculer l'âge de l'annonce en jours
    public int AgeEnJours()
    {
        return (DateTime.UtcNow - DateCreation).Days;
    }

    // Méthode pour obtenir le texte d'affichage du statut
    public string GetStatutText()
    {
        return Statut switch
        {
            StatutAnnonce.Disponible => "Disponible",
            StatutAnnonce.Reservee => "Réservée",
            StatutAnnonce.Echangee => "Échangée",
            StatutAnnonce.Archivee => "Archivée",
            _ => "Inconnu"
        };
    }

    // Méthode pour vérifier si l'annonce est récente (moins de 7 jours)
    public bool EstRecente()
    {
        return AgeEnJours() <= 7;
    }

    // Méthode pour obtenir une description courte (limitée à 100 caractères)
    public string GetDescriptionCourte()
    {
        if (string.IsNullOrEmpty(Description))
            return string.Empty;
            
        return Description.Length <= 100 ? Description : Description.Substring(0, 100) + "...";
    }
}

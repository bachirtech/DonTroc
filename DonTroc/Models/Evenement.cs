using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DonTroc.Models;

/// <summary>
/// Type d'événement DonTroc.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TypeEvenement
{
    /// <summary>Vide-grenier virtuel : un créateur invite des amis qui apportent leurs annonces.</summary>
    VideGrenier,
    /// <summary>Événement saisonnier officiel (créé par un admin).</summary>
    Saisonnier,
    /// <summary>Événement local géolocalisé : troc de quartier avec point de RDV physique.</summary>
    LocalGeolocalise
}

/// <summary>
/// Saisons / thèmes officiels pour les événements `Saisonnier`.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum SaisonEvenement
{
    Rentree,
    NoelSolidaire,
    RamadanPartage,
    PrintempsTroc,
    EteVacances,
    Halloween
}

/// <summary>
/// Statut courant d'un événement.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum StatutEvenement
{
    Brouillon,
    AVenir,
    EnCours,
    Termine,
    Annule
}

/// <summary>
/// Modèle d'événement / troc groupé DonTroc.
/// Stocké dans le nœud Realtime DB <c>Events</c>.
/// </summary>
public class Evenement : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // === IDENTITÉ ===
    public string Id { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    /// <summary>Catégorie principale (ex : "Rentrée", "Famille", "Quartier"). Optionnel.</summary>
    public string? Categorie { get; set; }

    // === CRÉATEUR ===
    public string CreateurId { get; set; } = string.Empty;
    public string CreateurName { get; set; } = string.Empty;
    public string? CreateurAvatarUrl { get; set; }

    // === TYPE & STATUT ===
    public TypeEvenement Type { get; set; } = TypeEvenement.VideGrenier;
    public SaisonEvenement? Saison { get; set; }
    public StatutEvenement Statut { get; set; } = StatutEvenement.AVenir;

    /// <summary>True pour les événements officiels (saisonniers admin) avec broadcast FCM.</summary>
    public bool IsOfficial { get; set; }

    // === DATES ===
    [Newtonsoft.Json.JsonIgnore]
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public long DateCreationTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [Newtonsoft.Json.JsonIgnore]
    public DateTime DateDebut { get; set; } = DateTime.UtcNow.AddDays(7);
    public long DateDebutTimestamp { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public DateTime DateFin { get; set; } = DateTime.UtcNow.AddDays(7).AddHours(3);
    public long DateFinTimestamp { get; set; }

    // === GÉOLOCALISATION (pour Type == LocalGeolocalise) ===
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? AdresseRdv { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }

    /// <summary>Distance par rapport à l'utilisateur (calculée côté client, non persistée).</summary>
    [Newtonsoft.Json.JsonIgnore]
    public double? DistanceFromUser { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public string DistanceFormatted
    {
        get
        {
            if (!DistanceFromUser.HasValue || DistanceFromUser == double.MaxValue)
                return "—";
            if (DistanceFromUser < 1)
                return $"{(int)(DistanceFromUser * 1000)} m";
            return $"{DistanceFromUser:F1} km";
        }
    }

    // === PARTICIPATION ===
    public int? NombreMaxParticipants { get; set; }

    private int _nombreParticipants;
    public int NombreParticipants
    {
        get => _nombreParticipants;
        set { if (_nombreParticipants != value) { _nombreParticipants = value; OnPropertyChanged(); } }
    }

    /// <summary>Liste des IDs d'annonces exposées dans le marché virtuel de l'événement.</summary>
    public List<string> AnnoncesIds { get; set; } = new();

    private bool _estParticipant;
    [Newtonsoft.Json.JsonIgnore]
    public bool EstParticipant
    {
        get => _estParticipant;
        set { if (_estParticipant != value) { _estParticipant = value; OnPropertyChanged(); } }
    }

    // === MÉTHODES UTILITAIRES ===

    /// <summary>Synchronise les timestamps Firebase avec les DateTime .NET (à appeler avant un Put).</summary>
    public void SyncTimestamps()
    {
        DateCreationTimestamp = new DateTimeOffset(DateCreation, TimeSpan.Zero).ToUnixTimeMilliseconds();
        DateDebutTimestamp = new DateTimeOffset(DateDebut.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeMilliseconds();
        DateFinTimestamp = new DateTimeOffset(DateFin.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeMilliseconds();
    }

    /// <summary>Reconstruit les DateTime à partir des timestamps (à appeler après un OnceAsync).</summary>
    public void RestoreDatesFromTimestamps()
    {
        if (DateCreationTimestamp > 0)
            DateCreation = DateTimeOffset.FromUnixTimeMilliseconds(DateCreationTimestamp).UtcDateTime;
        if (DateDebutTimestamp > 0)
            DateDebut = DateTimeOffset.FromUnixTimeMilliseconds(DateDebutTimestamp).UtcDateTime;
        if (DateFinTimestamp > 0)
            DateFin = DateTimeOffset.FromUnixTimeMilliseconds(DateFinTimestamp).UtcDateTime;
    }

    public bool EstAVenir() => DateDebut > DateTime.UtcNow;
    public bool EstEnCours() => DateDebut <= DateTime.UtcNow && DateFin >= DateTime.UtcNow;
    public bool EstTermine() => DateFin < DateTime.UtcNow;

    public int JoursAvantDebut()
    {
        var span = DateDebut - DateTime.UtcNow;
        return (int)Math.Ceiling(span.TotalDays);
    }

    public string GetCountdownText()
    {
        if (EstTermine()) return "Terminé";
        if (EstEnCours()) return "🔴 En cours";
        var jours = JoursAvantDebut();
        return jours switch
        {
            <= 0 => "Aujourd'hui",
            1 => "Demain",
            _ => $"J-{jours}"
        };
    }

    /// <summary>Texte de countdown pour binding XAML (alias propriété de <see cref="GetCountdownText"/>).</summary>
    [Newtonsoft.Json.JsonIgnore]
    public string CountdownText => GetCountdownText();

    public bool ComplèteSurParticipants()
        => NombreMaxParticipants.HasValue && NombreParticipants >= NombreMaxParticipants.Value;

    public bool PeutEtreModifie(string? userId)
        => !string.IsNullOrEmpty(userId)
           && userId == CreateurId
           && Statut != StatutEvenement.Termine
           && Statut != StatutEvenement.Annule;
}


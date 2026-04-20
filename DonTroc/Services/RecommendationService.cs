// Fichier: DonTroc/Services/RecommendationService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services;

/// <summary>
/// Service de recommandation personnalisée pour le dashboard.
/// Analyse les favoris, la géolocalisation et les préférences utilisateur
/// pour proposer des annonces pertinentes.
/// </summary>
public class RecommendationService
{
    private readonly FirebaseService _firebaseService;
    private readonly FavoritesService _favoritesService;
    private readonly GeolocationService _geolocationService;
    private readonly AuthService _authService;
    private readonly ILogger<RecommendationService> _logger;

    // Cache léger pour éviter de re-calculer trop souvent
    private List<Annonce>? _cachedRecommandations;
    private List<Annonce>? _cachedNearby;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Rayon maximal (km) pour les recommandations et le contact entre utilisateurs.
    /// Garantit des échanges locaux et évite que des utilisateurs éloignés
    /// (ex : Maroc ↔ France) puissent interagir.
    /// </summary>
    public const double MAX_LOCAL_RADIUS_KM = 50.0;

    public RecommendationService(
        FirebaseService firebaseService,
        FavoritesService favoritesService,
        GeolocationService geolocationService,
        AuthService authService,
        ILogger<RecommendationService> logger)
    {
        _firebaseService = firebaseService;
        _favoritesService = favoritesService;
        _geolocationService = geolocationService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Obtient les annonces recommandées pour l'utilisateur basées sur ses favoris et préférences.
    /// </summary>
    public async Task<List<Annonce>> GetRecommandationsAsync(int maxResults = 10)
    {
        try
        {
            if (_cachedRecommandations != null && DateTime.Now - _lastCacheTime < _cacheDuration)
                return _cachedRecommandations;

            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return new List<Annonce>();

            // 1. Récupérer toutes les annonces
            var allAnnonces = (await _firebaseService.GetAllAnnoncesAsync())
                .Where(a => a.UtilisateurId != userId && a.Statut == StatutAnnonce.Disponible)
                .ToList();

            if (!allAnnonces.Any())
                return new List<Annonce>();

            // 1bis. 🌍 Filtrer par rayon géographique (50 km max) pour garantir des échanges locaux.
            // Si la position de l'utilisateur n'est pas connue, on ne recommande RIEN
            // (mieux vaut une liste vide qu'une liste d'annonces inaccessibles à l'utilisateur).
            var userLocation = _geolocationService.GetLastKnownLocation();
            if (userLocation == null)
            {
                _logger.LogInformation("Recommandations : aucune position GPS connue, retourne liste vide");
                return new List<Annonce>();
            }

            allAnnonces = _geolocationService
                .FilterAnnoncesByRadius(allAnnonces, userLocation, MAX_LOCAL_RADIUS_KM)
                .ToList();

            if (!allAnnonces.Any())
                return new List<Annonce>();

            // Calculer les distances pour affichage
            foreach (var a in allAnnonces)
            {
                if (a.Latitude.HasValue && a.Longitude.HasValue)
                {
                    a.DistanceFromUser = _geolocationService.CalculateDistance(
                        userLocation.Latitude, userLocation.Longitude,
                        a.Latitude.Value, a.Longitude.Value);
                }
            }

            // 2. Analyser les préférences à partir des favoris
            var preferences = await AnalyzeUserPreferencesAsync(userId);

            // 3. Scorer chaque annonce selon les préférences
            var scoredAnnonces = allAnnonces.Select(a => new
            {
                Annonce = a,
                Score = CalculateRelevanceScore(a, preferences)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Annonce.DateCreation)
            .Take(maxResults)
            .Select(x => x.Annonce)
            .ToList();

            _cachedRecommandations = scoredAnnonces;
            _lastCacheTime = DateTime.Now;

            return scoredAnnonces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du calcul des recommandations");
            return new List<Annonce>();
        }
    }

    /// <summary>
    /// Obtient les annonces proches de l'utilisateur, triées par distance.
    /// Utilise le cache de géolocation pour ne pas demander la position à chaque fois.
    /// </summary>
    public async Task<(List<Annonce> annonces, string locationText)> GetNearbyAnnoncesAsync(int maxResults = 10)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return (new List<Annonce>(), "");

            var allAnnonces = (await _firebaseService.GetAllAnnoncesAsync())
                .Where(a => a.UtilisateurId != userId && a.Statut == StatutAnnonce.Disponible)
                .ToList();

            if (!allAnnonces.Any())
                return (new List<Annonce>(), "");

            // Utiliser le cache de localisation d'abord pour éviter le popup GPS
            var location = _geolocationService.GetLastKnownLocation();
            var locationText = "";
            
            if (location != null)
            {
                try
                {
                    locationText = await _geolocationService.GetApproximateLocationAsync(
                        location.Latitude, location.Longitude);
                }
                catch
                {
                    locationText = "Votre zone";
                }
            }
            else
            {
                // 🌍 Pas de localisation connue : on ne peut pas garantir la proximité,
                // donc on ne montre AUCUNE annonce "près de chez vous" (cohérent avec
                // la règle des échanges locaux 50 km).
                _logger.LogInformation("Près de chez vous : aucune position GPS connue, liste vide");
                return (new List<Annonce>(), "");
            }

            // Filtrer et trier par distance (50 km max — même rayon que recommandations et contact)
            var nearbyAnnonces = _geolocationService
                .FilterAnnoncesByRadius(allAnnonces, location, MAX_LOCAL_RADIUS_KM)
                .OrderBy(a => a.DistanceFromUser ?? double.MaxValue)
                .Take(maxResults)
                .ToList();

            // Calculer les distances pour l'affichage
            foreach (var annonce in nearbyAnnonces)
            {
                if (annonce.Latitude.HasValue && annonce.Longitude.HasValue)
                {
                    annonce.DistanceFromUser = _geolocationService.CalculateDistance(
                        location.Latitude, location.Longitude,
                        annonce.Latitude.Value, annonce.Longitude.Value);
                }
            }

            _cachedNearby = nearbyAnnonces;

            return (nearbyAnnonces, locationText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des annonces proches");
            return (new List<Annonce>(), "");
        }
    }

    /// <summary>
    /// Analyse les préférences de l'utilisateur à partir de ses favoris.
    /// </summary>
    private async Task<UserPreferences> AnalyzeUserPreferencesAsync(string userId)
    {
        var preferences = new UserPreferences();

        try
        {
            var favorites = await _favoritesService.GetUserFavoritesAsync();

            if (!favorites.Any())
                return preferences;

            // Compter les catégories favorites
            var categoryCounts = favorites
                .Where(f => !string.IsNullOrEmpty(f.AnnonceCategory))
                .GroupBy(f => f.AnnonceCategory)
                .ToDictionary(g => g.Key, g => g.Count());

            preferences.PreferredCategories = categoryCounts;

            // Compter les types préférés (don/troc)
            var typeCounts = favorites
                .Where(f => !string.IsNullOrEmpty(f.AnnonceType))
                .GroupBy(f => f.AnnonceType)
                .ToDictionary(g => g.Key, g => g.Count());

            preferences.PreferredTypes = typeCounts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur lors de l'analyse des préférences utilisateur");
        }

        return preferences;
    }

    /// <summary>
    /// Calcule un score de pertinence pour une annonce donnée en fonction des préférences.
    /// </summary>
    private double CalculateRelevanceScore(Annonce annonce, UserPreferences preferences)
    {
        double score = 0;

        // Bonus pour les annonces récentes (< 24h)
        if (annonce.DateCreation > DateTime.UtcNow.AddHours(-24))
            score += 30;
        else if (annonce.DateCreation > DateTime.UtcNow.AddDays(-3))
            score += 15;
        else if (annonce.DateCreation > DateTime.UtcNow.AddDays(-7))
            score += 5;

        // Bonus pour catégorie préférée
        if (preferences.PreferredCategories.TryGetValue(annonce.Categorie, out var catCount))
        {
            score += Math.Min(catCount * 10, 50); // Max 50 points de bonus catégorie
        }

        // Bonus pour type préféré
        if (preferences.PreferredTypes.TryGetValue(annonce.Type, out var typeCount))
        {
            score += Math.Min(typeCount * 5, 20); // Max 20 points de bonus type
        }

        // Bonus si l'annonce est boostée
        if (annonce.IsBoosted)
            score += 10;

        // Bonus si l'annonce a beaucoup de vues (populaire)
        if (annonce.NombreVues > 10)
            score += 5;
        if (annonce.NombreVues > 50)
            score += 5;

        return score;
    }

    /// <summary>
    /// Invalide le cache pour forcer un nouveau calcul.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedRecommandations = null;
        _cachedNearby = null;
        _lastCacheTime = DateTime.MinValue;
    }

    /// <summary>
    /// Préférences de l'utilisateur extraites des favoris.
    /// </summary>
    private class UserPreferences
    {
        public Dictionary<string, int> PreferredCategories { get; set; } = new();
        public Dictionary<string, int> PreferredTypes { get; set; } = new();
    }
}


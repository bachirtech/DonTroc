// Fichier: DonTroc/Services/GeolocationService.cs

using System;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui;
using System.Collections.Generic;
using System.Linq;
using DonTroc.Models;
using Microsoft.Maui.Controls;

namespace DonTroc.Services;

// Service pour gérer la récupération de la géolocalisation
public class GeolocationService
{
    private Location? _lastKnownLocation; // Cache de la dernière position connue
    private GeolocationRequest _request;

    public GeolocationService()
    {
        // Configure la requête pour une précision élevée et un timeout plus long
        _request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));
    }

    // Vérifie si la géolocalisation est supportée sur l'appareil
    public bool IsLocationSupported
    {
        get
        {
            try
            {
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // Méthode principale pour obtenir la localisation
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            // 1. Vérifier si les services de localisation sont activés
            if (!IsLocationSupported)
            {
                await Application.Current!.MainPage!.DisplayAlert("Service indisponible",
                    "Les services de localisation ne sont pas disponibles sur cet appareil.", "OK");
                return _lastKnownLocation;
            }

            // 2. Vérifier les permissions de localisation
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Permissions.RequestAsync<Permissions.LocationWhenInUse>());
            }

            if (status != PermissionStatus.Granted)
            {
                await Application.Current!.MainPage!.DisplayAlert("Permission refusée",
                    "La permission d'accéder à la localisation a été refusée. Activez la géolocalisation dans les paramètres de l'application.",
                    "OK");
                return _lastKnownLocation;
            }

            // 3. Essayer plusieurs méthodes pour obtenir la position
            Location? location;

            // Méthode 1: Position actuelle avec timeout
            try
            {
                location = await Geolocation.GetLocationAsync(_request);
            }
            catch (FeatureNotSupportedException)
            {
                await Application.Current!.MainPage!.DisplayAlert("Fonctionnalité non supportée",
                    "La géolocalisation n'est pas supportée sur cet appareil.", "OK");
                return _lastKnownLocation;
            }
            catch (FeatureNotEnabledException)
            {
                await Application.Current!.MainPage!.DisplayAlert("GPS désactivé",
                    "Veuillez activer le GPS dans les paramètres de votre appareil.", "OK");
                return _lastKnownLocation;
            }
            catch (PermissionException)
            {
                await Application.Current!.MainPage!.DisplayAlert("Permission requise",
                    "L'application a besoin de l'autorisation d'accéder à votre localisation.", "OK");
                return _lastKnownLocation;
            }

            // Méthode 2: Si échec, essayer la dernière position connue
            if (location == null)
            {
                try
                {
                    location = await Geolocation.GetLastKnownLocationAsync();
                }
                catch
                {
                    // Ignorer l'erreur et continuer
                }
            }

            // Méthode 3: Si toujours null, essayer avec une précision réduite
            if (location == null)
            {
                try
                {
                    var fallbackRequest = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15));
                    location = await Geolocation.GetLocationAsync(fallbackRequest);
                }
                catch
                {
                    // Dernière tentative échouée
                }
            }

            // 4. Validation de la position obtenue
            if (location != null && IsValidLocation(location))
            {
                _lastKnownLocation = location;
                return location;
            }
            else if (_lastKnownLocation != null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Position approximative",
                    "Impossible d'obtenir votre position actuelle. Utilisation de la dernière position connue.", "OK");
                return _lastKnownLocation;
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("Position introuvable",
                    "Impossible de récupérer votre position. Assurez-vous que :\n" +
                    "• Le GPS est activé\n" +
                    "• Vous êtes à l'extérieur ou près d'une fenêtre\n" +
                    "• L'application a les permissions nécessaires\n" +
                    "• Les services de localisation sont activés", "OK");
                return null;
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erreur de géolocalisation",
                $"Erreur technique : {ex.Message}\n\nVeuillez :\n" +
                "• Redémarrer l'application\n" +
                "• Vérifier vos paramètres de localisation\n" +
                "• Vous assurer d'être connecté", "OK");
            return _lastKnownLocation;
        }
    }

    // Vérifie si une position est valide
    private bool IsValidLocation(Location? location)
    {
        return location != null &&
               location.Latitude != 0 &&
               location.Longitude != 0 &&
               Math.Abs(location.Latitude) <= 90 &&
               Math.Abs(location.Longitude) <= 180;
    }

    // Obtenir la dernière position en cache
    public Location? GetLastKnownLocation()
    {
        return _lastKnownLocation;
    }

    // Vérifier rapidement le statut des permissions
    public async Task<bool> HasLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        return status == PermissionStatus.Granted;
    }

    // Méthode améliorée pour calculer la distance entre deux points GPS
    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Vérifier les coordonnées valides
        if (lat1 == 0 && lon1 == 0 || lat2 == 0 && lon2 == 0)
            return double.MaxValue;

        // Utiliser la formule de Haversine pour un calcul précis
        const double R = 6371; // Rayon de la Terre en kilomètres

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        return distance;
    }

    // Convertit les degrés en radians
    private double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    // === NOUVELLES FONCTIONNALITÉS AVANCÉES ===

    /// <summary>
    /// Filtre les annonces par rayon personnalisable
    /// </summary>
    public List<Annonce> FilterAnnoncesByRadius(List<Annonce> annonces, Location userLocation, double radiusKm)
    {
        if (userLocation == null || annonces == null)
            return annonces ?? new List<Annonce>();

        return annonces.Where(annonce =>
        {
            // Vérifier que les coordonnées de l'annonce sont valides
            if (!annonce.Latitude.HasValue || !annonce.Longitude.HasValue)
                return false; // Exclure les annonces sans coordonnées

            if (annonce.Latitude == 0 && annonce.Longitude == 0)
                return false; // Exclure les annonces avec coordonnées par défaut

            var distance = CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                annonce.Latitude.Value, annonce.Longitude.Value);

            return distance <= radiusKm;
        }).ToList();
    }

    /// <summary>
    /// Obtient les annonces proches triées par distance
    /// </summary>
    public async Task<List<Annonce>> GetNearbyAnnoncesAsync(List<Annonce> allAnnonces, double radiusKm = 10)
    {
        var userLocation = await GetCurrentLocationAsync();
        if (userLocation == null)
            return allAnnonces;

        var nearbyAnnonces = FilterAnnoncesByRadius(allAnnonces, userLocation, radiusKm);

        // Calculer et assigner les distances
        foreach (var annonce in nearbyAnnonces)
        {
            if (annonce.Latitude.HasValue && annonce.Longitude.HasValue)
            {
                annonce.DistanceFromUser = CalculateDistance(
                    userLocation.Latitude, userLocation.Longitude,
                    annonce.Latitude.Value, annonce.Longitude.Value);
            }
        }

        // Trier par distance croissante
        return nearbyAnnonces.OrderBy(a => a.DistanceFromUser ?? double.MaxValue).ToList();
    }

    /// <summary>
    /// Obtient des suggestions d'annonces "près de chez vous"
    /// </summary>
    public async Task<List<Annonce>> GetSuggestionsNearbyAsync(List<Annonce> allAnnonces, int maxSuggestions = 5)
    {
        var nearbyAnnonces = await GetNearbyAnnoncesAsync(allAnnonces, 5.0); // Dans un rayon de 5km
        return nearbyAnnonces.Take(maxSuggestions).ToList();
    }

    /// <summary>
    /// Détermine le quartier/ville approximatif basé sur les coordonnées
    /// </summary>
    public async Task<string> GetApproximateLocationAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(latitude, longitude);
            var placemark = placemarks.FirstOrDefault();

            if (placemark != null)
            {
                // Retourner quartier/ville
                if (!string.IsNullOrEmpty(placemark.SubLocality))
                    return placemark.SubLocality;
                if (!string.IsNullOrEmpty(placemark.Locality))
                    return placemark.Locality;
                if (!string.IsNullOrEmpty(placemark.AdminArea))
                    return placemark.AdminArea;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur géocodage: {ex.Message}");
        }

        return "Localisation inconnue";
    }

    /// <summary>
    /// Obtient la localisation approximative de l'utilisateur
    /// </summary>
    public async Task<string> GetUserApproximateLocationAsync()
    {
        var location = await GetCurrentLocationAsync();
        if (location == null)
            return "Position non disponible";

        return await GetApproximateLocationAsync(location.Latitude, location.Longitude);
    }

    /// <summary>
    /// Groupe les annonces par zone géographique
    /// </summary>
    public async Task<Dictionary<string, List<Annonce>>> GroupAnnoncesByLocationAsync(List<Annonce> annonces)
    {
        var groupedAnnonces = new Dictionary<string, List<Annonce>>();

        foreach (var annonce in annonces)
        {
            // Vérifier que les coordonnées sont valides et non nulles
            if (!annonce.Latitude.HasValue || !annonce.Longitude.HasValue)
                continue;

            if (annonce.Latitude == 0 && annonce.Longitude == 0)
                continue;

            var locationName = await GetApproximateLocationAsync(annonce.Latitude.Value, annonce.Longitude.Value);

            if (!groupedAnnonces.ContainsKey(locationName))
                groupedAnnonces[locationName] = new List<Annonce>();

            groupedAnnonces[locationName].Add(annonce);
        }

        return groupedAnnonces;
    }

    /// <summary>
    /// Calcule la zone de recherche optimale basée sur la densité d'annonces
    /// </summary>
    public async Task<double> GetOptimalSearchRadiusAsync(List<Annonce> allAnnonces, int targetAnnonceCount = 10)
    {
        var userLocation = await GetCurrentLocationAsync();
        if (userLocation == null)
            return 10.0; // Rayon par défaut

        // Tester différents rayons pour trouver celui qui donne le nombre cible d'annonces
        var radiusOptions = new[] { 1.0, 2.0, 5.0, 10.0, 20.0, 50.0 };

        foreach (var radius in radiusOptions)
        {
            var annoncesByRadius = FilterAnnoncesByRadius(allAnnonces, userLocation, radius);
            if (annoncesByRadius.Count >= targetAnnonceCount)
                return radius;
        }

        return 50.0; // Rayon maximum si pas assez d'annonces trouvées
    }

    /// <summary>
    /// Obtient les coordonnées d'une adresse (géocodage)
    /// </summary>
    public async Task<Location?> GetLocationFromAddressAsync(string address)
    {
        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(address);
            return locations.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur géocodage adresse: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Vérifie si deux points sont dans la même zone géographique approximative
    /// </summary>
    public Task<bool> AreInSameAreaAsync(double lat1, double lon1, double lat2, double lon2, double maxDistanceKm = 5.0)
    {
        var distance = CalculateDistance(lat1, lon1, lat2, lon2);
        return Task.FromResult(distance <= maxDistanceKm);
    }
}
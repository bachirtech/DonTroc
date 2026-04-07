using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DonTroc.Models;
using Microsoft.Maui.Devices.Sensors;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour gérer les notifications de proximité des nouvelles annonces.
    /// Notifie les utilisateurs dans un rayon défini (par défaut 50 km) lorsqu'une nouvelle annonce est publiée.
    /// </summary>
    public class ProximityNotificationService
    {
        private readonly ILogger<ProximityNotificationService> _logger;
        private readonly FirebaseService _firebaseService;
        private readonly NotificationService _notificationService;
        private readonly GeolocationService _geolocationService;
        private readonly AuthService _authService;
        private readonly PushNotificationService _pushNotificationService;

        // Rayon par défaut en kilomètres
        private const double DEFAULT_RADIUS_KM = 50.0;
        
        // Limite du nombre de notifications à envoyer par annonce
        private const int MAX_NOTIFICATIONS_PER_ANNONCE = 100;

        public ProximityNotificationService(
            ILogger<ProximityNotificationService> logger,
            FirebaseService firebaseService,
            NotificationService notificationService,
            GeolocationService geolocationService,
            AuthService authService,
            PushNotificationService pushNotificationService)
        {
            _logger = logger;
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _geolocationService = geolocationService;
            _authService = authService;
            _pushNotificationService = pushNotificationService;
        }

        /// <summary>
        /// Met à jour la position de l'utilisateur actuel dans son profil
        /// </summary>
        public async Task UpdateUserLocationAsync()
        {
            try
            {
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Impossible de mettre à jour la position: utilisateur non connecté");
                    return;
                }

                var location = await _geolocationService.GetCurrentLocationAsync();
                if (location == null)
                {
                    _logger.LogWarning("Impossible d'obtenir la position actuelle");
                    return;
                }

                // Mettre à jour le profil utilisateur avec la nouvelle position
                await _firebaseService.UpdateUserLocationAsync(userId, location.Latitude, location.Longitude);
                
                _logger.LogInformation("Position utilisateur mise à jour: {Lat}, {Lon}", 
                    location.Latitude, location.Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la position utilisateur");
            }
        }

        /// <summary>
        /// Notifie tous les utilisateurs dans un rayon de 50 km d'une nouvelle annonce
        /// </summary>
        /// <param name="annonce">L'annonce nouvellement créée</param>
        public async Task NotifyNearbyUsersAsync(Annonce annonce)
        {
            try
            {
                // Vérifier que l'annonce a des coordonnées valides
                if (!annonce.Latitude.HasValue || !annonce.Longitude.HasValue)
                {
                    _logger.LogWarning("Annonce {Id} sans coordonnées, pas de notification de proximité", annonce.Id);
                    return;
                }

                _logger.LogInformation("Recherche des utilisateurs à proximité de l'annonce {Id} ({Lat}, {Lon})",
                    annonce.Id, annonce.Latitude, annonce.Longitude);

                // Récupérer tous les profils utilisateurs avec positions
                var nearbyUsers = await GetUsersNearLocationAsync(
                    annonce.Latitude.Value, 
                    annonce.Longitude.Value, 
                    DEFAULT_RADIUS_KM);

                // Exclure l'auteur de l'annonce
                nearbyUsers = nearbyUsers.Where(u => u.Id != annonce.UtilisateurId).ToList();

                if (nearbyUsers.Count == 0)
                {
                    _logger.LogInformation("Aucun utilisateur à proximité de l'annonce {Id}", annonce.Id);
                    return;
                }

                _logger.LogInformation("{Count} utilisateur(s) trouvé(s) à proximité de l'annonce {Id}",
                    nearbyUsers.Count, annonce.Id);

                // Envoyer les notifications (limiter pour éviter le spam)
                var notificationCount = 0;
                foreach (var user in nearbyUsers.Take(MAX_NOTIFICATIONS_PER_ANNONCE))
                {
                    if (user.ProximityNotificationsEnabled && !string.IsNullOrEmpty(user.FcmToken))
                    {
                        var distance = CalculateDistance(
                            user.LastLatitude!.Value, 
                            user.LastLongitude!.Value,
                            annonce.Latitude.Value, 
                            annonce.Longitude.Value);

                        await SendProximityNotificationToUserAsync(user, annonce, distance);
                        notificationCount++;
                    }
                }

                _logger.LogInformation("{Count} notification(s) de proximité envoyée(s) pour l'annonce {Id}",
                    notificationCount, annonce.Id);

                // Sauvegarder les statistiques de notification
                await SaveNotificationStatsAsync(annonce.Id, notificationCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la notification des utilisateurs à proximité");
            }
        }

        /// <summary>
        /// Récupère les utilisateurs dans un rayon donné d'une position
        /// </summary>
        private async Task<List<UserProfile>> GetUsersNearLocationAsync(double latitude, double longitude, double radiusKm)
        {
            try
            {
                var allUsers = await _firebaseService.GetAllUserProfilesAsync();
                var nearbyUsers = new List<UserProfile>();

                foreach (var user in allUsers)
                {
                    // Vérifier que l'utilisateur a une position valide
                    if (!user.LastLatitude.HasValue || !user.LastLongitude.HasValue)
                        continue;

                    // Vérifier que la position n'est pas trop ancienne (max 30 jours)
                    if (user.LastLocationUpdate.HasValue && 
                        (DateTime.UtcNow - user.LastLocationUpdate.Value).TotalDays > 30)
                        continue;

                    // Calculer la distance
                    var distance = CalculateDistance(
                        user.LastLatitude.Value, 
                        user.LastLongitude.Value,
                        latitude, 
                        longitude);

                    // Utiliser le rayon préféré de l'utilisateur ou le rayon par défaut
                    var userRadius = user.NotificationRadius > 0 ? user.NotificationRadius : radiusKm;

                    if (distance <= userRadius)
                    {
                        nearbyUsers.Add(user);
                    }
                }

                return nearbyUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs à proximité");
                return new List<UserProfile>();
            }
        }

        /// <summary>
        /// Envoie une notification push à un utilisateur spécifique
        /// </summary>
        private async Task SendProximityNotificationToUserAsync(UserProfile user, Annonce annonce, double distanceKm)
        {
            try
            {
                var title = "📍 Nouvelle annonce près de chez vous !";
                var distanceText = distanceKm < 1 
                    ? $"{(distanceKm * 1000):F0}m" 
                    : $"{distanceKm:F1}km";
                var body = $"'{annonce.Titre}' à {distanceText} de votre position";

                // Envoyer via FCM si le token existe
                if (!string.IsNullOrEmpty(user.FcmToken))
                {
                    await _pushNotificationService.SendNotificationAsync(
                        user.FcmToken,
                        title,
                        body,
                        new Dictionary<string, string>
                        {
                            { "type", "proximity_annonce" },
                            { "annonceId", annonce.Id },
                            { "distance", distanceKm.ToString("F2") }
                        });

                    _logger.LogDebug("Notification de proximité envoyée à {UserId} pour l'annonce {AnnonceId}",
                        user.Id, annonce.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification à l'utilisateur {UserId}", user.Id);
            }
        }

        /// <summary>
        /// Calcule la distance entre deux points GPS (formule de Haversine)
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Rayon de la Terre en km

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        /// <summary>
        /// Sauvegarde les statistiques de notification
        /// </summary>
        private async Task SaveNotificationStatsAsync(string annonceId, int notificationsSent)
        {
            try
            {
                var stats = new ProximityNotificationStats
                {
                    AnnonceId = annonceId,
                    NotificationsSent = notificationsSent,
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseService.SaveProximityNotificationStatsAsync(stats);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de sauvegarder les stats de notification");
            }
        }

        /// <summary>
        /// Configure les préférences de notification de proximité pour l'utilisateur
        /// </summary>
        /// <param name="enabled">Activer/désactiver</param>
        /// <param name="radiusKm">Rayon en kilomètres (5-50)</param>
        public async Task ConfigureProximityNotificationsAsync(bool enabled, double radiusKm = 50.0)
        {
            try
            {
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return;

                // Valider le rayon (entre 5 et 50 km)
                radiusKm = Math.Clamp(radiusKm, 5.0, 50.0);

                await _firebaseService.UpdateProximityPreferencesAsync(userId, enabled, radiusKm);
                
                _logger.LogInformation("Préférences de notification de proximité mises à jour: enabled={Enabled}, radius={Radius}km",
                    enabled, radiusKm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la configuration des notifications de proximité");
            }
        }

        /// <summary>
        /// Obtient le statut des notifications de proximité de l'utilisateur
        /// </summary>
        public async Task<(bool enabled, double radiusKm)> GetProximityNotificationStatusAsync()
        {
            try
            {
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return (false, DEFAULT_RADIUS_KM);

                var profile = await _firebaseService.GetUserProfileAsync(userId);
                if (profile == null)
                    return (true, DEFAULT_RADIUS_KM);

                return (profile.ProximityNotificationsEnabled, profile.NotificationRadius);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du statut des notifications de proximité");
                return (true, DEFAULT_RADIUS_KM);
            }
        }
    }

    /// <summary>
    /// Modèle pour les statistiques de notifications de proximité
    /// </summary>
    public class ProximityNotificationStats
    {
        public string AnnonceId { get; set; } = string.Empty;
        public int NotificationsSent { get; set; }
        public DateTime Timestamp { get; set; }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DonTroc.Models;
using Microsoft.Maui.Devices.Sensors;


namespace DonTroc.Services
{
    /// <summary>
    /// Service pour les notifications intelligentes et la gamification
    /// </summary>
    public class SmartNotificationService
    {
        private readonly ILogger<SmartNotificationService> _logger;
        private readonly NotificationService _notificationService;
        private readonly GeolocationService _geolocationService;
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;

        public SmartNotificationService(
            ILogger<SmartNotificationService> logger,
            NotificationService notificationService,
            GeolocationService geolocationService,
            FirebaseService firebaseService,
            AuthService authService)
        {
            _logger = logger;
            _notificationService = notificationService;
            _geolocationService = geolocationService;
            _firebaseService = firebaseService;
            _authService = authService;
        }

        #region Notifications Intelligentes

        /// <summary>
        /// Envoie un rappel pour récupérer un objet
        /// </summary>
        public async Task SendPickupReminderAsync(Transaction transaction)
        {
            try
            {
                var title = "⏰ Rappel de récupération";
                var message = $"N'oubliez pas de récupérer votre objet pour la transaction {transaction.Id}";
                
                await ScheduleNotificationAsync(title, message, "pickup_reminder", transaction.Id);
                _logger.LogInformation($"Rappel de récupération envoyé pour la transaction {transaction.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du rappel de récupération");
            }
        }

        /// <summary>
        /// Suggestions basées sur l'historique de l'utilisateur
        /// </summary>
        public async Task SendPersonalizedSuggestionsAsync(string userId)
        {
            try
            {
                var userHistory = await GetUserHistoryAsync(userId);
                var suggestions = await GenerateSuggestionsAsync(userHistory);

                foreach (var suggestion in suggestions)
                {
                    var title = "💡 Suggestion personnalisée";
                    var message = $"Nouvelle annonce qui pourrait vous intéresser : {suggestion.Titre}";
                    
                    await ScheduleNotificationAsync(title, message, "suggestion", suggestion.Id);
                }

                _logger.LogInformation($"Suggestions personnalisées envoyées pour l'utilisateur {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi des suggestions personnalisées");
            }
        }

        /// <summary>
        /// Notifications de proximité pour nouvelles annonces
        /// </summary>
        public async Task SendProximityNotificationAsync(Annonce newAnnonce)
        {
            try
            {
                var currentLocation = await _geolocationService.GetCurrentLocationAsync();
                if (currentLocation == null) return;

                var annonceLocation = new Location(newAnnonce.Latitude ?? 0, newAnnonce.Longitude ?? 0);
                var distance = CalculateDistance(currentLocation, annonceLocation);
                
                if (distance <= 5) // Dans un rayon de 5km
                {
                    var title = "📍 Nouvelle annonce près de chez vous";
                    var message = $"{newAnnonce.Titre} à {distance:F1}km de votre position";
                    
                    await ScheduleNotificationAsync(title, message, "proximity", newAnnonce.Id);
                    _logger.LogInformation($"Notification de proximité envoyée pour l'annonce {newAnnonce.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification de proximité");
            }
        }

        /// <summary>
        /// Notification de fin d'enchère ou d'offre limitée
        /// </summary>
        public async Task SendUrgencyNotificationAsync(Annonce annonce)
        {
            try
            {
                var title = "⚡ Offre limitée";
                var message = $"Dernière chance pour '{annonce.Titre}' - Plus que quelques heures !";
                
                await ScheduleNotificationAsync(title, message, "urgency", annonce.Id);
                _logger.LogInformation($"Notification d'urgence envoyée pour l'annonce {annonce.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification d'urgence");
            }
        }

        #endregion

        #region Gamification

        /// <summary>
        /// Notification d'achievement/succès débloqué
        /// </summary>
        public async Task SendAchievementNotificationAsync(string userId, Achievement achievement)
        {
            try
            {
                var title = "🏆 Succès débloqué !";
                var message = $"Félicitations ! Vous avez débloqué : {achievement.Title}";
                
                await ScheduleNotificationAsync(title, message, "achievement", achievement.Id);
                
                // Ajouter des points de récompense
                await AddRewardPointsAsync(userId, achievement.Points);
                
                _logger.LogInformation($"Notification d'achievement envoyée pour l'utilisateur {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification d'achievement");
            }
        }

        /// <summary>
        /// Notification de niveau atteint
        /// </summary>
        public async Task SendLevelUpNotificationAsync(string userId, int newLevel)
        {
            try
            {
                var title = "🎉 Niveau supérieur !";
                var message = $"Bravo ! Vous êtes maintenant niveau {newLevel} sur DonTroc !";
                
                await ScheduleNotificationAsync(title, message, "level_up", userId);
                _logger.LogInformation($"Notification de niveau envoyée pour l'utilisateur {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification de niveau");
            }
        }

        /// <summary>
        /// Notification de défi quotidien/hebdomadaire
        /// </summary>
        public async Task SendChallengeNotificationAsync(string userId, NotificationChallenge challenge)
        {
            try
            {
                var title = "🎯 Nouveau défi !";
                var message = $"Nouveau défi disponible : {challenge.Description}";
                
                await ScheduleNotificationAsync(title, message, "challenge", challenge.Id);
                _logger.LogInformation($"Notification de défi envoyée pour l'utilisateur {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification de défi");
            }
        }

        #endregion

        #region Méthodes privées

        /// <summary>
        /// Route la notification vers la méthode typée appropriée du NotificationService.
        /// Chaque type utilise son propre canal de notification Android pour un tri clair dans les paramètres.
        /// </summary>
        private async Task ScheduleNotificationAsync(string title, string message, string type, string itemId)
        {
            switch (type)
            {
                // Gamification : achievements, level_up, challenges → canal dédié haute priorité
                case "achievement":
                case "level_up":
                case "challenge":
                    await _notificationService.ShowGamificationNotificationAsync(title, message, type, itemId);
                    break;

                // Proximité : notifications géolocalisées → canal dédié
                case "proximity":
                    await _notificationService.ShowProximityNotificationAsync(title, message, itemId);
                    break;

                // Suggestions et rappels généraux → canal messages par défaut
                case "suggestion":
                case "pickup_reminder":
                case "urgency":
                default:
                    await _notificationService.ShowMessageNotificationAsync(title, message, $"{type}_{itemId}");
                    break;
            }
        }

        private async Task<List<UserAction>> GetUserHistoryAsync(string userId)
        {
            // Récupérer l'historique de l'utilisateur depuis Firebase
            return await _firebaseService.GetUserActionsAsync(userId, 50);
        }

        private async Task<List<Annonce>> GenerateSuggestionsAsync(List<UserAction> history)
        {
            var suggestions = new List<Annonce>();
            var currentUserId = _authService.GetUserId();

            if (history == null || !history.Any() || string.IsNullOrEmpty(currentUserId))
            {
                return suggestions; // Pas d'historique, pas de suggestions
            }

            try
            {
                // 1. Analyser l'historique pour trouver les catégories préférées
                var categoryCounts = history
                    .Where(h => !string.IsNullOrEmpty(h.Category))
                    .GroupBy(h => h.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                if (!categoryCounts.Any())
                {
                    return suggestions; // Aucune catégorie dans l'historique
                }

                // Trier les catégories par fréquence et prendre les 3 premières
                var favoriteCategories = categoryCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .Take(3)
                    .ToHashSet();

                _logger.LogInformation($"Catégories préférées pour {currentUserId}: {string.Join(", ", favoriteCategories)}");

                // 2. Récupérer toutes les annonces
                var allAnnonces = await _firebaseService.GetAnnoncesAsync();
                if (allAnnonces == null || !allAnnonces.Any())
                {
                    return suggestions;
                }

                // 3. Exclure les annonces déjà vues ou créées par l'utilisateur
                var seenAnnonceIds = history
                    .Where(h => h.Metadata.ContainsKey("annonceId"))
                    .Select(h => h.Metadata["annonceId"].ToString())
                    .ToHashSet();

                var potentialSuggestions = allAnnonces
                    .Where(a => a.UtilisateurId != currentUserId && !seenAnnonceIds.Contains(a.Id))
                    .ToList();

                // 4. Filtrer par catégories préférées et prendre les plus récentes
                suggestions = potentialSuggestions
                    .Where(a => !string.IsNullOrEmpty(a.Categorie) && favoriteCategories.Contains(a.Categorie))
                    .OrderByDescending(a => a.DateCreation)
                    .Take(3) // Suggérer jusqu'à 3 annonces
                    .ToList();

                _logger.LogInformation($"Généré {suggestions.Count} suggestions pour l'utilisateur {currentUserId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des suggestions.");
            }

            return suggestions;
        }

        private double CalculateDistance(Location location1, Location location2)
        {
            return Location.CalculateDistance(
                location1.Latitude, location1.Longitude,
                location2.Latitude, location2.Longitude,
                DistanceUnits.Kilometers);
        }

        private async Task AddRewardPointsAsync(string userId, int points)
        {
            try
            {
                // Ajouter des points à l'utilisateur dans Firebase
                await _firebaseService.UpdateUserPointsAsync(userId, points);
                _logger.LogInformation($"Points ajoutés : {points} pour l'utilisateur {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout des points de récompense");
            }
        }

        #endregion
    }

    #region Modèles pour la gamification

    public class Achievement
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = "";
        public int Points { get; set; }
        public string IconUrl { get; set; } = "";
        public DateTime UnlockedAt { get; set; }
        public bool IsUnlocked { get; set; }
    }

    public class NotificationChallenge
    {
        public required string Id { get; set; }
        public string Title { get; set; } = "";
        public required string Description { get; set; }
        public int TargetValue { get; set; }
        public int CurrentProgress { get; set; }
        public int RewardPoints { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class UserAction
    {
        public string Id { get; set; } = "";
        public required string UserId { get; set; }
        public required string ActionType { get; set; } // "view", "favorite", "search", etc.
        public string Category { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion
}

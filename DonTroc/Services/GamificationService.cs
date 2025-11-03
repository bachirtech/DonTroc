using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DonTroc.Models;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de gamification pour engager les utilisateurs
    /// </summary>
    public class GamificationService
    {
        private readonly ILogger<GamificationService> _logger;
        private readonly FirebaseService _firebaseService;
        private readonly SmartNotificationService _notificationService;
        private readonly AnimationService _animationService;

        // Définition des niveaux et points requis
        private readonly Dictionary<int, int> _levelRequirements = new()
        {
            { 1, 0 }, { 2, 100 }, { 3, 250 }, { 4, 500 }, { 5, 1000 },
            { 6, 1500 }, { 7, 2500 }, { 8, 4000 }, { 9, 6000 }, { 10, 10000 }
        };

        public GamificationService(
            ILogger<GamificationService> logger,
            FirebaseService firebaseService,
            SmartNotificationService notificationService,
            AnimationService animationService)
        {
            _logger = logger;
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _animationService = animationService;
        }

        #region Gestion des points et niveaux

        /// <summary>
        /// Ajoute des points à un utilisateur et vérifie les progressions
        /// </summary>
        public async Task AddPointsAsync(string userId, int points, string reason)
        {
            try
            {
                var userStats = await GetUserStatsAsync(userId);
                var oldLevel = userStats.Level;
                
                userStats.TotalPoints += points;
                userStats.Level = CalculateLevel(userStats.TotalPoints);
                
                await SaveUserStatsAsync(userId, userStats);
                
                // Vérifier si l'utilisateur a monté de niveau
                if (userStats.Level > oldLevel)
                {
                    await _notificationService.SendLevelUpNotificationAsync(userId, userStats.Level);
                    await CheckLevelAchievements(userId, userStats.Level);
                    
                    // 🎉 NOUVELLE ANIMATION : Montée de niveau
                    _ = Task.Run(async () => await _animationService.ShowLevelUpAsync(userStats.Level, points));
                }
                
                // Vérifier les achievements liés aux points
                await CheckPointsAchievements(userId, userStats.TotalPoints);
                
                _logger.LogInformation($"Points ajoutés : {points} à {userId} pour {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout des points");
            }
        }

        /// <summary>
        /// Actions qui donnent des points - avec option de délai
        /// </summary>
        public async Task OnUserActionAsync(string userId, string actionType, object? metadata = null, int delayMs = 0)
        {
            var pointsEarned = actionType switch
            {
                "annonce_created" => 50,
                "annonce_completed" => 100,
                "first_message" => 10,
                "message_sent" => 5,
                "profile_completed" => 25,
                "photo_uploaded" => 5,
                "annonce_shared" => 15,
                "positive_rating" => 30,
                "daily_login" => 10,
                "weekly_active" => 50,
                "browse_annonces" => 2,
                "add_favorite" => 3,
                "share_annonce" => 8,
                _ => 0
            };

            if (pointsEarned > 0)
            {
                _logger.LogInformation($"🎮 Gamification: Action '{actionType}' pour utilisateur {userId} - {pointsEarned} points (délai: {delayMs}ms)");
                
                await AddPointsAsync(userId, pointsEarned, actionType);
                await UpdateStreaks(userId, actionType);
                
                // 🎉 ANIMATIONS avec délai optionnel
                _ = Task.Run(async () => 
                {
                    try
                    {
                        if (delayMs > 0)
                        {
                            _logger.LogInformation($"⏱️ Attente de {delayMs}ms avant l'animation de gamification");
                            await Task.Delay(delayMs);
                        }
                        await ShowActionAnimation(actionType, pointsEarned);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erreur lors de l'animation pour {actionType}");
                    }
                });
            }
            else
            {
                _logger.LogWarning($"Action '{actionType}' ne donne aucun point");
            }
        }

        /// <summary>
        /// Déclenche l'animation de gamification après un popup de confirmation
        /// </summary>
        public async Task OnUserActionAfterConfirmationAsync(string userId, string actionType, object? metadata = null)
        {
            // Délai de 2 secondes pour laisser le temps au popup de se fermer
            await OnUserActionAsync(userId, actionType, metadata, 2000);
        }

        /// <summary>
        /// Version immédiate de l'action utilisateur (pour compatibilité)
        /// </summary>
        public async Task OnUserActionImmediateAsync(string userId, string actionType, object? metadata = null)
        {
            await OnUserActionAsync(userId, actionType, metadata, 0);
        }

        /// <summary>
        /// Méthode de test pour déclencher manuellement une animation
        /// </summary>
        public async Task TestAnimationAsync(string actionType = "annonce_created")
        {
            try
            {
                _logger.LogInformation($"🧪 Test animation gamification: {actionType}");
                await ShowActionAnimation(actionType, 50);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur test animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Affiche l'animation appropriée selon l'action accomplie
        /// </summary>
        private async Task ShowActionAnimation(string actionType, int points)
        {
            try
            {
                switch (actionType)
                {
                    case "annonce_created":
                        await _animationService.ShowFirstAnnonceRewardAsync();
                        break;
                    
                    case "message_sent":
                    case "first_message":
                        await _animationService.ShowMessageSentRewardAsync();
                        break;
                    
                    case "annonce_completed":
                        await _animationService.ShowGamificationRewardAsync(
                            "Transaction réussie ! 🤝",
                            "Merci de contribuer à la communauté !",
                            points,
                            "🤝"
                        );
                        break;
                    
                    case "positive_rating":
                        await _animationService.ShowGamificationRewardAsync(
                            "Excellente réputation ! ⭐",
                            "Votre fiabilité est reconnue !",
                            points,
                            "⭐"
                        );
                        break;
                    
                    case "weekly_active":
                        await _animationService.ShowGamificationRewardAsync(
                            "Utilisateur actif ! 🔥",
                            "Votre engagement hebdomadaire est remarquable !",
                            points,
                            "🔥"
                        );
                        break;
                    
                    case "profile_completed":
                        await _animationService.ShowGamificationRewardAsync(
                            "Profil complété ! 👤",
                            "Un profil complet inspire confiance !",
                            points,
                            "👤"
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'animation pour {actionType}");
            }
        }

        #endregion

        #region Système d'achievements

        /// <summary>
        /// Initialise les achievements par défaut
        /// </summary>
        public List<Achievement> GetDefaultAchievements()
        {
            return new List<Achievement>
            {
                // Achievements de base
                new Achievement { Id = "first_annonce", Title = "Premier pas", Description = "Créer votre première annonce", Points = 50, IconUrl = "🎯" },
                new Achievement { Id = "first_exchange", Title = "Premier échange", Description = "Réaliser votre premier échange", Points = 100, IconUrl = "🤝" },
                new Achievement { Id = "socializer", Title = "Sociable", Description = "Envoyer 10 messages", Points = 75, IconUrl = "💬" },
                
                // Achievements de points
                new Achievement { Id = "collector_100", Title = "Collectionneur", Description = "Atteindre 100 points", Points = 25, IconUrl = "⭐" },
                new Achievement { Id = "collector_500", Title = "Expert", Description = "Atteindre 500 points", Points = 50, IconUrl = "🌟" },
                new Achievement { Id = "collector_1000", Title = "Maître", Description = "Atteindre 1000 points", Points = 100, IconUrl = "✨" },
                
                // Achievements de niveau
                new Achievement { Id = "level_5", Title = "Expérimenté", Description = "Atteindre le niveau 5", Points = 100, IconUrl = "🏅" },
                new Achievement { Id = "level_10", Title = "Légendaire", Description = "Atteindre le niveau 10", Points = 200, IconUrl = "👑" },
                
                // Achievements spéciaux
                new Achievement { Id = "early_bird", Title = "Lève-tôt", Description = "Se connecter avant 8h", Points = 20, IconUrl = "🌅" },
                new Achievement { Id = "night_owl", Title = "Noctambule", Description = "Se connecter après 22h", Points = 20, IconUrl = "🦉" },
                new Achievement { Id = "weekend_warrior", Title = "Guerrier du week-end", Description = "Actif le week-end", Points = 30, IconUrl = "⚔️" },
                
                // Achievements de streak
                new Achievement { Id = "streak_7", Title = "Assidu", Description = "7 jours consécutifs de connexion", Points = 75, IconUrl = "🔥" },
                new Achievement { Id = "streak_30", Title = "Dévoué", Description = "30 jours consécutifs", Points = 200, IconUrl = "💎" },
                
                // Achievements de catégories
                new Achievement { Id = "tech_lover", Title = "Passionné de tech", Description = "10 annonces dans 'Électronique'", Points = 50, IconUrl = "📱" },
                new Achievement { Id = "book_worm", Title = "Rat de bibliothèque", Description = "10 annonces dans 'Livres'", Points = 50, IconUrl = "📚" },
                new Achievement { Id = "fashionista", Title = "Mode et style", Description = "10 annonces dans 'Vêtements'", Points = 50, IconUrl = "👗" }
            };
        }

        /// <summary>
        /// Vérifie et débloque les achievements
        /// </summary>
        public async Task CheckAchievementsAsync(string userId, string actionType, object? metadata = null)
        {
            try
            {
                var userStats = await GetUserStatsAsync(userId);
                var achievements = GetDefaultAchievements();
                
                foreach (var achievement in achievements.Where(a => !userStats.UnlockedAchievements.Contains(a.Id)))
                {
                    if (metadata != null && await IsAchievementUnlocked(userId, achievement, userStats, actionType, metadata))
                    {
                        await UnlockAchievementAsync(userId, achievement);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des achievements");
            }
        }

        #endregion

        #region Défis et streaks

        /// <summary>
        /// Crée des défis quotidiens personnalisés
        /// </summary>
        public async Task<List<Challenge>> GenerateDailyChallengesAsync(string userId)
        {
            var challenges = new List<Challenge>();
            var userStats = await GetUserStatsAsync(userId);
            
            // Défi basé sur l'activité récente
            challenges.Add(new Challenge
            {
                Id = $"daily_{DateTime.Now:yyyyMMdd}_messages",
                Title = "Communicateur du jour",
                Description = "Envoyer 3 messages aujourd'hui",
                TargetValue = 3,
                RewardPoints = 30,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            });
            
            challenges.Add(new Challenge
            {
                Id = $"daily_{DateTime.Now:yyyyMMdd}_browse",
                Title = "Explorateur",
                Description = "Consulter 10 annonces aujourd'hui",
                TargetValue = 10,
                RewardPoints = 20,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            });
            
            // Défi adapté au niveau de l'utilisateur
            if (userStats.Level >= 3)
            {
                challenges.Add(new Challenge
                {
                    Id = $"daily_{DateTime.Now:yyyyMMdd}_create",
                    Title = "Créateur actif",
                    Description = "Créer une nouvelle annonce",
                    TargetValue = 1,
                    RewardPoints = 50,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(1)
                });
            }
            
            return challenges;
        }

        /// <summary>
        /// Met à jour les streaks de connexion
        /// </summary>
        private async Task UpdateStreaks(string userId, string actionType)
        {
            if (actionType == "daily_login")
            {
                var userStats = await GetUserStatsAsync(userId);
                var today = DateTime.Today;
                
                if (userStats.LastLoginDate.Date == today.AddDays(-1))
                {
                    // Continuation du streak
                    userStats.CurrentStreak++;
                    userStats.BestStreak = Math.Max(userStats.BestStreak, userStats.CurrentStreak);
                }
                else if (userStats.LastLoginDate.Date != today)
                {
                    // Nouveau streak ou reset
                    userStats.CurrentStreak = 1;
                }
                
                userStats.LastLoginDate = today;
                await SaveUserStatsAsync(userId, userStats);
                
                // Vérifier les achievements de streak
                await CheckStreakAchievements(userId, userStats.CurrentStreak);
            }
        }

        #endregion

        #region Méthodes privées

        private int CalculateLevel(int totalPoints)
        {
            return _levelRequirements
                .Where(kvp => totalPoints >= kvp.Value)
                .Max(kvp => kvp.Key);
        }

        private async Task<UserStats> GetUserStatsAsync(string userId)
        {
            // Récupérer les stats depuis Firebase ou créer nouvelles stats
            var stats = await _firebaseService.GetUserStatsAsync(userId);
            return stats ?? new UserStats { UserId = userId };
        }

        private async Task SaveUserStatsAsync(string userId, UserStats stats)
        {
            await _firebaseService.SaveUserStatsAsync(userId, stats);
        }

        private Task<bool> IsAchievementUnlocked(string userId, Achievement achievement, UserStats userStats, string actionType, object metadata)
        {
            var result = achievement.Id switch
            {
                "first_annonce" when actionType == "annonce_created" => true,
                "first_exchange" when actionType == "annonce_completed" => true,
                "collector_100" => userStats.TotalPoints >= 100,
                "collector_500" => userStats.TotalPoints >= 500,
                "collector_1000" => userStats.TotalPoints >= 1000,
                "level_5" => userStats.Level >= 5,
                "level_10" => userStats.Level >= 10,
                "streak_7" => userStats.CurrentStreak >= 7,
                "streak_30" => userStats.CurrentStreak >= 30,
                _ => false
            };
            
            return Task.FromResult(result);
        }

        private async Task UnlockAchievementAsync(string userId, Achievement achievement) // Méthode asynchrone
        {
            var userStats = await GetUserStatsAsync(userId);
            userStats.UnlockedAchievements.Add(achievement.Id);
            userStats.TotalPoints += achievement.Points;
            
            await SaveUserStatsAsync(userId, userStats);
            await _notificationService.SendAchievementNotificationAsync(userId, achievement);
            
            _logger.LogInformation($"Achievement débloqué : {achievement.Title} pour {userId}");
        }

        private async Task CheckLevelAchievements(string userId, int level)
        {
            if (level == 5)
                await CheckAchievementsAsync(userId, "level_achieved", new { level = 5 });
            if (level == 10)
                await CheckAchievementsAsync(userId, "level_achieved", new { level = 10 });
        }

        private async Task CheckPointsAchievements(string userId, int totalPoints)
        {
            if (totalPoints >= 100)
                await CheckAchievementsAsync(userId, "points_milestone", new { points = 100 });
            if (totalPoints >= 500)
                await CheckAchievementsAsync(userId, "points_milestone", new { points = 500 });
            if (totalPoints >= 1000)
                await CheckAchievementsAsync(userId, "points_milestone", new { points = 1000 });
        }

        private async Task CheckStreakAchievements(string userId, int streak)
        {
            if (streak == 7)
                await CheckAchievementsAsync(userId, "streak_milestone", new { streak = 7 });
            if (streak == 30)
                await CheckAchievementsAsync(userId, "streak_milestone", new { streak = 30 });
        }

        #endregion
    }

    #region Modèles pour les statistiques utilisateur

    public class UserStats
    {
        public required string UserId { get; set; }
        public int TotalPoints { get; set; }
        public int Level { get; set; } = 1;
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public DateTime LastLoginDate { get; set; }
        public List<string> UnlockedAchievements { get; set; } = new();
        public List<string> CompletedChallenges { get; set; } = new();
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    #endregion
}

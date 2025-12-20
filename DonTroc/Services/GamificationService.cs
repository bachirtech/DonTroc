using DonTroc.Configuration;
using DonTroc.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DonTroc.Services;

// Alias pour éviter les ambiguïtés
using Challenge = DonTroc.Models.Challenge;
using Badge = DonTroc.Models.Badge;
using DailyReward = DonTroc.Models.DailyReward;
using WheelReward = DonTroc.Models.WheelReward;

/// <summary>
/// Interface pour le service de gamification
/// </summary>
public interface IGamificationService
{
    // Profil
    Task<UserGamificationProfile> GetUserProfileAsync(string userId);
    Task<UserGamificationProfile> CreateProfileAsync(string userId);
    
    // XP et Niveaux
    Task<RewardNotification?> AddXpAsync(string userId, string actionType, int? customAmount = null);
    Task<int> GetUserLevelAsync(string userId);
    Task<string> GetUserTitleAsync(string userId);
    
    // Badges
    Task<List<Badge>> GetUserBadgesAsync(string userId);
    Task<List<Badge>> GetAvailableBadgesAsync(string userId);
    Task<Badge?> CheckAndUnlockBadgeAsync(string userId, string statKey, int newValue);
    
    // Défis
    Task<List<Challenge>> GetActiveChallengesAsync(string userId);
    Task<Challenge?> UpdateChallengeProgressAsync(string userId, string actionType, int increment = 1);
    Task GenerateDailyChallengesAsync(string userId);
    Task GenerateWeeklyChallengesAsync(string userId);
    
    // Récompenses quotidiennes
    Task<List<DailyReward>> GetDailyRewardsStatusAsync(string userId);
    Task<RewardNotification?> ClaimDailyRewardAsync(string userId);
    Task<bool> CanClaimDailyRewardAsync(string userId);
    
    // Roue de la fortune
    Task<bool> CanSpinWheelAsync(string userId);
    Task<WheelReward> SpinWheelAsync(string userId);
    
    // Stats
    Task IncrementStatAsync(string userId, string statKey, int amount = 1);
    Task<int> GetStatAsync(string userId, string statKey);
    
    // Streak
    Task<int> GetCurrentStreakAsync(string userId);
    Task UpdateDailyStreakAsync(string userId);
    
    // Compatibilité ancien code
    Task OnUserActionAsync(string userId, string actionType);
    Task OnUserActionAfterConfirmationAsync(string userId, string actionType);
    Task AddPointsAsync(string userId, int points, string reason);
    Task ShowRewardAnimationAsync(string title, string description, int points, string icon = "🏆");
}

/// <summary>
/// Service gérant toute la logique de gamification avec stockage local
/// </summary>
public class GamificationService : IGamificationService
{
    private readonly ILogger<GamificationService> _logger;
    private readonly Random _random = new();
    
    // Préfixe pour les clés de stockage
    private const string ProfileKeyPrefix = "gamification_profile_";
    private const string ChallengesKeyPrefix = "gamification_challenges_";
    
    // Cache local pour éviter trop de lectures
    private readonly Dictionary<string, UserGamificationProfile> _profileCache = new();
    private readonly Dictionary<string, List<Challenge>> _challengesCache = new();

    public GamificationService(ILogger<GamificationService> logger)
    {
        _logger = logger;
    }

    #region Stockage Local

    private async Task<T?> GetFromStorageAsync<T>(string key) where T : class
    {
        try
        {
            var json = Preferences.Get(key, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture du stockage pour {Key}", key);
            return null;
        }
    }

    private async Task SaveToStorageAsync<T>(string key, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            Preferences.Set(key, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'écriture du stockage pour {Key}", key);
        }
    }

    #endregion

    #region Profil

    public async Task<UserGamificationProfile> GetUserProfileAsync(string userId)
    {
        try
        {
            if (_profileCache.TryGetValue(userId, out var cachedProfile))
                return cachedProfile;

            var profile = await GetFromStorageAsync<UserGamificationProfile>($"{ProfileKeyPrefix}{userId}");

            if (profile == null)
            {
                profile = await CreateProfileAsync(userId);
            }

            _profileCache[userId] = profile;
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du profil gamification pour {UserId}", userId);
            return new UserGamificationProfile { UserId = userId };
        }
    }

    public async Task<UserGamificationProfile> CreateProfileAsync(string userId)
    {
        var profile = new UserGamificationProfile
        {
            UserId = userId,
            TotalXp = 0,
            CurrentLevel = 1,
            BoostCredits = 1, // Bonus de départ
            DailyStreak = 0,
            UnlockedBadges = new List<string>(),
            CompletedChallenges = new List<string>(),
            Stats = new Dictionary<string, int>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await SaveProfileAsync(profile);
        _profileCache[userId] = profile;
        
        _logger.LogInformation("Profil gamification créé pour {UserId}", userId);
        return profile;
    }

    private async Task SaveProfileAsync(UserGamificationProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        await SaveToStorageAsync($"{ProfileKeyPrefix}{profile.UserId}", profile);
        _profileCache[profile.UserId] = profile;
    }

    #endregion

    #region XP et Niveaux

    public async Task<RewardNotification?> AddXpAsync(string userId, string actionType, int? customAmount = null)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId);
            
            int xpToAdd = customAmount ?? GamificationConfig.XpRewards.GetValueOrDefault(actionType, 0);
            
            if (xpToAdd <= 0) return null;

            int oldLevel = profile.CurrentLevel;
            profile.TotalXp += xpToAdd;
            profile.CurrentLevel = profile.CalculateLevel();
            
            await SaveProfileAsync(profile);

            // Créer la notification
            var notification = new RewardNotification
            {
                Title = "XP Gagnés !",
                Message = $"+{xpToAdd} XP pour {GetActionDescription(actionType)}",
                Icon = "✨",
                XpGained = xpToAdd
            };

            // Vérifier si level up
            if (profile.CurrentLevel > oldLevel)
            {
                notification.LevelUp = true;
                notification.NewLevel = profile.CurrentLevel;
                notification.Title = "Niveau Supérieur ! 🎉";
                notification.Message = $"Félicitations ! Vous êtes maintenant niveau {profile.CurrentLevel} !";
                
                _logger.LogInformation("Utilisateur {UserId} passe au niveau {Level}", userId, profile.CurrentLevel);
            }

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ajout de XP pour {UserId}", userId);
            return null;
        }
    }

    public async Task<int> GetUserLevelAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        return profile.CurrentLevel;
    }

    public async Task<string> GetUserTitleAsync(string userId)
    {
        var level = await GetUserLevelAsync(userId);
        return GamificationConfig.GetLevelTitle(level);
    }

    private string GetActionDescription(string actionType)
    {
        return actionType switch
        {
            "create_annonce" => "création d'annonce",
            "complete_transaction_donor" => "don réalisé",
            "complete_transaction_receiver" => "objet reçu",
            "send_message" => "message envoyé",
            "daily_login" => "connexion quotidienne",
            "share_app" => "partage de l'app",
            "give_rating" => "évaluation donnée",
            "browse_annonces" => "exploration",
            "add_favorite" => "ajout aux favoris",
            "share_annonce" => "partage d'annonce",
            _ => actionType.Replace("_", " ")
        };
    }

    #endregion

    #region Badges

    public async Task<List<Badge>> GetUserBadgesAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        return GamificationConfig.AllBadges
            .Where(b => profile.UnlockedBadges.Contains(b.Id))
            .ToList();
    }

    public async Task<List<Badge>> GetAvailableBadgesAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        return GamificationConfig.AllBadges
            .Where(b => !profile.UnlockedBadges.Contains(b.Id) && !b.IsSecret)
            .ToList();
    }

    public async Task<Badge?> CheckAndUnlockBadgeAsync(string userId, string statKey, int newValue)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId);
            
            // Chercher un badge débloquable
            var badge = GamificationConfig.AllBadges
                .FirstOrDefault(b => 
                    b.StatKey == statKey && 
                    b.RequiredValue <= newValue && 
                    !profile.UnlockedBadges.Contains(b.Id));

            if (badge != null)
            {
                profile.UnlockedBadges.Add(badge.Id);
                profile.TotalXp += badge.XpReward;
                profile.CurrentLevel = profile.CalculateLevel();
                
                await SaveProfileAsync(profile);
                
                _logger.LogInformation("Badge {BadgeId} débloqué pour {UserId}", badge.Id, userId);
                return badge;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification des badges pour {UserId}", userId);
            return null;
        }
    }

    #endregion

    #region Défis

    public async Task<List<Challenge>> GetActiveChallengesAsync(string userId)
    {
        try
        {
            if (_challengesCache.TryGetValue(userId, out var cached))
            {
                var activeCached = cached.Where(c => c.ExpiresAt > DateTime.UtcNow && !c.IsCompleted).ToList();
                if (activeCached.Any()) return activeCached;
            }

            var challenges = await GetFromStorageAsync<List<Challenge>>($"{ChallengesKeyPrefix}{userId}") 
                             ?? new List<Challenge>();
            
            var activeChallenges = challenges
                .Where(c => c.ExpiresAt > DateTime.UtcNow && !c.IsCompleted)
                .ToList();

            // Si pas de défis quotidiens, en générer
            if (!activeChallenges.Any(c => c.Type == ChallengeType.Daily))
            {
                await GenerateDailyChallengesAsync(userId);
                challenges = await GetFromStorageAsync<List<Challenge>>($"{ChallengesKeyPrefix}{userId}") 
                             ?? new List<Challenge>();
                activeChallenges = challenges
                    .Where(c => c.ExpiresAt > DateTime.UtcNow && !c.IsCompleted)
                    .ToList();
            }

            _challengesCache[userId] = challenges;
            return activeChallenges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des défis pour {UserId}", userId);
            return new List<Challenge>();
        }
    }

    public async Task<Challenge?> UpdateChallengeProgressAsync(string userId, string actionType, int increment = 1)
    {
        try
        {
            var challenges = await GetActiveChallengesAsync(userId);
            var matchingChallenge = challenges.FirstOrDefault(c => c.ActionType == actionType && !c.IsCompleted);

            if (matchingChallenge != null)
            {
                matchingChallenge.CurrentProgress += increment;
                
                // Sauvegarder les défis mis à jour
                var allChallenges = await GetFromStorageAsync<List<Challenge>>($"{ChallengesKeyPrefix}{userId}") 
                                    ?? new List<Challenge>();
                var index = allChallenges.FindIndex(c => c.Id == matchingChallenge.Id);
                if (index >= 0) allChallenges[index] = matchingChallenge;
                else allChallenges.Add(matchingChallenge);
                
                await SaveToStorageAsync($"{ChallengesKeyPrefix}{userId}", allChallenges);
                _challengesCache[userId] = allChallenges;

                // Si le défi est complété, donner les récompenses
                if (matchingChallenge.IsCompleted)
                {
                    await AddXpAsync(userId, 
                        matchingChallenge.Type == ChallengeType.Daily 
                            ? "complete_daily_challenge" 
                            : "complete_weekly_challenge",
                        matchingChallenge.XpReward);

                    if (matchingChallenge.BoostCreditsReward > 0)
                    {
                        var profile = await GetUserProfileAsync(userId);
                        profile.BoostCredits += matchingChallenge.BoostCreditsReward;
                        await SaveProfileAsync(profile);
                    }

                    _logger.LogInformation("Défi {ChallengeId} complété par {UserId}", matchingChallenge.Id, userId);
                }

                return matchingChallenge;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du défi pour {UserId}", userId);
            return null;
        }
    }

    public async Task GenerateDailyChallengesAsync(string userId)
    {
        try
        {
            var allChallenges = await GetFromStorageAsync<List<Challenge>>($"{ChallengesKeyPrefix}{userId}") 
                                ?? new List<Challenge>();
            
            // Supprimer les anciens défis quotidiens expirés
            allChallenges.RemoveAll(c => c.Type == ChallengeType.Daily && c.ExpiresAt < DateTime.UtcNow);

            // Sélectionner 3 défis aléatoires
            var newChallenges = GamificationConfig.DailyChallengeTemplates
                .OrderBy(_ => _random.Next())
                .Take(3)
                .Select(template => new Challenge
                {
                    Id = $"daily_{template.Id}_{DateTime.UtcNow:yyyyMMdd}",
                    Title = template.Title,
                    Description = template.Description,
                    Icon = template.Icon,
                    Type = ChallengeType.Daily,
                    Difficulty = template.Difficulty,
                    ActionType = template.ActionType,
                    RequiredCount = template.RequiredCount,
                    CurrentProgress = 0,
                    XpReward = template.XpReward,
                    BoostCreditsReward = template.BoostCreditsReward,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(1).AddHours(23).AddMinutes(59)
                })
                .ToList();

            allChallenges.AddRange(newChallenges);
            await SaveToStorageAsync($"{ChallengesKeyPrefix}{userId}", allChallenges);
            _challengesCache[userId] = allChallenges;

            _logger.LogInformation("Défis quotidiens générés pour {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération des défis quotidiens pour {UserId}", userId);
        }
    }

    public async Task GenerateWeeklyChallengesAsync(string userId)
    {
        try
        {
            var allChallenges = await GetFromStorageAsync<List<Challenge>>($"{ChallengesKeyPrefix}{userId}") 
                                ?? new List<Challenge>();
            
            // Supprimer les anciens défis hebdomadaires expirés
            allChallenges.RemoveAll(c => c.Type == ChallengeType.Weekly && c.ExpiresAt < DateTime.UtcNow);

            // Sélectionner 2 défis hebdomadaires
            var newChallenges = GamificationConfig.WeeklyChallengeTemplates
                .OrderBy(_ => _random.Next())
                .Take(2)
                .Select(template => new Challenge
                {
                    Id = $"weekly_{template.Id}_{DateTime.UtcNow:yyyyMMdd}",
                    Title = template.Title,
                    Description = template.Description,
                    Icon = template.Icon,
                    Type = ChallengeType.Weekly,
                    Difficulty = template.Difficulty,
                    ActionType = template.ActionType,
                    RequiredCount = template.RequiredCount,
                    CurrentProgress = 0,
                    XpReward = template.XpReward,
                    BoostCreditsReward = template.BoostCreditsReward,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek).AddHours(23).AddMinutes(59)
                })
                .ToList();

            allChallenges.AddRange(newChallenges);
            await SaveToStorageAsync($"{ChallengesKeyPrefix}{userId}", allChallenges);
            _challengesCache[userId] = allChallenges;

            _logger.LogInformation("Défis hebdomadaires générés pour {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération des défis hebdomadaires pour {UserId}", userId);
        }
    }

    #endregion

    #region Récompenses Quotidiennes

    public async Task<List<DailyReward>> GetDailyRewardsStatusAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        var currentDay = Math.Max(1, Math.Min((profile.DailyStreak % 7) + 1, 7));
        var canClaim = await CanClaimDailyRewardAsync(userId);

        return GamificationConfig.DailyRewards
            .Select(r => new DailyReward
            {
                Day = r.Day,
                Icon = r.Icon,
                Type = r.Type,
                Value = r.Value,
                IsClaimed = r.Day < currentDay || (r.Day == currentDay && !canClaim),
                IsToday = r.Day == currentDay && canClaim,
                IsLocked = r.Day > currentDay
            })
            .ToList();
    }

    public async Task<bool> CanClaimDailyRewardAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        return profile.LastDailyRewardClaimed.Date < DateTime.UtcNow.Date;
    }

    public async Task<RewardNotification?> ClaimDailyRewardAsync(string userId)
    {
        try
        {
            if (!await CanClaimDailyRewardAsync(userId))
                return null;

            var profile = await GetUserProfileAsync(userId);
            
            // Mettre à jour le streak
            await UpdateDailyStreakAsync(userId);
            profile = await GetUserProfileAsync(userId); // Recharger après mise à jour
            
            // Calculer le jour du streak (1-7, puis recommence)
            var rewardDay = Math.Max(1, Math.Min((profile.DailyStreak % 7), 7));
            if (rewardDay == 0) rewardDay = 7;
            
            var reward = GamificationConfig.DailyRewards.FirstOrDefault(r => r.Day == rewardDay) 
                         ?? GamificationConfig.DailyRewards.First();

            // Appliquer la récompense
            var notification = new RewardNotification
            {
                Title = $"Récompense Jour {rewardDay} ! 🎁",
                Icon = reward.Icon
            };

            switch (reward.Type)
            {
                case WheelRewardType.Xp:
                    profile.TotalXp += reward.Value;
                    notification.XpGained = reward.Value;
                    notification.Message = $"Vous avez gagné {reward.Value} XP !";
                    break;
                case WheelRewardType.BoostCredits:
                    profile.BoostCredits += reward.Value;
                    notification.BoostCreditsGained = reward.Value;
                    notification.Message = $"Vous avez gagné {reward.Value} crédit(s) boost !";
                    break;
            }

            profile.LastDailyRewardClaimed = DateTime.UtcNow;
            profile.CurrentLevel = profile.CalculateLevel();
            
            await SaveProfileAsync(profile);
            
            _logger.LogInformation("Récompense quotidienne réclamée par {UserId}, jour {Day}", userId, rewardDay);
            
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réclamation de la récompense quotidienne pour {UserId}", userId);
            return null;
        }
    }

    #endregion

    #region Roue de la Fortune

    public async Task<bool> CanSpinWheelAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        return profile.LastWheelSpin.Date < DateTime.UtcNow.Date;
    }

    public async Task<WheelReward> SpinWheelAsync(string userId)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId);
            
            // Sélectionner une récompense basée sur les probabilités
            var roll = _random.NextDouble();
            var cumulativeProbability = 0.0;
            WheelReward? selectedReward = null;

            foreach (var reward in GamificationConfig.WheelRewards)
            {
                cumulativeProbability += reward.Probability;
                if (roll <= cumulativeProbability)
                {
                    selectedReward = reward;
                    break;
                }
            }

            selectedReward ??= GamificationConfig.WheelRewards.Last();

            // Appliquer la récompense
            switch (selectedReward.Type)
            {
                case WheelRewardType.Xp:
                    profile.TotalXp += selectedReward.Value;
                    break;
                case WheelRewardType.BoostCredits:
                    profile.BoostCredits += selectedReward.Value;
                    break;
                case WheelRewardType.DoubleXpHours:
                    // TODO: Implémenter le double XP temporaire
                    break;
            }

            profile.LastWheelSpin = DateTime.UtcNow;
            profile.CurrentLevel = profile.CalculateLevel();
            
            await SaveProfileAsync(profile);
            
            _logger.LogInformation("Roue tournée par {UserId}, récompense: {RewardId}", userId, selectedReward.Id);
            
            return selectedReward;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du spin de la roue pour {UserId}", userId);
            return new WheelReward { Id = "error", Name = "Erreur", Type = WheelRewardType.Nothing, Value = 0 };
        }
    }

    #endregion

    #region Stats

    public async Task IncrementStatAsync(string userId, string statKey, int amount = 1)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId);
            
            if (!profile.Stats.ContainsKey(statKey))
                profile.Stats[statKey] = 0;
            
            profile.Stats[statKey] += amount;
            
            await SaveProfileAsync(profile);
            
            // Vérifier si un badge peut être débloqué
            await CheckAndUnlockBadgeAsync(userId, statKey, profile.Stats[statKey]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'incrémentation de la stat {StatKey} pour {UserId}", statKey, userId);
        }
    }

    public async Task<int> GetStatAsync(string userId, string statKey)
    {
        var profile = await GetUserProfileAsync(userId);
        return profile.Stats.GetValueOrDefault(statKey, 0);
    }

    #endregion

    #region Streak

    public async Task<int> GetCurrentStreakAsync(string userId)
    {
        var profile = await GetUserProfileAsync(userId);
        return profile.DailyStreak;
    }

    public async Task UpdateDailyStreakAsync(string userId)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId);
            var lastClaim = profile.LastDailyRewardClaimed.Date;
            var today = DateTime.UtcNow.Date;

            if (lastClaim == today.AddDays(-1))
            {
                // Continuation du streak
                profile.DailyStreak++;
            }
            else if (lastClaim < today.AddDays(-1))
            {
                // Streak cassé
                profile.DailyStreak = 1;
            }
            else if (lastClaim == DateTime.MinValue.Date)
            {
                // Premier jour
                profile.DailyStreak = 1;
            }
            // Si lastClaim == today, ne rien faire (déjà compté)

            // Mettre à jour le max streak
            var maxStreak = profile.Stats.GetValueOrDefault("max_streak", 0);
            if (profile.DailyStreak > maxStreak)
            {
                profile.Stats["max_streak"] = profile.DailyStreak;
                await CheckAndUnlockBadgeAsync(userId, "max_streak", profile.DailyStreak);
            }

            await SaveProfileAsync(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du streak pour {UserId}", userId);
        }
    }

    #endregion

    #region Méthodes de compatibilité avec l'ancien code

    /// <summary>
    /// Méthode de compatibilité pour les actions utilisateur (ancien système)
    /// </summary>
    public async Task OnUserActionAsync(string userId, string actionType)
    {
        try
        {
            // Ajouter les XP pour l'action
            await AddXpAsync(userId, actionType);
            
            // Mettre à jour les défis
            await UpdateChallengeProgressAsync(userId, actionType);
            
            // Incrémenter les stats
            await IncrementStatAsync(userId, actionType);
            
            // Mettre à jour le streak si c'est une connexion quotidienne
            if (actionType == "daily_login")
            {
                await UpdateDailyStreakAsync(userId);
            }
            
            _logger.LogDebug("Action gamification enregistrée: {Action} pour {UserId}", actionType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de l'action {Action} pour {UserId}", actionType, userId);
        }
    }

    /// <summary>
    /// Méthode de compatibilité pour les actions après confirmation
    /// </summary>
    public async Task OnUserActionAfterConfirmationAsync(string userId, string actionType)
    {
        await OnUserActionAsync(userId, actionType);
    }

    /// <summary>
    /// Ajouter des points avec une raison personnalisée (méthode de compatibilité)
    /// </summary>
    public async Task AddPointsAsync(string userId, int points, string reason)
    {
        try
        {
            await AddXpAsync(userId, reason, points);
            _logger.LogDebug("Points ajoutés: {Points} pour {Reason} à {UserId}", points, reason, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ajout de points pour {UserId}", userId);
        }
    }

    /// <summary>
    /// Affiche une animation de récompense
    /// </summary>
    public async Task ShowRewardAnimationAsync(string title, string description, int points, string icon = "🏆")
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var animationView = new Views.GamificationAnimationView();
                await animationView.ShowAnimationAsync(title, description, points, icon);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage de l'animation de récompense");
        }
    }

    #endregion
}


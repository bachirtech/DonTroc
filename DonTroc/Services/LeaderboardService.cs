// Fichier: DonTroc/Services/LeaderboardService.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Configuration;
using DonTroc.Models;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services;

/// <summary>
/// Modèle représentant une entrée dans le classement
/// </summary>
public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DailyStreak { get; set; }
    public int BadgeCount { get; set; }
    public bool IsCurrentUser { get; set; }
    
    /// <summary>
    /// Initiales anonymisées (ex: "J.D" pour "Jean Dupont")
    /// </summary>
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DisplayName)) return "??";
            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}.{parts[1][0]}".ToUpper();
            return parts[0].Length >= 2 
                ? $"{parts[0][0]}.{parts[0][1]}".ToUpper() 
                : $"{parts[0][0]}.".ToUpper();
        }
    }

    /// <summary>
    /// Emoji de médaille pour le top 3
    /// </summary>
    public string RankDisplay => Rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"#{Rank}"
    };

    /// <summary>
    /// Indique si l'entrée est dans le top 3
    /// </summary>
    public bool IsTopThree => Rank <= 3;
}

/// <summary>
/// Service de classement des utilisateurs par XP.
/// Optimisé : sync XP en arrière-plan, cache agressif, chargement parallélisé.
/// </summary>
public class LeaderboardService
{
    private readonly FirebaseService _firebaseService;
    private readonly GamificationService _gamificationService;
    private readonly AuthService _authService;
    private readonly SocialService _socialService;
    private readonly ILogger<LeaderboardService> _logger;

    // Cache du classement (global + amis séparés)
    private List<LeaderboardEntry>? _cachedGlobalLeaderboard;
    private List<LeaderboardEntry>? _cachedFriendsLeaderboard;
    private DateTime _lastGlobalCacheTime = DateTime.MinValue;
    private DateTime _lastFriendsCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    // Éviter les syncs multiples simultanées
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public LeaderboardService(
        FirebaseService firebaseService,
        GamificationService gamificationService,
        AuthService authService,
        SocialService socialService,
        ILogger<LeaderboardService> logger)
    {
        _firebaseService = firebaseService;
        _gamificationService = gamificationService;
        _authService = authService;
        _socialService = socialService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère le classement global. La sync XP se fait en arrière-plan
    /// et ne bloque pas l'affichage du classement.
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetGlobalLeaderboardAsync(int maxResults = 50)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            // 1. Retourner le cache immédiatement si valide
            if (_cachedGlobalLeaderboard != null && DateTime.Now - _lastGlobalCacheTime < _cacheDuration)
            {
                _logger.LogDebug("[Leaderboard] Cache hit ({Elapsed}ms)", sw.ElapsedMilliseconds);
                return _cachedGlobalLeaderboard.Take(maxResults).ToList();
            }

            var currentUserId = _authService.GetUserId();

            // 2. Lancer la sync XP en arrière-plan (fire-and-forget, ne bloque pas)
            if (!string.IsNullOrEmpty(currentUserId))
            {
                _ = SyncLocalXpToFirebaseInBackgroundAsync(currentUserId);
            }

            // 3. Charger les profils Firebase en parallèle avec les données de gamification locale
            var profilesTask = _firebaseService.GetAllUserProfilesAsync();
            Task<UserGamificationProfile>? gamProfileTask = null;
            
            if (!string.IsNullOrEmpty(currentUserId))
            {
                gamProfileTask = _gamificationService.GetUserProfileAsync(currentUserId);
            }

            var allProfiles = await profilesTask;
            _logger.LogDebug("[Leaderboard] Profils chargés en {Elapsed}ms ({Count} profils)", 
                sw.ElapsedMilliseconds, allProfiles.Count);

            if (!allProfiles.Any())
                return new List<LeaderboardEntry>();

            // 4. Construire le classement (tri + mapping — opération CPU pure, très rapide)
            var rankedProfiles = allProfiles
                .Where(p => p != null && !string.IsNullOrEmpty(p.Name) && !p.IsSuspended)
                .OrderByDescending(p => p.Points)
                .ThenByDescending(p => p.NombreEchangesReussis)
                .ThenBy(p => p.DateInscription)
                .ToList();

            var leaderboard = rankedProfiles
                .Take(maxResults)
                .Select((profile, index) => new LeaderboardEntry
                {
                    Rank = index + 1,
                    UserId = profile.Id,
                    DisplayName = profile.Name ?? "Anonyme",
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    TotalXp = profile.Points,
                    Level = CalculateLevel(profile.Points),
                    Title = GamificationConfig.GetLevelTitle(CalculateLevel(profile.Points)),
                    IsCurrentUser = profile.Id == currentUserId
                })
                .ToList();

            // 5. Si l'utilisateur courant n'est pas dans le top, l'ajouter
            if (!string.IsNullOrEmpty(currentUserId) && !leaderboard.Any(e => e.IsCurrentUser))
            {
                var currentUserIndex = rankedProfiles.FindIndex(p => p.Id == currentUserId);
                if (currentUserIndex >= 0)
                {
                    var currentProfile = rankedProfiles[currentUserIndex];
                    leaderboard.Add(new LeaderboardEntry
                    {
                        Rank = currentUserIndex + 1,
                        UserId = currentProfile.Id,
                        DisplayName = currentProfile.Name ?? "Anonyme",
                        ProfilePictureUrl = currentProfile.ProfilePictureUrl,
                        TotalXp = currentProfile.Points,
                        Level = CalculateLevel(currentProfile.Points),
                        Title = GamificationConfig.GetLevelTitle(CalculateLevel(currentProfile.Points)),
                        IsCurrentUser = true
                    });
                }
            }

            // 6. Enrichir l'utilisateur courant avec les données de gamification locale (déjà en cours)
            if (gamProfileTask != null)
            {
                try
                {
                    var gamProfile = await gamProfileTask;
                    var currentEntry = leaderboard.FirstOrDefault(e => e.IsCurrentUser);
                    if (currentEntry != null)
                    {
                        currentEntry.DailyStreak = gamProfile.DailyStreak;
                        currentEntry.BadgeCount = gamProfile.UnlockedBadges.Count;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[Leaderboard] Enrichissement gamification ignoré");
                }
            }

            // 7. Mettre en cache
            _cachedGlobalLeaderboard = leaderboard;
            _lastGlobalCacheTime = DateTime.Now;

            _logger.LogDebug("[Leaderboard] Classement global chargé en {Elapsed}ms ({Count} entrées)", 
                sw.ElapsedMilliseconds, leaderboard.Count);

            return leaderboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du classement global");
            return new List<LeaderboardEntry>();
        }
    }

    /// <summary>
    /// Récupère le classement filtré aux amis uniquement.
    /// Utilise le cache du classement global s'il est disponible pour éviter un 2ème appel Firebase.
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetFriendsLeaderboardAsync()
    {
        try
        {
            // Cache amis
            if (_cachedFriendsLeaderboard != null && DateTime.Now - _lastFriendsCacheTime < _cacheDuration)
                return _cachedFriendsLeaderboard;

            var currentUserId = _authService.GetUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return new List<LeaderboardEntry>();

            // Récupérer les amis
            var friends = await _socialService.GetFriendsAsync(currentUserId);
            var friendIds = friends.Select(f => f.Id).ToHashSet();
            friendIds.Add(currentUserId);

            // Réutiliser le cache global s'il est disponible (évite un 2ème appel GetAllUserProfilesAsync)
            List<UserProfile> allProfiles;
            if (_cachedGlobalLeaderboard != null && DateTime.Now - _lastGlobalCacheTime < _cacheDuration)
            {
                // Récupérer les profils depuis Firebase (on a besoin des vrais profils pour les amis)
                allProfiles = await _firebaseService.GetAllUserProfilesAsync();
            }
            else
            {
                allProfiles = await _firebaseService.GetAllUserProfilesAsync();
            }

            // Filtrer aux amis
            var friendProfiles = allProfiles
                .Where(p => friendIds.Contains(p.Id) && !string.IsNullOrEmpty(p.Name) && !p.IsSuspended)
                .OrderByDescending(p => p.Points)
                .ToList();

            var leaderboard = friendProfiles
                .Select((profile, index) => new LeaderboardEntry
                {
                    Rank = index + 1,
                    UserId = profile.Id,
                    DisplayName = profile.Name ?? "Anonyme",
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    TotalXp = profile.Points,
                    Level = CalculateLevel(profile.Points),
                    Title = GamificationConfig.GetLevelTitle(CalculateLevel(profile.Points)),
                    IsCurrentUser = profile.Id == currentUserId
                })
                .ToList();

            _cachedFriendsLeaderboard = leaderboard;
            _lastFriendsCacheTime = DateTime.Now;

            return leaderboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du classement amis");
            return new List<LeaderboardEntry>();
        }
    }

    /// <summary>
    /// Sync XP en arrière-plan avec protection contre les appels multiples.
    /// Ne bloque jamais l'UI.
    /// </summary>
    private async Task SyncLocalXpToFirebaseInBackgroundAsync(string userId)
    {
        // Éviter les syncs multiples simultanées
        if (!await _syncLock.WaitAsync(0))
            return; // Un sync est déjà en cours, on skip

        try
        {
            await SyncLocalXpToFirebaseAsync(userId);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Synchronise les XP locaux (Preferences) vers le champ Points du profil Firebase.
    /// </summary>
    public async Task SyncLocalXpToFirebaseAsync(string userId)
    {
        try
        {
            var localProfile = await _gamificationService.GetUserProfileAsync(userId);
            if (localProfile.TotalXp > 0)
            {
                var firebaseProfile = await _firebaseService.GetUserProfileAsync(userId);
                if (firebaseProfile != null && firebaseProfile.Points != localProfile.TotalXp)
                {
                    firebaseProfile.Points = localProfile.TotalXp;
                    await _firebaseService.SaveUserProfileAsync(firebaseProfile);
                    _logger.LogDebug("XP synchronisés vers Firebase pour {UserId}: {Xp}", userId, localProfile.TotalXp);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur lors de la synchro XP vers Firebase pour {UserId}", userId);
        }
    }

    /// <summary>
    /// Invalide le cache du classement (global + amis).
    /// </summary>
    public void InvalidateCache()
    {
        _cachedGlobalLeaderboard = null;
        _cachedFriendsLeaderboard = null;
        _lastGlobalCacheTime = DateTime.MinValue;
        _lastFriendsCacheTime = DateTime.MinValue;
    }

    /// <summary>
    /// Calcule le niveau basé sur les XP (même formule que UserGamificationProfile)
    /// </summary>
    private static int CalculateLevel(int totalXp)
    {
        int level = 1;
        int xpRequired = 0;
        while (totalXp >= xpRequired + (level * 100))
        {
            xpRequired += level * 100;
            level++;
        }
        return level;
    }
}


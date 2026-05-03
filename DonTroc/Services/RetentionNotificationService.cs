using System.Diagnostics;
using DonTroc.Models;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services;

/// <summary>
/// Service centralisé pour les notifications de rétention utilisateur.
/// Vérifie périodiquement 5 types de conditions et envoie des notifications ciblées
/// pour maximiser l'engagement et le temps passé dans l'app.
/// 
/// Cooldown : max 1 notification par type par jour (via Preferences).
/// </summary>
public class RetentionNotificationService
{
    private readonly GamificationService _gamificationService;
    private readonly FavoritesService _favoritesService;
    private readonly FirebaseService _firebaseService;
    private readonly NotificationService _notificationService;
    private readonly AuthService _authService;
    private readonly EventService? _eventService;
    private readonly ILogger<RetentionNotificationService> _logger;

    // Préfixe pour les clés de cooldown dans Preferences
    private const string CooldownPrefix = "retention_last_";

    // Durée minimale entre 2 notifications du même type
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromHours(20);

    // Max favoris à vérifier par cycle (pour limiter les requêtes Firebase)
    private const int MaxFavoritesToCheck = 20;

    public RetentionNotificationService(
        GamificationService gamificationService,
        FavoritesService favoritesService,
        FirebaseService firebaseService,
        AuthService authService,
        NotificationService notificationService,
        ILogger<RetentionNotificationService> logger,
        EventService? eventService = null)
    {
        _gamificationService = gamificationService;
        _favoritesService = favoritesService;
        _firebaseService = firebaseService;
        _authService = authService;
        _notificationService = notificationService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Exécute toutes les vérifications de rétention pour un utilisateur.
    /// Appelé périodiquement depuis App.xaml.cs (toutes les 2h).
    /// </summary>
    public async Task RunAllChecksAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        _logger.LogDebug("[Retention] Lancement des vérifications de rétention pour {UserId}", userId);

        try
        {
            await SafeExecuteAsync(() => CheckStreakDangerAsync(userId), "StreakDanger");
            await SafeExecuteAsync(() => CheckBadgeProgressAsync(userId), "BadgeProgress");
            await SafeExecuteAsync(() => CheckAlertMatchesAsync(userId), "AlertMatches");
            await SafeExecuteAsync(() => CheckFavoriteStatusChangesAsync(userId), "FavoriteStatus");
            await SafeExecuteAsync(() => CheckUnreadMessagesAsync(userId), "UnreadMessages");
            await SafeExecuteAsync(() => CheckUpcomingEventRemindersAsync(userId), "EventReminders");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Retention] Erreur globale des vérifications de rétention");
        }

        _logger.LogDebug("[Retention] Vérifications de rétention terminées");
    }

    // ═══════════════════════════════════════════════════
    // 1. STREAK EN DANGER
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie si le streak de l'utilisateur est en danger de se casser.
    /// Conditions : streak ≥ 2 jours, pas de récompense réclamée aujourd'hui, heure ≥ 18h locale.
    /// </summary>
    private async Task CheckStreakDangerAsync(string userId)
    {
        if (!CanSendNotification("streak_danger")) return;

        var profile = await _gamificationService.GetUserProfileAsync(userId);
        if (profile.DailyStreak < 2) return;

        var lastClaim = profile.LastDailyRewardClaimed.Date;
        var today = DateTime.UtcNow.Date;

        if (lastClaim >= today) return;

        var localHour = DateTime.Now.Hour;
        if (localHour < 18) return;

        var streakDays = profile.DailyStreak;
        var title = "🔥 Votre série est en danger !";
        var message = $"Votre série de {streakDays} jours va se casser à minuit ! Ouvrez l'app pour la maintenir.";

        await _notificationService.ShowGamificationNotificationAsync(title, message, "streak_danger", userId);
        MarkNotificationSent("streak_danger");

        _logger.LogInformation("[Retention] Notification streak danger envoyée ({Days}j) pour {UserId}", streakDays, userId);
    }

    // ═══════════════════════════════════════════════════
    // 2. BADGE PROCHE DU DÉBLOCAGE
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie si l'utilisateur est proche de débloquer un badge (≥ 75% de progression).
    /// Envoie un nudge pour l'encourager à finir.
    /// </summary>
    private async Task CheckBadgeProgressAsync(string userId)
    {
        if (!CanSendNotification("badge_progress")) return;

        var profile = await _gamificationService.GetUserProfileAsync(userId);
        var allBadges = await _gamificationService.GetAvailableBadgesAsync(userId);

        var lockedBadges = allBadges
            .Where(b => !profile.UnlockedBadges.Contains(b.Id) && !b.IsSecret)
            .ToList();

        Badge? bestCandidate = null;
        double bestProgress = 0;
        int bestRemaining = int.MaxValue;

        foreach (var badge in lockedBadges)
        {
            var currentValue = profile.Stats.GetValueOrDefault(badge.StatKey, 0);
            if (badge.RequiredValue <= 0) continue;

            var progress = (double)currentValue / badge.RequiredValue;
            var remaining = badge.RequiredValue - currentValue;

            if (progress >= 0.75 && progress < 1.0 && remaining < bestRemaining)
            {
                bestCandidate = badge;
                bestProgress = progress;
                bestRemaining = remaining;
            }
        }

        if (bestCandidate == null) return;

        var progressPct = (int)(bestProgress * 100);
        var title = $"🏅 Badge {bestCandidate.Icon} presque débloqué !";
        var message = $"Plus que {bestRemaining} action(s) pour obtenir \"{bestCandidate.Name}\" ({progressPct}%)";

        await _notificationService.ShowGamificationNotificationAsync(title, message, "badge_progress", bestCandidate.Id);
        MarkNotificationSent("badge_progress");

        _logger.LogInformation("[Retention] Nudge badge '{Badge}' ({Progress}%) pour {UserId}",
            bestCandidate.Name, progressPct, userId);
    }

    // ═══════════════════════════════════════════════════
    // 3. ALERTES D'ANNONCES CORRESPONDANTES
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie les alertes utilisateur contre les annonces publiées récemment.
    /// </summary>
    private async Task CheckAlertMatchesAsync(string userId)
    {
        if (!CanSendNotification("alert_match")) return;

        var alerts = await _favoritesService.GetUserAlertsAsync();
        var activeAlerts = alerts.Where(a => a.IsActive).ToList();

        if (!activeAlerts.Any()) return;

        var allAnnonces = await _firebaseService.GetAnnoncesAsync();
        var cutoff = DateTime.UtcNow.AddHours(-3);
        var recentAnnonces = allAnnonces
            .Where(a => a.DateCreation > cutoff &&
                        a.UtilisateurId != userId &&
                        a.Statut == StatutAnnonce.Disponible)
            .ToList();

        if (!recentAnnonces.Any()) return;

        var matchCount = 0;

        foreach (var alert in activeAlerts)
        {
            foreach (var annonce in recentAnnonces)
            {
                if (alert.MatchedAnnonceIds.Contains(annonce.Id)) continue;

                if (DoesAnnonceMatchAlert(annonce, alert))
                {
                    var title = $"🔔 Alerte : {alert.Name}";
                    var message = $"Nouvelle annonce : {annonce.Titre}";

                    if (!string.IsNullOrEmpty(annonce.Ville))
                        message += $" à {annonce.Ville}";

                    await _notificationService.ShowProximityNotificationAsync(title, message, annonce.Id);
                    await UpdateAlertTriggerAsync(alert, annonce.Id);
                    matchCount++;

                    if (matchCount >= 3) break;
                }
            }

            if (matchCount >= 3) break;
        }

        if (matchCount > 0)
        {
            MarkNotificationSent("alert_match");
            _logger.LogInformation("[Retention] {Count} alerte(s) déclenchée(s) pour {UserId}", matchCount, userId);
        }
    }

    /// <summary>
    /// Vérifie si une annonce correspond aux critères d'une alerte.
    /// </summary>
    private static bool DoesAnnonceMatchAlert(Annonce annonce, AnnonceAlert alert)
    {
        if (alert.AnnonceType != "both" &&
            !string.Equals(alert.AnnonceType, annonce.Type, StringComparison.OrdinalIgnoreCase))
            return false;

        if (alert.Categories.Any() && !alert.Categories.Contains(annonce.Categorie))
            return false;

        if (alert.Keywords.Any())
        {
            var annonceText = $"{annonce.Titre} {annonce.Description}".ToLower();
            if (!alert.Keywords.Any(kw => annonceText.Contains(kw.ToLower())))
                return false;
        }

        if (!string.IsNullOrEmpty(alert.Location))
        {
            var loc = annonce.Localisation ?? annonce.Ville ?? "";
            if (!loc.Contains(alert.Location, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Met à jour le compteur de déclenchement d'une alerte.
    /// </summary>
    private Task UpdateAlertTriggerAsync(AnnonceAlert alert, string annonceId)
    {
        try
        {
            alert.LastTriggered = DateTime.Now;
            alert.TriggerCount++;
            if (!alert.MatchedAnnonceIds.Contains(annonceId))
                alert.MatchedAnnonceIds.Add(annonceId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Retention] Erreur mise à jour alerte: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════
    // 4. FAVORIS DONT LE STATUT A CHANGÉ
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie si des annonces en favoris ont changé de statut
    /// (réservée, échangée, archivée) et notifie l'utilisateur.
    /// </summary>
    private async Task CheckFavoriteStatusChangesAsync(string userId)
    {
        if (!CanSendNotification("favorite_status")) return;

        var favorites = await _favoritesService.GetUserFavoritesAsync();
        if (!favorites.Any()) return;

        var favoritesToCheck = favorites.Take(MaxFavoritesToCheck).ToList();
        var notificationsSent = 0;

        foreach (var fav in favoritesToCheck)
        {
            try
            {
                var annonce = await _firebaseService.GetAnnonceAsync(fav.AnnonceId);
                if (annonce == null) continue;

                var prefKey = $"fav_status_{fav.AnnonceId}";
                var lastKnownStatus = Preferences.Get(prefKey, "Disponible");
                var currentStatus = annonce.Statut.ToString();

                if (lastKnownStatus != currentStatus)
                {
                    Preferences.Set(prefKey, currentStatus);

                    if (annonce.Statut == StatutAnnonce.Disponible && lastKnownStatus != "Disponible")
                    {
                        await _notificationService.ShowMessageNotificationAsync(
                            "✅ Annonce à nouveau disponible !",
                            $"\"{annonce.Titre}\" est à nouveau disponible",
                            $"favorite_{fav.AnnonceId}");
                        notificationsSent++;
                    }
                    else if (annonce.Statut == StatutAnnonce.Reservee)
                    {
                        await _notificationService.ShowMessageNotificationAsync(
                            "⚡ Annonce favorite réservée",
                            $"\"{annonce.Titre}\" vient d'être réservée. Dépêchez-vous !",
                            $"favorite_{fav.AnnonceId}");
                        notificationsSent++;
                    }
                    else if (annonce.Statut == StatutAnnonce.Echangee)
                    {
                        await _notificationService.ShowMessageNotificationAsync(
                            "📦 Annonce favorite échangée",
                            $"\"{annonce.Titre}\" a été échangée",
                            $"favorite_{fav.AnnonceId}");
                        notificationsSent++;
                    }
                    else if (annonce.Statut == StatutAnnonce.Archivee)
                    {
                        await _notificationService.ShowMessageNotificationAsync(
                            "📁 Annonce favorite archivée",
                            $"\"{annonce.Titre}\" a été retirée par son auteur",
                            $"favorite_{fav.AnnonceId}");
                        notificationsSent++;
                    }
                }

                if (notificationsSent >= 3) break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Retention] Erreur vérification favori {fav.AnnonceId}: {ex.Message}");
            }
        }

        if (notificationsSent > 0)
        {
            MarkNotificationSent("favorite_status");
            _logger.LogInformation("[Retention] {Count} changement(s) de statut favori notifié(s) pour {UserId}",
                notificationsSent, userId);
        }
    }

    // ═══════════════════════════════════════════════════
    // 5. MESSAGES NON LUS — RAPPELS INTELLIGENTS
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie s'il y a des messages non lus depuis longtemps et envoie des rappels.
    /// - Rappel doux après 2h : "Vous avez X message(s) non lu(s)"
    /// - Rappel urgent après 24h : "X attend votre réponse depuis hier"
    /// Cooldowns séparés pour les 2 niveaux d'urgence.
    /// </summary>
    private async Task CheckUnreadMessagesAsync(string userId)
    {
        try
        {
            // Récupérer les conversations de l'utilisateur
            var conversations = await _firebaseService.GetUserConversationsAsync(userId);
            if (!conversations.Any()) return;

            var now = DateTime.UtcNow;
            var urgentConversations = new List<(Conversation conv, string senderName, TimeSpan age)>();
            var oldConversations = new List<(Conversation conv, string senderName, TimeSpan age)>();

            foreach (var conv in conversations)
            {
                try
                {
                    // Récupérer les messages de la conversation
                    var messages = await _firebaseService.GetMessagesAsync(conv.Id);
                    if (!messages.Any()) continue;

                    // Trouver le dernier message non lu envoyé par l'autre personne
                    var lastUnreadFromOther = messages
                        .Where(m => m.SenderId != userId && !m.IsRead)
                        .OrderByDescending(m => m.Timestamp)
                        .FirstOrDefault();

                    if (lastUnreadFromOther == null) continue;

                    var messageAge = now - lastUnreadFromOther.Timestamp;
                    
                    // Ignorer les messages très récents (< 2h) — GlobalNotificationService les a déjà notifiés
                    if (messageAge.TotalHours < 2) continue;

                    // Récupérer le nom de l'expéditeur
                    var senderProfile = await _firebaseService.GetUserProfileAsync(lastUnreadFromOther.SenderId);
                    var senderName = senderProfile?.Name ?? "Quelqu'un";

                    if (messageAge.TotalHours >= 24)
                    {
                        urgentConversations.Add((conv, senderName, messageAge));
                    }
                    else // Entre 2h et 24h
                    {
                        oldConversations.Add((conv, senderName, messageAge));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[Retention] Erreur lecture conversation {ConvId}", conv.Id);
                }
            }

            // Priorité 1 : Rappel urgent (24h+)
            if (urgentConversations.Any() && CanSendNotification("unread_urgent"))
            {
                await SendUrgentUnreadNotificationAsync(urgentConversations);
                MarkNotificationSent("unread_urgent");
                return; // Ne pas envoyer les 2 types dans le même cycle
            }

            // Priorité 2 : Rappel doux (2h-24h)
            if (oldConversations.Any() && CanSendNotification("unread_reminder"))
            {
                await SendSoftUnreadNotificationAsync(oldConversations);
                MarkNotificationSent("unread_reminder");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Retention] Erreur lors de la vérification des messages non lus");
        }
    }

    /// <summary>
    /// Envoie un rappel urgent pour les messages non lus depuis 24h+.
    /// Mentionne le nom de l'expéditeur pour créer un sentiment d'obligation sociale.
    /// </summary>
    private async Task SendUrgentUnreadNotificationAsync(
        List<(Conversation conv, string senderName, TimeSpan age)> conversations)
    {
        var sorted = conversations.OrderByDescending(c => c.age).ToList();
        var first = sorted.First();

        string title;
        string message;

        if (sorted.Count == 1)
        {
            var days = (int)first.age.TotalDays;
            var timeText = days >= 2 ? $"depuis {days} jours" : "depuis hier";
            title = $"💬 {first.senderName} attend votre réponse";
            message = $"{first.senderName} vous a envoyé un message {timeText}. Ne le faites pas attendre !";
        }
        else
        {
            title = $"💬 {sorted.Count} conversations en attente";
            message = $"{first.senderName} et {sorted.Count - 1} autre(s) attendent votre réponse depuis plus de 24h";
        }

        await _notificationService.ShowMessageNotificationAsync(title, message, first.conv.Id);
        _logger.LogInformation("[Retention] Rappel URGENT messages non lus ({Count} conv) pour {UserId}",
            sorted.Count, first.conv.BuyerId);
    }

    /// <summary>
    /// Envoie un rappel doux pour les messages non lus depuis 2h-24h.
    /// Plus générique, sans urgence excessive.
    /// </summary>
    private async Task SendSoftUnreadNotificationAsync(
        List<(Conversation conv, string senderName, TimeSpan age)> conversations)
    {
        var sorted = conversations.OrderByDescending(c => c.age).ToList();
        var first = sorted.First();

        string title;
        string message;

        if (sorted.Count == 1)
        {
            title = "💬 Nouveau message non lu";
            message = $"{first.senderName} vous a envoyé un message. Consultez votre boîte de réception !";
        }
        else
        {
            title = $"💬 {sorted.Count} messages non lus";
            message = $"Vous avez des messages de {first.senderName} et {sorted.Count - 1} autre(s). Répondez pour ne rien manquer !";
        }

        await _notificationService.ShowMessageNotificationAsync(title, message, first.conv.Id);
        _logger.LogInformation("[Retention] Rappel doux messages non lus ({Count} conv) pour utilisateur",
            sorted.Count);
    }

    // ═══════════════════════════════════════════════════
    // SYSTÈME DE COOLDOWN
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie si on peut envoyer une notification de ce type (cooldown 20h).
    /// </summary>
    private bool CanSendNotification(string notificationType)
    {
        var key = $"{CooldownPrefix}{notificationType}";
        var lastSentTicks = Preferences.Get(key, 0L);

        if (lastSentTicks == 0) return true;

        var lastSent = new DateTime(lastSentTicks, DateTimeKind.Utc);
        var elapsed = DateTime.UtcNow - lastSent;

        if (elapsed < CooldownDuration)
        {
            _logger.LogDebug("[Retention] Cooldown actif pour {Type} (encore {Remaining})",
                notificationType, CooldownDuration - elapsed);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Marque qu'une notification de ce type vient d'être envoyée.
    /// </summary>
    private void MarkNotificationSent(string notificationType)
    {
        var key = $"{CooldownPrefix}{notificationType}";
        Preferences.Set(key, DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// Exécute une tâche de manière sécurisée (catch + log).
    /// </summary>
    private async Task SafeExecuteAsync(Func<Task> action, string checkName)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Retention] Erreur lors de la vérification {Check}", checkName);
        }
    }

    // ═══════════════════════════════════════════════════
    // 6. RAPPELS ÉVÉNEMENTS J-3 / J-1 / J0 (Phase 4)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Vérifie les événements à venir auxquels l'utilisateur participe (créés ou rejoints)
    /// et envoie un rappel local J-3, J-1 et J0 (jour J).
    /// Une notification est envoyée au maximum une fois par event/jour grâce à un flag local.
    /// </summary>
    private async Task CheckUpcomingEventRemindersAsync(string userId)
    {
        if (_eventService == null) return;

        try
        {
            var (created, joined) = await _eventService.GetMyEventsAsync(userId);
            var allMine = created.Concat(joined).ToList();
            if (!allMine.Any()) return;

            var now = DateTime.UtcNow;
            var notificationsSent = 0;

            foreach (var ev in allMine)
            {
                if (ev.EstTermine() || ev.Statut == Models.StatutEvenement.Annule)
                    continue;

                var hoursUntil = (ev.DateDebut - now).TotalHours;
                if (hoursUntil < 0 || hoursUntil > 24 * 4) continue; // Pas dans la fenêtre J-3 → jour J

                int? targetDay = null;
                string title = string.Empty;
                string message = string.Empty;

                if (hoursUntil <= 6) // Jour J (≤ 6h avant le début, ou en cours dans la journée)
                {
                    targetDay = 0;
                    title = $"🎉 C'est aujourd'hui : {ev.Titre} !";
                    message = !string.IsNullOrEmpty(ev.AdresseRdv)
                        ? $"📍 RDV à {ev.AdresseRdv} — {ev.DateDebut:HH:mm}"
                        : $"L'événement commence à {ev.DateDebut:HH:mm}. À tout de suite !";
                }
                else if (hoursUntil <= 30) // J-1 (entre 6h et 30h avant)
                {
                    targetDay = 1;
                    title = $"⏰ Demain : {ev.Titre}";
                    message = $"Plus que 24h ! RDV {ev.DateDebut:dd/MM 'à' HH:mm}"
                              + (!string.IsNullOrEmpty(ev.AdresseRdv) ? $" — {ev.AdresseRdv}" : "");
                }
                else if (hoursUntil >= 66 && hoursUntil <= 78) // J-3 (≈ 72h ± 6h)
                {
                    targetDay = 3;
                    title = $"📅 Dans 3 jours : {ev.Titre}";
                    message = $"N'oublie pas l'événement le {ev.DateDebut:dddd dd MMMM 'à' HH:mm}";
                }

                if (targetDay == null) continue;

                // Une seule notification par (event, jour). Pas de cooldown — flag "déjà envoyé"
                // qui sera nettoyé naturellement quand l'événement sera supprimé/passé.
                var sentKey = $"event_reminder_sent_{ev.Id}_d{targetDay}";
                if (Preferences.Get(sentKey, false)) continue;

                await _notificationService.ShowGamificationNotificationAsync(
                    title, message, "event_reminder", ev.Id);

                Preferences.Set(sentKey, true);
                notificationsSent++;

                _logger.LogInformation(
                    "[Retention] Rappel événement J-{Day} envoyé pour '{Title}' ({EventId})",
                    targetDay, ev.Titre, ev.Id);

                // Limite : max 3 rappels événements par cycle pour ne pas spammer
                if (notificationsSent >= 3) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Retention] Erreur lors de la vérification des rappels d'événements");
        }
    }
}


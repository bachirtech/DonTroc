using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services;

/// <summary>
/// Service de gestion des événements / trocs groupés DonTroc.
/// Stocke dans Firebase Realtime Database :
///  - <c>Events/{eventId}</c> → <see cref="Evenement"/>
///  - <c>EventParticipants/{eventId}/{userId}</c> → <see cref="EventParticipant"/>
/// </summary>
public class EventService
{
    private readonly FirebaseClient _firebaseClient;
    private readonly AuthService _authService;
    private readonly FirebaseService _firebaseService;
    private readonly GeolocationService _geolocationService;
    private readonly GamificationService? _gamificationService;
    private readonly ILogger<EventService>? _logger;

    /// <summary>Rayon par défaut pour les événements locaux (cohérent avec RecommendationService).</summary>
    public const double DEFAULT_RADIUS_KM = 50.0;

    /// <summary>
    /// Dernier message d'erreur rencontré (utile pour afficher la cause réelle dans l'UI
    /// quand <see cref="CreateEventAsync"/> retourne null).
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Convertit une exception Firebase en message d'erreur safe pour l'UI.
    /// Supprime les URLs, tokens JWT, stack traces et autres données sensibles.
    /// </summary>
    private static string SanitizeError(Exception ex)
    {
        var raw = ex.Message ?? string.Empty;

        // Détection des erreurs Firebase typiques (basées sur le status code dans le message)
        if (raw.Contains("401") || raw.Contains("Unauthorized") || raw.Contains("Permission denied"))
            return "Permission refusée par le serveur. Tu n'es peut-être pas autorisé à effectuer cette action.";
        if (raw.Contains("403") || raw.Contains("Forbidden"))
            return "Accès interdit.";
        if (raw.Contains("404") || raw.Contains("Not Found"))
            return "Ressource introuvable.";
        if (raw.Contains("400") || raw.Contains("Bad Request"))
            return "Données invalides envoyées au serveur.";
        if (raw.Contains("timeout", StringComparison.OrdinalIgnoreCase) || ex is TaskCanceledException)
            return "Délai dépassé. Vérifie ta connexion internet.";
        if (raw.Contains("No such host", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("network", StringComparison.OrdinalIgnoreCase)
            || ex is System.Net.Http.HttpRequestException)
            return "Problème de connexion. Vérifie ton réseau.";

        // Strip toute URL ou token éventuel restant (sécurité défensive)
        var clean = System.Text.RegularExpressions.Regex.Replace(
            raw, @"https?://\S+", "[url]");
        clean = System.Text.RegularExpressions.Regex.Replace(
            clean, @"eyJ[A-Za-z0-9_\-\.]{20,}", "[token]");

        // Limiter la longueur pour éviter d'afficher une stack
        if (clean.Length > 200) clean = clean.Substring(0, 200) + "…";

        return string.IsNullOrWhiteSpace(clean) ? "Erreur inconnue." : clean;
    }

    public EventService(
        AuthService authService,
        FirebaseService firebaseService,
        GeolocationService geolocationService,
        GamificationService? gamificationService = null,
        ILogger<EventService>? logger = null)
    {
        _authService = authService;
        _firebaseService = firebaseService;
        _geolocationService = geolocationService;
        _gamificationService = gamificationService;
        _logger = logger;

        _firebaseClient = new FirebaseClient(
            ConfigurationService.FirebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await _authService.GetAuthTokenAsync() ?? string.Empty
            });
    }

    // === CRUD ===

    /// <summary>
    /// Crée un nouvel événement et inscrit automatiquement le créateur comme participant.
    /// </summary>
    /// <returns>L'ID de l'événement créé, ou null si échec.</returns>
    public async Task<string?> CreateEventAsync(Evenement evenement)
    {
        LastError = null;
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Utilisateur non authentifié");

            // Forcer le créateur côté serveur (ne pas faire confiance au client)
            evenement.CreateurId = userId;
            evenement.Id = string.IsNullOrEmpty(evenement.Id) ? Guid.NewGuid().ToString() : evenement.Id;
            evenement.DateCreation = DateTime.UtcNow;
            evenement.NombreParticipants = 1;
            if (evenement.Statut == StatutEvenement.Brouillon)
                evenement.Statut = StatutEvenement.AVenir;

            // Seul un admin pourrait positionner IsOfficial, mais on ne peut pas le valider côté client :
            // les règles Firebase font foi (cf. firebase_rules.json → Events).
            evenement.SyncTimestamps();

            await _firebaseClient
                .Child("Events")
                .Child(evenement.Id)
                .PutAsync(evenement);

            // Inscrire le créateur comme participant
            var participant = new EventParticipant
            {
                EventId = evenement.Id,
                UserId = userId,
                UserName = evenement.CreateurName,
                UserAvatarUrl = evenement.CreateurAvatarUrl,
                Role = RoleParticipant.Createur
            };

            await _firebaseClient
                .Child("EventParticipants")
                .Child(evenement.Id)
                .Child(userId)
                .PutAsync(participant);

            // Gamification : XP + stat + check badges (Phase 4)
            await TryAwardCreateEventAsync(userId);

            return evenement.Id;
        }
        catch (Exception ex)
        {
            LastError = SanitizeError(ex);
            _logger?.LogError(ex, "Erreur création événement");
            Debug.WriteLine($"[EventService] CreateEvent: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(Evenement evenement)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId) || !evenement.PeutEtreModifie(userId))
                return false;

            evenement.SyncTimestamps();

            await _firebaseClient
                .Child("Events")
                .Child(evenement.Id)
                .PutAsync(evenement);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur mise à jour événement {Id}", evenement.Id);
            return false;
        }
    }

    /// <summary>
    /// Supprime un événement. Seul le créateur (ou un admin via les Firebase rules) peut supprimer.
    /// Supprime aussi tous les participants associés.
    /// </summary>
    public async Task<bool> DeleteEventAsync(string eventId)
    {
        LastError = null;
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                LastError = "Utilisateur non authentifié";
                return false;
            }

            // Vérification client : seul le créateur peut supprimer.
            // (Les Firebase rules font foi côté serveur — un admin pourra aussi supprimer.)
            var ev = await GetEventByIdAsync(eventId);
            if (ev == null)
            {
                LastError = "Événement introuvable";
                return false;
            }

            if (ev.CreateurId != userId)
            {
                LastError = "Tu n'es pas le créateur de cet événement";
                return false;
            }

            // 1) Supprimer d'abord tous les participants.
            //    Tentative en bulk (rapide), avec fallback per-user si les règles ne le permettent pas
            //    (cas où les règles Firebase ne sont pas encore déployées).
            try
            {
                await _firebaseClient.Child("EventParticipants").Child(eventId).DeleteAsync();
            }
            catch (Exception bulkEx)
            {
                _logger?.LogWarning(bulkEx, "Bulk delete EventParticipants/{Id} failed, falling back per-user", eventId);
                try
                {
                    var participants = await GetParticipantsAsync(eventId);
                    foreach (var p in participants)
                    {
                        if (string.IsNullOrEmpty(p.UserId)) continue;
                        try
                        {
                            await _firebaseClient
                                .Child("EventParticipants")
                                .Child(eventId)
                                .Child(p.UserId)
                                .DeleteAsync();
                        }
                        catch (Exception perUserEx)
                        {
                            _logger?.LogWarning(perUserEx, "Per-user delete failed {Event}/{User}", eventId, p.UserId);
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger?.LogError(fallbackEx, "Fallback per-user delete failed for {Id}", eventId);
                    // On continue quand même pour tenter de supprimer l'Event (les orphelins seront nettoyés plus tard).
                }
            }

            // 2) Supprimer l'événement lui-même
            await _firebaseClient.Child("Events").Child(eventId).DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            LastError = SanitizeError(ex);
            _logger?.LogError(ex, "Erreur suppression événement {Id}", eventId);
            return false;
        }
    }

    public async Task<Evenement?> GetEventByIdAsync(string eventId)
    {
        try
        {
            var ev = await _firebaseClient
                .Child("Events")
                .Child(eventId)
                .OnceSingleAsync<Evenement>();

            if (ev == null) return null;
            ev.Id = eventId;
            ev.RestoreDatesFromTimestamps();

            // Marquer EstParticipant pour l'utilisateur courant
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                ev.EstParticipant = await IsParticipantAsync(eventId, userId);
            }

            return ev;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur GetEventByIdAsync {Id}", eventId);
            return null;
        }
    }

    // === REQUÊTES LISTE ===

    /// <summary>Tous les événements à venir (DateDebut > maintenant), triés du plus proche au plus lointain.</summary>
    public async Task<List<Evenement>> GetUpcomingEventsAsync(int limit = 20)
    {
        var all = await GetAllEventsRawAsync();
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return all
            .Where(e => e.DateDebutTimestamp >= nowMs && e.Statut == StatutEvenement.AVenir)
            .OrderBy(e => e.DateDebutTimestamp)
            .Take(limit)
            .ToList();
    }

    /// <summary>Événements officiels saisonniers (broadcast admin).</summary>
    public async Task<List<Evenement>> GetSeasonalEventsAsync(int limit = 10)
    {
        var all = await GetAllEventsRawAsync();
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return all
            .Where(e => e.IsOfficial && e.DateFinTimestamp >= nowMs)
            .OrderBy(e => e.DateDebutTimestamp)
            .Take(limit)
            .ToList();
    }

    /// <summary>Événements géolocalisés dans un rayon autour de la position courante (default 50 km).</summary>
    public async Task<List<Evenement>> GetNearbyEventsAsync(double radiusKm = DEFAULT_RADIUS_KM, int limit = 20)
    {
        var location = _geolocationService.GetLastKnownLocation();
        if (location == null)
            return new List<Evenement>();

        var all = await GetAllEventsRawAsync();
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var filtered = all
            .Where(e => e.Latitude.HasValue && e.Longitude.HasValue
                        && !(e.Latitude == 0 && e.Longitude == 0)
                        && e.DateFinTimestamp >= nowMs)
            .Select(e =>
            {
                e.DistanceFromUser = _geolocationService.CalculateDistance(
                    location.Latitude, location.Longitude,
                    e.Latitude!.Value, e.Longitude!.Value);
                return e;
            })
            .Where(e => e.DistanceFromUser <= radiusKm)
            .OrderBy(e => e.DistanceFromUser)
            .Take(limit)
            .ToList();

        return filtered;
    }

    /// <summary>
    /// Événements créés par l'utilisateur ou auxquels il participe.
    /// </summary>
    public async Task<(List<Evenement> created, List<Evenement> joined)> GetMyEventsAsync(string userId)
    {
        var all = await GetAllEventsRawAsync();
        var created = all.Where(e => e.CreateurId == userId).OrderByDescending(e => e.DateDebutTimestamp).ToList();

        var joined = new List<Evenement>();
        foreach (var ev in all.Where(e => e.CreateurId != userId))
        {
            if (await IsParticipantAsync(ev.Id, userId))
            {
                ev.EstParticipant = true;
                joined.Add(ev);
            }
        }
        joined = joined.OrderByDescending(e => e.DateDebutTimestamp).ToList();

        return (created, joined);
    }

    /// <summary>
    /// Récupère TOUS les événements (utilisé en interne, pas de pagination pour < 3000 events).
    /// </summary>
    private async Task<List<Evenement>> GetAllEventsRawAsync()
    {
        try
        {
            var items = await _firebaseClient
                .Child("Events")
                .OnceAsync<Evenement>();

            return items?.Select(item =>
            {
                var ev = item.Object;
                if (ev != null)
                {
                    ev.Id = item.Key;
                    ev.RestoreDatesFromTimestamps();
                }
                return ev;
            }).Where(e => e != null).Cast<Evenement>().ToList() ?? new List<Evenement>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur récupération événements");
            return new List<Evenement>();
        }
    }

    // === PARTICIPATION ===

    public async Task<bool> JoinEventAsync(string eventId, string userName, string? avatarUrl = null)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Utilisateur non authentifié");

            // Vérifier que l'événement existe et n'est pas complet
            var ev = await GetEventByIdAsync(eventId);
            if (ev == null || ev.EstTermine() || ev.Statut == StatutEvenement.Annule)
                return false;

            if (ev.ComplèteSurParticipants() && ev.CreateurId != userId)
                return false;

            // Si déjà participant → no-op
            if (await IsParticipantAsync(eventId, userId))
                return true;

            var participant = new EventParticipant
            {
                EventId = eventId,
                UserId = userId,
                UserName = userName,
                UserAvatarUrl = avatarUrl,
                Role = RoleParticipant.Confirme
            };

            await _firebaseClient
                .Child("EventParticipants")
                .Child(eventId)
                .Child(userId)
                .PutAsync(participant);

            // Mettre à jour le compteur sur l'événement
            await _firebaseClient
                .Child("Events")
                .Child(eventId)
                .Child("NombreParticipants")
                .PutAsync(ev.NombreParticipants + 1);

            // Gamification : XP + stat + check badges (Phase 4)
            await TryAwardJoinEventAsync(userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur JoinEvent {Id}", eventId);
            return false;
        }
    }

    public async Task<bool> LeaveEventAsync(string eventId)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var ev = await GetEventByIdAsync(eventId);
            if (ev == null) return false;

            // Le créateur ne peut pas "quitter" son propre événement (il doit le supprimer/annuler)
            if (ev.CreateurId == userId)
                return false;

            if (!await IsParticipantAsync(eventId, userId))
                return true;

            await _firebaseClient
                .Child("EventParticipants")
                .Child(eventId)
                .Child(userId)
                .DeleteAsync();

            await _firebaseClient
                .Child("Events")
                .Child(eventId)
                .Child("NombreParticipants")
                .PutAsync(Math.Max(0, ev.NombreParticipants - 1));

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur LeaveEvent {Id}", eventId);
            return false;
        }
    }

    public async Task<bool> IsParticipantAsync(string eventId, string userId)
    {
        try
        {
            var p = await _firebaseClient
                .Child("EventParticipants")
                .Child(eventId)
                .Child(userId)
                .OnceSingleAsync<EventParticipant>();
            return p != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<EventParticipant>> GetParticipantsAsync(string eventId)
    {
        try
        {
            var items = await _firebaseClient
                .Child("EventParticipants")
                .Child(eventId)
                .OnceAsync<EventParticipant>();

            return items?.Select(item =>
            {
                var p = item.Object;
                if (p != null) p.UserId = item.Key;
                return p;
            }).Where(p => p != null).Cast<EventParticipant>().ToList() ?? new List<EventParticipant>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur GetParticipants {Id}", eventId);
            return new List<EventParticipant>();
        }
    }

    // === MARCHÉ VIRTUEL ===

    /// <summary>
    /// Ajoute une annonce de l'utilisateur courant au marché virtuel de l'événement.
    /// L'utilisateur doit être participant et propriétaire de l'annonce.
    /// </summary>
    public async Task<bool> AddAnnonceToEventAsync(string eventId, string annonceId)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return false;

            var ev = await GetEventByIdAsync(eventId);
            if (ev == null) return false;
            if (ev.AnnoncesIds.Contains(annonceId)) return true;

            ev.AnnoncesIds.Add(annonceId);

            await _firebaseClient
                .Child("Events")
                .Child(eventId)
                .Child("AnnoncesIds")
                .PutAsync(ev.AnnoncesIds);

            // Référence dans le profil participant
            var participant = await _firebaseClient
                .Child("EventParticipants").Child(eventId).Child(userId)
                .OnceSingleAsync<EventParticipant>();

            if (participant != null)
            {
                if (!participant.AnnoncesPartagees.Contains(annonceId))
                    participant.AnnoncesPartagees.Add(annonceId);

                await _firebaseClient
                    .Child("EventParticipants").Child(eventId).Child(userId)
                    .Child("AnnoncesPartagees")
                    .PutAsync(participant.AnnoncesPartagees);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur AddAnnonceToEvent {Event}/{Annonce}", eventId, annonceId);
            return false;
        }
    }

    public async Task<bool> RemoveAnnonceFromEventAsync(string eventId, string annonceId)
    {
        try
        {
            var ev = await GetEventByIdAsync(eventId);
            if (ev == null) return false;

            ev.AnnoncesIds.Remove(annonceId);
            await _firebaseClient
                .Child("Events")
                .Child(eventId)
                .Child("AnnoncesIds")
                .PutAsync(ev.AnnoncesIds);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur RemoveAnnonceFromEvent {Event}/{Annonce}", eventId, annonceId);
            return false;
        }
    }

    /// <summary>
    /// Charge les annonces du marché virtuel d'un événement.
    /// </summary>
    public async Task<List<Annonce>> GetMarketAnnoncesAsync(string eventId)
    {
        try
        {
            var ev = await GetEventByIdAsync(eventId);
            if (ev == null || ev.AnnoncesIds.Count == 0)
                return new List<Annonce>();

            var result = new List<Annonce>();
            foreach (var id in ev.AnnoncesIds)
            {
                var a = await _firebaseService.GetAnnonceAsync(id);
                if (a != null && a.Statut == StatutAnnonce.Disponible)
                    result.Add(a);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur GetMarketAnnonces {Id}", eventId);
            return new List<Annonce>();
        }
    }

    // === GAMIFICATION (Phase 4) ===

    /// <summary>
    /// Incrémente la stat <c>events_created</c>, attribue les XP et vérifie les badges
    /// "Organisateur", "Maître Organisateur", "Légende des Événements".
    /// </summary>
    private async Task TryAwardCreateEventAsync(string userId)
    {
        if (_gamificationService == null) return;
        try
        {
            await _gamificationService.IncrementStatAsync(userId, "events_created", 1);
            await _gamificationService.AddXpAsync(userId, "create_event");
            var newCount = await _gamificationService.GetStatAsync(userId, "events_created");
            var badge = await _gamificationService.CheckAndUnlockBadgeAsync(userId, "events_created", newCount);
            if (badge != null)
            {
                _logger?.LogInformation("[Gamification] Badge {Id} débloqué (events_created={Count})", badge.Id, newCount);
            }
            // Mettre à jour la progression des défis si applicable
            await _gamificationService.UpdateChallengeProgressAsync(userId, "create_event");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[Gamification] Erreur award create_event");
        }
    }

    /// <summary>
    /// Incrémente la stat <c>events_joined</c>, attribue les XP et vérifie les badges
    /// "Participant", "Participant Assidu", "Accro aux Événements".
    /// </summary>
    private async Task TryAwardJoinEventAsync(string userId)
    {
        if (_gamificationService == null) return;
        try
        {
            await _gamificationService.IncrementStatAsync(userId, "events_joined", 1);
            await _gamificationService.AddXpAsync(userId, "join_event");
            var newCount = await _gamificationService.GetStatAsync(userId, "events_joined");
            var badge = await _gamificationService.CheckAndUnlockBadgeAsync(userId, "events_joined", newCount);
            if (badge != null)
            {
                _logger?.LogInformation("[Gamification] Badge {Id} débloqué (events_joined={Count})", badge.Id, newCount);
            }
            await _gamificationService.UpdateChallengeProgressAsync(userId, "join_event");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[Gamification] Erreur award join_event");
        }
    }
}


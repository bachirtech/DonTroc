using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;

namespace DonTroc.Services;

/// <summary>
/// Service pour gérer les favoris, listes personnalisées et alertes
/// </summary>
public class FavoritesService
{
    private readonly FirebaseClient _firebaseClient;
    private readonly AuthService _authService;
    private readonly NotificationService _notificationService;
    
    // Collections Firebase
    private const string FAVORITES_COLLECTION = "favorites";
    private const string FAVORITE_LISTS_COLLECTION = "favoriteLists";
    private const string ALERTS_COLLECTION = "annonceAlerts";
    
    // Cache local
    private List<Favorite> _userFavorites = new();
    private List<FavoriteList> _userLists = new();
    private List<AnnonceAlert> _userAlerts = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheValidDuration = TimeSpan.FromMinutes(5);

    public FavoritesService(AuthService authService, NotificationService notificationService)
    {
        _authService = authService;
        _notificationService = notificationService;
        _firebaseClient = new FirebaseClient(
            ConfigurationService.FirebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await _authService.GetAuthTokenAsync() ?? ""
            });
    }

    #region Gestion des Favoris

    /// <summary>
    /// Ajoute une annonce aux favoris
    /// </summary>
    public async Task<bool> AddToFavoritesAsync(Annonce annonce, string? listName = null, string? notes = null)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            // Vérifier si déjà en favoris
            if (await IsFavoriteAsync(annonce.Id))
            {
                return false;
            }

            var favorite = new Favorite
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                AnnonceId = annonce.Id,
                ListName = listName,
                Notes = notes,
                DateAdded = DateTime.Now,
                
                // Données de l'annonce pour éviter les requêtes multiples
                AnnonceTitle = annonce.Titre,
                AnnonceImageUrl = annonce.PhotosUrls?.FirstOrDefault() ?? "",
                AnnonceLocation = annonce.Localisation ?? "",
                AnnonceAuthorId = annonce.UtilisateurId,
                AnnonceAuthorName = "", // Sera mis à jour plus tard
                AnnonceCreatedAt = annonce.DateCreation,
                AnnonceType = annonce.Type,
                AnnonceCategory = annonce.Categorie
            };

            await _firebaseClient
                .Child(FAVORITES_COLLECTION)
                .Child(favorite.Id)
                .PutAsync(favorite);
                
            // Mettre à jour le cache local
            _userFavorites.Add(favorite);
            
            // Mettre à jour le compteur de la liste si spécifiée
            if (!string.IsNullOrEmpty(listName))
            {
                await UpdateListItemCountAsync(listName);
            }
            
            
            // Déclencher une notification locale
            await _notificationService.ShowMessageNotificationAsync(
                "DonTroc",
                $"'{annonce.Titre}' a été ajouté à vos favoris",
                "favorites"
            );

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de l'ajout aux favoris: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Supprime une annonce des favoris
    /// </summary>
    public async Task<bool> RemoveFromFavoritesAsync(string annonceId)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var favorite = _userFavorites.FirstOrDefault(f => f.AnnonceId == annonceId && f.UserId == userId);
            if (favorite == null)
                return false;

            await _firebaseClient
                .Child(FAVORITES_COLLECTION)
                .Child(favorite.Id)
                .DeleteAsync();
            
            // Mettre à jour le compteur de la liste
            if (!string.IsNullOrEmpty(favorite.ListName))
            {
                await UpdateListItemCountAsync(favorite.ListName);
            }
            
            // Supprimer du cache local
            _userFavorites.Remove(favorite);
            

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la suppression du favori: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Vérifie si une annonce est en favoris
    /// </summary>
    public async Task<bool> IsFavoriteAsync(string annonceId)
    {
        try
        {
            await LoadUserFavoritesAsync();
            var userId = _authService.GetUserId();
            return _userFavorites.Any(f => f.AnnonceId == annonceId && f.UserId == userId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Récupère tous les favoris de l'utilisateur
    /// </summary>
    public async Task<List<Favorite>> GetUserFavoritesAsync(string? listName = null)
    {
        try
        {
            await LoadUserFavoritesAsync();
            
            if (string.IsNullOrEmpty(listName))
                return _userFavorites.ToList();
            
            return _userFavorites.Where(f => f.ListName == listName).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la récupération des favoris: {ex.Message}");
            return new List<Favorite>();
        }
    }

    #endregion

    #region Gestion des Listes

    /// <summary>
    /// Crée une nouvelle liste personnalisée
    /// </summary>
    public async Task<bool> CreateListAsync(string name, string? description = null, string color = "#FF6B6B", string icon = "heart")
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var list = new FavoriteList
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = name,
                Description = description,
                Color = color,
                Icon = icon,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _firebaseClient
                .Child(FAVORITE_LISTS_COLLECTION)
                .Child(list.Id)
                .PutAsync(list);
            
            _userLists.Add(list);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la création de la liste: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Récupère toutes les listes de l'utilisateur
    /// </summary>
    public async Task<List<FavoriteList>> GetUserListsAsync()
    {
        try
        {
            await LoadUserListsAsync();
            return _userLists.ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la récupération des listes: {ex.Message}");
            return new List<FavoriteList>();
        }
    }

    /// <summary>
    /// Initialise les listes par défaut pour un nouvel utilisateur
    /// </summary>
    public async Task InitializeDefaultListsAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            var existingLists = await GetUserListsAsync();
            
            foreach (var defaultList in FavoriteList.DefaultLists.Values)
            {
                if (!existingLists.Any(l => l.Name == defaultList.Name))
                {
                    await CreateListAsync(defaultList.Name, defaultList.Description, defaultList.Color, defaultList.Icon);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de l'initialisation des listes par défaut: {ex.Message}");
        }
    }

    #endregion

    #region Gestion des Alertes

    /// <summary>
    /// Crée une nouvelle alerte
    /// </summary>
    public async Task<bool> CreateAlertAsync(string name, List<string> keywords, List<string> categories, 
        string? location = null, double? maxDistance = null, string annonceType = "both")
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var alert = new AnnonceAlert
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = name,
                Keywords = keywords,
                Categories = categories,
                Location = location,
                MaxDistance = maxDistance,
                AnnonceType = annonceType,
                CreatedAt = DateTime.Now
            };

            await _firebaseClient
                .Child(ALERTS_COLLECTION)
                .Child(alert.Id)
                .PutAsync(alert);
            
            _userAlerts.Add(alert);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la création de l'alerte: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Récupère toutes les alertes de l'utilisateur
    /// </summary>
    public async Task<List<AnnonceAlert>> GetUserAlertsAsync()
    {
        try
        {
            await LoadUserAlertsAsync();
            return _userAlerts.ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la récupération des alertes: {ex.Message}");
            return new List<AnnonceAlert>();
        }
    }

    /// <summary>
    /// Supprime une alerte
    /// </summary>
    public async Task<bool> DeleteAlertAsync(string alertId)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var alert = _userAlerts.FirstOrDefault(a => a.Id == alertId && a.UserId == userId);
            if (alert == null)
                return false;

            await _firebaseClient
                .Child(ALERTS_COLLECTION)
                .Child(alert.Id)
                .DeleteAsync();
            
            _userAlerts.Remove(alert);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la suppression de l'alerte: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Active ou désactive une alerte
    /// </summary>
    public async Task<bool> ToggleAlertAsync(string alertId, bool isActive)
    {
        try
        {
            var alert = _userAlerts.FirstOrDefault(a => a.Id == alertId);
            if (alert == null)
                return false;

            alert.IsActive = isActive;
            
            await _firebaseClient
                .Child(ALERTS_COLLECTION)
                .Child(alert.Id)
                .PutAsync(alert);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur toggle alerte: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Supprime une liste et tous ses favoris
    /// </summary>
    public async Task<bool> DeleteListAsync(string listId)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var list = _userLists.FirstOrDefault(l => l.Id == listId && l.UserId == userId);
            if (list == null)
                return false;

            var favoritesToDelete = _userFavorites.Where(f => f.ListName == list.Name).ToList();
            foreach (var favorite in favoritesToDelete)
            {
                await _firebaseClient
                    .Child(FAVORITES_COLLECTION)
                    .Child(favorite.Id)
                    .DeleteAsync();
                _userFavorites.Remove(favorite);
            }

            await _firebaseClient
                .Child(FAVORITE_LISTS_COLLECTION)
                .Child(list.Id)
                .DeleteAsync();
            
            _userLists.Remove(list);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur suppression liste: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Déplace un favori vers une autre liste
    /// </summary>
    public async Task<bool> MoveFavoriteToListAsync(string annonceId, string newListName)
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var favorite = _userFavorites.FirstOrDefault(f => f.AnnonceId == annonceId && f.UserId == userId);
            if (favorite == null)
                return false;

            // Mettre à jour le compteur de l'ancienne liste
            if (!string.IsNullOrEmpty(favorite.ListName))
            {
                await UpdateListItemCountAsync(favorite.ListName);
            }

            // Changer la liste
            favorite.ListName = newListName;
            
            await _firebaseClient
                .Child(FAVORITES_COLLECTION)
                .Child(favorite.Id)
                .PutAsync(favorite);
            
            // Mettre à jour le compteur de la nouvelle liste
            await UpdateListItemCountAsync(newListName);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors du déplacement du favori: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Méthodes privées

    /// <summary>
    /// Charge les favoris de l'utilisateur depuis Firebase
    /// </summary>
    private async Task LoadUserFavoritesAsync()
    {
        if (DateTime.Now - _lastCacheUpdate < _cacheValidDuration && _userFavorites.Any())
            return;

        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            var favorites = await _firebaseClient
                .Child(FAVORITES_COLLECTION)
                .OrderBy("UserId")
                .EqualTo(userId)
                .OnceAsync<Favorite>();

            _userFavorites = favorites.Select(f => f.Object).ToList();
            _lastCacheUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors du chargement des favoris: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les listes de l'utilisateur depuis Firebase
    /// </summary>
    private async Task LoadUserListsAsync()
    {
        if (DateTime.Now - _lastCacheUpdate < _cacheValidDuration && _userLists.Any())
            return;

        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            var lists = await _firebaseClient
                .Child(FAVORITE_LISTS_COLLECTION)
                .OrderBy("UserId")
                .EqualTo(userId)
                .OnceAsync<FavoriteList>();

            _userLists = lists.Select(l => l.Object).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors du chargement des listes: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les alertes de l'utilisateur depuis Firebase
    /// </summary>
    private async Task LoadUserAlertsAsync()
    {
        if (DateTime.Now - _lastCacheUpdate < _cacheValidDuration && _userAlerts.Any())
            return;

        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return;

            var alerts = await _firebaseClient
                .Child(ALERTS_COLLECTION)
                .OrderBy("UserId")
                .EqualTo(userId)
                .OnceAsync<AnnonceAlert>();

            _userAlerts = alerts.Select(a => a.Object).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors du chargement des alertes: {ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour le compteur d'éléments d'une liste
    /// </summary>
    private async Task UpdateListItemCountAsync(string listName)
    {
        try
        {
            var list = _userLists.FirstOrDefault(l => l.Name == listName);
            if (list == null)
                return;

            var count = _userFavorites.Count(f => f.ListName == listName);
            list.ItemCount = count;
            list.UpdatedAt = DateTime.Now;

            await _firebaseClient
                .Child(FAVORITE_LISTS_COLLECTION)
                .Child(list.Id)
                .PutAsync(list);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur lors de la mise à jour du compteur: {ex.Message}");
        }
    }

    /// <summary>
    /// Vérifie si une annonce correspond à une alerte
    /// </summary>
    private Task<bool> DoesAnnonceMatchAlertAsync(Annonce annonce, AnnonceAlert alert)
    {
        // Vérifier le type d'annonce
        if (alert.AnnonceType != "both" && alert.AnnonceType != annonce.Type)
            return Task.FromResult(false);

        // Vérifier les catégories
        if (alert.Categories.Any() && !alert.Categories.Contains(annonce.Categorie))
            return Task.FromResult(false);

        // Vérifier les mots-clés
        if (alert.Keywords.Any())
        {
            var annonceText = $"{annonce.Titre} {annonce.Description}".ToLower();
            var hasKeyword = alert.Keywords.Any(keyword => 
                annonceText.Contains(keyword.ToLower()));
            
            if (!hasKeyword)
                return Task.FromResult(false);
        }

        // Vérifier la localisation et distance (si spécifiées)
        if (!string.IsNullOrEmpty(alert.Location) && alert.MaxDistance.HasValue)
        {
            // Ici, vous pourriez implémenter une vérification de distance géographique
            // Pour l'instant, on fait une comparaison simple de chaîne
            if (string.IsNullOrEmpty(annonce.Localisation) || !annonce.Localisation.ToLower().Contains(alert.Location.ToLower()))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Déclenche une alerte pour une annonce correspondante
    /// </summary>
    private async Task TriggerAlertAsync(AnnonceAlert alert, Annonce annonce)
    {
        try
        {
            // Mettre à jour l'alerte
            alert.LastTriggered = DateTime.Now;
            alert.TriggerCount++;
            alert.MatchedAnnonceIds.Add(annonce.Id);

            await _firebaseClient
                .Child(ALERTS_COLLECTION)
                .Child(alert.Id)
                .PutAsync(alert);

            // Envoyer une notification
            if (alert.NotifyInApp)
            {
                await _notificationService.ShowMessageNotificationAsync(
                    $"Alerte: {alert.Name}",
                    $"Nouvelle annonce: {annonce.Titre} à {annonce.Localisation}",
                    "alerts"
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesService] Erreur déclenchement alerte: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Efface le cache local (utile lors de la déconnexion)
    /// </summary>
    public void ClearCache()
    {
        _userFavorites.Clear();
        _userLists.Clear();
        _userAlerts.Clear();
        _lastCacheUpdate = DateTime.MinValue;
    }

    public async Task AddFavoriteAsync(string annonceId, string? listName = null, string? notes = null)
    {
        // Cette méthode est redondante avec AddToFavoritesAsync
        // Elle est laissée ici pour compatibilité si nécessaire
        var annonce = new Annonce { Id = annonceId }; // Vous devriez récupérer l'annonce complète depuis votre source de données
        await AddToFavoritesAsync(annonce, listName, notes);
    }

    public async Task RemoveFavoriteAsync(string userId, string annonceId) // methodologie de suppression
    {
        await RemoveFromFavoritesAsync(annonceId);
    }
}

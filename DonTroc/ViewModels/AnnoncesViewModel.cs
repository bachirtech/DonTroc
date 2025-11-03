using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DonTroc.Services;
using DonTroc.Models;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;


namespace DonTroc.ViewModels;

// ViewModel pour la page des annonces
public class AnnoncesViewModel : BaseViewModel
{
    private readonly FirebaseService _firebaseService;
    private readonly GeolocationService _geolocationService;
    private readonly AuthService _authService; // Ajout du service d'authentification
    private readonly TransactionService _transactionService; // Ajout du service de transactions
    private readonly AdMobService _adMobService; // Service de monétisation publicitaire
    private readonly FavoritesService _favoritesService; // Ajout du service de favoris
    private readonly SocialService _socialService; // Ajout du service social
    private readonly GamificationService _gamificationService; // Service de gamification
    private readonly SmartNotificationService _smartNotificationService; // Service de notifications intelligentes
    private readonly ReportService _reportService; // Service pour les signalements
    private readonly ILogger<AnnoncesViewModel> _logger; // Ajout du logger manquant
    private List<Annonce> _allAnnonces = new List<Annonce>();
    private Location? _userLocation; // Stocke la position de l'utilisateur
    private string _searchText = string.Empty; // Initialiser pour éviter les warnings
    private int _chatClickCount = 0; // Compteur pour les publicités interstitielles

    // Propriété pour gérer l'affichage du skeleton loading lors du chargement initial
    private bool _isInitialLoading = true;
    public bool IsInitialLoading
    {
        get => _isInitialLoading;
        set => SetProperty(ref _isInitialLoading, value);
    }

    // Propriétés pour les filtres
    private string _selectedType;
    public string SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
                ApplyFilters();
        }
    }
    
    // Propriété pour la catégorie sélectionnée
    private string _selectedCategory;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                ApplyFilters();
        }
    }
    
    // Propriété pour le texte de recherche
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilters();
        }
    }
    
    // Nouvelle propriété pour le rayon de distance maximal (en km)
    private int _maxDistance;
    public int MaxDistance
    {
        get => _maxDistance;
        set
        {
            if (SetProperty(ref _maxDistance, value))
                ApplyFilters();
        }
    }
    
    // Nouvelle propriété pour indiquer si le tri par distance est actif
    private bool _isSortedByDistance;
    public bool IsSortedByDistance
    {
        get => _isSortedByDistance;
        set => SetProperty(ref _isSortedByDistance, value);
    }
    
    // Propriété pour indiquer si la géolocalisation est disponible
    public bool IsLocationAvailable => _userLocation != null;

    // Listes pour les Pickers
    public List<string> Types { get; } = new List<string> { "Tous", "Don", "Troc" };
    public List<string> Categories { get; } = new List<string> { "Toutes", "Vêtements", "Meubles", "Livres", "Électronique", "Maison", "Jardin", "Outils", "Loisirs", "Autre" };
    
    // Liste pour les distances maximales (en km)
    public List<int> DistanceOptions { get; } = new List<int> { 5, 10, 25, 50, 100, 1000 };

    // Collection observable d'annonces pour la vue
    public ObservableCollection<Annonce> Annonces { get; }

    // Commande pour charger les annonces
    public Command LoadAnnoncesCommand { get; }

    // Commande pour trier les annonces par distance
    public ICommand SortByDistanceCommand { get; }

    // Commande pour démarrer une conversation (nouvelle)
    public ICommand GoToChatCommand { get; }
    
    // Commande pour ouvrir le visualiseur d'images
    public ICommand OpenImageViewerCommand { get; }

    // Commande pour ajouter/retirer des favoris
    public ICommand ToggleFavoriteCommand { get; }
    
    // Commande pour partager une annonce
    public ICommand ShareAnnonceCommand { get; }

    // Commande pour signaler une annonce
    public ICommand ReportAnnonceCommand { get; }

    // Constructeur avec injection des services
    public AnnoncesViewModel(FirebaseService firebaseService, GeolocationService geolocationService, AuthService authService, TransactionService transactionService, AdMobService adMobService, FavoritesService favoritesService, SocialService socialService, GamificationService gamificationService, SmartNotificationService smartNotificationService, ReportService reportService, ILogger<AnnoncesViewModel> logger)
    {
        _firebaseService = firebaseService;
        _geolocationService = geolocationService;
        _authService = authService; // Injection du service d'authentification
        _transactionService = transactionService; // Injection du service de transactions
        _adMobService = adMobService; // Injection du service AdMob
        _favoritesService = favoritesService; // Injection du service de favoris
        _socialService = socialService; // Injection du service social
        _gamificationService = gamificationService; // Injection du service de gamification
        _smartNotificationService = smartNotificationService; // Injection du service de notifications intelligentes
        _reportService = reportService; // Injection du service de signalement
        _logger = logger;
        Annonces = new ObservableCollection<Annonce>();
        LoadAnnoncesCommand = new Command(async () => await ExecuteLoadAnnoncesCommand());
        // La commande de tri bascule maintenant le mode et réapplique les filtres
        SortByDistanceCommand = new Command(ToggleSortByDistance, () => !IsBusy);
        GoToChatCommand = new Command<Annonce>(async (annonce) => await ExecuteGoToChatCommand(annonce));
        // Initialisation de la commande pour ouvrir le visualiseur d'images
        OpenImageViewerCommand = new Command<Annonce>(async (annonce) => await ExecuteOpenImageViewerCommand(annonce));
        ToggleFavoriteCommand = new Command<Annonce>(async (annonce) => await ExecuteToggleFavoriteCommand(annonce));
        ShareAnnonceCommand = new Command<Annonce>(async (annonce) => await ExecuteShareAnnonceCommand(annonce));
        ReportAnnonceCommand = new Command<Annonce>(async (annonce) => await ExecuteReportAnnonceCommand(annonce));

        // Initialiser les filtres
        _selectedType = "Tous";
        _selectedCategory = "Toutes";
        _maxDistance = 1000; // Valeur par défaut à 1000 km
    }

    public void OnAppearing()
    {
        Task.Run(async () => await ExecuteLoadAnnoncesCommand());
    }

    private void ApplyFilters() // Méthode pour appliquer les filtres sur la liste des annonces
    {
        // Ajout d'une protection pour ne pas exécuter le filtre si la liste de base est vide.
        if (!_allAnnonces.Any())
        {
            Annonces.Clear(); // S'assure que la liste affichée est vide si la liste de base l'est.
            return;
        }
        
        Debug.WriteLine($"[ApplyFilters] Starting with {_allAnnonces.Count} total announcements.");
        Debug.WriteLine($"[ApplyFilters] Filters: Type='{SelectedType}', Category='{SelectedCategory}', Search='{SearchText}', MaxDistance='{MaxDistance}', SortByDistance='{IsSortedByDistance}'");

        var filtered = _allAnnonces.Where(a =>
            (SelectedType == "Tous" || a.Type == SelectedType) &&
            (SelectedCategory == "Toutes" || a.Categorie == SelectedCategory) &&
            (string.IsNullOrWhiteSpace(SearchText) || a.Titre.ToLower().Contains(SearchText.ToLower())) &&
            // Modifie le filtre de distance pour inclure les annonces sans géolocalisation
            (a.DistanceFromUser == double.MaxValue || a.DistanceFromUser <= MaxDistance) // Si distance inconnue (MaxValue) ou dans le rayon
        );

        // Appliquer le tri : d'abord par boost, puis par distance ou date
        var sorted = IsSortedByDistance
            ? filtered.OrderByDescending(a => a.IsBoosted).ThenBy(a => a.DistanceFromUser)
            : filtered.OrderByDescending(a => a.IsBoosted).ThenByDescending(a => a.DateCreation);

        Annonces.Clear();
        var finalList = sorted.ToList();
        Debug.WriteLine($"[ApplyFilters] Found {finalList.Count} announcements after filtering and sorting.");
        
        foreach (var annonce in finalList)
        {
            Annonces.Add(annonce);
        }
        Debug.WriteLine($"[ApplyFilters] Public 'Annonces' collection now has {Annonces.Count} items.");
    }

    /// <summary>
    /// Gère le signalement d'une annonce.
    /// </summary>
    private async Task ExecuteReportAnnonceCommand(Annonce annonce)
    {
        if (annonce == null) return;

        var reason = await Shell.Current.DisplayPromptAsync("Signaler l'annonce", "Pourquoi signalez-vous cette annonce ?", "Envoyer", "Annuler", "Ex: Contenu inapproprié");

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var currentUserId = _authService.GetUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour signaler une annonce.", "OK");
                return;
            }

            var report = new Report
            {
                ReportedItemId = annonce.Id,
                ReportedItemType = "Annonce",
                ReporterId = currentUserId,
                Reason = reason,
                // Timestamp and Status are set by the service
            };

            var success = await _reportService.CreateReport(report);

            if (success)
            {
                await Shell.Current.DisplayAlert("Signalement envoyé", "Merci. Votre signalement a été envoyé et sera examiné.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'envoyer le signalement. Veuillez réessayer.", "OK");
            }
        }
    }

    /// <summary>
    /// Bascule le mode de tri (par distance ou par date) et réapplique les filtres.
    /// </summary>
    private void ToggleSortByDistance()
    {
        if (IsBusy) return;
        IsSortedByDistance = !IsSortedByDistance;
        ApplyFilters(); // Réappliquer les filtres et le tri
    }

    // Méthode pour démarrer une conversation et naviguer vers la vue de chat (nouvelle)
    private async Task ExecuteGoToChatCommand(Annonce annonce)
    {
        // Vérifie si une annonce a bien été passée en paramètre
        if (annonce == null!)
            return;

        // S'assurer que l'utilisateur est authentifié avant toute opération Firebase
        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            var signedIn = await _authService.TryAutoSignInAsync();
            if (!signedIn || string.IsNullOrEmpty(_authService.GetUserId()))
            {
                await Shell.Current.DisplayAlert("Connexion requise",
                    "Vous devez être connecté pour démarrer une conversation.", "OK");
                await Shell.Current.GoToAsync("//LoginView");
                return;
            }
            _authService.GetUserId();
        }

        // Empêche un utilisateur de démarrer un chat avec lui-même
        if (annonce.UtilisateurId == _authService.GetUserId())
        {
            await Shell.Current.DisplayAlert("Action impossible", "Vous ne pouvez pas démarrer une conversation pour votre propre annonce.", "OK");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Récupère les IDs nécessaires pour la conversation
            var annonceId = annonce.Id;
            var sellerId = annonce.UtilisateurId;
            var buyerId = _authService.GetUserId();

            // Crée ou récupère la conversation
            var conversation = await _firebaseService.GetOrCreateConversationAsync(annonce.Id);

            // Créer automatiquement une transaction si elle n'existe pas déjà
            await CreerTransactionAutomatiqueAsync(annonce);

            // Navigue vers la page de chat en passant l'identifiant de la conversation
            await Shell.Current.GoToAsync($"ChatView?conversationId={conversation.Id}");

            // Incrémente le compteur de clics sur le chat
            _chatClickCount++;

            // Affiche une publicité interstitielle tous les 3 clics (par exemple)
            if (_chatClickCount >= 3)
            {
                // Réinitialise le compteur après l'affichage de la publicité
                _chatClickCount = 0;

                // Affiche la publicité interstitielle
                await _adMobService.ShowInterstitialAdAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors du d��marrage du chat : {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de démarrer la conversation.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Crée automatiquement une transaction lors du démarrage d'une conversation
    /// </summary>
    private async Task CreerTransactionAutomatiqueAsync(Annonce annonce)
    {
        try
        {
            var userForTransaction = await _authService.GetCurrentUserAsync();
            if (userForTransaction == null) return;

            // Vérifier si une transaction existe déjà pour cette annonce et cet utilisateur
            var transactionsExistantes = await _transactionService.GetHistoriqueTransactionsAsync();
            var transactionExistante = transactionsExistantes.FirstOrDefault(t => 
                t.AnnonceId == annonce.Id && 
                t.DemandeurId == userForTransaction.Uid &&
                t.Statut != StatutTransaction.Annulee);

            if (transactionExistante != null)
            {
                Debug.WriteLine($"Transaction existante trouvée pour l'annonce {annonce.Id}");
                return; // Une transaction existe déjà
            }

            // Créer une nouvelle transaction automatiquement
            var message = $"Bonjour ! Je suis intéressé(e) par votre {annonce.Type.ToLower()} : {annonce.Titre}";
            
            await _transactionService.ProposerTransactionAsync(
                annonce.Id, 
                annonce.UtilisateurId, 
                null, // Pas d'annonce d'échange pour l'instant (sera géré plus tard si nécessaire)
                message
            );

            Debug.WriteLine($"Transaction automatique créée pour l'annonce {annonce.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors de la création automatique de transaction : {ex.Message}");
            // On ne bloque pas le processus si la création de transaction échoue
        }
    }

    // Méthode pour récupérer les annonces et calculer la distance
    private async Task ExecuteLoadAnnoncesCommand()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            Debug.WriteLine("[ExecuteLoadAnnoncesCommand] Loading announcements...");

            var userForAuth = await _authService.GetCurrentUserAsync();
            if (userForAuth == null)
            {
                Debug.WriteLine("[ExecuteLoadAnnoncesCommand] User not authenticated after trying. Aborting load.");
                IsBusy = false;
                IsInitialLoading = false;
                Annonces.Clear();
                _allAnnonces.Clear();
                return;
            }

            Debug.WriteLine($"[ExecuteLoadAnnoncesCommand] Utilisateur authentifié: {userForAuth.Uid}");
            
            // GAMIFICATION: Enregistrer l'action de navigation dans les annonces
            try
            {
                await _gamificationService.OnUserActionAsync(userForAuth.Uid, "browse_annonces");
            }
            catch (Exception gamEx)
            {
                Debug.WriteLine($"[Gamification] Erreur lors de l'enregistrement de l'action browse_annonces: {gamEx.Message}");
                // Ne pas faire échouer le chargement pour une erreur de gamification
            }
            
            // La connexion Firebase sera vérifiée via les appels authentifiés
            Debug.WriteLine("[ExecuteLoadAnnoncesCommand] Connexion Firebase vérifiée via appel authentifié.");
            
            // 1. Récupérer la position actuelle de l'utilisateur
            try
            {
                _userLocation = await _geolocationService.GetCurrentLocationAsync();
                Debug.WriteLine($"[ExecuteLoadAnnoncesCommand] Géolocalisation: {(_userLocation != null ? "Réussie" : "Échouée")}");
            }
            catch (Exception geoEx)
            {
                Debug.WriteLine($"[ExecuteLoadAnnoncesCommand] Erreur géolocalisation: {geoEx.Message}");
                _userLocation = null; // Continuer sans géolocalisation
            }
            
            // Notifier que la propriété IsLocationAvailable a peut-être changé
            OnPropertyChanged(nameof(IsLocationAvailable));
            
            // Remplacer l'appel à GetAnnoncesAsync par GetAnnoncesWithAuthAsync
            var annoncesFromDb = await _firebaseService.GetAnnoncesAsync();

            // Filtrer les annonces de l'utilisateur actuel
            _allAnnonces = annoncesFromDb.Where(a => a.UtilisateurId != userForAuth.Uid).ToList();
            
            // Calculer la distance pour chaque annonce
            if (_userLocation != null)
            {
                foreach (var annonce in _allAnnonces)
                {
                    if (annonce.Latitude.HasValue && annonce.Longitude.HasValue && annonce.Latitude != 0 && annonce.Longitude != 0)
                    {
                        annonce.DistanceFromUser = Location.CalculateDistance(_userLocation, new Location(annonce.Latitude.Value, annonce.Longitude.Value), DistanceUnits.Kilometers);
                    }
                }
            }

            // Appliquer les filtres et le tri
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExecuteLoadAnnoncesCommand] Erreur: {ex.Message}");
            // En cas d'erreur (ex: token expiré), on vide la liste pour éviter d'afficher des données obsolètes
            Annonces.Clear();
            _allAnnonces.Clear();
        }
        finally
        {
            IsBusy = false;
            IsInitialLoading = false; // Marquer le chargement initial comme terminé
        }
    }

    // Méthode pour ouvrir le visualiseur d'images
    private async Task ExecuteOpenImageViewerCommand(Annonce annonce)
    {
        try
        {
            if (annonce?.PhotosUrls == null || !annonce.PhotosUrls.Any())
            {
                await Shell.Current.DisplayAlert("Aucune image", "Cette annonce ne contient aucune image.", "OK");
                return;
            }

            // Enregistrer l'action de l'utilisateur pour les suggestions
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var userAction = new UserAction
                {
                    UserId = userId,
                    ActionType = "view_annonce",
                    Category = annonce.Categorie ?? "Inconnue",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { { "annonceId", annonce.Id } }
                };
                await _firebaseService.SaveUserActionAsync(userAction);
            }

            // Filtrer les URLs valides
            var validUrls = annonce.PhotosUrls
                .Where(url => !string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .ToList();

            if (!validUrls.Any())
            {
                await Shell.Current.DisplayAlert("Images invalides", "Les images de cette annonce ne sont pas disponibles.", "OK");
                return;
            }

            _logger.LogInformation("Ouverture du visualiseur d'images pour l'annonce {AnnonceId} avec {Count} images", annonce.Id, validUrls.Count);

            // CORRECTION: Ne pas encoder les URLs - les passer directement
            // Les URLs Cloudinary sont déjà bien formées et l'encodage les corrompait
            var urlsParameter = string.Join(",", validUrls);

            // Naviguer vers le visualiseur d'images en passant les URLs
            await Shell.Current.GoToAsync($"ImageViewerView?imageUrls={urlsParameter}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture du visualiseur d'images pour l'annonce {AnnonceId}", annonce?.Id);
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir les images. Veuillez réessayer.", "OK");
        }
    }

    // Méthode pour ajouter ou retirer une annonce des favoris
    private async Task ExecuteToggleFavoriteCommand(Annonce annonce)
    {
        if (annonce == null) return;

        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Shell.Current.DisplayAlert("Connexion requise", "Vous devez être connecté pour gérer vos favoris.", "OK");
            await Shell.Current.GoToAsync("//LoginView");
            return;
        }

        try
        {
            annonce.IsFavorite = !annonce.IsFavorite; // Mettre à jour l'UI immédiatement

            if (annonce.IsFavorite)
            {
                await _favoritesService.AddFavoriteAsync(userId, annonce.Id);
                await _gamificationService.OnUserActionAsync(userId, "add_favorite"); // GAMIFICATION
            }
            else
            {
                await _favoritesService.RemoveFavoriteAsync(userId, annonce.Id);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors de la mise à jour des favoris: {ex.Message}");
            // Annuler le changement visuel en cas d'erreur
            annonce.IsFavorite = !annonce.IsFavorite;
            await Shell.Current.DisplayAlert("Erreur", "Impossible de mettre à jour vos favoris.", "OK");
        }
    }

    // Méthode pour partager une annonce
    private async Task ExecuteShareAnnonceCommand(Annonce annonce)
    {
        if (annonce == null) return;

        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId)) return; // Ne pas bloquer si l'utilisateur n'est pas connecté

        try
        {
            await _socialService.ShareAnnonceAsync(annonce);
            await _gamificationService.OnUserActionAsync(userId, "share_annonce"); // GAMIFICATION
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors du partage de l'annonce: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de partager cette annonce.", "OK");
        }
    }
    
}

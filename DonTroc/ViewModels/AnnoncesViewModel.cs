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
    private readonly GlobalNotificationService _globalNotificationService; // Service de notifications en temps réel
    private readonly ILogger<AnnoncesViewModel> _logger; // Ajout du logger manquant
    private List<Annonce> _allAnnonces = new List<Annonce>();
    private Location? _userLocation; // Stocke la position de l'utilisateur
    private string _searchText = string.Empty; // Initialiser pour éviter les warnings
    private string? _currentUserId; // ID de l'utilisateur connecté pour filtrage

    // ── Premium : pub récompensée pour voir plus d'annonces ──
    /// <summary>Nombre d'annonces visibles gratuitement avant le mur publicitaire</summary>
    private const int FreeAnnonceLimit = 6;
    
    private bool _hasUnlockedPremiumAnnonces;
    /// <summary>Indique si l'utilisateur a débloqué les annonces premium (via rewarded ad)</summary>
    public bool HasUnlockedPremiumAnnonces
    {
        get => _hasUnlockedPremiumAnnonces;
        set
        {
            if (SetProperty(ref _hasUnlockedPremiumAnnonces, value))
            {
                OnPropertyChanged(nameof(ShowPremiumBanner));
                OnPropertyChanged(nameof(HiddenAnnoncesCount));
            }
        }
    }
    
    private int _totalFilteredCount;
    /// <summary>Nombre total d'annonces filtrées (avant limitation)</summary>
    public int TotalFilteredCount
    {
        get => _totalFilteredCount;
        set
        {
            if (SetProperty(ref _totalFilteredCount, value))
            {
                OnPropertyChanged(nameof(ShowPremiumBanner));
                OnPropertyChanged(nameof(HiddenAnnoncesCount));
            }
        }
    }
    
    /// <summary>Nombre d'annonces cachées derrière le mur publicitaire</summary>
    public int HiddenAnnoncesCount => Math.Max(0, TotalFilteredCount - FreeAnnonceLimit);
    
    /// <summary>Afficher le bandeau premium si il y a plus d'annonces et pas encore débloqué</summary>
    public bool ShowPremiumBanner => !HasUnlockedPremiumAnnonces && TotalFilteredCount > FreeAnnonceLimit;
    
    private bool _isRewardedAdReady;
    /// <summary>Indique si la pub récompensée est prête (pour l'état du bouton)</summary>
    public bool IsRewardedAdReady
    {
        get => _isRewardedAdReady;
        set => SetProperty(ref _isRewardedAdReady, value);
    }
 
    
    
    
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
    
    // Liste pour les distances maximales (en km) - Rayon géolocalisé pour les annonces
    public List<int> DistanceOptions { get; } = new List<int> { 5, 10, 20, 30, 40, 50 };

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
    
    // Commande pour débloquer les annonces premium via pub récompensée
    public ICommand UnlockPremiumAnnoncesCommand { get; }
    
    // Commande pour naviguer vers le détail d'une annonce
    public ICommand NavigateToDetailCommand { get; }

    // Constructeur avec injection des services
    public AnnoncesViewModel(FirebaseService firebaseService, GeolocationService geolocationService, AuthService authService, TransactionService transactionService, AdMobService adMobService, FavoritesService favoritesService, SocialService socialService, GamificationService gamificationService, SmartNotificationService smartNotificationService, ReportService reportService, GlobalNotificationService globalNotificationService, ILogger<AnnoncesViewModel> logger)
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
        _globalNotificationService = globalNotificationService; // Injection du service de notifications en temps réel
        _logger = logger;
        Annonces = new ObservableCollection<Annonce>();
        LoadAnnoncesCommand = new Command(() => SafeExecuteAsync(ExecuteLoadAnnoncesCommand));
        // La commande de tri bascule maintenant le mode et réapplique les filtres
        SortByDistanceCommand = new Command(ToggleSortByDistance, () => !IsBusy);
        GoToChatCommand = new Command<Annonce>((annonce) => SafeExecuteAsync(() => ExecuteGoToChatCommand(annonce)));
        // Initialisation de la commande pour ouvrir le visualiseur d'images
        OpenImageViewerCommand = new Command<Annonce>((annonce) => SafeExecuteAsync(() => ExecuteOpenImageViewerCommand(annonce)));
        ToggleFavoriteCommand = new Command<Annonce>((annonce) => SafeExecuteAsync(() => ExecuteToggleFavoriteCommand(annonce)));
        ShareAnnonceCommand = new Command<Annonce>((annonce) => SafeExecuteAsync(() => ExecuteShareAnnonceCommand(annonce)));
        ReportAnnonceCommand = new Command<Annonce>((annonce) => SafeExecuteAsync(() => ExecuteReportAnnonceCommand(annonce)));
        UnlockPremiumAnnoncesCommand = new Command(() => SafeExecuteAsync(ExecuteUnlockPremiumAnnoncesAsync));
        NavigateToDetailCommand = new Command<Annonce>((annonce) => SafeExecuteAsync(() => ExecuteNavigateToDetailAsync(annonce)));

        // Initialiser les filtres
        _selectedType = "Tous";
        _selectedCategory = "Toutes";
        _maxDistance = 50; // Valeur par défaut: rayon de 50 km
    }

    /// <summary>
    /// Exécute une tâche async de manière sécurisée en capturant les exceptions
    /// </summary>
    private async void SafeExecuteAsync(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SafeExecuteAsync] {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("Erreur", "Une erreur inattendue s'est produite.", "OK");
            });
        }
    }

    public void OnAppearing()
    {
        // Réinitialiser le déblocage premium à chaque visite de la page
        HasUnlockedPremiumAnnonces = false;
        SafeExecuteAsync(ExecuteLoadAnnoncesCommand);
    }

    /// <summary>
    /// Affiche une pub récompensée pour débloquer toutes les annonces premium.
    /// </summary>
    private async Task ExecuteUnlockPremiumAnnoncesAsync()
    {
        try
        {
            if (!_adMobService.IsRewardedAdReady())
            {
                await Shell.Current.DisplayAlert(
                    "Publicité non disponible",
                    "La publicité n'est pas encore prête. Veuillez patienter quelques secondes et réessayer.",
                    "OK");
                    
                // Recharger la pub en arrière-plan
                _adMobService.LoadRewardedAd();
                return;
            }

            Debug.WriteLine("[Premium] 🎬 Lancement pub récompensée pour débloquer les annonces");

            // ✨ PHASE 4 : teaser pré-pub (augmente le completion rate)
            await Services.AnimationService.ShowPreRewardedTeaserAsync("Préparation de votre déblocage...");

            var result = await _adMobService.ShowRewardedAdAsync();
            
            if (result)
            {
                // L'utilisateur a regardé la pub entièrement → débloquer
                HasUnlockedPremiumAnnonces = true;
                ApplyFilters(); // Réappliquer les filtres pour afficher toutes les annonces
                
                Debug.WriteLine($"[Premium] ✅ Annonces premium débloquées ! {HiddenAnnoncesCount} annonces supplémentaires");

                // 🎉 Animation post-pub : célébration + count-up des annonces débloquées
                _ = Services.AnimationService.ShowRewardEarnedAsync(
                    "Annonces débloquées !",
                    TotalFilteredCount,
                    suffix: " annonces");
            }
            else
            {
                Debug.WriteLine("[Premium] ❌ Pub récompensée non complétée");
                await Shell.Current.DisplayAlert(
                    "Publicité non complétée",
                    "Vous devez regarder la publicité en entier pour débloquer les annonces supplémentaires.",
                    "Compris");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Premium] Erreur: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'afficher la publicité. Veuillez réessayer.", "OK");
        }
    }

    private void ApplyFilters() // Méthode pour appliquer les filtres sur la liste des annonces
    {
        // Ajout d'une protection pour ne pas exécuter le filtre si la liste de base est vide.
        if (!_allAnnonces.Any())
        {
            Annonces.Clear(); // S'assure que la liste affichée est vide si la liste de base l'est.
            return;
        }
        

        var filtered = _allAnnonces.Where(a =>
            // Double sécurité : exclure les annonces de l'utilisateur connecté
            (string.IsNullOrEmpty(_currentUserId) || !string.Equals(a.UtilisateurId?.Trim(), _currentUserId.Trim(), StringComparison.Ordinal)) &&
            (SelectedType == "Tous" || a.Type == SelectedType) &&
            (SelectedCategory == "Toutes" || a.Categorie == SelectedCategory) &&
            (string.IsNullOrWhiteSpace(SearchText) || a.Titre.ToLower().Contains(SearchText.ToLower())) &&
            // Filtre de distance:
            // - Si position utilisateur non disponible, afficher toutes les annonces
            // - Sinon, afficher seulement les annonces dans le rayon MaxDistance
            // - Les annonces sans coordonnées (DistanceFromUser == MaxValue) sont exclues si position dispo
            (!IsLocationAvailable || (a.DistanceFromUser != double.MaxValue && a.DistanceFromUser <= MaxDistance))
        );

        // Appliquer le tri : d'abord par boost, puis par distance ou date
        IEnumerable<Annonce> sorted;
        if (IsSortedByDistance)
        {
            // Trier par distance, mais mettre les annonces sans coordonnées à la fin
            sorted = filtered
                .OrderByDescending(a => a.IsBoosted)
                .ThenBy(a => a.DistanceFromUser == double.MaxValue ? 1 : 0) // Annonces avec distance connue d'abord
                .ThenBy(a => a.DistanceFromUser == double.MaxValue ? 0 : a.DistanceFromUser); // Puis par distance
        }
        else
        {
            sorted = filtered.OrderByDescending(a => a.IsBoosted).ThenByDescending(a => a.DateCreation);
        }

        Annonces.Clear();
        var finalList = sorted.ToList();
        TotalFilteredCount = finalList.Count;
        Debug.WriteLine($"[ApplyFilters] {_allAnnonces.Count} → {finalList.Count} résultats (Premium: {HasUnlockedPremiumAnnonces})");
        
        // Limiter les annonces visibles si l'utilisateur n'a pas débloqué le premium
        var visibleList = HasUnlockedPremiumAnnonces ? finalList : finalList.Take(FreeAnnonceLimit).ToList();
        
        foreach (var annonce in visibleList)
        {
            Annonces.Add(annonce);
        }
        
        // Mettre à jour l'état de la pub récompensée
        IsRewardedAdReady = _adMobService.IsRewardedAdReady();
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
        {
            return;
        }

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
            userId = _authService.GetUserId();
        }

        // Empêche un utilisateur de démarrer un chat avec lui-même
        if (annonce.UtilisateurId == _authService.GetUserId())
        {
            await Shell.Current.DisplayAlert("Action impossible", "Vous ne pouvez pas démarrer une conversation pour votre propre annonce.", "OK");
            return;
        }

        // Vérifier que le GPS est activé avant de permettre le contact
        if (!IsLocationAvailable)
        {
            // Tenter de récupérer la position maintenant
            try
            {
                _userLocation = await _geolocationService.GetCurrentLocationAsync();
                OnPropertyChanged(nameof(IsLocationAvailable));
            }
            catch (Exception geoEx)
            {
                Debug.WriteLine($"[Chat] Géoloc échouée: {geoEx.Message}");
                _userLocation = null;
            }

            if (!IsLocationAvailable)
            {
                await Shell.Current.DisplayAlert("GPS requis 📍",
                    "Vous devez activer votre GPS pour pouvoir contacter un annonceur.\n\n" +
                    "Cela permet de :\n" +
                    "• Vérifier que vous êtes à proximité\n" +
                    "• Garantir des échanges locaux\n" +
                    "• Assurer la sécurité des utilisateurs\n\n" +
                    "Veuillez activer le GPS dans les paramètres de votre appareil et réessayer.",
                    "Compris");
                return;
            }
        }

        // 🌍 Vérification du rayon local (50 km max) — empêche les contacts longue distance
        // (ex: utilisateur Maroc ↔ annonce France). Garantit des échanges locaux.
        if (annonce.Latitude.HasValue && annonce.Longitude.HasValue && _userLocation != null)
        {
            var distanceKm = _geolocationService.CalculateDistance(
                _userLocation.Latitude, _userLocation.Longitude,
                annonce.Latitude.Value, annonce.Longitude.Value);

            if (distanceKm > Services.RecommendationService.MAX_LOCAL_RADIUS_KM)
            {
                await Shell.Current.DisplayAlert("📍 Annonce trop éloignée",
                    $"Cette annonce se trouve à environ {distanceKm:0} km de vous.\n\n" +
                    $"DonTroc limite les contacts aux annonces situées dans un rayon de " +
                    $"{Services.RecommendationService.MAX_LOCAL_RADIUS_KM:0} km pour favoriser " +
                    "des échanges locaux et sécurisés.",
                    "Compris");
                return;
            }
        }

        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            // Afficher un interstitiel AVANT la navigation vers le chat
            // L'utilisateur voit la pub puis est redirigé vers la conversation
            await _adMobService.ShowInterstitialAfterActionAsync("ChatContact");

            // Crée ou récupère la conversation
            var conversation = await _firebaseService.GetOrCreateConversationAsync(annonce.Id);
            
            // Vérifier que la conversation a un ID valide
            if (conversation == null || string.IsNullOrEmpty(conversation.Id))
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible de créer la conversation. Veuillez réessayer.", "OK");
                return;
            }

            // Ajouter la conversation au service de notifications en temps réel
            _globalNotificationService.AddConversation(conversation.Id);

            // Créer automatiquement une transaction si elle n'existe pas déjà
            await CreerTransactionAutomatiqueAsync(annonce);

            // Navigue vers la page de chat en passant l'identifiant de la conversation
            var encodedConversationId = Uri.EscapeDataString(conversation.Id);
            await Shell.Current.GoToAsync($"ChatView?conversationId={encodedConversationId}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Chat] Erreur: {ex.Message}");
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
                return; // Une transaction existe déjà
            }

            // Créer une nouvelle transaction automatiquement
            var message = $"Bonjour ! Je suis intéressé(e) par votre {annonce.Type.ToLower()} : {annonce.Titre}";
            
            await _transactionService.ProposerTransactionAsync(
                annonce.Id, 
                annonce.UtilisateurId, 
                null,
                message
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Transaction] Erreur auto: {ex.Message}");
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

            var userForAuth = await _authService.GetCurrentUserAsync();
            if (userForAuth == null)
            {
                IsBusy = false;
                IsInitialLoading = false;
                Annonces.Clear();
                _allAnnonces.Clear();
                return;
            }

            _currentUserId = userForAuth.Uid;
            
            // GAMIFICATION
            try
            {
                await _gamificationService.OnUserActionAsync(userForAuth.Uid, "browse_annonces");
            }
            catch (Exception gamEx)
            {
                Debug.WriteLine($"[Gamification] Erreur browse_annonces: {gamEx.Message}");
            }
            
            // Récupérer la position
            try
            {
                _userLocation = await _geolocationService.GetCurrentLocationAsync();
            }
            catch (Exception geoEx)
            {
                Debug.WriteLine($"[Annonces] Géoloc échouée: {geoEx.Message}");
                _userLocation = null;
            }
            
            // Notifier que la propriété IsLocationAvailable a peut-être changé
            OnPropertyChanged(nameof(IsLocationAvailable));
            
            // Remplacer l'appel à GetAnnoncesAsync par GetAnnoncesWithAuthAsync
            var annoncesFromDb = await _firebaseService.GetAnnoncesAsync();

            // Filtrer les annonces de l'utilisateur actuel
            _allAnnonces = annoncesFromDb
                .Where(a => !string.IsNullOrEmpty(a.UtilisateurId) && 
                            !string.Equals(a.UtilisateurId.Trim(), userForAuth.Uid.Trim(), StringComparison.Ordinal))
                .ToList();
            
            // Calculer la distance pour chaque annonce
            if (_userLocation != null)
            {
                foreach (var annonce in _allAnnonces)
                {
                    if (annonce.Latitude.HasValue && annonce.Longitude.HasValue && annonce.Latitude != 0 && annonce.Longitude != 0)
                    {
                        annonce.DistanceFromUser = Location.CalculateDistance(_userLocation, new Location(annonce.Latitude.Value, annonce.Longitude.Value), DistanceUnits.Kilometers);
                    }
                    else
                    {
                        annonce.DistanceFromUser = double.MaxValue;
                    }
                }
            }
            else
            {
                foreach (var annonce in _allAnnonces)
                {
                    annonce.DistanceFromUser = double.MaxValue;
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

            // Enregistrer l'action de l'utilisateur pour les suggestions (en arrière-plan)
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                _ = Task.Run(async () =>
                {
                    try
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
                    catch { }
                });
            }

            // Filtrer les URLs valides
            var validUrls = annonce.PhotosUrls
                .Where(url => !string.IsNullOrEmpty(url) && 
                              (Uri.IsWellFormedUriString(url, UriKind.Absolute) || url.StartsWith("http")))
                .ToList();

            if (!validUrls.Any())
            {
                await Shell.Current.DisplayAlert("Images invalides", "Les images de cette annonce ne sont pas disponibles.", "OK");
                return;
            }

            // Joindre les URLs avec le séparateur pipe
            var urlsParameter = string.Join("|", validUrls);

            // Naviguer vers le visualiseur d'images avec un dictionnaire de paramètres
            var navigationParameters = new Dictionary<string, object>
            {
                { "imageUrls", urlsParameter }
            };
            await Shell.Current.GoToAsync("ImageViewerView", navigationParameters);
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
            annonce.IsFavorite = !annonce.IsFavorite;

            if (annonce.IsFavorite)
            {
                await _favoritesService.AddToFavoritesAsync(annonce);
                await _gamificationService.OnUserActionAsync(userId, "add_favorite");
                _ = Services.AnimationService.ShowToastAsync("Ajouté à vos favoris ❤️", Services.AnimationService.ToastType.Success, 1800);
            }
            else
            {
                await _favoritesService.RemoveFavoriteAsync(userId, annonce.Id);
                _ = Services.AnimationService.ShowToastAsync("Retiré de vos favoris", Services.AnimationService.ToastType.Info, 1500);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Favoris] Erreur: {ex.Message}");
            annonce.IsFavorite = !annonce.IsFavorite;
            _ = Services.AnimationService.ShowToastAsync("Impossible de mettre à jour vos favoris", Services.AnimationService.ToastType.Error);
        }
    }

    // Méthode pour partager une annonce
    private async Task ExecuteShareAnnonceCommand(Annonce annonce)
    {
        if (annonce == null) return;

        try
        {
            await _socialService.ShareAnnonceAsync(annonce);
            
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _gamificationService.OnUserActionAsync(userId, "share_annonce");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Partage] Erreur: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de partager cette annonce.", "OK");
        }
    }

    /// <summary>
    /// Navigue vers la page de détail d'une annonce.
    /// </summary>
    private async Task ExecuteNavigateToDetailAsync(Annonce annonce)
    {
        if (annonce == null) return;

        try
        {
            await Shell.Current.GoToAsync($"AnnonceDetailView?annonceId={annonce.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NavigateToDetail] Erreur: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir le détail de l'annonce.", "OK");
        }
    }
    
}

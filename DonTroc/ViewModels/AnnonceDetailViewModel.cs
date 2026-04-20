using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour la page de détail d'une annonce.
/// Charge l'annonce, le profil du publieur et les annonces similaires.
/// </summary>
public class AnnonceDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly FavoritesService _favoritesService;
    private readonly SocialService _socialService;
    private readonly ReportService _reportService;
    private readonly AdMobService _adMobService;
    private readonly GamificationService _gamificationService;
    private readonly GeolocationService _geolocationService;
    private readonly TransactionService _transactionService;
    private readonly GlobalNotificationService _globalNotificationService;
    private readonly ILogger<AnnonceDetailViewModel> _logger;

    // ── Propriétés bindées ──

    private Annonce? _annonce;
    public Annonce? Annonce
    {
        get => _annonce;
        set => SetProperty(ref _annonce, value);
    }

    private UserProfile? _publisherProfile;
    public UserProfile? PublisherProfile
    {
        get => _publisherProfile;
        set
        {
            if (SetProperty(ref _publisherProfile, value))
            {
                OnPropertyChanged(nameof(HasPublisherProfile));
                OnPropertyChanged(nameof(PublisherRatingStars));
                OnPropertyChanged(nameof(PublisherMemberSince));
            }
        }
    }

    public bool HasPublisherProfile => PublisherProfile != null;

    public string PublisherRatingStars
    {
        get
        {
            if (PublisherProfile == null || PublisherProfile.NombreEvaluations == 0)
                return "Pas encore évalué";
            var stars = PublisherProfile.NoteMoyenne;
            var fullStars = (int)stars;
            var result = new string('⭐', fullStars);
            if (stars - fullStars >= 0.5) result += "½";
            return $"{result} ({PublisherProfile.NoteMoyenne:F1}/5 · {PublisherProfile.NombreEvaluations} avis)";
        }
    }

    public string PublisherMemberSince
    {
        get
        {
            if (PublisherProfile == null) return "";
            var days = (DateTime.UtcNow - PublisherProfile.DateInscription).Days;
            if (days < 30) return $"Membre depuis {days} jour(s)";
            if (days < 365) return $"Membre depuis {days / 30} mois";
            return $"Membre depuis {days / 365} an(s)";
        }
    }

    private bool _isOwner;
    public bool IsOwner
    {
        get => _isOwner;
        set
        {
            if (SetProperty(ref _isOwner, value))
            {
                OnPropertyChanged(nameof(IsNotOwner));
                OnPropertyChanged(nameof(CanProposeTrade));
            }
        }
    }
    public bool IsNotOwner => !IsOwner;

    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private int _currentPhotoIndex;
    public int CurrentPhotoIndex
    {
        get => _currentPhotoIndex;
        set
        {
            if (SetProperty(ref _currentPhotoIndex, value))
                OnPropertyChanged(nameof(PhotoIndicatorText));
        }
    }

    public string PhotoIndicatorText =>
        Annonce?.PhotosCount > 0
            ? $"{CurrentPhotoIndex + 1}/{Annonce.PhotosCount}"
            : "0/0";

    public string DateCreationFormatted =>
        Annonce != null ? Annonce.DateCreation.ToString("dd MMMM yyyy") : "";

    public string StatutFormatted =>
        Annonce?.GetStatutText() ?? "Inconnu";

    public bool HasTrocDetails =>
        Annonce?.Type == "Troc" &&
        (!string.IsNullOrEmpty(Annonce.DescriptionRecherche) ||
         (Annonce.CategoriesRecherchees?.Any() ?? false));

    /// <summary>
    /// Indique si l'utilisateur courant peut proposer un troc sur cette annonce.
    /// Visible uniquement pour les annonces de type Troc, non-propriétaire, disponibles.
    /// </summary>
    public bool CanProposeTrade =>
        IsNotOwner &&
        Annonce?.Type == "Troc" &&
        Annonce?.Statut == StatutAnnonce.Disponible;

    public bool HasLocation =>
        Annonce != null && (!string.IsNullOrEmpty(Annonce.Localisation) ||
                            !string.IsNullOrEmpty(Annonce.Ville) ||
                            !string.IsNullOrEmpty(Annonce.AdresseComplete));

    public string LocationText
    {
        get
        {
            if (Annonce == null) return "";
            if (!string.IsNullOrEmpty(Annonce.AdresseComplete)) return Annonce.AdresseComplete;
            if (!string.IsNullOrEmpty(Annonce.Ville))
            {
                return !string.IsNullOrEmpty(Annonce.CodePostal)
                    ? $"{Annonce.Ville} ({Annonce.CodePostal})"
                    : Annonce.Ville;
            }
            return Annonce.Localisation ?? "";
        }
    }

    public ObservableCollection<Annonce> SimilarAnnonces { get; } = new();

    // ── Commandes ──

    public ICommand GoToChatCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ShareCommand { get; }
    public ICommand ReportCommand { get; }
    public ICommand OpenImageViewerCommand { get; }
    public ICommand NavigateToSimilarCommand { get; }
    public ICommand GoBackCommand { get; }
    public ICommand ProposeTradeCommand { get; }

    // ── Constructeur ──

    public AnnonceDetailViewModel(
        FirebaseService firebaseService,
        AuthService authService,
        FavoritesService favoritesService,
        SocialService socialService,
        ReportService reportService,
        AdMobService adMobService,
        GamificationService gamificationService,
        GeolocationService geolocationService,
        TransactionService transactionService,
        GlobalNotificationService globalNotificationService,
        ILogger<AnnonceDetailViewModel> logger)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _favoritesService = favoritesService;
        _socialService = socialService;
        _reportService = reportService;
        _adMobService = adMobService;
        _gamificationService = gamificationService;
        _geolocationService = geolocationService;
        _transactionService = transactionService;
        _globalNotificationService = globalNotificationService;
        _logger = logger;

        GoToChatCommand = new Command(async () => await OnGoToChatAsync());
        ToggleFavoriteCommand = new Command(async () => await OnToggleFavoriteAsync());
        ShareCommand = new Command(async () => await OnShareAsync());
        ReportCommand = new Command(async () => await OnReportAsync());
        OpenImageViewerCommand = new Command(async () => await OnOpenImageViewerAsync());
        NavigateToSimilarCommand = new Command<Annonce>(async (a) => await OnNavigateToSimilarAsync(a));
        GoBackCommand = new Command(async () => await SafeGoBackAsync());
        ProposeTradeCommand = new Command(async () => await OnProposeTradeAsync());
    }

    // ── IQueryAttributable ──

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("annonceId", out var idObj))
        {
            var annonceId = idObj?.ToString();
            if (!string.IsNullOrEmpty(annonceId))
            {
                _ = LoadAnnonceAsync(annonceId);
            }
        }
    }

    // ── Chargement des données ──

    private async Task LoadAnnonceAsync(string annonceId)
    {
        try
        {
            IsLoading = true;

            // 1. Charger l'annonce
            var annonce = await _firebaseService.GetAnnonceAsync(annonceId);
            if (annonce == null)
            {
                await Shell.Current.DisplayAlert("Introuvable", "Cette annonce n'existe plus.", "OK");
                await SafeGoBackAsync();
                return;
            }

            Annonce = annonce;
            OnPropertyChanged(nameof(DateCreationFormatted));
            OnPropertyChanged(nameof(StatutFormatted));
            OnPropertyChanged(nameof(HasTrocDetails));
            OnPropertyChanged(nameof(HasLocation));
            OnPropertyChanged(nameof(LocationText));
            OnPropertyChanged(nameof(PhotoIndicatorText));
            OnPropertyChanged(nameof(CanProposeTrade));

            // 2. Vérifier si c'est le propriétaire
            var currentUserId = _authService.GetUserId();
            IsOwner = !string.IsNullOrEmpty(currentUserId) &&
                      string.Equals(currentUserId.Trim(), annonce.UtilisateurId?.Trim(), StringComparison.Ordinal);

            // 3. Calculer la distance
            try
            {
                var location = await _geolocationService.GetCurrentLocationAsync();
                if (location != null && annonce.Latitude.HasValue && annonce.Longitude.HasValue
                    && annonce.Latitude != 0 && annonce.Longitude != 0)
                {
                    annonce.DistanceFromUser = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(
                        location,
                        new Microsoft.Maui.Devices.Sensors.Location(annonce.Latitude.Value, annonce.Longitude.Value),
                        DistanceUnits.Kilometers);
                    OnPropertyChanged(nameof(Annonce));
                }
            }
            catch (Exception geoEx)
            {
                Debug.WriteLine($"[AnnonceDetail] Géoloc échouée: {geoEx.Message}");
            }

            // 4. Vérifier si en favoris
            if (!string.IsNullOrEmpty(currentUserId))
            {
                try
                {
                    annonce.IsFavorite = await _favoritesService.IsFavoriteAsync(annonce.Id);
                }
                catch (Exception favEx)
                {
                    Debug.WriteLine($"[AnnonceDetail] Erreur favoris: {favEx.Message}");
                }
            }

            // 5. Charger le profil du publieur (en parallèle avec les similaires)
            var publisherTask = LoadPublisherProfileAsync(annonce.UtilisateurId);
            var similarTask = LoadSimilarAnnoncesAsync(annonce);
            
            await Task.WhenAll(publisherTask, similarTask);

            // 6. Gamification : enregistrer la vue
            if (!string.IsNullOrEmpty(currentUserId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gamificationService.OnUserActionAsync(currentUserId, "view_annonce");
                    }
                    catch { }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement de l'annonce {AnnonceId}", annonceId);
            await Shell.Current.DisplayAlert("Erreur", "Impossible de charger les détails de l'annonce.", "OK");
            await SafeGoBackAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPublisherProfileAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId)) return;
            PublisherProfile = await _firebaseService.GetUserProfileAsync(userId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AnnonceDetail] Erreur chargement profil publieur: {ex.Message}");
        }
    }

    private async Task LoadSimilarAnnoncesAsync(Annonce currentAnnonce)
    {
        try
        {
            var allAnnonces = await _firebaseService.GetAnnoncesAsync();
            var currentUserId = _authService.GetUserId();

            var similar = allAnnonces
                .Where(a => a.Id != currentAnnonce.Id &&
                            a.Categorie == currentAnnonce.Categorie &&
                            a.Statut == StatutAnnonce.Disponible)
                .Take(6)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SimilarAnnonces.Clear();
                foreach (var a in similar)
                    SimilarAnnonces.Add(a);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AnnonceDetail] Erreur chargement annonces similaires: {ex.Message}");
        }
    }

    // ── Actions ──

    private async Task OnGoToChatAsync()
    {
        if (Annonce == null) return;

        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Shell.Current.DisplayAlert("Connexion requise",
                "Vous devez être connecté pour démarrer une conversation.", "OK");
            return;
        }

        if (IsOwner)
        {
            await Shell.Current.DisplayAlert("Action impossible",
                "Vous ne pouvez pas démarrer une conversation pour votre propre annonce.", "OK");
            return;
        }

        // 🌍 Vérification du rayon local (50 km max) — interdit les contacts longue distance
        // (ex: utilisateur Maroc ↔ annonce France). Garantit des échanges locaux et sécurisés.
        if (Annonce.Latitude.HasValue && Annonce.Longitude.HasValue)
        {
            var userLocation = _geolocationService.GetLastKnownLocation()
                               ?? await _geolocationService.GetCurrentLocationAsync();

            if (userLocation == null)
            {
                await Shell.Current.DisplayAlert("📍 GPS requis",
                    "Activez votre localisation pour contacter cet annonceur.\n\n" +
                    "DonTroc favorise les échanges locaux dans un rayon de " +
                    $"{RecommendationService.MAX_LOCAL_RADIUS_KM:0} km.",
                    "OK");
                return;
            }

            var distanceKm = _geolocationService.CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                Annonce.Latitude.Value, Annonce.Longitude.Value);

            if (distanceKm > RecommendationService.MAX_LOCAL_RADIUS_KM)
            {
                await Shell.Current.DisplayAlert("📍 Annonce trop éloignée",
                    $"Cette annonce se trouve à environ {distanceKm:0} km de vous.\n\n" +
                    $"DonTroc limite les contacts aux annonces situées dans un rayon de " +
                    $"{RecommendationService.MAX_LOCAL_RADIUS_KM:0} km pour favoriser " +
                    "des échanges locaux et sécurisés.",
                    "Compris");
                return;
            }
        }

        try
        {
            IsBusy = true;

            await _adMobService.ShowInterstitialAfterActionAsync("ChatFromDetail");

            var conversation = await _firebaseService.GetOrCreateConversationAsync(Annonce.Id);
            if (conversation == null || string.IsNullOrEmpty(conversation.Id))
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible de créer la conversation.", "OK");
                return;
            }

            _globalNotificationService.AddConversation(conversation.Id);

            // Créer une transaction auto si nécessaire
            try
            {
                var transactionsExistantes = await _transactionService.GetHistoriqueTransactionsAsync();
                var transactionExistante = transactionsExistantes.FirstOrDefault(t =>
                    t.AnnonceId == Annonce.Id &&
                    t.DemandeurId == userId &&
                    t.Statut != StatutTransaction.Annulee);

                if (transactionExistante == null)
                {
                    var message = $"Bonjour ! Je suis intéressé(e) par votre {Annonce.Type.ToLower()} : {Annonce.Titre}";
                    await _transactionService.ProposerTransactionAsync(
                        Annonce.Id, Annonce.UtilisateurId, null, message);
                }
            }
            catch (Exception txEx)
            {
                Debug.WriteLine($"[AnnonceDetail] Erreur transaction auto: {txEx.Message}");
            }

            var encodedId = Uri.EscapeDataString(conversation.Id);
            await Shell.Current.GoToAsync($"ChatView?conversationId={encodedId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du démarrage du chat depuis le détail");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de démarrer la conversation.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnToggleFavoriteAsync()
    {
        if (Annonce == null) return;

        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Shell.Current.DisplayAlert("Connexion requise",
                "Vous devez être connecté pour gérer vos favoris.", "OK");
            return;
        }

        try
        {
            Annonce.IsFavorite = !Annonce.IsFavorite;
            OnPropertyChanged(nameof(Annonce));

            if (Annonce.IsFavorite)
            {
                await _favoritesService.AddToFavoritesAsync(Annonce);
                await _gamificationService.OnUserActionAsync(userId, "add_favorite");
                _ = Services.AnimationService.ShowToastAsync("Ajouté à vos favoris ❤️", Services.AnimationService.ToastType.Success, 1800);
            }
            else
            {
                await _favoritesService.RemoveFavoriteAsync(userId, Annonce.Id);
                _ = Services.AnimationService.ShowToastAsync("Retiré de vos favoris", Services.AnimationService.ToastType.Info, 1500);
            }
        }
        catch (Exception ex)
        {
            Annonce.IsFavorite = !Annonce.IsFavorite;
            OnPropertyChanged(nameof(Annonce));
            Debug.WriteLine($"[AnnonceDetail] Erreur favoris: {ex.Message}");
            _ = Services.AnimationService.ShowToastAsync("Impossible de mettre à jour les favoris", Services.AnimationService.ToastType.Error);
        }
    }

    private async Task OnShareAsync()
    {
        if (Annonce == null) return;

        try
        {
            await _socialService.ShareAnnonceAsync(Annonce);

            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _gamificationService.OnUserActionAsync(userId, "share_annonce");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AnnonceDetail] Erreur partage: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de partager cette annonce.", "OK");
        }
    }

    private async Task OnReportAsync()
    {
        if (Annonce == null) return;

        var reason = await Shell.Current.DisplayPromptAsync(
            "Signaler l'annonce",
            "Pourquoi signalez-vous cette annonce ?",
            "Envoyer", "Annuler", "Ex: Contenu inapproprié");

        if (string.IsNullOrWhiteSpace(reason)) return;

        var currentUserId = _authService.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Shell.Current.DisplayAlert("Erreur",
                "Vous devez être connecté pour signaler une annonce.", "OK");
            return;
        }

        var report = new Report
        {
            ReportedItemId = Annonce.Id,
            ReportedItemType = "Annonce",
            ReporterId = currentUserId,
            Reason = reason,
        };

        var success = await _reportService.CreateReport(report);
        if (success)
        {
            await Shell.Current.DisplayAlert("Signalement envoyé",
                "Merci. Votre signalement a été envoyé et sera examiné.", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlert("Erreur",
                "Impossible d'envoyer le signalement.", "OK");
        }
    }

    private async Task OnOpenImageViewerAsync()
    {
        if (Annonce?.PhotosUrls == null || !Annonce.PhotosUrls.Any()) return;

        try
        {
            var validUrls = Annonce.PhotosUrls
                .Where(url => !string.IsNullOrEmpty(url) &&
                              (Uri.IsWellFormedUriString(url, UriKind.Absolute) || url.StartsWith("http")))
                .ToList();

            if (!validUrls.Any()) return;

            var urlsParameter = string.Join("|", validUrls);
            var navParams = new Dictionary<string, object> { { "imageUrls", urlsParameter } };
            await Shell.Current.GoToAsync("ImageViewerView", navParams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur ouverture images");
        }
    }

    private async Task OnNavigateToSimilarAsync(Annonce? annonce)
    {
        if (annonce == null) return;
        
        // Recharger les données en place au lieu d'empiler une nouvelle page
        // Cela évite l'erreur "Ambiguous routes" de Shell quand la même route est empilée
        CurrentPhotoIndex = 0;
        SimilarAnnonces.Clear();
        await LoadAnnonceAsync(annonce.Id);
    }

    private async Task OnProposeTradeAsync()
    {
        if (Annonce == null) return;

        if (!CanProposeTrade)
        {
            await Shell.Current.DisplayAlert("Action impossible",
                "Cette annonce n'est pas disponible pour un troc.", "OK");
            return;
        }

        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Shell.Current.DisplayAlert("Connexion requise",
                "Vous devez être connecté pour proposer un troc.", "OK");
            return;
        }

        try
        {
            await Shell.Current.GoToAsync($"TradeProposalPage?annonceId={Uri.EscapeDataString(Annonce.Id)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur navigation vers TradeProposalPage");
            await Shell.Current.DisplayAlert("Erreur",
                "Impossible d'ouvrir la page de proposition.", "OK");
        }
    }

    /// <summary>
    /// Retour arrière sécurisé qui gère les routes ambiguës de Shell.
    /// </summary>
    private async Task SafeGoBackAsync()
    {
        try
        {
            var nav = Shell.Current.Navigation;
            if (nav.NavigationStack.Count > 1)
            {
                await nav.PopAsync();
            }
            else
            {
                await Shell.Current.GoToAsync("//AnnoncesView");
            }
        }
        catch
        {
            // Fallback absolu : retourner à l'onglet annonces
            try { await Shell.Current.GoToAsync("//AnnoncesView"); }
            catch { /* abandon */ }
        }
    }
}


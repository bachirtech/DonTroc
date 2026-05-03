// Fichier: DonTroc/ViewModels/DashboardViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.Views;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

// ViewModel pour la page d'accueil (Dashboard) avec feed personnalisé
public class DashboardViewModel : BaseViewModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly AdMobService _adMobService;
    private readonly PremiumFeaturesViewModel _premiumFeatures; // ViewModel des fonctionnalités premium
    private readonly GamificationService _gamificationService;
    private readonly GeolocationService _geolocationService;
    private readonly FavoritesService _favoritesService;
    private readonly RecommendationService _recommendationService;
    private readonly UnreadMessageService _unreadMessageService;
    private readonly OnboardingService _onboardingService;
    private readonly EventService _eventService;

    // Collections pour l'affichage des données
    public ObservableCollection<Annonce> AnnoncesRecentes { get; } = new();
    public ObservableCollection<Annonce> AnnoncesRecommandees { get; } = new();
    public ObservableCollection<Annonce> AnnoncesProches { get; } = new();
    public ObservableCollection<OnboardingStep> ChecklistSteps { get; } = new();
    public ObservableCollection<Evenement> EvenementsAVenir { get; } = new();
    
    // === PROPRIÉTÉS STREAK / GAMIFICATION ===
    
    private int _streakCount;
    public int StreakCount
    {
        get => _streakCount;
        set => SetProperty(ref _streakCount, value);
    }

    private int _userLevel;
    public int UserLevel
    {
        get => _userLevel;
        set => SetProperty(ref _userLevel, value);
    }

    private int _userXp;
    public int UserXp
    {
        get => _userXp;
        set => SetProperty(ref _userXp, value);
    }

    private string _userTitle = "";
    public string UserTitle
    {
        get => _userTitle;
        set => SetProperty(ref _userTitle, value);
    }

    private bool _hasStreak;
    public bool HasStreak
    {
        get => _hasStreak;
        set => SetProperty(ref _hasStreak, value);
    }

    // === PROPRIÉTÉS SECTIONS ===

    private bool _hasRecommandations;
    public bool HasRecommandations
    {
        get => _hasRecommandations;
        set => SetProperty(ref _hasRecommandations, value);
    }

    private bool _hasAnnoncesProches;
    public bool HasAnnoncesProches
    {
        get => _hasAnnoncesProches;
        set => SetProperty(ref _hasAnnoncesProches, value);
    }

    private string _locationText = "";
    public string LocationText
    {
        get => _locationText;
        set => SetProperty(ref _locationText, value);
    }

    // === PROPRIÉTÉS ACTIVITÉ RÉCENTE ===

    private int _nombreMessagesNonLus;
    public int NombreMessagesNonLus
    {
        get => _nombreMessagesNonLus;
        set => SetProperty(ref _nombreMessagesNonLus, value);
    }

    private int _nombreBadgesDebloques;
    public int NombreBadgesDebloques
    {
        get => _nombreBadgesDebloques;
        set => SetProperty(ref _nombreBadgesDebloques, value);
    }

    private int _nombreFavoris;
    public int NombreFavoris
    {
        get => _nombreFavoris;
        set => SetProperty(ref _nombreFavoris, value);
    }

    private bool _hasActiviteRecente;
    public bool HasActiviteRecente
    {
        get => _hasActiviteRecente;
        set => SetProperty(ref _hasActiviteRecente, value);
    }

    // === PROPRIÉTÉS CHECKLIST ONBOARDING ===

    private bool _isChecklistVisible;
    public bool IsChecklistVisible
    {
        get => _isChecklistVisible;
        set => SetProperty(ref _isChecklistVisible, value);
    }

    private double _checklistProgress;
    public double ChecklistProgress
    {
        get => _checklistProgress;
        set => SetProperty(ref _checklistProgress, value);
    }

    private int _checklistCompletedCount;
    public int ChecklistCompletedCount
    {
        get => _checklistCompletedCount;
        set => SetProperty(ref _checklistCompletedCount, value);
    }

    private int _checklistTotalSteps;
    public int ChecklistTotalSteps
    {
        get => _checklistTotalSteps;
        set => SetProperty(ref _checklistTotalSteps, value);
    }

    // Propriétés pour les statistiques utilisateur
    private int _nombreMesAnnonces;
    public int NombreMesAnnonces
    {
        get => _nombreMesAnnonces;
        set => SetProperty(ref _nombreMesAnnonces, value);
    }

    private int _nombreConversations;
    public int NombreConversations
    {
        get => _nombreConversations;
        set => SetProperty(ref _nombreConversations, value);
    }

    private string _messageAccueil;
    public string MessageAccueil
    {
        get => _messageAccueil;
        set => SetProperty(ref _messageAccueil, value);
    }

    // Propriétés et commandes pour les fonctionnalités premium
    public bool IsAdFreeActive => _premiumFeatures.IsAdFreeActive;
    public DateTime AdFreeUntil => _premiumFeatures.AdFreeUntil;
    public int BoostCredits => _premiumFeatures.BoostCredits;
    public bool IsPermanentPremium => _premiumFeatures.IsPermanentPremium;
    public string RemoveAdsPriceText => _premiumFeatures.RemoveAdsPriceText;
    public ICommand WatchAdForAdFreeCommand => _premiumFeatures.WatchAdForAdFreeCommand;
    public ICommand WatchAdForBoostCreditsCommand => _premiumFeatures.WatchAdForBoostCreditsCommand;
    public ICommand WatchAdForStatsCommand => _premiumFeatures.WatchAdForStatsCommand;
    public ICommand PurchaseRemoveAdsCommand => _premiumFeatures.PurchaseRemoveAdsCommand;
    public ICommand RestorePurchasesCommand => _premiumFeatures.RestorePurchasesCommand;

    // Commandes
    public ICommand GoToCreationAnnonceCommand { get; }
    public ICommand GoToAnnoncesCommand { get; }
    public ICommand GoToRewardsCommand { get; }
    public ICommand GoToMessagesCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand VoirAnnonceCommand { get; }
    public ICommand DismissChecklistCommand { get; }
    public ICommand ChecklistStepTappedCommand { get; }
    public ICommand GoToEventsCommand { get; private set; } = null!;
    public ICommand OpenEventDetailCommand { get; private set; } = null!;

    private bool _hasEvenements;
    public bool HasEvenements
    {
        get => _hasEvenements;
        set => SetProperty(ref _hasEvenements, value);
    }

    // Constructeur avec injection de dépendances
    public DashboardViewModel(
        FirebaseService firebaseService, 
        AuthService authService, 
        AdMobService adMobService, 
        PremiumFeaturesViewModel premiumFeatures,
        GamificationService gamificationService,
        GeolocationService geolocationService,
        FavoritesService favoritesService,
        RecommendationService recommendationService,
        UnreadMessageService unreadMessageService,
        OnboardingService onboardingService,
        EventService eventService)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _adMobService = adMobService;
        _premiumFeatures = premiumFeatures;
        _gamificationService = gamificationService;
        _geolocationService = geolocationService;
        _favoritesService = favoritesService;
        _recommendationService = recommendationService;
        _unreadMessageService = unreadMessageService;
        _onboardingService = onboardingService;
        _eventService = eventService;

        // S'abonner aux changements de propriétés du ViewModel premium
        _premiumFeatures.PropertyChanged += OnPremiumFeaturesPropertyChanged;

        // S'abonner aux messages non lus
        _unreadMessageService.PropertyChanged += OnUnreadMessageServicePropertyChanged;

        // Initialisation des propriétés
        _messageAccueil = "Bienvenue sur DonTroc !";
        _nombreMesAnnonces = 0;
        _nombreConversations = 0;

        // Initialisation des commandes
        GoToCreationAnnonceCommand = new Command(async void () => 
        {
            await Shell.Current.GoToAsync(nameof(CreationAnnonceView));
        });

        GoToAnnoncesCommand = new Command(async void () =>
        {
            await Shell.Current.GoToAsync("//AnnoncesView");
        });

        GoToRewardsCommand = new Command(async void () =>
        {
            await Shell.Current.GoToAsync(nameof(Views.RewardsPage));
        });

        GoToMessagesCommand = new Command(async void () =>
        {
            await Shell.Current.GoToAsync("//ConversationsView");
        });

        RefreshCommand = new Command(async void () =>
        {
            _recommendationService.InvalidateCache();
            await LoadDashboardDataAsync();
        });
        
        VoirAnnonceCommand = new Command<Annonce>(async void (annonce) => await OnVoirAnnonce(annonce));
        
        DismissChecklistCommand = new Command(() =>
        {
            _onboardingService.DismissChecklist();
            IsChecklistVisible = false;
        });
        
        ChecklistStepTappedCommand = new Command<OnboardingStep>(async void (step) => await OnChecklistStepTapped(step));

        GoToEventsCommand = new Command(async void () =>
        {
            await Shell.Current.GoToAsync(nameof(EventsListView));
        });

        OpenEventDetailCommand = new Command<Evenement>(async void (ev) =>
        {
            if (ev != null)
                await Shell.Current.GoToAsync($"{nameof(EventDetailView)}?eventId={ev.Id}");
        });

        // Charger les données au démarrage
        _ = LoadDashboardDataAsync();
        
        // NOTE: Pas de LoadAds() ici — AdMobNativeService.WaitForSdkAndPreloadAsync()
        // gère déjà le préchargement initial au démarrage de l'app.
        // Appeler LoadAds() ici générait des requêtes redondantes.
    }

    /// <summary>
    /// Charge toutes les données nécessaires pour le dashboard
    /// </summary>
    public async Task LoadDashboardDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Charger en parallèle pour la performance
            var recentesTask = LoadAnnoncesRecentesAsync();
            var statsTask = LoadUserStatsAsync();
            var gamificationTask = LoadGamificationDataAsync();
            var activiteTask = LoadActiviteRecenteAsync();
            var checklistTask = LoadChecklistAsync();

            await Task.WhenAll(recentesTask, statsTask, gamificationTask, activiteTask, checklistTask);

            // Charger les recommandations et les annonces proches (dépendent des données ci-dessus)
            var recommandationsTask = LoadRecommandationsAsync();
            var prochesTask = LoadAnnoncesProchesAsync();

            await Task.WhenAll(recommandationsTask, prochesTask);
            
            // Charger les événements à venir (en parallèle, non bloquant)
            await LoadEvenementsAsync();

            // Mettre à jour le message d'accueil
            UpdateMessageAccueil();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des données du dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Charge les données de gamification (streak, XP, niveau)
    /// </summary>
    private async Task LoadGamificationDataAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var profile = await _gamificationService.GetUserProfileAsync(userId);
            
            StreakCount = profile.DailyStreak;
            UserLevel = profile.CurrentLevel;
            UserXp = profile.TotalXp;
            HasStreak = profile.DailyStreak > 0;

            var title = await _gamificationService.GetUserTitleAsync(userId);
            UserTitle = title;

            NombreBadgesDebloques = profile.UnlockedBadges.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement gamification: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les événements à venir (limite 5 pour le carrousel du Dashboard).
    /// Privilégie les événements proches géographiquement, complète avec les officiels saisonniers.
    /// </summary>
    private async Task LoadEvenementsAsync()
    {
        try
        {
            var nearby = await _eventService.GetNearbyEventsAsync(50, 5);
            var seasonal = await _eventService.GetSeasonalEventsAsync(5);

            // Fusion : nearby d'abord, puis seasonal sans doublons
            var combined = nearby.ToList();
            foreach (var s in seasonal)
            {
                if (combined.All(e => e.Id != s.Id))
                    combined.Add(s);
            }
            // Si rien de proche/officiel, fallback sur les prochains événements globaux
            if (combined.Count == 0)
            {
                combined = (await _eventService.GetUpcomingEventsAsync(5)).ToList();
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                EvenementsAVenir.Clear();
                foreach (var e in combined.Take(5)) EvenementsAVenir.Add(e);
                HasEvenements = EvenementsAVenir.Count > 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement événements dashboard: {ex.Message}");
            HasEvenements = false;
        }
    }

    /// <summary>
    /// Charge les recommandations personnalisées
    /// </summary>
    private async Task LoadRecommandationsAsync()
    {
        try
        {
            var recommandations = await _recommendationService.GetRecommandationsAsync(8);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AnnoncesRecommandees.Clear();
                foreach (var annonce in recommandations)
                {
                    AnnoncesRecommandees.Add(annonce);
                }
                HasRecommandations = AnnoncesRecommandees.Count > 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement recommandations: {ex.Message}");
            HasRecommandations = false;
        }
    }

    /// <summary>
    /// Charge les annonces proches de l'utilisateur
    /// </summary>
    private async Task LoadAnnoncesProchesAsync()
    {
        try
        {
            var (annonces, locationText) = await _recommendationService.GetNearbyAnnoncesAsync(8);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AnnoncesProches.Clear();
                foreach (var annonce in annonces)
                {
                    AnnoncesProches.Add(annonce);
                }
                HasAnnoncesProches = AnnoncesProches.Count > 0;
                LocationText = !string.IsNullOrEmpty(locationText) ? $"📍 {locationText}" : "📍 Près de chez vous";
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement annonces proches: {ex.Message}");
            HasAnnoncesProches = false;
        }
    }

    /// <summary>
    /// Charge les données d'activité récente
    /// </summary>
    private async Task LoadActiviteRecenteAsync()
    {
        try
        {
            NombreMessagesNonLus = _unreadMessageService.TotalUnreadCount;

            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var favorites = await _favoritesService.GetUserFavoritesAsync();
                NombreFavoris = favorites.Count;
            }

            HasActiviteRecente = NombreMessagesNonLus > 0 || NombreBadgesDebloques > 0 || NombreFavoris > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement activité récente: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les 5 annonces les plus récentes
    /// </summary>
    private async Task LoadAnnoncesRecentesAsync()
    {
        try
        {
            var toutesLesAnnonces = await _firebaseService.GetAllAnnoncesAsync();
            
            // Prendre les 5 annonces les plus récentes
            var annoncesRecentes = toutesLesAnnonces
                .OrderByDescending(a => a.DateCreation)
                .Take(5)
                .ToList();

            // Mettre à jour la collection
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AnnoncesRecentes.Clear();
                foreach (var annonce in annoncesRecentes)
                {
                    AnnoncesRecentes.Add(annonce);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des annonces récentes: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les statistiques de l'utilisateur connecté
    /// </summary>
    private async Task LoadUserStatsAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                // Compter les annonces de l'utilisateur
                var mesAnnonces = await _firebaseService.GetAnnoncesForUserAsync(userId);
                NombreMesAnnonces = mesAnnonces.Count();

                // Compter les conversations (approximatif pour l'instant)
                // TODO: Ajouter une méthode pour compter les conversations réelles
                NombreConversations = 0; // Placeholder
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des statistiques: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge la checklist d'onboarding "Premiers pas" (visible 7 jours après inscription)
    /// </summary>
    private async Task LoadChecklistAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            // Vérifier si la checklist doit être affichée (7 jours après onboarding)
            if (!_onboardingService.IsChecklistVisible)
            {
                IsChecklistVisible = false;
                return;
            }

            // Auto-compléter les étapes basées sur l'état réel de l'utilisateur
            try
            {
                var profile = await _firebaseService.GetUserProfileAsync(userId);
                var mesAnnonces = await _firebaseService.GetAnnoncesForUserAsync(userId);
                var favoris = await _favoritesService.GetUserFavoritesAsync();
                var gamifProfile = await _gamificationService.GetUserProfileAsync(userId);
                var canClaim = await _gamificationService.CanClaimDailyRewardAsync(userId);

                bool hasPhoto = !string.IsNullOrEmpty(profile?.ProfilePictureUrl);
                bool hasBio = !string.IsNullOrEmpty(profile?.Name); // Le profil est "complet" si un nom est défini
                int annonceCount = mesAnnonces?.Count() ?? 0;
                int favoriteCount = favoris?.Count ?? 0;
                // Vérifier les messages via les conversations de l'utilisateur
                int messageCount = 0;
                try
                {
                    var conversations = await _firebaseService.GetUserConversationsAsync(userId);
                    messageCount = conversations?.Count() ?? 0;
                }
                catch { /* ignoré */ }
                bool hasDailyReward = !canClaim; // Si ne peut pas réclamer → déjà réclamé

                await _onboardingService.CheckAndCompleteStepsAsync(
                    hasPhoto, hasBio, annonceCount, favoriteCount, messageCount, hasDailyReward);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Erreur auto-check checklist: {ex.Message}");
            }

            // Récupérer les étapes avec leur état mis à jour
            var steps = _onboardingService.GetChecklistSteps();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ChecklistSteps.Clear();
                foreach (var step in steps)
                {
                    ChecklistSteps.Add(step);
                }

                ChecklistTotalSteps = steps.Count;
                ChecklistCompletedCount = steps.Count(s => s.IsCompleted);
                ChecklistProgress = ChecklistTotalSteps > 0 
                    ? (double)ChecklistCompletedCount / ChecklistTotalSteps 
                    : 0;
                IsChecklistVisible = ChecklistCompletedCount < ChecklistTotalSteps;

                // Si toute la checklist est complétée, masquer définitivement
                if (ChecklistCompletedCount >= ChecklistTotalSteps)
                {
                    _onboardingService.DismissChecklist();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement checklist: {ex.Message}");
            IsChecklistVisible = false;
        }
    }

    /// <summary>
    /// Navigation vers la page correspondante à l'étape de la checklist
    /// </summary>
    private async Task OnChecklistStepTapped(OnboardingStep? step)
    {
        if (step == null || step.IsCompleted || string.IsNullOrEmpty(step.NavigationRoute)) return;

        try
        {
            await Shell.Current.GoToAsync(step.NavigationRoute);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Erreur navigation checklist: {ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour le message d'accueil basé sur les statistiques
    /// </summary>
    private void UpdateMessageAccueil()
    {
        if (NombreMesAnnonces == 0)
        {
            MessageAccueil = "Bienvenue sur DonTroc ! Publiez votre première annonce 🎉";
        }
        else if (NombreMesAnnonces == 1)
        {
            MessageAccueil = "Vous avez publié votre première annonce ! 👍";
        }
        else
        {
            MessageAccueil = $"Vous avez {NombreMesAnnonces} annonces publiées 🌟";
        }
    }

    /// <summary>
    /// Navigation vers le détail d'une annonce
    /// Affiche un interstitiel (si les conditions anti-spam sont remplies) avant la navigation.
    /// </summary>
    private async Task OnVoirAnnonce(Annonce? annonce)
    {
        if (annonce == null) return;

        try
        {
            // Tenter d'afficher un interstitiel avant la navigation
            // Respecte automatiquement : Ad-Free, cooldown 120s, max 5/session, fréquence 1/3 nav
            await _adMobService.TryShowInterstitialOnNavigationAsync("AnnonceDetail");
            
            await Shell.Current.GoToAsync($"AnnonceDetailView?annonceId={annonce.Id}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Erreur navigation détail: {ex.Message}");
            // Fallback vers la liste des annonces
            await Shell.Current.GoToAsync("//AnnoncesView");
        }
    }

    private void OnPremiumFeaturesPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null) OnPropertyChanged(e.PropertyName);
    }

    private void OnUnreadMessageServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnreadMessageService.TotalUnreadCount))
        {
            NombreMessagesNonLus = _unreadMessageService.TotalUnreadCount;
            HasActiviteRecente = NombreMessagesNonLus > 0 || NombreBadgesDebloques > 0 || NombreFavoris > 0;
        }
    }

    public override void Dispose()
    {
        _premiumFeatures.PropertyChanged -= OnPremiumFeaturesPropertyChanged;
        _unreadMessageService.PropertyChanged -= OnUnreadMessageServicePropertyChanged;
        base.Dispose();
    }
}

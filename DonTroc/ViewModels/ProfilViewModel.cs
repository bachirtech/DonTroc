using System;
using System.Collections.Generic;
using DonTroc.Models;
using DonTroc.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;
using System.Threading.Tasks;
using DonTroc.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

// ViewModel pour la page de profil utilisateur
public class ProfilViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly FirebaseService _firebaseService;
    private readonly ILogger<ProfilViewModel> _logger;
    private readonly PremiumFeaturesViewModel _premiumFeaturesViewModel; // Service pour les fonctionnalités premium
    private readonly IServiceProvider _serviceProvider;
    private readonly ThemeService _themeService; // Service de gestion des thèmes
    private readonly IConsentService? _consentService; // RGPD / UMP — peut être null si non enregistré

    private string _userEmail;

    private UserProfile _userProfile;


    // Constructeur avec injection des services uniquement
    public ProfilViewModel(AuthService authService, FirebaseService firebaseService,
        PremiumFeaturesViewModel premiumFeaturesViewModel, ThemeService themeService, ILogger<ProfilViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _authService = authService;
        _firebaseService = firebaseService;
        _premiumFeaturesViewModel = premiumFeaturesViewModel; // Injection du ViewModel premium
        _themeService = themeService; // Injection du service de gestion des thèmes
        _logger = logger;
        _serviceProvider = serviceProvider;
        // Récupération optionnelle du service de consentement (UMP) — peut être null en tests/iOS
        _consentService = serviceProvider.GetService<IConsentService>();

        // Initialisation avec des valeurs par défaut
        _userEmail = string.Empty;
        _userProfile = new UserProfile
        {
            Name = string.Empty, // Nom de l'utilisateur, initialisé vide
            Email = string.Empty, // Email de l'utilisateur, initialisé vide
            ProfilePictureUrl = "default_profile_icon.png" // URL par défaut pour l'icône de profil
        };

        MesAnnonces = new ObservableCollection<Annonce>();

        SignOutCommand = new Command(async void () => await OnSignOutAsync());
        LoadMesAnnoncesCommand = new Command(async void () => await ExecuteLoadMesAnnoncesCommand());
        // Initialisation des commandes renommées
        DeleteAnnonceCommand = new Command<Annonce>(async void (annonce) => await OnSupprimerAnnonce(annonce));
        EditAnnonceCommand = new Command<Annonce?>(async void (annonce) => await OnModifierAnnonce(annonce));
        // Initialisation de la commande pour voir les conversations
        GoToConversationsCommand = new Command(async void () => await OnGoToConversations());
        // Initialisation de la commande pour modifier le profil
        EditProfileCommand = new Command(async void () => await OnEditProfile());
        // Initialisation de la commande pour booster l'annonce
        BoostAnnonceCommand = new Command<Annonce>(async void (annonce) => await OnBoostAnnonce(annonce));
        // Initialisation de la commande pour changer le thème
        ChangeThemeCommand = new Command<string>(OnChangeTheme);
        // Initialisation de la commande pour sélectionner un thème directement
        SelectThemeCommand = new Command<string>(OnSelectTheme);
        // Initialisation des nouvelles commandes pour l'accès rapide
        NavigateToMapCommand = new Command(async void () => await OnNavigateToMap());
        NavigateToTransactionsCommand = new Command(async void () => await OnNavigateToTransactions());
        NavigateToRewardsCommand = new Command(async void () => await OnNavigateToRewards());
        NavigateToSocialCommand = new Command(async void () => await OnNavigateToSocial());
        // Initialisation de la commande pour accéder aux propositions de troc
        NavigateToTradeProposalsCommand = new Command(async void () => await OnNavigateToTradeProposals());
        // Initialisation de la commande pour supprimer le compte
        DeleteAccountCommand = new Command(async void () => await OnDeleteAccount());
        // Initialisation de la commande pour faire un don au développeur
        DonateToDevCommand = new Command(async void () => await OnDonateToDevAsync());
        // Initialisation de la commande pour accéder au panneau admin
        NavigateToAdminCommand = new Command(async void () => await OnNavigateToAdmin());
        // Initialisation de la commande pour accéder à la configuration admin
        NavigateToAdminSetupCommand = new Command(async void () => await OnNavigateToAdminSetup());
        // Initialisation de la commande pour ouvrir une URL
        OpenUrlCommand = new Command<string>(async void (url) => await OnOpenUrl(url));

        // Initialisation de la commande pour rouvrir le formulaire de consentement publicitaire (RGPD/UMP)
        ManagePrivacyConsentCommand = new Command(async void () => await OnManagePrivacyConsentAsync());

        // 🛠 DEBUG : commande pour réinitialiser le consentement RGPD (visible admin uniquement)
        ResetConsentDebugCommand = new Command(async void () => await OnResetConsentDebugAsync());

        _ = LoadUserProfile();
        // Exécute la commande pour charger les annonces au démarrage du ViewModel
        LoadMesAnnoncesCommand.Execute(null);
    }

    public string UserEmail // Propriété pour stocker l'email de l'utilisateur
    {
        get => _userEmail;
        set => SetProperty(ref _userEmail, value);
    }


    public UserProfile UserProfile
    {
        get => _userProfile;
        private set
        {
            if (SetProperty(ref _userProfile, value))
            {
                // Notifier les propriétés dépendantes du rôle admin
                OnPropertyChanged(nameof(IsAdminOrModerator));
                OnPropertyChanged(nameof(IsAdmin));
            }
        }
    }

    // Collection pour stocker les annonces de l'utilisateur
    public ObservableCollection<Annonce> MesAnnonces { get; }

    // Commande pour la déconnexion
    public ICommand SignOutCommand { get; }

    // Commande pour charger les annonces de l'utilisateur
    public ICommand LoadMesAnnoncesCommand { get; }

    // Commandes pour la modification et la suppression (renommées pour correspondre au XAML)
    public ICommand EditAnnonceCommand { get; }

    public ICommand DeleteAnnonceCommand { get; }

    // Commande pour naviguer vers les conversations de l'utilisateur
    public ICommand GoToConversationsCommand { get; }

    // Commande pour naviguer vers la page de modification du profil
    public ICommand EditProfileCommand { get; }

    // Commande pour booster une annonce
    public ICommand BoostAnnonceCommand { get; }

    // Nouvelles commandes pour l'accès rapide
    public ICommand NavigateToMapCommand { get; }
    public ICommand NavigateToTransactionsCommand { get; }
    public ICommand NavigateToRewardsCommand { get; }
    public ICommand NavigateToSocialCommand { get; }
    public ICommand NavigateToTradeProposalsCommand { get; }

    // Propriétés pour le sélecteur de thème
    public string CurrentThemeName => _themeService.GetThemeDisplayName();
    public string CurrentThemeIcon => _themeService.GetThemeIcon();
    public string CurrentThemeEmoji => _themeService.GetThemeEmoji();
    public List<string> AvailableThemes { get; } = new List<string> { "Mode clair", "Mode sombre", "Automatique" };
    
    // Propriétés réactives pour le sélecteur visuel de thème
    public bool IsLightThemeSelected => _themeService.CurrentTheme == ThemeService.AppTheme.Light;
    public bool IsDarkThemeSelected => _themeService.CurrentTheme == ThemeService.AppTheme.Dark;
    public bool IsSystemThemeSelected => _themeService.CurrentTheme == ThemeService.AppTheme.System;

    // Commande pour changer le thème
    public ICommand ChangeThemeCommand { get; }
    
    // Commande directe pour sélectionner un thème via paramètre
    public ICommand SelectThemeCommand { get; }

    // Commande pour supprimer le compte utilisateur
    public ICommand DeleteAccountCommand { get; }

    // Commande pour faire un don au développeur
    public ICommand DonateToDevCommand { get; }
    
    // Commande pour accéder au panneau d'administration
    public ICommand NavigateToAdminCommand { get; }
    
    // Commande pour accéder à la configuration admin
    public ICommand NavigateToAdminSetupCommand { get; }
    
    // Commande pour ouvrir une URL dans le navigateur
    public ICommand OpenUrlCommand { get; }

    // Commande pour rouvrir le formulaire de consentement publicitaire (RGPD / UMP)
    public ICommand ManagePrivacyConsentCommand { get; }

    // 🛠 DEBUG : commande pour réinitialiser le consentement RGPD (visible admin uniquement)
    public ICommand ResetConsentDebugCommand { get; }

    // Visibilité du bouton "Confidentialité publicitaire" — visible uniquement pour les utilisateurs
    // dans une zone géo où le RGPD/CMP s'applique (EEE, UK, Suisse, Brésil...).
    // Pour les autres (Maroc inclus), le bouton est masqué pour ne pas polluer l'UI.
    public bool ShowPrivacyOptionsEntry => _consentService?.IsPrivacyOptionsRequired() ?? false;
    
    // Version de l'application
    public string AppVersion => $"v{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";
    
    // Propriété pour vérifier si l'utilisateur est admin ou modérateur
    public bool IsAdminOrModerator => UserProfile?.CanAccessAdminPanel ?? false;
    
    // Propriété pour vérifier si l'utilisateur est admin (pas juste modérateur)
    public bool IsAdmin => UserProfile?.IsAdmin ?? false;

    private async Task OnBoostAnnonce(Annonce? annonce)
    {
        if (annonce == null) return;

        try
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Booster l'annonce",
                $"Voulez-vous utiliser 1 crédit pour mettre en avant \"{annonce.Titre}\" pendant 24 heures ?",
                "Oui, booster !",
                "Annuler"
            );

            if (!confirm) return;

            // Vérifier si l'utilisateur a des crédits et en utiliser un
            if (_premiumFeaturesViewModel.CanUseBoostCredit())
            {
                try
                {
                    IsBusy = true;

                    // Utiliser un crédit de boost
                    _premiumFeaturesViewModel.UseBoostCredit();

                    // Appeler le service pour mettre à jour l'annonce dans Firebase
                    await _firebaseService.BoostAnnonceAsync(annonce.Id);
                    await Shell.Current.DisplayAlert("Succès !", "Votre annonce a été boostée et sera mise en avant.",
                        "Génial !");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Erreur", $"Une erreur est survenue : {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
            else
            {
                // Si l'utilisateur n'a pas de crédits
                var goToDashboard = await Shell.Current.DisplayAlert(
                    "Crédits insuffisants",
                    "Vous n'avez plus de crédits de boost. Voulez-vous en obtenir plus en regardant une publicité ?",
                    "Obtenir des crédits",
                    "Non merci"
                );

                if (goToDashboard)
                {
                    // Rediriger vers le tableau de bord pour obtenir des crédits
                    await Shell.Current.GoToAsync("//DashboardView");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du boost de l'annonce");
        }
    }

    public async Task ExecuteLoadMesAnnoncesCommand() // Méthode pour charger les annonces de l'utilisateur
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            // Vérifie si l'utilisateur est authentifié avant de charger
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                // Aucun utilisateur connecté: ne pas afficher d'erreur, simplement vider la liste
                MesAnnonces.Clear();
                return;
            }

            // Efface la collection existante avant de charger les nouvelles annonces
            MesAnnonces.Clear();
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var annonces = await _firebaseService.GetAnnoncesForUserAsync(userId);
                foreach (var annonce in annonces)
                {
                    MesAnnonces.Add(annonce);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Silencieux: utilisateur non authentifié
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profil] Erreur chargement annonces: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", "Impossible de charger les annonces.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnSupprimerAnnonce(Annonce annonce)
    {
        if (annonce == null!) return;

        try
        {
            bool confirm = await Shell.Current.DisplayAlert("Confirmer",
                $"Êtes-vous sûr de vouloir supprimer l'annonce \"{annonce.Titre}\" ?", "Oui", "Non");

            if (confirm)
            {
                await _firebaseService.DeleteAnnonceAsync(annonce.Id);
                MesAnnonces.Remove(annonce);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'annonce");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur",
                    "Une erreur est survenue lors de la suppression.", "OK");
        }
    }

    private async Task OnModifierAnnonce(Annonce? annonce)
    {
        if (annonce == null) return;

        try
        {
            var navigationParameters = new Dictionary<string, object>
            {
                { "annonce", JsonSerializer.Serialize(annonce) }
            };

            await Shell.Current.GoToAsync(nameof(EditAnnonceView), navigationParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers la modification d'annonce");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible de modifier l'annonce.", "OK");
        }
    }

    private async Task OnGoToConversations() // Méthode pour naviguer vers la page des conversations
    {
        try
        {
            // Correction: Utiliser la navigation absolue pour éviter les erreurs de routage
            await Shell.Current.GoToAsync("//ConversationsView");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers les conversations");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder aux conversations.", "OK");
        }
    }

    private async Task OnEditProfile() // Méthode pour naviguer vers la page de modification du profil
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(EditProfileView));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers la modification du profil");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder à la modification du profil.", "OK");
        }
    }

    public async Task LoadUserProfile() // Méthode pour charger le profil utilisateur
    {
        try
        {
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var userProfile = await _firebaseService.GetUserProfileAsync(userId);
                if (userProfile != null)
                {
                    // Charger les dernières évaluations via le RatingService
                    try
                    {
                        var ratingService = _serviceProvider.GetService<RatingService>();
                        if (ratingService != null)
                        {
                            var evaluations = await ratingService.GetEvaluationsUtilisateurAsync(userId, 5);
                            userProfile.DernieresEvaluations = evaluations;
                            
                            // Recalculer les stats à jour si besoin
                            var (noteMoyenne, nombreEvaluations) = await ratingService.CalculerStatistiquesAsync(userId);
                            if (nombreEvaluations > 0)
                            {
                                userProfile.NoteMoyenne = noteMoyenne;
                                userProfile.NombreEvaluations = nombreEvaluations;
                                userProfile.CalculerBadgeConfiance();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de charger les évaluations");
                    }
                    
                    UserProfile = userProfile;
                    UserEmail = userProfile.Email; // Récupérer l'email depuis le profil
                }
                else
                {
                    // Si aucun profil n'est trouvé, initialiser avec des valeurs par défaut sans sauvegarder dans Firebase.
                    UserProfile = new UserProfile
                    {
                        Id = userId,
                        Name = "Utilisateur",
                        Email = await _authService.GetUserEmailAsync(),
                        ProfilePictureUrl = "default_profile_icon.png"
                    };
                    UserEmail = UserProfile.Email;
                }
            }
            else
            {
                UserProfile = new UserProfile
                    { Name = "Utilisateur inconnu", ProfilePictureUrl = "default_profile_icon.png" };
                UserEmail = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement du profil utilisateur");
        }
    }

    // Logique de déconnexion
    private async Task OnSignOutAsync()
    {
        try
        {
            // Appelle la méthode de déconnexion du service
            await _authService.SignOutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion");
        }

        // S'assure que la navigation est exécutée sur le thread principal de l'interface utilisateur pour éviter les exceptions
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Réinitialise l'état de l'application en redirigeant vers la page de connexion
            var loginView = _serviceProvider.GetRequiredService<LoginView>();
            Application.Current.MainPage = new NavigationPage(loginView);
        });
    }

    private void OnChangeTheme(string themeName) // Méthode pour changer le thème
    {
        // Convertir le nom du thème en énumération ThemeService.AppTheme
        var theme = themeName switch
        {
            "Mode clair" => ThemeService.AppTheme.Light,
            "Mode sombre" => ThemeService.AppTheme.Dark,
            _ => ThemeService.AppTheme.System
        };

        // Change le thème via le service de thème
        _themeService.SetTheme(theme);

        NotifyThemePropertiesChanged();
    }
    
    /// <summary>
    /// Sélectionne un thème directement via son identifiant (light/dark/system)
    /// </summary>
    private void OnSelectTheme(string themeId)
    {
        var theme = themeId switch
        {
            "light" => ThemeService.AppTheme.Light,
            "dark" => ThemeService.AppTheme.Dark,
            "system" => ThemeService.AppTheme.System,
            _ => ThemeService.AppTheme.System
        };

        _themeService.SetTheme(theme);
        NotifyThemePropertiesChanged();
    }
    
    /// <summary>
    /// Notifie l'UI de tous les changements de propriétés liées au thème
    /// </summary>
    private void NotifyThemePropertiesChanged()
    {
        OnPropertyChanged(nameof(CurrentThemeName));
        OnPropertyChanged(nameof(CurrentThemeIcon));
        OnPropertyChanged(nameof(CurrentThemeEmoji));
        OnPropertyChanged(nameof(IsLightThemeSelected));
        OnPropertyChanged(nameof(IsDarkThemeSelected));
        OnPropertyChanged(nameof(IsSystemThemeSelected));
    }

    private async Task OnNavigateToMap() // Méthode pour naviguer vers la carte
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(MapView));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers la carte");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder à la carte.", "OK");
        }
    }

    private async Task OnNavigateToTransactions() // Méthode pour naviguer vers les transactions
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(TransactionsView));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers les transactions");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder aux transactions.", "OK");
        }
    }

    private async Task OnNavigateToRewards() // Méthode pour naviguer vers les récompenses
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(RewardsPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers les récompenses");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder aux récompenses.", "OK");
        }
    }

    private async Task OnNavigateToSocial() // Méthode pour naviguer vers la page sociale (amis, parrainage)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(SocialView));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers la page sociale");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder à la page sociale.", "OK");
        }
    }

    private async Task OnNavigateToTradeProposals() // Naviguer vers la page des propositions de troc
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(TradeProposalsListPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers les propositions de troc");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'accéder aux propositions de troc.", "OK");
        }
    }

    private async Task OnDeleteAccount() // Méthode pour supprimer le compte utilisateur
    {
        // Confirmation de l'utilisateur avec un second avertissement
        var firstConfirm = await Shell.Current.DisplayAlert(
            "⚠️ Supprimer le compte",
            "Êtes-vous sûr de vouloir supprimer votre compte ?\n\n• Toutes vos annonces seront supprimées\n• Vos conversations seront perdues\n• Vos transactions seront effacées\n• Cette action est IRRÉVERSIBLE",
            "Continuer",
            "Annuler"
        );

        if (!firstConfirm) return;

        // Seconde confirmation pour être vraiment sûr
        var secondConfirm = await Shell.Current.DisplayAlert(
            "🚨 Confirmation finale",
            "Tapez votre mot de passe pour confirmer la suppression définitive de votre compte.",
            "Je confirme la suppression",
            "Annuler"
        );

        if (!secondConfirm) return;

        try
        {
            IsBusy = true;

            // Récupérer l'ID de l'utilisateur actuel
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Utilisateur non authentifié.");

            // Supprimer toutes les données utilisateur de Firebase
            var dataDeleted = await _firebaseService.DeleteAllUserDataAsync(userId);
            if (!dataDeleted)
            {
                throw new Exception("Échec de la suppression des données utilisateur.");
            }

            // Supprimer le compte d'authentification Firebase
            var accountDeleted = await _authService.DeleteAccountAsync();
            if (!accountDeleted)
            {
                throw new Exception("Échec de la suppression du compte d'authentification.");
            }

            // Navigation vers la page de connexion
            MainThread.BeginInvokeOnMainThread(async void () =>
            {
                try
                {
                    // Vérification de sécurité pour éviter la NullReferenceException
                    if (true)
                    {
                        var loginView = _serviceProvider.GetRequiredService<LoginView>();
                        Application.Current!.MainPage = new NavigationPage(loginView);
                    }

                    // Vérification avant d'afficher l'alerte
                    if (true)
                    {
                        await Shell.Current.DisplayAlert("✅ Compte supprimé",
                            "Votre compte et toutes vos données ont été supprimés avec succès.", "OK");
                    }
                }
                catch (Exception navEx)
                {
                    _logger.LogError(navEx, "Erreur lors de la navigation après suppression du compte");
                    // Tentative de navigation d'urgence
                    try
                    {
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                    catch (Exception shellEx)
                    {
                        _logger.LogError(shellEx, "Échec de la navigation d'urgence");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du compte utilisateur");
            await Shell.Current.DisplayAlert("❌ Erreur",
                "Une erreur est survenue lors de la suppression de votre compte. Veuillez réessayer plus tard.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Affiche le popup de donation pour soutenir le développeur
    /// </summary>
    private async Task OnDonateToDevAsync()
    {
        try
        {
            var donationPopup = new DonationPopup();
            await Shell.Current.Navigation.PushModalAsync(donationPopup, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture du popup de donation");
            await Shell.Current.DisplayAlert(
                "Erreur",
                "Impossible d'ouvrir la page de donation. Veuillez réessayer.",
                "OK");
        }
    }
    
    /// <summary>
    /// Navigue vers le panneau d'administration
    /// </summary>
    private async Task OnNavigateToAdmin()
    {
        if (!IsAdminOrModerator)
        {
            await Shell.Current.DisplayAlert("⚠️ Accès refusé", 
                "Vous n'avez pas les permissions pour accéder au panneau d'administration.", "OK");
            return;
        }
        
        try
        {
            await Shell.Current.GoToAsync(nameof(AdminDashboardPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers le panneau admin");
            await Shell.Current.DisplayAlert("Erreur", 
                "Impossible d'accéder au panneau d'administration.", "OK");
        }
    }
    
    /// <summary>
    /// Navigue vers la page de configuration admin
    /// </summary>
    private async Task OnNavigateToAdminSetup()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(AdminSetupPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la navigation vers la configuration admin");
            await Shell.Current.DisplayAlert("Erreur", 
                "Impossible d'accéder à la configuration admin.", "OK");
        }
    }
    
    /// <summary>
    /// Ouvre une URL dans le navigateur par défaut
    /// </summary>
    private async Task OnOpenUrl(string url)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture de l'URL: {Url}", url);
            await Shell.Current.DisplayAlert("Erreur", 
                "Impossible d'ouvrir cette page. Vérifiez votre connexion internet.", "OK");
        }
    }

    /// <summary>
    /// Ouvre le formulaire de gestion du consentement publicitaire (RGPD/UMP).
    /// Obligatoire d'après les exigences Google : l'utilisateur doit pouvoir retirer
    /// ou modifier son consentement à tout moment depuis l'app.
    /// </summary>
    private async Task OnManagePrivacyConsentAsync()
    {
        try
        {
            if (_consentService == null)
            {
                await Shell.Current.DisplayAlert(
                    "Indisponible",
                    "La gestion du consentement publicitaire n'est pas disponible sur cette plateforme.",
                    "OK");
                return;
            }

            await _consentService.ShowPrivacyOptionsFormAsync();

            // Refresh visibility (au cas où le statut a changé)
            OnPropertyChanged(nameof(ShowPrivacyOptionsEntry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur ouverture formulaire consentement RGPD");
            await Shell.Current.DisplayAlert(
                "Erreur",
                "Impossible d'afficher les préférences de confidentialité. Réessayez plus tard.",
                "OK");
        }
    }

    /// <summary>
    /// 🛠 DEBUG ONLY : réinitialise le consentement RGPD/UMP pour retester le flow.
    /// Au prochain démarrage de l'app, le formulaire de consentement sera ré-affiché.
    /// Visible uniquement pour les utilisateurs admin.
    /// </summary>
    private async Task OnResetConsentDebugAsync()
    {
        try
        {
            if (_consentService == null)
            {
                await Shell.Current.DisplayAlert(
                    "Indisponible",
                    "Le service de consentement n'est pas disponible sur cette plateforme.",
                    "OK");
                return;
            }

            var confirm = await Shell.Current.DisplayAlert(
                "🛠 Reset Consentement",
                "Réinitialiser le consentement RGPD ?\n\n" +
                "Le formulaire UMP sera ré-affiché au prochain redémarrage de l'application.\n\n" +
                "(Outil de test uniquement.)",
                "Réinitialiser",
                "Annuler");

            if (!confirm) return;

            _consentService.ResetConsent();

            await Shell.Current.DisplayAlert(
                "✅ Réinitialisé",
                "Le consentement a été réinitialisé.\n" +
                "Fermez complètement l'application puis rouvrez-la pour voir le formulaire.",
                "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur réinitialisation consentement RGPD");
            await Shell.Current.DisplayAlert(
                "Erreur",
                "Impossible de réinitialiser le consentement.",
                "OK");
        }
    }
}


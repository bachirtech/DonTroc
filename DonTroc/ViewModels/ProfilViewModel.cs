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
    private readonly PremiumFeaturesViewModel _premiumFeaturesViewModel; // Service pour les fonctionnalités premium
    private readonly ThemeService _themeService; // Service de gestion des thèmes
    private readonly ILogger<ProfilViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    private string _userEmail;
    public string UserEmail // Propriété pour stocker l'email de l'utilisateur
    {
        get => _userEmail;
        set => SetProperty(ref _userEmail, value);
    }
    
    private UserProfile _userProfile;
   

    public UserProfile UserProfile
    {
        get => _userProfile;
        private set => SetProperty(ref _userProfile, value);
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
    
    // Propriétés pour le sélecteur de thème
    public string CurrentThemeName => _themeService.GetThemeDisplayName();
    public string CurrentThemeIcon => _themeService.GetThemeIcon();
    public List<string> AvailableThemes { get; } = new List<string> { "Mode clair", "Mode sombre", "Automatique" };
    
    // Commande pour changer le thème
    public ICommand ChangeThemeCommand { get; }
    
    // Commande pour supprimer le compte utilisateur
    public ICommand DeleteAccountCommand { get; }
    

    // Constructeur avec injection des services uniquement
    public ProfilViewModel(AuthService authService, FirebaseService firebaseService, PremiumFeaturesViewModel premiumFeaturesViewModel, ThemeService themeService, ILogger<ProfilViewModel> logger, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _firebaseService = firebaseService;
        _premiumFeaturesViewModel = premiumFeaturesViewModel; // Injection du ViewModel premium
        _themeService = themeService; // Injection du service de gestion des thèmes
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Initialisation avec des valeurs par défaut
        _userEmail = string.Empty;
        _userProfile = new UserProfile
        {
            Name = string.Empty, // Nom de l'utilisateur, initialisé vide
            Email = string.Empty, // Email de l'utilisateur, initialisé vide
            ProfilePictureUrl = "default_profile_icon.png" // URL par défaut pour l'icône de profil
        };

        MesAnnonces = new ObservableCollection<Annonce>();

        SignOutCommand = new Command(OnSignOut);
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
        // Initialisation des nouvelles commandes pour l'accès rapide
        NavigateToMapCommand = new Command(async void () => await OnNavigateToMap());
        NavigateToTransactionsCommand = new Command(async void () => await OnNavigateToTransactions());
        // Initialisation de la commande pour supprimer le compte
        DeleteAccountCommand = new Command(async void () => await OnDeleteAccount());

        _ = LoadUserProfile();
        // Exécute la commande pour charger les annonces au démarrage du ViewModel
        LoadMesAnnoncesCommand.Execute(null);
    }

    private async Task OnBoostAnnonce(Annonce? annonce) // Méthode pour booster une annonce
    {
        if (annonce == null) return;

        // Demander confirmation à l'utilisateur
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
                await Shell.Current.DisplayAlert("Succès !", "Votre annonce a été boostée et sera mise en avant.", "Génial !");
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
            System.Diagnostics.Debug.WriteLine("[ProfilViewModel] Utilisateur non authentifié, chargement des annonces ignoré.");
            // Silencieux: ne pas afficher d'alerte pour ce cas
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
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

    private async Task OnSupprimerAnnonce(Annonce annonce) // Méthode pour supprimer une annonce
    {
        if (annonce == null!) return;

        // Demande de confirmation à l'utilisateur
        bool confirm = await Application.Current?.MainPage?.DisplayAlert("Confirmer", $"Êtes-vous sûr de vouloir supprimer l'annonce \"{annonce.Titre}\" ?", "Oui", "Non")!;

        if (confirm)
        {
            try
            {
                // Supprime l'annonce de Firebase
                await _firebaseService.DeleteAnnonceAsync(annonce.Id);
                // Supprime l'annonce de la liste affichée
                MesAnnonces.Remove(annonce);
            }
            catch (Exception )
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", "Une erreur est survenue lors de la suppression.", "OK");
            }
        }
    }

    private async Task OnModifierAnnonce(Annonce? annonce) // Méthode pour modifier une annonce
    {
        if (annonce == null) return;

        // Prépare les données de l'annonce pour la navigation
        var navigationParameters = new Dictionary<string, object>
        {
            { "annonce", JsonSerializer.Serialize(annonce) }
        };

        // Navigue vers la page de modification en passant l'annonce en paramètre
        await Shell.Current.GoToAsync(nameof(EditAnnonceView), navigationParameters);
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
            await Application.Current.MainPage.DisplayAlert("Erreur", "Impossible d'accéder aux conversations.", "OK");
        }
    }

    private async Task OnEditProfile() // Méthode pour naviguer vers la page de modification du profil
    {
        await Shell.Current.GoToAsync(nameof(EditProfileView));
    }

    public async Task LoadUserProfile() // Méthode pour charger le profil utilisateur
    {
        var userId = _authService.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            var userProfile = await _firebaseService.GetUserProfileAsync(userId);
            if (userProfile != null)
            {
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
                    Email = await _authService.GetUserEmailAsync() ?? string.Empty, 
                    ProfilePictureUrl = "default_profile_icon.png" 
                };
                UserEmail = UserProfile.Email;
            }
        }
        else
        {
            UserProfile = new UserProfile { Name = "Utilisateur inconnu", ProfilePictureUrl = "default_profile_icon.png" };
            UserEmail = string.Empty;
        }
    }

    // Logique de déconnexion
    private void OnSignOut()
    {
        // Appelle la méthode de déconnexion du service
        _authService.SignOut();

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

        // Notifie que les propriétés ont changé pour mettre à jour l'interface utilisateur
        OnPropertyChanged(nameof(CurrentThemeName));
        OnPropertyChanged(nameof(CurrentThemeIcon));
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
            await Application.Current.MainPage.DisplayAlert("Erreur", "Impossible d'accéder à la carte.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Erreur", "Impossible d'accéder aux transactions.", "OK");
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
                        await Shell.Current.DisplayAlert("✅ Compte supprimé", "Votre compte et toutes vos données ont été supprimés avec succès.", "OK");
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
            await Shell.Current.DisplayAlert("❌ Erreur", "Une erreur est survenue lors de la suppression de votre compte. Veuillez réessayer plus tard.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

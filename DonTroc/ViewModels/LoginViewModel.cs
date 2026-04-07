using System;
using System.Threading.Tasks;
using DonTroc.Services;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace DonTroc.ViewModels;

// ViewModel pour la page de connexion/inscription
public class LoginViewModel : BaseViewModel
{
    private string _email;
    private string _password;
    private bool _rememberMe;
    private bool _isPasswordVisible = false; // Nouvelle propriété pour la visibilité du mot de passe
    private readonly AuthService _authService;
    private readonly UnreadMessageService _unreadMessageService;
    private readonly GamificationService _gamificationService; // Service de gamification
    private readonly GlobalNotificationService _globalNotificationService; // Service de notifications en temps réel

    // Clés pour SecureStorage (mêmes que dans AuthService)
    private const string EmailKey = "user_email";
    private const string PasswordKey = "user_password";
    private const string RememberMeKey = "remember_me";

    // Propriété pour l'adresse e-mail
    public string Email
    {
        get => _email;
        set
        {
            SetProperty(ref _email, value);
            ValidateEmail();
        }
    }

    // Propriété pour le mot de passe
    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            ValidatePassword();
        }
    }

    public bool RememberMe // Proprieté pour se souvenir de la connexion
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    /// <summary>
    /// Propriété pour gérer la visibilité du mot de passe
    /// </summary>
    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }

    private bool _isEmailValid;

    private bool IsEmailValid // Proprieté si le mail est valide
    {
        get => _isEmailValid;
        set => SetProperty(ref _isEmailValid, value);
    }

    private bool _isPasswordValid;
    public bool IsPasswordValid // Proprieté si le mot de passe est valide
    {
        get => _isPasswordValid;
        set => SetProperty(ref _isPasswordValid, value);
    }

    private string _emailErrorMessage;
    public string EmailErrorMessage // Proprieté pour les erreurs de validation
    {
        get => _emailErrorMessage;
        set => SetProperty(ref _emailErrorMessage, value);
    }

    private string _passwordErrorMessage;
    public string PasswordErrorMessage // Proprieté pour les erreurs de validation
    {
        get => _passwordErrorMessage;
        set => SetProperty(ref _passwordErrorMessage, value);
    }

    private bool _isBusy;
    public new bool IsBusy // Proprieté pour le bouton de connexion s'il est en cours de chargement
    {
        get => _isBusy;
        set
        {
            SetProperty(ref _isBusy, value);
            OnPropertyChanged(nameof(IsNotBusy));
            (SignInCommand as Command)?.ChangeCanExecute();
            (SignUpCommand as Command)?.ChangeCanExecute();
        }
    }
    public new bool IsNotBusy => !IsBusy; // Preprieté pour le bouton de connexion s'il n'est pas en cours de chargement

    // Commandes pour la connexion et l'inscription
    public ICommand SignInCommand { get; }
    public ICommand SignUpCommand { get; }
    public ICommand ForgotPasswordCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; } // Nouvelle commande pour basculer la visibilité
    // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
    // public ICommand SignInWithGoogleCommand { get; } // Commande pour connexion Google SSO
    public ICommand InitializeCommand { get; } // Commande pour charger les identifiants au démarrage

    // Constructeur avec injection du service d'authentification uniquement
    public LoginViewModel(AuthService authService, UnreadMessageService unreadMessageService, GamificationService gamificationService, GlobalNotificationService globalNotificationService)
    {
        _authService = authService;
        _unreadMessageService = unreadMessageService;
        _gamificationService = gamificationService;
        _globalNotificationService = globalNotificationService;
        _email = string.Empty; // Initialisation avec valeur par défaut
        _password = string.Empty; // Initialisation avec valeur par défaut
        _emailErrorMessage = string.Empty;
        _passwordErrorMessage = string.Empty;
        
        SignInCommand = new Command(ExecuteSignInCommand, CanSignIn);
        SignUpCommand = new Command(ExecuteSignUpCommand, CanSignIn);
        ForgotPasswordCommand = new Command(ExecuteForgotPasswordCommand);
        TogglePasswordVisibilityCommand = new Command(ExecuteTogglePasswordVisibility); // Initialisation de la nouvelle commande
        // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
        // SignInWithGoogleCommand = new Command(ExecuteSignInWithGoogleCommand); // Initialisation connexion Google
        InitializeCommand = new Command(async () => await LoadSavedCredentialsAsync()); // Charger les identifiants sauvegardés
        
        // Initialiser les validations
        ValidateEmail();
        ValidatePassword();
        
        // Charger automatiquement les identifiants sauvegardés si "Se souvenir de moi" était activé
        _ = LoadSavedCredentialsAsync();
    }

    /// <summary>
    /// Charge les identifiants sauvegardés si l'utilisateur avait coché "Se souvenir de moi"
    /// </summary>
    private async Task LoadSavedCredentialsAsync()
    {
        try
        {
            var rememberMeValue = await SecureStorage.GetAsync(RememberMeKey);
            if (rememberMeValue == "true")
            {
                var savedEmail = await SecureStorage.GetAsync(EmailKey);
                var savedPassword = await SecureStorage.GetAsync(PasswordKey);

                if (!string.IsNullOrEmpty(savedEmail))
                {
                    Email = savedEmail;
                }
                
                if (!string.IsNullOrEmpty(savedPassword))
                {
                    Password = savedPassword;
                }

                RememberMe = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur lors du chargement des identifiants: {ex.Message}");
        }
    }

    // Condition pour activer le bouton de connexion
    private bool CanSignIn() // Proprieté pour activer le bouton de connexion
    {
        return IsEmailValid && IsPasswordValid && !IsBusy;
    }

    private void ValidateEmail() // Methode pour valider l'email'
    {
        IsEmailValid = !string.IsNullOrWhiteSpace(Email) && Email.Contains("@") && Email.Contains(".");
        EmailErrorMessage = IsEmailValid ? string.Empty : "Email invalide";
        (SignInCommand as Command)?.ChangeCanExecute();
        (SignUpCommand as Command)?.ChangeCanExecute();
    }

    private void ValidatePassword() // Méthode pour valider le mot de passe
    {
        IsPasswordValid = !string.IsNullOrWhiteSpace(Password) && Password.Length >= 6;
        PasswordErrorMessage = IsPasswordValid ? string.Empty : "Le mot de passe doit contenir au moins 6 caractères";
        (SignInCommand as Command)?.ChangeCanExecute();
        (SignUpCommand as Command)?.ChangeCanExecute();
    }

    // Logique de connexion
    private async Task OnSignIn()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var userCredential = await _authService.SignInAsync(Email, Password, RememberMe);
            if (userCredential != null)
            {
                // GAMIFICATION: Enregistrer la connexion quotidienne pour les streaks
                try
                {
                    var userId = userCredential?.Uid;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await _gamificationService.OnUserActionAsync(userId, "daily_login");
                    }
                }
                catch (Exception gamEx)
                {
                    // Ne pas faire échouer la connexion pour une erreur de gamification
                    System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur gamification: {gamEx.Message}");
                }

                // Initialiser les notifications en temps réel
                try
                {
                    await _globalNotificationService.InitializeAsync();
                }
                catch (Exception notifEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur notifications: {notifEx.Message}");
                }
                
                Application.Current.MainPage = new AppShell(_unreadMessageService);
                
                // Navigation explicite si Shell.Current est disponible
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//MainApp");
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", "Connexion échouée. Vérifiez vos identifiants.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", $"Une erreur est survenue lors de la connexion : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExecuteSignInCommand() // Methode de commande pour la connexion
    {
        try
        {
            await OnSignIn();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur lors de la connexion : {e.Message}");
        }
    }
    private async void ExecuteSignUpCommand() // Methode de commande pour l'inscription'
    {
        try
        {
            await OnSignUp();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur lors de l'inscription : {e.Message}");
        }
    }
    private async void ExecuteForgotPasswordCommand() // Methode de commande mot de passe oublié
    {
        try
        {
            await OnForgotPassword();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Mot de Passe Oublié : {e.Message} ");
        }
    }

    // Logique d'inscription
    private async Task OnSignUp()
    {
        if (IsBusy) return;
        IsBusy = true;

        var success = await _authService.SignUpAsync(Email, Password);
        if (success)
        {
            await Application.Current?.MainPage?.DisplayAlert("Succès", "Votre compte a été créé. Vous pouvez maintenant vous connecter.", "OK")!;
        }
        else
        {
            await Application.Current?.MainPage?.DisplayAlert("Erreur", "Impossible de créer le compte. L'email est peut-être déjà utilisé.", "OK")!;
        }

        IsBusy = false;
    }

    // Logique de réinitialisation du mot de passe
    private async Task OnForgotPassword()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            await Shell.Current.DisplayAlert("Email requis", 
                "Veuillez saisir votre adresse e-mail avant de réinitialiser votre mot de passe.", "OK");
            return;
        }

        // Normaliser l'email
        var normalizedEmail = Email.Trim().ToLowerInvariant();
        
        // Validation basique de l'email
        if (!normalizedEmail.Contains('@') || !normalizedEmail.Contains('.'))
        {
            await Shell.Current.DisplayAlert("Email invalide", 
                "Veuillez saisir une adresse e-mail valide (ex: exemple@email.com).", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            await _authService.SendPasswordResetEmailAsync(normalizedEmail);
            
            await Shell.Current.DisplayAlert("Email envoyé ✉️", 
                $"Un e-mail de réinitialisation a été envoyé à :\n{normalizedEmail}\n\n" +
                "📬 Vérifiez votre boîte de réception.\n\n" +
                "Si vous ne le trouvez pas :\n" +
                "• Vérifiez le dossier spam/courrier indésirable\n" +
                "• Attendez quelques minutes\n" +
                "• Assurez-vous que l'adresse email est correcte", "OK");
        }
        catch (ArgumentException argEx)
        {
            await Shell.Current.DisplayAlert("Erreur de saisie", argEx.Message, "OK");
        }
        catch (InvalidOperationException opEx)
        {
            await Shell.Current.DisplayAlert("Échec de l'envoi", opEx.Message, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur reset password: {ex.Message}");
            
            await Shell.Current.DisplayAlert("Erreur", 
                "Une erreur inattendue est survenue lors de l'envoi de l'email de réinitialisation.\n\n" +
                "Veuillez réessayer dans quelques instants ou contacter le support si le problème persiste.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Méthode pour basculer la visibilité du mot de passe
    /// </summary>
    private void ExecuteTogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    /// <summary>
    /// Méthode de commande pour la connexion avec Google
    /// </summary>
    // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
    /*
    private async void ExecuteSignInWithGoogleCommand()
    {
        try
        {
            await OnSignInWithGoogle();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur lors de la connexion Google : {e.Message}");
        }
    }

    /// <summary>
    /// Logique de connexion avec Google SSO
    /// </summary>
    private async Task OnSignInWithGoogle()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var userCredential = await _authService.SignInWithGoogleAsync();
            if (userCredential != null)
            {
                // GAMIFICATION: Enregistrer la connexion quotidienne pour les streaks
                try
                {
                    var userId = userCredential?.Uid;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await _gamificationService.OnUserActionAsync(userId, "daily_login");
                    }
                }
                catch (Exception gamEx)
                {
                    // Ne pas faire échouer la connexion pour une erreur de gamification
                    System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Erreur gamification: {gamEx.Message}");
                }
                
                Application.Current.MainPage = new AppShell(_unreadMessageService);
                
                // Navigation explicite si Shell.Current est disponible
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//MainApp");
                }
            }
            // Pas de message d'erreur ici car SignInWithGoogleAsync gère déjà les cas d'annulation
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", $"Une erreur est survenue lors de la connexion Google : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    */
}

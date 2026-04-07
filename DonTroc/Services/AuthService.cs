using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Firebase.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Core.Exceptions;

namespace DonTroc.Services;

public class AuthService : ObservableObject
{
    private readonly IFirebaseAuth _client;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordValidationService _passwordValidationService;
    private const string EmailKey = "user_email";
    private const string PasswordKey = "user_password";
    private const string RememberMeKey = "remember_me";

    public AuthService(ILogger<AuthService> logger, PasswordValidationService passwordValidationService)
    {
        _logger = logger;
        _passwordValidationService = passwordValidationService;
        _client = CrossFirebaseAuth.Current;
    }

    private IFirebaseUser? CurrentUser => _client.CurrentUser;

    public async Task<IFirebaseUser?> GetCurrentUserAsync()
    {
        try
        {
            if (CurrentUser == null)
            {
                _logger.LogInformation("Aucun utilisateur connecté, tentative de reconnexion automatique...");
                var autoLoginSuccess = await TryAutoSignInAsync();
                if (autoLoginSuccess)
                {
                    _logger.LogInformation("Reconnexion automatique réussie: {UserId}", CurrentUser?.Uid);
                    return CurrentUser;
                }
                _logger.LogWarning("Reconnexion automatique échouée");
                return null;
            }

            try
            {
                var tokenResult = await CurrentUser.GetIdTokenResultAsync();
                if (!string.IsNullOrEmpty(tokenResult?.Token)) return CurrentUser;
                
                _logger.LogInformation("Token vide, tentative de rafraîchissement...");
                await RefreshAuthAsync();
                return CurrentUser;
            }
            catch (Exception tokenEx)
            {
                _logger.LogWarning(tokenEx, "Erreur de token, tentative de rafraîchissement...");
                var refreshSuccess = await RefreshAuthAsync();
                return refreshSuccess ? CurrentUser : null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dans GetCurrentUserAsync");
            return null;
        }
    }

    public string? GetUserId()
    {
        return CurrentUser?.Uid;
    }

    public bool IsSignedIn => _client.CurrentUser != null;

    public async Task<bool> SignUpAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Tentative d'inscription pour: {Email}", email);
            
            // Valider le mot de passe avant de procéder à l'inscription
            var passwordValidation = _passwordValidationService.ValidatePassword(password);
            if (!passwordValidation.IsValid)
            {
                _logger.LogWarning("Échec inscription pour {Email} - Mot de passe non conforme: {Errors}", 
                    email, string.Join(", ", passwordValidation.Errors));
                
                // Afficher un message d'erreur détaillé à l'utilisateur
                await ShowPasswordValidationErrorAlert(passwordValidation);
                return false;
            }

            await _client.CreateUserAsync(email, password);

            // Sauvegarder les identifiants pour "Se souvenir de moi" après inscription
            await SecureStorage.SetAsync(EmailKey, email);
            await SecureStorage.SetAsync(PasswordKey, password);
            await SecureStorage.SetAsync(RememberMeKey, "true");

            _logger.LogInformation("Inscription réussie pour: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec inscription pour {Email}", email);
            await ShowAuthErrorAlert(ex.Message);
            return false;
        }
    }

    public async Task<IFirebaseUser?> SignInAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            _logger.LogInformation("Tentative de connexion pour: {Email}", email);
            var authResult = await _client.SignInWithEmailAndPasswordAsync(email, password);

            if (rememberMe)
            {
                await SecureStorage.SetAsync(EmailKey, email);
                await SecureStorage.SetAsync(PasswordKey, password);
                await SecureStorage.SetAsync(RememberMeKey, "true");
                _logger.LogInformation("Identifiants sauvegardés pour reconnexion automatique.");
            }
            else
            {
                SecureStorage.Remove(EmailKey);
                SecureStorage.Remove(PasswordKey);
                SecureStorage.Remove(RememberMeKey);
            }

            _logger.LogInformation("Connexion réussie pour: {Email}", email);
            return authResult.Uid != null ? authResult : null;
        }
        catch (Exception ex) when (ex.GetBaseException() is FirebaseException)
        {
            _logger.LogError(ex, "Échec de la connexion pour {Email}", email);
            await ShowAuthErrorAlert(ex.GetBaseException().Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de la connexion pour {Email}", email);
            await ShowGenericErrorAlert("Une erreur inattendue est survenue.");
            return null;
        }
    }

    public void SignOut()
    {
        _logger.LogInformation("Déconnexion de l'utilisateur.");
        _client.SignOutAsync();
        SecureStorage.Remove(EmailKey);
        SecureStorage.Remove(PasswordKey);
        SecureStorage.Remove(RememberMeKey);
    }

    // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
    /*
    /// <summary>
    /// Authentification avec Google (Single Sign-On)
    /// </summary>
    /// <returns>L'utilisateur Firebase authentifié ou null en cas d'échec</returns>
    public async Task<IFirebaseUser?> SignInWithGoogleAsync()
    {
        try
        {
            _logger.LogInformation("Tentative de connexion avec Google...");
            
#if ANDROID
            GoogleAuthService? googleAuthService = null;
            
            googleAuthService = Application.Current?.Handler?.MauiContext?.Services.GetService<GoogleAuthService>();
            
            if (googleAuthService == null)
            {
                var mauiContext = Application.Current?.Windows?.FirstOrDefault()?.Handler?.MauiContext;
                googleAuthService = mauiContext?.Services.GetService<GoogleAuthService>();
            }
            
            if (googleAuthService != null)
            {
                var authResult = await googleAuthService.SignInAndAuthenticateWithFirebaseAsync();
                
                if (authResult != null)
                {
                    _logger.LogInformation("Connexion Google réussie: {Email}", authResult.Email);
                    await SecureStorage.SetAsync("auth_provider", "google");
                    return authResult;
                }
                else
                {
                    _logger.LogWarning("Échec auth Firebase avec Google - authResult null");
                    await ShowGenericErrorAlert("La connexion Google a échoué. Veuillez vérifier que vous avez sélectionné un compte Google valide.");
                    return null;
                }
            }
            else
            {
                _logger.LogError("Service Google Auth non disponible");
                await ShowGenericErrorAlert("Le service de connexion Google n'est pas disponible. Veuillez redémarrer l'application.");
            }
#else
            await ShowGenericErrorAlert("La connexion Google n'est disponible que sur Android pour le moment.");
#endif
            
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Connexion Google annulée par l'utilisateur");
            return null;
        }
        catch (Exception ex) when (ex.GetBaseException() is FirebaseException firebaseEx)
        {
            _logger.LogError(ex, "Erreur Firebase connexion Google: {Message}", firebaseEx.Message);
            await ShowAuthErrorAlert(firebaseEx.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur connexion Google: {Message}", ex.Message);
            await ShowGenericErrorAlert($"Une erreur est survenue lors de la connexion avec Google: {ex.Message}");
            return null;
        }
    }
    */

    /// <summary>
    /// Vérifie si l'utilisateur est connecté via Google
    /// </summary>
    public async Task<bool> IsGoogleUserAsync()
    {
        try
        {
            var provider = await SecureStorage.GetAsync("auth_provider");
            return provider == "google";
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            if (!IsSignedIn)
            {
                _logger.LogWarning("Aucun utilisateur connecté pour récupérer le token.");
                return null;
            }

            var tokenResult = await CurrentUser?.GetIdTokenResultAsync(true)!;
            _logger.LogInformation("Token récupéré avec succès.");
            return tokenResult.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération token.");
            return null;
        }
    }

    public async Task<bool> TryAutoSignInAsync()
    {
        try
        {
            if (IsSignedIn)
            {
                _logger.LogInformation("Utilisateur déjà connecté.");
                return true;
            }

            var email = await SecureStorage.GetAsync(EmailKey);
            var password = await SecureStorage.GetAsync(PasswordKey);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _logger.LogInformation("Aucuns identifiants stockés pour reconnexion automatique.");
                return false;
            }

            _logger.LogInformation("Tentative de connexion automatique avec l'email: {Email}", email);
            var authResult = await _client.SignInWithEmailAndPasswordAsync(email, password);

            var success = authResult != null;
            _logger.LogInformation("Connexion automatique {Status}", success ? "réussie" : "échouée");
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur lors de la connexion automatique. Les identifiants stockés vont être effacés.");
            SecureStorage.Remove(EmailKey);
            SecureStorage.Remove(PasswordKey);
            return false;
        }
    }

    public async Task<bool> RefreshAuthAsync()
    {
        if (!IsSignedIn)
        {
            _logger.LogWarning("Aucun utilisateur connecté pour rafraîchir.");
            return false;
        }

        try
        {
            _logger.LogInformation("Rafraîchissement du token...");
            await CurrentUser?.GetIdTokenResultAsync(true)!;
            _logger.LogInformation("Token rafraîchi avec succès.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du rafraîchissement du token.");
            _logger.LogInformation("Tentative de reconnexion après échec de rafraîchissement...");
            return await TryAutoSignInAsync();
        }
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Tentative d'envoi d'email de réinitialisation pour : {Email}", email);
            
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("L'adresse email est requise");
            }
            
            if (!email.Contains('@') || !email.Contains('.'))
            {
                throw new ArgumentException("L'adresse email n'est pas valide");
            }
            
            email = email.Trim().ToLowerInvariant();
            await _client.SendPasswordResetEmailAsync(email);
            
            _logger.LogInformation("Email de réinitialisation envoyé pour : {Email}", email);
        }
        catch (FirebaseException firebaseEx)
        {
            _logger.LogError(firebaseEx, "Erreur Firebase reset password pour {Email}", email);
            var userMessage = GetPasswordResetErrorMessage(firebaseEx.Message);
            throw new InvalidOperationException(userMessage, firebaseEx);
        }
        catch (Exception ex) when (ex.GetBaseException() is FirebaseException baseFirebaseEx)
        {
            _logger.LogError(ex, "Erreur Firebase (base) reset password pour {Email}", email);
            var userMessage = GetPasswordResetErrorMessage(baseFirebaseEx.Message);
            throw new InvalidOperationException(userMessage, ex);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur générale reset password pour {Email}", email);
            throw new InvalidOperationException(
                "Une erreur est survenue lors de l'envoi de l'email de réinitialisation. " +
                "Veuillez vérifier votre connexion internet et réessayer.", ex);
        }
    }

    /// <summary>
    /// Traduit les messages d'erreur Firebase en messages utilisateur compréhensibles
    /// </summary>
    private static string GetPasswordResetErrorMessage(string firebaseError)
    {
        return firebaseError switch
        {
            var e when e.Contains("USER_NOT_FOUND") || e.Contains("user-not-found") => 
                "Aucun compte n'existe avec cette adresse email. Veuillez vérifier l'adresse ou créer un nouveau compte.",
            var e when e.Contains("INVALID_EMAIL") || e.Contains("invalid-email") => 
                "L'adresse email n'est pas valide.",
            var e when e.Contains("TOO_MANY_REQUESTS") || e.Contains("too-many-requests") => 
                "Trop de tentatives. Veuillez attendre quelques minutes avant de réessayer.",
            var e when e.Contains("NETWORK") || e.Contains("network") => 
                "Erreur de connexion réseau. Veuillez vérifier votre connexion internet.",
            var e when e.Contains("OPERATION_NOT_ALLOWED") || e.Contains("operation-not-allowed") => 
                "L'envoi d'emails de réinitialisation n'est pas activé. Contactez le support.",
            _ => $"Erreur lors de l'envoi de l'email : {firebaseError}"
        };
    }

    public async Task<bool> EnsureAuthenticatedAsync() // méthode pour assuré la connexion
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<bool> AutoAuthenticateAsync()
    {
        return await TryAutoSignInAsync();
    }

    public async Task<string> GetUserEmailAsync() // méthode pour récupéré l'email de l'utilisateur
    {
        return await SecureStorage.GetAsync(EmailKey) ?? string.Empty;
    }

    public bool IsUserAuthenticated()
    {
        return IsSignedIn;
    }

    private async Task ShowAuthErrorAlert(string reason)
    {
        string message = reason switch
        {
            var r when r.Contains("INVALID_EMAIL") => "L'adresse e-mail n'est pas valide.",
            var r when r.Contains("WRONG_PASSWORD") => "Le mot de passe est incorrect.",
            var r when r.Contains("USER_NOT_FOUND") => "Aucun utilisateur trouvé avec cette adresse e-mail.",
            var r when r.Contains("USER_DISABLED") => "Ce compte utilisateur a été désactivé.",
            var r when r.Contains("EMAIL_EXISTS") => "Un compte existe déjà avec cette adresse e-mail.",
            var r when r.Contains("WEAK_PASSWORD") => "Le mot de passe est trop faible.",
            var r when r.Contains("TOO_MANY_ATTEMPTS_TRY_LATER") => "Trop de tentatives de connexion. Veuillez réessayer plus tard.",
            _ => "Une erreur d'authentification est survenue. Veuillez réessayer."
        };
        await ShowGenericErrorAlert(message);
    }

    /// <summary>
    /// Valide un mot de passe selon les règles de sécurité renforcées
    /// </summary>
    /// <param name="password">Le mot de passe à valider</param>
    /// <returns>Résultat de la validation</returns>
    public PasswordValidationResult ValidatePassword(string password)
    {
        return _passwordValidationService.ValidatePassword(password);
    }

    /// <summary>
    /// Obtient les règles de mot de passe pour l'affichage
    /// </summary>
    /// <returns>Liste des règles formatées</returns>
    public List<string> GetPasswordRules()
    {
        return _passwordValidationService.GetPasswordRules();
    }

    /// <summary>
    /// Affiche une alerte avec les erreurs de validation du mot de passe
    /// </summary>
    private async Task ShowPasswordValidationErrorAlert(PasswordValidationResult validation)
    {
        try
        {
            var title = "Mot de passe non conforme";
            var message = $"Votre mot de passe doit respecter les critères suivants :\n\n" +
                         $"• {string.Join("\n• ", _passwordValidationService.GetPasswordRules())}\n\n" +
                         $"Problèmes détectés :\n• {string.Join("\n• ", validation.Errors)}";

            if (validation.Suggestions.Any())
            {
                message += $"\n\nSuggestions :\n• {string.Join("\n• ", validation.Suggestions)}";
            }

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage de l'alerte de validation du mot de passe");
        }
    }

    private async Task ShowGenericErrorAlert(string message) // Méthode pour afficher une alerte d'erreur générique
    {
        if (Application.Current?.MainPage != null)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current.MainPage.DisplayAlert("Erreur", message, "OK"));
        }
    }

    public async Task<bool> DeleteAccountAsync() // Méthode pour supprimer le compte
    {
        try
        {
            if (!IsSignedIn)
            {
                _logger.LogWarning("Aucun utilisateur connecté pour supprimer le compte.");
                return false;
            }

            var userId = GetUserId();
            _logger.LogInformation("Tentative de suppression du compte pour l'utilisateur: {UserId}", userId);

            // Supprimer le compte Firebase Auth
            await CurrentUser!.DeleteAsync();

            // Nettoyer les données locales
            SecureStorage.Remove(EmailKey);
            SecureStorage.Remove(PasswordKey);

            _logger.LogInformation("Compte supprimé avec succès pour l'utilisateur: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du compte");
            await ShowGenericErrorAlert("Une erreur est survenue lors de la suppression du compte.");
            return false;
        }
    }

    public object GetCurrentUser() // Méthode de recupération de l'utilisateur courant
    {
        return CurrentUser ?? new object();
    }
}

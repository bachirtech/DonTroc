using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.Gms.Tasks;
using DonTroc.Services;
using Firebase.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Plugin.Firebase.Auth;
using Task = System.Threading.Tasks.Task;

namespace DonTroc.Platforms.Android;

/// <summary>
/// Service d'authentification Google pour Android
/// </summary>
public class GoogleAuthService : Java.Lang.Object, IOnCompleteListener
{
    private readonly ILogger<GoogleAuthService> _logger;
    private GoogleSignInClient? _googleSignInClient;
    private TaskCompletionSource<string?>? _signInCompletionSource;
    
    // Web Client ID depuis la console Firebase (client_type: 3)
    // Console Firebase > Authentication > Sign-in method > Google > Web client ID
    // IMPORTANT: C'est le Web Client ID (type 3) qui doit être utilisé pour RequestIdToken
    private const string WebClientId = "12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com";
    
    /// <summary>
    /// Vérifie si un utilisateur Google est actuellement connecté
    /// </summary>
    public bool IsSignedIn => GoogleSignIn.GetLastSignedInAccount(Platform.CurrentActivity) != null;

    public GoogleAuthService(ILogger<GoogleAuthService> logger)
    {
        _logger = logger;
        InitializeGoogleSignIn();
    }

    private void InitializeGoogleSignIn()
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                _logger.LogWarning("Activité Android non disponible pour initialiser Google Sign-In");
                return;
            }

            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken(WebClientId)
                .RequestEmail()
                .RequestProfile()
                .Build();

            _googleSignInClient = GoogleSignIn.GetClient(activity, gso);
            _logger.LogInformation("Google Sign-In initialisé");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur initialisation Google Sign-In");
        }
    }

    public async Task<string?> SignInAsync()
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                _logger.LogError("Activité Android non disponible");
                return null;
            }

            if (_googleSignInClient == null)
            {
                InitializeGoogleSignIn();
                if (_googleSignInClient == null)
                {
                    _logger.LogError("Impossible d'initialiser Google Sign-In");
                    return null;
                }
            }

            _signInCompletionSource = new TaskCompletionSource<string?>();

            // Déconnecter d'abord pour forcer le choix de compte
            await _googleSignInClient.SignOutAsync();

            // Lancer l'intent de connexion Google
            var signInIntent = _googleSignInClient.SignInIntent;
            activity.StartActivityForResult(signInIntent, RequestCodes.GoogleSignIn);

            // Attendre le résultat (timeout de 120 secondes pour laisser le temps à l'utilisateur)
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(120));
            var completedTask = await Task.WhenAny(_signInCompletionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("Timeout Google Sign-In (120s)");
                _signInCompletionSource.TrySetResult(null);
                return null;
            }

            return await _signInCompletionSource.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur connexion Google");
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            if (_googleSignInClient != null)
            {
                await _googleSignInClient.SignOutAsync();
                _logger.LogInformation("Déconnexion Google réussie");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion Google");
        }
    }

    /// <summary>
    /// Authentifie avec Google et Firebase en une seule opération
    /// </summary>
    /// <returns>L'utilisateur Firebase authentifié ou null en cas d'échec</returns>
    public async Task<IFirebaseUser?> SignInAndAuthenticateWithFirebaseAsync()
    {
        try
        {
            var googleIdToken = await SignInAsync();
            
            if (string.IsNullOrEmpty(googleIdToken))
            {
                _logger.LogWarning("Token Google non obtenu");
                return null;
            }
            
            var credential = GoogleAuthProvider.GetCredential(googleIdToken, null);
            var firebaseAuth = FirebaseAuth.Instance;
            var authResult = await firebaseAuth.SignInWithCredentialAsync(credential);
            
            if (authResult?.User != null)
            {
                _logger.LogInformation("Connexion Google Firebase réussie: {Email}", authResult.User.Email);
                
                // Attendre un court instant pour permettre la synchronisation avec Plugin.Firebase
                await Task.Delay(500);
                
                // Retourner l'utilisateur via Plugin.Firebase (synchronisé avec le SDK natif)
                var pluginUser = CrossFirebaseAuth.Current.CurrentUser;
                if (pluginUser != null) return pluginUser;
                
                _logger.LogWarning("Plugin.Firebase.CurrentUser null, attente...");
                await Task.Delay(1000);
                pluginUser = CrossFirebaseAuth.Current.CurrentUser;
                
                if (pluginUser != null) return pluginUser;
                
                _logger.LogError("Impossible de synchroniser avec Plugin.Firebase");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur auth Google/Firebase");
            return null;
        }
    }

    /// <summary>
    /// Traite le résultat de l'activité Google Sign-In
    /// </summary>
    public void HandleActivityResult(int requestCode, global::Android.App.Result resultCode, Intent? data)
    {
        if (requestCode != RequestCodes.GoogleSignIn)
            return;

        try
        {
            if (resultCode == global::Android.App.Result.Canceled)
            {
                if (data != null)
                {
                    try
                    {
                        var task = GoogleSignIn.GetSignedInAccountFromIntent(data);
                        task.AddOnCompleteListener(this);
                        return;
                    }
                    catch (global::Android.Gms.Common.Apis.ApiException apiEx)
                    {
                        _logger.LogWarning("Google Sign-In annulé/erreur: code={Code}", apiEx.StatusCode);
                    }
                }
                
                _signInCompletionSource?.TrySetResult(null);
                return;
            }
            
            var signInTask = GoogleSignIn.GetSignedInAccountFromIntent(data);
            signInTask.AddOnCompleteListener(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur HandleActivityResult");
            _signInCompletionSource?.TrySetResult(null);
        }
    }

    public void OnComplete(global::Android.Gms.Tasks.Task task)
    {
        try
        {
            if (task.IsSuccessful)
            {
                var account = (GoogleSignInAccount?)task.Result;
                var idToken = account?.IdToken;
                
                if (!string.IsNullOrEmpty(idToken))
                {
                    _logger.LogInformation("Connexion Google réussie: {Email}", account?.Email);
                    _signInCompletionSource?.TrySetResult(idToken);
                }
                else
                {
                    _logger.LogWarning("Token Google vide - Web Client ID incorrect ?");
                    _signInCompletionSource?.TrySetResult(null);
                }
            }
            else
            {
                _logger.LogError(task.Exception, "Échec connexion Google");
                _signInCompletionSource?.TrySetResult(null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur OnComplete Google Sign-In");
            _signInCompletionSource?.TrySetResult(null);
        }
    }
}

/// <summary>
/// Codes de requête pour les activités
/// </summary>
public static class RequestCodes
{
    public const int GoogleSignIn = 9001;
}

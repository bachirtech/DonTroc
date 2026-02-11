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

            System.Diagnostics.Debug.WriteLine("🔵 [GoogleAuthService] Initialisation Google Sign-In...");
            System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] Web Client ID utilisé: {WebClientId}");

            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken(WebClientId)
                .RequestEmail()
                .RequestProfile()
                .Build();

            _googleSignInClient = GoogleSignIn.GetClient(activity, gso);
            
            System.Diagnostics.Debug.WriteLine("✅ [GoogleAuthService] Google Sign-In initialisé avec succès");
            _logger.LogInformation("Google Sign-In initialisé avec succès");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Erreur initialisation: {ex.Message}");
            _logger.LogError(ex, "Erreur lors de l'initialisation de Google Sign-In");
        }
    }

    public async Task<string?> SignInAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔵 [GoogleAuthService] Début SignInAsync");
            
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                _logger.LogError("❌ Activité Android non disponible");
                System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] Platform.CurrentActivity est null");
                return null;
            }

            if (_googleSignInClient == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ [GoogleAuthService] _googleSignInClient null, réinitialisation...");
                InitializeGoogleSignIn();
                if (_googleSignInClient == null)
                {
                    _logger.LogError("❌ Impossible d'initialiser Google Sign-In");
                    System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] Échec réinitialisation GoogleSignInClient");
                    return null;
                }
            }

            _signInCompletionSource = new TaskCompletionSource<string?>();

            // Déconnecter d'abord pour forcer le choix de compte
            System.Diagnostics.Debug.WriteLine("🔵 [GoogleAuthService] Déconnexion du compte précédent...");
            await _googleSignInClient.SignOutAsync();

            // Lancer l'intent de connexion Google
            System.Diagnostics.Debug.WriteLine("🔵 [GoogleAuthService] Lancement de l'intent Google Sign-In...");
            var signInIntent = _googleSignInClient.SignInIntent;
            activity.StartActivityForResult(signInIntent, RequestCodes.GoogleSignIn);

            // Attendre le résultat (timeout de 120 secondes pour laisser le temps à l'utilisateur)
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(120));
            var completedTask = await Task.WhenAny(_signInCompletionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("⚠️ Timeout lors de la connexion Google (120s)");
                System.Diagnostics.Debug.WriteLine("⚠️ [GoogleAuthService] TIMEOUT - pas de réponse après 120 secondes");
                _signInCompletionSource.TrySetResult(null);
                return null;
            }

            var result = await _signInCompletionSource.Task;
            System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] SignInAsync terminé, token obtenu: {(result != null ? "OUI" : "NON")}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de la connexion Google");
            System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Exception dans SignInAsync: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("🔵 [GoogleAuthService] Début SignInAndAuthenticateWithFirebaseAsync");
            
            // Étape 1: Obtenir le token Google
            var googleIdToken = await SignInAsync();
            
            if (string.IsNullOrEmpty(googleIdToken))
            {
                _logger.LogWarning("❌ Token Google non obtenu - connexion annulée ou échouée");
                System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] Token Google est NULL ou vide");
                return null;
            }
            
            _logger.LogInformation("✅ Token Google obtenu, authentification avec Firebase...");
            System.Diagnostics.Debug.WriteLine($"✅ [GoogleAuthService] Token Google obtenu (longueur: {googleIdToken.Length})");
            
            // Étape 2: Créer le credential Firebase avec le token Google (SDK natif Android)
            var credential = GoogleAuthProvider.GetCredential(googleIdToken, null);
            System.Diagnostics.Debug.WriteLine("✅ [GoogleAuthService] Credential Firebase créé");
            
            // Étape 3: Authentifier avec Firebase SDK natif
            var firebaseAuth = FirebaseAuth.Instance;
            System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] FirebaseAuth.Instance obtenu, CurrentUser avant auth: {firebaseAuth.CurrentUser?.Email ?? "null"}");
            
            var authResult = await firebaseAuth.SignInWithCredentialAsync(credential);
            
            if (authResult?.User != null)
            {
                _logger.LogInformation("✅ Connexion Google Firebase réussie pour: {Email}", authResult.User.Email);
                System.Diagnostics.Debug.WriteLine($"✅ [GoogleAuthService] Firebase auth réussie: {authResult.User.Email} (UID: {authResult.User.Uid})");
                
                // Attendre un court instant pour permettre la synchronisation avec Plugin.Firebase
                await Task.Delay(500);
                
                // Retourner l'utilisateur via Plugin.Firebase (synchronisé avec le SDK natif)
                var pluginUser = CrossFirebaseAuth.Current.CurrentUser;
                if (pluginUser != null)
                {
                    _logger.LogInformation("✅ Utilisateur Plugin.Firebase synchronisé: {Uid}", pluginUser.Uid);
                    System.Diagnostics.Debug.WriteLine($"✅ [GoogleAuthService] Plugin.Firebase synchronisé: {pluginUser.Email} (UID: {pluginUser.Uid})");
                    return pluginUser;
                }
                
                // Si Plugin.Firebase n'est pas synchronisé, forcer un refresh
                _logger.LogWarning("⚠️ Plugin.Firebase.CurrentUser est null, tentative de refresh...");
                System.Diagnostics.Debug.WriteLine("⚠️ [GoogleAuthService] Plugin.Firebase.CurrentUser est null, attente supplémentaire...");
                await Task.Delay(1000);
                pluginUser = CrossFirebaseAuth.Current.CurrentUser;
                
                if (pluginUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ [GoogleAuthService] Plugin.Firebase synchronisé après attente: {pluginUser.Email}");
                    return pluginUser;
                }
                
                _logger.LogError("❌ Impossible de synchroniser l'utilisateur avec Plugin.Firebase");
                System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] Plugin.Firebase.CurrentUser reste null après toutes les tentatives");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] authResult ou authResult.User est null");
            }
            
            _logger.LogWarning("❌ Échec de l'authentification Firebase avec Google");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de l'authentification Google/Firebase: {Message}", ex.Message);
            System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Exception: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] StackTrace: {ex.StackTrace}");
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

        System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] HandleActivityResult - resultCode: {resultCode}");
        
        try
        {
            if (resultCode == global::Android.App.Result.Canceled)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ [GoogleAuthService] Utilisateur a annulé ou erreur Google Sign-In");
                
                // Essayer quand même de récupérer l'erreur depuis l'intent
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
                        var statusCode = apiEx.StatusCode;
                        var message = apiEx.Message ?? "Unknown error";
                        System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] ApiException code: " + statusCode);
                        System.Diagnostics.Debug.WriteLine("❌ [GoogleAuthService] ApiException message: " + message);
                        
                        // Interpréter le code d'erreur
                        switch (statusCode)
                        {
                            case 10:
                                System.Diagnostics.Debug.WriteLine("🚨 DEVELOPER_ERROR: Vérifiez la configuration OAuth dans Google Cloud Console");
                                System.Diagnostics.Debug.WriteLine("   - Le Web Client ID doit correspondre exactement");
                                System.Diagnostics.Debug.WriteLine("   - Google Sign-In doit être activé dans Firebase Authentication");
                                break;
                            case 12500:
                                System.Diagnostics.Debug.WriteLine("🚨 SIGN_IN_CANCELLED: L'utilisateur a annulé");
                                break;
                            case 12501:
                                System.Diagnostics.Debug.WriteLine("🚨 SIGN_IN_CURRENTLY_IN_PROGRESS: Une connexion est déjà en cours");
                                break;
                            case 12502:
                                System.Diagnostics.Debug.WriteLine("🚨 SIGN_IN_FAILED: Échec de la connexion");
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine("🚨 Code d'erreur inconnu: " + statusCode);
                                break;
                        }
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
            System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Exception dans HandleActivityResult: {ex.Message}");
            _logger.LogError(ex, "Erreur lors du traitement du résultat Google Sign-In");
            _signInCompletionSource?.TrySetResult(null);
        }
    }

    public void OnComplete(global::Android.Gms.Tasks.Task task)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] OnComplete appelé - IsSuccessful: {task.IsSuccessful}");
            
            if (task.IsSuccessful)
            {
                var account = (GoogleSignInAccount?)task.Result;
                var idToken = account?.IdToken;
                
                System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] Compte Google: {account?.Email}");
                System.Diagnostics.Debug.WriteLine($"🔵 [GoogleAuthService] IdToken présent: {!string.IsNullOrEmpty(idToken)}");
                
                if (!string.IsNullOrEmpty(idToken))
                {
                    _logger.LogInformation("✅ Connexion Google réussie pour: {Email}", account?.Email);
                    System.Diagnostics.Debug.WriteLine($"✅ [GoogleAuthService] Token obtenu pour: {account?.Email}");
                    _signInCompletionSource?.TrySetResult(idToken);
                }
                else
                {
                    _logger.LogWarning("⚠️ Token Google vide - le Web Client ID est peut-être incorrect");
                    System.Diagnostics.Debug.WriteLine("⚠️ [GoogleAuthService] IdToken est NULL ou VIDE!");
                    System.Diagnostics.Debug.WriteLine($"⚠️ [GoogleAuthService] Vérifiez que le Web Client ID '{WebClientId}' correspond à celui dans google-services.json");
                    _signInCompletionSource?.TrySetResult(null);
                }
            }
            else
            {
                var exception = task.Exception;
                _logger.LogError(exception, "❌ Échec de la connexion Google");
                System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] task.IsSuccessful = false");
                System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Exception type: {exception?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Exception message: {exception?.Message}");
                
                // Analyser l'exception pour donner plus de détails
                if (exception != null)
                {
                    var innerEx = exception.InnerException ?? exception;
                    System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Inner exception: {innerEx.GetType().Name}: {innerEx.Message}");
                    
                    // Vérifier si c'est une ApiException avec code d'erreur
                    if (innerEx.Message.Contains("10:") || innerEx.Message.Contains("10 :"))
                    {
                        System.Diagnostics.Debug.WriteLine("========================================");
                        System.Diagnostics.Debug.WriteLine("🚨 ERREUR DEVELOPER_ERROR (code 10) 🚨");
                        System.Diagnostics.Debug.WriteLine("========================================");
                        System.Diagnostics.Debug.WriteLine("Cette erreur signifie que l'empreinte SHA-1 de votre APK");
                        System.Diagnostics.Debug.WriteLine("ne correspond PAS à celle configurée dans Firebase.");
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine("SOLUTIONS:");
                        System.Diagnostics.Debug.WriteLine("1. Vérifiez l'empreinte SHA-1 de votre APK actuel:");
                        System.Diagnostics.Debug.WriteLine("   keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android");
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine("2. Comparez avec celle dans google-services.json:");
                        System.Diagnostics.Debug.WriteLine("   Debug SHA-1 attendu: 39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24");
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine("3. Si différent, ajoutez la bonne empreinte dans Firebase Console");
                        System.Diagnostics.Debug.WriteLine("   puis re-téléchargez google-services.json");
                        System.Diagnostics.Debug.WriteLine("========================================");
                    }
                }
                
                _signInCompletionSource?.TrySetResult(null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur dans OnComplete Google Sign-In");
            System.Diagnostics.Debug.WriteLine($"❌ [GoogleAuthService] Exception dans OnComplete: {ex.Message}");
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

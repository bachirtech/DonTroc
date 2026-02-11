using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace DonTroc.Services;

/// <summary>
/// Service pour gérer la demande de notation de l'application
/// Affiche un popup après un certain temps d'utilisation pour inviter l'utilisateur à noter l'app
/// </summary>
public class AppRatingService
{
    private readonly ILogger<AppRatingService> _logger;
    private System.Timers.Timer? _usageTimer;
    private DateTime _sessionStartTime;
    private bool _isPopupShown;
    
    // Clés de préférences
    private const string HAS_RATED_KEY = "app_has_rated";
    private const string DONT_ASK_AGAIN_KEY = "app_dont_ask_rating";
    private const string LAST_PROMPT_DATE_KEY = "app_last_rating_prompt";
    private const string TOTAL_USAGE_MINUTES_KEY = "app_total_usage_minutes";
    private const string SESSION_COUNT_KEY = "app_session_count";
    
    // Configuration
    private const int MINUTES_BEFORE_FIRST_PROMPT = 3; // 3 minutes d'utilisation avant première demande
    private const int DAYS_BETWEEN_PROMPTS = 4; // 4 jours entre chaque demande si reporté
    private const int MIN_SESSIONS_BEFORE_PROMPT = 2; // Minimum 2 sessions avant de demander

    public AppRatingService(ILogger<AppRatingService> logger)
    {
        _logger = logger;
        _sessionStartTime = DateTime.Now;
    }

    /// <summary>
    /// Démarre le suivi du temps d'utilisation
    /// </summary>
    public void StartTracking()
    {
        try
        {
            // Incrémenter le compteur de sessions
            var sessionCount = Preferences.Get(SESSION_COUNT_KEY, 0);
            Preferences.Set(SESSION_COUNT_KEY, sessionCount + 1);
            
            _sessionStartTime = DateTime.Now;
            _isPopupShown = false;
            
            // Créer un timer qui vérifie toutes les minutes
            _usageTimer = new System.Timers.Timer(60000); // 60 secondes
            _usageTimer.Elapsed += OnTimerElapsed;
            _usageTimer.AutoReset = true;
            _usageTimer.Start();
            
            _logger.LogInformation("AppRatingService: Tracking démarré, session #{SessionCount}", sessionCount + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du démarrage du tracking");
        }
    }

    /// <summary>
    /// Arrête le suivi et sauvegarde le temps d'utilisation
    /// </summary>
    public void StopTracking()
    {
        try
        {
            _usageTimer?.Stop();
            _usageTimer?.Dispose();
            _usageTimer = null;
            
            // Sauvegarder le temps d'utilisation de cette session
            var sessionMinutes = (int)(DateTime.Now - _sessionStartTime).TotalMinutes;
            var totalMinutes = Preferences.Get(TOTAL_USAGE_MINUTES_KEY, 0);
            Preferences.Set(TOTAL_USAGE_MINUTES_KEY, totalMinutes + sessionMinutes);
            
            _logger.LogInformation("AppRatingService: Tracking arrêté, {SessionMinutes} minutes cette session, {TotalMinutes} minutes total", 
                sessionMinutes, totalMinutes + sessionMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'arrêt du tracking");
        }
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (_isPopupShown) return;
            
            var minutesInSession = (int)(DateTime.Now - _sessionStartTime).TotalMinutes;
            
            if (ShouldShowRatingPrompt(minutesInSession))
            {
                _isPopupShown = true;
                MainThread.BeginInvokeOnMainThread(async () => await ShowRatingPopupAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dans le timer de rating");
        }
    }

    /// <summary>
    /// Vérifie si on doit afficher la demande de notation
    /// </summary>
    private bool ShouldShowRatingPrompt(int minutesInSession)
    {
        // Déjà noté ?
        if (Preferences.Get(HAS_RATED_KEY, false))
            return false;
        
        // L'utilisateur a dit "ne plus demander" ?
        if (Preferences.Get(DONT_ASK_AGAIN_KEY, false))
            return false;
        
        // Assez de temps dans cette session ?
        if (minutesInSession < MINUTES_BEFORE_FIRST_PROMPT)
            return false;
        
        // Assez de sessions ?
        var sessionCount = Preferences.Get(SESSION_COUNT_KEY, 0);
        if (sessionCount < MIN_SESSIONS_BEFORE_PROMPT)
            return false;
        
        // Vérifier la date de la dernière demande
        var lastPromptDateStr = Preferences.Get(LAST_PROMPT_DATE_KEY, string.Empty);
        if (!string.IsNullOrEmpty(lastPromptDateStr))
        {
            if (DateTime.TryParse(lastPromptDateStr, out var lastPromptDate))
            {
                var daysSinceLastPrompt = (DateTime.Now - lastPromptDate).TotalDays;
                if (daysSinceLastPrompt < DAYS_BETWEEN_PROMPTS)
                    return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Affiche le popup de demande de notation
    /// </summary>
    public async Task ShowRatingPopupAsync()
    {
        try
        {
            // Sauvegarder la date de cette demande
            Preferences.Set(LAST_PROMPT_DATE_KEY, DateTime.Now.ToString("O"));
            
            // Utiliser DisplayActionSheet pour un affichage fiable sur toutes les pages
            await ShowRatingActionSheetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage du popup de notation");
        }
    }

    /// <summary>
    /// Affiche un ActionSheet simple comme fallback
    /// </summary>
    private async Task ShowRatingActionSheetAsync()
    {
        var result = await Shell.Current.DisplayActionSheet(
            "🌟 Aimez-vous DonTroc ?",
            null,
            null,
            "⭐⭐⭐⭐⭐ Oui, je note !",
            "🕐 Plus tard",
            "🚫 Ne plus demander");
        
        switch (result)
        {
            case "⭐⭐⭐⭐⭐ Oui, je note !":
                await OpenStoreForRating();
                HandleRatingResult(RatingResultType.Rated);
                break;
                
            case "🕐 Plus tard":
                HandleRatingResult(RatingResultType.Later);
                break;
                
            case "🚫 Ne plus demander":
                HandleRatingResult(RatingResultType.DontAskAgain);
                break;
        }
    }

    /// <summary>
    /// Traite le résultat de la demande de notation
    /// </summary>
    private void HandleRatingResult(RatingResultType result)
    {
        switch (result)
        {
            case RatingResultType.Rated:
                Preferences.Set(HAS_RATED_KEY, true);
                _logger.LogInformation("Utilisateur a accepté de noter l'app");
                break;
                
            case RatingResultType.Later:
                _logger.LogInformation("Utilisateur a reporté la notation");
                break;
                
            case RatingResultType.DontAskAgain:
                Preferences.Set(DONT_ASK_AGAIN_KEY, true);
                _logger.LogInformation("Utilisateur ne veut plus être sollicité pour la notation");
                break;
        }
    }

    /// <summary>
    /// Enum interne pour le résultat de la demande de notation
    /// </summary>
    private enum RatingResultType
    {
        Rated,
        Later,
        DontAskAgain
    }

    /// <summary>
    /// Ouvre le store pour noter l'application
    /// </summary>
    private async Task OpenStoreForRating()
    {
        try
        {
#if ANDROID
            // Package name de l'app Android
            var packageName = AppInfo.PackageName;
            var uri = new Uri($"market://details?id={packageName}");
            await Launcher.OpenAsync(uri);
#elif IOS
            // App Store ID - À remplacer par votre vrai ID
            var appId = "VOTRE_APP_STORE_ID"; // TODO: Remplacer par l'ID réel
            var uri = new Uri($"itms-apps://itunes.apple.com/app/id{appId}?action=write-review");
            await Launcher.OpenAsync(uri);
#else
            await Shell.Current.DisplayAlert("Merci !", 
                "Merci de vouloir noter notre application ! 🙏", "OK");
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture du store");
            
            // Fallback : ouvrir le Play Store dans le navigateur
            try
            {
#if ANDROID
                var packageName = AppInfo.PackageName;
                var webUri = new Uri($"https://play.google.com/store/apps/details?id={packageName}");
                await Browser.OpenAsync(webUri, BrowserLaunchMode.External);
#endif
            }
            catch
            {
                await Shell.Current.DisplayAlert("Merci !", 
                    "Merci pour votre soutien ! Vous pouvez nous noter sur le Play Store.", "OK");
            }
        }
    }

    /// <summary>
    /// Force l'affichage du popup (pour test ou menu paramètres)
    /// </summary>
    public async Task ForceShowRatingPopupAsync()
    {
        _isPopupShown = true;
        await ShowRatingPopupAsync();
    }

    /// <summary>
    /// Réinitialise toutes les préférences de notation (pour debug)
    /// </summary>
    public void ResetRatingPreferences()
    {
        Preferences.Remove(HAS_RATED_KEY);
        Preferences.Remove(DONT_ASK_AGAIN_KEY);
        Preferences.Remove(LAST_PROMPT_DATE_KEY);
        Preferences.Remove(TOTAL_USAGE_MINUTES_KEY);
        Preferences.Remove(SESSION_COUNT_KEY);
        _isPopupShown = false;
        _logger.LogInformation("Préférences de notation réinitialisées");
    }
}


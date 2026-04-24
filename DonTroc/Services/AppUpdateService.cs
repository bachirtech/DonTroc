using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace DonTroc.Services;

/// <summary>
/// Service de vérification des mises à jour de l'application.
///
/// Lit une configuration stockée dans Firebase Realtime DB sous /app_config/{android|ios} :
/// {
///   "latest_version_code": 29,            // Dernière version disponible (build number)
///   "latest_version_name": "1.2",          // Version affichée à l'utilisateur
///   "min_required_version_code": 26,       // En dessous → mise à jour OBLIGATOIRE
///   "update_message": "Nouvelle version..",
///   "release_notes": "- Feature A\n- Fix B"
/// }
///
/// - Si current &lt; min_required → popup bloquante (force update)
/// - Si current &lt; latest → popup optionnelle (soft update) avec rappel max 1×/24h
/// - Tout redirige vers le Play Store (conforme aux règles Google Play).
/// </summary>
public class AppUpdateService
{
    private const string SoftReminderKey = "app_update_last_soft_reminder";
    private const string DismissedVersionKey = "app_update_dismissed_version";
    private static readonly TimeSpan SoftReminderCooldown = TimeSpan.FromHours(24);

    private readonly AuthService _authService;
    private readonly FileLoggerService _logger;
    private readonly FirebaseClient _firebaseClient;
    private readonly IInAppUpdateService _inAppUpdate;

    public AppUpdateService(AuthService authService, FileLoggerService logger, IInAppUpdateService inAppUpdate)
    {
        _authService = authService;
        _logger = logger;
        _inAppUpdate = inAppUpdate;
        _firebaseClient = new FirebaseClient(
            ConfigurationService.FirebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await _authService.GetAuthTokenAsync() ?? string.Empty
            });

        // Brancher la notification "MAJ téléchargée" → snackbar invitant à installer
        _inAppUpdate.FlexibleUpdateDownloaded += OnFlexibleUpdateDownloaded;
    }

    /// <summary>
    /// Vérifie si une mise à jour est disponible et affiche la popup appropriée.
    /// Fire-and-forget safe : tout est enveloppé dans try/catch.
    /// </summary>
    public async Task CheckForUpdateAsync()
    {
        try
        {
            var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
            var config = await _firebaseClient
                .Child("app_config")
                .Child(platform)
                .OnceSingleAsync<AppUpdateConfig>();

            if (config == null)
            {
                Debug.WriteLine("[AppUpdate] Aucune config /app_config/" + platform);
                return;
            }

            // Version actuelle du build
            if (!int.TryParse(AppInfo.BuildString, out var currentBuild))
                currentBuild = 0;

            Debug.WriteLine($"[AppUpdate] Build actuel={currentBuild}, dernier={config.latest_version_code}, min={config.min_required_version_code}");

            // 1) Force update
            if (config.min_required_version_code > 0 && currentBuild < config.min_required_version_code)
            {
                await MainThread.InvokeOnMainThreadAsync(() => ShowForceUpdateAsync(config));
                return;
            }

            // 2) Soft update
            if (config.latest_version_code > 0 && currentBuild < config.latest_version_code)
            {
                // Ne pas harceler : max 1 fois par 24h, et ignorer si l'utilisateur a déjà refusé cette version
                var dismissed = Preferences.Default.Get(DismissedVersionKey, 0);
                if (dismissed == config.latest_version_code)
                    return;

                var lastReminderTicks = Preferences.Default.Get(SoftReminderKey, 0L);
                var lastReminder = lastReminderTicks > 0 ? new DateTime(lastReminderTicks) : DateTime.MinValue;
                if (DateTime.UtcNow - lastReminder < SoftReminderCooldown)
                    return;

                Preferences.Default.Set(SoftReminderKey, DateTime.UtcNow.Ticks);
                await MainThread.InvokeOnMainThreadAsync(() => ShowSoftUpdateAsync(config));
            }
        }
        catch (Exception ex)
        {
            // Silencieux : ne doit jamais bloquer l'app
            Debug.WriteLine($"[AppUpdate] Erreur check: {ex.Message}");
            try { _logger?.LogException(ex); } catch { }
        }
    }

    private async Task ShowForceUpdateAsync(AppUpdateConfig config)
    {
        // 🆕 Tentative Play In-App Update (Android) — overlay natif Google Play
        if (_inAppUpdate.IsSupported)
        {
            var result = await _inAppUpdate.TryStartUpdateAsync(InAppUpdateMode.Immediate);
            if (result == InAppUpdateResult.FlowStarted)
                return; // Google Play prend le relais
            Debug.WriteLine($"[AppUpdate] Play In-App Update Immediate indisponible ({result}) → fallback popup");
        }

        var page = Application.Current?.MainPage;
        if (page == null) return;

        var title = "🔄 Mise à jour requise";
        var message = string.IsNullOrWhiteSpace(config.update_message)
            ? $"Une nouvelle version ({config.latest_version_name}) de DonTroc est disponible.\n\nPour continuer à utiliser l'application, veuillez la mettre à jour."
            : config.update_message;

        if (!string.IsNullOrWhiteSpace(config.release_notes))
            message += "\n\nNouveautés :\n" + config.release_notes;

        // Boucle tant que l'utilisateur ne met pas à jour : l'app reste bloquée sur la popup.
        while (true)
        {
            await page.DisplayAlert(title, message, "Mettre à jour");
            OpenStore(config.update_url);
            // Laisse le temps d'ouvrir le store ; quand l'utilisateur revient, on re-propose.
            await Task.Delay(2000);
        }
    }

    private async Task ShowSoftUpdateAsync(AppUpdateConfig config)
    {
        // 🆕 Tentative Play In-App Update Flexible (Android) — DL en arrière-plan, UX premium
        if (_inAppUpdate.IsSupported)
        {
            var result = await _inAppUpdate.TryStartUpdateAsync(InAppUpdateMode.Flexible);
            if (result == InAppUpdateResult.FlowStarted)
            {
                Debug.WriteLine("[AppUpdate] Flow Flexible démarré, téléchargement en arrière-plan");
                return;
            }
            Debug.WriteLine($"[AppUpdate] Play In-App Update Flexible indisponible ({result}) → fallback popup");
        }

        var page = Application.Current?.MainPage;
        if (page == null) return;

        var title = $"✨ Nouvelle version {config.latest_version_name} disponible";
        var message = string.IsNullOrWhiteSpace(config.update_message)
            ? "Une nouvelle version de DonTroc est disponible avec des améliorations et corrections."
            : config.update_message;

        if (!string.IsNullOrWhiteSpace(config.release_notes))
            message += "\n\nNouveautés :\n" + config.release_notes;

        var update = await page.DisplayAlert(title, message, "Mettre à jour", "Plus tard");
        if (update)
        {
            OpenStore(config.update_url);
        }
        else
        {
            // L'utilisateur a dit « Plus tard » → mémoriser pour ne plus redemander pour cette version.
            Preferences.Default.Set(DismissedVersionKey, config.latest_version_code);
        }
    }

    /// <summary>
    /// Appelé quand un Flexible Update a fini de télécharger en arrière-plan.
    /// Demande à l'utilisateur s'il veut redémarrer pour installer.
    /// </summary>
    private void OnFlexibleUpdateDownloaded()
    {
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var page = Application.Current?.MainPage;
                if (page == null) return;
                var install = await page.DisplayAlert(
                    "✅ Mise à jour prête",
                    "La nouvelle version a été téléchargée. Redémarrer maintenant pour l'installer ?",
                    "Installer", "Plus tard");
                if (install)
                {
                    await _inAppUpdate.CompleteFlexibleUpdateAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppUpdate] OnFlexibleDownloaded error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// À appeler depuis MainActivity.OnResume : reprend un éventuel Immediate Update interrompu.
    /// </summary>
    public Task ResumeIfNeededAsync() => _inAppUpdate.ResumeIfImmediateUpdatePendingAsync();

    private void OpenStore(string? customUrl = null)
    {
        try
        {
            // 🆕 Si une URL custom est fournie via Firebase (ex. page GitHub Pages
            // hébergeant l'APK), on l'ouvre en priorité — utile quand l'app n'est
            // plus distribuée via le Play Store.
            if (!string.IsNullOrWhiteSpace(customUrl))
            {
                _ = Launcher.OpenAsync(new Uri(customUrl));
                return;
            }
#if ANDROID
            var packageName = AppInfo.PackageName;
            // D'abord tenter l'intent Play Store natif
            try
            {
                _ = Launcher.OpenAsync(new Uri($"market://details?id={packageName}"));
                return;
            }
            catch { }
            _ = Launcher.OpenAsync(new Uri($"https://play.google.com/store/apps/details?id={packageName}"));
#elif IOS
            // TODO: remplacer par le vrai App Store ID quand disponible
            var appId = "VOTRE_APP_STORE_ID";
            _ = Launcher.OpenAsync(new Uri($"itms-apps://itunes.apple.com/app/id{appId}"));
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppUpdate] Erreur ouverture store: {ex.Message}");
        }
    }

    /// <summary>
    /// Structure miroir du nœud Firebase /app_config/{platform}.
    /// Nommage snake_case pour faciliter l'édition côté console Firebase.
    /// </summary>
    public class AppUpdateConfig
    {
        public int latest_version_code { get; set; }
        public string latest_version_name { get; set; } = string.Empty;
        public int min_required_version_code { get; set; }
        public string update_message { get; set; } = string.Empty;
        public string release_notes { get; set; } = string.Empty;
        /// <summary>
        /// URL personnalisée vers laquelle envoyer l'utilisateur pour récupérer la nouvelle version
        /// (page GitHub Pages, lien APK direct, etc.). Si vide → fallback Play Store / App Store.
        /// </summary>
        public string update_url { get; set; } = string.Empty;
    }
}


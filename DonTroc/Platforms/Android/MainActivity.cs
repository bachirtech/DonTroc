using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Google.Android.Gms.Ads;
using AndroidX.Core.View;
using System.Collections.Generic;
using Microsoft.Maui;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // ✅ Activer l'affichage Edge-to-Edge pour Android 15+ (SDK 35)
        EnableEdgeToEdge();

        // ✅ Demander le consentement RGPD (UMP) PUIS initialiser AdMob
        // Sans consentement résolu, on peut quand même servir des pubs non-personnalisées,
        // mais la médiation perd ~50 % de fill rate. On lance le flow consent en parallèle
        // pour ne pas bloquer le démarrage de l'app.
        _ = GatherConsentThenInitAdMobAsync();

        // ✅ Traiter les extras de l'intent de lancement (notification push)
        HandleNotificationIntent(Intent);
    }

    /// <summary>
    /// Demande le consentement UMP, puis initialise AdMob une fois le consentement résolu.
    /// Si le consentement échoue ou est indisponible, on initialise AdMob quand même
    /// (les pubs non-personnalisées seront servies par défaut).
    /// </summary>
    private async Task GatherConsentThenInitAdMobAsync()
    {
        try
        {
            var consentService = IPlatformApplication.Current?.Services.GetService(typeof(Services.IConsentService))
                as Services.IConsentService;

            if (consentService != null)
            {
                var canRequestAds = await consentService.GatherConsentAsync();
                System.Diagnostics.Debug.WriteLine($"[Consent] CanRequestAds = {canRequestAds}");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Consent] Error: {ex.Message}");
        }
        finally
        {
            // Toujours initialiser AdMob, que le consent ait réussi ou non.
            // Le SDK AdMob respecte automatiquement l'état de consentement UMP stocké.
            InitializeAdMob();
        }
    }

    /// <summary>
    /// Appelé quand l'activité reçoit un nouvel intent (notification cliquée pendant que l'app est ouverte).
    /// LaunchMode = SingleTop → OnNewIntent au lieu de OnCreate.
    /// </summary>
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent != null)
        {
            HandleNotificationIntent(intent);
        }
    }

    /// <summary>
    /// Traite les extras d'un intent provenant d'une notification push ou locale.
    /// Navigue automatiquement vers la page correspondante.
    /// Attend que Shell.Current soit disponible (cold start depuis notification).
    /// </summary>
    private static void HandleNotificationIntent(Intent? intent)
    {
        if (intent?.Extras == null) return;

        // Déterminer la route cible en fonction des extras de l'intent
        string? targetRoute = null;

        try
        {
            // Notifications messages : ouvrir la conversation
            var conversationId = intent.GetStringExtra("conversationId");
            if (!string.IsNullOrEmpty(conversationId))
            {
                targetRoute = $"ChatView?conversationId={conversationId}";
            }
            // Notifications quiz
            else if (intent.GetBooleanExtra("openQuiz", false))
            {
                targetRoute = "QuizPage";
            }
            // Notifications modération
            else if (intent.GetBooleanExtra("openModeration", false))
            {
                targetRoute = "ModerationPage";
            }
            else
            {
                // Notifications gamification / rétention
                var notificationType = intent.GetStringExtra("notificationType");
                if (!string.IsNullOrEmpty(notificationType))
                {
                    targetRoute = notificationType switch
                    {
                        "streak_danger" or "achievement" or "level_up" or "challenge" => "RewardsPage",
                        "proximity_annonce" => !string.IsNullOrEmpty(intent.GetStringExtra("annonceId"))
                            ? $"AnnonceDetailView?annonceId={intent.GetStringExtra("annonceId")}"
                            : "//AnnoncesView",
                        _ => null
                    };
                }

                // Cloud Functions push : traiter le click_action des data FCM
                if (targetRoute == null)
                {
                    var clickAction = intent.GetStringExtra("click_action");
                    if (!string.IsNullOrEmpty(clickAction))
                    {
                        targetRoute = clickAction switch
                        {
                            "OPEN_CONVERSATION" => !string.IsNullOrEmpty(intent.GetStringExtra("conversationId"))
                                ? $"ChatView?conversationId={intent.GetStringExtra("conversationId")}"
                                : "//ConversationsView",
                            "OPEN_ANNONCES" or "OPEN_ANNONCE" => "//AnnoncesView",
                            "OPEN_REWARDS" => "RewardsPage",
                            "OPEN_DASHBOARD" => "//DashboardView",
                            "OPEN_ADMIN_REPORTS" => "ModerationPage",
                            "OPEN_TRANSACTION" => !string.IsNullOrEmpty(intent.GetStringExtra("transactionId"))
                                ? $"TransactionDetailsView?transactionId={intent.GetStringExtra("transactionId")}"
                                : null,
                            _ => null
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] Erreur parsing intent extras: {ex.Message}");
        }

        if (string.IsNullOrEmpty(targetRoute)) return;

        // Naviguer vers la route cible — attendre que Shell.Current soit prêt (cold start)
        var route = targetRoute;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Attendre que Shell.Current soit disponible (max 5 secondes)
                // En cold start, MAUI met ~1-3s à initialiser l'AppShell
                for (var i = 0; i < 50 && Shell.Current == null; i++)
                {
                    await Task.Delay(100);
                }

                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Erreur navigation deep link '{route}': {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Active l'affichage bord à bord (Edge-to-Edge) pour Android 15+.
    /// Assure la rétrocompatibilité avec les versions antérieures.
    /// Évite les API obsolètes (setStatusBarColor, setNavigationBarColor) sur Android 15+.
    /// </summary>
    private void EnableEdgeToEdge()
    {
        try
        {
            // Permettre à l'app de dessiner sous les barres système (API moderne)
            WindowCompat.SetDecorFitsSystemWindows(Window!, false);

            if (Window != null)
            {
                // Sur Android 15+ (API 35), les barres sont transparentes par défaut
                // et setStatusBarColor/setNavigationBarColor sont obsolètes.
                // On les utilise uniquement sur les versions antérieures pour la rétrocompatibilité.
                if (Build.VERSION.SdkInt < BuildVersionCodes.VanillaIceCream) // API < 35
                {
#pragma warning disable CA1422 // Validate platform compatibility
                    Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                    Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
#pragma warning restore CA1422
                }

                // Configurer l'apparence des icônes de la barre système selon le thème
                // Cette API est moderne et recommandée pour toutes les versions
                var windowInsetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
                if (windowInsetsController != null)
                {
                    // Définir les icônes claires ou sombres selon le thème de l'app
                    var isDarkTheme = (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
                    windowInsetsController.AppearanceLightStatusBars = !isDarkTheme;
                    windowInsetsController.AppearanceLightNavigationBars = !isDarkTheme;
                }
            }
        }
        catch { }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        // 🆕 Retour du flow Google Play In-App Update
        if (requestCode == Services.PlayInAppUpdateService.RequestCode)
        {
            // Result codes Play Core :
            //   Ok        = utilisateur a accepté
            //   Canceled  = utilisateur a refusé / fermé
            //   1 (RESULT_IN_APP_UPDATE_FAILED) = erreur Play Store
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] OnActivityResult: code={(int)resultCode}");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        // 🆕 Si un Immediate Update était en cours et a été interrompu (ex : utilisateur
        // a quitté pendant le téléchargement), Google Play exige de relancer l'overlay.
        // Aussi : détecter si un Flexible Update vient de finir de télécharger en background.
        try
        {
            var services = IPlatformApplication.Current?.Services;
            var updateSvc = services?.GetService(typeof(Services.AppUpdateService)) as Services.AppUpdateService;
            _ = updateSvc?.ResumeIfNeededAsync();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] OnResume error: {ex.Message}");
        }
    }

    private void InitializeAdMob()
    {
        try
        {
            if (!AdMobConfiguration.ADS_ENABLED) return;

#if DEBUG
            // ============================================================
            // MODE TEST RÉSEAUX PARTENAIRES (Debug uniquement)
            // setTestDeviceIds() d'AdMob ne s'applique QU'à AdMob.
            // Chaque réseau partenaire a son propre mode test qu'il faut
            // activer séparément via leurs APIs Java (réflexion JNI).
            // ============================================================
            DonTroc.Platforms.Android.MediationTestHelper.EnablePartnerTestModes("c91e98ae-8285-4cd6-bc6c-0f687f7b2584");
#endif

            // ============================================================
            // APPAREILS DE TEST — DEBUG uniquement
            // En Release, seul l'émulateur est en mode test (requis par Google).
            // ============================================================
            var testDeviceIds = new List<string>
            {
                AdRequest.DeviceIdEmulator,
#if DEBUG
                // ID de votre appareil physique de dev (visible dans Logcat)
                "c91e98ae-8285-4cd6-bc6c-0f687f7b2584"
#endif
            };

            var requestConfiguration = new RequestConfiguration.Builder()
                .SetTestDeviceIds(testDeviceIds)
                .Build();

            MobileAds.RequestConfiguration = requestConfiguration;

            // Initialiser le SDK avec callback pour savoir quand c'est prêt
            MobileAds.Initialize(this, new AdMobInitCallback());
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur init: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback d'initialisation AdMob - log le statut de chaque adaptateur de médiation
    /// </summary>
    private class AdMobInitCallback : Java.Lang.Object, Google.Android.Gms.Ads.Initialization.IOnInitializationCompleteListener
    {
        public void OnInitializationComplete(Google.Android.Gms.Ads.Initialization.IInitializationStatus status)
        {
            System.Diagnostics.Debug.WriteLine("✅ SDK AdMob initialisé dans MainActivity");
            
            // ══════════════════════════════════════════════════════
            // SIGNALER AU SERVICE NATIF QUE LE SDK EST PRÊT
            // AdMobNativeService attend ce flag avant de charger les pubs
            // Cela évite les requêtes avant que la médiation soit configurée
            // ══════════════════════════════════════════════════════
            DonTroc.Platforms.Android.AdMobNativeService.IsSdkReady = true;
            
            // Afficher le statut de chaque adaptateur de médiation
            int readyCount = 0, totalCount = 0;
            var adapterStatuses = status.AdapterStatusMap;
            if (adapterStatuses != null)
            {
                foreach (var entry in adapterStatuses)
                {
                    totalCount++;
                    var adapterClass = entry.Key;
                    var adapterStatus = entry.Value;
                    var isReady = adapterStatus.InitializationState == 
                        Google.Android.Gms.Ads.Initialization.AdapterStatusState.Ready;
                    if (isReady) readyCount++;
                    var state = isReady ? "✅ PRÊT" : "⏳ PAS PRÊT";
                    var latency = adapterStatus.Latency;
                    var desc = adapterStatus.Description;
                    System.Diagnostics.Debug.WriteLine(
                        $"[AdMob Mediation] {adapterClass}: {state} (latence={latency}ms) {desc}");
                }
            }
            System.Diagnostics.Debug.WriteLine($"[AdMob Mediation] Résumé: {readyCount}/{totalCount} adaptateurs prêts");
        }
    }
}
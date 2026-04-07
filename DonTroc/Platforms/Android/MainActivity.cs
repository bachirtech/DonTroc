using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Google.Android.Gms.Ads;
using AndroidX.Core.View;
using System.Collections.Generic;
using Microsoft.Maui;
using DonTroc.Services;

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

        // ✅ Initialisation AdMob
        InitializeAdMob();
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
            // APPAREILS DE TEST
            // L'ID de votre appareil physique s'affiche dans les logs au 
            // premier chargement d'une pub : "Use RequestConfiguration
            // .Builder().setTestDeviceIds(Arrays.asList("XXXXXXX"))…"
            // Copiez-le ici pour recevoir des pubs de test (AdMob + médiation).
            // ============================================================
            var testDeviceIds = new List<string>
            {
                AdRequest.DeviceIdEmulator,
                // TODO: Remplacez par l'ID réel de votre appareil physique
                // visible dans Logcat lors du premier chargement d'une pub.
              "c91e98ae-8285-4cd6-bc6c-0f687f7b2584"
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
            
            // Afficher le statut de chaque adaptateur de médiation
            var adapterStatuses = status.AdapterStatusMap;
            if (adapterStatuses != null)
            {
                foreach (var entry in adapterStatuses)
                {
                    var adapterClass = entry.Key;
                    var adapterStatus = entry.Value;
                    var state = adapterStatus.InitializationState == 
                        Google.Android.Gms.Ads.Initialization.AdapterStatusState.Ready 
                        ? "✅ PRÊT" : "⏳ PAS PRÊT";
                    var latency = adapterStatus.Latency;
                    var desc = adapterStatus.Description;
                    System.Diagnostics.Debug.WriteLine(
                        $"[AdMob Mediation] {adapterClass}: {state} (latence={latency}ms) {desc}");
                }
            }
        }
    }
}
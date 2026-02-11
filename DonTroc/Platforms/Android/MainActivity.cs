﻿﻿﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Gms.Ads;
using Android.Views;
using AndroidX.Core.View;
using System.Collections.Generic;
using Microsoft.Maui;
using DonTroc.Services;
using DonTroc.Platforms.Android;

namespace DonTroc;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // ✅ Activer l'affichage Edge-to-Edge pour Android 15+ (SDK 35)
        EnableEdgeToEdge();

        // ✅ Initialisation AdMob au démarrage de l'application
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

            System.Diagnostics.Debug.WriteLine("✅ Edge-to-Edge activé avec succès");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Erreur lors de l'activation Edge-to-Edge: {ex.Message}");
            // Ne pas bloquer l'app si Edge-to-Edge échoue
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        
        System.Diagnostics.Debug.WriteLine($"🔵 [MainActivity] OnActivityResult - requestCode: {requestCode}, resultCode: {resultCode}");
        
        // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
        /*
        // Transmettre le résultat au service Google Auth
        if (requestCode == RequestCodes.GoogleSignIn)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [MainActivity] Google Sign-In result reçu, resultCode: {resultCode}");
            
            try
            {
                // Essayer plusieurs méthodes pour obtenir le service
                GoogleAuthService? googleAuthService = null;
                
                // Méthode 1: Via IPlatformApplication
                googleAuthService = IPlatformApplication.Current?.Services.GetService<GoogleAuthService>();
                
                // Méthode 2: Via Application.Current si la première échoue
                if (googleAuthService == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ [MainActivity] Tentative via Application.Current...");
                    googleAuthService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<GoogleAuthService>();
                }
                
                if (googleAuthService != null)
                {
                    System.Diagnostics.Debug.WriteLine("✅ [MainActivity] GoogleAuthService trouvé, transmission du résultat...");
                    googleAuthService.HandleActivityResult(requestCode, resultCode, data);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ [MainActivity] GoogleAuthService est NULL!");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [MainActivity] Erreur lors du traitement Google Sign-In: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ [MainActivity] StackTrace: {ex.StackTrace}");
            }
        }
        */
    }

    private void InitializeAdMob()
    {
        try
        {
            // ⚠️ Vérifier si les publicités sont désactivées (suspension AdMob)
            if (!DonTroc.Services.AdMobConfiguration.ADS_ENABLED)
            {
                System.Diagnostics.Debug.WriteLine(DonTroc.Services.AdMobConfiguration.GetStatusMessage());
                System.Diagnostics.Debug.WriteLine("🚫 Initialisation AdMob ignorée - compte suspendu");
                return;
            }

            System.Diagnostics.Debug.WriteLine("🎯 Initialisation du SDK AdMob...");

            // Configuration des appareils de test
            var testDeviceIds = new List<string>
            {
                AdRequest.DeviceIdEmulator, // Émulateur Android
                // Ajoutez l'ID de votre appareil de test ici après l'avoir obtenu des logs
                // Exemple: "33BE2250B43518CCDA7DE426D04EE231"
            };

            var requestConfiguration = new RequestConfiguration.Builder()
                .SetTestDeviceIds(testDeviceIds)
                .Build();

            MobileAds.RequestConfiguration = requestConfiguration;

            // Initialiser le SDK AdMob
            MobileAds.Initialize(this);

            System.Diagnostics.Debug.WriteLine("✅ SDK AdMob initialisé avec succès");
            System.Diagnostics.Debug.WriteLine("🎯 Configuration AdMob en mode TEST activée");
            System.Diagnostics.Debug.WriteLine("📱 Appareils de test configurés pour voir les annonces de test");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'initialisation AdMob: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
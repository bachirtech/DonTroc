using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Gms.Ads;
using System.Collections.Generic;
using Microsoft.Maui;

namespace DonTroc;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // ✅ Initialisation AdMob au démarrage de l'application
        InitializeAdMob();
    }

    private void InitializeAdMob()
    {
        try
        {
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
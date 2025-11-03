using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace DonTroc;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public partial class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // L'initialisation AdMob se fait maintenant automatiquement via l'injection de dépendances
        // Le service sera initialisé quand il sera injecté pour la première fois
        System.Diagnostics.Debug.WriteLine("✅ MainActivity créée - AdMob s'initialisera via DI");
    }
}

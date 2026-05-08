using Foundation;
using UIKit;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.ApplicationModel;
using Google.MobileAds;
using UserNotifications;

namespace DonTroc;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // 🛠️ Désactive le KeyboardAutoManager d'iOS (MAUI .NET 9) qui peut laisser
        // une bande grise persistante sous le contenu (zone réservée pour le clavier
        // qui ne se rétracte pas correctement). On gère manuellement si besoin.
        Microsoft.Maui.Platform.KeyboardAutoManagerScroll.Disconnect();

        // 🛠️ FIX bande grise + petits cercles colorés sur transitions :
        // Force l'UITabBar native iOS à être OPAQUE (sinon le blur translucide
        // laisse transparaître la couleur Terracotta du tab sélectionné sous
        // forme de cercle pendant l'animation de switch d'onglet).
        // Force aussi le UIWindow.backgroundColor pour éviter le gris système
        // par défaut visible dans les zones safe-area / inset.
        ConfigureIosAppearance();

        // ⚠️ Firebase DOIT être initialisé AVANT base.FinishedLaunching :
        // base.FinishedLaunching() appelle CreateMauiApp() puis résout les services
        // (dont AuthService) qui accèdent à FirebaseAuth.Auth → si FIRApp.configure()
        // n'a pas encore tourné, crash "FirebaseAuth/Auth.swift:155 Fatal error".
        Plugin.Firebase.Bundled.Platforms.iOS.CrossFirebase.Initialize(MauiProgram.CreateCrossFirebaseSettings());

        // Initialiser Google Mobile Ads (AdMob)
        MobileAds.SharedInstance.Start(completionHandler: null);

        // Demander l'autorisation d'envoyer des notifications + s'enregistrer auprès d'APNs.
        // FirebaseAppDelegateProxyEnabled = false dans Info.plist → on doit s'en charger nous-mêmes.
        UNUserNotificationCenter.Current.RequestAuthorization(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
            (granted, error) =>
            {
                if (granted)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        UIApplication.SharedApplication.RegisterForRemoteNotifications());
                }
            });

        return base.FinishedLaunching(application, launchOptions);
    }

    // APNs token reçu → transmis à Plugin.Firebase pour FCM
    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void DidRegisterForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        // Avec FirebaseAppDelegateProxyEnabled=false, on passe nous-mêmes le token APNs à FCM.
        Firebase.CloudMessaging.Messaging.SharedInstance.ApnsToken = deviceToken;
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void DidFailToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        System.Diagnostics.Debug.WriteLine($"❌ APNs registration failed: {error.LocalizedDescription}");
    }

    /// <summary>
    /// Configure l'apparence native iOS pour éliminer les "petits cercles colorés"
    /// lors des transitions de tab (UITabBar translucide qui laisse transparaître
    /// la couleur Terracotta du tab sélectionné). Doit être appelée AVANT
    /// base.FinishedLaunching.
    /// </summary>
    private static void ConfigureIosAppearance()
    {
        // 🛠️ Force l'UITabBar native iOS à être OPAQUE avec la couleur BeigeClair
        // (#F6F1EB) identique à Shell.TabBarBackgroundColor, sinon le blur translucide
        // crée un effet de cercle coloré pendant l'animation de switch d'onglet.
        var tabBarBackground = UIColor.FromRGB(0xF6 / 255f, 0xF1 / 255f, 0xEB / 255f);

        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
        {
            var tabAppearance = new UITabBarAppearance();
            tabAppearance.ConfigureWithOpaqueBackground();
            tabAppearance.BackgroundColor = tabBarBackground;
            tabAppearance.ShadowColor = UIColor.Clear;

            UITabBar.Appearance.StandardAppearance = tabAppearance;
            if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
            {
                UITabBar.Appearance.ScrollEdgeAppearance = tabAppearance;
            }
        }
        UITabBar.Appearance.BarTintColor = tabBarBackground;
        UITabBar.Appearance.BackgroundColor = tabBarBackground;
    }
}
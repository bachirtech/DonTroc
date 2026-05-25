using Foundation;
using UIKit;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.ApplicationModel;
using Google.MobileAds;
using UserNotifications;
using DonTroc.Services;

namespace DonTroc;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>
    /// Constructeur statique : exécuté la TOUTE 1ʳᵉ fois que la classe AppDelegate
    /// est chargée par le runtime .NET, avant CreateMauiApp et FinishedLaunching.
    /// On écrit ici un fichier "DEMARRAGE.txt" dans Documents pour confirmer que
    /// le runtime managé .NET a bien démarré. Si ce fichier n'apparaît pas dans
    /// l'app Fichiers iOS → DonTroc, c'est que l'app crashe AVANT tout code C#
    /// (problème natif : Mono, signature, dylibs Firebase, etc.).
    /// </summary>
    static AppDelegate()
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(docs))
                docs = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var marker = System.IO.Path.Combine(docs, "DEMARRAGE.txt");
            System.IO.File.WriteAllText(marker,
                $"AppDelegate.cctor OK\nDate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nDocs: {docs}\n");
        }
        catch { /* ne JAMAIS faire échouer un cctor */ }

        try { BootLogger.Log("==> AppDelegate.cctor() — runtime .NET vivant"); } catch { }
    }

    protected override MauiApp CreateMauiApp()
    {
        BootLogger.Log("AppDelegate.CreateMauiApp() → start");
        try
        {
            var app = MauiProgram.CreateMauiApp();
            BootLogger.Log("AppDelegate.CreateMauiApp() → OK");
            return app;
        }
        catch (Exception ex)
        {
            BootLogger.LogException("CreateMauiApp", ex);
            throw;
        }
    }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        BootLogger.Log("==> FinishedLaunching START");
        // ⚠️ IMPORTANT : Ne JAMAIS appeler base.FinishedLaunching() deux fois.
        // Si le premier appel échoue partiellement, MAUI serait dans un état invalide
        // et la création de scène UIKit crasherait ensuite (EXC_CRASH/SIGABRT).
        // On initialise les dépendances AVANT base.FinishedLaunching, chacune protégée
        // par son propre try-catch, pour ne pas bloquer le démarrage si l'une échoue.

        // 1. Keyboard manager
        try { Microsoft.Maui.Platform.KeyboardAutoManagerScroll.Disconnect(); BootLogger.Log("1. KeyboardAutoManagerScroll.Disconnect OK"); } catch (Exception ex) { BootLogger.LogException("Keyboard", ex); }

        // 2. Apparence UITabBar (doit être avant création des windows)
        try { ConfigureIosAppearance(); BootLogger.Log("2. ConfigureIosAppearance OK"); } catch (Exception ex) { BootLogger.LogException("UIAppearance", ex); }

        // 3. Firebase ⚠️ DOIT être avant base.FinishedLaunching (AuthService y accède via DI)
        try
        {
            Plugin.Firebase.Bundled.Platforms.iOS.CrossFirebase.Initialize(MauiProgram.CreateCrossFirebaseSettings());
            BootLogger.Log("3. Firebase.Initialize OK");
        }
        catch (Exception ex)
        {
            BootLogger.LogException("Firebase.Initialize", ex);
            try { System.Diagnostics.Debug.WriteLine($"[AppDelegate] Firebase.Initialize failed: {ex}"); } catch { }
            // On continue quand même — l'app démarrera mais les features Firebase échoueront.
        }

        // 4. AdMob
        try { MobileAds.SharedInstance.Start(completionHandler: null); BootLogger.Log("4. AdMob start OK"); } catch (Exception ex) { BootLogger.LogException("AdMob", ex); }

        // 5. Crashlytics
        try { Firebase.Crashlytics.Crashlytics.SharedInstance.SetCrashlyticsCollectionEnabled(true); BootLogger.Log("5. Crashlytics OK"); } catch (Exception ex) { BootLogger.LogException("Crashlytics", ex); }

        // 6. Notifications APNs (fire-and-forget, callback sur thread background)
        try
        {
            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
                (granted, _) =>
                {
                    if (granted)
                    {
                        try
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try { UIApplication.SharedApplication.RegisterForRemoteNotifications(); } catch { }
                            });
                        }
                        catch { }
                    }
                });
        }
        catch { }

        // 7. Démarrage MAUI — UN SEUL appel, jamais retryé.
        try
        {
            BootLogger.Log("7. base.FinishedLaunching → start");
            var result = base.FinishedLaunching(application, launchOptions);
            BootLogger.Log($"7. base.FinishedLaunching → returned {result}");
            return result;
        }
        catch (Exception ex)
        {
            BootLogger.LogException("base.FinishedLaunching", ex);
            try { System.Diagnostics.Debug.WriteLine($"[AppDelegate] base.FinishedLaunching failed: {ex}"); } catch { }
            try { Console.Error.WriteLine($"[AppDelegate] base.FinishedLaunching failed: {ex}"); } catch { }
            // ⚠️ NE PAS retenter base.FinishedLaunching ici — cela laisserait MAUI dans
            // un état corrompu et crasherait lors de la création de scène UIKit.
            return false;
        }
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
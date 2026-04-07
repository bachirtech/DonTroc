using Foundation;
using UIKit;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Firebase.Core;
using Google.MobileAds;

namespace DonTroc;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Initialiser Firebase
        Firebase.Core.App.Configure();
        
        // Initialiser Google Mobile Ads (AdMob)
        MobileAds.SharedInstance.Start(completionHandler: null);
        
        return base.FinishedLaunching(application, launchOptions);
    }
    
    // Gérer les URL schemes (pour Google Sign-In)
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        // Gérer le callback de Google Sign-In
        if (Google.SignIn.SignIn.SharedInstance.HandleUrl(url))
        {
            return true;
        }
        
        return base.OpenUrl(app, url, options);
    }
}
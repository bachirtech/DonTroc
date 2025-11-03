
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace DonTroc;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
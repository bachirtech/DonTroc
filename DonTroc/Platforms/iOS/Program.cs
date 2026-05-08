using ObjCRuntime;
using UIKit;

namespace DonTroc;

public class Program
{
    // Ceci est l'entrée principale de l'application iOS.
    static void Main(string[] args)
    {
        // Si vous souhaitez utiliser un test différent de UIApplicationDelegate ou si vous avez besoin de configurer quelque chose avant de lancer l'application,
        // vous pouvez le faire ici. Par exemple, vous pouvez utiliser un test différent de UIApplicationDelegate en passant son type à la méthode UIApplication.Main.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
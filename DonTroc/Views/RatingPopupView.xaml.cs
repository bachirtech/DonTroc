using Microsoft.Maui.ApplicationModel;

namespace DonTroc.Views;

/// <summary>
/// Popup élégant pour demander à l'utilisateur de noter l'application
/// </summary>
public partial class RatingPopupView : ContentView
{
    public event EventHandler<RatingResult>? RatingCompleted;

    public RatingPopupView()
    {
        InitializeComponent();
    }

    private async void OnRateClicked(object sender, EventArgs e)
    {
        try
        {
            // Animation de feedback
            await RateButton.ScaleTo(0.95, 50);
            await RateButton.ScaleTo(1.0, 50);

            // Ouvrir le store
            await OpenStoreForRating();
            
            RatingCompleted?.Invoke(this, RatingResult.Rated);
        }
        catch { }
    }

    private async void OnLaterClicked(object sender, EventArgs e)
    {
        try
        {
            // Animation de feedback
            await LaterButton.ScaleTo(0.95, 50);
            await LaterButton.ScaleTo(1.0, 50);
            
            RatingCompleted?.Invoke(this, RatingResult.Later);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du clic sur Plus tard: {ex.Message}");
        }
    }

    private void OnDontAskClicked(object sender, EventArgs e)
    {
        RatingCompleted?.Invoke(this, RatingResult.DontAskAgain);
    }

    /// <summary>
    /// Ouvre le store pour noter l'application
    /// </summary>
    private async Task OpenStoreForRating()
    {
        try
        {
#if ANDROID
            var packageName = AppInfo.PackageName;
            var uri = new Uri($"market://details?id={packageName}");
            await Launcher.OpenAsync(uri);
#elif IOS
            // À remplacer par votre App Store ID
            var appId = "VOTRE_APP_STORE_ID";
            var uri = new Uri($"itms-apps://itunes.apple.com/app/id{appId}?action=write-review");
            await Launcher.OpenAsync(uri);
#else
            await Shell.Current.DisplayAlert("Merci !", 
                "Merci de vouloir noter notre application ! 🙏", "OK");
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur ouverture store: {ex.Message}");
            
            // Fallback : Play Store web
            try
            {
#if ANDROID
                var packageName = AppInfo.PackageName;
                var webUri = new Uri($"https://play.google.com/store/apps/details?id={packageName}");
                await Browser.OpenAsync(webUri, BrowserLaunchMode.External);
#endif
            }
            catch
            {
                await Shell.Current.DisplayAlert("Merci !", 
                    "Merci pour votre soutien !", "OK");
            }
        }
    }

    /// <summary>
    /// Anime l'apparition du popup
    /// </summary>
    public async Task ShowAsync()
    {
        this.Opacity = 0;
        this.IsVisible = true;
        await this.FadeTo(1, 300, Easing.CubicOut);
    }

    /// <summary>
    /// Anime la disparition du popup
    /// </summary>
    public async Task HideAsync()
    {
        await this.FadeTo(0, 200, Easing.CubicIn);
        this.IsVisible = false;
    }
}

/// <summary>
/// Résultat de la demande de notation
/// </summary>
public enum RatingResult
{
    Rated,
    Later,
    DontAskAgain
}


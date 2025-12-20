using DonTroc.Configuration;
using Microsoft.Maui.ApplicationModel;

namespace DonTroc.Views;

/// <summary>
/// Popup élégant pour permettre aux utilisateurs de faire un don au développeur.
/// Propose plusieurs méthodes de paiement et des alternatives gratuites (noter, partager).
/// </summary>
public partial class DonationPopup : ContentPage
{
    public DonationPopup()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Ferme le popup de donation
    /// </summary>
    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync(true);
    }

    /// <summary>
    /// Ouvre le lien Ko-fi pour paiement direct par carte bancaire
    /// </summary>
    private async void OnKofiClicked(object? sender, EventArgs e)
    {
        await OpenDonationLink(DonationConfig.KofiUrl, "Ko-fi");
    }

    /// <summary>
    /// Ouvre le lien PayPal pour faire un don
    /// </summary>
    private async void OnPayPalClicked(object? sender, EventArgs e)
    {
        await OpenDonationLink(DonationConfig.PayPalUrl, "PayPal");
    }

    /// <summary>
    /// Ouvre une URL de donation de manière sécurisée
    /// </summary>
    private async Task OpenDonationLink(string url, string platformName)
    {
        try
        {
            var confirmed = await DisplayAlert(
                $"Ouvrir {platformName}",
                $"Vous allez être redirigé vers {platformName} dans votre navigateur.",
                "Continuer",
                "Annuler");

            if (confirmed)
            {
                await Launcher.OpenAsync(new Uri(url));
                
                // Affiche un message de remerciement après ouverture
                await Task.Delay(500);
                await DisplayAlert(
                    "Merci ! 🎉",
                    DonationConfig.ThankYouMessage,
                    "Avec plaisir !");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DonationPopup] Erreur lors de l'ouverture de {platformName}: {ex.Message}");
            await DisplayAlert(
                "Erreur",
                $"Impossible d'ouvrir {platformName}. Veuillez réessayer plus tard.",
                "OK");
        }
    }

    /// <summary>
    /// Redirige vers le store pour noter l'application
    /// </summary>
    private async void OnRateAppClicked(object? sender, EventArgs e)
    {
        try
        {
            string storeUrl;
            
#if ANDROID
            storeUrl = DonationConfig.PlayStoreUrl;
#elif IOS
            storeUrl = DonationConfig.AppStoreUrl;
#else
            storeUrl = DonationConfig.PlayStoreUrl;
#endif
            await Launcher.OpenAsync(new Uri(storeUrl));
            
            await DisplayAlert(
                "Merci ! ⭐",
                "Votre avis nous aide énormément à améliorer l'application et à toucher plus d'utilisateurs !",
                "Super !");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DonationPopup] Erreur lors de l'ouverture du store: {ex.Message}");
            await DisplayAlert("Erreur", "Impossible d'ouvrir le store.", "OK");
        }
    }

    /// <summary>
    /// Partage l'application avec d'autres utilisateurs
    /// </summary>
    private async void OnShareAppClicked(object? sender, EventArgs e)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "Découvrez DonTroc !",
                Text = DonationConfig.ShareMessage,
                Subject = "Découvrez DonTroc - Dons et échanges"
            });
            
            await DisplayAlert(
                "Merci ! 📤",
                "Merci de partager DonTroc avec vos proches ! Plus on est nombreux, plus on peut s'entraider !",
                "Avec plaisir !");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DonationPopup] Erreur lors du partage: {ex.Message}");
            await DisplayAlert("Erreur", "Impossible de partager l'application.", "OK");
        }
    }
}


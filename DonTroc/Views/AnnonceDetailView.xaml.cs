using DonTroc.Services;
using DonTroc.ViewModels;

namespace DonTroc.Views;

/// <summary>
/// Page de détail d'une annonce : photos, description, publieur, actions, annonces similaires.
/// </summary>
public partial class AnnonceDetailView : ContentPage
{
    private readonly AdMobService _adMobService;

    public AnnonceDetailView(AnnonceDetailViewModel viewModel, AdMobService adMobService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _adMobService = adMobService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Afficher un interstitiel lors de la navigation (avec limitation de fréquence)
        await _adMobService.TryShowInterstitialOnNavigationAsync("AnnonceDetail");
    }

    /// <summary>
    /// Tap sur le bouton favori : animation heart-pop + toggle
    /// </summary>
    private async void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        if (FavoriteBorder != null)
            _ = AnimationService.HeartPopAsync(FavoriteBorder);

        if (BindingContext is AnnonceDetailViewModel vm && vm.ToggleFavoriteCommand.CanExecute(null))
            vm.ToggleFavoriteCommand.Execute(null);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Intercepte le bouton retour matériel/geste Android pour éviter l'erreur
    /// "Ambiguous routes" de Shell quand AnnonceDetailView est empilée.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        // Exécuter le retour sécurisé sur le MainThread
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                var nav = Shell.Current.Navigation;
                if (nav.NavigationStack.Count > 1)
                {
                    await nav.PopAsync();
                }
                else
                {
                    await Shell.Current.GoToAsync("//AnnoncesView");
                }
            }
            catch
            {
                try { await Shell.Current.GoToAsync("//AnnoncesView"); }
                catch { /* abandon */ }
            }
        });

        // Return true = on a géré le retour, Shell ne doit pas le faire
        return true;
    }

    /// <summary>
    /// Met à jour l'index de la photo courante quand l'utilisateur swipe le carrousel.
    /// </summary>
    private void OnCarouselPositionChanged(object? sender, CurrentItemChangedEventArgs e)
    {
        if (BindingContext is AnnonceDetailViewModel vm && sender is CarouselView carousel)
        {
            var currentItem = e.CurrentItem as string;
            if (currentItem != null && vm.Annonce?.PhotosUrls != null)
            {
                var index = vm.Annonce.PhotosUrls.IndexOf(currentItem);
                if (index >= 0)
                    vm.CurrentPhotoIndex = index;
            }
        }
    }
}


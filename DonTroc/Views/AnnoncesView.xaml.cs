using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/AnnoncesView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue des annonces
public partial class AnnoncesView : ContentPage
{
    private readonly AdMobService _adMobService;

    // Le constructeur accepte maintenant un AnnoncesViewModel via l'injection de dépendances
    public AnnoncesView(AnnoncesViewModel viewModel, AdMobService adMobService)
    {
        // Initialise les composants de la vue (définis en XAML)
        InitializeComponent();

        // Définit le ViewModel comme contexte de liaison pour cette vue
        BindingContext = viewModel;
        _adMobService = adMobService;
    }

    // Méthode appelée chaque fois que la page apparaît
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Tenter d'afficher un interstitiel (avec limitation de fréquence)
        await _adMobService.TryShowInterstitialOnNavigationAsync("Annonces");

        // Utilise la nouvelle méthode OnAppearing du ViewModel
        if (BindingContext is AnnoncesViewModel vm)
        {
            vm.OnAppearing();
        }
    }
}
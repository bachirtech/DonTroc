using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/CreationAnnonceView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue de création d'annonce
public partial class CreationAnnonceView : ContentPage
{
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    // Le constructeur accepte maintenant un CreationAnnonceViewModel via l'injection de dépendances
    public CreationAnnonceView(CreationAnnonceViewModel viewModel, ITipsService tipsService)
    {
        // Initialise les composants de la vue (définis en XAML)
        InitializeComponent();

        // Définit le ViewModel comme contexte de liaison pour cette vue
        BindingContext = viewModel;
        _tipsService = tipsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Afficher les conseils pour la première utilisation
        if (!_tipsShown)
        {
            _tipsShown = true;
            await ShowTipsAsync();
        }
    }

    private async Task ShowTipsAsync()
    {
        try
        {
            // Attendre que la page soit complètement chargée
            await Task.Delay(500);

            if (await _tipsService.HasUnseenTipsAsync("creation_annonce"))
            {
                await TipOverlay.ShowTipAsync("creation_annonce", _tipsService);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur affichage conseils: {ex.Message}");
        }
    }
}

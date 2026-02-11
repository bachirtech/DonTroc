using DonTroc.Models;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/AnnoncesView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue des annonces
public partial class AnnoncesView : ContentPage
{
    private readonly AdMobService _adMobService;
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    // Le constructeur accepte maintenant un AnnoncesViewModel via l'injection de dépendances
    public AnnoncesView(AnnoncesViewModel viewModel, AdMobService adMobService, ITipsService tipsService)
    {
        // Initialise les composants de la vue (définis en XAML)
        InitializeComponent();

        // Définit le ViewModel comme contexte de liaison pour cette vue
        BindingContext = viewModel;
        _adMobService = adMobService;
        _tipsService = tipsService;
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

            if (await _tipsService.HasUnseenTipsAsync("annonces_list"))
            {
                await TipOverlay.ShowTipAsync("annonces_list", _tipsService);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur affichage conseils: {ex.Message}");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le tap sur l'image d'une annonce
    /// </summary>
    private void OnImageTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [AnnoncesView] OnImageTapped appelé");
        
        if (sender is BoxView boxView && boxView.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [AnnoncesView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.OpenImageViewerCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [AnnoncesView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Chat
    /// </summary>
    private void OnChatClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [AnnoncesView] OnChatClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [AnnoncesView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.GoToChatCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [AnnoncesView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Favoris
    /// </summary>
    private void OnFavoriteClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [AnnoncesView] OnFavoriteClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [AnnoncesView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.ToggleFavoriteCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [AnnoncesView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Partager
    /// </summary>
    private void OnShareClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [AnnoncesView] OnShareClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [AnnoncesView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.ShareAnnonceCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [AnnoncesView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Signaler
    /// </summary>
    private void OnReportClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [AnnoncesView] OnReportClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [AnnoncesView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.ReportAnnonceCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [AnnoncesView] Annonce non trouvée dans le BindingContext");
        }
    }
}
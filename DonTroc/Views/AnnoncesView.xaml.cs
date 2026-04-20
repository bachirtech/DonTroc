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
        catch { }
    }

    private void OnImageTapped(object? sender, TappedEventArgs e)
    {
        Annonce? annonce = null;
        
        // Essayer d'obtenir l'annonce depuis différentes sources
        if (sender is VisualElement element)
        {
            annonce = element.BindingContext as Annonce;
        }
        
        if (annonce != null)
        {
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.OpenImageViewerCommand.Execute(annonce);
            }
        }
    }

    /// <summary>
    /// Navigue vers la page de détail quand l'utilisateur clique sur la carte d'une annonce.
    /// </summary>
    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        Annonce? annonce = null;
        
        if (sender is VisualElement element)
        {
            annonce = element.BindingContext as Annonce;
        }
        
        if (annonce != null)
        {
            if (BindingContext is AnnoncesViewModel vm)
            {
                vm.NavigateToDetailCommand.Execute(annonce);
            }
        }
    }

    private async void OnChatClicked(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            // Animation de tap — scale down puis up
            await button.ScaleTo(0.8, 80, Easing.CubicIn);
            await button.ScaleTo(1.0, 80, Easing.CubicOut);
            
            if (button.BindingContext is Annonce annonce)
            {
                if (BindingContext is AnnoncesViewModel vm)
                    vm.GoToChatCommand.Execute(annonce);
            }
        }
    }

    /// <summary>
    /// Animation pulse continue sur le bouton chat quand il apparaît
    /// </summary>
    private async void OnChatButtonLoaded(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;

        // Petit délai aléatoire pour décaler les animations entre items
        await Task.Delay(Random.Shared.Next(0, 300));

        // Boucle d'animation pulse douce et infinie
        while (button.IsLoaded)
        {
            await button.ScaleTo(1.15, 600, Easing.SinInOut);
            await button.ScaleTo(1.0, 600, Easing.SinInOut);
            await Task.Delay(1500); // Pause entre les pulsations
        }
    }

    private void OnFavoriteClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            // Animation heart-pop style Instagram (avec haptic feedback)
            _ = AnimationService.HeartPopAsync(button);

            if (BindingContext is AnnoncesViewModel vm)
                vm.ToggleFavoriteCommand.Execute(annonce);
        }
    }

    /// <summary>
    /// Cascade d'apparition des cartes d'annonces : slide-up + fade-in
    /// avec un léger délai pour effet ondulant (style Instagram).
    /// </summary>
    private int _cardLoadIndex = 0;
    private async void OnAnnonceCardLoaded(object? sender, EventArgs e)
    {
        if (sender is not VisualElement element) return;

        // Délai cascade basé sur l'index courant (max 8 cartes pour pas trop attendre)
        var index = System.Threading.Interlocked.Increment(ref _cardLoadIndex);
        var delay = Math.Min(index, 8) * 60;

        // Reset l'état initial
        element.Opacity = 0;
        element.TranslationY = 30;

        await Task.Delay(delay);
        await AnimationService.SlideUpFadeInAsync(element);
    }

    private void OnShareClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            if (BindingContext is AnnoncesViewModel vm)
                vm.ShareAnnonceCommand.Execute(annonce);
        }
    }

    private void OnReportClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            if (BindingContext is AnnoncesViewModel vm)
                vm.ReportAnnonceCommand.Execute(annonce);
        }
    }
}
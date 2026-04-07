using System.Threading.Tasks;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class DashboardView : ContentPage
{
    private bool _animationsInitialized = false;
    private ITipsService? _tipsService;
    private bool _tipsShown = false;

    // Constructeur sans paramètre pour Shell
    public DashboardView()
    {
        InitializeComponent();
    }

    // Le constructeur accepte maintenant un DashboardViewModel via l'injection de dépendances
    public DashboardView(DashboardViewModel viewModel, ITipsService tipsService)
    {
        InitializeComponent();

        // Définit le ViewModel comme contexte de liaison pour cette vue
        BindingContext = viewModel;
        _tipsService = tipsService;
    }

    protected override async void OnAppearing() // méthode pour initialiser les animations
    {
        base.OnAppearing();
        if (!_animationsInitialized)
        {
            _animationsInitialized = true;
            StartButtonAnimations();
        }

        // Afficher les conseils pour la première utilisation
        if (!_tipsShown && _tipsService != null)
        {
            _tipsShown = true;
            await ShowTipsAsync();
        }
    }

    private async Task ShowTipsAsync() // Méthode d'affichage des conseils
    {
        try
        {
            await Task.Delay(800);
            if (_tipsService != null && await _tipsService.HasUnseenTipsAsync("dashboard"))
            {
                await TipOverlay.ShowTipAsync("dashboard", _tipsService);
            }
        }
        catch { }
    }

    private async void StartButtonAnimations() // Méthode d'annimations des boutons
    {
        // Animation de pulsation infinie pour les deux boutons
        var pulseAnimation = new Animation
        {
            { 0, 0.5, new Animation(v => DonnerButton.Scale = v, 1.0, 1.1, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => DonnerButton.Scale = v, 1.1, 1.0, Easing.SinInOut) }
        };

        var pulseAnimation2 = new Animation
        {
            { 0, 0.5, new Animation(v => ExplorerButton.Scale = v, 1.0, 1.1, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => ExplorerButton.Scale = v, 1.1, 1.0, Easing.SinInOut) }
        };

        pulseAnimation.Commit(DonnerButton, "PulsingDonner", 16, 2000, null, null, () => true);

        // Démarrer la deuxième animation avec un léger décalage
        await Task.Delay(1000);

        pulseAnimation2.Commit(ExplorerButton, "PulsingExplorer", 16, 2000, null, null, () => true);
    }
}
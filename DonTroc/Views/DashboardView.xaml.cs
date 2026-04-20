using System.Diagnostics.CodeAnalysis;
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
    private GamificationService? _gamificationService;
    private AuthService? _authService;
    private bool _tipsShown = false;
    private bool _dailyRewardChecked = false;
    private AdMobService? _adMobService;
    private bool _isFirstAppearing = true;

    // Constructeur sans paramètre pour Shell
    public DashboardView()
    {
        InitializeComponent();
    }

    // Le constructeur accepte maintenant un DashboardViewModel via l'injection de dépendances
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DashboardView))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DashboardViewModel))]
    public DashboardView(DashboardViewModel viewModel, ITipsService tipsService,
        GamificationService gamificationService, AuthService authService, AdMobService adMobService)
    {
        InitializeComponent();

        // Définit le ViewModel comme contexte de liaison pour cette vue
        BindingContext = viewModel;
        _tipsService = tipsService;
        _gamificationService = gamificationService;
        _authService = authService;
        _adMobService = adMobService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Interstitiel au retour sur le Dashboard (pas au premier affichage)
        if (!_isFirstAppearing && _adMobService != null)
        {
            await _adMobService.TryShowInterstitialOnNavigationAsync("Dashboard");
        }
        _isFirstAppearing = false;
        
        if (!_animationsInitialized)
        {
            _animationsInitialized = true;
            await RunEntryAnimationsAsync();
            StartButtonAnimations();

            // 🔥 Flamme du streak qui respire en boucle (vie permanente sur l'écran)
            if (DashboardStreakFlame != null)
                AnimationService.StartBreathingAnimation(DashboardStreakFlame, 1.0, 1.15, 1300);
        }

        // Pop-up récompense quotidienne (une seule fois par session)
        if (!_dailyRewardChecked && _gamificationService != null && _authService != null)
        {
            _dailyRewardChecked = true;
            try
            {
                // Petit délai pour laisser le Dashboard se charger visuellement
                await Task.Delay(600);
                await DailyRewardOverlay.TryShowAsync(_gamificationService, _authService);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardView] Erreur DailyReward popup: {ex.Message}");
            }
        }

        // Afficher les conseils pour la première utilisation
        if (!_tipsShown && _tipsService != null)
        {
            _tipsShown = true;
            await ShowTipsAsync();
        }
    }

    /// <summary>
    /// Animations d'entrée fluides au premier affichage de la page
    /// </summary>
    private async Task RunEntryAnimationsAsync()
    {
        try
        {
            // Légère animation de fondu + glissement pour les boutons
            DonnerButton.Opacity = 0;
            DonnerButton.TranslationY = 20;
            ExplorerButton.Opacity = 0;
            ExplorerButton.TranslationY = 20;

            await Task.Delay(300);

            // Animer le bouton Donner
            await Task.WhenAll(
                DonnerButton.FadeTo(1, 400, Easing.CubicOut),
                DonnerButton.TranslateTo(0, 0, 400, Easing.CubicOut)
            );

            // Petit délai puis animer le bouton Explorer
            await Task.Delay(100);
            await Task.WhenAll(
                ExplorerButton.FadeTo(1, 400, Easing.CubicOut),
                ExplorerButton.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
        catch
        {
            // En cas d'erreur, s'assurer que les boutons sont visibles
            DonnerButton.Opacity = 1;
            DonnerButton.TranslationY = 0;
            ExplorerButton.Opacity = 1;
            ExplorerButton.TranslationY = 0;
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Arrêter le breathing de la flamme pour économiser le CPU lors de la navigation
        if (DashboardStreakFlame != null)
            AnimationService.StopBreathingAnimation(DashboardStreakFlame);
    }
}
using System.Diagnostics.CodeAnalysis;
using DonTroc.Services;

namespace DonTroc.Views;

public partial class RewardsPage : ContentPage
{
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RewardsPage))]
    public RewardsPage(ViewModels.RewardsViewModel viewModel, ITipsService tipsService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _tipsService = tipsService;
    }
    
    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            if (BindingContext is ViewModels.RewardsViewModel vm)
            {
                vm.LoadDataCommand.Execute(null);

                // ✨ Animation count-up du total XP (style jackpot)
                _ = AnimateXpCountUpAsync(vm);
            }

            // Afficher les conseils pour la première utilisation
            if (!_tipsShown)
            {
                _tipsShown = true;
                await ShowTipsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RewardsPage] Erreur OnAppearing: {ex.Message}");
        }
    }

    /// <summary>
    /// Anime le compteur XP de 0 à la valeur réelle au démarrage
    /// (effet "jackpot" satisfaisant). Attend que les données soient chargées.
    /// </summary>
    private async Task AnimateXpCountUpAsync(ViewModels.RewardsViewModel vm)
    {
        try
        {
            // Petit délai pour laisser LoadData se terminer
            await Task.Delay(450);
            if (TotalXpLabel != null && vm.TotalXp > 0)
            {
                await AnimationService.CountUpAsync(TotalXpLabel, 0, vm.TotalXp,
                    duration: 1200, suffix: " XP");
            }
        }
        catch { }
    }


    private async void OnLeaderboardTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(LeaderboardView));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RewardsPage] Erreur navigation leaderboard: {ex.Message}");
        }
    }

    private async Task ShowTipsAsync()
    {
        try
        {
            await Task.Delay(500);
            if (await _tipsService.HasUnseenTipsAsync("rewards"))
            {
                await TipOverlay.ShowTipAsync("rewards", _tipsService);
            }
        }
        catch { }
    }
}


using DonTroc.Services;

namespace DonTroc.Views;

public partial class RewardsPage : ContentPage
{
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    public RewardsPage(ViewModels.RewardsViewModel viewModel, ITipsService tipsService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _tipsService = tipsService;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.RewardsViewModel vm)
        {
            vm.LoadDataCommand.Execute(null);
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
            await Task.Delay(500);
            if (await _tipsService.HasUnseenTipsAsync("rewards"))
            {
                await TipOverlay.ShowTipAsync("rewards", _tipsService);
            }
        }
        catch { }
    }
}


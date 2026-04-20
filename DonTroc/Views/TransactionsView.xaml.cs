using System.Diagnostics.CodeAnalysis;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class TransactionsView : ContentPage
{
    private readonly AdMobService _adMobService;
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TransactionsView))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TransactionsViewModel))]
    public TransactionsView(TransactionsViewModel viewModel, AdMobService adMobService, ITipsService tipsService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _adMobService = adMobService;
        _tipsService = tipsService;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            // Tenter d'afficher un interstitiel (avec limitation de fréquence)
            await _adMobService.TryShowInterstitialOnNavigationAsync("Transactions");
            
            if (BindingContext is TransactionsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
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
            System.Diagnostics.Debug.WriteLine($"[TransactionsView] Erreur OnAppearing: {ex.Message}");
        }
    }

    private async Task ShowTipsAsync()
    {
        try
        {
            await Task.Delay(500);
            if (await _tipsService.HasUnseenTipsAsync("transactions"))
            {
                await TipOverlay.ShowTipAsync("transactions", _tipsService);
            }
        }
        catch { }
    }
}

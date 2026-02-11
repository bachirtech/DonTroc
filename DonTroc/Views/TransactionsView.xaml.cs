using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class TransactionsView : ContentPage
{
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    public TransactionsView(TransactionsViewModel viewModel, ITipsService tipsService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _tipsService = tipsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur affichage conseils: {ex.Message}");
        }
    }
}

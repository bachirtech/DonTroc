using DonTroc.ViewModels;

namespace DonTroc.Views;

/// <summary>
/// Page affichant tous les badges disponibles et leur état (débloqué/verrouillé)
/// </summary>
public partial class AllBadgesPage : ContentPage
{
    private readonly AllBadgesViewModel _viewModel;

    public AllBadgesPage(AllBadgesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}


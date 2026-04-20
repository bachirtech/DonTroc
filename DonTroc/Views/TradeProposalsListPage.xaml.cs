using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class TradeProposalsListPage : ContentPage
{
    private readonly TradeProposalsListViewModel _vm;

    public TradeProposalsListPage(TradeProposalsListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}


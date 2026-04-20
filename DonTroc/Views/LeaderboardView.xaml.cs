using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class LeaderboardView : ContentPage
{
    public LeaderboardView(LeaderboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LeaderboardViewModel vm)
        {
            await vm.OnAppearingAsync();
        }
    }
}


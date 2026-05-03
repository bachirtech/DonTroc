using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class AdminEventsPage : ContentPage
{
    private readonly AdminEventsViewModel _viewModel;

    public AdminEventsPage(AdminEventsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}


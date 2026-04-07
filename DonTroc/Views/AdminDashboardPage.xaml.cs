using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _viewModel;
    
    public AdminDashboardPage(AdminDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadDataCommand.Execute(null);
    }
}


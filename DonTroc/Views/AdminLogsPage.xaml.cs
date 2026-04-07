using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class AdminLogsPage : ContentPage
{
    private readonly AdminLogsViewModel _viewModel;
    
    public AdminLogsPage(AdminLogsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadLogsCommand.Execute(null);
    }
}


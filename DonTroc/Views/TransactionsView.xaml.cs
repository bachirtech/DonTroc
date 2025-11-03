using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class TransactionsView : ContentPage
{
    public TransactionsView(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is TransactionsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}

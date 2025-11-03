using DonTroc.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

/// <summary>
/// Vue pour gérer les évaluations et le système de notation
/// </summary>
[QueryProperty(nameof(TransactionId), "TransactionId")]
public partial class RatingView : ContentPage
{
    private string _transactionId = string.Empty;
    
    public string TransactionId
    {
        get => _transactionId;
        set
        {
            _transactionId = value;
            OnPropertyChanged();
            // Initialiser l'évaluation quand l'ID de transaction est défini
            if (BindingContext is RatingViewModel viewModel && !string.IsNullOrEmpty(value))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await viewModel.InitialiserEvaluationAsync(value);
                });
            }
        }
    }

    public RatingView(RatingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Appelé lorsque la page apparaît
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Si l'ID de transaction est déjà défini, initialiser l'évaluation
        if (BindingContext is RatingViewModel viewModel && !string.IsNullOrEmpty(TransactionId))
        {
            await viewModel.InitialiserEvaluationAsync(TransactionId);
        }
    }
}

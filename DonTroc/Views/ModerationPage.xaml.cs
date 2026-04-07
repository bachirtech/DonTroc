using DonTroc.Models;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class ModerationPage : ContentPage
{
    private readonly ModerationViewModel _viewModel;
    
    public ModerationPage(ModerationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadReportsCommand.Execute(null);
    }

    // Gestionnaire pour le swipe "Détails"
    private void OnDetailsSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Report report)
        {
            _viewModel.ViewReportDetailsCommand.Execute(report);
        }
    }

    // Gestionnaire pour le swipe "Résoudre"
    private void OnResolveSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Report report)
        {
            _viewModel.UpdateReportStatusCommand.Execute(report);
        }
    }

    // Gestionnaire pour le swipe "Suspendre"
    private void OnSuspendSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Report report)
        {
            _viewModel.SuspendReportedUserCommand.Execute(report);
        }
    }

    // Gestionnaire pour le bouton d'action rapide
    private void OnQuickActionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Report report)
        {
            _viewModel.UpdateReportStatusCommand.Execute(report);
        }
    }

    // Gestionnaire pour le tap sur un signalement (sélection)
    private void OnReportTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Report report)
        {
            _viewModel.ToggleSelectionCommand.Execute(report);
        }
    }
}

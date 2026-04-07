using DonTroc.Models;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class UserManagementPage : ContentPage
{
    private readonly UserManagementViewModel _viewModel;
    
    public UserManagementPage(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadUsersCommand.Execute(null);
    }

    /// <summary>
    /// Gestionnaire pour le bouton de changement de rôle
    /// </summary>
    private void OnChangeRoleClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UserProfile user)
        {
            _viewModel.ChangeRoleCommand.Execute(user);
        }
    }

    /// <summary>
    /// Gestionnaire pour le bouton de détails
    /// </summary>
    private void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UserProfile user)
        {
            _viewModel.ViewUserDetailsCommand.Execute(user);
        }
    }

    /// <summary>
    /// Gestionnaire pour le swipe de suspension
    /// </summary>
    private void OnSuspendSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is UserProfile user)
        {
            _viewModel.SuspendUserCommand.Execute(user);
        }
    }

    /// <summary>
    /// Gestionnaire pour le swipe de réactivation
    /// </summary>
    private void OnUnsuspendSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is UserProfile user)
        {
            _viewModel.UnsuspendUserCommand.Execute(user);
        }
    }

    /// <summary>
    /// Gestionnaire pour le swipe de détails
    /// </summary>
    private void OnDetailsSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is UserProfile user)
        {
            _viewModel.ViewUserDetailsCommand.Execute(user);
        }
    }
}


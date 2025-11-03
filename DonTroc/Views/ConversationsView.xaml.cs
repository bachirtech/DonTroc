using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class ConversationsView : ContentPage
{
    // Garde une référence au ViewModel pour appeler ses commandes
    private readonly ConversationsViewModel _viewModel;

	public ConversationsView(ConversationsViewModel viewModel)
	{
		InitializeComponent();
        // Injecte et assigne le ViewModel au BindingContext
		BindingContext = viewModel;
        _viewModel = viewModel;
	}

    // Cette méthode est appelée chaque fois que la page devient visible
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Exécute la commande pour charger ou rafraîchir les conversations
        if (_viewModel.LoadConversationsCommand.CanExecute(null))
        {
            _viewModel.LoadConversationsCommand.Execute(null);
        }
    }
}

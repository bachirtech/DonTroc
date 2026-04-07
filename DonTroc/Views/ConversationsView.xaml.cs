using DonTroc.Models;
using DonTroc.Services;
using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class ConversationsView : ContentPage
{
    private readonly AdMobService _adMobService;

    // Garde une référence au ViewModel pour appeler ses commandes
    private readonly ConversationsViewModel _viewModel;

    public ConversationsView(ConversationsViewModel viewModel, AdMobService adMobService)
    {
        InitializeComponent();
        // Injecte et assigne le ViewModel au BindingContext
        BindingContext = viewModel;
        _viewModel = viewModel;
        _adMobService = adMobService;
    }

    // Cette méthode est appelée chaque fois que la page devient visible
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ⚠️ Pas d'interstitiel sur la page Messages — contenu critique (règles Google AdMob)

        // Exécute la commande pour charger ou rafraîchir les conversations
        if (_viewModel.LoadConversationsCommand.CanExecute(null))
        {
            _viewModel.LoadConversationsCommand.Execute(null);
        }
    }
}
using System;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views;

public partial class FavoritesView : ContentPage
{
    public FavoritesView(FavoritesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Charger les données à l'apparition de la page
        if (BindingContext is FavoritesViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }

    // Gestionnaires pour les onglets
    private void OnFavoritesTabClicked(object sender, EventArgs e)
    {
        // Mettre à jour l'apparence des boutons d'onglet
        FavoritesTabButton.BackgroundColor = Color.FromArgb("#512BD4"); // Primary color
        FavoritesTabButton.TextColor = Colors.White;
        AlertsTabButton.BackgroundColor = Colors.LightGray;
        AlertsTabButton.TextColor = Colors.Black;

        // Afficher le contenu des favoris
        FavoritesContent.IsVisible = true;
        AlertsContent.IsVisible = false;
    }

    private void OnAlertsTabClicked(object sender, EventArgs e)
    {
        // Mettre à jour l'apparence des boutons d'onglet
        AlertsTabButton.BackgroundColor = Color.FromArgb("#512BD4"); // Primary color
        AlertsTabButton.TextColor = Colors.White;
        FavoritesTabButton.BackgroundColor = Colors.LightGray;
        FavoritesTabButton.TextColor = Colors.Black;

        // Afficher le contenu des alertes
        FavoritesContent.IsVisible = false;
        AlertsContent.IsVisible = true;
    }

    // Gestionnaires pour les dialogues
    private void OnCancelCreateList(object sender, EventArgs e)
    {
        if (BindingContext is FavoritesViewModel viewModel)
        {
            viewModel.ShowCreateListDialog = false;
        }
    }

    private void OnCancelCreateAlert(object sender, EventArgs e)
    {
        if (BindingContext is FavoritesViewModel viewModel)
        {
            viewModel.ShowCreateAlertDialog = false;
        }
    }
}

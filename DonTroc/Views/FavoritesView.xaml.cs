using System;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views;

public partial class FavoritesView : ContentPage
{
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    public FavoritesView(FavoritesViewModel viewModel, ITipsService tipsService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _tipsService = tipsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Charger les données à l'apparition de la page
        if (BindingContext is FavoritesViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }

        // Afficher les conseils pour la première utilisation
        if (!_tipsShown)
        {
            _tipsShown = true;
            await ShowTipsAsync();
        }
    }

    private async Task ShowTipsAsync()
    {
        try
        {
            await Task.Delay(500);
            if (await _tipsService.HasUnseenTipsAsync("favoris"))
            {
                await TipOverlay.ShowTipAsync("favoris", _tipsService);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur affichage conseils: {ex.Message}");
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

    // Gestionnaires pour les favoris
    private void OnViewFavoriteClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [FavoritesView] OnViewFavoriteClicked appelé");
        
        if (sender is Button button && button.BindingContext is Favorite favorite)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesView] Favorite trouvé: {favorite.AnnonceId}");
            if (BindingContext is FavoritesViewModel vm)
            {
                vm.NavigateToAnnonceCommand.Execute(favorite);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [FavoritesView] Favorite non trouvé dans le BindingContext");
        }
    }

    private void OnDeleteFavoriteClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [FavoritesView] OnDeleteFavoriteClicked appelé");
        
        if (sender is Button button && button.BindingContext is Favorite favorite)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesView] Favorite trouvé: {favorite.AnnonceId}");
            if (BindingContext is FavoritesViewModel vm)
            {
                vm.DeleteFavoriteCommand.Execute(favorite);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [FavoritesView] Favorite non trouvé dans le BindingContext");
        }
    }

    private void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [FavoritesView] OnFavoriteTapped appelé");
        
        if (sender is Border border && border.BindingContext is Favorite favorite)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesView] Favorite trouvé: {favorite.AnnonceId}");
            if (BindingContext is FavoritesViewModel vm)
            {
                vm.NavigateToAnnonceCommand.Execute(favorite);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [FavoritesView] Favorite non trouvé dans le BindingContext");
        }
    }

    // Gestionnaire pour supprimer une alerte
    private void OnDeleteAlertClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [FavoritesView] OnDeleteAlertClicked appelé");
        
        if (sender is Button button && button.BindingContext is AnnonceAlert alert)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesView] Alert trouvé: {alert.Id}");
            if (BindingContext is FavoritesViewModel vm)
            {
                vm.DeleteAlertCommand.Execute(alert);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [FavoritesView] Alert non trouvé dans le BindingContext");
        }
    }
}

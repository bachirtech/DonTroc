using System;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views;

public partial class FavoritesView : ContentPage
{
    private readonly AdMobService _adMobService;
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    public FavoritesView(FavoritesViewModel viewModel, AdMobService adMobService, ITipsService tipsService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _adMobService = adMobService;
        _tipsService = tipsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Tenter d'afficher un interstitiel (avec limitation de fréquence)
        await _adMobService.TryShowInterstitialOnNavigationAsync("Favoris");
        
        // Charger les données à l'apparition de la page
        if (BindingContext is FavoritesViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }

        // 💔 Empty state animé : cœur qui bat doucement quand la liste est vide
        if (EmptyFavoriteIcon != null)
            AnimationService.StartBreathingAnimation(EmptyFavoriteIcon, 1.0, 1.18, 1100);

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

    // ─── Onglets ────────────────────────────────────────
    private void OnFavoritesTabClicked(object sender, EventArgs e)
    {
        FavoritesTabBorder.BackgroundColor = Color.FromArgb("#50FFFFFF");
        FavoritesTabButton.TextColor = Colors.White;
        FavoritesTabButton.FontAttributes = FontAttributes.Bold;
        AlertsTabBorder.BackgroundColor = Color.FromArgb("#20FFFFFF");
        AlertsTabButton.TextColor = Color.FromArgb("#D0FFFFFF");
        AlertsTabButton.FontAttributes = FontAttributes.None;

        FavoritesContent.IsVisible = true;
        AlertsContent.IsVisible = false;
    }

    private void OnAlertsTabClicked(object sender, EventArgs e)
    {
        AlertsTabBorder.BackgroundColor = Color.FromArgb("#50FFFFFF");
        AlertsTabButton.TextColor = Colors.White;
        AlertsTabButton.FontAttributes = FontAttributes.Bold;
        FavoritesTabBorder.BackgroundColor = Color.FromArgb("#20FFFFFF");
        FavoritesTabButton.TextColor = Color.FromArgb("#D0FFFFFF");
        FavoritesTabButton.FontAttributes = FontAttributes.None;

        FavoritesContent.IsVisible = false;
        AlertsContent.IsVisible = true;
    }

    // ─── Dialogues ──────────────────────────────────────
    private void OnCancelCreateList(object sender, EventArgs e)
    {
        if (BindingContext is FavoritesViewModel viewModel)
            viewModel.ShowCreateListDialog = false;
    }

    private void OnCancelCreateAlert(object sender, EventArgs e)
    {
        if (BindingContext is FavoritesViewModel viewModel)
            viewModel.ShowCreateAlertDialog = false;
    }

    // ─── Favoris ────────────────────────────────────────
    private void OnViewFavoriteClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Favorite favorite)
        {
            if (BindingContext is FavoritesViewModel vm)
                vm.NavigateToAnnonceCommand.Execute(favorite);
        }
    }

    private void OnDeleteFavoriteClicked(object? sender, EventArgs e)
    {
        // Maintenant déclenché par TapGestureRecognizer sur Border
        Favorite? favorite = null;
        if (sender is Border border && border.BindingContext is Favorite f)
            favorite = f;
        else if (sender is Button button && button.BindingContext is Favorite f2)
            favorite = f2;

        if (favorite != null && BindingContext is FavoritesViewModel vm)
            vm.DeleteFavoriteCommand.Execute(favorite);
    }

    private void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is Favorite favorite)
        {
            if (BindingContext is FavoritesViewModel vm)
                vm.NavigateToAnnonceCommand.Execute(favorite);
        }
    }

    // ─── Alertes ────────────────────────────────────────
    private void OnDeleteAlertClicked(object? sender, EventArgs e)
    {
        AnnonceAlert? alert = null;
        if (sender is Border border && border.BindingContext is AnnonceAlert a)
            alert = a;
        else if (sender is Button button && button.BindingContext is AnnonceAlert a2)
            alert = a2;

        if (alert != null && BindingContext is FavoritesViewModel vm)
            vm.DeleteAlertCommand.Execute(alert);
    }

    private void OnToggleAlertClicked(object? sender, EventArgs e)
    {
        AnnonceAlert? alert = null;
        if (sender is Border border && border.BindingContext is AnnonceAlert a)
            alert = a;
        else if (sender is Button button && button.BindingContext is AnnonceAlert a2)
            alert = a2;

        if (alert != null && BindingContext is FavoritesViewModel vm)
            vm.ToggleAlertCommand.Execute(alert);
    }

    private void OnCategoryChipTapped(object? sender, EventArgs e)
    {
        if (sender is Border border && border.BindingContext is AlertCategoryItem item
            && BindingContext is FavoritesViewModel vm)
        {
            vm.ToggleCategoryCommand.Execute(item);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (EmptyFavoriteIcon != null) AnimationService.StopBreathingAnimation(EmptyFavoriteIcon);
    }
}

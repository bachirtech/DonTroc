using System;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/ProfilView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue du profil utilisateur
public partial class ProfilView : ContentPage
{
    private readonly AdMobService _adMobService;
    private readonly ITipsService _tipsService;
    private bool _tipsShown = false;

    // Le constructeur accepte maintenant un ProfilViewModel via l'injection de dépendances
    public ProfilView(ProfilViewModel viewModel, AdMobService adMobService, ITipsService tipsService)
    {
        // Initialise les composants de la vue (définis en XAML)
        InitializeComponent();

        // Définit le ViewModel comme contexte de liaison pour cette vue
        BindingContext = viewModel;
        _adMobService = adMobService;
        _tipsService = tipsService;

        // 🎬 Effet parallax sur le header au scroll
        if (ProfilScrollView != null)
            ProfilScrollView.Scrolled += OnProfilScrolled;
    }

    private void OnProfilScrolled(object? sender, ScrolledEventArgs e)
    {
        try
        {
            if (ProfilHeaderBorder == null) return;

            // Le header se déplace à 50% de la vitesse du scroll (effet parallax pro)
            ProfilHeaderBorder.TranslationY = e.ScrollY * 0.5;

            // Légère atténuation de l'opacité pour fondre dans le contenu
            var fade = Math.Max(0.4, 1 - e.ScrollY / 350.0);
            ProfilHeaderBorder.Opacity = fade;
        }
        catch { /* anim non critique */ }
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            // ⚠️ Pas d'interstitiel sur la page Profil — page personnelle critique

            if (BindingContext is not ProfilViewModel vm) return;
            await vm.LoadUserProfile();
            await vm.ExecuteLoadMesAnnoncesCommand();

            // Afficher les conseils pour la première utilisation
            if (!_tipsShown)
            {
                _tipsShown = true;
                await ShowTipsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfilView] Erreur OnAppearing: {ex.Message}");
        }
    }

    private async Task ShowTipsAsync()
    {
        try
        {
            await Task.Delay(500);
            if (await _tipsService.HasUnseenTipsAsync("profil"))
            {
                await TipOverlay.ShowTipAsync("profil", _tipsService);
            }
        }
        catch { }
    }

    private void OnEditAnnonceClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            if (BindingContext is ProfilViewModel vm)
                vm.EditAnnonceCommand.Execute(annonce);
        }
    }

    private void OnDeleteAnnonceClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            if (BindingContext is ProfilViewModel vm)
                vm.DeleteAnnonceCommand.Execute(annonce);
        }
    }

    private void OnBoostAnnonceClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            if (BindingContext is ProfilViewModel vm)
                vm.BoostAnnonceCommand.Execute(annonce);
        }
    }
}
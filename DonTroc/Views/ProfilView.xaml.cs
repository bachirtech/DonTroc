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
    }

    protected override async void OnAppearing() // Méthode appelée lorsque la vue apparaît
    {
        base.OnAppearing();

        // Tenter d'afficher un interstitiel (avec limitation de fréquence)
        await _adMobService.TryShowInterstitialOnNavigationAsync("Profil");

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur affichage conseils: {ex.Message}");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Modifier une annonce
    /// </summary>
    private void OnEditAnnonceClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [ProfilView] OnEditAnnonceClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [ProfilView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is ProfilViewModel vm)
            {
                vm.EditAnnonceCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [ProfilView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Supprimer une annonce
    /// </summary>
    private void OnDeleteAnnonceClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [ProfilView] OnDeleteAnnonceClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [ProfilView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is ProfilViewModel vm)
            {
                vm.DeleteAnnonceCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [ProfilView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton Booster une annonce
    /// </summary>
    private void OnBoostAnnonceClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔵 [ProfilView] OnBoostAnnonceClicked appelé");
        
        if (sender is Button button && button.BindingContext is Annonce annonce)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [ProfilView] Annonce trouvée: {annonce.Id}");
            if (BindingContext is ProfilViewModel vm)
            {
                vm.BoostAnnonceCommand.Execute(annonce);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ [ProfilView] Annonce non trouvée dans le BindingContext");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour le bouton "Changer" du sélecteur de thème
    /// </summary>
    private async void OnThemeButtonClicked(object sender, EventArgs e)
    {
        if (BindingContext is not ProfilViewModel viewModel) return;

        try
        {
            // Afficher une ActionSheet avec les options de thème disponibles
            var result = await DisplayActionSheet(
                "Choisir l'apparence",
                "Annuler",
                null,
                viewModel.AvailableThemes.ToArray()
            );

            // Si l'utilisateur a sélectionné une option valide
            if (!string.IsNullOrEmpty(result) && result != "Annuler")
            {
                // Exécuter la commande pour changer le thème
                if (viewModel.ChangeThemeCommand.CanExecute(result))
                {
                    viewModel.ChangeThemeCommand.Execute(result);

                    // Afficher une confirmation subtile
                    await DisplayAlert("✨ Thème modifié", $"L'apparence a été changée en : {result}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            // Gestion d'erreur en cas de problème
            await DisplayAlert("Erreur", $"Impossible de changer le thème : {ex.Message}", "OK");
        }
    }
}
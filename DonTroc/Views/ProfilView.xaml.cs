using System;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/ProfilView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue du profil utilisateur
public partial class ProfilView : ContentPage
{
	// Le constructeur accepte maintenant un ProfilViewModel via l'injection de dépendances
	public ProfilView(ProfilViewModel viewModel)
	{
		// Initialise les composants de la vue (définis en XAML)
		InitializeComponent();

		// Définit le ViewModel comme contexte de liaison pour cette vue
		BindingContext = viewModel;
	}
	
	protected override async void OnAppearing() // Méthode appelée lorsque la vue apparaît
	{
		base.OnAppearing();
		if (BindingContext is not ProfilViewModel vm) return;
		await vm.LoadUserProfile();
		await vm.ExecuteLoadMesAnnoncesCommand();
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

using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/CreationAnnonceView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue de création d'annonce
public partial class CreationAnnonceView : ContentPage
{
	// Le constructeur accepte maintenant un CreationAnnonceViewModel via l'injection de dépendances
	public CreationAnnonceView(CreationAnnonceViewModel viewModel)
	{
		// Initialise les composants de la vue (définis en XAML)
		InitializeComponent();

		// Définit le ViewModel comme contexte de liaison pour cette vue
		BindingContext = viewModel;
	}
}

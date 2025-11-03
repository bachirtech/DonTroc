using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/EditAnnonceView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue de modification d'annonce
public partial class EditAnnonceView : ContentPage
{
	// Le constructeur accepte maintenant un EditAnnonceViewModel via l'injection de dépendances
	public EditAnnonceView(EditAnnonceViewModel viewModel)
	{
		// Initialise les composants de la vue (définis en XAML)
		InitializeComponent();

		// Définit le ViewModel comme contexte de liaison pour cette vue
		BindingContext = viewModel;
	}
}

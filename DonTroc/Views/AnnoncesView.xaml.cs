using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
// Fichier: DonTroc/Views/AnnoncesView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue des annonces
public partial class AnnoncesView : ContentPage
{
	// Le constructeur accepte maintenant un AnnoncesViewModel via l'injection de dépendances
	public AnnoncesView(AnnoncesViewModel viewModel)
	{
		// Initialise les composants de la vue (définis en XAML)
		InitializeComponent();

		// Définit le ViewModel comme contexte de liaison pour cette vue
		BindingContext = viewModel;
	}

    // Méthode appelée chaque fois que la page apparaît
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Utilise la nouvelle méthode OnAppearing du ViewModel
        if (BindingContext is AnnoncesViewModel vm)
        {
            vm.OnAppearing();
        }
    }
}

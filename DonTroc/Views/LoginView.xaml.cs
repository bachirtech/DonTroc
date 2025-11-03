using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

// Fichier: DonTroc/Views/LoginView.xaml.cs

namespace DonTroc.Views;

// Classe associée à la vue de connexion/inscription
public partial class LoginView : ContentPage
{
	// Le constructeur accepte maintenant un LoginViewModel via l'injection de dépendances
	public LoginView(LoginViewModel viewModel)
	{
		// Initialise les composants de la vue (définis en XAML)
		InitializeComponent();

		// Définit le ViewModel comme contexte de liaison pour cette vue
		BindingContext = viewModel;
		
	}
}

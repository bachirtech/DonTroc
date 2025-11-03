using Microsoft.Maui.Controls;

namespace DonTroc.Views;

using DonTroc.ViewModels;

public partial class EditProfileView : ContentPage
{
	public EditProfileView(EditProfileViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}


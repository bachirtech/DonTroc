using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

using DonTroc.ViewModels;

public partial class EditProfileView : ContentPage
{
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EditProfileView))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EditProfileViewModel))]
	public EditProfileView(EditProfileViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}


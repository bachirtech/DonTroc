using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class OnboardingView : ContentPage
{
    public OnboardingView(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}


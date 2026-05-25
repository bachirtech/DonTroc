using DonTroc.Services;
using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class OnboardingView : ContentPage
{
    public OnboardingView(OnboardingViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            BootLogger.Log("OnboardingView → InitializeComponent OK");
        }
        catch (Exception ex)
        {
            BootLogger.LogException("OnboardingView.InitializeComponent", ex);
            throw;
        }
        BindingContext = viewModel;
        BootLogger.Log($"OnboardingView → BindingContext set (Slides={viewModel.Slides?.Count ?? -1})");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BootLogger.Log("OnboardingView → OnAppearing ✅");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BootLogger.Log("OnboardingView → OnDisappearing");
    }
}

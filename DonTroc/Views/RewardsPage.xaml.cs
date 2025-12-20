namespace DonTroc.Views;

public partial class RewardsPage : ContentPage
{
    public RewardsPage(ViewModels.RewardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.RewardsViewModel vm)
        {
            vm.LoadDataCommand.Execute(null);
        }
    }
}


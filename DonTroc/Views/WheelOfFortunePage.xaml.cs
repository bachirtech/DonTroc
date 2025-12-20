using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class WheelOfFortunePage : ContentPage
{
    private readonly WheelOfFortuneViewModel _viewModel;

    public WheelOfFortunePage(WheelOfFortuneViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // S'abonner à l'événement d'animation
        _viewModel.OnSpinWheelAnimation += AnimateWheelAsync;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Se désabonner pour éviter les fuites mémoire
        _viewModel.OnSpinWheelAnimation -= AnimateWheelAsync;
    }

    /// <summary>
    /// Animation de rotation de la roue avec effet visuel amélioré
    /// </summary>
    public async Task AnimateWheelAsync(int rotations = 5)
    {
        if (WheelContainer != null)
        {
            // Réinitialiser la rotation pour partir de 0
            WheelContainer.Rotation = 0;
            
            // Nombre aléatoire de rotations supplémentaires pour l'effet de hasard
            var random = new Random();
            var extraDegrees = random.Next(0, 360);
            var totalDegrees = (360 * rotations) + extraDegrees;
            var totalDuration = (uint)(3000 + (rotations * 200)); // Durée proportionnelle
            
            // Animation avec effet de ralentissement progressif (CubicOut)
            await WheelContainer.RotateTo(totalDegrees, totalDuration, Easing.CubicOut);
            
            // Garder la position finale pour l'effet visuel
            WheelContainer.Rotation = WheelContainer.Rotation % 360;
        }
    }
}


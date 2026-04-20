using DonTroc.Services;
using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _viewModel;
    private readonly AdMobService _adMobService;

    public QuizPage(QuizViewModel viewModel, AdMobService adMobService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _adMobService = adMobService;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            // Tenter d'afficher un interstitiel (avec limitation de fréquence)
            await _adMobService.TryShowInterstitialOnNavigationAsync("Quiz");
            
            await _viewModel.InitializeCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QuizPage] Erreur OnAppearing: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // S'assurer que le timer est arrêté
        _viewModel.CloseQuizCommand.Execute(null);
    }

    private async void OnAnswerTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[QuizPage] OnAnswerTapped appelé, CommandParameter: {e.Parameter}");
        
        if (e.Parameter != null)
        {
            await _viewModel.SelectAnswerCommand.ExecuteAsync(e.Parameter);
        }
    }
}


using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _viewModel;

    public QuizPage(QuizViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeCommand.ExecuteAsync(null);
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


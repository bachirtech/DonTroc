// Fichier: DonTroc/ViewModels/OnboardingViewModel.cs

using System.Collections.ObjectModel;
using System.Windows.Input;
using DonTroc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonTroc.ViewModels;

/// <summary>
/// Modèle d'un slide d'onboarding
/// </summary>
public class OnboardingSlide
{
    public string Emoji { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool ShowNotificationButton { get; set; }
}

public class OnboardingViewModel : BaseViewModel
{
    private readonly OnboardingService _onboardingService;
    private readonly NotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<OnboardingSlide> Slides { get; } = new();

    private int _currentPosition;
    public int CurrentPosition
    {
        get => _currentPosition;
        set
        {
            if (SetProperty(ref _currentPosition, value))
            {
                OnPropertyChanged(nameof(IsLastSlide));
                OnPropertyChanged(nameof(ButtonText));
            }
        }
    }

    public bool IsLastSlide => CurrentPosition >= Slides.Count - 1;
    public string ButtonText => IsLastSlide ? "C'est parti ! 🚀" : "Suivant →";

    private bool _notificationsEnabled;
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public ICommand NextCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand RequestNotificationsCommand { get; }

    public OnboardingViewModel(
        OnboardingService onboardingService,
        NotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _onboardingService = onboardingService;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;

        // Définir les 4 slides
        Slides.Add(new OnboardingSlide
        {
            Emoji = "🤝",
            Title = "Bienvenue sur DonTroc !",
            Description = "La plateforme de dons et d'échanges entre particuliers.\nDonnez une seconde vie à vos objets et trouvez des trésors près de chez vous !"
        });

        Slides.Add(new OnboardingSlide
        {
            Emoji = "📦",
            Title = "Donnez ou échangez",
            Description = "Créez une annonce en quelques secondes.\nPrenez une photo, ajoutez une description et publiez !\nVos voisins recevront une notification."
        });

        Slides.Add(new OnboardingSlide
        {
            Emoji = "🏆",
            Title = "Gagnez des récompenses",
            Description = "Chaque action vous rapporte des XP !\nMontez de niveau, débloquez des badges et participez au classement.\nJouez au quiz quotidien et tournez la roue de la fortune."
        });

        Slides.Add(new OnboardingSlide
        {
            Emoji = "🔔",
            Title = "Restez informé",
            Description = "Activez les notifications pour ne rien manquer :\n• Nouvelles annonces dans votre zone\n• Messages et réponses\n• Rappels de récompenses quotidiennes",
            ShowNotificationButton = true
        });

        NextCommand = new Command(async void () => await OnNext());
        SkipCommand = new Command(async void () => await FinishOnboarding());
        RequestNotificationsCommand = new Command(async void () => await RequestNotifications());
    }

    private async Task OnNext()
    {
        if (IsLastSlide)
        {
            await FinishOnboarding();
        }
        else
        {
            CurrentPosition++;
        }
    }

    private async Task RequestNotifications()
    {
        try
        {
            await _notificationService.RequestNotificationPermissionAsync();
            NotificationsEnabled = true;

            await Application.Current?.MainPage?.DisplayAlert("Notifications activées ! 🔔",
                "Vous recevrez des notifications pour les nouvelles annonces et messages.", "Super !")!;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onboarding] Erreur permission notif: {ex.Message}");
        }
    }

    private Task FinishOnboarding()
    {
        try
        {
            _onboardingService.MarkOnboardingCompleted();

            // Naviguer vers la page de connexion (l'onboarding est juste une introduction)
            // L'utilisateur DOIT s'authentifier avant d'accéder à l'app
            var loginView = _serviceProvider.GetRequiredService<Views.LoginView>();
            Application.Current!.MainPage = new NavigationPage(loginView);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onboarding] Erreur navigation: {ex.Message}");
        }
        return Task.CompletedTask;
    }
}


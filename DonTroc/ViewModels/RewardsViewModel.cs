using System.Collections.ObjectModel;
using System.Windows.Input;
using DonTroc.Configuration;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;

namespace DonTroc.ViewModels;

// Alias pour éviter les ambiguïtés
using Challenge = DonTroc.Models.Challenge;
using Badge = DonTroc.Models.Badge;
using WheelReward = DonTroc.Models.WheelReward;

/// <summary>
/// ViewModel pour la page de récompenses et gamification
/// </summary>
public class RewardsViewModel : BaseViewModel
{
    private readonly IGamificationService _gamificationService;
    private readonly AuthService _authService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<RewardsViewModel> _logger;

    // Propriétés de profil
    private int _currentLevel = 1;
    private int _totalXp;
    private int _currentLevelXp;
    private int _xpForNextLevel = 100;
    private int _dailyStreak;
    private int _boostCredits;
    private string _levelTitle = "Nouveau Troqueur";
    private double _progressBarWidth;

    // États des boutons
    private bool _canSpinWheel = true;
    private bool _canClaimDailyReward = true;
    private string _wheelStatus = "Disponible !";
    private string _dailyRewardStatus = "Récupérer !";
    private Color _wheelStatusColor = Colors.Green;
    private Color _dailyRewardStatusColor = Colors.Green;
    
    // Rappel Quiz
    private bool _isQuizReminderEnabled;
    private TimeSpan _quizReminderTime = new TimeSpan(10, 0, 0); // 10h00 par défaut

    // Collections
    public ObservableCollection<Challenge> DailyChallenges { get; } = new();
    public ObservableCollection<BadgeDisplay> UnlockedBadges { get; } = new();
    public ObservableCollection<XpTransaction> RecentXpTransactions { get; } = new();

    public RewardsViewModel(
        IGamificationService gamificationService,
        AuthService authService,
        NotificationService notificationService,
        ILogger<RewardsViewModel> logger)
    {
        _gamificationService = gamificationService;
        _authService = authService;
        _notificationService = notificationService;
        _logger = logger;

        // Charger l'état du rappel
        _isQuizReminderEnabled = _notificationService.IsQuizReminderEnabled();
        var (hour, minute) = _notificationService.GetQuizReminderTime();
        _quizReminderTime = new TimeSpan(hour, minute, 0);

        // Commandes
        LoadDataCommand = new Command(async () => await LoadDataAsync());
        SpinWheelCommand = new Command(async () => await SpinWheelAsync());
        ClaimDailyRewardCommand = new Command(async () => await ClaimDailyRewardAsync());
        ViewAllBadgesCommand = new Command(async () => await ViewAllBadgesAsync());
        OpenQuizCommand = new Command(async () => await OpenQuizAsync());
        ToggleQuizReminderCommand = new Command(async () => await ToggleQuizReminderAsync());
        SetQuizReminderTimeCommand = new Command(async () => await SetQuizReminderTimeAsync());
    }

    #region Propriétés

    public int CurrentLevel
    {
        get => _currentLevel;
        set => SetProperty(ref _currentLevel, value);
    }

    public int NextLevel => CurrentLevel + 1;

    public int TotalXp
    {
        get => _totalXp;
        set => SetProperty(ref _totalXp, value);
    }

    public int CurrentLevelXp
    {
        get => _currentLevelXp;
        set => SetProperty(ref _currentLevelXp, value);
    }

    public int XpForNextLevel
    {
        get => _xpForNextLevel;
        set => SetProperty(ref _xpForNextLevel, value);
    }

    public int DailyStreak
    {
        get => _dailyStreak;
        set => SetProperty(ref _dailyStreak, value);
    }

    public int BoostCredits
    {
        get => _boostCredits;
        set => SetProperty(ref _boostCredits, value);
    }

    public string LevelTitle
    {
        get => _levelTitle;
        set => SetProperty(ref _levelTitle, value);
    }

    public double ProgressBarWidth
    {
        get => _progressBarWidth;
        set => SetProperty(ref _progressBarWidth, value);
    }

    public string WheelStatus
    {
        get => _wheelStatus;
        set => SetProperty(ref _wheelStatus, value);
    }

    public string DailyRewardStatus
    {
        get => _dailyRewardStatus;
        set => SetProperty(ref _dailyRewardStatus, value);
    }

    public Color WheelStatusColor
    {
        get => _wheelStatusColor;
        set => SetProperty(ref _wheelStatusColor, value);
    }

    public Color DailyRewardStatusColor
    {
        get => _dailyRewardStatusColor;
        set => SetProperty(ref _dailyRewardStatusColor, value);
    }

    public int BadgeCount => UnlockedBadges.Count;
    public bool HasNoBadges => UnlockedBadges.Count == 0;
    public bool HasXpHistory => RecentXpTransactions.Count > 0;
    
    public bool IsQuizReminderEnabled
    {
        get => _isQuizReminderEnabled;
        set => SetProperty(ref _isQuizReminderEnabled, value);
    }

    public TimeSpan QuizReminderTime
    {
        get => _quizReminderTime;
        set => SetProperty(ref _quizReminderTime, value);
    }

    public string QuizReminderTimeFormatted => $"{QuizReminderTime.Hours:D2}:{QuizReminderTime.Minutes:D2}";

    #endregion

    #region Commandes

    public ICommand LoadDataCommand { get; }
    public ICommand SpinWheelCommand { get; }
    public ICommand ClaimDailyRewardCommand { get; }
    public ICommand ViewAllBadgesCommand { get; }
    public ICommand OpenQuizCommand { get; }
    public ICommand ToggleQuizReminderCommand { get; }
    public ICommand SetQuizReminderTimeCommand { get; }

    #endregion

    #region Méthodes

    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            // Charger le profil
            var profile = await _gamificationService.GetUserProfileAsync(userId);
            
            CurrentLevel = profile.CurrentLevel;
            TotalXp = profile.TotalXp;
            CurrentLevelXp = profile.CurrentLevelXp;
            XpForNextLevel = profile.XpForNextLevel;
            DailyStreak = profile.DailyStreak;
            BoostCredits = profile.BoostCredits;
            LevelTitle = GamificationConfig.GetLevelTitle(profile.CurrentLevel);
            
            // Calculer la largeur de la barre de progression (max 300px)
            ProgressBarWidth = Math.Min(300, (profile.LevelProgress / 100.0) * 300);

            // États des boutons
            _canSpinWheel = await _gamificationService.CanSpinWheelAsync(userId);
            _canClaimDailyReward = await _gamificationService.CanClaimDailyRewardAsync(userId);
            
            UpdateButtonStates();

            // Charger les défis
            await LoadChallengesAsync(userId);

            // Charger les badges
            await LoadBadgesAsync(userId);

            OnPropertyChanged(nameof(BadgeCount));
            OnPropertyChanged(nameof(HasNoBadges));
            OnPropertyChanged(nameof(HasXpHistory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des données de récompenses");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateButtonStates()
    {
        if (_canSpinWheel)
        {
            WheelStatus = "Disponible !";
            WheelStatusColor = Colors.Green;
        }
        else
        {
            WheelStatus = "Demain";
            WheelStatusColor = Colors.Gray;
        }

        if (_canClaimDailyReward)
        {
            DailyRewardStatus = "Récupérer !";
            DailyRewardStatusColor = Colors.Green;
        }
        else
        {
            DailyRewardStatus = "Récupéré ✓";
            DailyRewardStatusColor = Colors.Gray;
        }
    }

    private async Task LoadChallengesAsync(string userId)
    {
        try
        {
            var challenges = await _gamificationService.GetActiveChallengesAsync(userId);
            
            DailyChallenges.Clear();
            foreach (var challenge in challenges.Where(c => c.Type == ChallengeType.Daily))
            {
                DailyChallenges.Add(challenge);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des défis");
        }
    }

    private async Task LoadBadgesAsync(string userId)
    {
        try
        {
            var badges = await _gamificationService.GetUserBadgesAsync(userId);
            
            UnlockedBadges.Clear();
            foreach (var badge in badges.Take(10)) // Afficher les 10 derniers
            {
                UnlockedBadges.Add(new BadgeDisplay
                {
                    Icon = badge.Icon,
                    Name = badge.Name,
                    RarityColor = GetRarityColor(badge.Rarity)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des badges");
        }
    }

    private Color GetRarityColor(BadgeRarity rarity)
    {
        return rarity switch
        {
            BadgeRarity.Common => Color.FromArgb("#9E9E9E"),
            BadgeRarity.Uncommon => Color.FromArgb("#4CAF50"),
            BadgeRarity.Rare => Color.FromArgb("#2196F3"),
            BadgeRarity.Epic => Color.FromArgb("#9C27B0"),
            BadgeRarity.Legendary => Color.FromArgb("#FF9800"),
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    private async Task SpinWheelAsync()
    {
        try
        {
            // Naviguer vers la page de la roue de la fortune
            await Shell.Current.GoToAsync("WheelOfFortunePage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture de la roue de la fortune");
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir la roue de la fortune.", "OK");
        }
    }

    private async Task ClaimDailyRewardAsync()
    {
        if (!_canClaimDailyReward)
        {
            await Shell.Current.DisplayAlert("Récompense Quotidienne", 
                "Vous avez déjà récupéré votre récompense aujourd'hui. Revenez demain !", "OK");
            return;
        }

        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var notification = await _gamificationService.ClaimDailyRewardAsync(userId);

            if (notification != null)
            {
                await Shell.Current.DisplayAlert(notification.Title, notification.Message, "Merci !");
            }

            _canClaimDailyReward = false;
            UpdateButtonStates();
            await LoadDataAsync(); // Rafraîchir les données
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réclamation de la récompense quotidienne");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de récupérer la récompense.", "OK");
        }
    }

    private async Task ViewAllBadgesAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("AllBadgesPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture de la page des badges");
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir la page des badges.", "OK");
        }
    }

    private async Task OpenQuizAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("QuizPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ouverture du quiz");
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir le quiz.", "OK");
        }
    }

    private async Task ToggleQuizReminderAsync()
    {
        try
        {
            if (IsQuizReminderEnabled)
            {
                // Désactiver le rappel
                await _notificationService.CancelDailyQuizReminderAsync();
                IsQuizReminderEnabled = false;
                await Shell.Current.DisplayAlert("Rappel Quiz", 
                    "Les rappels quotidiens du quiz ont été désactivés.", "OK");
            }
            else
            {
                // Vérifier les permissions
                var notificationsEnabled = await _notificationService.AreNotificationsEnabledAsync();
                if (!notificationsEnabled)
                {
                    var openSettings = await Shell.Current.DisplayAlert(
                        "Notifications désactivées",
                        "Les notifications sont désactivées pour DonTroc. Voulez-vous ouvrir les paramètres pour les activer ?",
                        "Ouvrir les paramètres", "Non merci");
                    
                    if (openSettings)
                    {
                        await _notificationService.OpenNotificationSettingsAsync();
                    }
                    return;
                }

                // Activer le rappel
                await _notificationService.ScheduleDailyQuizReminderAsync(
                    QuizReminderTime.Hours, 
                    QuizReminderTime.Minutes);
                IsQuizReminderEnabled = true;
                
                await Shell.Current.DisplayAlert("Rappel Quiz activé ! 🔔", 
                    $"Vous recevrez un rappel tous les jours à {QuizReminderTimeFormatted} pour votre défi quiz quotidien.", "Super !");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la modification du rappel quiz");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de modifier le rappel quiz.", "OK");
        }
    }

    private async Task SetQuizReminderTimeAsync()
    {
        try
        {
            // Afficher un sélecteur d'heure simple
            var result = await Shell.Current.DisplayActionSheet(
                "Choisir l'heure du rappel",
                "Annuler",
                null,
                "08:00 - Matin tôt",
                "10:00 - Milieu de matinée",
                "12:00 - Midi",
                "14:00 - Après-midi",
                "18:00 - Fin de journée",
                "20:00 - Soirée");

            if (string.IsNullOrEmpty(result) || result == "Annuler")
                return;

            // Extraire l'heure de la sélection
            var hour = result switch
            {
                "08:00 - Matin tôt" => 8,
                "10:00 - Milieu de matinée" => 10,
                "12:00 - Midi" => 12,
                "14:00 - Après-midi" => 14,
                "18:00 - Fin de journée" => 18,
                "20:00 - Soirée" => 20,
                _ => 10
            };

            QuizReminderTime = new TimeSpan(hour, 0, 0);
            OnPropertyChanged(nameof(QuizReminderTimeFormatted));

            // Si le rappel est déjà activé, mettre à jour l'heure
            if (IsQuizReminderEnabled)
            {
                await _notificationService.ScheduleDailyQuizReminderAsync(hour, 0);
                await Shell.Current.DisplayAlert("Heure modifiée ✓", 
                    $"Le rappel a été reprogrammé pour {QuizReminderTimeFormatted}.", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la définition de l'heure du rappel");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de modifier l'heure du rappel.", "OK");
        }
    }

    #endregion
}

/// <summary>
/// Classe d'affichage pour les badges dans la liste
/// </summary>
public class BadgeDisplay
{
    public string Icon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Color RarityColor { get; set; } = Colors.Gray;
}

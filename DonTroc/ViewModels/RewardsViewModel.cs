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

    // Collections
    public ObservableCollection<Challenge> DailyChallenges { get; } = new();
    public ObservableCollection<BadgeDisplay> UnlockedBadges { get; } = new();
    public ObservableCollection<XpTransaction> RecentXpTransactions { get; } = new();

    public RewardsViewModel(
        IGamificationService gamificationService,
        AuthService authService,
        ILogger<RewardsViewModel> logger)
    {
        _gamificationService = gamificationService;
        _authService = authService;
        _logger = logger;

        // Commandes
        LoadDataCommand = new Command(async () => await LoadDataAsync());
        SpinWheelCommand = new Command(async () => await SpinWheelAsync());
        ClaimDailyRewardCommand = new Command(async () => await ClaimDailyRewardAsync());
        ViewAllBadgesCommand = new Command(async () => await ViewAllBadgesAsync());
        OpenQuizCommand = new Command(async () => await OpenQuizAsync());
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

    #endregion

    #region Commandes

    public ICommand LoadDataCommand { get; }
    public ICommand SpinWheelCommand { get; }
    public ICommand ClaimDailyRewardCommand { get; }
    public ICommand ViewAllBadgesCommand { get; }
    public ICommand OpenQuizCommand { get; }

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
        // TODO: Naviguer vers une page listant tous les badges
        await Shell.Current.DisplayAlert("Badges", 
            "Page des badges complète à venir dans une prochaine mise à jour !", "OK");
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

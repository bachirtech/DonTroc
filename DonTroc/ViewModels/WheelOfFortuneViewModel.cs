using CommunityToolkit.Mvvm.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DonTroc.ViewModels;

public class WheelOfFortuneViewModel : INotifyPropertyChanged
{
    private readonly IGamificationService _gamificationService;
    private readonly AuthService _authService;
    private readonly IAdMobService _adMobService;
    private readonly ILogger<WheelOfFortuneViewModel> _logger;

    // Indique si l'utilisateur a utilisé sa seconde chance aujourd'hui
    private bool _hasUsedSecondChance;

    public event PropertyChangedEventHandler? PropertyChanged;
    
    // Événement pour déclencher l'animation de rotation de la roue
    public event Func<int, Task>? OnSpinWheelAnimation;

    public WheelOfFortuneViewModel(
        IGamificationService gamificationService,
        AuthService authService,
        IAdMobService adMobService,
        ILogger<WheelOfFortuneViewModel> logger)
    {
        _gamificationService = gamificationService;
        _authService = authService;
        _adMobService = adMobService;
        _logger = logger;

        SpinCommand = new AsyncRelayCommand(SpinWheelAsync);
        CloseCommand = new AsyncRelayCommand(CloseAsync);
        WatchAdForSecondChanceCommand = new AsyncRelayCommand(WatchAdForSecondChanceAsync);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #region Commands

    public IAsyncRelayCommand SpinCommand { get; }
    public IAsyncRelayCommand CloseCommand { get; }
    public IAsyncRelayCommand WatchAdForSecondChanceCommand { get; }

    #endregion

    #region Properties

    private bool _canSpin = true;
    public bool CanSpin
    {
        get => _canSpin;
        set => SetProperty(ref _canSpin, value);
    }

    private bool _isSpinning;
    public bool IsSpinning
    {
        get => _isSpinning;
        set => SetProperty(ref _isSpinning, value);
    }

    private bool _showResult;
    public bool ShowResult
    {
        get => _showResult;
        set => SetProperty(ref _showResult, value);
    }

    private string _spinButtonText = "🎰 Tourner la Roue !";
    public string SpinButtonText
    {
        get => _spinButtonText;
        set => SetProperty(ref _spinButtonText, value);
    }

    private string _resultIcon = "🎁";
    public string ResultIcon
    {
        get => _resultIcon;
        set => SetProperty(ref _resultIcon, value);
    }

    private string _resultTitle = string.Empty;
    public string ResultTitle
    {
        get => _resultTitle;
        set => SetProperty(ref _resultTitle, value);
    }

    private string _resultMessage = string.Empty;
    public string ResultMessage
    {
        get => _resultMessage;
        set => SetProperty(ref _resultMessage, value);
    }

    private string _resultBackgroundColor = "#E8F5E9";
    public string ResultBackgroundColor
    {
        get => _resultBackgroundColor;
        set => SetProperty(ref _resultBackgroundColor, value);
    }

    private bool _canWatchAdForSecondChance;
    public bool CanWatchAdForSecondChance
    {
        get => _canWatchAdForSecondChance;
        set => SetProperty(ref _canWatchAdForSecondChance, value);
    }

    private string _secondChanceButtonText = "🎬 Regarder une pub pour rejouer";
    public string SecondChanceButtonText
    {
        get => _secondChanceButtonText;
        set => SetProperty(ref _secondChanceButtonText, value);
    }

    #endregion

    #region Methods

    public async Task InitializeAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                CanSpin = false;
                SpinButtonText = "Connexion requise";
                return;
            }

            // Charger les pubs récompensées
            _adMobService.LoadRewardedAd();

            CanSpin = await _gamificationService.CanSpinWheelAsync(userId);
            
            if (CanSpin)
            {
                SpinButtonText = "🎰 Tourner la Roue !";
                CanWatchAdForSecondChance = false;
            }
            else
            {
                SpinButtonText = "Déjà joué aujourd'hui";
                // Afficher l'option de seconde chance si pas encore utilisée
                CanWatchAdForSecondChance = !_hasUsedSecondChance;
            }
            
            ShowResult = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'initialisation de la roue");
        }
    }

    private async Task SpinWheelAsync()
    {
        if (!CanSpin || IsSpinning) return;

        try
        {
            IsSpinning = true;
            ShowResult = false;
            SpinButtonText = "🎰 La roue tourne...";

            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await ShowAlertAsync("Erreur", "Vous devez être connecté.");
                return;
            }

            // Animation de la roue via l'événement
            if (OnSpinWheelAnimation != null)
            {
                await OnSpinWheelAnimation.Invoke(5);
            }
            else
            {
                // Animation simple si l'événement n'est pas connecté
                await Task.Delay(2500);
            }

            // Obtenir le résultat
            var reward = await _gamificationService.SpinWheelAsync(userId);

            // Afficher le résultat
            ResultIcon = reward.Icon;
            ResultTitle = reward.Name;
            
            switch (reward.Type)
            {
                case WheelRewardType.Xp:
                    ResultMessage = $"Vous avez gagné {reward.Value} XP !";
                    ResultBackgroundColor = "#E8F5E9"; // Vert clair
                    break;
                case WheelRewardType.BoostCredits:
                    ResultMessage = $"Vous avez gagné {reward.Value} crédit(s) boost !";
                    ResultBackgroundColor = "#FFF3E0"; // Orange clair
                    break;
                case WheelRewardType.DoubleXpHours:
                    ResultMessage = $"Double XP activé pendant {reward.Value} heures !";
                    ResultBackgroundColor = "#F3E5F5"; // Violet clair
                    break;
                case WheelRewardType.Nothing:
                    ResultMessage = "Pas de chance cette fois... Réessayez demain !";
                    ResultBackgroundColor = "#FAFAFA"; // Gris clair
                    break;
                default:
                    ResultMessage = "Félicitations !";
                    ResultBackgroundColor = "#E8F5E9";
                    break;
            }

            ShowResult = true;
            CanSpin = false;
            SpinButtonText = "Déjà joué aujourd'hui";
            
            // Afficher l'option de seconde chance si pas encore utilisée
            CanWatchAdForSecondChance = !_hasUsedSecondChance;
            
            // Précharger une pub pour la seconde chance
            if (!_hasUsedSecondChance)
            {
                _adMobService.LoadRewardedAd();
            }

            _logger.LogInformation("Roue tournée, récompense: {RewardName}", reward.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du spin de la roue");
            await ShowAlertAsync("Erreur", "Impossible de tourner la roue.");
        }
        finally
        {
            IsSpinning = false;
        }
    }

    /// <summary>
    /// Regarde une publicité pour obtenir une seconde chance de tourner la roue
    /// </summary>
    private async Task WatchAdForSecondChanceAsync()
    {
        if (_hasUsedSecondChance || IsSpinning) return;

        try
        {
            SecondChanceButtonText = "🎬 Chargement de la pub...";
            
            // Vérifier si la pub est prête
            if (!_adMobService.IsRewardedAdReady())
            {
                _adMobService.LoadRewardedAd();
                await Task.Delay(2000); // Attendre un peu le chargement
            }

            // Afficher la pub récompensée
            var adWatched = await _adMobService.ShowRewardedAdAsync();

            if (adWatched)
            {
                _logger.LogInformation("Seconde chance obtenue via pub pour la roue de la fortune");
                
                // Marquer la seconde chance comme utilisée
                _hasUsedSecondChance = true;
                CanWatchAdForSecondChance = false;
                
                // Permettre un nouveau tour
                CanSpin = true;
                SpinButtonText = "🎰 Tour Bonus !";
                ShowResult = false;
                
                await ShowAlertAsync("🎉 Seconde Chance", "Vous avez gagné un tour bonus ! Tournez la roue !");
            }
            else
            {
                SecondChanceButtonText = "🎬 Regarder une pub pour rejouer";
                await ShowAlertAsync("Pub non visionnée", "Regardez la publicité en entier pour obtenir votre seconde chance.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la pub pour seconde chance");
            SecondChanceButtonText = "🎬 Regarder une pub pour rejouer";
            await ShowAlertAsync("Erreur", "Impossible de charger la publicité. Réessayez plus tard.");
        }
    }

    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task ShowAlertAsync(string title, string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
            {
                await page.DisplayAlert(title, message, "OK");
            }
        });
    }

    #endregion
}


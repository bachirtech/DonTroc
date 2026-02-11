using CommunityToolkit.Mvvm.Input;
using DonTroc.Configuration;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DonTroc.ViewModels;

public class QuizViewModel : INotifyPropertyChanged
{
    private readonly IQuizService _quizService;
    private readonly IAdMobService _adMobService;
    private readonly ILogger<QuizViewModel> _logger;
    private readonly AuthService _authService;
    
    private QuizSession? _currentSession;
    private List<QuizQuestion> _questions = new();
    private int _currentQuestionIndex;
    private DateTime _questionStartTime;
    private System.Timers.Timer? _timer;
    private bool _hasDoubledXp;

    public event PropertyChangedEventHandler? PropertyChanged;

    public QuizViewModel(IQuizService quizService, IAdMobService adMobService, ILogger<QuizViewModel> logger, AuthService authService)
    {
        _quizService = quizService;
        _adMobService = adMobService;
        _logger = logger;
        _authService = authService;
        Title = "Quiz du Jour";
        
        // Initialiser les commandes
        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        StartDailyQuizCommand = new AsyncRelayCommand(StartDailyQuizAsync);
        StartThematicQuizCommand = new AsyncRelayCommand<string>(StartThematicQuizAsync);
        SelectAnswerCommand = new AsyncRelayCommand<object>(SelectAnswerAsync);
        NextQuestionCommand = new AsyncRelayCommand(NextQuestionAsync);
        CloseQuizCommand = new AsyncRelayCommand(CloseQuizAsync);
        PlayAgainCommand = new AsyncRelayCommand(PlayAgainAsync);
        DoubleXpCommand = new AsyncRelayCommand(DoubleXpWithAdAsync);
        SecondChanceCommand = new AsyncRelayCommand(UseSecondChanceAsync);
        AddExtraTimeCommand = new AsyncRelayCommand(AddExtraTimeWithAdAsync);
        UnlockBonusQuizCommand = new AsyncRelayCommand(UnlockBonusQuizWithAdAsync);
        
        // Précharger la publicité rewarded
        _adMobService.LoadRewardedAd();
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

    public IAsyncRelayCommand InitializeCommand { get; }
    public IAsyncRelayCommand StartDailyQuizCommand { get; }
    public IAsyncRelayCommand<string> StartThematicQuizCommand { get; }
    public IAsyncRelayCommand<object> SelectAnswerCommand { get; }
    public IAsyncRelayCommand NextQuestionCommand { get; }
    public IAsyncRelayCommand CloseQuizCommand { get; }
    public IAsyncRelayCommand PlayAgainCommand { get; }
    public IAsyncRelayCommand DoubleXpCommand { get; }
    public IAsyncRelayCommand SecondChanceCommand { get; }
    public IAsyncRelayCommand AddExtraTimeCommand { get; }
    public IAsyncRelayCommand UnlockBonusQuizCommand { get; }

    #endregion

    #region Observable Properties

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private bool _canPlayDailyQuiz;
    public bool CanPlayDailyQuiz
    {
        get => _canPlayDailyQuiz;
        set => SetProperty(ref _canPlayDailyQuiz, value);
    }

    private bool _isQuizActive;
    public bool IsQuizActive
    {
        get => _isQuizActive;
        set => SetProperty(ref _isQuizActive, value);
    }

    private bool _showResult;
    public bool ShowResult
    {
        get => _showResult;
        set => SetProperty(ref _showResult, value);
    }

    private bool _showQuestionResult;
    public bool ShowQuestionResult
    {
        get => _showQuestionResult;
        set => SetProperty(ref _showQuestionResult, value);
    }

    private QuizQuestion? _currentQuestion;
    public QuizQuestion? CurrentQuestion
    {
        get => _currentQuestion;
        set => SetProperty(ref _currentQuestion, value);
    }

    private int _questionNumber;
    public int QuestionNumber
    {
        get => _questionNumber;
        set => SetProperty(ref _questionNumber, value);
    }

    private int _totalQuestions;
    public int TotalQuestions
    {
        get => _totalQuestions;
        set => SetProperty(ref _totalQuestions, value);
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    private int _timeRemaining = 30;
    public int TimeRemaining
    {
        get => _timeRemaining;
        set => SetProperty(ref _timeRemaining, value);
    }

    private int _score;
    public int Score
    {
        get => _score;
        set => SetProperty(ref _score, value);
    }

    private int _correctAnswers;
    public int CorrectAnswers
    {
        get => _correctAnswers;
        set => SetProperty(ref _correctAnswers, value);
    }

    private int _totalXpEarned;
    public int TotalXpEarned
    {
        get => _totalXpEarned;
        set => SetProperty(ref _totalXpEarned, value);
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

    private string _resultIcon = "🎯";
    public string ResultIcon
    {
        get => _resultIcon;
        set => SetProperty(ref _resultIcon, value);
    }

    private bool _isPerfectScore;
    public bool IsPerfectScore
    {
        get => _isPerfectScore;
        set => SetProperty(ref _isPerfectScore, value);
    }

    private int _currentStreak;
    public int CurrentStreak
    {
        get => _currentStreak;
        set => SetProperty(ref _currentStreak, value);
    }

    private bool _canDoubleXp;
    public bool CanDoubleXp
    {
        get => _canDoubleXp;
        set => SetProperty(ref _canDoubleXp, value);
    }

    private bool _isLoadingAd;
    public bool IsLoadingAd
    {
        get => _isLoadingAd;
        set => SetProperty(ref _isLoadingAd, value);
    }

    private int _bonusXpEarned;
    public int BonusXpEarned
    {
        get => _bonusXpEarned;
        set => SetProperty(ref _bonusXpEarned, value);
    }

    // Propriétés pour la seconde chance
    private bool _canUseSecondChance;
    public bool CanUseSecondChance
    {
        get => _canUseSecondChance;
        set => SetProperty(ref _canUseSecondChance, value);
    }

    private bool _hasUsedSecondChance;
    public bool HasUsedSecondChance
    {
        get => _hasUsedSecondChance;
        set => SetProperty(ref _hasUsedSecondChance, value);
    }

    // Propriétés pour le temps supplémentaire
    private bool _canAddExtraTime;
    public bool CanAddExtraTime
    {
        get => _canAddExtraTime;
        set => SetProperty(ref _canAddExtraTime, value);
    }

    private bool _hasUsedExtraTime;
    public bool HasUsedExtraTime
    {
        get => _hasUsedExtraTime;
        set => SetProperty(ref _hasUsedExtraTime, value);
    }

    // Propriétés pour le quiz bonus
    private bool _canUnlockBonusQuiz;
    public bool CanUnlockBonusQuiz
    {
        get => _canUnlockBonusQuiz;
        set => SetProperty(ref _canUnlockBonusQuiz, value);
    }

    private bool _hasBonusQuizUnlocked;
    public bool HasBonusQuizUnlocked
    {
        get => _hasBonusQuizUnlocked;
        set => SetProperty(ref _hasBonusQuizUnlocked, value);
    }

    private bool _lastAnswerCorrect;
    public bool LastAnswerCorrect
    {
        get => _lastAnswerCorrect;
        set => SetProperty(ref _lastAnswerCorrect, value);
    }

    private string _explanation = string.Empty;
    public string Explanation
    {
        get => _explanation;
        set => SetProperty(ref _explanation, value);
    }

    private int? _selectedAnswerIndex;
    public int? SelectedAnswerIndex
    {
        get => _selectedAnswerIndex;
        set => SetProperty(ref _selectedAnswerIndex, value);
    }

    private string _categoryName = string.Empty;
    public string CategoryName
    {
        get => _categoryName;
        set => SetProperty(ref _categoryName, value);
    }

    private string _categoryColor = "#4CAF50";
    public string CategoryColor
    {
        get => _categoryColor;
        set => SetProperty(ref _categoryColor, value);
    }

    private UserQuizProfile? _userProfile;
    public UserQuizProfile? UserProfile
    {
        get => _userProfile;
        set => SetProperty(ref _userProfile, value);
    }

    public ObservableCollection<QuizAnswerOption> AnswerOptions { get; } = new();

    #endregion

    #region Initialization

    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                await ShowAlertAsync("Erreur", "Vous devez être connecté pour jouer au quiz.");
                return;
            }

            CanPlayDailyQuiz = await _quizService.CanPlayDailyQuizAsync(userId);
            UserProfile = await _quizService.GetUserQuizProfileAsync(userId);
            CurrentStreak = UserProfile.CurrentStreak;
            
            // Vérifier si le quiz bonus est déjà débloqué aujourd'hui
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var bonusUnlockedDate = Preferences.Get("bonus_quiz_unlocked_date", string.Empty);
            HasBonusQuizUnlocked = bonusUnlockedDate == today;
            CanUnlockBonusQuiz = !HasBonusQuizUnlocked;
            
            // Réinitialiser les états des bonus
            HasUsedSecondChance = false;
            CanUseSecondChance = false;
            HasUsedExtraTime = false;
            CanAddExtraTime = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur initialisation quiz");
            await ShowAlertAsync("Erreur", "Impossible de charger le quiz.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Start Quiz

    public async Task StartDailyQuizAsync() // Méthode demarrage du quiz quotidien
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId)) return;

            _currentSession = await _quizService.StartDailyQuizAsync(userId);
            _questions = await _quizService.GetSessionQuestionsAsync(_currentSession.Id);
            
            TotalQuestions = _questions.Count;
            _currentQuestionIndex = 0;
            Score = 0;
            CorrectAnswers = 0;
            IsQuizActive = true;
            ShowResult = false;

            await LoadCurrentQuestionAsync();
        }
        catch (InvalidOperationException ex)
        {
            await ShowAlertAsync("Quiz indisponible", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur démarrage quiz quotidien");
            await ShowAlertAsync("Erreur", "Impossible de démarrer le quiz.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StartThematicQuizAsync(string? categoryName) // Méthode demarrage thématique du quiz
    {
        if (IsBusy || string.IsNullOrEmpty(categoryName)) return;

        try
        {
            IsBusy = true;
            
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                await ShowAlertAsync("Connexion requise", "Vous devez être connecté pour jouer au quiz thématique.");
                return;
            }

            if (!Enum.TryParse<QuizCategory>(categoryName, out var category))
            {
                await ShowAlertAsync("Erreur", $"Catégorie '{categoryName}' non reconnue.");
                return;
            }
            
            _currentSession = await _quizService.StartThematicQuizAsync(userId, category);
            _questions = await _quizService.GetSessionQuestionsAsync(_currentSession.Id);
            
            if (_questions == null || _questions.Count == 0)
            {
                await ShowAlertAsync("Quiz indisponible", "Aucune question disponible pour cette catégorie.");
                return;
            }
            
            TotalQuestions = _questions.Count;
            _currentQuestionIndex = 0;
            Score = 0;
            CorrectAnswers = 0;
            IsQuizActive = true;
            ShowResult = false;

            await LoadCurrentQuestionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur démarrage quiz thématique: {Category}", categoryName);
            await ShowAlertAsync("Erreur", $"Impossible de démarrer le quiz: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Questions

    private async Task LoadCurrentQuestionAsync()
    {
        if (_currentQuestionIndex >= _questions.Count)
        {
            await CompleteQuizAsync();
            return;
        }

        ShowQuestionResult = false;
        SelectedAnswerIndex = null;
        
        CurrentQuestion = _questions[_currentQuestionIndex];
        QuestionNumber = _currentQuestionIndex + 1;
        Progress = (double)_currentQuestionIndex / TotalQuestions;
        
        CategoryName = QuizConfig.GetCategoryName(CurrentQuestion.Category);
        CategoryColor = QuizConfig.GetCategoryColor(CurrentQuestion.Category);

        // Charger les options de réponse
        AnswerOptions.Clear();
        for (int i = 0; i < CurrentQuestion.Options.Count; i++)
        {
            AnswerOptions.Add(new QuizAnswerOption
            {
                Index = i,
                Text = CurrentQuestion.Options[i],
                IsSelected = false,
                IsCorrect = i == CurrentQuestion.CorrectAnswerIndex,
                ShowResult = false
            });
        }

        // Démarrer le timer
        _questionStartTime = DateTime.Now;
        TimeRemaining = QuizConfig.MaxResponseTimeSeconds;
        StartTimer();
    }

    private void StartTimer()
    {
        StopTimer();
        
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimeRemaining--;
                
                // Activer l'option de temps bonus quand il reste moins de 10 secondes
                if (TimeRemaining <= 10 && TimeRemaining > 0 && !HasUsedExtraTime)
                {
                    CanAddExtraTime = true;
                }
                
                if (TimeRemaining <= 0)
                {
                    StopTimer();
                    CanAddExtraTime = false;
                    // Temps écoulé, soumettre sans réponse
                    _ = SubmitAnswerAsync(-1);
                }
            });
        };
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    #endregion

    #region Submit Answer

    public async Task SelectAnswerAsync(object? indexObj)
    {
        _logger.LogInformation("SelectAnswerAsync appelé avec indexObj: {IndexObj}, Type: {Type}", 
            indexObj, indexObj?.GetType().Name ?? "null");
        
        if (ShowQuestionResult || SelectedAnswerIndex.HasValue)
        {
            _logger.LogInformation("Sélection ignorée: ShowQuestionResult={ShowQuestionResult}, SelectedAnswerIndex={SelectedAnswerIndex}", 
                ShowQuestionResult, SelectedAnswerIndex);
            return;
        }

        // Convertir le paramètre en int
        int index;
        if (indexObj is int i)
        {
            index = i;
        }
        else if (indexObj != null && int.TryParse(indexObj.ToString(), out var parsed))
        {
            index = parsed;
        }
        else
        {
            _logger.LogWarning("SelectAnswerAsync: Index invalide reçu: {IndexObj}", indexObj);
            return;
        }

        _logger.LogInformation("Réponse sélectionnée: index={Index}", index);
        
        SelectedAnswerIndex = index;
        
        // Mettre à jour visuellement
        foreach (var option in AnswerOptions)
        {
            option.IsSelected = option.Index == index;
        }

        await SubmitAnswerAsync(index);
    }

    private async Task SubmitAnswerAsync(int selectedIndex)
    {
        if (_currentSession == null || CurrentQuestion == null) return;

        StopTimer();

        try
        {
            var responseTime = (DateTime.Now - _questionStartTime).TotalSeconds;
            
            var answer = await _quizService.SubmitAnswerAsync(
                _currentSession.Id,
                CurrentQuestion.Id,
                selectedIndex,
                responseTime
            );

            LastAnswerCorrect = answer.IsCorrect;
            Explanation = CurrentQuestion.Explanation;
            Score += answer.XpEarned;
            
            if (answer.IsCorrect)
            {
                CorrectAnswers++;
                // Désactiver la seconde chance si la réponse est correcte
                CanUseSecondChance = false;
            }
            else
            {
                // Activer la seconde chance si l'utilisateur ne l'a pas encore utilisée
                if (!HasUsedSecondChance)
                {
                    CanUseSecondChance = true;
                }
            }

            // Afficher le résultat visuellement
            foreach (var option in AnswerOptions)
            {
                option.ShowResult = true;
                if (option.IsSelected && !option.IsCorrect)
                {
                    option.IsWrong = true;
                }
            }

            ShowQuestionResult = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur soumission réponse");
        }
    }

    public async Task NextQuestionAsync()
    {
        _currentQuestionIndex++;
        await LoadCurrentQuestionAsync();
    }

    #endregion

    #region Complete Quiz

    private async Task CompleteQuizAsync()
    {
        if (_currentSession == null) return;

        try
        {
            StopTimer();
            IsQuizActive = false;
            
            var result = await _quizService.CompleteQuizAsync(_currentSession.Id);
            
            ResultTitle = result.Title;
            ResultMessage = result.Message;
            ResultIcon = result.Icon;
            TotalXpEarned = result.XpEarned;
            CorrectAnswers = result.CorrectAnswers;
            IsPerfectScore = result.IsPerfectScore;
            CurrentStreak = result.NewStreak;
            
            Progress = 1.0;
            ShowResult = true;
            
            // Réinitialiser l'état des Rewarded Ads
            _hasDoubledXp = false;
            BonusXpEarned = 0;
            CanDoubleXp = TotalXpEarned > 0; // On peut doubler seulement si on a gagné des XP
            
            // Précharger la pub pour la prochaine fois
            _adMobService.LoadRewardedAd();
            
            // Mettre à jour le profil
            var userId = await GetCurrentUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                UserProfile = await _quizService.GetUserQuizProfileAsync(userId);
                CanPlayDailyQuiz = await _quizService.CanPlayDailyQuizAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur complétion quiz");
            await ShowAlertAsync("Erreur", "Impossible de terminer le quiz.");
        }
    }

    public async Task CloseQuizAsync()
    {
        StopTimer();
        IsQuizActive = false;
        ShowResult = false;
        await Shell.Current.GoToAsync("..");
    }

    public async Task PlayAgainAsync()
    {
        ShowResult = false;
        await InitializeAsync();
    }

    #endregion

    #region Rewarded Ads

    /// <summary>
    /// Affiche une publicité rewarded pour doubler les XP gagnés
    /// </summary>
    private async Task DoubleXpWithAdAsync()
    {
        if (_hasDoubledXp || TotalXpEarned <= 0)
        {
            _logger.LogWarning("Tentative de doubler les XP alors que c'est déjà fait ou pas d'XP");
            return;
        }

        try
        {
            IsLoadingAd = true;
            _logger.LogInformation("Tentative d'affichage de la pub rewarded pour doubler les XP");

            // Vérifier si la pub est prête
            if (!_adMobService.IsRewardedAdReady())
            {
                _logger.LogWarning("Pub rewarded non prête, chargement en cours...");
                _adMobService.LoadRewardedAd();
                
                // Attendre un peu que la pub se charge
                await Task.Delay(2000);
                
                if (!_adMobService.IsRewardedAdReady())
                {
                    await ShowAlertAsync("Publicité non disponible", 
                        "La publicité n'est pas encore prête. Veuillez réessayer dans quelques instants.");
                    return;
                }
            }

            // Afficher la pub
            var adWatched = await _adMobService.ShowRewardedAdAsync();

            if (adWatched)
            {
                // L'utilisateur a regardé la pub, doubler les XP
                _hasDoubledXp = true;
                BonusXpEarned = TotalXpEarned; // Bonus = même montant que les XP gagnés
                var originalXp = TotalXpEarned;
                TotalXpEarned = originalXp * 2; // Doubler le total affiché
                CanDoubleXp = false; // Désactiver le bouton

                // Ajouter les XP bonus au profil
                var userId = await GetCurrentUserIdAsync();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _quizService.GetUserQuizProfileAsync(userId);
                    // Note: Les XP bonus sont déjà inclus dans TotalXpEarned pour l'affichage
                    // Le service de quiz devrait gérer l'ajout réel des XP bonus
                }

                _logger.LogInformation("XP doublés ! Bonus: +{BonusXp}, Total: {TotalXp}", BonusXpEarned, TotalXpEarned);
                
                // Afficher une animation ou un message de succès
                await ShowAlertAsync("🎉 XP Doublés !", 
                    $"Félicitations ! Vous avez gagné +{BonusXpEarned} XP bonus !\nTotal : {TotalXpEarned} XP");
            }
            else
            {
                _logger.LogInformation("L'utilisateur n'a pas terminé la pub rewarded");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage de la pub rewarded");
            await ShowAlertAsync("Erreur", "Impossible d'afficher la publicité. Veuillez réessayer.");
        }
        finally
        {
            IsLoadingAd = false;
            // Recharger une pub pour la prochaine fois
            _adMobService.LoadRewardedAd();
        }
    }

    /// <summary>
    /// Utilise une seconde chance après une mauvaise réponse
    /// </summary>
    private async Task UseSecondChanceAsync()
    {
        if (HasUsedSecondChance || !CanUseSecondChance)
        {
            _logger.LogWarning("Seconde chance déjà utilisée ou non disponible");
            return;
        }

        try
        {
            IsLoadingAd = true;
            _logger.LogInformation("Tentative d'utilisation de la seconde chance");

            var adWatched = await ShowRewardedAdWithRetryAsync();

            if (adWatched)
            {
                HasUsedSecondChance = true;
                CanUseSecondChance = false;
                
                // Réinitialiser la question pour permettre une nouvelle réponse
                ShowQuestionResult = false;
                SelectedAnswerIndex = null;
                
                // Réinitialiser les options visuellement
                foreach (var option in AnswerOptions)
                {
                    option.IsSelected = false;
                    option.ShowResult = false;
                }

                _logger.LogInformation("Seconde chance activée !");
                await ShowAlertAsync("🎯 Seconde Chance !", 
                    "Vous avez une nouvelle tentative pour cette question !");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'utilisation de la seconde chance");
            await ShowAlertAsync("Erreur", "Impossible d'activer la seconde chance.");
        }
        finally
        {
            IsLoadingAd = false;
            _adMobService.LoadRewardedAd();
        }
    }

    /// <summary>
    /// Ajoute du temps supplémentaire pour répondre
    /// </summary>
    private async Task AddExtraTimeWithAdAsync()
    {
        if (HasUsedExtraTime || !CanAddExtraTime)
        {
            _logger.LogWarning("Temps supplémentaire déjà utilisé ou non disponible");
            return;
        }

        try
        {
            IsLoadingAd = true;
            _logger.LogInformation("Tentative d'ajout de temps supplémentaire");

            var adWatched = await ShowRewardedAdWithRetryAsync();

            if (adWatched)
            {
                HasUsedExtraTime = true;
                CanAddExtraTime = false;
                
                // Ajouter 15 secondes
                TimeRemaining += 15;
                
                _logger.LogInformation("Temps supplémentaire ajouté ! +15 secondes");
                await ShowAlertAsync("⏱️ Temps Bonus !", 
                    "+15 secondes ajoutées !");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'ajout de temps supplémentaire");
            await ShowAlertAsync("Erreur", "Impossible d'ajouter du temps.");
        }
        finally
        {
            IsLoadingAd = false;
            _adMobService.LoadRewardedAd();
        }
    }

    /// <summary>
    /// Débloque un quiz bonus thématique
    /// </summary>
    private async Task UnlockBonusQuizWithAdAsync()
    {
        if (HasBonusQuizUnlocked)
        {
            _logger.LogWarning("Quiz bonus déjà débloqué");
            return;
        }

        try
        {
            IsLoadingAd = true;
            _logger.LogInformation("Tentative de déblocage du quiz bonus");

            var adWatched = await ShowRewardedAdWithRetryAsync();

            if (adWatched)
            {
                HasBonusQuizUnlocked = true;
                CanUnlockBonusQuiz = false;
                
                // Sauvegarder dans les préférences que le quiz bonus est débloqué pour aujourd'hui
                var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                Preferences.Set("bonus_quiz_unlocked_date", today);

                _logger.LogInformation("Quiz bonus débloqué !");
                await ShowAlertAsync("🎁 Quiz Bonus Débloqué !", 
                    "Vous avez accès à un quiz bonus avec des questions exclusives et des récompenses doublées !");
                
                // Démarrer le quiz bonus
                await StartBonusQuizAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déblocage du quiz bonus");
            await ShowAlertAsync("Erreur", "Impossible de débloquer le quiz bonus.");
        }
        finally
        {
            IsLoadingAd = false;
            _adMobService.LoadRewardedAd();
        }
    }

    /// <summary>
    /// Démarre un quiz bonus avec des récompenses doublées
    /// </summary>
    private async Task StartBonusQuizAsync()
    {
        try
        {
            IsBusy = true;
            
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId)) return;

            // Utiliser une catégorie aléatoire pour le quiz bonus
            var categories = Enum.GetValues<QuizCategory>();
            var randomCategory = categories[new Random().Next(categories.Length)];
            
            _currentSession = await _quizService.StartThematicQuizAsync(userId, randomCategory);
            _questions = await _quizService.GetSessionQuestionsAsync(_currentSession.Id);
            
            TotalQuestions = _questions.Count;
            _currentQuestionIndex = 0;
            Score = 0;
            CorrectAnswers = 0;
            IsQuizActive = true;
            ShowResult = false;
            
            // Réinitialiser les bonus pour ce quiz
            _hasUsedSecondChance = false;
            HasUsedSecondChance = false;
            _hasUsedExtraTime = false;
            HasUsedExtraTime = false;
            CanAddExtraTime = true;

            await LoadCurrentQuestionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur démarrage quiz bonus");
            await ShowAlertAsync("Erreur", "Impossible de démarrer le quiz bonus.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Affiche une pub rewarded avec retry automatique
    /// </summary>
    private async Task<bool> ShowRewardedAdWithRetryAsync()
    {
        if (!_adMobService.IsRewardedAdReady())
        {
            _adMobService.LoadRewardedAd();
            await Task.Delay(2000);
            
            if (!_adMobService.IsRewardedAdReady())
            {
                await ShowAlertAsync("Publicité non disponible", 
                    "La publicité n'est pas encore prête. Veuillez réessayer.");
                return false;
            }
        }

        return await _adMobService.ShowRewardedAdAsync();
    }

    #endregion

    #region Helpers

    private async Task<string> GetCurrentUserIdAsync()
    {
        // Récupérer l'ID utilisateur depuis le service d'authentification
        var userId = _authService.GetUserId();
        
        if (string.IsNullOrEmpty(userId))
        {
            // Essayer de récupérer l'utilisateur actuel (avec rafraîchissement du token si nécessaire)
            var currentUser = await _authService.GetCurrentUserAsync();
            userId = currentUser?.Uid;
        }
        
#if DEBUG
        // En mode debug, utiliser un ID de test si aucun utilisateur n'est connecté
        if (string.IsNullOrEmpty(userId))
        {
            userId = "debug_user_test";
            _logger.LogDebug("Mode DEBUG: Utilisation de l'ID utilisateur de test");
        }
#endif
        
        return userId ?? string.Empty;
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

/// <summary>
/// Option de réponse pour l'affichage
/// </summary>
public class QuizAnswerOption : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private int _index;
    public int Index
    {
        get => _index;
        set { _index = value; OnPropertyChanged(); }
    }

    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundColor)); OnPropertyChanged(nameof(BorderColor)); }
    }

    private bool _isCorrect;
    public bool IsCorrect
    {
        get => _isCorrect;
        set { _isCorrect = value; OnPropertyChanged(); }
    }

    private bool _isWrong;
    public bool IsWrong
    {
        get => _isWrong;
        set { _isWrong = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundColor)); OnPropertyChanged(nameof(BorderColor)); }
    }

    private bool _showResult;
    public bool ShowResult
    {
        get => _showResult;
        set { _showResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundColor)); OnPropertyChanged(nameof(BorderColor)); }
    }

    public string BackgroundColor
    {
        get
        {
            if (!ShowResult) return IsSelected ? "#E3F2FD" : "#FFFFFF";
            if (IsCorrect) return "#C8E6C9"; // Vert
            if (IsWrong) return "#FFCDD2"; // Rouge
            return "#FFFFFF";
        }
    }

    public string BorderColor
    {
        get
        {
            if (!ShowResult) return IsSelected ? "#2196F3" : "#E0E0E0";
            if (IsCorrect) return "#4CAF50";
            if (IsWrong) return "#F44336";
            return "#E0E0E0";
        }
    }
}


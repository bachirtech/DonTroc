using DonTroc.Configuration;
using DonTroc.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DonTroc.Services;

/// <summary>
/// Interface pour le service de Quiz
/// </summary>
public interface IQuizService
{
    /// <summary>
    /// Vérifie si l'utilisateur peut jouer au quiz quotidien
    /// </summary>
    Task<bool> CanPlayDailyQuizAsync(string userId);
    
    /// <summary>
    /// Démarre une nouvelle session de quiz quotidien
    /// </summary>
    Task<QuizSession> StartDailyQuizAsync(string userId);
    
    /// <summary>
    /// Démarre un quiz thématique
    /// </summary>
    Task<QuizSession> StartThematicQuizAsync(string userId, QuizCategory category);
    
    /// <summary>
    /// Soumet une réponse à une question
    /// </summary>
    Task<QuizAnswer> SubmitAnswerAsync(string sessionId, string questionId, int selectedAnswerIndex, double responseTime);
    
    /// <summary>
    /// Termine une session de quiz et calcule les récompenses
    /// </summary>
    Task<QuizResultNotification> CompleteQuizAsync(string sessionId);
    
    /// <summary>
    /// Obtient le profil quiz de l'utilisateur
    /// </summary>
    Task<UserQuizProfile> GetUserQuizProfileAsync(string userId);
    
    /// <summary>
    /// Obtient les questions d'une session
    /// </summary>
    Task<List<QuizQuestion>> GetSessionQuestionsAsync(string sessionId);
    
    /// <summary>
    /// Obtient une session de quiz
    /// </summary>
    Task<QuizSession?> GetSessionAsync(string sessionId);
    
    /// <summary>
    /// Obtient le streak actuel du quiz
    /// </summary>
    Task<int> GetQuizStreakAsync(string userId);
}

/// <summary>
/// Service gérant la logique du système de quiz
/// </summary>
public class QuizService : IQuizService
{
    private readonly ILogger<QuizService> _logger;
    private readonly IGamificationService _gamificationService;
    
    // Préfixes pour le stockage
    private const string ProfileKeyPrefix = "quiz_profile_";
    private const string SessionKeyPrefix = "quiz_session_";
    private const string QuestionsKeyPrefix = "quiz_questions_";
    
    // Cache local
    private readonly Dictionary<string, UserQuizProfile> _profileCache = new();
    private readonly Dictionary<string, QuizSession> _sessionCache = new();
    private readonly Dictionary<string, List<QuizQuestion>> _questionsCache = new();

    public QuizService(ILogger<QuizService> logger, IGamificationService gamificationService)
    {
        _logger = logger;
        _gamificationService = gamificationService;
    }

    #region Stockage

    private T? GetFromStorage<T>(string key) where T : class
    {
        try
        {
            var json = Preferences.Get(key, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lecture stockage quiz {Key}", key);
            return null;
        }
    }

    private void SaveToStorage<T>(string key, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            Preferences.Set(key, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur écriture stockage quiz {Key}", key);
        }
    }

    #endregion

    #region Profil

    public async Task<UserQuizProfile> GetUserQuizProfileAsync(string userId)
    {
        try
        {
            if (_profileCache.TryGetValue(userId, out var cached))
                return cached;

            var profile = GetFromStorage<UserQuizProfile>($"{ProfileKeyPrefix}{userId}");
            
            if (profile == null)
            {
                profile = new UserQuizProfile
                {
                    UserId = userId,
                    LastQuizDate = DateTime.MinValue
                };
                SaveToStorage($"{ProfileKeyPrefix}{userId}", profile);
            }

            _profileCache[userId] = profile;
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération profil quiz {UserId}", userId);
            return new UserQuizProfile { UserId = userId };
        }
    }

    private void SaveProfile(UserQuizProfile profile)
    {
        SaveToStorage($"{ProfileKeyPrefix}{profile.UserId}", profile);
        _profileCache[profile.UserId] = profile;
    }

    #endregion

    #region Quiz Quotidien

    public async Task<bool> CanPlayDailyQuizAsync(string userId)
    {
        var profile = await GetUserQuizProfileAsync(userId);
        return profile.CanPlayDailyQuiz;
    }

    public async Task<QuizSession> StartDailyQuizAsync(string userId)
    {
        try
        {
            var canPlay = await CanPlayDailyQuizAsync(userId);
            if (!canPlay)
            {
                _logger.LogWarning("Utilisateur {UserId} a déjà joué au quiz aujourd'hui", userId);
                throw new InvalidOperationException("Vous avez déjà joué au quiz quotidien aujourd'hui !");
            }

            var questions = QuizConfig.GetDailyQuizQuestions();
            
            var session = new QuizSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = QuizSessionType.Daily,
                TotalQuestions = questions.Count,
                StartedAt = DateTime.UtcNow
            };

            // Sauvegarder la session et les questions
            SaveToStorage($"{SessionKeyPrefix}{session.Id}", session);
            SaveToStorage($"{QuestionsKeyPrefix}{session.Id}", questions);
            
            _sessionCache[session.Id] = session;
            _questionsCache[session.Id] = questions;

            _logger.LogInformation("Quiz quotidien démarré pour {UserId}, session {SessionId}", userId, session.Id);
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur démarrage quiz quotidien {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Quiz Thématique

    public async Task<QuizSession> StartThematicQuizAsync(string userId, QuizCategory category)
    {
        try
        {
            var questions = QuizConfig.GetQuestionsByCategory(category, 5);
            
            var session = new QuizSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = QuizSessionType.Thematic,
                TotalQuestions = questions.Count,
                StartedAt = DateTime.UtcNow
            };

            SaveToStorage($"{SessionKeyPrefix}{session.Id}", session);
            SaveToStorage($"{QuestionsKeyPrefix}{session.Id}", questions);
            
            _sessionCache[session.Id] = session;
            _questionsCache[session.Id] = questions;

            _logger.LogInformation("Quiz thématique {Category} démarré pour {UserId}", category, userId);
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur démarrage quiz thématique {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Réponses

    public async Task<QuizAnswer> SubmitAnswerAsync(string sessionId, string questionId, int selectedAnswerIndex, double responseTime)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null)
                throw new InvalidOperationException("Session de quiz introuvable");

            var questions = await GetSessionQuestionsAsync(sessionId);
            var question = questions.FirstOrDefault(q => q.Id == questionId);
            
            if (question == null)
                throw new InvalidOperationException("Question introuvable");

            var isCorrect = selectedAnswerIndex == question.CorrectAnswerIndex;
            var xpEarned = 0;

            if (isCorrect)
            {
                xpEarned = question.XpReward;
                
                // Bonus réponse rapide
                if (responseTime < 10)
                {
                    xpEarned += QuizConfig.QuickAnswerBonus;
                }
            }

            var answer = new QuizAnswer
            {
                QuestionId = questionId,
                SelectedAnswerIndex = selectedAnswerIndex,
                IsCorrect = isCorrect,
                XpEarned = xpEarned,
                ResponseTimeSeconds = responseTime,
                AnsweredAt = DateTime.UtcNow
            };

            session.Answers.Add(answer);
            session.TotalXpEarned += xpEarned;

            // Mettre à jour la session
            SaveToStorage($"{SessionKeyPrefix}{sessionId}", session);
            _sessionCache[sessionId] = session;

            _logger.LogDebug("Réponse soumise pour question {QuestionId}: {IsCorrect}", questionId, isCorrect);

            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur soumission réponse {SessionId}", sessionId);
            throw;
        }
    }

    #endregion

    #region Complétion

    public async Task<QuizResultNotification> CompleteQuizAsync(string sessionId)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null)
                throw new InvalidOperationException("Session introuvable");

            if (session.IsCompleted)
                throw new InvalidOperationException("Quiz déjà terminé");

            session.CompletedAt = DateTime.UtcNow;

            // Mettre à jour le profil utilisateur
            var profile = await GetUserQuizProfileAsync(session.UserId);
            profile.TotalQuizzesCompleted++;
            profile.TotalQuestionsAnswered += session.TotalQuestions;
            profile.TotalCorrectAnswers += session.CorrectAnswers;
            profile.TotalXpFromQuiz += session.TotalXpEarned;

            // Gérer le streak pour le quiz quotidien
            var isStreakBonus = false;
            if (session.Type == QuizSessionType.Daily)
            {
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                
                if (profile.LastQuizDate.Date == yesterday)
                {
                    // Continuation du streak
                    profile.CurrentStreak++;
                    isStreakBonus = true;
                }
                else if (profile.LastQuizDate.Date < yesterday)
                {
                    // Streak cassé
                    profile.CurrentStreak = 1;
                }
                
                if (profile.CurrentStreak > profile.BestStreak)
                {
                    profile.BestStreak = profile.CurrentStreak;
                }
                
                profile.LastQuizDate = DateTime.UtcNow;
            }

            // Bonus streak
            var streakBonus = isStreakBonus ? profile.CurrentStreak * QuizConfig.StreakBonusPerDay : 0;
            
            // Bonus score parfait
            var perfectBonus = session.CorrectAnswers == session.TotalQuestions ? QuizConfig.PerfectScoreBonus : 0;
            
            var totalXp = session.TotalXpEarned + streakBonus + perfectBonus;

            // Ajouter les XP via le service de gamification
            if (totalXp > 0)
            {
                await _gamificationService.AddXpAsync(session.UserId, "quiz_completed", totalXp);
            }

            // Mettre à jour les stats par catégorie
            var questions = await GetSessionQuestionsAsync(sessionId);
            foreach (var answer in session.Answers.Where(a => a.IsCorrect))
            {
                var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question != null)
                {
                    var categoryKey = question.Category.ToString();
                    profile.CategoryStats.TryAdd(categoryKey, 0);
                    profile.CategoryStats[categoryKey]++;
                }
            }

            SaveProfile(profile);
            SaveToStorage($"{SessionKeyPrefix}{sessionId}", session);
            _sessionCache[sessionId] = session;

            // Incrémenter la stat de gamification
            await _gamificationService.IncrementStatAsync(session.UserId, "quizzes_completed");
            
            if (session.CorrectAnswers == session.TotalQuestions)
            {
                await _gamificationService.IncrementStatAsync(session.UserId, "perfect_quizzes");
            }

            _logger.LogInformation("Quiz terminé {SessionId}: {Correct}/{Total}, XP: {Xp}", 
                sessionId, session.CorrectAnswers, session.TotalQuestions, totalXp);

            // Créer la notification de résultat
            var notification = new QuizResultNotification
            {
                Title = session.CorrectAnswers == session.TotalQuestions 
                    ? "Score Parfait ! 🎉" 
                    : $"{session.CorrectAnswers}/{session.TotalQuestions} Bonnes réponses !",
                Message = BuildResultMessage(session, streakBonus, perfectBonus),
                Icon = session.CorrectAnswers == session.TotalQuestions ? "🏆" : "🎯",
                XpEarned = totalXp,
                CorrectAnswers = session.CorrectAnswers,
                TotalQuestions = session.TotalQuestions,
                IsStreakBonus = isStreakBonus,
                NewStreak = profile.CurrentStreak
            };

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur complétion quiz {SessionId}", sessionId);
            throw;
        }
    }

    private string BuildResultMessage(QuizSession session, int streakBonus, int perfectBonus)
    {
        var messages = new List<string>
        {
            $"+{session.TotalXpEarned} XP pour vos réponses"
        };

        if (streakBonus > 0)
        {
            messages.Add($"+{streakBonus} XP bonus streak");
        }

        if (perfectBonus > 0)
        {
            messages.Add($"+{perfectBonus} XP score parfait !");
        }

        return string.Join("\n", messages);
    }

    #endregion

    #region Getters

    public async Task<QuizSession?> GetSessionAsync(string sessionId)
    {
        if (_sessionCache.TryGetValue(sessionId, out var cached))
            return cached;

        var session = GetFromStorage<QuizSession>($"{SessionKeyPrefix}{sessionId}");
        if (session != null)
        {
            _sessionCache[sessionId] = session;
        }
        return session;
    }

    public async Task<List<QuizQuestion>> GetSessionQuestionsAsync(string sessionId)
    {
        if (_questionsCache.TryGetValue(sessionId, out var cached))
            return cached;

        var questions = GetFromStorage<List<QuizQuestion>>($"{QuestionsKeyPrefix}{sessionId}");
        if (questions != null)
        {
            _questionsCache[sessionId] = questions;
        }
        return questions ?? new List<QuizQuestion>();
    }

    public async Task<int> GetQuizStreakAsync(string userId)
    {
        var profile = await GetUserQuizProfileAsync(userId);
        return profile.CurrentStreak;
    }

    #endregion
}


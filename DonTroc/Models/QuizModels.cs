namespace DonTroc.Models;

/// <summary>
/// Question de quiz
/// </summary>
public class QuizQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public QuizCategory Category { get; set; }
    public QuizDifficulty Difficulty { get; set; }
    public string Icon { get; set; } = "❓";
    
    /// <summary>
    /// XP gagnés pour une bonne réponse
    /// </summary>
    public int XpReward => Difficulty switch
    {
        QuizDifficulty.Easy => 10,
        QuizDifficulty.Medium => 20,
        QuizDifficulty.Hard => 35,
        _ => 10
    };
}

/// <summary>
/// Catégories de quiz
/// </summary>
public enum QuizCategory
{
    Ecologie,           // Questions sur l'environnement
    Recyclage,          // Questions sur le recyclage
    Solidarite,         // Questions sur le don et l'entraide
    EconomieCirculaire, // Questions sur la réutilisation
    Astuces,            // Conseils pratiques
    Culture             // Culture générale liée au thème
}

/// <summary>
/// Difficulté des questions
/// </summary>
public enum QuizDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Résultat d'une session de quiz
/// </summary>
public class QuizSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<QuizAnswer> Answers { get; set; } = new();
    public int TotalQuestions { get; set; }
    public int CorrectAnswers => Answers.Count(a => a.IsCorrect);
    public int TotalXpEarned { get; set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public double SuccessRate => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions * 100 : 0;
    public QuizSessionType Type { get; set; }
}

/// <summary>
/// Type de session de quiz
/// </summary>
public enum QuizSessionType
{
    Daily,      // Quiz quotidien (3 questions)
    Thematic,   // Quiz thématique (5-10 questions)
    Challenge   // Défi quiz (série de questions)
}

/// <summary>
/// Réponse à une question de quiz
/// </summary>
public class QuizAnswer
{
    public string QuestionId { get; set; } = string.Empty;
    public int SelectedAnswerIndex { get; set; }
    public bool IsCorrect { get; set; }
    public int XpEarned { get; set; }
    public double ResponseTimeSeconds { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Profil quiz de l'utilisateur
/// </summary>
public class UserQuizProfile
{
    public string UserId { get; set; } = string.Empty;
    public int TotalQuizzesCompleted { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int CurrentStreak { get; set; } // Jours consécutifs de quiz
    public int BestStreak { get; set; }
    public DateTime LastQuizDate { get; set; }
    public int TotalXpFromQuiz { get; set; }
    public Dictionary<string, int> CategoryStats { get; set; } = new(); // Bonnes réponses par catégorie
    
    /// <summary>
    /// Taux de réussite global
    /// </summary>
    public double OverallSuccessRate => TotalQuestionsAnswered > 0 
        ? (double)TotalCorrectAnswers / TotalQuestionsAnswered * 100 
        : 0;
    
    /// <summary>
    /// Peut faire le quiz quotidien aujourd'hui
    /// </summary>
    public bool CanPlayDailyQuiz => LastQuizDate.Date < DateTime.UtcNow.Date;
}

/// <summary>
/// Notification de résultat de quiz
/// </summary>
public class QuizResultNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public int XpEarned { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsPerfectScore => CorrectAnswers == TotalQuestions;
    public bool IsStreakBonus { get; set; }
    public int NewStreak { get; set; }
}


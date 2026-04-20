// Fichier: DonTroc/Services/OnboardingService.cs

using DonTroc.Models;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services;

/// <summary>
/// Modèle d'une étape de la checklist onboarding
/// </summary>
public class OnboardingStep
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string CompletedIcon { get; set; } = "✅";
    public int XpReward { get; set; }
    public bool IsCompleted { get; set; }
    /// <summary>
    /// Route Shell pour naviguer vers la page associée
    /// </summary>
    public string? NavigationRoute { get; set; }
}

/// <summary>
/// Service gérant l'onboarding (carousel bienvenue + checklist premiers pas).
/// État stocké dans Preferences.
/// </summary>
public class OnboardingService
{
    private readonly IGamificationService _gamificationService;
    private readonly AuthService _authService;
    private readonly ILogger<OnboardingService> _logger;

    // Clés Preferences
    private const string OnboardingCompletedKey = "onboarding_completed";
    private const string FirstSignInDateKey = "onboarding_first_signin_date";
    private const string ChecklistDismissedKey = "onboarding_checklist_dismissed";
    private const string StepPrefix = "onboarding_step_";

    // XP par étape
    private const int StepXpReward = 25;

    // Durée d'affichage de la checklist
    private static readonly TimeSpan ChecklistDuration = TimeSpan.FromDays(7);

    // Définition des 5 étapes
    private static readonly List<(string Id, string Title, string Description, string Icon, string? Route)> StepDefinitions = new()
    {
        ("complete_profile", "Compléter le profil", "Ajoutez une photo et une bio", "👤", "EditProfileView"),
        ("first_annonce", "Publier une annonce", "Créez votre première annonce", "📝", "CreationAnnonceView"),
        ("first_favorite", "Ajouter un favori", "Sauvegardez une annonce", "❤️", "//AnnoncesView"),
        ("first_message", "Envoyer un message", "Contactez un utilisateur", "💬", "//ConversationsView"),
        ("first_daily_reward", "Récompense quotidienne", "Réclamez votre premier bonus", "🎁", "RewardsPage"),
    };

    public OnboardingService(
        IGamificationService gamificationService,
        AuthService authService,
        ILogger<OnboardingService> logger)
    {
        _gamificationService = gamificationService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Indique si le carousel d'onboarding a été complété/skippé
    /// </summary>
    public bool IsOnboardingCompleted
    {
        get => Preferences.Get(OnboardingCompletedKey, false);
    }

    /// <summary>
    /// Marque le carousel comme terminé et enregistre la date de premier sign-in
    /// </summary>
    public void MarkOnboardingCompleted()
    {
        Preferences.Set(OnboardingCompletedKey, true);
        
        // Enregistrer la date de premier sign-in si pas déjà fait
        if (!Preferences.ContainsKey(FirstSignInDateKey))
        {
            Preferences.Set(FirstSignInDateKey, DateTime.UtcNow.ToString("o"));
        }

        _logger.LogInformation("[Onboarding] Carousel marqué comme complété");
    }

    /// <summary>
    /// Indique si la checklist doit être visible sur le Dashboard
    /// (non dismissée, dans les 7 premiers jours, pas toutes complétées)
    /// </summary>
    public bool IsChecklistVisible
    {
        get
        {
            // Pas encore passé l'onboarding → pas de checklist
            if (!IsOnboardingCompleted) return false;
            
            // Dismissée manuellement
            if (Preferences.Get(ChecklistDismissedKey, false)) return false;

            // Vérifier la fenêtre de 7 jours
            var dateStr = Preferences.Get(FirstSignInDateKey, string.Empty);
            if (string.IsNullOrEmpty(dateStr)) return false;

            if (DateTime.TryParse(dateStr, out var firstSignIn))
            {
                if (DateTime.UtcNow - firstSignIn > ChecklistDuration) return false;
            }

            // Vérifier si toutes les étapes sont complétées
            var allCompleted = StepDefinitions.All(s => Preferences.Get($"{StepPrefix}{s.Id}", false));
            if (allCompleted) return false;

            return true;
        }
    }

    /// <summary>
    /// Nombre d'étapes complétées
    /// </summary>
    public int CompletedCount => StepDefinitions.Count(s => Preferences.Get($"{StepPrefix}{s.Id}", false));

    /// <summary>
    /// Nombre total d'étapes
    /// </summary>
    public int TotalSteps => StepDefinitions.Count;

    /// <summary>
    /// Progression de 0.0 à 1.0
    /// </summary>
    public double Progress => TotalSteps > 0 ? (double)CompletedCount / TotalSteps : 0;

    /// <summary>
    /// Récupère la liste des étapes avec leur état
    /// </summary>
    public List<OnboardingStep> GetChecklistSteps()
    {
        return StepDefinitions.Select(s => new OnboardingStep
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Icon = s.Icon,
            XpReward = StepXpReward,
            IsCompleted = Preferences.Get($"{StepPrefix}{s.Id}", false),
            NavigationRoute = s.Route
        }).ToList();
    }

    /// <summary>
    /// Complète une étape et donne le bonus XP (si pas déjà complétée)
    /// </summary>
    public async Task<bool> CompleteStepAsync(string stepId)
    {
        var key = $"{StepPrefix}{stepId}";
        
        // Déjà complétée ?
        if (Preferences.Get(key, false)) return false;

        Preferences.Set(key, true);

        // Donner le bonus XP
        try
        {
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _gamificationService.AddXpAsync(userId, "onboarding_step", StepXpReward);
                _logger.LogInformation("[Onboarding] Étape '{Step}' complétée → +{Xp} XP", stepId, StepXpReward);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Onboarding] Erreur XP pour étape {Step}", stepId);
        }

        return true;
    }

    /// <summary>
    /// Masque définitivement la checklist
    /// </summary>
    public void DismissChecklist()
    {
        Preferences.Set(ChecklistDismissedKey, true);
        _logger.LogInformation("[Onboarding] Checklist masquée");
    }

    /// <summary>
    /// Vérifie et complète automatiquement les étapes basées sur l'état actuel
    /// (appelé au chargement du Dashboard)
    /// </summary>
    public async Task CheckAndCompleteStepsAsync(
        bool hasProfilePhoto,
        bool hasBio,
        int annonceCount,
        int favoriteCount,
        int messageCount,
        bool hasDailyReward)
    {
        if (hasProfilePhoto && hasBio)
            await CompleteStepAsync("complete_profile");

        if (annonceCount > 0)
            await CompleteStepAsync("first_annonce");

        if (favoriteCount > 0)
            await CompleteStepAsync("first_favorite");

        if (messageCount > 0)
            await CompleteStepAsync("first_message");

        if (hasDailyReward)
            await CompleteStepAsync("first_daily_reward");
    }
}


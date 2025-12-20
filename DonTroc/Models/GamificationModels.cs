namespace DonTroc.Models;

/// <summary>
/// Profil de gamification de l'utilisateur
/// </summary>
public class UserGamificationProfile
{
    public string UserId { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; } = 1;
    public int BoostCredits { get; set; }
    public int DailyStreak { get; set; }
    public DateTime LastDailyRewardClaimed { get; set; }
    public DateTime LastWheelSpin { get; set; }
    public List<string> UnlockedBadges { get; set; } = new();
    public List<string> CompletedChallenges { get; set; } = new();
    public Dictionary<string, int> Stats { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Calcule le niveau basé sur les XP
    /// </summary>
    public int CalculateLevel()
    {
        // Formule: chaque niveau nécessite 100 * niveau XP
        // Niveau 1: 0-99, Niveau 2: 100-299, Niveau 3: 300-599, etc.
        int level = 1;
        int xpRequired = 0;
        while (TotalXp >= xpRequired + (level * 100))
        {
            xpRequired += level * 100;
            level++;
        }
        return level;
    }
    
    /// <summary>
    /// XP nécessaires pour le prochain niveau
    /// </summary>
    public int XpForNextLevel => CurrentLevel * 100;
    
    /// <summary>
    /// XP actuels dans le niveau courant
    /// </summary>
    public int CurrentLevelXp
    {
        get
        {
            int xpUsed = 0;
            for (int i = 1; i < CurrentLevel; i++)
                xpUsed += i * 100;
            return TotalXp - xpUsed;
        }
    }
    
    /// <summary>
    /// Progression vers le prochain niveau (0-100%)
    /// </summary>
    public double LevelProgress => (double)CurrentLevelXp / XpForNextLevel * 100;
}

/// <summary>
/// Badge/Trophée que l'utilisateur peut débloquer
/// </summary>
public class Badge
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public BadgeRarity Rarity { get; set; } = BadgeRarity.Common;
    public BadgeCategory Category { get; set; }
    public int RequiredValue { get; set; }
    public string StatKey { get; set; } = string.Empty; // Ex: "annonces_created", "dons_realises"
    public int XpReward { get; set; }
    public bool IsSecret { get; set; }
}

public enum BadgeRarity
{
    Common,      // Gris
    Uncommon,    // Vert
    Rare,        // Bleu
    Epic,        // Violet
    Legendary    // Or
}

public enum BadgeCategory
{
    Donateur,       // Badges liés aux dons
    Receveur,       // Badges liés aux objets reçus
    Social,         // Badges liés aux interactions
    Explorateur,    // Badges liés à l'exploration
    Veteran,        // Badges liés au temps passé
    Special         // Badges événementiels
}

/// <summary>
/// Défi quotidien ou hebdomadaire
/// </summary>
public class Challenge
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ChallengeType Type { get; set; }
    public ChallengeDifficulty Difficulty { get; set; }
    public string ActionType { get; set; } = string.Empty; // Ex: "create_annonce", "complete_transaction"
    public int RequiredCount { get; set; }
    public int CurrentProgress { get; set; }
    public int XpReward { get; set; }
    public int BoostCreditsReward { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCompleted => CurrentProgress >= RequiredCount;
    public double ProgressPercentage => Math.Min(100, (double)CurrentProgress / RequiredCount * 100);
}

public enum ChallengeType
{
    Daily,
    Weekly,
    Special
}

public enum ChallengeDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Récompense de la roue de la fortune
/// </summary>
public class WheelReward
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public WheelRewardType Type { get; set; }
    public int Value { get; set; }
    public double Probability { get; set; } // 0.0 - 1.0
    public string Color { get; set; } = "#4CAF50";
}

public enum WheelRewardType
{
    Xp,
    BoostCredits,
    Badge,
    DoubleXpHours,
    Nothing
}

/// <summary>
/// Récompense quotidienne (streak)
/// </summary>
public class DailyReward
{
    public int Day { get; set; }
    public string Icon { get; set; } = string.Empty;
    public WheelRewardType Type { get; set; }
    public int Value { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsToday { get; set; }
    public bool IsLocked { get; set; }
}

/// <summary>
/// Historique des XP gagnés
/// </summary>
public class XpTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification de récompense (pour afficher les popups)
/// </summary>
public class RewardNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int? XpGained { get; set; }
    public int? BoostCreditsGained { get; set; }
    public Badge? BadgeUnlocked { get; set; }
    public bool LevelUp { get; set; }
    public int? NewLevel { get; set; }
}

/// <summary>
/// Statistiques utilisateur pour le tableau de bord
/// </summary>
public class UserStats
{
    public string UserId { get; set; } = string.Empty;
    public int TotalAnnonces { get; set; }
    public int TotalDonsRealises { get; set; }
    public int TotalObjetsRecus { get; set; }
    public int TotalConversations { get; set; }
    public int TotalFavoris { get; set; }
    public int TotalEvaluations { get; set; }
    public double NoteMoyenne { get; set; }
    public DateTime DerniereActivite { get; set; } = DateTime.UtcNow;
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
}


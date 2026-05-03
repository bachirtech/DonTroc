using DonTroc.Models;

namespace DonTroc.Configuration;

/// <summary>
/// Configuration du système de gamification.
/// Définit tous les badges, défis, et récompenses disponibles.
/// </summary>
public static class GamificationConfig
{
    // ============================================================================
    // CONFIGURATION XP - Points gagnés pour chaque action
    // ============================================================================
    
    public static readonly Dictionary<string, int> XpRewards = new()
    {
        // Actions principales
        { "create_annonce", 25 },
        { "complete_transaction_donor", 50 },      // Donneur qui finalise
        { "complete_transaction_receiver", 30 },   // Receveur qui finalise
        { "first_annonce", 100 },                  // Bonus première annonce
        { "first_transaction", 150 },              // Bonus première transaction
        
        // Actions sociales
        { "send_message", 5 },
        { "receive_rating_5stars", 40 },
        { "give_rating", 10 },
        { "share_app", 20 },
        { "invite_friend", 75 },
        
        // Engagement
        { "daily_login", 10 },
        { "daily_streak_bonus", 5 },              // Par jour de streak
        { "complete_profile", 50 },
        { "add_profile_photo", 30 },
        { "wheel_spin", 0 },                      // XP donnés par la roue
        
        // Défis
        { "complete_daily_challenge", 25 },
        { "complete_weekly_challenge", 100 },
        { "complete_monthly_challenge", 500 },

        // Propositions de troc structurées
        { "propose_trade", 10 },       // XP pour envoyer une proposition
        { "accept_trade", 25 },        // XP pour accepter une proposition

        // Événements & trocs groupés (Phase 4)
        { "create_event", 50 },        // XP pour créer un événement
        { "join_event", 15 },          // XP pour rejoindre un événement
        { "event_attended", 30 },      // XP quand on participe le jour J
    };
    
    // ============================================================================
    // NIVEAUX ET TITRES
    // ============================================================================
    
    public static readonly Dictionary<int, string> LevelTitles = new()
    {
        { 1, "Nouveau Troqueur" },
        { 2, "Troqueur Débutant" },
        { 3, "Troqueur Apprenti" },
        { 4, "Troqueur Confirmé" },
        { 5, "Troqueur Expérimenté" },
        { 6, "Troqueur Expert" },
        { 7, "Maître Troqueur" },
        { 8, "Grand Troqueur" },
        { 9, "Troqueur Légendaire" },
        { 10, "Troqueur Mythique" },
        { 15, "Gardien du Troc" },
        { 20, "Légende Vivante" },
        { 25, "Icône de DonTroc" },
        { 30, "Dieu du Troc" },
    };
    
    public static string GetLevelTitle(int level)
    {
        var title = LevelTitles
            .Where(x => x.Key <= level)
            .OrderByDescending(x => x.Key)
            .FirstOrDefault();
        return title.Value ?? "Troqueur";
    }
    
    // ============================================================================
    // BADGES DISPONIBLES
    // ============================================================================
    
    public static readonly List<Badge> AllBadges = new()
    {
        // === BADGES DONATEUR ===
        new Badge
        {
            Id = "first_gift",
            Name = "Premier Don",
            Description = "Effectuez votre premier don",
            Icon = "🎁",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Donateur,
            RequiredValue = 1,
            StatKey = "dons_realises",
            XpReward = 50
        },
        new Badge
        {
            Id = "generous_heart",
            Name = "Cœur Généreux",
            Description = "Faites 10 dons",
            Icon = "💝",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Donateur,
            RequiredValue = 10,
            StatKey = "dons_realises",
            XpReward = 150
        },
        new Badge
        {
            Id = "santa_claus",
            Name = "Père Noël",
            Description = "Faites 50 dons",
            Icon = "🎅",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Donateur,
            RequiredValue = 50,
            StatKey = "dons_realises",
            XpReward = 500
        },
        new Badge
        {
            Id = "philanthropist",
            Name = "Philanthrope",
            Description = "Faites 100 dons",
            Icon = "👑",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Donateur,
            RequiredValue = 100,
            StatKey = "dons_realises",
            XpReward = 1000
        },
        new Badge
        {
            Id = "legend_of_giving",
            Name = "Légende de la Générosité",
            Description = "Faites 500 dons",
            Icon = "🌟",
            Rarity = BadgeRarity.Legendary,
            Category = BadgeCategory.Donateur,
            RequiredValue = 500,
            StatKey = "dons_realises",
            XpReward = 5000
        },
        
        // === BADGES ANNONCES ===
        new Badge
        {
            Id = "first_annonce",
            Name = "Première Publication",
            Description = "Publiez votre première annonce",
            Icon = "📝",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Donateur,
            RequiredValue = 1,
            StatKey = "annonces_created",
            XpReward = 50
        },
        new Badge
        {
            Id = "active_seller",
            Name = "Vendeur Actif",
            Description = "Publiez 10 annonces",
            Icon = "📢",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Donateur,
            RequiredValue = 10,
            StatKey = "annonces_created",
            XpReward = 200
        },
        new Badge
        {
            Id = "shop_owner",
            Name = "Propriétaire de Boutique",
            Description = "Publiez 50 annonces",
            Icon = "🏪",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Donateur,
            RequiredValue = 50,
            StatKey = "annonces_created",
            XpReward = 500
        },
        
        // === BADGES RECEVEUR ===
        new Badge
        {
            Id = "first_receive",
            Name = "Premier Cadeau",
            Description = "Recevez votre premier objet",
            Icon = "🎀",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Receveur,
            RequiredValue = 1,
            StatKey = "objets_recus",
            XpReward = 50
        },
        new Badge
        {
            Id = "lucky_finder",
            Name = "Trouveur Chanceux",
            Description = "Recevez 10 objets",
            Icon = "🍀",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Receveur,
            RequiredValue = 10,
            StatKey = "objets_recus",
            XpReward = 150
        },
        new Badge
        {
            Id = "treasure_hunter",
            Name = "Chasseur de Trésors",
            Description = "Recevez 50 objets",
            Icon = "💎",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Receveur,
            RequiredValue = 50,
            StatKey = "objets_recus",
            XpReward = 500
        },
        
        // === BADGES SOCIAUX ===
        new Badge
        {
            Id = "communicator",
            Name = "Communicateur",
            Description = "Envoyez 50 messages",
            Icon = "💬",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Social,
            RequiredValue = 50,
            StatKey = "messages_sent",
            XpReward = 100
        },
        new Badge
        {
            Id = "social_butterfly",
            Name = "Papillon Social",
            Description = "Envoyez 500 messages",
            Icon = "🦋",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Social,
            RequiredValue = 500,
            StatKey = "messages_sent",
            XpReward = 300
        },
        new Badge
        {
            Id = "five_star_rated",
            Name = "Étoile Montante",
            Description = "Recevez 5 évaluations 5 étoiles",
            Icon = "⭐",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Social,
            RequiredValue = 5,
            StatKey = "five_star_ratings",
            XpReward = 200
        },
        new Badge
        {
            Id = "super_star",
            Name = "Super Star",
            Description = "Recevez 25 évaluations 5 étoiles",
            Icon = "🌟",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Social,
            RequiredValue = 25,
            StatKey = "five_star_ratings",
            XpReward = 500
        },
        
        // === BADGES EXPLORATEUR ===
        new Badge
        {
            Id = "early_bird",
            Name = "Lève-Tôt",
            Description = "Connectez-vous avant 7h",
            Icon = "🌅",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Explorateur,
            RequiredValue = 1,
            StatKey = "early_logins",
            XpReward = 75
        },
        new Badge
        {
            Id = "night_owl",
            Name = "Oiseau de Nuit",
            Description = "Connectez-vous après minuit",
            Icon = "🦉",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Explorateur,
            RequiredValue = 1,
            StatKey = "night_logins",
            XpReward = 75
        },
        new Badge
        {
            Id = "map_explorer",
            Name = "Explorateur",
            Description = "Utilisez la carte 10 fois",
            Icon = "🗺️",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Explorateur,
            RequiredValue = 10,
            StatKey = "map_views",
            XpReward = 100
        },
        
        // === BADGES VÉTÉRAN ===
        new Badge
        {
            Id = "one_week",
            Name = "Une Semaine",
            Description = "Membre depuis 7 jours",
            Icon = "📆",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Veteran,
            RequiredValue = 7,
            StatKey = "days_member",
            XpReward = 50
        },
        new Badge
        {
            Id = "one_month",
            Name = "Un Mois",
            Description = "Membre depuis 30 jours",
            Icon = "🗓️",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Veteran,
            RequiredValue = 30,
            StatKey = "days_member",
            XpReward = 150
        },
        new Badge
        {
            Id = "six_months",
            Name = "Six Mois",
            Description = "Membre depuis 6 mois",
            Icon = "🏆",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Veteran,
            RequiredValue = 180,
            StatKey = "days_member",
            XpReward = 500
        },
        new Badge
        {
            Id = "one_year",
            Name = "Anniversaire",
            Description = "Membre depuis 1 an",
            Icon = "🎂",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Veteran,
            RequiredValue = 365,
            StatKey = "days_member",
            XpReward = 1000
        },
        
        // === BADGES STREAK ===
        new Badge
        {
            Id = "streak_7",
            Name = "Semaine Parfaite",
            Description = "7 jours de connexion consécutifs",
            Icon = "🔥",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Veteran,
            RequiredValue = 7,
            StatKey = "max_streak",
            XpReward = 100
        },
        new Badge
        {
            Id = "streak_30",
            Name = "Mois de Feu",
            Description = "30 jours de connexion consécutifs",
            Icon = "🔥🔥",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Veteran,
            RequiredValue = 30,
            StatKey = "max_streak",
            XpReward = 500
        },
        new Badge
        {
            Id = "streak_100",
            Name = "Centurion",
            Description = "100 jours de connexion consécutifs",
            Icon = "💯",
            Rarity = BadgeRarity.Legendary,
            Category = BadgeCategory.Veteran,
            RequiredValue = 100,
            StatKey = "max_streak",
            XpReward = 2000
        },
        
        // === BADGES ÉVÉNEMENTS (Phase 4 — Trocs groupés) ===
        new Badge
        {
            Id = "event_organizer",
            Name = "Organisateur",
            Description = "Créez votre premier événement",
            Icon = "🏪",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Social,
            RequiredValue = 1,
            StatKey = "events_created",
            XpReward = 100
        },
        new Badge
        {
            Id = "event_master",
            Name = "Maître Organisateur",
            Description = "Créez 5 événements",
            Icon = "🎪",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Social,
            RequiredValue = 5,
            StatKey = "events_created",
            XpReward = 350
        },
        new Badge
        {
            Id = "event_legend",
            Name = "Légende des Événements",
            Description = "Créez 20 événements",
            Icon = "👑",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Social,
            RequiredValue = 20,
            StatKey = "events_created",
            XpReward = 1000
        },
        new Badge
        {
            Id = "event_participant",
            Name = "Participant",
            Description = "Rejoignez votre premier événement",
            Icon = "🎟️",
            Rarity = BadgeRarity.Common,
            Category = BadgeCategory.Social,
            RequiredValue = 1,
            StatKey = "events_joined",
            XpReward = 50
        },
        new Badge
        {
            Id = "event_assidu",
            Name = "Participant Assidu",
            Description = "Rejoignez 5 événements",
            Icon = "⚡",
            Rarity = BadgeRarity.Uncommon,
            Category = BadgeCategory.Social,
            RequiredValue = 5,
            StatKey = "events_joined",
            XpReward = 200
        },
        new Badge
        {
            Id = "event_addict",
            Name = "Accro aux Événements",
            Description = "Rejoignez 20 événements",
            Icon = "🔥",
            Rarity = BadgeRarity.Rare,
            Category = BadgeCategory.Social,
            RequiredValue = 20,
            StatKey = "events_joined",
            XpReward = 600
        },

        // === BADGES SPÉCIAUX (Secrets) ===
        new Badge
        {
            Id = "supporter",
            Name = "Supporter",
            Description = "Soutenez le développeur",
            Icon = "❤️",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Special,
            RequiredValue = 1,
            StatKey = "donations_made",
            XpReward = 500,
            IsSecret = true
        },
        new Badge
        {
            Id = "beta_tester",
            Name = "Beta Testeur",
            Description = "Membre depuis la beta",
            Icon = "🧪",
            Rarity = BadgeRarity.Legendary,
            Category = BadgeCategory.Special,
            RequiredValue = 1,
            StatKey = "is_beta_tester",
            XpReward = 1000,
            IsSecret = true
        },
        
        // === BADGES MENSUELS EXCLUSIFS ===
        new Badge
        {
            Id = "monthly_trader",
            Name = "Troqueur du Mois",
            Description = "Complétez la quête mensuelle de transactions",
            Icon = "🏅",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Special,
            RequiredValue = 1,
            StatKey = "monthly_quest_trader",
            XpReward = 0, // XP déjà donnés via le challenge
            IsSecret = false
        },
        new Badge
        {
            Id = "monthly_publisher",
            Name = "Éditeur du Mois",
            Description = "Complétez la quête mensuelle de publications",
            Icon = "📰",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Special,
            RequiredValue = 1,
            StatKey = "monthly_quest_publisher",
            XpReward = 0,
            IsSecret = false
        },
        new Badge
        {
            Id = "monthly_social",
            Name = "Star Sociale du Mois",
            Description = "Complétez la quête mensuelle sociale",
            Icon = "🌟",
            Rarity = BadgeRarity.Epic,
            Category = BadgeCategory.Special,
            RequiredValue = 1,
            StatKey = "monthly_quest_social",
            XpReward = 0,
            IsSecret = false
        },
        new Badge
        {
            Id = "monthly_explorer",
            Name = "Explorateur du Mois",
            Description = "Complétez la quête mensuelle d'exploration",
            Icon = "🗺️",
            Rarity = BadgeRarity.Legendary,
            Category = BadgeCategory.Special,
            RequiredValue = 1,
            StatKey = "monthly_quest_explorer",
            XpReward = 0,
            IsSecret = false
        },
    };
    
    // ============================================================================
    // ROUE DE LA FORTUNE
    // ============================================================================
    
    public static readonly List<WheelReward> WheelRewards = new()
    {
        new WheelReward { Id = "xp_10", Name = "+10 XP", Icon = "✨", Type = WheelRewardType.Xp, Value = 10, Probability = 0.25, Color = "#4CAF50" },
        new WheelReward { Id = "xp_25", Name = "+25 XP", Icon = "⭐", Type = WheelRewardType.Xp, Value = 25, Probability = 0.20, Color = "#8BC34A" },
        new WheelReward { Id = "xp_50", Name = "+50 XP", Icon = "🌟", Type = WheelRewardType.Xp, Value = 50, Probability = 0.15, Color = "#CDDC39" },
        new WheelReward { Id = "xp_100", Name = "+100 XP", Icon = "💫", Type = WheelRewardType.Xp, Value = 100, Probability = 0.05, Color = "#FFEB3B" },
        new WheelReward { Id = "boost_1", Name = "+1 Boost", Icon = "🚀", Type = WheelRewardType.BoostCredits, Value = 1, Probability = 0.15, Color = "#FF9800" },
        new WheelReward { Id = "boost_3", Name = "+3 Boosts", Icon = "🚀🚀", Type = WheelRewardType.BoostCredits, Value = 3, Probability = 0.05, Color = "#FF5722" },
        new WheelReward { Id = "double_xp", Name = "2x XP (2h)", Icon = "⚡", Type = WheelRewardType.DoubleXpHours, Value = 2, Probability = 0.05, Color = "#9C27B0" },
        new WheelReward { Id = "nothing", Name = "Réessayez !", Icon = "😅", Type = WheelRewardType.Nothing, Value = 0, Probability = 0.10, Color = "#9E9E9E" },
    };
    
    // ============================================================================
    // RÉCOMPENSES QUOTIDIENNES (7 jours)
    // ============================================================================
    
    public static readonly List<DailyReward> DailyRewards = new()
    {
        new DailyReward { Day = 1, Icon = "🎁", Type = WheelRewardType.Xp, Value = 20 },
        new DailyReward { Day = 2, Icon = "🎁", Type = WheelRewardType.Xp, Value = 30 },
        new DailyReward { Day = 3, Icon = "🚀", Type = WheelRewardType.BoostCredits, Value = 1 },
        new DailyReward { Day = 4, Icon = "🎁", Type = WheelRewardType.Xp, Value = 50 },
        new DailyReward { Day = 5, Icon = "🎁", Type = WheelRewardType.Xp, Value = 75 },
        new DailyReward { Day = 6, Icon = "🚀", Type = WheelRewardType.BoostCredits, Value = 2 },
        new DailyReward { Day = 7, Icon = "🎉", Type = WheelRewardType.Xp, Value = 150 }, // Bonus jour 7
    };
    
    // ============================================================================
    // DÉFIS QUOTIDIENS TEMPLATES
    // ============================================================================
    
    public static readonly List<Challenge> DailyChallengeTemplates = new()
    {
        new Challenge
        {
            Id = "daily_login",
            Title = "Connexion du jour",
            Description = "Connectez-vous à l'application",
            Icon = "👋",
            Type = ChallengeType.Daily,
            Difficulty = ChallengeDifficulty.Easy,
            ActionType = "login",
            RequiredCount = 1,
            XpReward = 15,
            BoostCreditsReward = 0
        },
        new Challenge
        {
            Id = "daily_browse",
            Title = "Explorateur",
            Description = "Consultez 5 annonces",
            Icon = "👀",
            Type = ChallengeType.Daily,
            Difficulty = ChallengeDifficulty.Easy,
            ActionType = "view_annonce",
            RequiredCount = 5,
            XpReward = 20,
            BoostCreditsReward = 0
        },
        new Challenge
        {
            Id = "daily_message",
            Title = "Bavard",
            Description = "Envoyez 3 messages",
            Icon = "💬",
            Type = ChallengeType.Daily,
            Difficulty = ChallengeDifficulty.Easy,
            ActionType = "send_message",
            RequiredCount = 3,
            XpReward = 25,
            BoostCreditsReward = 0
        },
        new Challenge
        {
            Id = "daily_create",
            Title = "Créateur",
            Description = "Publiez une annonce",
            Icon = "📝",
            Type = ChallengeType.Daily,
            Difficulty = ChallengeDifficulty.Medium,
            ActionType = "create_annonce",
            RequiredCount = 1,
            XpReward = 40,
            BoostCreditsReward = 0
        },
        new Challenge
        {
            Id = "daily_map",
            Title = "Cartographe",
            Description = "Utilisez la carte",
            Icon = "🗺️",
            Type = ChallengeType.Daily,
            Difficulty = ChallengeDifficulty.Easy,
            ActionType = "view_map",
            RequiredCount = 1,
            XpReward = 15,
            BoostCreditsReward = 0
        },
    };
    
    // ============================================================================
    // DÉFIS HEBDOMADAIRES TEMPLATES
    // ============================================================================
    
    public static readonly List<Challenge> WeeklyChallengeTemplates = new()
    {
        new Challenge
        {
            Id = "weekly_transactions",
            Title = "Troqueur de la Semaine",
            Description = "Complétez 3 transactions",
            Icon = "🤝",
            Type = ChallengeType.Weekly,
            Difficulty = ChallengeDifficulty.Medium,
            ActionType = "complete_transaction",
            RequiredCount = 3,
            XpReward = 150,
            BoostCreditsReward = 1
        },
        new Challenge
        {
            Id = "weekly_annonces",
            Title = "Producteur",
            Description = "Publiez 5 annonces",
            Icon = "📢",
            Type = ChallengeType.Weekly,
            Difficulty = ChallengeDifficulty.Medium,
            ActionType = "create_annonce",
            RequiredCount = 5,
            XpReward = 200,
            BoostCreditsReward = 1
        },
        new Challenge
        {
            Id = "weekly_social",
            Title = "Ambassadeur",
            Description = "Envoyez 20 messages",
            Icon = "🦋",
            Type = ChallengeType.Weekly,
            Difficulty = ChallengeDifficulty.Easy,
            ActionType = "send_message",
            RequiredCount = 20,
            XpReward = 100,
            BoostCreditsReward = 0
        },
        new Challenge
        {
            Id = "weekly_streak",
            Title = "Fidélité",
            Description = "Connectez-vous 7 jours d'affilée",
            Icon = "🔥",
            Type = ChallengeType.Weekly,
            Difficulty = ChallengeDifficulty.Hard,
            ActionType = "daily_streak",
            RequiredCount = 7,
            XpReward = 300,
            BoostCreditsReward = 2
        },
        new Challenge
        {
            Id = "weekly_ratings",
            Title = "Évaluateur",
            Description = "Donnez 3 évaluations",
            Icon = "⭐",
            Type = ChallengeType.Weekly,
            Difficulty = ChallengeDifficulty.Easy,
            ActionType = "give_rating",
            RequiredCount = 3,
            XpReward = 75,
            BoostCreditsReward = 0
        },
    };
    
    // ============================================================================
    // QUÊTES MENSUELLES TEMPLATES
    // ============================================================================
    
    public static readonly List<Challenge> MonthlyChallengeTemplates = new()
    {
        new Challenge
        {
            Id = "monthly_transactions",
            Title = "Maître du Troc",
            Description = "Complétez 10 transactions ce mois",
            Icon = "🤝",
            Type = ChallengeType.Monthly,
            Difficulty = ChallengeDifficulty.Hard,
            ActionType = "complete_transaction",
            RequiredCount = 10,
            XpReward = 500,
            BoostCreditsReward = 3,
            ExclusiveBadgeId = "monthly_trader"
        },
        new Challenge
        {
            Id = "monthly_annonces",
            Title = "Éditeur Prolifique",
            Description = "Publiez 15 annonces ce mois",
            Icon = "📢",
            Type = ChallengeType.Monthly,
            Difficulty = ChallengeDifficulty.Hard,
            ActionType = "create_annonce",
            RequiredCount = 15,
            XpReward = 400,
            BoostCreditsReward = 2,
            ExclusiveBadgeId = "monthly_publisher"
        },
        new Challenge
        {
            Id = "monthly_social",
            Title = "Ambassadeur Social",
            Description = "Envoyez 100 messages ce mois",
            Icon = "💬",
            Type = ChallengeType.Monthly,
            Difficulty = ChallengeDifficulty.Medium,
            ActionType = "send_message",
            RequiredCount = 100,
            XpReward = 350,
            BoostCreditsReward = 2,
            ExclusiveBadgeId = "monthly_social"
        },
        new Challenge
        {
            Id = "monthly_explorer",
            Title = "Grand Explorateur",
            Description = "Consultez 50 annonces ce mois",
            Icon = "🔍",
            Type = ChallengeType.Monthly,
            Difficulty = ChallengeDifficulty.Medium,
            ActionType = "view_annonce",
            RequiredCount = 50,
            XpReward = 300,
            BoostCreditsReward = 1,
            ExclusiveBadgeId = "monthly_explorer"
        },
    };
}



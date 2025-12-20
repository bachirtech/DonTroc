using DonTroc.Models;

namespace DonTroc.Configuration;

/// <summary>
/// Configuration et banque de questions pour le système de quiz
/// </summary>
public static class QuizConfig
{
    /// <summary>
    /// Nombre de questions par quiz quotidien
    /// </summary>
    public const int DailyQuizQuestionCount = 3;
    
    /// <summary>
    /// Bonus XP pour un score parfait
    /// </summary>
    public const int PerfectScoreBonus = 25;
    
    /// <summary>
    /// Bonus XP par jour de streak
    /// </summary>
    public const int StreakBonusPerDay = 5;
    
    /// <summary>
    /// Temps maximum pour répondre (bonus si rapide)
    /// </summary>
    public const int MaxResponseTimeSeconds = 30;
    
    /// <summary>
    /// Bonus pour réponse rapide (moins de 10 secondes)
    /// </summary>
    public const int QuickAnswerBonus = 5;
    
    /// <summary>
    /// Banque complète de questions
    /// </summary>
    public static readonly List<QuizQuestion> AllQuestions = new()
    {
        // ===== ÉCOLOGIE =====
        new QuizQuestion
        {
            Id = "eco_1",
            Question = "Combien d'années faut-il pour qu'un sac plastique se dégrade dans la nature ?",
            Options = new List<string> { "10 ans", "100 ans", "450 ans", "1000 ans" },
            CorrectAnswerIndex = 2,
            Explanation = "Un sac plastique met environ 450 ans à se dégrader ! C'est pourquoi le don et la réutilisation sont essentiels.",
            Category = QuizCategory.Ecologie,
            Difficulty = QuizDifficulty.Medium,
            Icon = "🌍"
        },
        new QuizQuestion
        {
            Id = "eco_2",
            Question = "Quel pourcentage de déchets textiles est recyclé en France ?",
            Options = new List<string> { "Moins de 1%", "Environ 10%", "Environ 30%", "Plus de 50%" },
            CorrectAnswerIndex = 1,
            Explanation = "Seulement 10% des textiles sont recyclés ! Donner vos vêtements via DonTroc aide à réduire ce gaspillage.",
            Category = QuizCategory.Ecologie,
            Difficulty = QuizDifficulty.Medium,
            Icon = "👕"
        },
        new QuizQuestion
        {
            Id = "eco_3",
            Question = "Combien de litres d'eau faut-il pour fabriquer un jean neuf ?",
            Options = new List<string> { "500 litres", "2000 litres", "7500 litres", "15000 litres" },
            CorrectAnswerIndex = 2,
            Explanation = "Il faut environ 7500 litres d'eau pour produire un seul jean ! Donner ou recevoir un jean d'occasion économise cette eau.",
            Category = QuizCategory.Ecologie,
            Difficulty = QuizDifficulty.Hard,
            Icon = "💧"
        },
        new QuizQuestion
        {
            Id = "eco_4",
            Question = "Quelle est la durée de vie moyenne d'un smartphone avant d'être remplacé ?",
            Options = new List<string> { "1 an", "2-3 ans", "5 ans", "7 ans" },
            CorrectAnswerIndex = 1,
            Explanation = "En moyenne, un smartphone est remplacé tous les 2-3 ans alors qu'il pourrait fonctionner plus longtemps !",
            Category = QuizCategory.Ecologie,
            Difficulty = QuizDifficulty.Easy,
            Icon = "📱"
        },
        new QuizQuestion
        {
            Id = "eco_5",
            Question = "Quel est l'impact carbone de la production d'un ordinateur portable ?",
            Options = new List<string> { "50 kg CO2", "150 kg CO2", "350 kg CO2", "500 kg CO2" },
            CorrectAnswerIndex = 2,
            Explanation = "Produire un ordinateur émet environ 350 kg de CO2. Prolonger sa vie ou le donner réduit considérablement cet impact !",
            Category = QuizCategory.Ecologie,
            Difficulty = QuizDifficulty.Hard,
            Icon = "💻"
        },
        
        // ===== RECYCLAGE =====
        new QuizQuestion
        {
            Id = "rec_1",
            Question = "Que signifie le symbole du triangle avec un chiffre sur les plastiques ?",
            Options = new List<string> { "Le prix du produit", "Le type de plastique", "L'année de fabrication", "Le niveau de toxicité" },
            CorrectAnswerIndex = 1,
            Explanation = "Le chiffre indique le type de plastique (PET, PEHD, etc.) pour faciliter le tri et le recyclage.",
            Category = QuizCategory.Recyclage,
            Difficulty = QuizDifficulty.Easy,
            Icon = "♻️"
        },
        new QuizQuestion
        {
            Id = "rec_2",
            Question = "Combien de fois peut-on recycler le verre ?",
            Options = new List<string> { "1 à 2 fois", "5 à 10 fois", "À l'infini", "Il ne se recycle pas" },
            CorrectAnswerIndex = 2,
            Explanation = "Le verre peut être recyclé à l'infini sans perdre ses qualités ! C'est le matériau le plus recyclable.",
            Category = QuizCategory.Recyclage,
            Difficulty = QuizDifficulty.Medium,
            Icon = "🥛"
        },
        new QuizQuestion
        {
            Id = "rec_3",
            Question = "Où jeter une ampoule LED usagée ?",
            Options = new List<string> { "Poubelle classique", "Bac jaune", "Déchetterie ou magasin", "Bac vert" },
            CorrectAnswerIndex = 2,
            Explanation = "Les ampoules LED doivent être déposées en déchetterie ou dans les bacs de collecte en magasin.",
            Category = QuizCategory.Recyclage,
            Difficulty = QuizDifficulty.Medium,
            Icon = "💡"
        },
        new QuizQuestion
        {
            Id = "rec_4",
            Question = "Que devient le papier recyclé ?",
            Options = new List<string> { "Uniquement du carton", "Nouveau papier, carton, isolant", "Compost", "Il est incinéré" },
            CorrectAnswerIndex = 1,
            Explanation = "Le papier recyclé peut devenir du nouveau papier, du carton, ou même de l'isolant pour les maisons !",
            Category = QuizCategory.Recyclage,
            Difficulty = QuizDifficulty.Easy,
            Icon = "📄"
        },
        new QuizQuestion
        {
            Id = "rec_5",
            Question = "Quelle matière ne va PAS dans le bac jaune ?",
            Options = new List<string> { "Bouteille plastique", "Boîte de conserve", "Pot de yaourt en verre", "Carton" },
            CorrectAnswerIndex = 2,
            Explanation = "Le verre a son propre bac de collecte ! Il ne doit pas aller dans le bac jaune.",
            Category = QuizCategory.Recyclage,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🗑️"
        },
        
        // ===== SOLIDARITÉ =====
        new QuizQuestion
        {
            Id = "sol_1",
            Question = "Quel est le premier avantage du don entre particuliers ?",
            Options = new List<string> { "Gagner de l'argent", "Créer du lien social", "Se débarrasser rapidement", "Avoir des réductions" },
            CorrectAnswerIndex = 1,
            Explanation = "Le don crée du lien social et de l'entraide dans nos communautés. C'est bien plus qu'un simple échange !",
            Category = QuizCategory.Solidarite,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🤝"
        },
        new QuizQuestion
        {
            Id = "sol_2",
            Question = "Combien de tonnes d'objets sont jetés chaque année en France alors qu'ils pourraient être réutilisés ?",
            Options = new List<string> { "100 000 tonnes", "500 000 tonnes", "2 millions tonnes", "Plus de 3 millions tonnes" },
            CorrectAnswerIndex = 3,
            Explanation = "Plus de 3 millions de tonnes d'objets encore utilisables finissent à la poubelle chaque année !",
            Category = QuizCategory.Solidarite,
            Difficulty = QuizDifficulty.Hard,
            Icon = "📦"
        },
        new QuizQuestion
        {
            Id = "sol_3",
            Question = "Quel sentiment ressent-on le plus souvent après avoir fait un don ?",
            Options = new List<string> { "Regret", "Indifférence", "Satisfaction et bien-être", "Culpabilité" },
            CorrectAnswerIndex = 2,
            Explanation = "Des études montrent que donner active les zones du cerveau liées au plaisir et au bien-être !",
            Category = QuizCategory.Solidarite,
            Difficulty = QuizDifficulty.Easy,
            Icon = "😊"
        },
        new QuizQuestion
        {
            Id = "sol_4",
            Question = "Quelle période de l'année voit le plus de dons ?",
            Options = new List<string> { "Été", "Rentrée scolaire", "Fêtes de fin d'année", "Printemps" },
            CorrectAnswerIndex = 2,
            Explanation = "Les fêtes de fin d'année sont propices à la générosité, mais on peut donner toute l'année !",
            Category = QuizCategory.Solidarite,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🎄"
        },
        new QuizQuestion
        {
            Id = "sol_5",
            Question = "Quel est l'impact d'un don de jouets sur un enfant ?",
            Options = new List<string> { "Aucun impact particulier", "Développe sa créativité", "Le rend matérialiste", "Lui apprend à partager et recevoir" },
            CorrectAnswerIndex = 3,
            Explanation = "Recevoir un don apprend aux enfants la valeur du partage et de la gratitude.",
            Category = QuizCategory.Solidarite,
            Difficulty = QuizDifficulty.Medium,
            Icon = "🧸"
        },
        
        // ===== ÉCONOMIE CIRCULAIRE =====
        new QuizQuestion
        {
            Id = "circ_1",
            Question = "Qu'est-ce que l'économie circulaire ?",
            Options = new List<string> { "Acheter en rond", "Réutiliser et recycler au maximum", "Un type de monnaie", "L'économie des cercles" },
            CorrectAnswerIndex = 1,
            Explanation = "L'économie circulaire vise à réduire les déchets en réutilisant, réparant et recyclant au maximum.",
            Category = QuizCategory.EconomieCirculaire,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🔄"
        },
        new QuizQuestion
        {
            Id = "circ_2",
            Question = "Combien économise-t-on en moyenne en achetant d'occasion plutôt que neuf ?",
            Options = new List<string> { "10-20%", "30-50%", "50-70%", "Plus de 80%" },
            CorrectAnswerIndex = 2,
            Explanation = "L'occasion permet d'économiser 50 à 70% en moyenne, et c'est encore mieux quand c'est gratuit via le don !",
            Category = QuizCategory.EconomieCirculaire,
            Difficulty = QuizDifficulty.Medium,
            Icon = "💰"
        },
        new QuizQuestion
        {
            Id = "circ_3",
            Question = "Quelle est la règle des 5R de l'économie circulaire ?",
            Options = new List<string> { 
                "Réduire, Réparer, Réutiliser, Recycler, Rot (composter)", 
                "Riche, Rapide, Rentable, Réel, Rigoureux",
                "Rouge, Rose, Rien, Rare, Royal",
                "Relire, Réécrire, Refaire, Revoir, Reprendre"
            },
            CorrectAnswerIndex = 0,
            Explanation = "Les 5R : Refuser, Réduire, Réutiliser, Recycler, et Rot (composter). Le don fait partie de 'Réutiliser' !",
            Category = QuizCategory.EconomieCirculaire,
            Difficulty = QuizDifficulty.Medium,
            Icon = "5️⃣"
        },
        new QuizQuestion
        {
            Id = "circ_4",
            Question = "Quel objet a la plus longue durée de vie potentielle s'il est bien entretenu ?",
            Options = new List<string> { "Un téléphone", "Un meuble en bois", "Un vêtement synthétique", "Un jouet en plastique" },
            CorrectAnswerIndex = 1,
            Explanation = "Un meuble en bois de qualité peut durer plusieurs générations s'il est bien entretenu !",
            Category = QuizCategory.EconomieCirculaire,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🪑"
        },
        new QuizQuestion
        {
            Id = "circ_5",
            Question = "Qu'est-ce que l'obsolescence programmée ?",
            Options = new List<string> { 
                "Un programme informatique",
                "La durée de vie limitée volontairement par les fabricants",
                "Une mode passagère",
                "Le vieillissement naturel des objets"
            },
            CorrectAnswerIndex = 1,
            Explanation = "L'obsolescence programmée est une stratégie pour limiter la durée de vie des produits. Le don combat ce gaspillage !",
            Category = QuizCategory.EconomieCirculaire,
            Difficulty = QuizDifficulty.Medium,
            Icon = "⏰"
        },
        
        // ===== ASTUCES =====
        new QuizQuestion
        {
            Id = "ast_1",
            Question = "Quelle est la meilleure façon de présenter un objet à donner ?",
            Options = new List<string> { "Photo floue rapide", "Pas de photo nécessaire", "Photos claires avec description détaillée", "Juste le prix" },
            CorrectAnswerIndex = 2,
            Explanation = "Des photos claires et une description honnête augmentent vos chances de trouver preneur rapidement !",
            Category = QuizCategory.Astuces,
            Difficulty = QuizDifficulty.Easy,
            Icon = "📸"
        },
        new QuizQuestion
        {
            Id = "ast_2",
            Question = "Que faire avant de donner un appareil électronique ?",
            Options = new List<string> { "Le jeter directement", "Effacer ses données personnelles", "Le casser", "Rien de particulier" },
            CorrectAnswerIndex = 1,
            Explanation = "Pensez toujours à effacer vos données personnelles avant de donner un appareil électronique !",
            Category = QuizCategory.Astuces,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🔐"
        },
        new QuizQuestion
        {
            Id = "ast_3",
            Question = "Comment maximiser les chances qu'un vêtement donné soit pris ?",
            Options = new List<string> { "Le donner froissé", "Le laver et bien le plier", "Cacher les défauts", "Ne rien préciser" },
            CorrectAnswerIndex = 1,
            Explanation = "Un vêtement propre et bien présenté sera plus attractif. Mentionnez aussi les éventuels défauts !",
            Category = QuizCategory.Astuces,
            Difficulty = QuizDifficulty.Easy,
            Icon = "✨"
        },
        new QuizQuestion
        {
            Id = "ast_4",
            Question = "Quel est le meilleur moment pour publier une annonce de don ?",
            Options = new List<string> { "Lundi matin tôt", "En semaine le soir", "Dimanche après-midi", "Peu importe" },
            CorrectAnswerIndex = 1,
            Explanation = "Les soirs de semaine (18h-21h) sont souvent les moments où les gens consultent le plus les annonces.",
            Category = QuizCategory.Astuces,
            Difficulty = QuizDifficulty.Medium,
            Icon = "⏰"
        },
        new QuizQuestion
        {
            Id = "ast_5",
            Question = "Que faire si personne ne veut votre objet après plusieurs semaines ?",
            Options = new List<string> { 
                "Le jeter", 
                "Améliorer la description/photos ou proposer à une association",
                "Augmenter le prix",
                "Abandonner"
            },
            CorrectAnswerIndex = 1,
            Explanation = "Essayez d'améliorer votre annonce ou proposez l'objet à une association locale !",
            Category = QuizCategory.Astuces,
            Difficulty = QuizDifficulty.Easy,
            Icon = "💡"
        },
        
        // ===== CULTURE =====
        new QuizQuestion
        {
            Id = "cul_1",
            Question = "Dans quel pays le troc est-il encore très pratiqué au quotidien ?",
            Options = new List<string> { "États-Unis", "Japon", "Inde rurale", "Australie" },
            CorrectAnswerIndex = 2,
            Explanation = "Dans les zones rurales de l'Inde, le troc reste une pratique courante pour les échanges locaux.",
            Category = QuizCategory.Culture,
            Difficulty = QuizDifficulty.Medium,
            Icon = "🌏"
        },
        new QuizQuestion
        {
            Id = "cul_2",
            Question = "Quelle civilisation antique utilisait principalement le troc ?",
            Options = new List<string> { "Romains", "Égyptiens", "Phéniciens", "Toutes ces civilisations" },
            CorrectAnswerIndex = 3,
            Explanation = "Le troc était la base des échanges dans toutes les civilisations avant l'invention de la monnaie !",
            Category = QuizCategory.Culture,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🏛️"
        },
        new QuizQuestion
        {
            Id = "cul_3",
            Question = "Qu'est-ce qu'un 'Repair Café' ?",
            Options = new List<string> { 
                "Un café qui répare les machines",
                "Un lieu où des bénévoles aident à réparer des objets",
                "Un café recyclé",
                "Une marque de café"
            },
            CorrectAnswerIndex = 1,
            Explanation = "Les Repair Cafés sont des lieux conviviaux où des bénévoles aident gratuitement à réparer des objets.",
            Category = QuizCategory.Culture,
            Difficulty = QuizDifficulty.Medium,
            Icon = "🔧"
        },
        new QuizQuestion
        {
            Id = "cul_4",
            Question = "Quelle est l'origine du mot 'troc' ?",
            Options = new List<string> { "Latin 'trocare'", "Grec 'trokos'", "Origine incertaine, peut-être germanique", "Arabe 'taraka'" },
            CorrectAnswerIndex = 2,
            Explanation = "L'origine du mot 'troc' est incertaine, probablement d'origine germanique ou d'un dialecte roman.",
            Category = QuizCategory.Culture,
            Difficulty = QuizDifficulty.Hard,
            Icon = "📚"
        },
        new QuizQuestion
        {
            Id = "cul_5",
            Question = "Quel mouvement prône le 'zéro déchet' ?",
            Options = new List<string> { "Zero Waste", "Green Peace", "Blue Planet", "Clean World" },
            CorrectAnswerIndex = 0,
            Explanation = "Le mouvement Zero Waste encourage à réduire nos déchets au maximum via le don, la réparation et le recyclage.",
            Category = QuizCategory.Culture,
            Difficulty = QuizDifficulty.Easy,
            Icon = "🌱"
        }
    };
    
    /// <summary>
    /// Obtenir des questions aléatoires pour un quiz quotidien
    /// </summary>
    public static List<QuizQuestion> GetDailyQuizQuestions()
    {
        var random = new Random();
        return AllQuestions
            .OrderBy(_ => random.Next())
            .Take(DailyQuizQuestionCount)
            .ToList();
    }
    
    /// <summary>
    /// Obtenir des questions par catégorie
    /// </summary>
    public static List<QuizQuestion> GetQuestionsByCategory(QuizCategory category, int count = 5)
    {
        var random = new Random();
        return AllQuestions
            .Where(q => q.Category == category)
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();
    }
    
    /// <summary>
    /// Obtenir le nom français d'une catégorie
    /// </summary>
    public static string GetCategoryName(QuizCategory category)
    {
        return category switch
        {
            QuizCategory.Ecologie => "🌍 Écologie",
            QuizCategory.Recyclage => "♻️ Recyclage",
            QuizCategory.Solidarite => "🤝 Solidarité",
            QuizCategory.EconomieCirculaire => "🔄 Économie Circulaire",
            QuizCategory.Astuces => "💡 Astuces",
            QuizCategory.Culture => "📚 Culture",
            _ => "❓ Divers"
        };
    }
    
    /// <summary>
    /// Obtenir la couleur d'une catégorie
    /// </summary>
    public static string GetCategoryColor(QuizCategory category)
    {
        return category switch
        {
            QuizCategory.Ecologie => "#4CAF50",
            QuizCategory.Recyclage => "#2196F3",
            QuizCategory.Solidarite => "#E91E63",
            QuizCategory.EconomieCirculaire => "#FF9800",
            QuizCategory.Astuces => "#9C27B0",
            QuizCategory.Culture => "#795548",
            _ => "#607D8B"
        };
    }
}


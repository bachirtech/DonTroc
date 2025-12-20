namespace DonTroc.Configuration;

/// <summary>
/// Configuration centralisée pour les liens de donation et informations développeur.
/// Modifiez ces valeurs avec vos propres liens de donation.
/// </summary>
public static class DonationConfig
{
    // ============================================================================
    // LIENS DE DONATION - À personnaliser avec vos propres URLs
    // ============================================================================
    
    /// <summary>
    /// Lien PayPal pour recevoir des dons
    /// Format: https://paypal.me/votrenom
    /// Créez votre lien sur: https://www.paypal.com/paypalme/
    /// </summary>
    public const string PayPalUrl = "https://paypal.me/b1entertenment";
    
    /// <summary>
    /// Lien Ko-fi pour paiement direct par carte bancaire
    /// Gratuit, sans frais - Aucun compte requis pour le donateur
    /// Ko-fi accepte les créateurs du Maroc !
    /// Format: https://ko-fi.com/votrenom
    /// Créez votre page sur: https://ko-fi.com
    /// </summary>
    public const string KofiUrl = "https://ko-fi.com/bachirdev";
    
    // ============================================================================
    // LIENS DES STORES - Pour noter l'application
    // ============================================================================
    
    /// <summary>
    /// ID de l'application sur Google Play Store
    /// </summary>
    public const string AndroidPackageId = "com.bachirdev.dontroc";
    
    /// <summary>
    /// Lien direct vers le Play Store
    /// </summary>
    public const string PlayStoreUrl = $"https://play.google.com/store/apps/details?id={AndroidPackageId}";
    
    /// <summary>
    /// ID de l'application sur l'App Store (à remplir après publication)
    /// </summary>
    public const string AppStoreId = "XXXXXXXXXX";
    
    /// <summary>
    /// Lien direct vers l'App Store
    /// </summary>
    public const string AppStoreUrl = $"https://apps.apple.com/app/dontroc/id{AppStoreId}";
    
    // ============================================================================
    // INFORMATIONS DÉVELOPPEUR
    // ============================================================================
    
    /// <summary>
    /// Nom du développeur
    /// </summary>
    public const string DeveloperName = "Bassirou Balde";
    
    /// <summary>
    /// Nom de l'entreprise/marque
    /// </summary>
    public const string CompanyName = "BachirDev";
    
    /// <summary>
    /// Email de contact (optionnel)
    /// </summary>
    public const string ContactEmail = "bachirdev.pro@gmail.com";
    
    /// <summary>
    /// Site web (optionnel)
    /// </summary>
    //public const string WebsiteUrl = "https://bachirdev.com";
    
    // ============================================================================
    // MESSAGES PERSONNALISABLES
    // ============================================================================
    
    /// <summary>
    /// Message d'accueil du popup de donation
    /// </summary>
    public const string WelcomeMessage = 
        "Merci de votre intérêt pour DonTroc ! Votre soutien aide à maintenir " +
        "l'application gratuite et à ajouter de nouvelles fonctionnalités.";
    
    /// <summary>
    /// Message de remerciement après un don
    /// </summary>
    public const string ThankYouMessage = 
        "Merci infiniment pour votre générosité ! Votre soutien me permet " +
        "de continuer à améliorer DonTroc.";
    
    /// <summary>
    /// Message de partage de l'application
    /// </summary>
    public const string ShareMessage = 
        "🌿 DonTroc - L'application de dons et échanges entre particuliers !\n\n" +
        "Donnez une seconde vie à vos objets et trouvez des trésors près de chez vous.\n\n" +
        $"📱 Téléchargez gratuitement :\n{PlayStoreUrl}";
}


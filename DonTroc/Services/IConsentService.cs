namespace DonTroc.Services;

/// <summary>
/// Abstraction du Google User Messaging Platform (UMP) pour le consentement RGPD/CMP.
/// Implémentation Android via Play Services UMP, no-op sur iOS (à venir si besoin).
/// </summary>
public interface IConsentService
{
    /// <summary>
    /// Demande la mise à jour du consentement et affiche le formulaire si nécessaire.
    /// À appeler AU DÉMARRAGE de l'app, AVANT MobileAds.Initialize().
    /// Méthode safe : ne lève jamais d'exception.
    /// </summary>
    /// <returns>true si on peut servir des pubs (consentement accordé ou non requis), false sinon.</returns>
    Task<bool> GatherConsentAsync();

    /// <summary>
    /// True si on peut faire des requêtes de pubs (consentement accordé OU géo non concernée).
    /// Vérifié systématiquement avant chaque LoadAd().
    /// </summary>
    bool CanRequestAds();

    /// <summary>
    /// True si l'utilisateur peut/doit avoir accès à un bouton "Modifier mes préférences"
    /// (obligatoire en EEE/UK, optionnel ailleurs).
    /// </summary>
    bool IsPrivacyOptionsRequired();

    /// <summary>
    /// Affiche le formulaire de modification du consentement.
    /// À appeler depuis un bouton "Confidentialité publicitaire" dans Profil/Paramètres.
    /// </summary>
    Task ShowPrivacyOptionsFormAsync();

    /// <summary>
    /// DEBUG ONLY : réinitialise le consentement pour retester le formulaire.
    /// </summary>
    void ResetConsent();
}


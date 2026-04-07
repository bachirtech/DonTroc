using System;
using System.Threading.Tasks;

namespace DonTroc.Services;

/// <summary>
/// Service pour gérer les achats in-app (suppression de publicités).
/// 
/// ══════════════════════════════════════════════════════════
/// DÉSACTIVÉ : Profil marchand Google Play non disponible au Maroc.
/// Ce stub conserve l'API publique pour ne rien casser dans le reste du code.
/// Réactiver quand un profil marchand sera configuré.
/// ══════════════════════════════════════════════════════════
/// </summary>
public class InAppBillingService
{
    public const string RemoveAdsProductId = "premium_remove_ads";

    /// <summary>Toujours false — achats désactivés</summary>
    public bool IsPermanentPremium
    {
        get => false;
        private set { /* no-op */ }
    }

    public event EventHandler<bool>? PremiumStatusChanged;

    public InAppBillingService() { }

    /// <summary>Retourne null — achats désactivés</summary>
    public Task<object?> GetProductInfoAsync() => Task.FromResult<object?>(null);

    /// <summary>Retourne false — achats désactivés</summary>
    public Task<bool> PurchaseRemoveAdsAsync()
    {
        System.Diagnostics.Debug.WriteLine("[IAB] ⚠️ Achats in-app désactivés (pas de profil marchand)");
        return Task.FromResult(false);
    }

    /// <summary>Retourne false — achats désactivés</summary>
    public Task<bool> RestorePurchasesAsync()
    {
        System.Diagnostics.Debug.WriteLine("[IAB] ⚠️ Restauration désactivée (pas de profil marchand)");
        return Task.FromResult(false);
    }

    /// <summary>No-op — achats désactivés</summary>
    public Task CheckAndRestorePurchaseAsync() => Task.CompletedTask;
}


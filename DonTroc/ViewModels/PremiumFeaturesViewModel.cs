using System;
using System.Threading.Tasks;
using DonTroc.Services;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.Generic;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour gérer les fonctionnalités premium via publicités récompensées
/// </summary>
public class PremiumFeaturesViewModel : BaseViewModel
{
    private readonly AdMobService _adMobService;
    private readonly AuthService _authService;
    private readonly InAppBillingService _billingService;

    // ── Achat permanent (in-app purchase) ──
    private bool _isPermanentPremium;
    public bool IsPermanentPremium
    {
        get => _isPermanentPremium;
        set => SetProperty(ref _isPermanentPremium, value);
    }

    private string _removeAdsPriceText = "Chargement...";
    /// <summary>Prix affiché pour la suppression des pubs (ex: "3,49 €")</summary>
    public string RemoveAdsPriceText
    {
        get => _removeAdsPriceText;
        set => SetProperty(ref _removeAdsPriceText, value);
    }

    // ── Statuts premium temporaires (rewarded ads) ──
    private bool _isAdFreeActive;
    public bool IsAdFreeActive
    {
        get => _isAdFreeActive;
        set => SetProperty(ref _isAdFreeActive, value);
    }

    private DateTime _adFreeUntil;
    public DateTime AdFreeUntil
    {
        get => _adFreeUntil;
        set => SetProperty(ref _adFreeUntil, value);
    }

    private int _boostCredits;
    public int BoostCredits
    {
        get => _boostCredits;
        set => SetProperty(ref _boostCredits, value);
    }

    // Commandes pour les fonctionnalités premium
    public ICommand WatchAdForAdFreeCommand { get; }
    public ICommand WatchAdForBoostCreditsCommand { get; }
    public ICommand WatchAdForStatsCommand { get; }
    public ICommand PurchaseRemoveAdsCommand { get; }
    public ICommand RestorePurchasesCommand { get; }

    public PremiumFeaturesViewModel(AdMobService adMobService, AuthService authService, InAppBillingService billingService)
    {
        _adMobService = adMobService;
        _authService = authService;
        _billingService = billingService;

        // Initialiser les commandes
        WatchAdForAdFreeCommand = new Command(async () => await WatchAdForAdFree());
        WatchAdForBoostCreditsCommand = new Command(async () => await WatchAdForBoostCredits());
        WatchAdForStatsCommand = new Command(async () => await WatchAdForStats());
        
        // DÉSACTIVÉ : Achats in-app non disponibles (pas de profil marchand au Maroc)
        // PurchaseRemoveAdsCommand = new Command(async () => await PurchaseRemoveAds());
        // RestorePurchasesCommand = new Command(async () => await RestorePurchases());
        PurchaseRemoveAdsCommand = new Command(() => { /* désactivé */ });
        RestorePurchasesCommand = new Command(() => { /* désactivé */ });

        // DÉSACTIVÉ : État premium toujours false
        // IsPermanentPremium = _billingService.IsPermanentPremium;
        // _billingService.PremiumStatusChanged += (_, isPremium) =>
        // {
        //     MainThread.BeginInvokeOnMainThread(() => IsPermanentPremium = isPremium);
        // };
        IsPermanentPremium = false;

        // Charger les données utilisateur (temporaires / rewarded ads)
        LoadUserPremiumStatus();

        // DÉSACTIVÉ : Pas de chargement de prix Store
        // _ = LoadProductPriceAsync();
        RemoveAdsPriceText = "Bientôt disponible";
    }

    /// <summary>
    /// Charge le prix du produit depuis le Store pour l'afficher dans l'UI
    /// DÉSACTIVÉ : Achats in-app non disponibles
    /// </summary>
    private Task LoadProductPriceAsync()
    {
        RemoveAdsPriceText = "Bientôt disponible";
        return Task.CompletedTask;
        /* ANCIEN CODE — Réactiver avec profil marchand
        try
        {
            var product = await _billingService.GetProductInfoAsync();
            if (product != null)
            {
                RemoveAdsPriceText = product.LocalizedPrice;
            }
            else
            {
                RemoveAdsPriceText = "3,49 €";
            }
        }
        catch
        {
            RemoveAdsPriceText = "3,49 €";
        }
        */
    }

    /// <summary>
    /// Lance l'achat de la suppression permanente des publicités
    /// DÉSACTIVÉ : Achats in-app non disponibles (pas de profil marchand au Maroc)
    /// </summary>
    private Task PurchaseRemoveAds()
    {
        return Task.CompletedTask;
        /* ANCIEN CODE — Réactiver avec profil marchand
        if (IsBusy) return;
        if (IsPermanentPremium)
        {
            await Shell.Current.DisplayAlert("Déjà Premium 👑",
                "Vous bénéficiez déjà de la suppression permanente des publicités.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var success = await _billingService.PurchaseRemoveAdsAsync();

            if (success)
            {
                IsPermanentPremium = true;
                IsAdFreeActive = true;

                await Shell.Current.DisplayAlert("Achat réussi ! 👑",
                    "Merci pour votre achat !\nLes publicités ont été supprimées définitivement.", "Génial !");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur",
                "Une erreur s'est produite lors de l'achat. Veuillez réessayer.", "OK");
            System.Diagnostics.Debug.WriteLine($"[Premium] Erreur achat: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
        */
    }

    /// <summary>
    /// Restaure les achats précédents (réinstallation / changement d'appareil)
    /// DÉSACTIVÉ : Achats in-app non disponibles
    /// </summary>
    private Task RestorePurchases()
    {
        return Task.CompletedTask;
        /* ANCIEN CODE — Réactiver avec profil marchand
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var restored = await _billingService.RestorePurchasesAsync();

            if (restored)
            {
                IsPermanentPremium = true;
                IsAdFreeActive = true;

                await Shell.Current.DisplayAlert("Achats restaurés ! 👑",
                    "Votre achat Premium a été restauré avec succès.\nLes publicités sont supprimées.", "Parfait !");
            }
            else
            {
                await Shell.Current.DisplayAlert("Aucun achat trouvé",
                    "Aucun achat Premium n'a été trouvé associé à votre compte Google Play.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur",
                "Impossible de restaurer les achats. Vérifiez votre connexion.", "OK");
            System.Diagnostics.Debug.WriteLine($"[Premium] Erreur restauration: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
        */
    }

    /// <summary>
    /// Regarde une publicité pour obtenir 2 heures sans publicité
    /// </summary>
    private async Task WatchAdForAdFree()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Vérifier si une publicité récompensée est disponible
            if (!_adMobService.IsRewardedAdReady())
            {
                await Shell.Current.DisplayAlert("Publicité non disponible", 
                    "Aucune publicité récompensée n'est disponible pour le moment. Veuillez réessayer plus tard.", "OK");
                return;
            }

            // Afficher la publicité récompensée
            var rewardEarned = await _adMobService.ShowRewardedAdAsync();

            if (rewardEarned)
            {
                // Accorder 2 heures sans publicité
                AdFreeUntil = DateTime.Now.AddHours(2);
                IsAdFreeActive = true;

                await Shell.Current.DisplayAlert("Récompense obtenue ! 🎉", 
                    "Vous bénéficiez maintenant de 2 heures sans publicité !", "Génial !");

                // Sauvegarder le statut
                SaveUserPremiumStatus();
            }
            else
            {
                await Shell.Current.DisplayAlert("Publicité non terminée", 
                    "Vous devez regarder la publicité entièrement pour obtenir votre récompense.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", 
                "Une erreur s'est produite lors de l'affichage de la publicité.", "OK");
            System.Diagnostics.Debug.WriteLine($"Erreur WatchAdForAdFree: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Regarde une publicité pour obtenir des crédits de boost
    /// </summary>
    private async Task WatchAdForBoostCredits()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            if (!_adMobService.IsRewardedAdReady())
            {
                await Shell.Current.DisplayAlert("Publicité non disponible", 
                    "Aucune publicité récompensée n'est disponible pour le moment.", "OK");
                return;
            }

            var rewardEarned = await _adMobService.ShowRewardedAdAsync();

            if (rewardEarned)
            {
                // Accorder 3 crédits de boost
                BoostCredits += 3;

                await Shell.Current.DisplayAlert("Crédits obtenus ! 🚀", 
                    "Vous avez reçu 3 crédits de boost pour mettre en avant vos annonces !", "Super !");

                SaveUserPremiumStatus();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", 
                "Une erreur s'est produite lors de l'affichage de la publicité.", "OK");
            System.Diagnostics.Debug.WriteLine($"Erreur WatchAdForBoostCredits: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Regarde une publicité pour débloquer les statistiques détaillées
    /// </summary>
    private async Task WatchAdForStats()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            if (!_adMobService.IsRewardedAdReady())
            {
                await Shell.Current.DisplayAlert("Publicité non disponible", 
                    "Aucune publicité récompensée n'est disponible pour le moment.", "OK");
                return;
            }

            var rewardEarned = await _adMobService.ShowRewardedAdAsync();

            if (rewardEarned)
            {
                await Shell.Current.DisplayAlert("Statistiques débloquées ! 📊", 
                    "Vous avez maintenant accès aux statistiques détaillées de vos annonces !", "Parfait !");

                // Ici vous pourriez implémenter l'accès aux statistiques détaillées
                // par exemple en sauvegardant un flag dans les préférences
                Preferences.Set("DetailedStatsUnlocked", true);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", 
                "Une erreur s'est produite lors de l'affichage de la publicité.", "OK");
            System.Diagnostics.Debug.WriteLine($"Erreur WatchAdForStats: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Charge le statut premium de l'utilisateur depuis les préférences
    /// </summary>
    private void LoadUserPremiumStatus()
    {
        try
        {
            // Charger le statut sans publicité
            var adFreeUntilTicks = Preferences.Get("AdFreeUntil", 0L);
            if (adFreeUntilTicks > 0)
            {
                AdFreeUntil = new DateTime(adFreeUntilTicks);
                IsAdFreeActive = AdFreeUntil > DateTime.Now;
            }

            // Charger les crédits de boost
            BoostCredits = Preferences.Get("BoostCredits", 0);

            // Vérifier si le statut sans publicité a expiré
            if (IsAdFreeActive && AdFreeUntil <= DateTime.Now)
            {
                IsAdFreeActive = false;
                SaveUserPremiumStatus();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement du statut premium: {ex.Message}");
        }
    }

    /// <summary>
    /// Sauvegarde le statut premium de l'utilisateur dans les préférences
    /// </summary>
    private void SaveUserPremiumStatus()
    {
        try
        {
            Preferences.Set("AdFreeUntil", AdFreeUntil.Ticks);
            Preferences.Set("BoostCredits", BoostCredits);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde du statut premium: {ex.Message}");
        }
    }

    /// <summary>
    /// Utilise un crédit de boost (appelé depuis d'autres ViewModels)
    /// </summary>
    public bool UseBoostCredit()
    {
        if (BoostCredits > 0)
        {
            BoostCredits--;
            SaveUserPremiumStatus();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Vérifie si l'utilisateur doit voir des publicités.
    /// Retourne false si Premium permanent OU si mode Ad-Free temporaire actif.
    /// </summary>
    public bool ShouldShowAds()
    {
        if (IsPermanentPremium) return false;
        return !IsAdFreeActive || AdFreeUntil <= DateTime.Now;
    }

    public bool CanUseBoostCredit() // Vérifie si l'utilisateur a des crédits de boost disponibles
    {
        return BoostCredits > 0;
    }
}

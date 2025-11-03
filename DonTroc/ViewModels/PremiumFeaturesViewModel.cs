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

    // Propriétés pour les statuts premium temporaires
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

    public PremiumFeaturesViewModel(AdMobService adMobService, AuthService authService)
    {
        _adMobService = adMobService;
        _authService = authService;

        // Initialiser les commandes
        WatchAdForAdFreeCommand = new Command(async () => await WatchAdForAdFree());
        WatchAdForBoostCreditsCommand = new Command(async () => await WatchAdForBoostCredits());
        WatchAdForStatsCommand = new Command(async () => await WatchAdForStats());

        // Charger les données utilisateur
        LoadUserPremiumStatus();
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
    /// Vérifie si l'utilisateur a le statut sans publicité actif
    /// </summary>
    public bool ShouldShowAds()
    {
        return !IsAdFreeActive || AdFreeUntil <= DateTime.Now;
    }

    public bool CanUseBoostCredit() // Vérifie si l'utilisateur a des crédits de boost disponibles
    {
        return BoostCredits > 0;
    }
}

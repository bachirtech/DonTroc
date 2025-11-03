using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DonTroc.Services
{
    /// <summary>
    /// Service principal pour la gestion des publicités AdMob
    /// Utilise l'injection de dépendances pour déléguer aux implémentations de plateforme
    /// </summary>
    public class AdMobService
    {
        private readonly IAdMobService _platformService;

        // IDs des unités publicitaires (Production - remplacer par vos vrais IDs)
        public const string RewardedAdUnitId = "ca-app-pub-5085236088670848/1650434769";
        public const string InterstitialAdUnitId = "ca-app-pub-5085236088670848/8273475447";
        public const string BannerAdUnitId = "ca-app-pub-5085236088670848/2349645674";

        public AdMobService(IAdMobService platformService)
        {
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            // L'initialisation se fait automatiquement via le constructeur du service de plateforme
            _platformService.Initialize();
        }

        /// <summary>
        /// Vérifie si une publicité récompensée est prête à être affichée
        /// </summary>
        public bool IsRewardedAdReady()
        {
            try
            {
                return _platformService?.IsRewardedAdReady() ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur IsRewardedAdReady: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Affiche une publicité récompensée
        /// </summary>
        public async Task<bool> ShowRewardedAdAsync()
        {
            try
            {
                if (_platformService == null) return false;
                return await _platformService.ShowRewardedAdAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ShowRewardedAdAsync: {ex.Message}");
                _platformService?.LogApiLimitation();
                return false;
            }
        }

        /// <summary>
        /// Vérifie si une publicité interstitielle est prête à être affichée
        /// </summary>
        public bool IsInterstitialAdReady()
        {
            try
            {
                return _platformService?.IsInterstitialAdReady() ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur IsInterstitialAdReady: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Affiche une publicité interstitielle
        /// </summary>
        public async Task ShowInterstitialAdAsync()
        {
            try
            {
                if (_platformService == null) return;
                await _platformService.ShowInterstitialAdAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ShowInterstitialAdAsync: {ex.Message}");
                _platformService?.LogApiLimitation();
            }
        }

        /// <summary>
        /// Charge manuellement les publicités
        /// </summary>
        public void LoadAds()
        {
            try
            {
                _platformService?.LoadRewardedAd();
                _platformService?.LoadInterstitialAd();
                System.Diagnostics.Debug.WriteLine("🔄 Chargement des publicités demandé");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur LoadAds: {ex.Message}");
                _platformService?.LogApiLimitation();
            }
        }
    }


}

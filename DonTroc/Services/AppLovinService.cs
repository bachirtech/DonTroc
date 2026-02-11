using System;
using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Interface pour le service AppLovin MAX multiplateforme
    /// </summary>
    public interface IAppLovinService
    {
        /// <summary>
        /// Initialise le SDK AppLovin MAX
        /// </summary>
        void Initialize();

        /// <summary>
        /// Vérifie si une publicité interstitielle est prête
        /// </summary>
        bool IsInterstitialReady();

        /// <summary>
        /// Affiche une publicité interstitielle
        /// </summary>
        Task ShowInterstitialAsync();

        /// <summary>
        /// Vérifie si une publicité récompensée est prête
        /// </summary>
        bool IsRewardedAdReady();

        /// <summary>
        /// Affiche une publicité récompensée
        /// </summary>
        /// <returns>True si l'utilisateur a gagné la récompense</returns>
        Task<bool> ShowRewardedAdAsync();

        /// <summary>
        /// Charge les publicités (interstitiel et rewarded)
        /// </summary>
        void LoadAds();
    }

    /// <summary>
    /// Service principal AppLovin MAX pour la gestion des publicités
    /// Utilise la médiation pour combiner plusieurs réseaux et maximiser les revenus
    /// </summary>
    public class AppLovinService
    {
        private readonly IAppLovinService? _platformService;
        private int _navigationCount;
        private const int InterstitialFrequency = 3;

        public AppLovinService(IAppLovinService? platformService = null)
        {
            _platformService = platformService;
            
            if (!AppLovinConfiguration.APPLOVIN_ENABLED)
            {
                System.Diagnostics.Debug.WriteLine(AppLovinConfiguration.GetStatusMessage());
                return;
            }

            _platformService?.Initialize();
        }

        /// <summary>
        /// Vérifie si une publicité interstitielle est prête
        /// </summary>
        public bool IsInterstitialReady()
        {
            if (!AppLovinConfiguration.APPLOVIN_ENABLED) return false;
            return _platformService?.IsInterstitialReady() ?? false;
        }

        /// <summary>
        /// Affiche une publicité interstitielle
        /// </summary>
        public async Task ShowInterstitialAsync()
        {
            if (!AppLovinConfiguration.APPLOVIN_ENABLED) return;
            
            try
            {
                if (_platformService != null)
                {
                    await _platformService.ShowInterstitialAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AppLovin Interstitial error: {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si une publicité récompensée est prête
        /// </summary>
        public bool IsRewardedAdReady()
        {
            if (!AppLovinConfiguration.APPLOVIN_ENABLED) return false;
            return _platformService?.IsRewardedAdReady() ?? false;
        }

        /// <summary>
        /// Affiche une publicité récompensée
        /// </summary>
        public async Task<bool> ShowRewardedAdAsync()
        {
            if (!AppLovinConfiguration.APPLOVIN_ENABLED) return false;
            
            try
            {
                if (_platformService != null)
                {
                    return await _platformService.ShowRewardedAdAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AppLovin Rewarded error: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Charge manuellement les publicités
        /// </summary>
        public void LoadAds()
        {
            if (!AppLovinConfiguration.APPLOVIN_ENABLED) return;
            _platformService?.LoadAds();
        }

        /// <summary>
        /// Tente d'afficher un interstitiel lors d'une navigation
        /// </summary>
        public async Task TryShowInterstitialOnNavigationAsync(string pageName = "")
        {
            if (!AppLovinConfiguration.APPLOVIN_ENABLED) return;
            
            try
            {
                _navigationCount++;
                
                if (_navigationCount >= InterstitialFrequency && IsInterstitialReady())
                {
                    _navigationCount = 0;
                    await ShowInterstitialAsync();
                }
            }
            catch (Exception)
            {
                // Ignorer silencieusement
            }
        }
    }
}

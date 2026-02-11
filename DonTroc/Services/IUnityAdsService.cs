using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Interface pour les services Unity Ads
    /// Remplacement temporaire pour AdMob pendant la suspension du compte
    /// </summary>
    public interface IUnityAdsService
    {
        /// <summary>
        /// Initialise le SDK Unity Ads
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Vérifie si une publicité récompensée est prête
        /// </summary>
        bool IsRewardedAdReady();
        
        /// <summary>
        /// Vérifie si une publicité interstitielle est prête
        /// </summary>
        bool IsInterstitialAdReady();
        
        /// <summary>
        /// Vérifie si une bannière est prête
        /// </summary>
        bool IsBannerReady();
        
        /// <summary>
        /// Affiche une publicité récompensée et attend la récompense
        /// </summary>
        /// <returns>True si l'utilisateur a regardé la pub jusqu'au bout</returns>
        Task<bool> ShowRewardedAdAsync();
        
        /// <summary>
        /// Affiche une publicité interstitielle
        /// </summary>
        Task ShowInterstitialAdAsync();
        
        /// <summary>
        /// Charge une publicité récompensée
        /// </summary>
        void LoadRewardedAd();
        
        /// <summary>
        /// Charge une publicité interstitielle
        /// </summary>
        void LoadInterstitialAd();
        
        /// <summary>
        /// Affiche une bannière en bas de l'écran
        /// </summary>
        void ShowBanner();
        
        /// <summary>
        /// Cache la bannière
        /// </summary>
        void HideBanner();
        
        /// <summary>
        /// Vérifie si le SDK est initialisé
        /// </summary>
        bool IsInitialized { get; }
    }
}

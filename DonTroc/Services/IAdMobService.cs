using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Interface pour les services AdMob spécifiques à chaque plateforme
    /// </summary>
    public interface IAdMobService
    {
        void Initialize();
        bool IsRewardedAdReady();
        bool IsInterstitialAdReady();
        Task<bool> ShowRewardedAdAsync();
        Task ShowInterstitialAdAsync();
        void LoadRewardedAd();
        void LoadInterstitialAd();
        void LogApiLimitation();
    }
}

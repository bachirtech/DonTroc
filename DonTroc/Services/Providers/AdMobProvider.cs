#if ANDROID
using System;
using System.Threading.Tasks;
using DonTroc.Platforms.Android;

namespace DonTroc.Services.Providers
{
    /// <summary>
    /// Provider AdMob qui wrappe le service natif existant
    /// Utilisé par MediatedAdsService pour la médiation multi-réseau
    /// </summary>
    public class AdMobProvider : IAdsProvider
    {
        private readonly AdMobNativeService _nativeService;
        
        public string Name => "AdMob";
        public int Priority => 1; // Priorité la plus haute (réseau principal)
        
        public bool IsInitialized { get; private set; }
        public bool IsInterstitialReady => _nativeService.IsInterstitialAdReady();
        public bool IsRewardedReady => _nativeService.IsRewardedAdReady();

        public AdMobProvider()
        {
            _nativeService = new AdMobNativeService();
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            
            _nativeService.Initialize();
            IsInitialized = true;
            
            System.Diagnostics.Debug.WriteLine("✅ AdMobProvider initialisé");
        }

        public void PreloadInterstitial()
        {
            _nativeService.LoadInterstitialAd();
        }

        public void PreloadRewarded()
        {
            _nativeService.LoadRewardedAd();
        }

        public async Task ShowInterstitialAsync()
        {
            await _nativeService.ShowInterstitialAdAsync();
        }

        public async Task<bool> ShowRewardedAsync()
        {
            return await _nativeService.ShowRewardedAdAsync();
        }
    }
}
#endif

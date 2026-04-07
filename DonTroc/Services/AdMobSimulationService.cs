using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace DonTroc.Services
{
    /// <summary>
    /// Implémentation de simulation pour IAdMobService sur les plateformes non-Android.
    /// Fournit un comportement cohérent pour le développement et les tests.
    /// </summary>
    public class AdMobSimulationService : IAdMobService
    {
        private bool _rewardedAdLoaded;
        private bool _interstitialAdLoaded;

        public void Initialize()
        {
            LoadInterstitialAd();
            LoadRewardedAd();
        }

        public bool IsInterstitialAdReady() => _interstitialAdLoaded;
        public bool IsRewardedAdReady() => _rewardedAdLoaded;

        public void LoadInterstitialAd()
        {
            if (_interstitialAdLoaded) return;
            _interstitialAdLoaded = true;
        }

        public void LoadRewardedAd()
        {
            if (_rewardedAdLoaded) return;
            _rewardedAdLoaded = true;
        }

        public async Task ShowInterstitialAdAsync()
        {
            if (!IsInterstitialAdReady()) return;

            try
            {
                await Shell.Current.DisplayAlert("📺 Publicité (Simulation)", 
                    "Ceci est une publicité interstitielle simulée.", "Fermer");
                _interstitialAdLoaded = false;
                LoadInterstitialAd();
            }
            catch { }
        }

        public async Task<bool> ShowRewardedAdAsync()
        {
            if (!IsRewardedAdReady()) return false;

            try
            {
                bool userWatched = await Shell.Current.DisplayAlert("🎬 Publicité Récompensée (Simulation)", 
                    "Regardez cette publicité simulée pour obtenir une récompense.", "Regarder", "Passer");

                if (userWatched)
                {
                    await Task.Delay(1000);
                    _rewardedAdLoaded = false;
                    LoadRewardedAd();
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public void LogApiLimitation() { }
    }
}

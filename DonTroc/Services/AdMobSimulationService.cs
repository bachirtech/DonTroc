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
            System.Diagnostics.Debug.WriteLine("✅ Service AdMob (simulation) initialisé");
            LoadInterstitialAd();
            LoadRewardedAd();
        }

        public bool IsInterstitialAdReady() => _interstitialAdLoaded;

        public bool IsRewardedAdReady() => _rewardedAdLoaded;

        public void LoadInterstitialAd()
        {
            if (_interstitialAdLoaded) return;
            System.Diagnostics.Debug.WriteLine("🔄 Chargement publicité interstitielle (simulation)...");
            _interstitialAdLoaded = true;
            System.Diagnostics.Debug.WriteLine("✅ Publicité interstitielle chargée (simulation)");
        }

        public void LoadRewardedAd()
        {
            if (_rewardedAdLoaded) return;
            System.Diagnostics.Debug.WriteLine("🔄 Chargement publicité récompensée (simulation)...");
            _rewardedAdLoaded = true;
            System.Diagnostics.Debug.WriteLine("✅ Publicité récompensée chargée (simulation)");
        }

        public async Task ShowInterstitialAdAsync()
        {
            if (!IsInterstitialAdReady())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Pub interstitielle pas prête (simulation)");
                return;
            }

            try
            {
                await Shell.Current.DisplayAlert("📺 Publicité (Simulation)", 
                    "Ceci est une publicité interstitielle simulée.", "Fermer");
                _interstitialAdLoaded = false;
                LoadInterstitialAd();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur simulation pub interstitielle: {ex.Message}");
            }
        }

        public async Task<bool> ShowRewardedAdAsync()
        {
            if (!IsRewardedAdReady())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Pub récompensée pas prête (simulation)");
                return false;
            }

            try
            {
                bool userWatched = await Shell.Current.DisplayAlert("🎬 Publicité Récompensée (Simulation)", 
                    "Regardez cette publicité simulée pour obtenir une récompense.", "Regarder", "Passer");

                if (userWatched)
                {
                    await Task.Delay(1000); // Simule le temps de visionnage
                    System.Diagnostics.Debug.WriteLine("🎉 Récompense accordée (simulation)");
                    _rewardedAdLoaded = false;
                    LoadRewardedAd();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur simulation pub récompensée: {ex.Message}");
                return false;
            }
        }

        public void LogApiLimitation()
        {
            System.Diagnostics.Debug.WriteLine("ℹ️ Limitation API (simulation) - Aucune action requise");
        }
    }
}

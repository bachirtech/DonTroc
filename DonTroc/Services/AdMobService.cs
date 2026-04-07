using System;
using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Service principal pour la gestion des publicités AdMob.
    /// 
    /// ══════════════════════════════════════════════════════════════
    /// POLITIQUE ANTI-SUSPENSION — Respecter les règles Google AdMob :
    /// 
    /// 1. Cooldown minimum de 120 secondes entre deux interstitiels
    /// 2. Maximum 5 interstitiels par session utilisateur
    /// 3. Fréquence : 1 interstitiel toutes les 2 navigations minimum
    /// 4. Pas d'interstitiel au lancement de l'app (attendre 1 navigation)
    /// 5. Pas d'interstitiel sur les pages de contenu critique (Chat, Création)
    /// 6. Rewarded : uniquement sur action volontaire de l'utilisateur
    /// 7. Respect du mode Ad-Free (PremiumFeatures)
    /// ══════════════════════════════════════════════════════════════
    /// </summary>
    public class AdMobService
    {
        private readonly IAdMobService _platformService;

        // IDs de production AdMob
        public const string RewardedAdUnitId = "ca-app-pub-5085236088670848/1650434769";
        public const string InterstitialAdUnitId = "ca-app-pub-5085236088670848/8273475447";
        public const string BannerAdUnitId = "ca-app-pub-5085236088670848/1004542862";

        // ── Protection anti-suspension : Limites interstitiels ──
        private int _navigationCount;
        private int _sessionInterstitialCount;
        private DateTime _lastInterstitialTime = DateTime.MinValue;

        /// <summary>Nombre minimum de navigations entre deux interstitiels (garde-fou en plus du cooldown)</summary>
        private const int InterstitialFrequency = 2;

        /// <summary>Délai minimum (secondes) entre deux interstitiels — Google recommande ≥ 60s, on met 120s</summary>
        private const int MinInterstitialCooldownSeconds = 120;

        /// <summary>Maximum d'interstitiels par session pour ne pas saturer l'utilisateur</summary>
        private const int MaxInterstitialsPerSession = 5;

        // ── Protection anti-suspension : Limites rewarded ──
        private DateTime _lastRewardedTime = DateTime.MinValue;

        /// <summary>Délai minimum (secondes) entre deux rewarded — éviter le spam même volontaire</summary>
        private const int MinRewardedCooldownSeconds = 30;

        public AdMobService(IAdMobService platformService)
        {
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            
            if (AdMobConfiguration.ADS_ENABLED)
            {
                _platformService.Initialize();
            }
        }

        /// <summary>
        /// Vérifie si l'utilisateur a un mode sans pub actif
        /// (achat permanent OU récompense temporaire via rewarded ad)
        /// </summary>
        private bool IsAdFreeActive()
        {
            try
            {
                // 1. Vérifier l'achat permanent (in-app purchase)
                if (Preferences.Get("PremiumPurchased", false))
                {
                    return true;
                }

                // 2. Vérifier la récompense temporaire (rewarded ad → 2h sans pub)
                var adFreeUntilTicks = Preferences.Get("AdFreeUntil", 0L);
                if (adFreeUntilTicks > 0)
                {
                    var adFreeUntil = new DateTime(adFreeUntilTicks);
                    return DateTime.Now < adFreeUntil;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie si une publicité récompensée est prête
        /// </summary>
        public bool IsRewardedAdReady()
        {
            try
            {
                if (!AdMobConfiguration.ADS_ENABLED) return false;
                return _platformService?.IsRewardedAdReady() ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Affiche une publicité récompensée (action volontaire de l'utilisateur uniquement).
        /// Cooldown de 30s entre deux affichages pour éviter le spam.
        /// </summary>
        public async Task<bool> ShowRewardedAdAsync()
        {
            try
            {
                if (!AdMobConfiguration.ADS_ENABLED || _platformService == null) return false;

                // Cooldown entre deux rewarded
                var elapsed = (DateTime.Now - _lastRewardedTime).TotalSeconds;
                if (elapsed < MinRewardedCooldownSeconds)
                {
                    return false;
                }

                var result = await _platformService.ShowRewardedAdAsync();
                if (result)
                {
                    _lastRewardedTime = DateTime.Now;
                }
                return result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie si une publicité interstitielle est prête
        /// </summary>
        public bool IsInterstitialAdReady()
        {
            try
            {
                if (!AdMobConfiguration.ADS_ENABLED) return false;
                return _platformService?.IsInterstitialAdReady() ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Affiche une publicité interstitielle (usage interne uniquement via TryShowInterstitialOnNavigationAsync).
        /// </summary>
        private async Task ShowInterstitialAdInternalAsync()
        {
            try
            {
                if (!AdMobConfiguration.ADS_ENABLED || _platformService == null) return;
                await _platformService.ShowInterstitialAdAsync();
                _lastInterstitialTime = DateTime.Now;
                _sessionInterstitialCount++;
            }
            catch
            {
                // Ne pas bloquer la navigation
            }
        }

        /// <summary>
        /// Charge manuellement les publicités
        /// </summary>
        public void LoadAds()
        {
            try
            {
                if (!AdMobConfiguration.ADS_ENABLED) return;
                _platformService?.LoadRewardedAd();
                _platformService?.LoadInterstitialAd();
            }
            catch
            {
                // Ignorer
            }
        }

        /// <summary>
        /// Charge une publicité récompensée
        /// </summary>
        public void LoadRewardedAd()
        {
            try
            {
                if (!AdMobConfiguration.ADS_ENABLED) return;
                _platformService?.LoadRewardedAd();
            }
            catch
            {
                // Ignorer
            }
        }

        /// <summary>
        /// Tente d'afficher un interstitiel lors d'une navigation.
        /// Respecte toutes les limites anti-suspension :
        /// - Cooldown de 120s minimum
        /// - Maximum 5 par session
        /// - Toutes les 2 navigations minimum
        /// - Pas si l'utilisateur a le mode Ad-Free
        /// </summary>
        public async Task TryShowInterstitialOnNavigationAsync(string pageName = "")
        {
            try
            {
                // Ne rien faire si les pubs sont désactivées
                if (!AdMobConfiguration.ADS_ENABLED) return;

                // Respecter le mode Ad-Free
                if (IsAdFreeActive()) return;

                _navigationCount++;

                // Vérifier la fréquence de navigation
                if (_navigationCount < InterstitialFrequency) return;

                // Vérifier le limite par session
                if (_sessionInterstitialCount >= MaxInterstitialsPerSession) return;

                // Vérifier le cooldown temporel (120 secondes minimum)
                var elapsed = (DateTime.Now - _lastInterstitialTime).TotalSeconds;
                if (elapsed < MinInterstitialCooldownSeconds) return;

                // Vérifier si une pub est prête
                if (!IsInterstitialAdReady()) return;

                // Toutes les conditions sont remplies — afficher
                _navigationCount = 0;
                await ShowInterstitialAdInternalAsync();
            }
            catch
            {
                // Ne jamais bloquer la navigation
            }
        }
    }

}

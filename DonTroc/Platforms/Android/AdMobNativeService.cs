using System;
using System.Threading.Tasks;
using Android.Content;
using PreserveAttribute = global::Android.Runtime.PreserveAttribute;
using Google.Android.Gms.Ads;
using Google.Android.Gms.Ads.Interstitial;
using Google.Android.Gms.Ads.Rewarded;
using DonTroc.Services;

namespace DonTroc.Platforms.Android
{
    /// <summary>
    /// Service natif Android pour gérer les publicités AdMob (récompensées et interstitielles)
    /// </summary>
    public class AdMobNativeService : Java.Lang.Object, IAdMobService
    {
        private const string RewardedAdUnitId = "ca-app-pub-5085236088670848/1650434769";
        private const string InterstitialAdUnitId = "ca-app-pub-5085236088670848/8273475447";
        private InterstitialAd? _interstitialAd;
        private bool _isInitialized;
        private RewardedAd? _rewardedAd;
        private TaskCompletionSource<bool>? _rewardedAdTcs;
        
        // Backoff exponentiel pour les retries (anti-suspension)
        private int _rewardedRetryCount;
        private int _interstitialRetryCount;
        private const int MaxLoadRetries = 3;

        /// <summary>
        /// Initialise le SDK AdMob de manière sécurisée
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            if (!AdMobConfiguration.ADS_ENABLED) return;

            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    var context = GetAndroidContext();
                    if (context == null) return;

                    MobileAds.Initialize(context);
                    _isInitialized = true;

                    System.Diagnostics.Debug.WriteLine("✅ SDK AdMob initialisé");

                    // Précharger les publicités
                    LoadRewardedAd();
                    LoadInterstitialAd();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur init: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge une publicité récompensée
        /// </summary>
        public void LoadRewardedAd()
        {
            // ⚠️ Ne pas charger si suspendu
            if (!AdMobConfiguration.ADS_ENABLED || !_isInitialized) return;

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = GetAndroidContext();
                    if (context == null) return;

                    var adRequest = new AdRequest.Builder().Build();
                    // Utiliser le callback simplifié
                    RewardedAd.Load(context, RewardedAdUnitId, adRequest, new RewardedAdLoadCallbackWrapper(
                        onLoaded: ad => OnRewardedAdLoaded(ad),
                        onFailed: error => OnRewardedAdFailedToLoad(error)
                    ));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur chargement rewarded: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Charge une publicité interstitielle
        /// </summary>
        public void LoadInterstitialAd()
        {
            // ⚠️ Ne pas charger si suspendu
            if (!AdMobConfiguration.ADS_ENABLED || !_isInitialized) return;

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = GetAndroidContext();
                    if (context == null) return;

                    var adRequest = new AdRequest.Builder().Build();
                    // Utiliser le callback simplifié
                    InterstitialAd.Load(context, InterstitialAdUnitId, adRequest, new InterstitialAdLoadCallbackWrapper(
                        onLoaded: ad => OnInterstitialAdLoaded(ad),
                        onFailed: error => OnInterstitialAdFailedToLoad(error)
                    ));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur chargement interstitiel: {ex.Message}");
                }
            });
        }

        public bool IsRewardedAdReady() => _rewardedAd != null;

        public bool IsInterstitialAdReady() => _interstitialAd != null;

        public async Task<bool> ShowRewardedAdAsync()
        {
            if (!IsRewardedAdReady())
            {
                LoadRewardedAd();
                return false;
            }

            _rewardedAdTcs = new TaskCompletionSource<bool>();

            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    var activity =
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as
                            AndroidX.AppCompat.App.AppCompatActivity;
                    if (activity == null)
                    {
                        _rewardedAdTcs.TrySetResult(false);
                        return;
                    }

                    // Configurer les callbacks avant l'affichage
                    var contentCallback = new FullScreenContentCallbackImpl(this, true);
                    _rewardedAd!.FullScreenContentCallback = contentCallback;

                    // Afficher la publicité avec callback de récompense
                    var rewardCallback = new UserEarnedRewardListenerImpl(this);
                    _rewardedAd.Show(activity, rewardCallback);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur affichage rewarded: {ex.Message}");
                    _rewardedAdTcs.TrySetResult(false);
                }
            });

            return await _rewardedAdTcs.Task;
        }

        public async Task ShowInterstitialAdAsync()
        {
            if (!IsInterstitialAdReady())
            {
                LoadInterstitialAd();
                return;
            }

            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    var activity =
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as
                            AndroidX.AppCompat.App.AppCompatActivity;
                    if (activity == null) return;

                    // Configurer les callbacks avant l'affichage
                    var contentCallback = new FullScreenContentCallbackImpl(this, false);
                    _interstitialAd!.FullScreenContentCallback = contentCallback;

                    _interstitialAd.Show(activity);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur affichage interstitiel: {ex.Message}");
                }
            });
        }

        public void LogApiLimitation() { }

        /// <summary>
        /// Récupère le contexte Android de manière fiable
        /// </summary>
        private static Context? GetAndroidContext()
        {
            try
            {
                // Méthode 1: Utiliser Platform.CurrentActivity
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity != null)
                    return activity;

                // Méthode 2: Récupérer via le contexte application
                var context = global::Android.App.Application.Context;
                if (context != null)
                    return context;

                // Méthode 3: Essayer de récupérer via le MainPage (fallback)
                var mainContext = Microsoft.Maui.Controls.Application.Current?.MainPage?.Handler?.MauiContext?.Context;
                return mainContext;
            }
            catch
            {
                return null;
            }
        }

        // Méthodes internes pour les callbacks
        internal void OnRewardedAdLoaded(RewardedAd ad)
        {
            _rewardedAd = ad;
            _rewardedRetryCount = 0;
        }

        internal void OnRewardedAdFailedToLoad(LoadAdError error)
        {
            _rewardedAd = null;
            _rewardedRetryCount++;
            if (_rewardedRetryCount <= MaxLoadRetries)
            {
                // Backoff exponentiel : 60s, 120s, 240s
                var delay = 60000 * (int)Math.Pow(2, _rewardedRetryCount - 1);
                Task.Delay(delay).ContinueWith(_ => LoadRewardedAd());
            }
        }

        internal void OnInterstitialAdLoaded(InterstitialAd ad)
        {
            _interstitialAd = ad;
            _interstitialRetryCount = 0;
        }

        internal void OnInterstitialAdFailedToLoad(LoadAdError error)
        {
            _interstitialAd = null;
            _interstitialRetryCount++;
            if (_interstitialRetryCount <= MaxLoadRetries)
            {
                // Backoff exponentiel : 60s, 120s, 240s
                var delay = 60000 * (int)Math.Pow(2, _interstitialRetryCount - 1);
                Task.Delay(delay).ContinueWith(_ => LoadInterstitialAd());
            }
        }

        internal void OnAdDismissed(bool isRewarded)
        {
            if (isRewarded)
            {
                _rewardedAdTcs?.TrySetResult(false); // Pas de récompense si fermée sans visionnage complet
                _rewardedAd = null;
                LoadRewardedAd();
            }
            else
            {
                _interstitialAd = null;
                LoadInterstitialAd();
            }
        }

        internal void OnAdFailedToShow(AdError error, bool isRewarded)
        {
            if (isRewarded)
            {
                _rewardedAdTcs?.TrySetResult(false);
                _rewardedAd = null;
                LoadRewardedAd();
            }
            else
            {
                _interstitialAd = null;
                LoadInterstitialAd();
            }
        }

        internal void OnUserEarnedReward(IRewardItem reward)
        {
            _rewardedAdTcs?.TrySetResult(true);
        }
    }

    #region Callbacks

    /// <summary>
    /// Callback pour le chargement des publicités récompensées
    /// Utilise l'annotation Export pour résoudre le conflit de signature Java
    /// </summary>
    [Preserve(AllMembers = true)]
    internal class RewardedAdLoadCallbackWrapper : RewardedAdLoadCallback
    {
        private readonly Action<LoadAdError> _onFailed;
        private readonly Action<RewardedAd> _onLoaded;

        public RewardedAdLoadCallbackWrapper(Action<RewardedAd> onLoaded, Action<LoadAdError> onFailed)
        {
            _onLoaded = onLoaded;
            _onFailed = onFailed;
        }

        // Utiliser la nouvelle signature avec le type spécifique via Export
        [Java.Interop.Export("onAdLoaded")]
        public void OnRewardedAdLoaded(RewardedAd ad) => _onLoaded?.Invoke(ad);

        public override void OnAdFailedToLoad(LoadAdError error) => _onFailed?.Invoke(error);
    }

    /// <summary>
    /// Callback pour le chargement des publicités interstitielles
    /// Utilise l'annotation Export pour résoudre le conflit de signature Java
    /// </summary>
    [Preserve(AllMembers = true)]
    internal class InterstitialAdLoadCallbackWrapper : InterstitialAdLoadCallback
    {
        private readonly Action<LoadAdError> _onFailed;
        private readonly Action<InterstitialAd> _onLoaded;

        public InterstitialAdLoadCallbackWrapper(Action<InterstitialAd> onLoaded, Action<LoadAdError> onFailed)
        {
            _onLoaded = onLoaded;
            _onFailed = onFailed;
        }

        // Utiliser la nouvelle signature avec le type spécifique via Export
        [Java.Interop.Export("onAdLoaded")]
        public void OnInterstitialAdLoaded(InterstitialAd ad) => _onLoaded?.Invoke(ad);

        public override void OnAdFailedToLoad(LoadAdError error) => _onFailed?.Invoke(error);
    }

    /// <summary>
    /// Callback pour la gestion du contenu plein écran
    /// </summary>
    [Preserve(AllMembers = true)]
    internal class FullScreenContentCallbackImpl : FullScreenContentCallback
    {
        private readonly bool _isRewarded;
        private readonly AdMobNativeService _service;

        public FullScreenContentCallbackImpl(AdMobNativeService service, bool isRewarded)
        {
            _service = service;
            _isRewarded = isRewarded;
        }

        public override void OnAdDismissedFullScreenContent() => _service.OnAdDismissed(_isRewarded);
        public override void OnAdFailedToShowFullScreenContent(AdError error) => _service.OnAdFailedToShow(error, _isRewarded);
        public override void OnAdShowedFullScreenContent() { }
    }

    /// <summary>
    /// Callback pour la gestion des récompenses
    /// </summary>
    [Preserve(AllMembers = true)]
    internal class UserEarnedRewardListenerImpl : Java.Lang.Object, IOnUserEarnedRewardListener
    {
        private readonly AdMobNativeService _service;

        public UserEarnedRewardListenerImpl(AdMobNativeService service) => _service = service;

        public void OnUserEarnedReward(IRewardItem reward) => _service.OnUserEarnedReward(reward);
    }

    #endregion
}

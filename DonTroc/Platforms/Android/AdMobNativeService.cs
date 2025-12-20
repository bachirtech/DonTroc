using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Ads;
using Android.Gms.Ads.Interstitial;
using Android.Gms.Ads.Rewarded;
using DonTroc.Services;

namespace DonTroc.Platforms.Android
{
    /// <summary>
    /// Service natif Android pour gérer les publicités AdMob (récompensées et interstitielles)
    /// Compatible avec AdMob SDK v120.4.0
    /// </summary>
    public class AdMobNativeService : Java.Lang.Object, IAdMobService
    {
        // IDs des publicités - Utilisation des IDs de test Google
        private const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        private const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        private InterstitialAd? _interstitialAd;
        private bool _isInitialized;
        private RewardedAd? _rewardedAd;
        private TaskCompletionSource<bool>? _rewardedAdTcs;

        /// <summary>
        /// Initialise le SDK AdMob de manière sécurisée
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    var context = GetAndroidContext();

                    if (context == null)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Impossible d'obtenir le contexte Android");
                        return;
                    }

                    // Initialisation simple et robuste du SDK AdMob
                    MobileAds.Initialize(context);
                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("✅ SDK AdMob initialisé avec succès");

                    // Précharger les publicités
                    LoadRewardedAd();
                    LoadInterstitialAd();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur initialisation AdMob: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge une publicité récompensée
        /// </summary>
        public void LoadRewardedAd()
        {
            if (!_isInitialized) return;

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = GetAndroidContext();

                    if (context == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "❌ Contexte Android non disponible pour charger la pub récompensée");
                        return;
                    }

                    var adRequest = new AdRequest.Builder().Build();

                    // Utiliser le callback simplifié
                    RewardedAd.Load(context, RewardedAdUnitId, adRequest, new RewardedAdLoadCallbackWrapper(
                        onLoaded: ad => OnRewardedAdLoaded(ad),
                        onFailed: error => OnRewardedAdFailedToLoad(error)
                    ));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Exception chargement pub récompensée: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Charge une publicité interstitielle
        /// </summary>
        public void LoadInterstitialAd()
        {
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ LoadInterstitialAd: SDK pas encore initialisé");
                return;
            }

            System.Diagnostics.Debug.WriteLine("🔄 LoadInterstitialAd: Début du chargement...");

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = GetAndroidContext();

                    if (context == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "❌ Contexte Android non disponible pour charger la pub interstitielle");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"🎯 Chargement interstitiel avec ID: {InterstitialAdUnitId}");

                    var adRequest = new AdRequest.Builder().Build();

                    // Utiliser le callback simplifié
                    InterstitialAd.Load(context, InterstitialAdUnitId, adRequest, new InterstitialAdLoadCallbackWrapper(
                        onLoaded: ad => OnInterstitialAdLoaded(ad),
                        onFailed: error => OnInterstitialAdFailedToLoad(error)
                    ));

                    System.Diagnostics.Debug.WriteLine("📤 Requête interstitiel envoyée au serveur AdMob");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Exception chargement pub interstitielle: {ex.Message}");
                }
            });
        }

        public bool IsRewardedAdReady() => _rewardedAd != null;

        public bool IsInterstitialAdReady() => _interstitialAd != null;

        public async Task<bool> ShowRewardedAdAsync()
        {
            if (!IsRewardedAdReady())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Publicité récompensée non disponible");
                LoadRewardedAd(); // Tenter de recharger
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
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur affichage pub récompensée: {ex.Message}");
                    _rewardedAdTcs.TrySetResult(false);
                }
            });

            return await _rewardedAdTcs.Task;
        }

        public async Task ShowInterstitialAdAsync()
        {
            if (!IsInterstitialAdReady())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Publicité interstitielle non disponible");
                LoadInterstitialAd(); // Tenter de recharger
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
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur affichage pub interstitielle: {ex.Message}");
                }
            });
        }

        public void LogApiLimitation()
        {
            System.Diagnostics.Debug.WriteLine("ℹ️ Limitation API AdMob respectée");
        }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur récupération contexte Android: {ex.Message}");
                return null;
            }
        }

        // Méthodes internes pour les callbacks
        internal void OnRewardedAdLoaded(RewardedAd ad)
        {
            _rewardedAd = ad;
            System.Diagnostics.Debug.WriteLine("✅ Publicité récompensée chargée");
        }

        internal void OnRewardedAdFailedToLoad(LoadAdError error)
        {
            _rewardedAd = null;
            System.Diagnostics.Debug.WriteLine($"❌ Échec chargement pub récompensée: {error.Message}");
            // Retry après 30 secondes
            Task.Delay(30000).ContinueWith(_ => LoadRewardedAd());
        }

        internal void OnInterstitialAdLoaded(InterstitialAd ad)
        {
            _interstitialAd = ad;
            System.Diagnostics.Debug.WriteLine("✅ Publicité interstitielle chargée");
        }

        internal void OnInterstitialAdFailedToLoad(LoadAdError error)
        {
            _interstitialAd = null;
            System.Diagnostics.Debug.WriteLine($"❌ Échec chargement pub interstitielle: {error.Message}");
            // Retry après 30 secondes
            Task.Delay(30000).ContinueWith(_ => LoadInterstitialAd());
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
            System.Diagnostics.Debug.WriteLine($"❌ Échec affichage pub: {error.Message}");

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
            System.Diagnostics.Debug.WriteLine($"🎉 Récompense accordée: {reward.Amount} {reward.Type}");
            _rewardedAdTcs?.TrySetResult(true);
        }
    }

    #region Callbacks Implementation

    /// <summary>
    /// Callback pour le chargement des publicités récompensées
    /// Utilise l'annotation Export pour résoudre le conflit de signature Java
    /// </summary>
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
        public void OnRewardedAdLoaded(RewardedAd ad)
        {
            System.Diagnostics.Debug.WriteLine("📢 RewardedAdLoadCallback.OnAdLoaded appelé");
            _onLoaded?.Invoke(ad);
        }

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            System.Diagnostics.Debug.WriteLine($"📢 RewardedAdLoadCallback.OnAdFailedToLoad: {error?.Message}");
            _onFailed?.Invoke(error);
        }
    }

    /// <summary>
    /// Callback pour le chargement des publicités interstitielles
    /// Utilise l'annotation Export pour résoudre le conflit de signature Java
    /// </summary>
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
        public void OnInterstitialAdLoaded(InterstitialAd ad)
        {
            System.Diagnostics.Debug.WriteLine("📢 InterstitialAdLoadCallback.OnAdLoaded appelé");
            _onLoaded?.Invoke(ad);
        }

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            System.Diagnostics.Debug.WriteLine($"📢 InterstitialAdLoadCallback.OnAdFailedToLoad: {error?.Message}");
            _onFailed?.Invoke(error);
        }
    }

    /// <summary>
    /// Callback pour la gestion du contenu plein écran
    /// </summary>
    internal class FullScreenContentCallbackImpl : FullScreenContentCallback
    {
        private readonly bool _isRewarded;
        private readonly AdMobNativeService _service;

        public FullScreenContentCallbackImpl(AdMobNativeService service, bool isRewarded)
        {
            _service = service;
            _isRewarded = isRewarded;
        }

        public override void OnAdDismissedFullScreenContent()
        {
            _service.OnAdDismissed(_isRewarded);
        }

        public override void OnAdFailedToShowFullScreenContent(AdError error)
        {
            _service.OnAdFailedToShow(error, _isRewarded);
        }

        public override void OnAdShowedFullScreenContent()
        {
            // Publicité affichée avec succès
        }
    }

    /// <summary>
    /// Callback pour la gestion des récompenses
    /// </summary>
    internal class UserEarnedRewardListenerImpl : Java.Lang.Object, IOnUserEarnedRewardListener
    {
        private readonly AdMobNativeService _service;

        public UserEarnedRewardListenerImpl(AdMobNativeService service)
        {
            _service = service;
        }

        public void OnUserEarnedReward(IRewardItem reward)
        {
            _service.OnUserEarnedReward(reward);
        }
    }

    #endregion
}
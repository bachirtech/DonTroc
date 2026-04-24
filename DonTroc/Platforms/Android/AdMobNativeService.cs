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
    /// Service natif Android pour gérer les publicités AdMob (récompensées et interstitielles).
    /// 
    /// ══════════════════════════════════════════════════════════════
    /// OPTIMISATIONS FILL RATE v2 :
    /// 1. Ne pas appeler MobileAds.Initialize() ici — MainActivity le fait déjà avec callback
    /// 2. Attendre que le SDK soit prêt (via flag statique) avant de charger les pubs
    /// 3. Protéger contre les chargements multiples simultanés (isLoading flags)
    /// 4. Métriques de diagnostic : compteurs requêtes/impressions/échecs
    /// 5. Ne pas recharger si une pub est déjà disponible
    /// ══════════════════════════════════════════════════════════════
    /// </summary>
    public class AdMobNativeService : Java.Lang.Object, IAdMobService
    {
        private const string RewardedAdUnitId = "ca-app-pub-5085236088670848/4273402055";
        private const string InterstitialAdUnitId = "ca-app-pub-5085236088670848/8212647060";
        private InterstitialAd? _interstitialAd;
        private bool _isInitialized;
        private RewardedAd? _rewardedAd;
        private TaskCompletionSource<bool>? _rewardedAdTcs;
        
        // Protection contre les chargements multiples simultanés
        private bool _isLoadingRewarded;
        private bool _isLoadingInterstitial;
        
        // Backoff exponentiel pour les retries (anti-suspension)
        private int _rewardedRetryCount;
        private int _interstitialRetryCount;
        private const int MaxLoadRetries = 2; // Réduit de 3 à 2 pour moins de requêtes inutiles

        // ── Flag statique : le SDK est-il prêt ? ──
        // Mis à true par le callback dans MainActivity.AdMobInitCallback
        internal static bool IsSdkReady { get; set; }

        // ══════════════════════════════════════════════════════
        // MÉTRIQUES DE DIAGNOSTIC (compteurs de session)
        // Permet de surveiller le ratio requêtes/impressions
        // ══════════════════════════════════════════════════════
        private static int _rewardedRequests;
        private static int _rewardedLoaded;
        private static int _rewardedFailed;
        private static int _rewardedImpressions;
        private static int _interstitialRequests;
        private static int _interstitialLoaded;
        private static int _interstitialFailed;
        private static int _interstitialImpressions;

        /// <summary>
        /// Initialise le service AdMob.
        /// NOTE : N'appelle PAS MobileAds.Initialize() car MainActivity le fait déjà.
        /// Se contente de marquer le service comme prêt et de précharger les pubs
        /// une fois que le SDK est initialisé.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            if (!AdMobConfiguration.ADS_ENABLED) return;

            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("[AdMob] AdMobNativeService.Initialize() — en attente du SDK...");

            // Lancer le préchargement une fois le SDK prêt (polling léger)
            _ = WaitForSdkAndPreloadAsync();
        }

        /// <summary>
        /// Attend que le SDK soit prêt (initialisé par MainActivity) avant de charger les premières pubs.
        /// Évite les requêtes avant que la médiation soit configurée.
        /// </summary>
        private async Task WaitForSdkAndPreloadAsync()
        {
            try
            {
                // Attendre jusqu'à 10 secondes que le SDK soit prêt
                var maxWait = 20; // 20 x 500ms = 10s
                for (int i = 0; i < maxWait; i++)
                {
                    if (IsSdkReady) break;
                    await Task.Delay(500);
                }

                if (!IsSdkReady)
                {
                    System.Diagnostics.Debug.WriteLine("[AdMob] ⚠️ SDK non prêt après 10s — chargement différé");
                    // On continue quand même, le SDK peut être prêt d'un moment à l'autre
                }

                System.Diagnostics.Debug.WriteLine("[AdMob] ✅ SDK prêt — préchargement des pubs");
                
                // Petit délai supplémentaire pour laisser les adaptateurs de médiation s'initialiser
                await Task.Delay(2000);
                
                LoadRewardedAd();
                LoadInterstitialAd();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur WaitForSdkAndPreload: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge une publicité récompensée.
        /// Protégé contre les appels multiples simultanés.
        /// </summary>
        public void LoadRewardedAd()
        {
            // ⚠️ Ne pas charger si suspendu ou pas initialisé
            if (!AdMobConfiguration.ADS_ENABLED || !_isInitialized) return;
            
            // Ne pas recharger si une pub est déjà disponible
            if (_rewardedAd != null) return;
            
            // Ne pas lancer un chargement si un est déjà en cours
            if (_isLoadingRewarded) return;
            _isLoadingRewarded = true;
            _rewardedRequests++;

            System.Diagnostics.Debug.WriteLine($"[AdMob] 📤 Chargement rewarded (requête #{_rewardedRequests})");

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = GetAndroidContext();
                    if (context == null) { _isLoadingRewarded = false; return; }

                    var adRequest = new AdRequest.Builder().Build();
                    RewardedAd.Load(context, RewardedAdUnitId, adRequest, new RewardedAdLoadCallbackWrapper(
                        onLoaded: ad => OnRewardedAdLoaded(ad),
                        onFailed: error => OnRewardedAdFailedToLoad(error)
                    ));
                }
                catch (Exception ex)
                {
                    _isLoadingRewarded = false;
                    System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur chargement rewarded: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Charge une publicité interstitielle.
        /// Protégé contre les appels multiples simultanés.
        /// </summary>
        public void LoadInterstitialAd()
        {
            // ⚠️ Ne pas charger si suspendu ou pas initialisé
            if (!AdMobConfiguration.ADS_ENABLED || !_isInitialized) return;
            
            // Ne pas recharger si une pub est déjà disponible
            if (_interstitialAd != null) return;
            
            // Ne pas lancer un chargement si un est déjà en cours
            if (_isLoadingInterstitial) return;
            _isLoadingInterstitial = true;
            _interstitialRequests++;

            System.Diagnostics.Debug.WriteLine($"[AdMob] 📤 Chargement interstitiel (requête #{_interstitialRequests})");

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = GetAndroidContext();
                    if (context == null) { _isLoadingInterstitial = false; return; }

                    var adRequest = new AdRequest.Builder().Build();
                    InterstitialAd.Load(context, InterstitialAdUnitId, adRequest, new InterstitialAdLoadCallbackWrapper(
                        onLoaded: ad => OnInterstitialAdLoaded(ad),
                        onFailed: error => OnInterstitialAdFailedToLoad(error)
                    ));
                }
                catch (Exception ex)
                {
                    _isLoadingInterstitial = false;
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

                    var contentCallback = new FullScreenContentCallbackImpl(this, true);
                    _rewardedAd!.FullScreenContentCallback = contentCallback;

                    var rewardCallback = new UserEarnedRewardListenerImpl(this);
                    _rewardedAd.Show(activity, rewardCallback);
                    
                    _rewardedImpressions++;
                    System.Diagnostics.Debug.WriteLine($"[AdMob] 📺 Rewarded affiché (impression #{_rewardedImpressions})");
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

                    var contentCallback = new FullScreenContentCallbackImpl(this, false);
                    _interstitialAd!.FullScreenContentCallback = contentCallback;

                    _interstitialAd.Show(activity);
                    
                    _interstitialImpressions++;
                    System.Diagnostics.Debug.WriteLine($"[AdMob] 📺 Interstitiel affiché (impression #{_interstitialImpressions})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob] Erreur affichage interstitiel: {ex.Message}");
                }
            });
        }

        public void LogApiLimitation()
        {
            System.Diagnostics.Debug.WriteLine("══════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[AdMob] 📊 MÉTRIQUES DE SESSION :");
            System.Diagnostics.Debug.WriteLine($"  Rewarded    → Requêtes: {_rewardedRequests} | Chargés: {_rewardedLoaded} | Échecs: {_rewardedFailed} | Impressions: {_rewardedImpressions}");
            System.Diagnostics.Debug.WriteLine($"  Interstitiel→ Requêtes: {_interstitialRequests} | Chargés: {_interstitialLoaded} | Échecs: {_interstitialFailed} | Impressions: {_interstitialImpressions}");
            var totalReq = _rewardedRequests + _interstitialRequests;
            var totalImp = _rewardedImpressions + _interstitialImpressions;
            if (totalReq > 0)
                System.Diagnostics.Debug.WriteLine($"  Fill Rate   → {totalImp}/{totalReq} = {(double)totalImp / totalReq * 100:F1}%");
            System.Diagnostics.Debug.WriteLine("══════════════════════════════════════════════════");
        }

        /// <summary>
        /// Récupère le contexte Android de manière fiable
        /// </summary>
        private static Context? GetAndroidContext()
        {
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity != null)
                    return activity;

                var context = global::Android.App.Application.Context;
                if (context != null)
                    return context;

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
            _isLoadingRewarded = false;
            _rewardedRetryCount = 0;
            _rewardedLoaded++;
            System.Diagnostics.Debug.WriteLine($"[AdMob] ✅ Rewarded chargé (total chargés: {_rewardedLoaded}/{_rewardedRequests})");
        }

        internal void OnRewardedAdFailedToLoad(LoadAdError error)
        {
            _rewardedAd = null;
            _isLoadingRewarded = false;
            _rewardedFailed++;
            _rewardedRetryCount++;
            
            System.Diagnostics.Debug.WriteLine($"[AdMob] ❌ Rewarded échec ({_rewardedRetryCount}/{MaxLoadRetries}): {error.Message} (code={error.Code})");
            LogMediationDetails(error, "Rewarded");
            
            if (_rewardedRetryCount <= MaxLoadRetries)
            {
                // Backoff exponentiel : 60s, 120s
                var delay = 60000 * (int)Math.Pow(2, _rewardedRetryCount - 1);
                System.Diagnostics.Debug.WriteLine($"[AdMob] ⏳ Retry rewarded dans {delay / 1000}s");
                Task.Delay(delay).ContinueWith(_ => LoadRewardedAd());
            }
        }

        internal void OnInterstitialAdLoaded(InterstitialAd ad)
        {
            _interstitialAd = ad;
            _isLoadingInterstitial = false;
            _interstitialRetryCount = 0;
            _interstitialLoaded++;
            System.Diagnostics.Debug.WriteLine($"[AdMob] ✅ Interstitiel chargé (total chargés: {_interstitialLoaded}/{_interstitialRequests})");
        }

        internal void OnInterstitialAdFailedToLoad(LoadAdError error)
        {
            _interstitialAd = null;
            _isLoadingInterstitial = false;
            _interstitialFailed++;
            _interstitialRetryCount++;
            
            System.Diagnostics.Debug.WriteLine($"[AdMob] ❌ Interstitiel échec ({_interstitialRetryCount}/{MaxLoadRetries}): {error.Message} (code={error.Code})");
            LogMediationDetails(error, "Interstitiel");
            
            if (_interstitialRetryCount <= MaxLoadRetries)
            {
                // Backoff exponentiel : 60s, 120s
                var delay = 60000 * (int)Math.Pow(2, _interstitialRetryCount - 1);
                System.Diagnostics.Debug.WriteLine($"[AdMob] ⏳ Retry interstitiel dans {delay / 1000}s");
                Task.Delay(delay).ContinueWith(_ => LoadInterstitialAd());
            }
        }

        /// <summary>
        /// Log les détails de médiation pour diagnostiquer les échecs de fill
        /// </summary>
        private static void LogMediationDetails(LoadAdError error, string adType)
        {
            var responseInfo = error.ResponseInfo;
            if (responseInfo == null) return;
            
            System.Diagnostics.Debug.WriteLine($"[AdMob]    Adapter gagnant: {responseInfo.MediationAdapterClassName ?? "aucun"}");
            
            var adapterResponses = responseInfo.AdapterResponses;
            if (adapterResponses == null) return;
            
            foreach (var adapter in adapterResponses)
            {
                var adapterName = adapter.AdapterClassName ?? "inconnu";
                var latency = adapter.LatencyMillis;
                var adError = adapter.AdError?.Message ?? "aucune";
                System.Diagnostics.Debug.WriteLine(
                    $"[AdMob]    → {adapterName}: latence={latency}ms, erreur={adError}");
            }
        }

        internal void OnAdDismissed(bool isRewarded)
        {
            if (isRewarded)
            {
                _rewardedAdTcs?.TrySetResult(false);
                _rewardedAd = null;
                // Précharger la prochaine pub avec un petit délai
                Task.Delay(1000).ContinueWith(_ => LoadRewardedAd());
            }
            else
            {
                _interstitialAd = null;
                // Précharger la prochaine pub avec un petit délai
                Task.Delay(1000).ContinueWith(_ => LoadInterstitialAd());
            }
        }

        internal void OnAdFailedToShow(AdError error, bool isRewarded)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob] ❌ Échec affichage {(isRewarded ? "rewarded" : "interstitiel")}: {error.Message}");
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

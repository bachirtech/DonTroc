using System;
using System.Threading.Tasks;
using Google.Android.Gms.Ads;
using Android.Views;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using View = Microsoft.Maui.Controls.View;

namespace DonTroc
{
    /// <summary>
    /// Handler natif Android pour AdBannerView - Bannière AdMob directe.
    /// 
    /// ══════════════════════════════════════════════════════════════
    /// OPTIMISATIONS FILL RATE v3 — Analyse données réelles :
    /// 
    /// PROBLÈME : 6 060 requêtes → 2 797 matches → 252 impressions
    ///            = 91% des pubs chargées sont GASPILLÉES
    /// 
    /// CAUSES IDENTIFIÉES :
    /// 1. L'utilisateur navigue rapidement → la bannière charge mais
    ///    la page est déjà quittée avant l'affichage
    /// 2. L'auto-refresh AdMob (30-60s) génère des requêtes même
    ///    quand l'utilisateur ne regarde pas la page
    /// 3. Chaque navigation crée un nouveau handler (vues Transient)
    ///    → nouvelle requête à chaque visite de page
    /// 
    /// CORRECTIONS :
    /// - Flag _isDisposed pour annuler si la page est quittée
    /// - Délai de 3s avant chargement (filtre les navigations rapides)
    /// - Pas de retry si le handler est déjà disposé
    /// - Compteurs de diagnostic améliorés
    /// ══════════════════════════════════════════════════════════════
    /// </summary>
    [global::Android.Runtime.Preserve(AllMembers = true)]
    public class AdMobBannerHandler : ContentViewHandler
    {
        private const string BannerAdUnitId = "ca-app-pub-5085236088670848/4140917995";
        
        private AdView? _adView;
        private ContentViewGroup? _container;
        private float _density;
        private int _bannerHeightPx;
        private bool _isInitialized;
        private bool _isLoaded;
        private bool _isDisposed; // ← NOUVEAU : annuler si page quittée
        private int _retryCount;
        private const int MaxRetries = 1;

        // Compteurs globaux partagés entre toutes les instances
        private static int _totalBannerRequests;
        private static int _totalBannerLoaded;
        private static int _totalBannerFailed;
        private static int _totalBannerCancelled; // ← pubs annulées car page quittée

        public AdMobBannerHandler() : base(new PropertyMapper<IContentView, IContentViewHandler>(),
            new CommandMapper<IContentView, IContentViewHandler>())
        {
        }

        protected override ContentViewGroup CreatePlatformView()
        {
            var view = base.CreatePlatformView();
            view.SetBackgroundColor(Android.Graphics.Color.Transparent);
            view.SetWillNotDraw(true);
            return view;
        }

        protected override void ConnectHandler(ContentViewGroup platformView)
        {
            base.ConnectHandler(platformView);

            ForceTransparentRecursive(platformView);

            if (!DonTroc.Services.AdMobConfiguration.ADS_ENABLED)
            {
                return;
            }

            if (_isInitialized) return;
            _isInitialized = true;
            _isDisposed = false;
            _container = platformView;

            _ = InitializeWithDelayAsync(platformView);
        }

        private static void ForceTransparentRecursive(Android.Views.View view)
        {
            view.SetBackgroundColor(Android.Graphics.Color.Transparent);
            if (view is ViewGroup vg)
            {
                for (int i = 0; i < vg.ChildCount; i++)
                {
                    ForceTransparentRecursive(vg.GetChildAt(i)!);
                }
            }
        }

        private async Task InitializeWithDelayAsync(ContentViewGroup platformView)
        {
            try
            {
                // ══════════════════════════════════════════════════
                // ÉTAPE 1 : Attendre que le SDK soit prêt
                // ══════════════════════════════════════════════════
                var maxWait = 30; // 30 x 500ms = 15s max
                for (int i = 0; i < maxWait; i++)
                {
                    if (_isDisposed) return; // ← Page quittée pendant l'attente
                    if (DonTroc.Platforms.Android.AdMobNativeService.IsSdkReady) break;
                    await Task.Delay(500);
                }
                
                // ══════════════════════════════════════════════════
                // ÉTAPE 2 : Délai anti-navigation rapide (3 secondes)
                // Si l'utilisateur quitte la page en moins de 3s,
                // on ne charge PAS la bannière → économise une requête
                // ══════════════════════════════════════════════════
                await Task.Delay(3000);
                
                if (_isDisposed)
                {
                    _totalBannerCancelled++;
                    System.Diagnostics.Debug.WriteLine(
                        $"[Banner] 🚫 Chargement annulé (page quittée avant 3s) — total annulés: {_totalBannerCancelled}");
                    return;
                }
                
                MainThread.BeginInvokeOnMainThread(() => InitializeAdMobBanner(platformView));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Banner] Erreur init: {ex.Message}");
            }
        }

        private void InitializeAdMobBanner(ContentViewGroup container)
        {
            // Vérification supplémentaire au cas où la page serait quittée
            // entre le BeginInvokeOnMainThread et l'exécution
            if (_isDisposed) return;
            
            try
            {
                var context = container.Context;
                if (context == null) return;

                _density = context.Resources?.DisplayMetrics?.Density ?? 2.75f;
                _bannerHeightPx = (int)(50 * _density);

                container.RemoveAllViews();
                container.SetBackgroundColor(Android.Graphics.Color.Transparent);

                _adView = new AdView(context)
                {
                    AdUnitId = BannerAdUnitId,
                    AdSize = AdSize.Banner // 320x50
                };
                _adView.SetBackgroundColor(Android.Graphics.Color.Transparent);

                _adView.AdListener = new BannerAdListener(this);

                var layoutParams = new Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    _bannerHeightPx
                )
                {
                    Gravity = GravityFlags.CenterHorizontal | GravityFlags.CenterVertical
                };
                _adView.LayoutParameters = layoutParams;
                _adView.Visibility = ViewStates.Invisible;

                container.AddView(_adView);

                _totalBannerRequests++;
                System.Diagnostics.Debug.WriteLine(
                    $"[Banner] 📤 Requête bannière #{_totalBannerRequests} " +
                    $"(chargées: {_totalBannerLoaded}, échecs: {_totalBannerFailed}, annulées: {_totalBannerCancelled})");
                
                var adRequest = new AdRequest.Builder().Build();
                _adView.LoadAd(adRequest);

                if (VirtualView is View mauiView)
                {
                    mauiView.HeightRequest = 50;
                    mauiView.MinimumHeightRequest = 50;
                    mauiView.BackgroundColor = Colors.Transparent;
                    mauiView.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Banner] Erreur: {ex.Message}");
            }
        }

        protected override void DisconnectHandler(ContentViewGroup platformView)
        {
            // Marquer comme disposé IMMÉDIATEMENT pour annuler tout chargement en cours
            _isDisposed = true;
            
            try
            {
                if (_adView != null)
                {
                    _adView.Pause(); // Pause avant Destroy pour arrêter l'auto-refresh
                    _adView.Destroy();
                }
                _adView = null;
                _isInitialized = false;
            }
            catch { }

            base.DisconnectHandler(platformView);
        }

        internal void OnAdLoaded()
        {
            // Si la page est déjà quittée, ne pas afficher → pas d'impression gaspillée
            if (_isDisposed)
            {
                _totalBannerCancelled++;
                System.Diagnostics.Debug.WriteLine(
                    $"[Banner] 🚫 Bannière chargée mais page déjà quittée — gaspillage évité (total: {_totalBannerCancelled})");
                return;
            }
            
            _isLoaded = true;
            _retryCount = 0;
            _totalBannerLoaded++;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_adView == null || _container == null || _isDisposed) return;

                    _adView.Visibility = ViewStates.Visible;

                    var containerWidth = _container.Width > 0 ? _container.Width : _container.MeasuredWidth;
                    if (containerWidth <= 0)
                    {
                        var dm = _container.Context?.Resources?.DisplayMetrics;
                        containerWidth = dm?.WidthPixels ?? 1080;
                    }

                    int wSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec(containerWidth, MeasureSpecMode.Exactly);
                    int hSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec(_bannerHeightPx, MeasureSpecMode.Exactly);
                    _adView.Measure(wSpec, hSpec);
                    _adView.Layout(0, 0, containerWidth, _bannerHeightPx);

                    _adView.BringToFront();
                    _adView.RequestLayout();
                    _container.RequestLayout();

                    if (VirtualView is View mauiView)
                    {
                        mauiView.HeightRequest = 50;
                        mauiView.IsVisible = true;
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"[Banner] ✅ Bannière visible ! (affichées: {_totalBannerLoaded}/{_totalBannerRequests} req, " +
                        $"annulées: {_totalBannerCancelled})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Banner] Erreur affichage: {ex.Message}");
                }
            });
        }

        internal void OnAdFailedToLoad(LoadAdError error)
        {
            _isLoaded = false;
            _retryCount++;
            _totalBannerFailed++;

            System.Diagnostics.Debug.WriteLine(
                $"[Banner] ❌ Échec ({_retryCount}/{MaxRetries}): {error.Message} " +
                $"(échecs: {_totalBannerFailed}/{_totalBannerRequests} req)");
            System.Diagnostics.Debug.WriteLine($"[Banner]    Code: {error.Code} | Domaine: {error.Domain}");
            
            // Détails de médiation
            var responseInfo = error.ResponseInfo;
            if (responseInfo != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Banner]    Adapter: {responseInfo.MediationAdapterClassName ?? "aucun"}");
                
                var adapterResponses = responseInfo.AdapterResponses;
                if (adapterResponses != null)
                {
                    foreach (var adapter in adapterResponses)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Banner]    → {adapter.AdapterClassName ?? "?"}: " +
                            $"latence={adapter.LatencyMillis}ms, erreur={adapter.AdError?.Message ?? "aucune"}");
                    }
                }
            }

            // Ne pas retenter si la page est déjà quittée
            if (_isDisposed) return;
            
            if (_retryCount <= MaxRetries)
            {
                var delay = 90000 * (int)Math.Pow(2, _retryCount - 1);
                System.Diagnostics.Debug.WriteLine($"[Banner] ⏳ Retry dans {delay / 1000}s");
                Task.Delay(delay).ContinueWith(_ =>
                {
                    // Vérifier encore une fois avant de retenter
                    if (_isDisposed) return;
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_adView != null && !_isLoaded && !_isDisposed)
                        {
                            _totalBannerRequests++;
                            var adRequest = new AdRequest.Builder().Build();
                            _adView.LoadAd(adRequest);
                        }
                    });
                });
            }
        }
    }

    /// <summary>
    /// Listener pour les événements de la bannière AdMob
    /// </summary>
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class BannerAdListener : AdListener
    {
        private readonly AdMobBannerHandler _handler;

        public BannerAdListener(AdMobBannerHandler handler) => _handler = handler;

        public override void OnAdLoaded() => _handler.OnAdLoaded();
        public override void OnAdFailedToLoad(LoadAdError error) => _handler.OnAdFailedToLoad(error);
        public override void OnAdClicked() { }
        public override void OnAdOpened() { }
        public override void OnAdImpression() 
        {
            System.Diagnostics.Debug.WriteLine("[Banner] 👁️ Impression comptabilisée par AdMob");
        }
    }
}


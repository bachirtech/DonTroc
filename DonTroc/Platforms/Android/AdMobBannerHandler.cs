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
    /// Handler natif Android pour AdBannerView - Bannière AdMob directe
    /// </summary>
    public class AdMobBannerHandler : ContentViewHandler
    {
        // ID de production — Toujours utiliser les IDs de production.
        // Les appareils de test (configurés dans MainActivity.cs via setTestDeviceIds)
        // recevront automatiquement des pubs de test, ce qui respecte la policy Google.
        // IMPORTANT : Avec un ID de test Google (ca-app-pub-3940256099942544/...),
        // la médiation (Facebook, Unity, Pangle, Vungle, IronSource) n'est JAMAIS déclenchée.
        // Seul l'ID de production permet de tester la chaîne de médiation complète.
        private const string BannerAdUnitId = "ca-app-pub-5085236088670848/1004542862";
        
        private AdView? _adView;
        private ContentViewGroup? _container;
        private float _density;
        private int _bannerHeightPx;
        private bool _isInitialized;
        private bool _isLoaded;
        private int _retryCount;
        private const int MaxRetries = 3;

        public AdMobBannerHandler() : base(new PropertyMapper<IContentView, IContentViewHandler>(),
            new CommandMapper<IContentView, IContentViewHandler>())
        {
        }

        protected override void ConnectHandler(ContentViewGroup platformView)
        {
            base.ConnectHandler(platformView);

            if (!DonTroc.Services.AdMobConfiguration.ADS_ENABLED)
            {
                return;
            }

            if (_isInitialized) return;
            _isInitialized = true;
            _container = platformView;

            _ = InitializeWithDelayAsync(platformView);
        }

        private async Task InitializeWithDelayAsync(ContentViewGroup platformView)
        {
            try
            {
                // Attendre que le layout MAUI se stabilise
                await Task.Delay(1500);
                MainThread.BeginInvokeOnMainThread(() => InitializeAdMobBanner(platformView));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Banner] Erreur init: {ex.Message}");
            }
        }

        private void InitializeAdMobBanner(ContentViewGroup container)
        {
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

                _adView.AdListener = new BannerAdListener(this);

                var layoutParams = new Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    _bannerHeightPx
                )
                {
                    Gravity = GravityFlags.CenterHorizontal | GravityFlags.CenterVertical
                };
                _adView.LayoutParameters = layoutParams;
                _adView.Visibility = ViewStates.Visible;

                container.AddView(_adView);

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
            try
            {
                _adView?.Destroy();
                _adView = null;
                _isInitialized = false;
            }
            catch { }

            base.DisconnectHandler(platformView);
        }

        internal void OnAdLoaded()
        {
            _isLoaded = true;
            _retryCount = 0;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_adView == null || _container == null) return;

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

                    System.Diagnostics.Debug.WriteLine("[Banner] ✅ Bannière AdMob chargée");
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

            // Diagnostic détaillé pour identifier le problème de médiation
            System.Diagnostics.Debug.WriteLine($"[Banner] ❌ Échec ({_retryCount}/{MaxRetries}): {error.Message}");
            System.Diagnostics.Debug.WriteLine($"[Banner]    Code erreur: {error.Code}");
            System.Diagnostics.Debug.WriteLine($"[Banner]    Domaine: {error.Domain}");
            System.Diagnostics.Debug.WriteLine($"[Banner]    Ad Unit ID utilisé: {BannerAdUnitId}");
            
            // Afficher la cause racine de la médiation si disponible
            var cause = error.Cause;
            if (cause != null)
            {
                System.Diagnostics.Debug.WriteLine("[Banner]    Cause médiation: " + cause.Message);
            }
            
            // ResponseInfo contient les détails de chaque adaptateur de médiation
            var responseInfo = error.ResponseInfo;
            if (responseInfo != null)
            {
                System.Diagnostics.Debug.WriteLine("[Banner]    Mediation Adapter: " + (responseInfo.MediationAdapterClassName ?? "aucun"));
                System.Diagnostics.Debug.WriteLine("[Banner]    Response ID: " + (responseInfo.ResponseId ?? "aucun"));
                
                var adapterResponses = responseInfo.AdapterResponses;
                if (adapterResponses != null)
                {
                    foreach (var adapter in adapterResponses)
                    {
                        var adapterName = adapter.AdapterClassName ?? "inconnu";
                        var latency = adapter.LatencyMillis;
                        var adError = adapter.AdError?.Message ?? "aucune";
                        System.Diagnostics.Debug.WriteLine(
                            $"[Banner]    → Adapter: {adapterName} | Latence: {latency}ms | Erreur: {adError}");
                    }
                }
            }

            if (_retryCount <= MaxRetries)
            {
                var delay = 30000 * (int)Math.Pow(2, _retryCount - 1);
                Task.Delay(delay).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_adView != null && !_isLoaded)
                        {
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
        public override void OnAdImpression() { }
    }
}
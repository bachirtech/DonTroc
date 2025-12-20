using System;
using System.Threading.Tasks;
using Android.Gms.Ads;
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
    /// Handler natif Android pour AdBannerView.
    /// 
    /// Ce handler remplace le contenu MAUI par une vraie bannière AdMob native.
    /// Il gère :
    /// - L'initialisation de l'AdView avec les bonnes dimensions
    /// - Le chargement de la publicité
    /// - Les événements de succès/échec
    /// - La destruction propre des ressources
    /// </summary>
    public class AdMobBannerHandler : ContentViewHandler
    {
        // ID de production AdMob pour les bannières
        private const string TestBannerAdUnitId = "ca-app-pub-5085236088670848/2349645674";
        
        private AdView? _adView;
        private ContentViewGroup? _container;
        private float _density;
        private bool _isInitialized;
        private bool _isLoaded;

        public AdMobBannerHandler() : base(new PropertyMapper<IContentView, IContentViewHandler>(),
            new CommandMapper<IContentView, IContentViewHandler>())
        {
        }

        private static string BannerAdUnitId => TestBannerAdUnitId;

        /// <summary>
        /// Appelé quand le handler est connecté à une vue.
        /// Initialise la bannière AdMob avec un léger délai pour laisser le layout MAUI se stabiliser.
        /// </summary>
        protected override void ConnectHandler(ContentViewGroup platformView)
        {
            base.ConnectHandler(platformView);

            if (_isInitialized) return;
            _isInitialized = true;
            _container = platformView;

            _ = InitializeWithDelayAsync(platformView);
        }

        /// <summary>
        /// Initialise l'AdView avec un délai pour permettre au layout parent de s'établir.
        /// </summary>
        private async Task InitializeWithDelayAsync(ContentViewGroup platformView)
        {
            try
            {
                await Task.Delay(100);
                MainThread.BeginInvokeOnMainThread(() => InitializeAdView(platformView));
            }
            catch (Exception)
            {
                // Ignorer les erreurs d'initialisation silencieusement
            }
        }

        /// <summary>
        /// Crée et configure l'AdView native Android.
        /// 
        /// Processus :
        /// 1. Calcule les dimensions en pixels basées sur la densité d'écran
        /// 2. Configure le container parent avec les dimensions appropriées
        /// 3. Crée l'AdView avec l'ID de bannière et la taille standard (320x50dp)
        /// 4. Attache un listener pour gérer les événements de chargement
        /// 5. Lance la requête de publicité
        /// </summary>
        private void InitializeAdView(ContentViewGroup container)
        {
            try
            {
                var context = container.Context;
                if (context == null) return;

                _density = context.Resources?.DisplayMetrics?.Density ?? 2.75f;
                var bannerWidthPx = (int)(320 * _density);
                var bannerHeightPx = (int)(50 * _density);

                // Configurer le container parent
                container.RemoveAllViews();
                container.SetBackgroundColor(Android.Graphics.Color.Transparent);
                container.SetMinimumHeight(bannerHeightPx);
                container.SetMinimumWidth(bannerWidthPx);

                var containerParams = new Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    bannerHeightPx
                );
                container.LayoutParameters = containerParams;

                // Créer l'AdView avec dimensions fixes
                _adView = new AdView(context)
                {
                    AdUnitId = BannerAdUnitId,
                    AdSize = AdSize.Banner
                };

                var adLayoutParams = new Android.Widget.FrameLayout.LayoutParams(
                    bannerWidthPx,
                    bannerHeightPx
                )
                {
                    Gravity = GravityFlags.CenterHorizontal | GravityFlags.Top
                };
                _adView.LayoutParameters = adLayoutParams;
                _adView.SetMinimumWidth(bannerWidthPx);
                _adView.SetMinimumHeight(bannerHeightPx);

                _adView.AdListener = new AdMobBannerListener(this);
                container.AddView(_adView);

                // Charger la publicité
                var adRequest = new AdRequest.Builder().Build();
                _adView.LoadAd(adRequest);

                // Configurer le VirtualView MAUI
                if (VirtualView is View mauiView)
                {
                    mauiView.HeightRequest = 50;
                    mauiView.MinimumHeightRequest = 50;
                    mauiView.BackgroundColor = Colors.Transparent;
                }
            }
            catch (Exception)
            {
                // Ignorer les erreurs silencieusement
            }
        }

        /// <summary>
        /// Nettoie les ressources quand le handler est déconnecté.
        /// </summary>
        protected override void DisconnectHandler(ContentViewGroup platformView)
        {
            try
            {
                _adView?.Destroy();
                _adView = null;
                _isInitialized = false;
            }
            catch (Exception)
            {
                // Ignorer les erreurs de destruction
            }

            base.DisconnectHandler(platformView);
        }

        /// <summary>
        /// Appelé quand la publicité est chargée avec succès.
        /// Force le layout et la visibilité de l'AdView.
        /// </summary>
        internal void OnAdLoaded()
        {
            _isLoaded = true;

            if (_adView != null && _container != null)
            {
                var bannerWidthPx = (int)(320 * _density);
                var bannerHeightPx = (int)(50 * _density);

                _adView.Visibility = ViewStates.Visible;
                _adView.BringToFront();

                // Forcer la mesure et le layout avec les dimensions exactes
                var widthSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec(bannerWidthPx, MeasureSpecMode.Exactly);
                var heightSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec(bannerHeightPx, MeasureSpecMode.Exactly);
                _adView.Measure(widthSpec, heightSpec);
                _adView.Layout(0, 0, bannerWidthPx, bannerHeightPx);

                _adView.RequestLayout();
                _adView.Invalidate();
                _container.RequestLayout();
                _container.Invalidate();
            }
        }

        /// <summary>
        /// Appelé quand le chargement de la publicité échoue.
        /// Réessaie automatiquement après 30 secondes pour les erreurs réseau.
        /// </summary>
        internal void OnAdFailedToLoad(LoadAdError error)
        {
            _isLoaded = false;

            // Retry après 30 secondes pour erreurs réseau (code 2) ou no fill (code 3)
            if (error.Code == 2 || error.Code == 3)
            {
                Task.Delay(30000).ContinueWith(_ =>
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
    internal class AdMobBannerListener : AdListener
    {
        private readonly AdMobBannerHandler _handler;

        public AdMobBannerListener(AdMobBannerHandler handler)
        {
            _handler = handler;
        }

        public override void OnAdLoaded()
        {
            base.OnAdLoaded();
            _handler.OnAdLoaded();
        }

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            base.OnAdFailedToLoad(error);
            _handler.OnAdFailedToLoad(error);
        }

        public override void OnAdClicked() => base.OnAdClicked();
        public override void OnAdOpened() => base.OnAdOpened();
        public override void OnAdImpression() => base.OnAdImpression();
    }
}
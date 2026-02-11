using System;
using System.Threading.Tasks;
using Android.Gms.Ads;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using View = Microsoft.Maui.Controls.View;

namespace DonTroc
{
    /// <summary>
    /// Handler unifié pour les bannières publicitaires sur Android.
    /// Bascule automatiquement entre AppLovin MAX et AdMob selon la configuration.
    /// 
    /// Priorité:
    /// 1. AppLovin MAX (si activé et configuré)
    /// 2. AdMob (si activé)
    /// 3. Placeholder (si aucun n'est actif)
    /// </summary>
    public class UnifiedAdBannerHandler : ContentViewHandler
    {
        private global::Android.Views.View? _adView;
        private ContentViewGroup? _container;
        private float _density;
        private bool _isInitialized;
        private bool _isLoaded;
        private AdProvider _activeProvider = AdProvider.None;

        // IDs AdMob
        private const string AdMobBannerAdUnitId = "ca-app-pub-5085236088670848/2349645674";

        public UnifiedAdBannerHandler() : base(new PropertyMapper<IContentView, IContentViewHandler>(),
            new CommandMapper<IContentView, IContentViewHandler>())
        {
        }

        protected override void ConnectHandler(ContentViewGroup platformView)
        {
            base.ConnectHandler(platformView);

            // Déterminer quelle plateforme utiliser
            _activeProvider = GetActiveAdProvider();

            if (_activeProvider == AdProvider.None)
            {
                System.Diagnostics.Debug.WriteLine("📱 UnifiedAdBanner: Aucune pub active");
                HideView(platformView);
                return;
            }

            if (_isInitialized) return;
            _isInitialized = true;
            _container = platformView;

            _ = InitializeWithDelayAsync(platformView);
        }

        private static AdProvider GetActiveAdProvider()
        {
            // Priorité 1: AppLovin MAX
            if (DonTroc.Services.AppLovinConfiguration.APPLOVIN_ENABLED &&
                DonTroc.Services.AppLovinConfiguration.IsConfigurationValid())
            {
                return AdProvider.AppLovin;
            }

            // Priorité 2: AdMob
            if (DonTroc.Services.AdMobConfiguration.ADS_ENABLED)
            {
                return AdProvider.AdMob;
            }

            return AdProvider.None;
        }

        private async Task InitializeWithDelayAsync(ContentViewGroup platformView)
        {
            try
            {
                await Task.Delay(100);
                MainThread.BeginInvokeOnMainThread(() => InitializeAdView(platformView));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UnifiedAd init error: {ex.Message}");
            }
        }

        private void InitializeAdView(ContentViewGroup container)
        {
            try
            {
                var context = container.Context;
                if (context == null) return;

                _density = context.Resources?.DisplayMetrics?.Density ?? 2.75f;
                var bannerWidthPx = (int)(320 * _density);
                var bannerHeightPx = (int)(50 * _density);

                // Configurer le container
                container.RemoveAllViews();
                container.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
                container.SetMinimumHeight(bannerHeightPx);
                container.SetMinimumWidth(bannerWidthPx);

                var containerParams = new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    bannerHeightPx
                );
                container.LayoutParameters = containerParams;

                // Initialiser selon le provider actif
                switch (_activeProvider)
                {
                    case AdProvider.AppLovin:
                        InitializeAppLovinBanner(context, container, bannerWidthPx, bannerHeightPx);
                        break;

                    case AdProvider.AdMob:
                        InitializeAdMobBanner(context, container, bannerWidthPx, bannerHeightPx);
                        break;

                    default:
                        ShowPlaceholder(container, "Pub non configurée");
                        break;
                }

                // Configurer le VirtualView MAUI
                if (VirtualView is View mauiView)
                {
                    mauiView.HeightRequest = 50;
                    mauiView.MinimumHeightRequest = 50;
                    mauiView.BackgroundColor = Colors.Transparent;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ InitializeAdView error: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialise une bannière AdMob
        /// </summary>
        private void InitializeAdMobBanner(global::Android.Content.Context context, 
            ContentViewGroup container, int width, int height)
        {
            try
            {
                var adView = new AdView(context)
                {
                    AdUnitId = AdMobBannerAdUnitId,
                    AdSize = AdSize.Banner
                };

                var adLayoutParams = new FrameLayout.LayoutParams(width, height)
                {
                    Gravity = GravityFlags.CenterHorizontal | GravityFlags.Top
                };
                adView.LayoutParameters = adLayoutParams;
                adView.SetMinimumWidth(width);
                adView.SetMinimumHeight(height);

                adView.AdListener = new UnifiedAdMobListener(this);
                container.AddView(adView);

                var adRequest = new AdRequest.Builder().Build();
                adView.LoadAd(adRequest);

                _adView = adView;
                System.Diagnostics.Debug.WriteLine("📱 AdMob banner initialisée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AdMob init error: {ex.Message}");
                ShowPlaceholder(container, "Erreur AdMob");
            }
        }

        /// <summary>
        /// Initialise une bannière AppLovin MAX
        /// </summary>
        private void InitializeAppLovinBanner(global::Android.Content.Context context,
            ContentViewGroup container, int width, int height)
        {
            try
            {
                // TODO: Implémenter avec le SDK AppLovin MAX
                // Une fois le package NuGet ajouté:
                /*
                var adView = new MaxAdView(Services.AppLovinConfiguration.BANNER_AD_UNIT_ID, context);
                adView.SetListener(new AppLovinBannerListener(this));
                
                var layoutParams = new FrameLayout.LayoutParams(width, height)
                {
                    Gravity = GravityFlags.CenterHorizontal | GravityFlags.Top
                };
                adView.LayoutParameters = layoutParams;
                
                container.AddView(adView);
                adView.LoadAd();
                _adView = adView;
                */

                ShowPlaceholder(container, "AppLovin - À configurer");
                System.Diagnostics.Debug.WriteLine("📱 AppLovin placeholder - SDK à ajouter");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AppLovin init error: {ex.Message}");
                ShowPlaceholder(container, "Erreur AppLovin");
            }
        }

        private void ShowPlaceholder(ContentViewGroup container, string message)
        {
            try
            {
                var context = container.Context;
                if (context == null) return;

                var bannerHeightPx = (int)(50 * _density);

                var placeholder = new TextView(context)
                {
                    Text = message,
                    Gravity = GravityFlags.Center
                };
                placeholder.SetTextColor(global::Android.Graphics.Color.Gray);
                placeholder.SetBackgroundColor(global::Android.Graphics.Color.ParseColor("#F5F5F5"));

                var layoutParams = new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    bannerHeightPx
                );
                placeholder.LayoutParameters = layoutParams;

                container.RemoveAllViews();
                container.AddView(placeholder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ShowPlaceholder error: {ex.Message}");
            }
        }

        private void HideView(ContentViewGroup container)
        {
            try
            {
                container.RemoveAllViews();
                container.Visibility = ViewStates.Gone;
                
                if (VirtualView is View mauiView)
                {
                    mauiView.HeightRequest = 0;
                    mauiView.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HideView error: {ex.Message}");
            }
        }

        protected override void DisconnectHandler(ContentViewGroup platformView)
        {
            try
            {
                if (_adView is AdView adMobView)
                {
                    adMobView.Destroy();
                }
                _adView = null;
                _isInitialized = false;
            }
            catch (Exception)
            {
                // Ignorer
            }

            base.DisconnectHandler(platformView);
        }

        internal void OnAdLoaded()
        {
            _isLoaded = true;
            System.Diagnostics.Debug.WriteLine($"✅ {_activeProvider} banner chargée");

            if (_adView != null && _container != null)
            {
                var bannerWidthPx = (int)(320 * _density);
                var bannerHeightPx = (int)(50 * _density);

                _adView.Visibility = ViewStates.Visible;
                _adView.BringToFront();

                var widthSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(bannerWidthPx, MeasureSpecMode.Exactly);
                var heightSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(bannerHeightPx, MeasureSpecMode.Exactly);
                _adView.Measure(widthSpec, heightSpec);
                _adView.Layout(0, 0, bannerWidthPx, bannerHeightPx);

                _adView.RequestLayout();
                _adView.Invalidate();
                _container.RequestLayout();
                _container.Invalidate();
            }
        }

        internal void OnAdFailedToLoad(string error)
        {
            _isLoaded = false;
            System.Diagnostics.Debug.WriteLine($"❌ {_activeProvider} banner failed: {error}");
        }

        private enum AdProvider
        {
            None,
            AdMob,
            AppLovin
        }
    }

    /// <summary>
    /// Listener AdMob pour le handler unifié
    /// </summary>
    internal class UnifiedAdMobListener : AdListener
    {
        private readonly UnifiedAdBannerHandler _handler;

        public UnifiedAdMobListener(UnifiedAdBannerHandler handler)
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
            _handler.OnAdFailedToLoad(error.Message);
        }
    }
}

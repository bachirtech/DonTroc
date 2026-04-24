using System;
using System.Threading.Tasks;
using Google.Android.Gms.Ads;
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
    /// Handler unifié pour les bannières publicitaires AdMob sur Android.
    /// </summary>
    public class UnifiedAdBannerHandler : ContentViewHandler
    {
        private global::Android.Views.View? _adView;
        private ContentViewGroup? _container;
        private float _density;
        private bool _isInitialized;
        private bool _isLoaded;
        private bool _isDisposed; // Protection anti-gaspillage : annuler si page quittée
        private AdProvider _activeProvider = AdProvider.None;

        // IDs AdMob
        private const string ProductionAdMobBannerAdUnitId = "ca-app-pub-5085236088670848/2935647536";
        // ID de test Google (affiche toujours une bannière test)
        private const string TestAdMobBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        
#if DEBUG
        private const string AdMobBannerAdUnitId = TestAdMobBannerAdUnitId;
#else
        private const string AdMobBannerAdUnitId = ProductionAdMobBannerAdUnitId;
#endif

        public UnifiedAdBannerHandler() : base(new PropertyMapper<IContentView, IContentViewHandler>(),
            new CommandMapper<IContentView, IContentViewHandler>())
        {
        }

        protected override ContentViewGroup CreatePlatformView()
        {
            var view = base.CreatePlatformView();
            view.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
            view.SetWillNotDraw(true);
            return view;
        }

        protected override void ConnectHandler(ContentViewGroup platformView)
        {
            base.ConnectHandler(platformView);

            // Forcer transparent sur toute la hiérarchie dès le départ
            ForceTransparentRecursive(platformView);

            // Déterminer quelle plateforme utiliser
            _activeProvider = GetActiveAdProvider();

            if (_activeProvider == AdProvider.None)
            {
                HideView(platformView);
                return;
            }

            if (_isInitialized) return;
            _isInitialized = true;
            _isDisposed = false;
            _container = platformView;

            _ = InitializeWithDelayAsync(platformView);
        }

        private static void ForceTransparentRecursive(global::Android.Views.View view)
        {
            view.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
            if (view is ViewGroup vg)
            {
                for (int i = 0; i < vg.ChildCount; i++)
                {
                    ForceTransparentRecursive(vg.GetChildAt(i)!);
                }
            }
        }

        private static AdProvider GetActiveAdProvider()
        {
            // Utiliser AdMob si activé
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
                // Attendre que le SDK soit prêt (max 15s)
                var maxWait = 30;
                for (int i = 0; i < maxWait; i++)
                {
                    if (_isDisposed) return;
                    if (DonTroc.Platforms.Android.AdMobNativeService.IsSdkReady) break;
                    await Task.Delay(500);
                }
                
                // Délai anti-navigation rapide (3 secondes)
                // Si l'utilisateur quitte la page en moins de 3s, on économise une requête
                await Task.Delay(3000);
                
                if (_isDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[UnifiedBanner] 🚫 Chargement annulé (page quittée avant 3s)");
                    return;
                }
                
                MainThread.BeginInvokeOnMainThread(() => InitializeAdView(platformView));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UnifiedAd init error: {ex.Message}");
            }
        }

        private void InitializeAdView(ContentViewGroup container)
        {
            // Vérification : page quittée entre BeginInvokeOnMainThread et exécution
            if (_isDisposed) return;
            
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
                adView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
                adView.Visibility = ViewStates.Invisible; // Masqué jusqu'au chargement

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AdMob init error: {ex.Message}");
                ShowPlaceholder(container, "Erreur AdMob");
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
                placeholder.SetBackgroundColor(global::Android.Graphics.Color.Transparent);

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
            // Marquer comme disposé IMMÉDIATEMENT pour annuler tout chargement en cours
            _isDisposed = true;
            
            try
            {
                if (_adView is AdView adMobView)
                {
                    adMobView.Pause(); // Arrêter l'auto-refresh avant destruction
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
            // Si la page est déjà quittée, ne pas afficher → pas d'impression gaspillée
            if (_isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[UnifiedBanner] 🚫 Bannière chargée mais page quittée — gaspillage évité");
                return;
            }
            
            _isLoaded = true;

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
            AdMob
        }
    }

    /// <summary>
    /// Listener AdMob pour le handler unifié
    /// </summary>
    [global::Android.Runtime.Preserve(AllMembers = true)]
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
            
            // Diagnostic détaillé médiation
            System.Diagnostics.Debug.WriteLine("[UnifiedBanner] ❌ Échec: " + error.Message + " (code=" + error.Code + ")");
            
            var responseInfo = error.ResponseInfo;
            if (responseInfo != null)
            {
                System.Diagnostics.Debug.WriteLine("[UnifiedBanner]    Adapter: " + (responseInfo.MediationAdapterClassName ?? "aucun"));
                var adapterResponses = responseInfo.AdapterResponses;
                if (adapterResponses != null)
                {
                    foreach (var adapter in adapterResponses)
                    {
                        var adapterName = adapter.AdapterClassName ?? "inconnu";
                        var latency = adapter.LatencyMillis;
                        var adError = adapter.AdError?.Message ?? "aucune";
                        System.Diagnostics.Debug.WriteLine(
                            $"[UnifiedBanner]    → {adapterName}: latence={latency}ms, erreur={adError}");
                    }
                }
            }
            
            _handler.OnAdFailedToLoad(error.Message);
        }
    }
}

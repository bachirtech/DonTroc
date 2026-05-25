using System;
using System.Threading.Tasks;
using CoreGraphics;
using DonTroc.Services;
using Foundation;
using Google.MobileAds;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace DonTroc.Platforms.iOS
{
    /// <summary>
    /// Handler natif iOS pour AdBannerView — affiche une vraie bannière Google AdMob.
    /// Remplace le placeholder MAUI par un UIView natif contenant la BannerView AdMob.
    /// </summary>
    public class AdMobBannerHandler : ContentViewHandler
    {
        // ID de production iOS
        private const string BannerAdUnitId = "ca-app-pub-5085236088670848/7822198620";

#if DEBUG
        private const string ActiveBannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
        private const string ActiveBannerAdUnitId = BannerAdUnitId;
#endif

        private BannerView? _bannerView;
        private bool _isDisposed;

        public AdMobBannerHandler()
            : base(new PropertyMapper<Microsoft.Maui.IContentView, IContentViewHandler>(),
                   new CommandMapper<Microsoft.Maui.IContentView, IContentViewHandler>())
        {
        }

        protected override Microsoft.Maui.Platform.ContentView CreatePlatformView()
        {
            var view = base.CreatePlatformView();
            view.BackgroundColor = UIColor.Clear;
            return view;
        }

        protected override void ConnectHandler(Microsoft.Maui.Platform.ContentView platformView)
        {
            base.ConnectHandler(platformView);

            if (!AdMobConfiguration.ADS_ENABLED)
                return;

            // Délai court pour laisser la vue s'intégrer dans la hiérarchie iOS
            Task.Delay(500).ContinueWith(_ =>
            {
                if (_isDisposed) return;
                MainThread.BeginInvokeOnMainThread(() => LoadBanner(platformView));
            });
        }

        private void LoadBanner(Microsoft.Maui.Platform.ContentView platformView)
        {
            if (_isDisposed) return;

            try
            {
                _bannerView = new BannerView(AdSizeCons.Banner)
                {
                    AdUnitId = ActiveBannerAdUnitId,
                    RootViewController = GetRootViewController(),
                    TranslatesAutoresizingMaskIntoConstraints = false
                };

                _bannerView.AdReceived += (_, _) =>
                    System.Diagnostics.Debug.WriteLine("[AdMob iOS Banner] ✅ Bannière chargée");

                _bannerView.ReceiveAdFailed += (_, e) =>
                    System.Diagnostics.Debug.WriteLine($"[AdMob iOS Banner] ❌ Échec : {e.Error.LocalizedDescription}");

                platformView.BackgroundColor = UIColor.Clear;
                platformView.AddSubview(_bannerView);

                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    _bannerView.CenterXAnchor.ConstraintEqualTo(platformView.CenterXAnchor),
                    _bannerView.CenterYAnchor.ConstraintEqualTo(platformView.CenterYAnchor),
                    _bannerView.HeightAnchor.ConstraintEqualTo(50)
                });

                _bannerView.LoadRequest(Request.GetDefaultRequest());
                System.Diagnostics.Debug.WriteLine("[AdMob iOS Banner] Requête bannière envoyée");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdMob iOS Banner] Erreur : {ex.Message}");
            }
        }

        protected override void DisconnectHandler(Microsoft.Maui.Platform.ContentView platformView)
        {
            _isDisposed = true;
            _bannerView?.RemoveFromSuperview();
            _bannerView?.Dispose();
            _bannerView = null;
            base.DisconnectHandler(platformView);
        }

        private static UIViewController? GetRootViewController()
        {
            try
            {
                foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
                {
                    if (scene is UIWindowScene windowScene)
                    {
                        foreach (var window in windowScene.Windows)
                        {
                            if (window.IsKeyWindow)
                                return window.RootViewController;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Ads;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Controls;

namespace DonTroc.Platforms.Android
{
    /// <summary>
    /// Handler natif Android pour AdBannerView
    /// Remplace le contenu MAUI par une vraie bannière AdMob native
    /// </summary>
    public class AdMobBannerHandler : ContentViewHandler
    {
        private AdView? _adView;
        private bool _isLoaded;

        // ID de test pour bannière AdMob (à remplacer par votre vraie ID en production)
        private const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

        protected override ContentViewGroup CreatePlatformView()
        {
            var container = base.CreatePlatformView();
            LoadAdMobBanner(container);
            return container;
        }

        private void LoadAdMobBanner(ContentViewGroup container)
        {
            try
            {
                var context = container.Context;
                if (context == null) return;

                // Créer la vue publicitaire
                _adView = new AdView(context)
                {
                    AdUnitId = BannerAdUnitId,
                    AdSize = AdSize.Banner
                };

                // Configurer les callbacks
                _adView.AdListener = new AdMobBannerListener(this);

                // Créer et charger la requête publicitaire
                var adRequest = new AdRequest.Builder().Build();
                _adView.LoadAd(adRequest);

                // Ajouter la vue à notre container
                container.AddView(_adView);

                System.Diagnostics.Debug.WriteLine("✅ Bannière AdMob créée et chargement en cours...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Erreur création bannière AdMob: " + ex.Message);
            }
        }

        protected override void DisconnectHandler(ContentViewGroup platformView)
        {
            try
            {
                if (_adView != null)
                {
                    _adView.Destroy();
                    _adView = null;
                    System.Diagnostics.Debug.WriteLine("🗑️ Bannière AdMob détruite");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Erreur destruction bannière: " + ex.Message);
            }

            base.DisconnectHandler(platformView);
        }

        internal void OnAdLoaded()
        {
            _isLoaded = true;
            System.Diagnostics.Debug.WriteLine("✅ Bannière AdMob chargée avec succès");
        }

        internal void OnAdFailedToLoad(LoadAdError error)
        {
            _isLoaded = false;
            System.Diagnostics.Debug.WriteLine("❌ Échec chargement bannière AdMob: " + error.Message);
            
            // Retry après 30 secondes en cas d'échec
            Task.Delay(30000).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        if (_adView != null && !_isLoaded)
                        {
                            var adRequest = new AdRequest.Builder().Build();
                            _adView.LoadAd(adRequest);
                            System.Diagnostics.Debug.WriteLine("🔄 Retry chargement bannière AdMob");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Erreur retry bannière: " + ex.Message);
                    }
                });
            });
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

        public override void OnAdClicked()
        {
            base.OnAdClicked();
            System.Diagnostics.Debug.WriteLine("👆 Bannière AdMob cliquée");
        }

        public override void OnAdClosed()
        {
            base.OnAdClosed();
            System.Diagnostics.Debug.WriteLine("❌ Bannière AdMob fermée");
        }

        public override void OnAdOpened()
        {
            base.OnAdOpened();
            System.Diagnostics.Debug.WriteLine("📖 Bannière AdMob ouverte");
        }
    }
}

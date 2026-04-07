using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views
{
    /// <summary>
    /// Vue de bannière publicitaire unifiée pour AdMob
    /// </summary>
    public class UnifiedAdBannerView : ContentView
    {
        private Border? _bannerContainer;
        private Label? _bannerLabel;

        public UnifiedAdBannerView()
        {
            // Déterminer quelle plateforme publicitaire utiliser
            var adProvider = GetActiveAdProvider();

            if (adProvider == AdProvider.None)
            {
                // Aucune pub active - rendre invisible
                IsVisible = false;
                HeightRequest = 0;
                System.Diagnostics.Debug.WriteLine("📱 UnifiedAdBanner: Aucune pub active");
                return;
            }

            // Dimensions standard bannière (320x50 dp)
            HeightRequest = 50;
            MinimumHeightRequest = 50;
            WidthRequest = 320;
            MinimumWidthRequest = 320;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Start;
            BackgroundColor = Colors.Transparent;

            System.Diagnostics.Debug.WriteLine($"📱 UnifiedAdBanner: Provider actif = {adProvider}");

#if !ANDROID
            // Sur les autres plateformes, afficher un placeholder
            LoadPlaceholder(adProvider.ToString());
#endif
        }

        /// <summary>
        /// Détermine quelle plateforme publicitaire utiliser
        /// </summary>
        private static AdProvider GetActiveAdProvider()
        {
            // Utiliser AdMob si activé
            if (Services.AdMobConfiguration.ADS_ENABLED)
            {
                return AdProvider.AdMob;
            }

            // Aucune pub active
            return AdProvider.None;
        }

        /// <summary>
        /// Charge un placeholder pour les plateformes non-Android
        /// </summary>
        private void LoadPlaceholder(string providerName)
        {
            _bannerLabel = new Label
            {
                Text = $"Publicité ({providerName})",
                FontSize = 12,
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            _bannerContainer = new Border
            {
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                HeightRequest = 50,
                WidthRequest = 320,
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#E0E0E0"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = _bannerLabel,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) }
            };

            Content = _bannerContainer;
            IsVisible = true;
        }

        /// <summary>
        /// Énumération des fournisseurs de publicités supportés
        /// </summary>
        private enum AdProvider
        {
            None,
            AdMob
        }
    }
}

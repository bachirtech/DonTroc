using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views
{
    /// <summary>
    /// Vue personnalisée pour afficher les bannières publicitaires AdMob
    /// Sur Android, un handler natif remplacera automatiquement ce contenu par une vraie bannière AdMob
    /// </summary>
    public class AdBannerView : ContentView
    {
        private Border? _bannerContainer;
        private Label? _bannerLabel;

        /// <summary>
        /// Indique si les bannières doivent être affichées
        /// </summary>
        public static bool ShouldShowBanners => DonTroc.Services.AdMobConfiguration.ADS_ENABLED;

        public AdBannerView()
        {
            if (!ShouldShowBanners)
            {
                IsVisible = false;
                HeightRequest = 0;
                return;
            }

            // Dimensions standard bannière AdMob (320x50 dp)
            HeightRequest = 50;
            MinimumHeightRequest = 50;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Start;
            BackgroundColor = Colors.Transparent;

#if !ANDROID
            LoadPlaceholder();
#endif
        }

        private void LoadPlaceholder()
        {
            _bannerLabel = new Label
            {
                Text = "Chargement publicité...",
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
    }
}
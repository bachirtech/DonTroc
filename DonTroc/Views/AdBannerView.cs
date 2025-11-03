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

        public AdBannerView()
        {
            // Définir des dimensions fixes pour la bannière AdMob standard
            HeightRequest = 50;
            WidthRequest = 320;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            
            LoadPlaceholder();
        }

        /// <summary>
        /// Charge un placeholder temporaire
        /// Sur Android, ce contenu sera remplacé par le handler natif AdMobBannerHandler
        /// </summary>
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

            System.Diagnostics.Debug.WriteLine("📱 AdBannerView créée (sera remplacée par une vraie pub sur Android)");
        }
    }
}

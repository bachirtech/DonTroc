using System;
using System.Threading.Tasks;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.Views
{
    /// <summary>
    /// Vue d'overlay pour afficher les conseils et infobulles
    /// </summary>
    public partial class TipOverlayView : ContentView
    {
        private ITipsService? _tipsService;
        private string _currentFeatureKey = string.Empty;
        private TipDisplayConfig? _currentConfig;
        private TaskCompletionSource<bool>? _dismissTaskSource;

        /// <summary>
        /// Événement déclenché quand le conseil est fermé
        /// </summary>
        public event EventHandler<TipClosedEventArgs>? TipClosed;

        /// <summary>
        /// Événement déclenché quand l'utilisateur demande à ne plus voir ce conseil
        /// </summary>
        public event EventHandler<string>? TipDismissedPermanently;

        public TipOverlayView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Affiche un conseil pour une fonctionnalité donnée
        /// </summary>
        /// <param name="featureKey">Clé de la fonctionnalité</param>
        /// <param name="tipsService">Service de conseils (optionnel, utilise DI sinon)</param>
        /// <returns>True si un conseil a été affiché</returns>
        public async Task<bool> ShowTipAsync(string featureKey, ITipsService? tipsService = null)
        {
            try
            {
                _tipsService = tipsService ?? GetTipsService();
                if (_tipsService == null)
                {
                    return false;
                }

                _currentFeatureKey = featureKey;
                _currentConfig = await _tipsService.GetNextTipAsync(featureKey);

                if (_currentConfig == null)
                {
                    return false;
                }

                UpdateUI(_currentConfig);
                await AnimateShowAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'affichage du conseil: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Affiche un conseil spécifique
        /// </summary>
        public async Task ShowSpecificTipAsync(Tip tip)
        {
            _currentConfig = new TipDisplayConfig
            {
                Tip = tip,
                HasMoreTips = false,
                CurrentIndex = 1,
                TotalTips = 1
            };

            UpdateUI(_currentConfig);
            await AnimateShowAsync();
        }

        /// <summary>
        /// Ferme le conseil actuel
        /// </summary>
        public async Task HideAsync()
        {
            await AnimateHideAsync();
            _dismissTaskSource?.TrySetResult(true);
        }

        /// <summary>
        /// Attend que le conseil soit fermé
        /// </summary>
        public Task WaitForDismissAsync()
        {
            _dismissTaskSource = new TaskCompletionSource<bool>();
            return _dismissTaskSource.Task;
        }

        private ITipsService? GetTipsService()
        {
            try
            {
                return Application.Current?.Handler?.MauiContext?.Services.GetService<ITipsService>();
            }
            catch
            {
                return null;
            }
        }

        private void UpdateUI(TipDisplayConfig config)
        {
            var tip = config.Tip;

            IconLabel.Text = tip.Icon;
            TitleLabel.Text = tip.Title;
            MessageLabel.Text = tip.Message;

            // Afficher la progression si plusieurs conseils
            if (config.TotalTips > 1)
            {
                ProgressLabel.Text = $"{config.CurrentIndex}/{config.TotalTips}";
                ProgressLabel.IsVisible = true;
            }
            else
            {
                ProgressLabel.IsVisible = false;
            }

            // Texte du bouton selon s'il y a d'autres conseils
            NextButton.Text = config.HasMoreTips ? "Suivant →" : "Compris !";

            // Positionner la carte selon la position spécifiée
            TipCard.VerticalOptions = tip.Position switch
            {
                TipPosition.Top => LayoutOptions.Start,
                TipPosition.Center => LayoutOptions.Center,
                TipPosition.Bottom => LayoutOptions.End,
                _ => LayoutOptions.Center
            };

            // Ajuster la marge selon la position
            TipCard.Margin = tip.Position switch
            {
                TipPosition.Top => new Thickness(20, 60, 20, 20),
                TipPosition.Bottom => new Thickness(20, 20, 20, 60),
                _ => new Thickness(20)
            };
        }

        private async Task AnimateShowAsync()
        {
            this.IsVisible = true;
            this.InputTransparent = false;

            // Animation d'entrée
            var backgroundAnimation = BackgroundOverlay.FadeTo(1, 250, Easing.CubicOut);
            var cardFadeAnimation = TipCard.FadeTo(1, 300, Easing.CubicOut);
            var cardScaleAnimation = TipCard.ScaleTo(1, 300, Easing.SpringOut);

            await Task.WhenAll(backgroundAnimation, cardFadeAnimation, cardScaleAnimation);
        }

        private async Task AnimateHideAsync()
        {
            // Animation de sortie
            var backgroundAnimation = BackgroundOverlay.FadeTo(0, 200, Easing.CubicIn);
            var cardFadeAnimation = TipCard.FadeTo(0, 200, Easing.CubicIn);
            var cardScaleAnimation = TipCard.ScaleTo(0.8, 200, Easing.CubicIn);

            await Task.WhenAll(backgroundAnimation, cardFadeAnimation, cardScaleAnimation);

            this.IsVisible = false;
            this.InputTransparent = true;
        }

        private async void OnNextClicked(object? sender, EventArgs e)
        {
            if (_currentConfig != null && _tipsService != null)
            {
                // Marquer le conseil comme vu
                await _tipsService.MarkTipAsSeenAsync(_currentConfig.Tip.Id);

                // Si d'autres conseils sont disponibles, afficher le suivant
                if (_currentConfig.HasMoreTips)
                {
                    _currentConfig = await _tipsService.GetNextTipAsync(_currentFeatureKey);
                    if (_currentConfig != null)
                    {
                        // Animation de transition
                        await TipCard.FadeTo(0, 150);
                        UpdateUI(_currentConfig);
                        await TipCard.FadeTo(1, 150);
                        return;
                    }
                }
            }

            // Fermer l'overlay
            await HideAsync();
            TipClosed?.Invoke(this, new TipClosedEventArgs(_currentConfig?.Tip.Id ?? "", false));
        }

        private async void OnDismissClicked(object? sender, EventArgs e)
        {
            if (_currentConfig != null && _tipsService != null)
            {
                // Ignorer définitivement ce conseil
                await _tipsService.DismissTipAsync(_currentConfig.Tip.Id);
                TipDismissedPermanently?.Invoke(this, _currentConfig.Tip.Id);
            }

            await HideAsync();
            TipClosed?.Invoke(this, new TipClosedEventArgs(_currentConfig?.Tip.Id ?? "", true));
        }

        private async void OnBackgroundTapped(object? sender, TappedEventArgs e)
        {
            // Fermer en tapant sur le fond
            if (_currentConfig != null && _tipsService != null)
            {
                await _tipsService.MarkTipAsSeenAsync(_currentConfig.Tip.Id);
            }

            await HideAsync();
            TipClosed?.Invoke(this, new TipClosedEventArgs(_currentConfig?.Tip.Id ?? "", false));
        }
    }

    /// <summary>
    /// Arguments de l'événement TipClosed
    /// </summary>
    public class TipClosedEventArgs : EventArgs
    {
        public string TipId { get; }
        public bool DismissedPermanently { get; }

        public TipClosedEventArgs(string tipId, bool dismissedPermanently)
        {
            TipId = tipId;
            DismissedPermanently = dismissedPermanently;
        }
    }
}


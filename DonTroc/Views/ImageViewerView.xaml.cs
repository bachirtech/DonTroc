using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Maui;

namespace DonTroc.Views
{
    [QueryProperty(nameof(ImageUrls), "imageUrls")]
    public partial class ImageViewerView : ContentPage
    {
        // Collection pour stocker les URLs des images à afficher dans le carrousel
        public ObservableCollection<string> DisplayableImageUrls { get; } = new ObservableCollection<string>();

        private string? _imageUrls;
        public string? ImageUrls
        {
            get => _imageUrls;
            set
            {
                _imageUrls = value;
                // Une fois que les URLs sont reçues, on les traite
                ProcessImageUrls();
            }
        }

        public ImageViewerView()
        {
            InitializeComponent();
            // Le contexte de liaison est la page elle-même pour accéder à DisplayableImageUrls
            BindingContext = this;
        }

        private void ProcessImageUrls()
        {
            DisplayableImageUrls.Clear();

            if (string.IsNullOrWhiteSpace(_imageUrls))
            {
                Debug.WriteLine("[ImageViewerView] Aucune URL d'image reçue.");
                return;
            }

            // Sépare la chaîne d'URLs en une liste
            var urls = _imageUrls.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var url in urls)
            {
                // On s'assure que l'URL est valide avant de l'ajouter
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    DisplayableImageUrls.Add(url);
                }
                else
                {
                    Debug.WriteLine($"[ImageViewerView] URL d'image invalide ignorée : {url}");
                }
            }

            // Lier la source du carrousel à notre collection d'URLs
            ImagesCarousel.ItemsSource = DisplayableImageUrls;
        }

        /// <summary>
        /// Gère le geste de pincement pour zoomer sur l'image.
        /// </summary>
        private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
        {
            if (sender is not Image image) return;

            switch (e.Status)
            {
                case GestureStatus.Started:
                    // Stocker l'échelle de départ
                    _startScale = image.Scale;
                    image.AnchorX = e.ScaleOrigin.X;
                    image.AnchorY = e.ScaleOrigin.Y;
                    break;
                case GestureStatus.Running:
                    // Calculer la nouvelle échelle
                    var currentScale = _startScale * e.Scale;
                    // Limiter le zoom entre 1x et 5x
                    image.Scale = Math.Clamp(currentScale, 1, 5);
                    break;
                case GestureStatus.Completed:
                    // Optionnel : réinitialiser si nécessaire
                    break;
            }
        }
        private double _startScale;

        /// <summary>
        /// Gère le clic sur le bouton de fermeture pour revenir à la page précédente.
        /// </summary>
        private async void OnCloseButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageViewerView] Erreur lors de la fermeture: {ex.Message}");
            }
        }
    }
}

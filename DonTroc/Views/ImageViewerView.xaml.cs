using System.Collections.ObjectModel;

namespace DonTroc.Views
{
    public partial class ImageViewerView : ContentPage, IQueryAttributable
    {
        // Collection pour stocker les URLs des images à afficher dans le carrousel
        public ObservableCollection<string> DisplayableImageUrls { get; } = new ObservableCollection<string>();

        private string? _imageUrls;

        public ImageViewerView()
        {
            InitializeComponent();
            // Le contexte de liaison est la page elle-même pour accéder à DisplayableImageUrls
            BindingContext = this;
            
            // Écouter les changements de position du carrousel
            ImagesCarousel.PositionChanged += OnCarouselPositionChanged;
        }
        
        /// <summary>
        /// Méthode appelée automatiquement par MAUI Shell pour recevoir les paramètres de navigation
        /// </summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("imageUrls", out var imageUrlsParam))
            {
                _imageUrls = imageUrlsParam?.ToString();
                MainThread.BeginInvokeOnMainThread(() => ProcessImageUrls());
            }
        }

        private void ProcessImageUrls()
        {
            try
            {
                DisplayableImageUrls.Clear();

                if (string.IsNullOrWhiteSpace(_imageUrls))
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                    return;
                }

                // Tenter de décoder si le paramètre a été encodé (compatibilité)
                string processedParam = _imageUrls;
                if (processedParam.Contains("%"))
                {
                    try
                    {
                        processedParam = Uri.UnescapeDataString(_imageUrls);
                    }
                    catch
                    {
                        // Garder la valeur originale si le décodage échoue
                    }
                }

                // Sépare la chaîne d'URLs en une liste (supporte | et , comme séparateurs)
                var urls = processedParam.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var url in urls)
                {
                    var trimmedUrl = url.Trim();

                    // On s'assure que l'URL est valide avant de l'ajouter
                    if (!string.IsNullOrEmpty(trimmedUrl) && 
                        (trimmedUrl.StartsWith("http://") || trimmedUrl.StartsWith("https://")))
                    {
                        DisplayableImageUrls.Add(trimmedUrl);
                    }
                }

                // Masquer l'indicateur de chargement et mettre à jour le compteur
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                
                if (DisplayableImageUrls.Count > 0)
                {
                    UpdateCounter(0);
                    ImageCounter.IsVisible = DisplayableImageUrls.Count > 1;
                }
                else
                {
                    ImageCounter.IsVisible = false;
                }
            }
            catch (Exception)
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
        {
            UpdateCounter(e.CurrentPosition);
        }

        private void UpdateCounter(int position)
        {
            if (DisplayableImageUrls.Count > 1)
            {
                CounterLabel.Text = $"{position + 1} / {DisplayableImageUrls.Count}";
            }
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
                    _startScale = image.Scale;
                    image.AnchorX = e.ScaleOrigin.X;
                    image.AnchorY = e.ScaleOrigin.Y;
                    break;
                case GestureStatus.Running:
                    var currentScale = _startScale * e.Scale;
                    image.Scale = Math.Clamp(currentScale, 1, 5);
                    break;
                case GestureStatus.Completed:
                    // Réinitialiser le zoom en douceur si trop petit
                    if (image.Scale < 1.1)
                    {
                        image.Scale = 1;
                    }
                    break;
            }
        }
        private double _startScale = 1;

        /// <summary>
        /// Gère le clic sur le bouton de fermeture pour revenir à la page précédente.
        /// </summary>
        private async void OnCloseButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch
            {
                try
                {
                    await Navigation.PopAsync();
                }
                catch
                {
                    await Shell.Current.GoToAsync("//AnnoncesView");
                }
            }
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}

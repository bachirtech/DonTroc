using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using DonTroc.ViewModels;
using DonTroc.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Collections.Specialized;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace DonTroc.Views
{
    public partial class MapView : ContentPage
    {
        private MapViewModel _viewModel;
        private bool _isUpdatingPins;
        private bool _eventsInitialized;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MapView))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MapViewModel))]
        public MapView(MapViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // Position initiale par défaut pour que la carte s'affiche immédiatement
            try
            {
                var defaultLocation = new Location(46.2276, 2.2137);
                GoogleMap.MoveToRegion(MapSpan.FromCenterAndRadius(defaultLocation, Distance.FromKilometers(500)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Erreur position initiale: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                // 🎬 Fade-in élégant de la carte au démarrage (au lieu d'un pop brutal)
                if (GoogleMap != null && GoogleMap.Opacity < 0.99)
                {
                    GoogleMap.Opacity = 0;
                    GoogleMap.Scale = 0.97;
                    _ = Task.WhenAll(
                        GoogleMap.FadeTo(1, 500, Easing.CubicOut),
                        GoogleMap.ScaleTo(1, 600, Easing.CubicOut)
                    );
                }

                // Attacher les événements une seule fois pour éviter les doublons
                if (!_eventsInitialized)
                {
                    InitializeMapEvents();
                    _eventsInitialized = true;
                }

                // Initialiser le ViewModel — les pins seront mis à jour via les événements PropertyChanged
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapView] Erreur OnAppearing: {ex.Message}");
            }
        }

        private void InitializeMapEvents()
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (_viewModel.NearbyAnnouncements != null)
            {
                _viewModel.NearbyAnnouncements.CollectionChanged += OnAnnouncementsChanged;
            }

            GoogleMap.MapClicked += OnMapClicked;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MapViewModel.UserLocation):
                    // Forcer l'exécution sur le MainThread (critique en Release/AOT)
                    MainThread.BeginInvokeOnMainThread(UpdateMapCenter);
                    break;
                case nameof(MapViewModel.NearbyAnnouncements):
                    // Réabonner à la nouvelle collection
                    if (_viewModel.NearbyAnnouncements != null)
                    {
                        _viewModel.NearbyAnnouncements.CollectionChanged -= OnAnnouncementsChanged;
                        _viewModel.NearbyAnnouncements.CollectionChanged += OnAnnouncementsChanged;
                    }
                    MainThread.BeginInvokeOnMainThread(UpdateMapPins);
                    break;
            }
        }

        private void OnAnnouncementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Rafraîchir les pins seulement pour un Reset ou Add
            if (e.Action == NotifyCollectionChangedAction.Reset || 
                e.Action == NotifyCollectionChangedAction.Add)
            {
                MainThread.BeginInvokeOnMainThread(UpdateMapPins);
            }
        }

        private void OnMapClicked(object? sender, MapClickedEventArgs e)
        {
            _viewModel.OnMapLocationSelected(e.Location.Latitude, e.Location.Longitude);
        }

        private void UpdateMapCenter()
        {
            if (_viewModel.UserLocation == null) return;
            
            try
            {
                var location = new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude);
                
                // Adapter le zoom selon le rayon de recherche
                var radiusKm = _viewModel.SearchRadius;
                var mapDistance = Distance.FromKilometers(Math.Max(radiusKm * 1.5, 2));
                
                GoogleMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, mapDistance));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Erreur UpdateMapCenter: {ex.Message}");
            }
        }

        private void UpdateMapPins()
        {
            // Éviter les appels multiples simultanés
            if (_isUpdatingPins) return;
            _isUpdatingPins = true;
            
            try
            {
                GoogleMap.Pins.Clear();

                // Copier la collection pour éviter les race conditions 
                // (la collection peut changer pendant l'itération en Release)
                var announcements = _viewModel.NearbyAnnouncements?.ToList();
                if (announcements == null) return;

                foreach (var annonce in announcements)
                {
                    if (!annonce.Latitude.HasValue || !annonce.Longitude.HasValue)
                        continue;
                    if (annonce.Latitude == 0 && annonce.Longitude == 0)
                        continue;

                    var distanceText = FormatDistance(annonce.DistanceFromUser);
                    var descSnippet = annonce.Description != null
                        ? annonce.Description.Substring(0, Math.Min(annonce.Description.Length, 50))
                        : "";
                    
                    var pin = new Pin
                    {
                        Label = annonce.Titre ?? "Annonce",
                        Address = $"{distanceText} — {descSnippet}",
                        Type = PinType.Place,
                        Location = new Location(annonce.Latitude.Value, annonce.Longitude.Value)
                    };

                    pin.MarkerClicked += (s, args) => OnPinClicked(annonce);
                    GoogleMap.Pins.Add(pin);
                }

                // Pin position utilisateur
                if (_viewModel.UserLocation != null)
                {
                    var userPin = new Pin
                    {
                        Label = "📍 Votre position",
                        Address = _viewModel.CurrentLocationText,
                        Type = PinType.Generic,
                        Location = new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude)
                    };
                    GoogleMap.Pins.Add(userPin);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Erreur UpdateMapPins: {ex.Message}");
            }
            finally
            {
                _isUpdatingPins = false;
            }
        }

        private async void OnPinClicked(Annonce annonce)
        {
            string distanceText = FormatDistance(annonce.DistanceFromUser);

            var action = await DisplayActionSheet(
                $"{annonce.Titre}",
                "Fermer",
                null,
                "Voir les détails",
                "Contacter",
                $"Distance: {distanceText}"
            );

            switch (action)
            {
                case "Voir les détails":
                    await Shell.Current.GoToAsync($"//AnnoncesView?annonceId={annonce.Id}");
                    break;
                case "Contacter":
                    // Vérifier que le GPS est activé avant de permettre le contact
                    if (_viewModel.UserLocation == null)
                    {
                        await DisplayAlert("GPS requis 📍",
                            "Vous devez activer votre GPS pour pouvoir contacter un annonceur.\n\n" +
                            "Cela permet de :\n" +
                            "• Vérifier que vous êtes à proximité\n" +
                            "• Garantir des échanges locaux\n" +
                            "• Assurer la sécurité des utilisateurs\n\n" +
                            "Veuillez activer le GPS dans les paramètres de votre appareil et réessayer.",
                            "Compris");
                        return;
                    }
                    await Shell.Current.GoToAsync($"//ConversationsView?userId={annonce.UtilisateurId}");
                    break;
            }
        }

        private string FormatDistance(double? distance)
        {
            if (!distance.HasValue || distance == double.MaxValue)
                return "Distance inconnue";

            if (distance < 1)
                return $"{(int)(distance * 1000)} m";

            return $"{distance:F1} km";
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Ne PAS détacher les événements ici — ils sont gérés par _eventsInitialized
            // Détacher causerait des problèmes quand la page revient (tab navigation)
        }
    }
}
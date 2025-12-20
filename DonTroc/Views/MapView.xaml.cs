using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using DonTroc.ViewModels;
using DonTroc.Models;
using System;
using System.Collections.Specialized;
using Microsoft.Maui.Devices.Sensors;

namespace DonTroc.Views
{
    public partial class MapView : ContentPage
    {
        private MapViewModel _viewModel;

        public MapView(MapViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // Initialiser les événements et la carte
            InitializeMapEvents();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initialiser le ViewModel quand la page apparaît
            await _viewModel.InitializeAsync();

            // Mettre à jour les pins sur la carte
            UpdateMapPins();
        }

        private void InitializeMapEvents()
        {
            // S'abonner aux changements du ViewModel
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // S'abonner aux changements de la collection d'annonces
            if (_viewModel.NearbyAnnouncements != null)
            {
                _viewModel.NearbyAnnouncements.CollectionChanged += OnAnnouncementsChanged;
            }

            // Événement de clic sur la carte
            GoogleMap.MapClicked += OnMapClicked;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MapViewModel.UserLocation):
                    UpdateMapCenter();
                    break;
                case nameof(MapViewModel.NearbyAnnouncements):
                    UpdateMapPins();
                    // S'abonner aux nouveaux changements de collection
                    if (_viewModel.NearbyAnnouncements != null)
                    {
                        _viewModel.NearbyAnnouncements.CollectionChanged += OnAnnouncementsChanged;
                    }

                    break;
            }
        }

        private void OnAnnouncementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Mettre à jour les pins quand la collection change
            UpdateMapPins();
        }

        private void OnMapClicked(object? sender, MapClickedEventArgs e)
        {
            // Notifier le ViewModel de la nouvelle position sélectionnée
            _viewModel.OnMapLocationSelected(e.Location.Latitude, e.Location.Longitude);
        }

        private void UpdateMapCenter()
        {
            if (_viewModel.UserLocation != null)
            {
                var location = new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude);
                var mapSpan = new MapSpan(location, 0.01, 0.01); // Zoom proche
                GoogleMap.MoveToRegion(mapSpan);
            }
        }

        private void UpdateMapPins()
        {
            // Vider les pins existants
            GoogleMap.Pins.Clear();

            // Ajouter des pins pour chaque annonce
            foreach (var annonce in _viewModel.NearbyAnnouncements)
            {
                // Vérifier que les coordonnées sont valides et non nulles
                if (!annonce.Latitude.HasValue || !annonce.Longitude.HasValue)
                    continue;

                if (annonce.Latitude == 0 && annonce.Longitude == 0)
                    continue;

                var pin = new Pin
                {
                    Label = annonce.Titre,
                    Address = annonce.Description,
                    Type = PinType.Place,
                    Location = new Location(annonce.Latitude.Value, annonce.Longitude.Value)
                };

                // Personnaliser l'apparence selon le type d'annonce
                pin.MarkerClicked += (s, args) => OnPinClicked(annonce);

                GoogleMap.Pins.Add(pin);
            }

            // Ajouter un pin pour la position utilisateur si disponible
            if (_viewModel.UserLocation != null)
            {
                var userPin = new Pin
                {
                    Label = "Votre position",
                    Address = _viewModel.CurrentLocationText,
                    Type = PinType.Generic,
                    Location = new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude)
                };

                GoogleMap.Pins.Add(userPin);
            }
        }

        private async void OnPinClicked(Annonce annonce)
        {
            // Créer le texte de distance formaté
            string distanceText = FormatDistance(annonce.DistanceFromUser);

            // Afficher les détails de l'annonce dans une popup
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
                    // Naviguer vers les détails de l'annonce
                    await Shell.Current.GoToAsync($"//AnnoncesView?annonceId={annonce.Id}");
                    break;
                case "Contacter":
                    // Naviguer vers le chat
                    await Shell.Current.GoToAsync($"//ConversationsView?userId={annonce.UtilisateurId}");
                    break;
            }
        }

        /// <summary>
        /// Formate la distance pour l'affichage
        /// </summary>
        private string FormatDistance(double? distance)
        {
            if (!distance.HasValue || distance == double.MaxValue)
                return "Distance inconnue";

            if (distance < 1)
                return $"{(int)(distance * 1000)} m"; // Afficher en mètres si < 1km

            return $"{distance:F1} km";
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Se désabonner des événements pour éviter les fuites mémoire
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

                if (_viewModel.NearbyAnnouncements != null)
                {
                    _viewModel.NearbyAnnouncements.CollectionChanged -= OnAnnouncementsChanged;
                }
            }

            GoogleMap.MapClicked -= OnMapClicked;
        }
    }
}
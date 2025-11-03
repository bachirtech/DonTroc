using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using DonTroc.Models;
using DonTroc.Services;

namespace DonTroc.ViewModels
{
    public class MapViewModel : BaseViewModel
    {
        private readonly GeolocationService _geolocationService;
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;

        // === PROPRIÉTÉS DE GÉOLOCALISATION ===
        
        private Location? _userLocation;
        public Location? UserLocation
        {
            get => _userLocation;
            set
            {
                if (SetProperty(ref _userLocation, value))
                {
                    HasUserLocation = value != null;
                    UpdateLocationText();
                }
            }
        }

        private bool _hasUserLocation;
        public bool HasUserLocation
        {
            get => _hasUserLocation;
            set => SetProperty(ref _hasUserLocation, value);
        }

        private string _searchAddress = string.Empty;
        public string SearchAddress
        {
            get => _searchAddress;
            set => SetProperty(ref _searchAddress, value);
        }

        private string _currentLocationText = "Localisation inconnue";
        public string CurrentLocationText
        {
            get => _currentLocationText;
            set => SetProperty(ref _currentLocationText, value);
        }

        // === PROPRIÉTÉS DE FILTRAGE ===
        
        private double _searchRadius = 10.0;
        public double SearchRadius
        {
            get => _searchRadius;
            set
            {
                if (SetProperty(ref _searchRadius, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private ObservableCollection<Annonce> _nearbyAnnouncements = new();
        public ObservableCollection<Annonce> NearbyAnnouncements
        {
            get => _nearbyAnnouncements;
            set => SetProperty(ref _nearbyAnnouncements, value);
        }

        // === COMMANDES ===
        
        public ICommand SearchCommand { get; }
        public ICommand LocationCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand BackCommand { get; }

        public MapViewModel(GeolocationService geolocationService, 
                          FirebaseService firebaseService, 
                          AuthService authService)
        {
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _firebaseService = firebaseService ?? throw new ArgumentNullException(nameof(firebaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Initialiser les collections
            NearbyAnnouncements = new ObservableCollection<Annonce>();
            
            // Initialiser les commandes
            SearchCommand = new Command(async () => await SearchLocationAsync());
            LocationCommand = new Command(async () => await GetCurrentLocationAsync());
            FilterCommand = new Command(async () => await ShowFiltersAsync());
            RefreshCommand = new Command(async () => await RefreshMapAsync());
            BackCommand = new Command(OnBack);

            // Initialiser les valeurs par défaut
            CurrentLocationText = "Recherche de votre position...";
            HasUserLocation = false;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                
                // Charger les annonces à proximité par défaut
                await LoadNearbyAnnouncementsAsync();
                
                // Essayer d'obtenir la position actuelle
                await GetCurrentLocationAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation de la carte: {ex.Message}");
                CurrentLocationText = "Erreur lors de l'initialisation";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchLocationAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchAddress))
                return;

            try
            {
                IsBusy = true;
                CurrentLocationText = $"Recherche de '{SearchAddress}'...";

                // Utiliser le service de géocodage intégré à MAUI
                var locations = await Geocoding.GetLocationsAsync(SearchAddress);
                var location = locations?.FirstOrDefault();
                
                if (location != null)
                {
                    UserLocation = location;
                    CurrentLocationText = $"Trouvé: {SearchAddress}";
                    
                    // Recharger les annonces pour cette nouvelle position
                    await LoadNearbyAnnouncementsAsync();
                }
                else
                {
                    // Si aucun résultat trouvé, utiliser une position centrale en France
                    UserLocation = new Location(46.2276, 2.2137); // Centre de la France
                    CurrentLocationText = $"'{SearchAddress}' introuvable, position par défaut";
                    await LoadNearbyAnnouncementsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la recherche: {ex.Message}");
                CurrentLocationText = "Erreur lors de la recherche";
                
                // Position de fallback
                UserLocation = new Location(46.2276, 2.2137);
                await LoadNearbyAnnouncementsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                IsBusy = true;
                CurrentLocationText = "Localisation en cours...";

                var location = await _geolocationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    UserLocation = location;
                    CurrentLocationText = "Position actuelle";
                    await LoadNearbyAnnouncementsAsync();
                }
                else
                {
                    // Position par défaut si la géolocalisation échoue
                    UserLocation = new Location(45.5, 2.5);
                    CurrentLocationText = "Position simulée";
                    await LoadNearbyAnnouncementsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la géolocalisation: {ex.Message}");
                CurrentLocationText = "Erreur de géolocalisation";
                
                // Position par défaut en cas d'erreur
                UserLocation = new Location(45.5, 2.5);
                await LoadNearbyAnnouncementsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShowFiltersAsync()
        {
            try
            {
                // Afficher une boîte de dialogue pour les filtres
                if (Application.Current?.MainPage != null)
                {
                    var result = await Application.Current.MainPage.DisplayActionSheet(
                        "Filtrer par distance",
                        "Annuler",
                        null,
                        "5 km", "10 km", "20 km", "50 km");

                    if (result != null && result != "Annuler")
                    {
                        switch (result)
                        {
                            case "5 km":
                                SearchRadius = 5.0;
                                break;
                            case "10 km":
                                SearchRadius = 10.0;
                                break;
                            case "20 km":
                                SearchRadius = 20.0;
                                break;
                            case "50 km":
                                SearchRadius = 50.0;
                                break;
                        }

                        await LoadNearbyAnnouncementsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'affichage des filtres: {ex.Message}");
            }
        }

        private async Task RefreshMapAsync()
        {
            try
            {
                IsBusy = true;
                CurrentLocationText = "Actualisation...";
                
                await LoadNearbyAnnouncementsAsync();
                
                if (UserLocation != null)
                {
                    CurrentLocationText = "Carte actualisée";
                }
                else
                {
                    CurrentLocationText = "Position inconnue";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'actualisation: {ex.Message}");
                CurrentLocationText = "Erreur lors de l'actualisation";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadNearbyAnnouncementsAsync()
        {
            try
            {
                IsBusy = true;
                
                // Vérifier l'authentification
                var isAuth = await _authService.EnsureAuthenticatedAsync();
                if (!isAuth)
                {
                    System.Diagnostics.Debug.WriteLine("[MapViewModel] Utilisateur non authentifié");
                    return;
                }

                // Charger toutes les annonces depuis Firebase
                var allAnnouncements = await _firebaseService.GetAnnoncesAsync();
                
                if (allAnnouncements == null || !allAnnouncements.Any())
                {
                    NearbyAnnouncements.Clear();
                    return;
                }

                // Filtrer les annonces selon la position et le rayon
                var filteredAnnouncements = new List<Annonce>();
                
                foreach (var annonce in allAnnouncements)
                {
                    // Ignorer les annonces sans coordonnées
                    if (!annonce.Latitude.HasValue || !annonce.Longitude.HasValue ||
                        annonce.Latitude == 0 || annonce.Longitude == 0)
                        continue;

                    // Calculer la distance si on a la position utilisateur
                    if (UserLocation != null)
                    {
                        var distance = _geolocationService.CalculateDistance(
                            UserLocation.Latitude, 
                            UserLocation.Longitude,
                            annonce.Latitude.Value, 
                            annonce.Longitude.Value);
                        
                        annonce.DistanceFromUser = distance;
                        
                        // Filtrer par rayon de recherche
                        if (distance <= SearchRadius)
                        {
                            filteredAnnouncements.Add(annonce);
                        }
                    }
                    else
                    {
                        // Si pas de position utilisateur, inclure toutes les annonces avec coordonnées
                        annonce.DistanceFromUser = double.MaxValue;
                        filteredAnnouncements.Add(annonce);
                    }
                }

                // Trier par distance (les plus proches en premier)
                var sortedAnnouncements = filteredAnnouncements
                    .OrderBy(a => a.DistanceFromUser)
                    .ToList();

                // Mettre à jour la collection ObservableCollection
                NearbyAnnouncements.Clear();
                foreach (var annonce in sortedAnnouncements)
                {
                    NearbyAnnouncements.Add(annonce);
                }

                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Chargé {NearbyAnnouncements.Count} annonces à proximité");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Erreur lors du chargement des annonces: {ex.Message}");
                // En cas d'erreur, vider la collection pour éviter d'afficher des données obsolètes
                NearbyAnnouncements.Clear();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                // Recharger les annonces avec les nouveaux filtres
                await LoadNearbyAnnouncementsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application des filtres: {ex.Message}");
            }
        }

        public void OnMapLocationSelected(double latitude, double longitude)
        {
            try
            {
                // Mettre à jour la position sélectionnée
                UserLocation = new Location(latitude, longitude);
                CurrentLocationText = $"Position sélectionnée: {latitude:F4}, {longitude:F4}";
                
                // Recharger les annonces pour cette position
                _ = LoadNearbyAnnouncementsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sélection de position: {ex.Message}");
            }
        }

        private void UpdateLocationText()
        {
            if (UserLocation != null)
            {
                CurrentLocationText = $"Lat: {UserLocation.Latitude:F4}, Lon: {UserLocation.Longitude:F4}";
            }
            else
            {
                CurrentLocationText = "Position inconnue";
            }
        }

        private void OnBack()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MapViewModel] Retour en arrière demandé");
                
                // Navigation vers la page précédente via Shell
                Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Erreur lors du retour: {ex.Message}");
                
                // Fallback - essayer de naviguer vers la page principale
                try
                {
                    Shell.Current.GoToAsync("//AnnoncesPage");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapViewModel] Erreur fallback navigation: {fallbackEx.Message}");
                }
            }
        }
    }
}

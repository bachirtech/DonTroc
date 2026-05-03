using System.ComponentModel;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace DonTroc.Views;

public partial class CreationEvenementView : ContentPage
{
    private readonly CreationEvenementViewModel _vm;
    private readonly GeolocationService _geolocationService;
    private Pin? _rdvPin;

    public CreationEvenementView(CreationEvenementViewModel vm, GeolocationService geolocationService)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _geolocationService = geolocationService;

        // Tap sur la carte → met à jour le VM + le pin
        PickerMap.MapClicked += OnPickerMapClicked;

        // Quand le VM met à jour la position (ex : bouton "Ma position"),
        // on déplace le pin et on recadre la carte.
        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 🎬 GATE REWARDED — L'utilisateur doit regarder une pub avant d'accéder au formulaire.
        // Si refus / erreur, on ferme immédiatement la page (retour arrière).
        var unlocked = await _vm.EnsureAccessUnlockedAsync();
        if (!unlocked)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Centrer la carte sur la dernière position GPS connue (sans demander de permission).
        try
        {
            var last = _geolocationService.GetLastKnownLocation();
            if (last != null)
            {
                PickerMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(last.Latitude, last.Longitude),
                    Distance.FromKilometers(2)));
            }
        }
        catch { /* best-effort */ }
    }

    private void OnPickerMapClicked(object? sender, MapClickedEventArgs e) // Quand l'utilisateur clique sur la carte pour choisir un lieu, on met à jour le ViewModel et le pin de rendez-vous.
    {
        if (e?.Location == null) return;
        _vm.SetSelectedLocation(e.Location.Latitude, e.Location.Longitude);
        UpdatePin(e.Location.Latitude, e.Location.Longitude, recenter: false);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CreationEvenementViewModel.SelectedLatitude) ||
            e.PropertyName == nameof(CreationEvenementViewModel.SelectedLongitude))
        {
            if (_vm.SelectedLatitude.HasValue && _vm.SelectedLongitude.HasValue)
            {
                UpdatePin(_vm.SelectedLatitude.Value, _vm.SelectedLongitude.Value, recenter: true);
            }
        }
    }

    private void UpdatePin(double lat, double lng, bool recenter) // mise à jour du Pin sur la carte
    {
        var location = new Location(lat, lng);
        if (_rdvPin == null)
        {
            _rdvPin = new Pin
            {
                Label = "📍 Lieu de rendez-vous",
                Address = "Touchez la carte pour ajuster",
                Type = PinType.Place,
                Location = location
            };
            PickerMap.Pins.Add(_rdvPin);
        }
        else
        {
            _rdvPin.Location = location;
        }

        if (recenter)
        {
            PickerMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1)));
        }
    }
}

using System.ComponentModel;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace DonTroc.Views;

public partial class EventsMapView : ContentPage
{
    private readonly EventsMapViewModel _vm;
    private bool _initialCenterDone;

    public EventsMapView(EventsMapViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EventsMapViewModel.Events))
        {
            RefreshPins();
        }
    }

    private void RefreshPins()
    {
        try
        {
            EventsMap.Pins.Clear();

            foreach (var ev in _vm.Events)
            {
                if (!ev.Latitude.HasValue || !ev.Longitude.HasValue) continue;
                var loc = new Location(ev.Latitude.Value, ev.Longitude.Value);
                var pin = new Pin
                {
                    Label = ev.Titre,
                    Address = string.IsNullOrEmpty(ev.AdresseRdv) ? (ev.Ville ?? "") : ev.AdresseRdv,
                    Type = PinType.Place,
                    Location = loc,
                    BindingContext = ev
                };
                pin.InfoWindowClicked += OnPinInfoWindowClicked;
                EventsMap.Pins.Add(pin);
            }

            // Centrer la carte sur la position user (une seule fois) ou sur le 1er pin
            if (!_initialCenterDone)
            {
                Location? center = null;
                Distance radius = Distance.FromKilometers(_vm.RadiusKm);

                if (_vm.UserLatitude.HasValue && _vm.UserLongitude.HasValue)
                {
                    center = new Location(_vm.UserLatitude.Value, _vm.UserLongitude.Value);
                }
                else if (EventsMap.Pins.Count > 0)
                {
                    center = EventsMap.Pins[0].Location;
                    radius = Distance.FromKilometers(5);
                }

                if (center != null)
                {
                    EventsMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, radius));
                    _initialCenterDone = true;
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EventsMapView] RefreshPins: {ex.Message}");
        }
    }

    private void OnPinInfoWindowClicked(object? sender, PinClickedEventArgs e)
    {
        if (sender is Pin pin && pin.BindingContext is Evenement ev)
        {
            // Empêche le comportement par défaut (centrer)
            e.HideInfoWindow = true;
            _vm.OpenDetailCommand.Execute(ev);
        }
    }
}


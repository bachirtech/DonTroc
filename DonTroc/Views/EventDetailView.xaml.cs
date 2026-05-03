using System.ComponentModel;
using DonTroc.Models;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace DonTroc.Views;

public partial class EventDetailView : ContentPage
{
    private readonly EventDetailViewModel _vm;

    public EventDetailView(EventDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EventDetailViewModel.Current) || e.PropertyName == nameof(EventDetailViewModel.HasLocation))
        {
            UpdateRdvPin();
        }
    }

    private void UpdateRdvPin()
    {
        try
        {
            var ev = _vm.Current;
            if (ev?.Latitude == null || ev?.Longitude == null) return;

            var location = new Location(ev.Latitude.Value, ev.Longitude.Value);
            RdvMap.Pins.Clear();
            RdvMap.Pins.Add(new Pin
            {
                Label = ev.Titre,
                Address = ev.AdresseRdv ?? ev.Ville ?? string.Empty,
                Type = PinType.Place,
                Location = location
            });
            RdvMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1.5)));
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDetailView] UpdateRdvPin: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}


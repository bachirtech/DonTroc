using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel de la vue carte globale "Tous les événements près de moi".
/// Charge les événements géolocalisés dans le rayon courant et les expose
/// pour l'affichage de pins sur la carte.
/// </summary>
[QueryProperty(nameof(RadiusKm), "radiusKm")]
public class EventsMapViewModel : BaseViewModel
{
    private readonly EventService _eventService;
    private readonly GeolocationService _geolocationService;

    public List<int> RadiusOptions { get; } = new() { 5, 10, 25, 50 };

    private int _radiusKm = 50;
    public int RadiusKm
    {
        get => _radiusKm;
        set
        {
            if (SetProperty(ref _radiusKm, value))
            {
                OnPropertyChanged(nameof(HeaderText));
                _ = LoadAsync();
            }
        }
    }

    public string HeaderText => $"🗺️ Événements dans {_radiusKm} km";

    private List<Evenement> _events = new();
    public List<Evenement> Events
    {
        get => _events;
        private set => SetProperty(ref _events, value);
    }

    public double? UserLatitude { get; private set; }
    public double? UserLongitude { get; private set; }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand OpenDetailCommand { get; }
    public ICommand RefreshCommand { get; }

    public EventsMapViewModel(
        EventService eventService,
        GeolocationService geolocationService,
        ILogger<EventsMapViewModel>? logger = null) : base(logger)
    {
        _eventService = eventService;
        _geolocationService = geolocationService;

        OpenDetailCommand = new Command<Evenement>(async ev => await OpenDetailAsync(ev));
        RefreshCommand = new Command(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var loc = _geolocationService.GetLastKnownLocation();
            if (loc == null)
            {
                StatusMessage = "📍 Position GPS indisponible. Activez la localisation pour voir les événements près de vous.";
                Events = new List<Evenement>();
                return;
            }

            UserLatitude = loc.Latitude;
            UserLongitude = loc.Longitude;

            var list = await _eventService.GetNearbyEventsAsync(_radiusKm, limit: 100);
            Events = list;

            StatusMessage = list.Count == 0
                ? $"Aucun événement géolocalisé dans {_radiusKm} km."
                : $"📍 {list.Count} événement(s) trouvé(s)";
        }, operationName: "LoadEventsMap");
    }

    private async Task OpenDetailAsync(Evenement? ev)
    {
        if (ev == null || string.IsNullOrEmpty(ev.Id)) return;
        await Shell.Current.GoToAsync($"{nameof(EventDetailView)}?eventId={ev.Id}");
    }
}


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour la liste principale des événements (à venir, près de chez toi, officiels).
/// </summary>
public class EventsListViewModel : BaseViewModel
{
    private readonly EventService _eventService;
    private readonly AuthService _authService;

    public ObservableCollection<Evenement> UpcomingEvents { get; } = new();
    public ObservableCollection<Evenement> NearbyEvents { get; } = new();
    public ObservableCollection<Evenement> SeasonalEvents { get; } = new();

    /// <summary>Options de rayon (km) proposées dans le Picker.</summary>
    public List<int> RadiusOptions { get; } = new() { 5, 10, 25, 50 };

    private int _selectedRadiusKm = 50;
    public int SelectedRadiusKm
    {
        get => _selectedRadiusKm;
        set
        {
            if (SetProperty(ref _selectedRadiusKm, value))
            {
                OnPropertyChanged(nameof(NearbyTitle));
                _ = ReloadNearbyAsync();
            }
        }
    }

    public string NearbyTitle => $"📍 Près de chez toi ({_selectedRadiusKm} km)";

    private bool _hasUpcoming;
    public bool HasUpcoming { get => _hasUpcoming; set => SetProperty(ref _hasUpcoming, value); }

    private bool _hasNearby;
    public bool HasNearby { get => _hasNearby; set => SetProperty(ref _hasNearby, value); }

    private bool _hasSeasonal;
    public bool HasSeasonal { get => _hasSeasonal; set => SetProperty(ref _hasSeasonal, value); }

    private bool _hasNoResults;
    public bool HasNoResults { get => _hasNoResults; set => SetProperty(ref _hasNoResults, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand CreateEventCommand { get; }
    public ICommand OpenMyEventsCommand { get; }
    public ICommand OpenMapCommand { get; }

    public EventsListViewModel(
        EventService eventService,
        AuthService authService,
        ILogger<EventsListViewModel>? logger = null) : base(logger)
    {
        _eventService = eventService;
        _authService = authService;

        RefreshCommand = new Command(async () => await LoadAsync());
        OpenDetailCommand = new Command<Evenement>(async ev => await OpenDetailAsync(ev));
        CreateEventCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(CreationEvenementView)));
        OpenMyEventsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(MesEvenementsView)));
        OpenMapCommand = new Command(async () => await Shell.Current.GoToAsync($"{nameof(EventsMapView)}?radiusKm={SelectedRadiusKm}"));
    }

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var upcoming = await _eventService.GetUpcomingEventsAsync(20);
            var nearby = await _eventService.GetNearbyEventsAsync(SelectedRadiusKm);
            var seasonal = await _eventService.GetSeasonalEventsAsync();

            UpcomingEvents.Clear();
            foreach (var e in upcoming) UpcomingEvents.Add(e);

            NearbyEvents.Clear();
            foreach (var e in nearby) NearbyEvents.Add(e);

            SeasonalEvents.Clear();
            foreach (var e in seasonal) SeasonalEvents.Add(e);

            HasUpcoming = UpcomingEvents.Count > 0;
            HasNearby = NearbyEvents.Count > 0;
            HasSeasonal = SeasonalEvents.Count > 0;
            HasNoResults = !HasUpcoming && !HasNearby && !HasSeasonal;
        }, operationName: "LoadEvents");
    }

    /// <summary>Recharge UNIQUEMENT la section "Près de chez toi" (ex : changement de rayon).</summary>
    private async Task ReloadNearbyAsync()
    {
        await ExecuteAsync(async () =>
        {
            var nearby = await _eventService.GetNearbyEventsAsync(SelectedRadiusKm);
            NearbyEvents.Clear();
            foreach (var e in nearby) NearbyEvents.Add(e);
            HasNearby = NearbyEvents.Count > 0;
            HasNoResults = !HasUpcoming && !HasNearby && !HasSeasonal;
        }, operationName: "ReloadNearby");
    }

    private async Task OpenDetailAsync(Evenement? ev)
    {
        if (ev == null || string.IsNullOrEmpty(ev.Id)) return;
        await Shell.Current.GoToAsync($"{nameof(EventDetailView)}?eventId={ev.Id}");
    }
}


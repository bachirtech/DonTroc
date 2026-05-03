using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour la page de détail d'un événement.
/// </summary>
[QueryProperty(nameof(EventId), "eventId")]
public class EventDetailViewModel : BaseViewModel
{
    private readonly EventService _eventService;
    private readonly AuthService _authService;
    private readonly FirebaseService _firebaseService;
    private readonly AdMobService? _adMobService;

    // Compteurs de fréquence pour interstitiels (1 sur N pour ne pas saouler l'utilisateur)
    private const string EventDetailViewCountKey = "ad_event_detail_view_count";
    private const string EventJoinCountKey = "ad_event_join_count";
    private const int InterstitialEveryNDetailViews = 3;
    private const int InterstitialEveryNJoins = 2;

    private string? _eventId;
    public string? EventId
    {
        get => _eventId;
        set
        {
            if (SetProperty(ref _eventId, value) && !string.IsNullOrEmpty(value))
            {
                _ = LoadAsync();
            }
        }
    }

    private Evenement? _current;
    public Evenement? Current
    {
        get => _current;
        set
        {
            if (SetProperty(ref _current, value))
            {
                OnPropertyChanged(nameof(IsOwner));
                OnPropertyChanged(nameof(IsParticipant));
                OnPropertyChanged(nameof(CanJoin));
                OnPropertyChanged(nameof(HasLocation));
                OnPropertyChanged(nameof(JoinButtonText));
            }
        }
    }

    public ObservableCollection<EventParticipant> Participants { get; } = new();
    public ObservableCollection<Annonce> MarketAnnonces { get; } = new();

    public bool IsOwner => Current != null && Current.CreateurId == _authService.GetUserId();
    public bool IsParticipant => Current?.EstParticipant == true;
    public bool CanJoin => Current != null && !IsOwner && !IsParticipant
                                            && !Current.EstTermine()
                                            && Current.Statut != StatutEvenement.Annule
                                            && !Current.ComplèteSurParticipants();
    public bool HasLocation => Current?.Latitude.HasValue == true && Current?.Longitude.HasValue == true;
    public string JoinButtonText => IsParticipant ? "Quitter l'événement" : "Rejoindre l'événement";

    public ICommand JoinCommand { get; }
    public ICommand LeaveCommand { get; }
    public ICommand ToggleParticipationCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenMapCommand { get; }
    public ICommand ShareCommand { get; }
    public ICommand DeleteCommand { get; }

    public EventDetailViewModel(
        EventService eventService,
        AuthService authService,
        FirebaseService firebaseService,
        AdMobService? adMobService = null,
        ILogger<EventDetailViewModel>? logger = null) : base(logger)
    {
        _eventService = eventService;
        _authService = authService;
        _firebaseService = firebaseService;
        _adMobService = adMobService;

        ToggleParticipationCommand = new Command(async () => await ToggleParticipationAsync());
        JoinCommand = new Command(async () => await JoinAsync());
        LeaveCommand = new Command(async () => await LeaveAsync());
        RefreshCommand = new Command(async () => await LoadAsync());
        OpenMapCommand = new Command(async () => await OpenMapAsync());
        ShareCommand = new Command(async () => await ShareAsync());
        DeleteCommand = new Command(async () => await DeleteAsync());
    }

    public async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(EventId)) return;

        await ExecuteAsync(async () =>
        {
            Current = await _eventService.GetEventByIdAsync(EventId);
            if (Current == null) return;

            var participants = await _eventService.GetParticipantsAsync(EventId);
            Participants.Clear();
            foreach (var p in participants) Participants.Add(p);

            var market = await _eventService.GetMarketAnnoncesAsync(EventId);
            MarketAnnonces.Clear();
            foreach (var a in market) MarketAnnonces.Add(a);

            OnPropertyChanged(nameof(IsOwner));
            OnPropertyChanged(nameof(IsParticipant));
            OnPropertyChanged(nameof(CanJoin));
            OnPropertyChanged(nameof(JoinButtonText));
        }, operationName: "LoadEventDetail");

        // 💰 INTERSTITIEL — Moment stratégique #2 : 1 sur N ouvertures de détail.
        // L'utilisateur consulte du contenu — moment naturel pour une pub courte.
        await TryShowAdEveryNAsync(EventDetailViewCountKey, InterstitialEveryNDetailViews, "event_detail_view");
    }

    private async Task ToggleParticipationAsync()
    {
        if (Current == null) return;
        if (IsParticipant) await LeaveAsync();
        else await JoinAsync();
    }

    private async Task JoinAsync()
    {
        if (Current == null || string.IsNullOrEmpty(EventId)) return;
        await ExecuteAsync(async () =>
        {
            var user = await _authService.GetCurrentUserAsync();
            var name = user?.DisplayName ?? "Utilisateur";
            var ok = await _eventService.JoinEventAsync(EventId, name);
            if (ok) await LoadAsync();
        }, operationName: "JoinEvent");

        // 💰 INTERSTITIEL — Moment stratégique #3 : 1 sur N rejoindre.
        // Action engageante terminée → moment naturel.
        await TryShowAdEveryNAsync(EventJoinCountKey, InterstitialEveryNJoins, "event_joined");
    }

    private async Task LeaveAsync()
    {
        if (string.IsNullOrEmpty(EventId)) return;
        await ExecuteAsync(async () =>
        {
            var ok = await _eventService.LeaveEventAsync(EventId);
            if (ok) await LoadAsync();
        }, operationName: "LeaveEvent");
    }

    private async Task OpenMapAsync()
    {
        if (Current?.Latitude == null || Current?.Longitude == null) return;
        try
        {
            var loc = new Location(Current.Latitude.Value, Current.Longitude.Value);
            var options = new MapLaunchOptions { Name = Current.Titre };
            await Map.OpenAsync(loc, options);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Erreur ouverture carte");
        }
    }

    private async Task ShareAsync()
    {
        if (Current == null) return;
        try
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Title = "Rejoins l'événement DonTroc !",
                Text = $"🏪 {Current.Titre}\n📅 {Current.DateDebut:dd/MM/yyyy HH:mm}\n\n{Current.Description}\n\nTélécharge DonTroc : https://bachirtech.github.io/DonTroc/download.html"
            });
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Erreur partage événement");
        }
    }

    private async Task DeleteAsync()
    {
        if (Current == null || string.IsNullOrEmpty(EventId)) return;
        if (!IsOwner) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Supprimer l'événement",
            $"Voulez-vous vraiment supprimer « {Current.Titre} » ? Cette action est irréversible et tous les participants seront désinscrits.",
            "Supprimer", "Annuler");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var ok = await _eventService.DeleteEventAsync(EventId);
            if (ok)
            {
                await Shell.Current.DisplayAlert("✅ Supprimé", "L'événement a bien été supprimé.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                var detail = string.IsNullOrEmpty(_eventService.LastError) ? "raison inconnue" : _eventService.LastError;
                await Shell.Current.DisplayAlert("Erreur", $"Suppression impossible : {detail}", "OK");
            }
        }, operationName: "DeleteEvent");
    }

    /// <summary>
    /// Affiche un interstitiel toutes les <paramref name="n"/> occurrences de l'action.
    /// Le compteur est persisté dans <see cref="Preferences"/> pour survivre aux redémarrages.
    /// Le service <see cref="AdMobService"/> applique en plus ses propres garde-fous
    /// (cooldown 120s, max/session, mode Ad-Free).
    /// </summary>
    private async Task TryShowAdEveryNAsync(string counterKey, int n, string actionName)
    {
        if (_adMobService == null) return;
        try
        {
            var current = Preferences.Get(counterKey, 0) + 1;
            Preferences.Set(counterKey, current);

            if (current % n != 0) return;

            await _adMobService.ShowInterstitialAfterActionAsync(actionName);
        }
        catch
        {
            // Ne jamais bloquer le flux utilisateur
        }
    }
}


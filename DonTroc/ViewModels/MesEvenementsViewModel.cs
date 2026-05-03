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
/// ViewModel pour la page "Mes événements" (créés / je participe).
/// </summary>
public class MesEvenementsViewModel : BaseViewModel
{
    private readonly EventService _eventService;
    private readonly AuthService _authService;

    public ObservableCollection<Evenement> CreatedEvents { get; } = new();
    public ObservableCollection<Evenement> JoinedEvents { get; } = new();

    private bool _hasCreated;
    public bool HasCreated { get => _hasCreated; set => SetProperty(ref _hasCreated, value); }

    private bool _hasJoined;
    public bool HasJoined { get => _hasJoined; set => SetProperty(ref _hasJoined, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand DeleteCommand { get; }

    public MesEvenementsViewModel(
        EventService eventService,
        AuthService authService,
        ILogger<MesEvenementsViewModel>? logger = null) : base(logger)
    {
        _eventService = eventService;
        _authService = authService;

        RefreshCommand = new Command(async () => await LoadAsync());
        OpenDetailCommand = new Command<Evenement>(async ev =>
        {
            if (ev != null)
                await Shell.Current.GoToAsync($"{nameof(EventDetailView)}?eventId={ev.Id}");
        });

        DeleteCommand = new Command<Evenement>(async ev => await DeleteAsync(ev));
    }

    private async Task DeleteAsync(Evenement? ev)
    {
        if (ev == null || string.IsNullOrEmpty(ev.Id)) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Supprimer l'événement",
            $"Voulez-vous vraiment supprimer « {ev.Titre} » ? Cette action est irréversible.",
            "Supprimer", "Annuler");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var ok = await _eventService.DeleteEventAsync(ev.Id);
            if (ok)
            {
                CreatedEvents.Remove(ev);
                HasCreated = CreatedEvents.Count > 0;
            }
            else
            {
                var detail = string.IsNullOrEmpty(_eventService.LastError) ? "raison inconnue" : _eventService.LastError;
                await Shell.Current.DisplayAlert("Erreur", $"Suppression impossible : {detail}", "OK");
            }
        }, operationName: "DeleteMyEvent");
    }

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var (created, joined) = await _eventService.GetMyEventsAsync(userId);

            CreatedEvents.Clear();
            foreach (var e in created) CreatedEvents.Add(e);
            JoinedEvents.Clear();
            foreach (var e in joined) JoinedEvents.Add(e);

            HasCreated = CreatedEvents.Count > 0;
            HasJoined = JoinedEvents.Count > 0;
        }, operationName: "LoadMyEvents");
    }
}


using System.Collections.ObjectModel;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel de la page listant les propositions de troc (reçues + envoyées).
/// </summary>
public class TradeProposalsListViewModel : BaseViewModel
{
    private readonly TradeProposalService _service;
    private readonly ILogger<TradeProposalsListViewModel> _logger;

    public TradeProposalsListViewModel(
        TradeProposalService service,
        ILogger<TradeProposalsListViewModel> logger)
    {
        _service = service;
        _logger = logger;

        RefreshCommand = new Command(async () => await LoadAsync());
        SwitchTabCommand = new Command<string>(tab => { CurrentTab = tab ?? "received"; });
        AcceptCommand = new Command<TradeProposal>(async p => await OnAcceptAsync(p));
        DeclineCommand = new Command<TradeProposal>(async p => await OnDeclineAsync(p));
        CancelCommand = new Command<TradeProposal>(async p => await OnCancelAsync(p));
    }

    public ObservableCollection<TradeProposal> Received { get; } = new();
    public ObservableCollection<TradeProposal> Sent { get; } = new();

    private string _currentTab = "received";
    public string CurrentTab
    {
        get => _currentTab;
        set
        {
            if (SetProperty(ref _currentTab, value))
            {
                OnPropertyChanged(nameof(IsReceivedTab));
                OnPropertyChanged(nameof(IsSentTab));
            }
        }
    }

    public bool IsReceivedTab => CurrentTab == "received";
    public bool IsSentTab => CurrentTab == "sent";

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SwitchTabCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand DeclineCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;

            var received = await _service.GetReceivedAsync();
            var sent = await _service.GetSentAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Received.Clear();
                foreach (var p in received) Received.Add(p);

                Sent.Clear();
                foreach (var p in sent) Sent.Add(p);

                IsEmpty = Received.Count == 0 && Sent.Count == 0;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur chargement propositions");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnAcceptAsync(TradeProposal? p)
    {
        if (p == null) return;
        var ok = await Shell.Current.DisplayAlert("Accepter le troc",
            $"Accepter la proposition : {p.OfferedAnnonceTitre} ↔ {p.AnnonceCibleTitre} ?",
            "Accepter", "Annuler");
        if (!ok) return;

        var success = await _service.AcceptAsync(p.Id);
        if (success)
        {
            await Shell.Current.DisplayAlert("✅ Troc accepté",
                "La transaction a été créée. Contactez l'autre utilisateur pour convenir du rendez-vous.", "OK");
            await LoadAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'accepter.", "OK");
        }
    }

    private async Task OnDeclineAsync(TradeProposal? p)
    {
        if (p == null) return;
        var motif = await Shell.Current.DisplayPromptAsync("Refuser",
            "Motif (optionnel) :", "Refuser", "Annuler", "Ex : ne correspond pas");

        // Si DisplayPromptAsync retourne null = annulation
        if (motif == null) return;

        var success = await _service.DeclineAsync(p.Id, motif);
        if (success) await LoadAsync();
    }

    private async Task OnCancelAsync(TradeProposal? p)
    {
        if (p == null) return;
        var ok = await Shell.Current.DisplayAlert("Annuler la proposition",
            "Voulez-vous retirer votre proposition ?", "Oui", "Non");
        if (!ok) return;

        var success = await _service.CancelAsync(p.Id);
        if (success) await LoadAsync();
    }
}


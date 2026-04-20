using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel de la page de création d'une proposition de troc.
/// Charge l'annonce cible + les annonces disponibles du proposeur.
/// </summary>
public class TradeProposalViewModel : BaseViewModel, IQueryAttributable
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly TradeProposalService _tradeProposalService;
    private readonly ILogger<TradeProposalViewModel> _logger;

    public TradeProposalViewModel(
        FirebaseService firebaseService,
        AuthService authService,
        TradeProposalService tradeProposalService,
        ILogger<TradeProposalViewModel> logger)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _tradeProposalService = tradeProposalService;
        _logger = logger;

        SubmitCommand = new Command(async () => await OnSubmitAsync(), () => CanSubmit);
        CancelCommand = new Command(async () => await Shell.Current.Navigation.PopAsync());
        SelectAnnonceCommand = new Command<Annonce>(OnSelectAnnonce);
    }

    // ── Propriétés ──

    private Annonce? _targetAnnonce;
    public Annonce? TargetAnnonce
    {
        get => _targetAnnonce;
        set => SetProperty(ref _targetAnnonce, value);
    }

    public ObservableCollection<Annonce> UserAnnonces { get; } = new();

    private Annonce? _selectedAnnonce;
    public Annonce? SelectedAnnonce
    {
        get => _selectedAnnonce;
        set
        {
            if (SetProperty(ref _selectedAnnonce, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(CanSubmit));
                ((Command)SubmitCommand).ChangeCanExecute();
            }
        }
    }

    public bool HasSelection => SelectedAnnonce != null;

    private string? _message;
    public string? Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private bool _isLoadingAnnonces = true;
    public bool IsLoadingAnnonces
    {
        get => _isLoadingAnnonces;
        set
        {
            if (SetProperty(ref _isLoadingAnnonces, value))
                OnPropertyChanged(nameof(HasNoAnnonces));
        }
    }

    public bool HasNoAnnonces => !IsLoadingAnnonces && UserAnnonces.Count == 0;

    private bool _isSending;
    public bool IsSending
    {
        get => _isSending;
        set
        {
            if (SetProperty(ref _isSending, value))
            {
                OnPropertyChanged(nameof(CanSubmit));
                ((Command)SubmitCommand).ChangeCanExecute();
            }
        }
    }

    public bool CanSubmit => SelectedAnnonce != null && !IsSending && TargetAnnonce != null;

    // ── Commandes ──

    public ICommand SubmitCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectAnnonceCommand { get; }

    // ── IQueryAttributable ──

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("annonceId", out var idObj))
        {
            var id = idObj?.ToString();
            if (!string.IsNullOrEmpty(id))
                _ = LoadAsync(id);
        }
    }

    private async Task LoadAsync(string targetAnnonceId)
    {
        try
        {
            IsBusy = true;
            IsLoadingAnnonces = true;

            var target = await _firebaseService.GetAnnonceAsync(targetAnnonceId);
            if (target == null)
            {
                await Shell.Current.DisplayAlert("Introuvable", "Cette annonce n'existe plus.", "OK");
                await Shell.Current.Navigation.PopAsync();
                return;
            }

            TargetAnnonce = target;

            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Shell.Current.DisplayAlert("Connexion requise",
                    "Vous devez être connecté pour proposer un troc.", "OK");
                await Shell.Current.Navigation.PopAsync();
                return;
            }

            var mine = await _firebaseService.GetAnnoncesForUserAsync(userId);
            var eligibles = mine
                .Where(a => a.Statut == StatutAnnonce.Disponible &&
                            !string.Equals(a.Id, targetAnnonceId, StringComparison.Ordinal))
                .OrderByDescending(a => a.DateCreation)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UserAnnonces.Clear();
                foreach (var a in eligibles) UserAnnonces.Add(a);
                OnPropertyChanged(nameof(HasNoAnnonces));
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur chargement page proposition");
            await Shell.Current.DisplayAlert("Erreur",
                "Impossible de charger les données.", "OK");
        }
        finally
        {
            IsLoadingAnnonces = false;
            IsBusy = false;
        }
    }

    private void OnSelectAnnonce(Annonce? annonce)
    {
        if (annonce == null) return;
        SelectedAnnonce = annonce;
    }

    private async Task OnSubmitAsync()
    {
        if (TargetAnnonce == null || SelectedAnnonce == null) return;

        try
        {
            IsSending = true;

            var proposal = await _tradeProposalService.CreateProposalAsync(
                TargetAnnonce, SelectedAnnonce, Message);

            if (proposal != null)
            {
                await Shell.Current.DisplayAlert(
                    "✅ Proposition envoyée",
                    "Votre proposition de troc a bien été transmise. Vous recevrez une notification dès que l'utilisateur répondra.",
                    "OK");
                await Shell.Current.Navigation.PopAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur",
                    "Impossible d'envoyer la proposition. Réessayez.", "OK");
            }
        }
        catch (InvalidOperationException opEx)
        {
            await Shell.Current.DisplayAlert("Action impossible", opEx.Message, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi proposition");
            await Shell.Current.DisplayAlert("Erreur",
                "Une erreur est survenue. Réessayez plus tard.", "OK");
        }
        finally
        {
            IsSending = false;
        }
    }
}


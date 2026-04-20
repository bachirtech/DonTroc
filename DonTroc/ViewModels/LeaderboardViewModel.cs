// Fichier: DonTroc/ViewModels/LeaderboardViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

public class LeaderboardViewModel : BaseViewModel
{
    private readonly LeaderboardService _leaderboardService;
    private readonly GamificationService _gamificationService;
    private readonly AuthService _authService;
    
    private bool _isInitialLoadDone;

    public ObservableCollection<LeaderboardEntry> LeaderboardEntries { get; } = new();

    // Podium (top 3)
    private LeaderboardEntry? _first;
    public LeaderboardEntry? First
    {
        get => _first;
        set => SetProperty(ref _first, value);
    }

    private LeaderboardEntry? _second;
    public LeaderboardEntry? Second
    {
        get => _second;
        set => SetProperty(ref _second, value);
    }

    private LeaderboardEntry? _third;
    public LeaderboardEntry? Third
    {
        get => _third;
        set => SetProperty(ref _third, value);
    }

    private bool _hasPodium;
    public bool HasPodium
    {
        get => _hasPodium;
        set => SetProperty(ref _hasPodium, value);
    }

    // Rang et stats de l'utilisateur courant
    private LeaderboardEntry? _currentUserEntry;
    public LeaderboardEntry? CurrentUserEntry
    {
        get => _currentUserEntry;
        set => SetProperty(ref _currentUserEntry, value);
    }

    private bool _hasCurrentUser;
    public bool HasCurrentUser
    {
        get => _hasCurrentUser;
        set => SetProperty(ref _hasCurrentUser, value);
    }

    // Filtre actif
    private string _selectedFilter = "Global";
    public string SelectedFilter
    {
        get => _selectedFilter;
        set => SetProperty(ref _selectedFilter, value);
    }

    private bool _isGlobalSelected = true;
    public bool IsGlobalSelected
    {
        get => _isGlobalSelected;
        set => SetProperty(ref _isGlobalSelected, value);
    }

    private bool _isFriendsSelected;
    public bool IsFriendsSelected
    {
        get => _isFriendsSelected;
        set => SetProperty(ref _isFriendsSelected, value);
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    private int _totalParticipants;
    public int TotalParticipants
    {
        get => _totalParticipants;
        set => SetProperty(ref _totalParticipants, value);
    }

    // Commandes
    public ICommand RefreshCommand { get; }
    public ICommand SelectGlobalCommand { get; }
    public ICommand SelectFriendsCommand { get; }

    public LeaderboardViewModel(
        LeaderboardService leaderboardService,
        GamificationService gamificationService,
        AuthService authService)
    {
        _leaderboardService = leaderboardService;
        _gamificationService = gamificationService;
        _authService = authService;

        RefreshCommand = new Command(async void () =>
        {
            _leaderboardService.InvalidateCache();
            await LoadLeaderboardAsync();
        });

        SelectGlobalCommand = new Command(async void () =>
        {
            if (IsGlobalSelected) return; // Déjà sélectionné
            IsGlobalSelected = true;
            IsFriendsSelected = false;
            _selectedFilter = "Global";
            OnPropertyChanged(nameof(SelectedFilter));
            await LoadLeaderboardAsync();
        });

        SelectFriendsCommand = new Command(async void () =>
        {
            if (IsFriendsSelected) return; // Déjà sélectionné
            IsGlobalSelected = false;
            IsFriendsSelected = true;
            _selectedFilter = "Amis";
            OnPropertyChanged(nameof(SelectedFilter));
            await LoadLeaderboardAsync();
        });

        // NE PAS charger dans le constructeur — OnAppearing s'en occupe
    }

    public async Task LoadLeaderboardAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            List<LeaderboardEntry> entries;

            if (_selectedFilter == "Amis")
            {
                entries = await _leaderboardService.GetFriendsLeaderboardAsync();
            }
            else
            {
                entries = await _leaderboardService.GetGlobalLeaderboardAsync(50);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Podium
                First = entries.FirstOrDefault(e => e.Rank == 1);
                Second = entries.FirstOrDefault(e => e.Rank == 2);
                Third = entries.FirstOrDefault(e => e.Rank == 3);
                HasPodium = First != null;

                // Utilisateur courant
                CurrentUserEntry = entries.FirstOrDefault(e => e.IsCurrentUser);
                HasCurrentUser = CurrentUserEntry != null;

                // Liste (à partir du rang 4 pour ne pas dupliquer le podium)
                LeaderboardEntries.Clear();
                foreach (var entry in entries.Where(e => e.Rank > 3))
                {
                    LeaderboardEntries.Add(entry);
                }

                TotalParticipants = entries.Count;
                IsEmpty = !entries.Any();
            });
            
            _isInitialLoadDone = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Leaderboard] Erreur: {ex.Message}");
            IsEmpty = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Appelé par la View dans OnAppearing. Ne recharge que si pas encore chargé.
    /// </summary>
    public async Task OnAppearingAsync()
    {
        if (!_isInitialLoadDone)
        {
            await LoadLeaderboardAsync();
        }
    }
}


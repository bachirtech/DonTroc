// Fichier: DonTroc/ViewModels/AdminDashboardViewModel.cs
// ViewModel pour le dashboard d'administration

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private readonly AdminService _adminService;
        
        private AdminStats? _stats;
        private bool _isAdmin;
        private bool _isModerator;
        private string _welcomeMessage = "Bienvenue !";

        public AdminDashboardViewModel(AdminService adminService)
        {
            _adminService = adminService;
            RecentLogs = new ObservableCollection<AdminLog>();
            
            LoadDataCommand = new Command(async () => await LoadDataAsync());
            NavigateToUsersCommand = new Command(async () => await NavigateToUsersAsync());
            NavigateToReportsCommand = new Command(async () => await NavigateToReportsAsync());
            NavigateToLogsCommand = new Command(async () => await NavigateToLogsAsync());
            NavigateToEventsCommand = new Command(async () => await NavigateToEventsAsync());
            RefreshCommand = new Command(async () => await RefreshAsync());
        }

        #region Propriétés

        public AdminStats? Stats
        {
            get => _stats;
            set
            {
                if (SetProperty(ref _stats, value))
                {
                    OnPropertyChanged(nameof(TotalUsersDisplay));
                    OnPropertyChanged(nameof(ActiveUsersDisplay));
                    OnPropertyChanged(nameof(SuspendedUsersDisplay));
                    OnPropertyChanged(nameof(TotalAnnoncesDisplay));
                    OnPropertyChanged(nameof(ActiveAnnoncesDisplay));
                    OnPropertyChanged(nameof(PendingReportsDisplay));
                    OnPropertyChanged(nameof(TotalReportsDisplay));
                    OnPropertyChanged(nameof(NewUsersTodayDisplay));
                    OnPropertyChanged(nameof(NewAnnoncesTodayDisplay));
                    OnPropertyChanged(nameof(TeamDisplay));
                    OnPropertyChanged(nameof(PendingReportsColor));
                }
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public bool IsModerator
        {
            get => _isModerator;
            set => SetProperty(ref _isModerator, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ObservableCollection<AdminLog> RecentLogs { get; }

        // Propriétés calculées pour l'affichage
        public string TotalUsersDisplay => Stats?.TotalUsers.ToString("N0") ?? "0";
        public string ActiveUsersDisplay => Stats?.ActiveUsers.ToString("N0") ?? "0";
        public string SuspendedUsersDisplay => Stats?.SuspendedUsers.ToString("N0") ?? "0";
        public string TotalAnnoncesDisplay => Stats?.TotalAnnonces.ToString("N0") ?? "0";
        public string ActiveAnnoncesDisplay => Stats?.ActiveAnnonces.ToString("N0") ?? "0";
        public string PendingReportsDisplay => Stats?.PendingReports.ToString("N0") ?? "0";
        public string TotalReportsDisplay => Stats?.TotalReports.ToString("N0") ?? "0";
        public string NewUsersTodayDisplay => $"+{Stats?.NewUsersToday ?? 0}";
        public string NewAnnoncesTodayDisplay => $"+{Stats?.NewAnnoncesToday ?? 0}";
        public string TeamDisplay => $"{Stats?.AdminCount ?? 0} admins, {Stats?.ModeratorCount ?? 0} modérateurs";

        // Couleur pour les signalements en attente
        public string PendingReportsColor => (Stats?.PendingReports ?? 0) > 0 ? "#E74C3C" : "#27AE60";

        #endregion

        #region Commandes

        public ICommand LoadDataCommand { get; }
        public ICommand NavigateToUsersCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToLogsCommand { get; }
        public ICommand NavigateToEventsCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Méthodes

        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Vérifier les permissions
                IsAdmin = await _adminService.IsCurrentUserAdminAsync();
                IsModerator = await _adminService.IsCurrentUserModeratorAsync();

                if (!IsModerator)
                {
                    WelcomeMessage = "⚠️ Accès non autorisé";
                    return;
                }

                var admin = await _adminService.GetCurrentAdminAsync();
                WelcomeMessage = $"Bienvenue, {admin?.Name ?? "Admin"} !";

                // Charger les statistiques
                Stats = await _adminService.GetAdminStatsAsync();

                // Charger les logs récents
                RecentLogs.Clear();
                var logs = await _adminService.GetAdminLogsAsync(10);
                foreach (var log in logs)
                {
                    RecentLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminDashboard] Erreur: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task NavigateToUsersAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.UserManagementPage));
        }

        private async Task NavigateToReportsAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.ModerationPage));
        }

        private async Task NavigateToLogsAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.AdminLogsPage));
        }

        private async Task NavigateToEventsAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.AdminEventsPage));
        }

        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        #endregion
    }
}


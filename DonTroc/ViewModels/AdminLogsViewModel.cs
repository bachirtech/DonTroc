// Fichier: DonTroc/ViewModels/AdminLogsViewModel.cs
// ViewModel pour l'historique des actions admin

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels
{
    public class AdminLogsViewModel : BaseViewModel
    {
        private readonly AdminService _adminService;
        
        private string _selectedActionFilter = "Toutes";
        private DateTime _dateFrom = DateTime.UtcNow.AddDays(-30);
        private DateTime _dateTo = DateTime.UtcNow;
        private int _totalLogs;

        public AdminLogsViewModel(AdminService adminService)
        {
            _adminService = adminService;
            Logs = new ObservableCollection<AdminLog>();
            FilteredLogs = new ObservableCollection<AdminLog>();
            
            LoadLogsCommand = new Command(async () => await LoadLogsAsync());
            ApplyFiltersCommand = new Command(ApplyFilters);
        }

        #region Propriétés

        public ObservableCollection<AdminLog> Logs { get; }
        public ObservableCollection<AdminLog> FilteredLogs { get; }

        public string SelectedActionFilter
        {
            get => _selectedActionFilter;
            set
            {
                if (SetProperty(ref _selectedActionFilter, value))
                    ApplyFilters();
            }
        }

        public DateTime DateFrom
        {
            get => _dateFrom;
            set
            {
                if (SetProperty(ref _dateFrom, value))
                    ApplyFilters();
            }
        }

        public DateTime DateTo
        {
            get => _dateTo;
            set
            {
                if (SetProperty(ref _dateTo, value))
                    ApplyFilters();
            }
        }

        public int TotalLogs
        {
            get => _totalLogs;
            set => SetProperty(ref _totalLogs, value);
        }

        public string[] ActionFilters => new[] 
        { 
            "Toutes", 
            "Utilisateurs", 
            "Annonces", 
            "Signalements",
            "Système"
        };

        #endregion

        #region Commandes

        public ICommand LoadLogsCommand { get; }
        public ICommand ApplyFiltersCommand { get; }

        #endregion

        #region Méthodes

        private async Task LoadLogsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Logs.Clear();
                var logs = await _adminService.GetAdminLogsAsync(500);
                
                foreach (var log in logs)
                {
                    Logs.Add(log);
                }

                TotalLogs = Logs.Count;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminLogs] Erreur: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            FilteredLogs.Clear();

            var filtered = Logs.AsEnumerable();

            // Filtre par date
            filtered = filtered.Where(l => l.Timestamp >= DateFrom && l.Timestamp <= DateTo.AddDays(1));

            // Filtre par type d'action
            if (SelectedActionFilter != "Toutes")
            {
                filtered = SelectedActionFilter switch
                {
                    "Utilisateurs" => filtered.Where(l => 
                        l.ActionType == AdminActionType.UserSuspended ||
                        l.ActionType == AdminActionType.UserUnsuspended ||
                        l.ActionType == AdminActionType.UserPromotedToAdmin ||
                        l.ActionType == AdminActionType.UserPromotedToModerator ||
                        l.ActionType == AdminActionType.UserDemoted ||
                        l.ActionType == AdminActionType.UserDeleted),
                    "Annonces" => filtered.Where(l => 
                        l.ActionType == AdminActionType.AnnonceApproved ||
                        l.ActionType == AdminActionType.AnnonceRejected ||
                        l.ActionType == AdminActionType.AnnonceDeleted ||
                        l.ActionType == AdminActionType.AnnonceFlagged),
                    "Signalements" => filtered.Where(l => 
                        l.ActionType == AdminActionType.ReportReviewed ||
                        l.ActionType == AdminActionType.ReportResolved ||
                        l.ActionType == AdminActionType.ReportDismissed),
                    "Système" => filtered.Where(l => 
                        l.ActionType == AdminActionType.SettingsChanged ||
                        l.ActionType == AdminActionType.BulkAction ||
                        l.ActionType == AdminActionType.Other),
                    _ => filtered
                };
            }

            foreach (var log in filtered.OrderByDescending(l => l.Timestamp))
            {
                FilteredLogs.Add(log);
            }
        }

        #endregion
    }
}


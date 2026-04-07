using DonTroc.Models;
using DonTroc.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels
{
    public class ModerationViewModel : BaseViewModel
    {
        private readonly ReportService _reportService;
        private readonly AdminService _adminService;
        
        private string _selectedStatusFilter = "Tous";
        private string _selectedTypeFilter = "Tous";
        private int _pendingCount;
        private int _totalCount;
        private bool _isSelectionMode;

        public ModerationViewModel(ReportService reportService, AdminService adminService)
        {
            _reportService = reportService;
            _adminService = adminService;
            Reports = new ObservableCollection<Report>();
            FilteredReports = new ObservableCollection<Report>();
            SelectedReports = new ObservableCollection<Report>();
            
            LoadReportsCommand = new Command(async () => await LoadReportsAsync());
            ApplyFiltersCommand = new Command(ApplyFilters);
            UpdateReportStatusCommand = new Command<Report>(async (r) => await UpdateReportStatusAsync(r));
            ViewReportDetailsCommand = new Command<Report>(async (r) => await ViewReportDetailsAsync(r));
            ToggleSelectionCommand = new Command<Report>(ToggleSelection);
            ToggleSelectionModeCommand = new Command(ToggleSelectionMode);
            BulkUpdateStatusCommand = new Command(async () => await BulkUpdateStatusAsync());
            SuspendReportedUserCommand = new Command<Report>(async (r) => await SuspendReportedUserAsync(r));
            DeleteReportedAnnonceCommand = new Command<Report>(async (r) => await DeleteReportedAnnonceAsync(r));
        }

        #region Propriétés

        public ObservableCollection<Report> Reports { get; }
        public ObservableCollection<Report> FilteredReports { get; }
        public ObservableCollection<Report> SelectedReports { get; }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                    ApplyFilters();
            }
        }

        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                    ApplyFilters();
            }
        }

        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            set => SetProperty(ref _isSelectionMode, value);
        }

        public string[] StatusFilters => new[] { "Tous", "En attente", "Examiné", "Résolu", "Rejeté" };
        public string[] TypeFilters => new[] { "Tous", "Annonce", "Utilisateur", "Message" };

        #endregion

        #region Commandes

        public ICommand LoadReportsCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand UpdateReportStatusCommand { get; }
        public ICommand ViewReportDetailsCommand { get; }
        public ICommand ToggleSelectionCommand { get; }
        public ICommand ToggleSelectionModeCommand { get; }
        public ICommand BulkUpdateStatusCommand { get; }
        public ICommand SuspendReportedUserCommand { get; }
        public ICommand DeleteReportedAnnonceCommand { get; }

        #endregion

        #region Méthodes

        private async Task LoadReportsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Reports.Clear();
                var reports = await _adminService.GetAllReportsAsync();
                
                foreach (var report in reports)
                {
                    Reports.Add(report);
                }

                TotalCount = Reports.Count;
                PendingCount = Reports.Count(r => r.Status == "Pending");
                
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Moderation] Erreur: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            FilteredReports.Clear();

            var filtered = Reports.AsEnumerable();

            if (SelectedStatusFilter != "Tous")
            {
                var status = SelectedStatusFilter switch
                {
                    "En attente" => "Pending",
                    "Examiné" => "Reviewed",
                    "Résolu" => "ActionTaken",
                    "Rejeté" => "Dismissed",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(status))
                    filtered = filtered.Where(r => r.Status == status);
            }

            if (SelectedTypeFilter != "Tous")
            {
                filtered = filtered.Where(r => r.ReportedItemType == SelectedTypeFilter);
            }

            foreach (var report in filtered.OrderByDescending(r => r.Timestamp))
            {
                FilteredReports.Add(report);
            }
        }

        private async Task UpdateReportStatusAsync(Report? report)
        {
            if (report == null) return;

            try
            {
                string newStatus = await Shell.Current.DisplayActionSheet(
                    "Mettre à jour le statut",
                    "Annuler",
                    null,
                    "En attente", "Examiné", "Action prise", "Rejeté");

                if (newStatus == "Annuler" || string.IsNullOrEmpty(newStatus)) return;

                var status = newStatus switch
                {
                    "En attente" => "Pending",
                    "Examiné" => "Reviewed",
                    "Action prise" => "ActionTaken",
                    "Rejeté" => "Dismissed",
                    _ => "Pending"
                };

                string? notes = await Shell.Current.DisplayPromptAsync(
                    "Notes de modération",
                    "Ajoutez des notes (optionnel) :",
                    "Confirmer",
                    "Passer",
                    placeholder: "Notes...");

                var success = await _adminService.UpdateReportStatusAsync(report.Id, status, notes ?? "");
                
                if (success)
                {
                    await Shell.Current.DisplayAlert("Succès", 
                        "Signalement mis à jour.", "OK");
                    await LoadReportsAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de mettre à jour le signalement.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Moderation] Erreur UpdateReportStatusAsync: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Une erreur s'est produite.", "OK");
            }
        }

        private async Task ViewReportDetailsAsync(Report? report)
        {
            if (report == null) return;

            try
            {
                var info = $"📋 Type: {report.ReportedItemType}\n" +
                           $"📝 Raison: {report.Reason}\n" +
                           $"📅 Date: {report.Timestamp:dd/MM/yyyy HH:mm}\n" +
                           $"📊 Statut: {report.Status}\n" +
                           $"👤 Signalé par: {report.ReporterId}";

                if (!string.IsNullOrEmpty(report.Notes))
                {
                    info += $"\n\n📝 Notes modération:\n{report.Notes}";
                }

                await Shell.Current.DisplayAlert("Détails du signalement", info, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Moderation] Erreur ViewReportDetailsAsync: {ex.Message}");
            }
        }

        private void ToggleSelection(Report? report)
        {
            if (report == null) return;

            if (SelectedReports.Contains(report))
                SelectedReports.Remove(report);
            else
                SelectedReports.Add(report);
        }

        private void ToggleSelectionMode()
        {
            IsSelectionMode = !IsSelectionMode;
            if (!IsSelectionMode)
                SelectedReports.Clear();
        }

        private async Task BulkUpdateStatusAsync()
        {
            if (!SelectedReports.Any())
            {
                await Shell.Current.DisplayAlert("Info", 
                    "Sélectionnez au moins un signalement.", "OK");
                return;
            }

            try
            {
                string newStatus = await Shell.Current.DisplayActionSheet(
                    $"Mettre à jour {SelectedReports.Count} signalements",
                    "Annuler",
                    null,
                    "Résolu", "Rejeté");

                if (newStatus == "Annuler" || string.IsNullOrEmpty(newStatus)) return;

                var status = newStatus switch
                {
                    "Résolu" => "ActionTaken",
                    "Rejeté" => "Dismissed",
                    _ => "Reviewed"
                };

                var ids = SelectedReports.Select(r => r.Id).ToList();
                var count = await _adminService.BulkUpdateReportsAsync(ids, status);

                await Shell.Current.DisplayAlert("Succès", 
                    $"{count} signalements mis à jour.", "OK");
                
                IsSelectionMode = false;
                SelectedReports.Clear();
                await LoadReportsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Moderation] Erreur BulkUpdateStatusAsync: {ex.Message}");
            }
        }

        private async Task SuspendReportedUserAsync(Report? report)
        {
            if (report == null || string.IsNullOrEmpty(report.ReportedItemId)) return;

            try
            {
                if (report.ReportedItemType != "Utilisateur" && report.ReportedItemType != "User")
                {
                    await Shell.Current.DisplayAlert("Info", 
                        "Cette action n'est disponible que pour les signalements d'utilisateurs.", "OK");
                    return;
                }

                var reason = await Shell.Current.DisplayPromptAsync(
                    "Suspension",
                    "Raison de la suspension :",
                    "Suspendre",
                    "Annuler",
                    placeholder: "Raison...",
                    initialValue: $"Suite au signalement: {report.Reason}");

                if (string.IsNullOrWhiteSpace(reason)) return;

                var success = await _adminService.SuspendUserAsync(report.ReportedItemId, reason);
                
                if (success)
                {
                    await _adminService.UpdateReportStatusAsync(report.Id, "ActionTaken", 
                        $"Utilisateur suspendu: {reason}");
                    
                    await Shell.Current.DisplayAlert("Succès", 
                        "Utilisateur suspendu et signalement résolu.", "OK");
                    await LoadReportsAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de suspendre l'utilisateur.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Moderation] Erreur SuspendReportedUserAsync: {ex.Message}");
            }
        }

        private async Task DeleteReportedAnnonceAsync(Report? report)
        {
            if (report == null || string.IsNullOrEmpty(report.ReportedItemId)) return;

            try
            {
                if (report.ReportedItemType != "Annonce")
                {
                    await Shell.Current.DisplayAlert("Info", 
                        "Cette action n'est disponible que pour les signalements d'annonces.", "OK");
                    return;
                }

                var confirm = await Shell.Current.DisplayAlert(
                    "Supprimer l'annonce",
                    "Voulez-vous supprimer cette annonce signalée ?",
                    "Supprimer",
                    "Annuler");

                if (!confirm) return;

                var success = await _adminService.DeleteAnnonceAsync(report.ReportedItemId, 
                    $"Suite au signalement: {report.Reason}");
                
                if (success)
                {
                    await _adminService.UpdateReportStatusAsync(report.Id, "ActionTaken", 
                        "Annonce supprimée");
                    
                    await Shell.Current.DisplayAlert("Succès", 
                        "Annonce supprimée et signalement résolu.", "OK");
                    await LoadReportsAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de supprimer l'annonce.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Moderation] Erreur DeleteReportedAnnonceAsync: {ex.Message}");
            }
        }

        #endregion
    }
}

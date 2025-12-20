using DonTroc.Models;
using DonTroc.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels
{
    public class ModerationViewModel : BaseViewModel
    {
        private readonly ReportService _reportService;

        public ModerationViewModel(ReportService reportService)
        {
            _reportService = reportService;
            LoadReportsCommand = new Command(async void () => await LoadReports());
            UpdateReportStatusCommand = new Command<Report>(async void (report) => await UpdateReportStatus(report));
        }

        public ObservableCollection<Report> Reports { get; } = new ObservableCollection<Report>();
        public ICommand LoadReportsCommand { get; }
        public ICommand UpdateReportStatusCommand { get; }

        private async Task LoadReports()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Reports.Clear();
                var reports = await _reportService.GetAllReports();
                foreach (var report in reports)
                {
                    Reports.Add(report);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateReportStatus(Report? report)
        {
            if (report == null)
                return;

            // This is a simplified version. In a real app, you would show a dialog
            // to the moderator to enter notes and select a new status.
            string newStatus = await Application.Current?.MainPage?.DisplayActionSheet(
                "Update Status",
                "Cancel",
                null,
                "Pending", "Reviewed", "ActionTaken")!;

            if (newStatus != "Cancel")
            {
                string notes =
                    await Application.Current.MainPage?.DisplayPromptAsync("Notes", "Enter moderation notes:")!;
                await _reportService.UpdateReportStatus(report.Id, newStatus, notes);
                await LoadReports(); // Refresh the list
            }
        }
    }
}
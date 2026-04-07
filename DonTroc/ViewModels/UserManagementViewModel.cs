// Fichier: DonTroc/ViewModels/UserManagementViewModel.cs
// ViewModel pour la gestion des utilisateurs

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
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly AdminService _adminService;
        
        private string _searchQuery = string.Empty;
        private string _selectedRoleFilter = "Tous";
        private bool _showSuspendedOnly;
        private UserProfile? _selectedUser;
        private bool _isAdmin;
        private int _totalUsers;
        private int _filteredCount;

        public UserManagementViewModel(AdminService adminService)
        {
            _adminService = adminService;
            Users = new ObservableCollection<UserProfile>();
            FilteredUsers = new ObservableCollection<UserProfile>();
            
            LoadUsersCommand = new Command(async () => await LoadUsersAsync());
            ApplyFiltersCommand = new Command(ApplyFilters);
            SuspendUserCommand = new Command<UserProfile>(async (user) => await SuspendUserAsync(user));
            UnsuspendUserCommand = new Command<UserProfile>(async (user) => await UnsuspendUserAsync(user));
            ChangeRoleCommand = new Command<UserProfile>(async (user) => await ChangeRoleAsync(user));
            DeleteUserCommand = new Command<UserProfile>(async (user) => await DeleteUserAsync(user));
            ViewUserDetailsCommand = new Command<UserProfile>(async (user) => await ViewUserDetailsAsync(user));
        }

        #region Propriétés

        public ObservableCollection<UserProfile> Users { get; }
        public ObservableCollection<UserProfile> FilteredUsers { get; }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                    ApplyFilters();
            }
        }

        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (SetProperty(ref _selectedRoleFilter, value))
                    ApplyFilters();
            }
        }

        public bool ShowSuspendedOnly
        {
            get => _showSuspendedOnly;
            set
            {
                if (SetProperty(ref _showSuspendedOnly, value))
                    ApplyFilters();
            }
        }

        public UserProfile? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set => SetProperty(ref _totalUsers, value);
        }

        public int FilteredCount
        {
            get => _filteredCount;
            set => SetProperty(ref _filteredCount, value);
        }

        public string[] RoleFilters => new[] { "Tous", "Standard", "Modérateur", "Admin" };

        #endregion

        #region Commandes

        public ICommand LoadUsersCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand SuspendUserCommand { get; }
        public ICommand UnsuspendUserCommand { get; }
        public ICommand ChangeRoleCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserDetailsCommand { get; }

        #endregion

        #region Méthodes

        private async Task LoadUsersAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                IsAdmin = await _adminService.IsCurrentUserAdminAsync();
                
                Users.Clear();
                var users = await _adminService.GetAllUsersAsync();
                
                foreach (var user in users.OrderByDescending(u => u.DateInscription))
                {
                    Users.Add(user);
                }

                TotalUsers = Users.Count;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagement] Erreur: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            FilteredUsers.Clear();

            var filtered = Users.AsEnumerable();

            // Filtre par recherche
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.ToLower();
                filtered = filtered.Where(u =>
                    (u.Name?.ToLower().Contains(query) ?? false) ||
                    (u.Email?.ToLower().Contains(query) ?? false) ||
                    (u.Id?.ToLower().Contains(query) ?? false)
                );
            }

            // Filtre par rôle
            if (SelectedRoleFilter != "Tous")
            {
                filtered = SelectedRoleFilter switch
                {
                    "Standard" => filtered.Where(u => u.Role == UserRole.Standard),
                    "Modérateur" => filtered.Where(u => u.Role == UserRole.Moderator),
                    "Admin" => filtered.Where(u => u.Role == UserRole.Admin),
                    _ => filtered
                };
            }

            // Filtre suspendus
            if (ShowSuspendedOnly)
            {
                filtered = filtered.Where(u => u.IsSuspended);
            }

            foreach (var user in filtered)
            {
                FilteredUsers.Add(user);
            }

            FilteredCount = FilteredUsers.Count;
        }

        private async Task SuspendUserAsync(UserProfile? user)
        {
            if (user == null) return;

            try
            {
                var reason = await Shell.Current.DisplayPromptAsync(
                    "Suspension",
                    $"Raison de la suspension de {user.Name} :",
                    "Suspendre",
                    "Annuler",
                    placeholder: "Raison...");

                if (string.IsNullOrWhiteSpace(reason)) return;

                // Demander la durée
                var duration = await Shell.Current.DisplayActionSheet(
                    "Durée de la suspension",
                    "Annuler",
                    null,
                    "7 jours",
                    "30 jours",
                    "90 jours",
                    "Indéfinie");

                if (duration == "Annuler" || string.IsNullOrEmpty(duration)) return;

                DateTime? endDate = duration switch
                {
                    "7 jours" => DateTime.UtcNow.AddDays(7),
                    "30 jours" => DateTime.UtcNow.AddDays(30),
                    "90 jours" => DateTime.UtcNow.AddDays(90),
                    _ => null
                };

                var success = await _adminService.SuspendUserAsync(user.Id, reason, endDate);
                
                if (success)
                {
                    await Shell.Current.DisplayAlert("Succès", 
                        $"{user.Name} a été suspendu.", "OK");
                    await LoadUsersAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de suspendre l'utilisateur.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagement] Erreur SuspendUserAsync: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Une erreur s'est produite.", "OK");
            }
        }

        private async Task UnsuspendUserAsync(UserProfile? user)
        {
            if (user == null) return;

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Lever la suspension",
                    $"Voulez-vous lever la suspension de {user.Name} ?",
                    "Oui",
                    "Non");

                if (!confirm) return;

                var success = await _adminService.UnsuspendUserAsync(user.Id);
                
                if (success)
                {
                    await Shell.Current.DisplayAlert("Succès", 
                        $"Suspension de {user.Name} levée.", "OK");
                    await LoadUsersAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de lever la suspension.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagement] Erreur UnsuspendUserAsync: {ex.Message}");
            }
        }

        private async Task ChangeRoleAsync(UserProfile? user)
        {
            if (user == null) return;
            
            if (!IsAdmin)
            {
                await Shell.Current.DisplayAlert("Accès refusé", 
                    "Seuls les administrateurs peuvent changer les rôles.", "OK");
                return;
            }

            try
            {
                var newRole = await Shell.Current.DisplayActionSheet(
                    $"Changer le rôle de {user.Name}",
                    "Annuler",
                    null,
                    "Standard",
                    "Modérateur",
                    "Admin");

                if (newRole == "Annuler" || string.IsNullOrEmpty(newRole)) return;

                var role = newRole switch
                {
                    "Modérateur" => UserRole.Moderator,
                    "Admin" => UserRole.Admin,
                    _ => UserRole.Standard
                };

                // Confirmation pour promotion admin
                if (role == UserRole.Admin)
                {
                    var confirm = await Shell.Current.DisplayAlert(
                        "⚠️ Promotion Admin",
                        $"Êtes-vous sûr de vouloir promouvoir {user.Name} en administrateur ? Cette action lui donnera tous les droits.",
                        "Confirmer",
                        "Annuler");

                    if (!confirm) return;
                }

                var success = await _adminService.ChangeUserRoleAsync(user.Id, role);
                
                if (success)
                {
                    await Shell.Current.DisplayAlert("Succès", 
                        $"Rôle de {user.Name} changé en {newRole}.", "OK");
                    await LoadUsersAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de changer le rôle.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagement] Erreur ChangeRoleAsync: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Une erreur s'est produite.", "OK");
            }
        }

        private async Task DeleteUserAsync(UserProfile? user)
        {
            if (user == null) return;
            
            if (!IsAdmin)
            {
                await Shell.Current.DisplayAlert("Accès refusé", 
                    "Seuls les administrateurs peuvent supprimer des comptes.", "OK");
                return;
            }

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "⚠️ Suppression définitive",
                    $"Voulez-vous vraiment supprimer le compte de {user.Name} ?\n\nCette action est irréversible et supprimera toutes ses données (annonces, messages, etc.).",
                    "Supprimer",
                    "Annuler");

                if (!confirm) return;

                // Double confirmation
                var doubleConfirm = await Shell.Current.DisplayAlert(
                    "Confirmation finale",
                    "Êtes-vous absolument sûr ?",
                    "OUI, SUPPRIMER",
                    "Annuler");

                if (!doubleConfirm) return;

                var success = await _adminService.DeleteUserAsync(user.Id);
                
                if (success)
                {
                    await Shell.Current.DisplayAlert("Succès", 
                        $"Le compte de {user.Name} a été supprimé.", "OK");
                    await LoadUsersAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Impossible de supprimer le compte.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagement] Erreur DeleteUserAsync: {ex.Message}");
            }
        }

        private async Task ViewUserDetailsAsync(UserProfile? user)
        {
            if (user == null) return;
            
            try
            {
                SelectedUser = user;
                
                // Afficher les détails dans une popup
                var info = $"👤 {user.Name}\n" +
                           $"📧 {user.Email}\n" +
                           $"🎭 Rôle: {user.Role}\n" +
                           $"📅 Inscrit: {user.DateInscription:dd/MM/yyyy}\n" +
                           $"⭐ Note: {user.NoteMoyenne:F1}/5 ({user.NombreEvaluations} avis)\n" +
                           $"🏆 Points: {user.Points}\n" +
                           $"🚫 Suspendu: {(user.IsSuspended ? "Oui" : "Non")}";

                if (user.IsSuspended)
                {
                    info += $"\n📝 Raison: {user.SuspensionReason}\n" +
                            $"📅 Depuis: {user.SuspensionDate:dd/MM/yyyy}";
                    if (user.SuspensionEndDate.HasValue)
                        info += $"\n⏰ Jusqu'au: {user.SuspensionEndDate:dd/MM/yyyy}";
                }

                await Shell.Current.DisplayAlert("Détails utilisateur", info, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagement] Erreur ViewUserDetailsAsync: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'afficher les détails.", "OK");
            }
        }

        #endregion
    }
}


using System.Diagnostics;
using DonTroc.Models;
using DonTroc.Services;

namespace DonTroc.Views;

public partial class AdminSetupPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirebaseService _firebaseService;
    private readonly AdminService _adminService;
    
    // ⚠️ CLÉ SECRÈTE D'ADMINISTRATION - Changez cette valeur !
    // Seule la personne qui connaît cette clé peut devenir admin
    private const string ADMIN_SECRET_KEY = "DonTroc2026Admin!SecretKey@BachirDev";
    
    private string? _currentUserId;
    private string? _currentUserEmail;
    private UserProfile? _currentProfile;
    private bool _hasAdmins;

    public AdminSetupPage(AuthService authService, FirebaseService firebaseService, AdminService adminService)
    {
        InitializeComponent();
        
        _authService = authService;
        _firebaseService = firebaseService;
        _adminService = adminService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCurrentUserInfo();
    }

    private async Task LoadCurrentUserInfo()
    {
        try
        {
            StatusLabel.Text = "Chargement...";
            StatusLabel.TextColor = Colors.Gray;

            // Récupérer l'utilisateur Firebase Auth
            var user = await _authService.GetCurrentUserAsync();
            
            if (user == null)
            {
                StatusLabel.Text = "❌ Vous n'êtes pas connecté. Veuillez vous connecter d'abord.";
                StatusLabel.TextColor = Colors.Red;
                CurrentUserIdLabel.Text = "ID: Non connecté";
                CurrentUserEmailLabel.Text = "Email: Non connecté";
                ProfileStatusLabel.Text = "Statut: Non connecté";
                RoleStatusLabel.Text = "Rôle: N/A";
                return;
            }

            _currentUserId = user.Uid;
            _currentUserEmail = user.Email ?? "Email inconnu";

            CurrentUserIdLabel.Text = $"ID: {_currentUserId}";
            CurrentUserEmailLabel.Text = $"Email: {_currentUserEmail}";

            // Vérifier si le profil existe
            _currentProfile = await _firebaseService.GetUserProfileAsync(_currentUserId);
            
            if (_currentProfile != null)
            {
                ProfileStatusLabel.Text = $"✅ Profil existant (Nom: {_currentProfile.Name})";
                ProfileStatusLabel.TextColor = Colors.Green;
                
                RoleStatusLabel.Text = $"Rôle: {_currentProfile.Role}";
                RoleStatusLabel.TextColor = _currentProfile.IsAdmin ? Colors.Green : 
                                            _currentProfile.IsModerator ? Colors.Orange : Colors.Gray;
                
                CreateProfileButton.IsVisible = false;
            }
            else
            {
                ProfileStatusLabel.Text = "⚠️ Profil non trouvé dans UserProfiles";
                ProfileStatusLabel.TextColor = Colors.Orange;
                RoleStatusLabel.Text = "Rôle: N/A (profil manquant)";
                CreateProfileButton.IsVisible = true;
            }

            // Vérifier s'il y a des admins
            var allUsers = await _adminService.GetAllUsersAsync();
            _hasAdmins = allUsers.Any(u => u.Role == UserRole.Admin);
            
            // Afficher les boutons appropriés
            if (_currentProfile != null)
            {
                if (_currentProfile.IsAdmin)
                {
                    // L'utilisateur est déjà admin, pas besoin de clé secrète
                    GoToAdminButton.IsVisible = true;
                    PromoteToAdminButton.IsVisible = false;
                    CreateProfileButton.IsVisible = false;
                    SecretKeyFrame.IsVisible = false; // Cacher le champ de clé
                    StatusLabel.Text = "✅ Vous êtes déjà administrateur";
                    StatusLabel.TextColor = Colors.Green;
                }
                else
                {
                    // Permettre de devenir admin avec la clé secrète
                    PromoteToAdminButton.IsVisible = true;
                    GoToAdminButton.IsVisible = false;
                    CreateProfileButton.IsVisible = false;
                    SecretKeyFrame.IsVisible = true; // Afficher le champ de clé
                    StatusLabel.Text = "ℹ️ Entrez la clé secrète pour devenir admin";
                    StatusLabel.TextColor = Colors.Blue;
                }
            }
            else
            {
                // Permettre de créer un profil admin avec la clé secrète
                CreateProfileButton.IsVisible = true;
                PromoteToAdminButton.IsVisible = false;
                GoToAdminButton.IsVisible = false;
                SecretKeyFrame.IsVisible = true; // Afficher le champ de clé
                StatusLabel.Text = "⚠️ Créez votre profil admin avec la clé secrète";
                StatusLabel.TextColor = Colors.Orange;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AdminSetupPage] Erreur: {ex.Message}");
            StatusLabel.Text = $"❌ Erreur: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
    }

    private async void OnCreateProfileClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            await DisplayAlert("Erreur", "ID utilisateur non disponible", "OK");
            return;
        }

        // ✅ Vérifier la clé secrète
        if (!ValidateSecretKey())
        {
            await DisplayAlert("❌ Accès refusé", 
                "La clé secrète est incorrecte. Vous ne pouvez pas créer un profil admin.", "OK");
            return;
        }

        try
        {
            CreateProfileButton.IsEnabled = false;
            StatusLabel.Text = "Création du profil...";

            // Demander le nom
            var name = await DisplayPromptAsync(
                "Nom", 
                "Entrez votre nom:",
                initialValue: "Admin",
                maxLength: 50);

            if (string.IsNullOrWhiteSpace(name))
            {
                StatusLabel.Text = "Création annulée";
                CreateProfileButton.IsEnabled = true;
                return;
            }

            var success = await _adminService.CreateAdminProfileAsync(
                _currentUserId, 
                name, 
                _currentUserEmail ?? "");

            if (success)
            {
                StatusLabel.Text = "✅ Profil créé avec succès (en tant qu'Admin) !";
                StatusLabel.TextColor = Colors.Green;
                SecretKeyEntry.Text = ""; // Effacer la clé
                await LoadCurrentUserInfo();
            }
            else
            {
                StatusLabel.Text = "❌ Échec de la création du profil";
                StatusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Erreur: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
        finally
        {
            CreateProfileButton.IsEnabled = true;
        }
    }

    private async void OnPromoteToAdminClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            await DisplayAlert("Erreur", "ID utilisateur non disponible", "OK");
            return;
        }

        // ✅ Vérifier la clé secrète
        if (!ValidateSecretKey())
        {
            await DisplayAlert("❌ Accès refusé", 
                "La clé secrète est incorrecte. Vous ne pouvez pas devenir administrateur.", "OK");
            return;
        }

        var confirm = await DisplayAlert(
            "Confirmation",
            "Voulez-vous devenir administrateur de l'application ?",
            "Oui", "Non");

        if (!confirm) return;

        try
        {
            PromoteToAdminButton.IsEnabled = false;
            StatusLabel.Text = "Promotion en cours...";

            var success = await _adminService.PromoteToAdminWithKeyAsync(_currentUserId);

            if (success)
            {
                StatusLabel.Text = "✅ Vous êtes maintenant Admin !";
                StatusLabel.TextColor = Colors.Green;
                SecretKeyEntry.Text = ""; // Effacer la clé
                _adminService.InvalidateAdminCache();
                await LoadCurrentUserInfo();
            }
            else
            {
                StatusLabel.Text = "❌ Erreur lors de la promotion";
                StatusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Erreur: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
        finally
        {
            PromoteToAdminButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Valide la clé secrète entrée par l'utilisateur
    /// </summary>
    private bool ValidateSecretKey()
    {
        var enteredKey = SecretKeyEntry?.Text?.Trim();
        return !string.IsNullOrEmpty(enteredKey) && enteredKey == ADMIN_SECRET_KEY;
    }

    private async void OnGoToAdminClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminDashboardPage));
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        _adminService.InvalidateAdminCache();
        await LoadCurrentUserInfo();
    }
}


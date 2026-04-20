using System;
using System.Diagnostics.CodeAnalysis;
using DonTroc.Models;
using DonTroc.Services;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;

namespace DonTroc.ViewModels
{
    public class EditProfileViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly FirebaseService _firebaseService;
        private readonly ProximityNotificationService _proximityNotificationService;

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string? _profilePictureUrl;
        public string? ProfilePictureUrl
        {
            get => _profilePictureUrl;
            set => SetProperty(ref _profilePictureUrl, value);
        }

        private FileResult? _newProfilePicture; // Stocke la nouvelle photo sélectionnée

        // Propriétés pour les notifications de proximité
        private bool _proximityNotificationsEnabled = true;
        public bool ProximityNotificationsEnabled
        {
            get => _proximityNotificationsEnabled;
            set => SetProperty(ref _proximityNotificationsEnabled, value);
        }

        private double _notificationRadius = 5.0;
        public double NotificationRadius
        {
            get => _notificationRadius;
            set
            {
                if (SetProperty(ref _notificationRadius, Math.Round(value, 1)))
                {
                    OnPropertyChanged(nameof(NotificationRadiusText));
                }
            }
        }

        public string NotificationRadiusText => $"{NotificationRadius:F1} km";

        // === PRÉFÉRENCES DE RAPPELS PUSH SERVEUR ===

        private bool _reminderJ1Enabled = true;
        public bool ReminderJ1Enabled
        {
            get => _reminderJ1Enabled;
            set { if (SetProperty(ref _reminderJ1Enabled, value)) _ = SaveNotificationPreferences(); }
        }

        private bool _reminderJ3Enabled = true;
        public bool ReminderJ3Enabled
        {
            get => _reminderJ3Enabled;
            set { if (SetProperty(ref _reminderJ3Enabled, value)) _ = SaveNotificationPreferences(); }
        }

        private bool _reminderJ7Enabled = true;
        public bool ReminderJ7Enabled
        {
            get => _reminderJ7Enabled;
            set { if (SetProperty(ref _reminderJ7Enabled, value)) _ = SaveNotificationPreferences(); }
        }

        private bool _reminderJ14Enabled = true;
        public bool ReminderJ14Enabled
        {
            get => _reminderJ14Enabled;
            set { if (SetProperty(ref _reminderJ14Enabled, value)) _ = SaveNotificationPreferences(); }
        }

        public ICommand SaveProfileCommand { get; }
        public ICommand PickPhotoCommand { get; }
        public ICommand UpdateLocationCommand { get; }
        public ICommand ToggleProximityNotificationsCommand { get; }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EditProfileViewModel))]
        public EditProfileViewModel(AuthService authService, FirebaseService firebaseService, 
            ProximityNotificationService proximityNotificationService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
            _proximityNotificationService = proximityNotificationService;

            SaveProfileCommand = new Command(async () => await OnSaveProfile(), () => !IsBusy);
            PickPhotoCommand = new Command(async () => await OnPickPhoto());
            UpdateLocationCommand = new Command(async () => await OnUpdateLocation());
            ToggleProximityNotificationsCommand = new Command(async () => await OnToggleProximityNotifications());

            LoadUserProfile();
            _ = LoadProximitySettings();
            _ = LoadNotificationPreferences();
        }

        private async Task LoadProximitySettings()
        {
            try
            {
                var (enabled, radius) = await _proximityNotificationService.GetProximityNotificationStatusAsync();
                ProximityNotificationsEnabled = enabled;
                NotificationRadius = radius;
            }
            catch
            {
                // Valeurs par défaut
                ProximityNotificationsEnabled = true;
                NotificationRadius = 5.0;
            }
        }

        private async Task OnUpdateLocation()
        {
            try
            {
                IsBusy = true;
                await _proximityNotificationService.UpdateUserLocationAsync();
                await App.Current!.MainPage!.DisplayAlert("Position mise à jour", 
                    "Votre position a été mise à jour. Vous recevrez des notifications pour les annonces à proximité.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur mise à jour position: {ex.Message}");
                await App.Current!.MainPage!.DisplayAlert("Erreur", 
                    "Impossible de mettre à jour votre position. Vérifiez vos paramètres de localisation.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnToggleProximityNotifications()
        {
            try
            {
                await _proximityNotificationService.ConfigureProximityNotificationsAsync(
                    ProximityNotificationsEnabled, NotificationRadius);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur configuration proximité: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge les préférences de rappels push depuis le profil Firebase
        /// </summary>
        private async Task LoadNotificationPreferences()
        {
            try
            {
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId)) return;

                var profile = await _firebaseService.GetUserProfileAsync(userId);
                if (profile?.NotificationPreferences != null)
                {
                    var prefs = profile.NotificationPreferences;
                    _reminderJ1Enabled = !prefs.ContainsKey("reminder_j1") || prefs["reminder_j1"];
                    _reminderJ3Enabled = !prefs.ContainsKey("reminder_j3") || prefs["reminder_j3"];
                    _reminderJ7Enabled = !prefs.ContainsKey("reminder_j7") || prefs["reminder_j7"];
                    _reminderJ14Enabled = !prefs.ContainsKey("reminder_j14") || prefs["reminder_j14"];

                    OnPropertyChanged(nameof(ReminderJ1Enabled));
                    OnPropertyChanged(nameof(ReminderJ3Enabled));
                    OnPropertyChanged(nameof(ReminderJ7Enabled));
                    OnPropertyChanged(nameof(ReminderJ14Enabled));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditProfile] Erreur chargement prefs notif: {ex.Message}");
            }
        }

        /// <summary>
        /// Sauvegarde les préférences de rappels push dans Firebase
        /// </summary>
        private async Task SaveNotificationPreferences()
        {
            try
            {
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId)) return;

                var prefs = new Dictionary<string, bool>
                {
                    ["reminder_j1"] = ReminderJ1Enabled,
                    ["reminder_j3"] = ReminderJ3Enabled,
                    ["reminder_j7"] = ReminderJ7Enabled,
                    ["reminder_j14"] = ReminderJ14Enabled,
                };

                await _firebaseService.UpdateNotificationPreferencesAsync(userId, prefs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditProfile] Erreur sauvegarde prefs notif: {ex.Message}");
            }
        }

        private async void LoadUserProfile() // Méthode pour charger le profil utilisateur
        {
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var userProfile = await _firebaseService.GetUserProfileAsync(userId);
                if (userProfile != null)
                {
                    Name = userProfile.Name;
                    ProfilePictureUrl = userProfile.ProfilePictureUrl;
                }
            }
        }

        private async Task OnPickPhoto() // Méthode pour sélectionner une photo depuis la galerie
        {
            try
            {
                _newProfilePicture = await MediaPicker.PickPhotoAsync();
                if (_newProfilePicture != null)
                {
                    // Affiche la nouvelle image localement avant le téléversement
                    ProfilePictureUrl = _newProfilePicture.FullPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error picking photo: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Erreur", "Impossible de sélectionner une photo.", "OK");
            }
        }

        private async Task OnSaveProfile() // Méthode pour sauvegarder le profil utilisateur
        {
            if (IsBusy) return;

            IsBusy = true;
            ((Command)SaveProfileCommand).ChangeCanExecute();

            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await App.Current.MainPage.DisplayAlert("Erreur", "Utilisateur non connecté.", "OK");
                IsBusy = false;
                ((Command)SaveProfileCommand).ChangeCanExecute();
                return;
            }

            string? uploadedImageUrl = ProfilePictureUrl; 

            if (_newProfilePicture != null)
            {
                try
                {
                    var stream = await _newProfilePicture.OpenReadAsync();
                    if (string.IsNullOrEmpty(userId))
                    {
                        throw new InvalidOperationException("Utilisateur non authentifié");
                    }
                    uploadedImageUrl = await _firebaseService.UploadProfilePictureAsync(stream, _newProfilePicture.FileName, userId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error uploading image: {ex.Message}");
                    await App.Current.MainPage.DisplayAlert("Erreur", "Erreur lors du téléversement de l'image.", "OK");
                    IsBusy = false;
                    ((Command)SaveProfileCommand).ChangeCanExecute();
                    return;
                }
            }

            try
            {
                // Charger le profil existant pour ne pas écraser les autres champs (Role, Points, Email, etc.)
                var userProfile = await _firebaseService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    userProfile = new UserProfile { Id = userId, Name = Name };
                }

                // Mettre à jour uniquement les champs modifiés
                userProfile.Name = Name;
                userProfile.ProfilePictureUrl = uploadedImageUrl;

                await _firebaseService.SaveUserProfileAsync(userProfile);
                await App.Current.MainPage.DisplayAlert("Succès", "Profil mis à jour.", "OK");
                await Shell.Current.GoToAsync(".."); 
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Erreur", "Impossible de sauvegarder le profil.", "OK");
            }
            finally
            {
                IsBusy = false;
                ((Command)SaveProfileCommand).ChangeCanExecute();
            }
        }
    }
}

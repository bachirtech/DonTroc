using System;
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

        public ICommand SaveProfileCommand { get; }
        public ICommand PickPhotoCommand { get; }
        public ICommand UpdateLocationCommand { get; }
        public ICommand ToggleProximityNotificationsCommand { get; }

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

            var userProfile = new UserProfile
            {
                Id = userId,
                Name = Name,
                ProfilePictureUrl = uploadedImageUrl
            };

            try
            {
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

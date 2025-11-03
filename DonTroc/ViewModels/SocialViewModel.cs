using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels
{
    /// <summary>
    /// ViewModel pour les fonctionnalités sociales
    /// </summary>
    public class SocialViewModel : BaseViewModel
    {
        private readonly SocialService _socialService;
        private readonly AuthService _authService;

        public SocialViewModel(SocialService socialService, AuthService authService)
        {
            _socialService = socialService;
            _authService = authService;

            // Initialisation des collections
            Friends = new ObservableCollection<UserProfile>();
            FriendsActivity = new ObservableCollection<FriendActivity>();

            // Initialisation des commandes
            GenerateReferralCodeCommand = new Command(async () => await GenerateReferralCodeAsync());
            UseReferralCodeCommand = new Command<string>(async (code) => await UseReferralCodeAsync(code));
            ShareAnnonceCommand = new Command<(Annonce annonce, SocialPlatform platform)>(
                async (param) => await ShareAnnonceAsync(param.annonce, param.platform));
            LoadSocialDataCommand = new Command(async () => await LoadSocialDataAsync());
            RefreshActivityCommand = new Command(async () => await RefreshActivityAsync());
        }

        #region Propriétés

        public ObservableCollection<UserProfile> Friends { get; }
        public ObservableCollection<FriendActivity> FriendsActivity { get; }

        private ReferralCode? _userReferralCode;
        public ReferralCode? UserReferralCode
        {
            get => _userReferralCode;
            set => SetProperty(ref _userReferralCode, value);
        }

        private string _referralCodeToUse = string.Empty;
        public string ReferralCodeToUse
        {
            get => _referralCodeToUse;
            set => SetProperty(ref _referralCodeToUse, value);
        }

        private SocialStats? _socialStats;
        public SocialStats? SocialStats
        {
            get => _socialStats;
            set => SetProperty(ref _socialStats, value);
        }

        private bool _showReferralDialog;
        public bool ShowReferralDialog
        {
            get => _showReferralDialog;
            set => SetProperty(ref _showReferralDialog, value);
        }

        private bool _showShareDialog;
        public bool ShowShareDialog
        {
            get => _showShareDialog;
            set => SetProperty(ref _showShareDialog, value);
        }

        private Annonce? _annonceToShare;
        public Annonce? AnnonceToShare
        {
            get => _annonceToShare;
            set => SetProperty(ref _annonceToShare, value);
        }

        #endregion

        #region Commandes

        public ICommand GenerateReferralCodeCommand { get; }
        public ICommand UseReferralCodeCommand { get; }
        public ICommand ShareAnnonceCommand { get; }
        public ICommand LoadSocialDataCommand { get; }
        public ICommand RefreshActivityCommand { get; }

        #endregion

        #region Méthodes

        /// <summary>
        /// Charge toutes les données sociales
        /// </summary>
        public async Task LoadSocialDataAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var user = await _authService.GetCurrentUserAsync();
                if (user == null) return;

                // Charger le code de parrainage
                UserReferralCode = await _socialService.GetUserReferralCodeAsync(user.Uid);

                // Charger les amis
                var friends = await _socialService.GetFriendsAsync(user.Uid);
                Friends.Clear();
                foreach (var friend in friends)
                {
                    Friends.Add(friend);
                }

                // Charger l'activité des amis
                await RefreshActivityAsync();

                // Charger les statistiques
                SocialStats = await _socialService.GetSocialStatsAsync(user.Uid);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", 
                    $"Impossible de charger les données sociales : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Génère un nouveau code de parrainage
        /// </summary>
        private async Task GenerateReferralCodeAsync()
        {
            try
            {
                UserReferralCode = await _socialService.GenerateReferralCodeAsync();
                await Shell.Current.DisplayAlert("Succès", 
                    $"Votre code de parrainage : {UserReferralCode.Code}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", 
                    $"Impossible de générer le code : {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Utilise un code de parrainage
        /// </summary>
        private async Task UseReferralCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                await Shell.Current.DisplayAlert("Erreur", "Veuillez saisir un code de parrainage", "OK");
                return;
            }

            try
            {
                var success = await _socialService.UseReferralCodeAsync(code.ToUpper());
                if (success)
                {
                    await Shell.Current.DisplayAlert("Félicitations !", 
                        "Code de parrainage utilisé avec succès ! Vous avez gagné des points et un nouvel ami.", "Super !");
                    
                    // Recharger les données
                    await LoadSocialDataAsync();
                    ShowReferralDialog = false;
                    ReferralCodeToUse = string.Empty;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erreur", 
                        "Code de parrainage invalide, expiré ou déjà utilisé", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", 
                    $"Impossible d'utiliser le code : {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Partage une annonce sur les réseaux sociaux
        /// </summary>
        private async Task ShareAnnonceAsync(Annonce annonce, SocialPlatform platform)
        {
            try
            {
                var success = await _socialService.ShareAnnonceAsync(annonce, platform);
                if (success)
                {
                    await Shell.Current.DisplayAlert("Merci !", 
                        "Annonce partagée avec succès ! Vous avez gagné des points.", "Super !");
                    
                    // Recharger les statistiques
                    var user = await _authService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        SocialStats = await _socialService.GetSocialStatsAsync(user.Uid);
                    }
                }
                ShowShareDialog = false;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", 
                    $"Impossible de partager : {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Actualise l'activité des amis
        /// </summary>
        private async Task RefreshActivityAsync()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null) return;

                var activities = await _socialService.GetFriendsActivityAsync(user.Uid);
                FriendsActivity.Clear();
                foreach (var activity in activities)
                {
                    FriendsActivity.Add(activity);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du rafraîchissement de l'activité : {ex.Message}");
            }
        }

        /// <summary>
        /// Ouvre la boîte de dialogue de partage pour une annonce
        /// </summary>
        public void OpenShareDialog(Annonce annonce)
        {
            AnnonceToShare = annonce;
            ShowShareDialog = true;
        }

        /// <summary>
        /// Ouvre la boîte de dialogue de parrainage
        /// </summary>
        public void OpenReferralDialog()
        {
            ShowReferralDialog = true;
        }

        #endregion
    }
}

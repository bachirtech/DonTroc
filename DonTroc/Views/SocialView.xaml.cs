using System;
using DonTroc.Models;
using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views
{
    public partial class SocialView : ContentPage
    {
        private SocialViewModel? _viewModel;

        public SocialView(SocialViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
           base.OnAppearing();
            if (_viewModel?.LoadSocialDataCommand != null)
            {
                _viewModel.LoadSocialDataCommand.Execute(null);
            }
        }

        #region Gestionnaires d'événements pour les dialogs

        private void OnUseReferralCodeClicked(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.OpenReferralDialog();
            }
        }

        private void OnCancelReferralDialog(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ShowReferralDialog = false;
                _viewModel.ReferralCodeToUse = string.Empty;
            }
        }

        private void OnCancelShareDialog(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ShowShareDialog = false;
                _viewModel.AnnonceToShare = null;
            }
        }

        private void OnCloseReferralCodeDialog(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.UserReferralCode = null;
            }
        }

        #endregion

        #region Gestionnaires de partage

        private void OnShareWhatsApp(object sender, EventArgs e)
        {
            if (_viewModel?.AnnonceToShare != null)
            {
                _viewModel.ShareAnnonceCommand.Execute((_viewModel.AnnonceToShare, SocialPlatform.WhatsApp));
            }
        }

        private void OnShareTelegram(object sender, EventArgs e)
        {
            if (_viewModel?.AnnonceToShare != null)
            {
                _viewModel.ShareAnnonceCommand.Execute((_viewModel.AnnonceToShare, SocialPlatform.Telegram));
            }
        }

        private void OnShareFacebook(object sender, EventArgs e)
        {
            if (_viewModel?.AnnonceToShare != null)
            {
                _viewModel.ShareAnnonceCommand.Execute((_viewModel.AnnonceToShare, SocialPlatform.Facebook));
            }
        }

        private void OnShareTwitter(object sender, EventArgs e)
        {
            if (_viewModel?.AnnonceToShare != null)
            {
                _viewModel.ShareAnnonceCommand.Execute((_viewModel.AnnonceToShare, SocialPlatform.Twitter));
            }
        }

        private void OnShareEmail(object sender, EventArgs e)
        {
            if (_viewModel?.AnnonceToShare != null)
            {
                _viewModel.ShareAnnonceCommand.Execute((_viewModel.AnnonceToShare, SocialPlatform.Email));
            }
        }

        private void OnShareSMS(object sender, EventArgs e)
        {
            if (_viewModel?.AnnonceToShare != null)
            {
                _viewModel.ShareAnnonceCommand.Execute((_viewModel.AnnonceToShare, SocialPlatform.SMS));
            }
        }

        #endregion
    }
}

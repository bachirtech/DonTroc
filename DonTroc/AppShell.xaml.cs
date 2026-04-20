﻿﻿﻿using DonTroc.Services;
using DonTroc.Views;
using System.ComponentModel;
using DonTroc.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc;

public partial class AppShell : Shell
{
    private readonly UnreadMessageService _unreadMessageService;

    public int TotalUnreadCount => _unreadMessageService?.TotalUnreadCount ?? 0;

    public AppShell(UnreadMessageService unreadMessageService) // Constructeur
    {
        // ⚠️ Initialiser le service AVANT BindingContext pour éviter NullReferenceException
        _unreadMessageService = unreadMessageService;
        _unreadMessageService.PropertyChanged += OnUnreadMessageServicePropertyChanged;

        InitializeComponent();
        
        // Le BindingContext est défini sur lui-même pour permettre les liaisons de données dans le XAML
        BindingContext = this;

        // Enregistre la route pour la page de création d'annonce
        Routing.RegisterRoute(nameof(CreationAnnonceView), typeof(CreationAnnonceView));
        // NOTE: AnnoncesView et DashboardView sont déjà définis comme Route dans AppShell.xaml (ShellContent)
        // Ne PAS les re-enregistrer ici, sinon Shell voit des routes dupliquées → "Ambiguous routes" au retour
        Routing.RegisterRoute(nameof(TransactionsView), typeof(TransactionsView)); // Enregistre la route pour la page des transactions
        Routing.RegisterRoute(nameof(TransactionDetailsView), typeof(TransactionDetailsView)); // Enregistre la route pour les détails de transaction
        Routing.RegisterRoute(nameof(ImageViewerView), typeof(ImageViewerView)); // Enregistre la route pour la visionneuse d'images
        // Enregistre la route pour la page de détail d'annonce
        Routing.RegisterRoute(nameof(AnnonceDetailView), typeof(AnnonceDetailView));

        // Enregistrement des autres routes importantes
        Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
        // Enregistre la route pour la page de modification du profil
        Routing.RegisterRoute(nameof(EditProfileView), typeof(EditProfileView));
        // Enregistre la route pour la page de modification d'annonce
        Routing.RegisterRoute(nameof(EditAnnonceView), typeof(EditAnnonceView));
        // Enregistre la route pour le système de notation et avis
        Routing.RegisterRoute(nameof(RatingView), typeof(RatingView));
        // Enregistre la route pour la vue carte interactive
        Routing.RegisterRoute(nameof(MapView), typeof(MapView));
        // Enregistre la route pour la page de récompenses et gamification
        Routing.RegisterRoute(nameof(RewardsPage), typeof(RewardsPage));
        // Enregistre la route pour la page de quiz
        Routing.RegisterRoute(nameof(QuizPage), typeof(QuizPage));
        // Enregistre la route pour la roue de la fortune
        Routing.RegisterRoute(nameof(WheelOfFortunePage), typeof(WheelOfFortunePage));
        // Enregistre la route pour la page de tous les badges
        Routing.RegisterRoute(nameof(AllBadgesPage), typeof(AllBadgesPage));
        // Route pour le classement
        Routing.RegisterRoute(nameof(LeaderboardView), typeof(LeaderboardView));
        // Route pour la page sociale (amis, parrainage)
        Routing.RegisterRoute(nameof(SocialView), typeof(SocialView));

        // Routes pour le système de propositions de troc
        Routing.RegisterRoute(nameof(TradeProposalPage), typeof(TradeProposalPage));
        Routing.RegisterRoute(nameof(TradeProposalsListPage), typeof(TradeProposalsListPage));
        
        // Routes pour le panneau d'administration
        Routing.RegisterRoute(nameof(AdminDashboardPage), typeof(AdminDashboardPage));
        Routing.RegisterRoute(nameof(UserManagementPage), typeof(UserManagementPage));
        Routing.RegisterRoute(nameof(AdminLogsPage), typeof(AdminLogsPage));
        Routing.RegisterRoute(nameof(ModerationPage), typeof(ModerationPage));
        Routing.RegisterRoute(nameof(AdminSetupPage), typeof(AdminSetupPage));

        UpdateMessagesBadge();
    }

    private void OnUnreadMessageServicePropertyChanged(object? sender, PropertyChangedEventArgs e) // Méthode pour gérer les changements de propriété dans le service de messages non lus
    {
        if (e.PropertyName == nameof(UnreadMessageService.TotalUnreadCount))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(TotalUnreadCount));
            });
        }
    }

    private void UpdateMessagesBadge() // Méthode pour mettre a jour le badge de messages 
    {
        OnPropertyChanged(nameof(TotalUnreadCount));
    }
}
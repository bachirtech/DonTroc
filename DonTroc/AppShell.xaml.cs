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

    public int TotalUnreadCount => _unreadMessageService.TotalUnreadCount;

    public AppShell(UnreadMessageService unreadMessageService) // Constructeur
    {
        InitializeComponent();
        
        // Le BindingContext est défini sur lui-même pour permettre les liaisons de données dans le XAML
        BindingContext = this;
        
        _unreadMessageService = unreadMessageService;
        _unreadMessageService.PropertyChanged += OnUnreadMessageServicePropertyChanged;

        // Enregistre la route pour la page de création d'annonce
        Routing.RegisterRoute(nameof(CreationAnnonceView), typeof(CreationAnnonceView));
        Routing.RegisterRoute(nameof(AnnoncesView), typeof(AnnoncesView)); // Enregistre la route pour la page des annonces
        Routing.RegisterRoute(nameof(DashboardView), typeof(DashboardView)); // Enregistre la route pour la page de tableau de bord
        Routing.RegisterRoute(nameof(TransactionsView), typeof(TransactionsView)); // Enregistre la route pour la page des transactions
        Routing.RegisterRoute(nameof(TransactionDetailsView), typeof(TransactionDetailsView)); // Enregistre la route pour les détails de transaction
        Routing.RegisterRoute(nameof(ImageViewerView), typeof(ImageViewerView)); // Enregistre la route pour la visionneuse d'images

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
        Routing.RegisterRoute(nameof(LoginViewModel), typeof(LoginViewModel));
        // Enregistre la route pour la page de récompenses et gamification
        Routing.RegisterRoute(nameof(RewardsPage), typeof(RewardsPage));
        // Enregistre la route pour la page de quiz
        Routing.RegisterRoute(nameof(QuizPage), typeof(QuizPage));
        // Enregistre la route pour la roue de la fortune
        Routing.RegisterRoute(nameof(WheelOfFortunePage), typeof(WheelOfFortunePage));
        // Enregistre la route pour la page de tous les badges
        Routing.RegisterRoute(nameof(AllBadgesPage), typeof(AllBadgesPage));

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
    
    public new event PropertyChangedEventHandler? PropertyChanged;

    private new void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
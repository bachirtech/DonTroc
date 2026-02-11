using System;
using DonTroc.Models;
using DonTroc.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels
{
    public class ConversationsViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;
        private readonly UnreadMessageService _unreadMessageService;

        public ObservableCollection<Conversation> Conversations { get; } = new();
        public ICommand LoadConversationsCommand { get; }
        public ICommand GoToChatCommand { get; }

        private Conversation? _selectedConversation;
        public Conversation? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                Debug.WriteLine($"[ConversationsViewModel] SelectedConversation setter appelé avec: {value?.Id ?? "null"}");
                if (SetProperty(ref _selectedConversation, value) && value != null)
                {
                    Debug.WriteLine($"[ConversationsViewModel] Navigation déclenchée par sélection");
                    // Naviguer automatiquement quand une conversation est sélectionnée
                    _ = ExecuteGoToChatCommand(value);
                    // Réinitialiser la sélection pour permettre de re-sélectionner
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _selectedConversation = null;
                        OnPropertyChanged(nameof(SelectedConversation));
                    });
                }
            }
        }

        private int _totalUnreadCount;
        public int TotalUnreadCount
        {
            get => _totalUnreadCount;
            set => SetProperty(ref _totalUnreadCount, value);
        }

        public ConversationsViewModel(FirebaseService firebaseService, AuthService authService, UnreadMessageService unreadMessageService)
        {
            _firebaseService = firebaseService;
            _authService = authService;
            _unreadMessageService = unreadMessageService;

            LoadConversationsCommand = new Command(async () => await ExecuteLoadConversationsCommand());
            GoToChatCommand = new Command<Conversation>(async (conversation) => await ExecuteGoToChatCommand(conversation));

            // S'abonner aux changements du compteur total de messages non lus
            _unreadMessageService.PropertyChanged += OnUnreadMessageServicePropertyChanged;
        }

        /// <summary>
        /// Gère les changements du service de messages non lus
        /// </summary>
        private void OnUnreadMessageServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UnreadMessageService.TotalUnreadCount))
            {
                TotalUnreadCount = _unreadMessageService.TotalUnreadCount;
            }
        }

        private async Task ExecuteLoadConversationsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Conversations.Clear();
                var userId = _authService.GetUserId();
                
                if (string.IsNullOrEmpty(userId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour voir vos conversations.", "OK");
                    return;
                }

                var conversations = await _firebaseService.GetUserConversationsAsync(userId);
                
                int totalUnread = 0;
                foreach (var conversation in conversations)
                {
                    // Le UnreadCount est déjà calculé par FirebaseService
                    // Ajouter au total
                    totalUnread += conversation.UnreadCount;
                    
                    Debug.WriteLine($"[ConversationsViewModel] Conversation {conversation.AnnonceTitre}: {conversation.UnreadCount} non lus");
                    Conversations.Add(conversation);
                }

                // Mettre à jour le compteur total
                TotalUnreadCount = totalUnread;
                Debug.WriteLine($"[ConversationsViewModel] Total messages non lus: {TotalUnreadCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du chargement des conversations: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de charger les conversations.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteGoToChatCommand(Conversation conversation)
        {
            Debug.WriteLine($"[ConversationsViewModel] ExecuteGoToChatCommand appelé");
            
            if (conversation == null)
            {
                Debug.WriteLine($"[ConversationsViewModel] Conversation est null!");
                return;
            }

            Debug.WriteLine($"[ConversationsViewModel] Conversation ID: {conversation.Id}, Titre: {conversation.AnnonceTitre}");

            if (string.IsNullOrEmpty(conversation.Id))
            {
                Debug.WriteLine($"[ConversationsViewModel] Conversation.Id est vide!");
                await Shell.Current.DisplayAlert("Erreur", "Cette conversation n'a pas d'identifiant valide.", "OK");
                return;
            }

            try
            {
                // Navigation vers la page de chat détaillée
                var encodedConversationId = Uri.EscapeDataString(conversation.Id);
                Debug.WriteLine($"[ConversationsViewModel] Navigation vers ChatView avec conversationId={encodedConversationId}");
                await Shell.Current.GoToAsync($"ChatView?conversationId={encodedConversationId}");
                Debug.WriteLine($"[ConversationsViewModel] Navigation réussie");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConversationsViewModel] Erreur de navigation: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", $"Impossible d'ouvrir la conversation: {ex.Message}", "OK");
            }
        }
    }
}

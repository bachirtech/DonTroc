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
                foreach (var conversation in conversations)
                {
                    // Ajouter le compteur de messages non lus à chaque conversation
                    conversation.UnreadCount = _unreadMessageService.GetUnreadCount(conversation.Id);
                    Conversations.Add(conversation);
                }

                // Mettre à jour le compteur total
                TotalUnreadCount = _unreadMessageService.TotalUnreadCount;
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
            if (conversation == null)
                return;

            // Navigation vers la page de chat détaillée
            await Shell.Current.GoToAsync($"ChatView?conversationId={conversation.Id}");
        }
    }
}

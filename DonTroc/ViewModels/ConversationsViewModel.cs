using System;
using System.Linq;
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
        public ICommand DeleteConversationCommand { get; }

        private Conversation? _selectedConversation;
        public Conversation? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                if (SetProperty(ref _selectedConversation, value) && value != null)
                {
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
            DeleteConversationCommand = new Command<Conversation>(async (conversation) => await ExecuteDeleteConversationCommand(conversation));

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
                    totalUnread += conversation.UnreadCount;
                    Conversations.Add(conversation);
                }

                TotalUnreadCount = totalUnread;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Conversations] Erreur chargement: {ex.Message}");
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

            if (string.IsNullOrEmpty(conversation.Id))
            {
                await Shell.Current.DisplayAlert("Erreur", "Cette conversation n'a pas d'identifiant valide.", "OK");
                return;
            }

            try
            {
                var encodedConversationId = Uri.EscapeDataString(conversation.Id);
                await Shell.Current.GoToAsync($"ChatView?conversationId={encodedConversationId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Conversations] Erreur navigation: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", $"Impossible d'ouvrir la conversation: {ex.Message}", "OK");
            }
        }
        private async Task ExecuteDeleteConversationCommand(Conversation conversation)
        {
            if (conversation == null)
                return;

            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Supprimer la conversation",
                    $"Voulez-vous vraiment supprimer la conversation \"{conversation.AnnonceTitre}\" ? Cette action est irréversible et supprimera tous les messages.",
                    "Supprimer",
                    "Annuler");

                if (!confirm)
                    return;

                await _firebaseService.DeleteConversationAsync(conversation.Id);
                Conversations.Remove(conversation);

                // Recalculer le total de messages non lus
                TotalUnreadCount = Conversations.Sum(c => c.UnreadCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Conversations] Erreur suppression: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de supprimer la conversation.", "OK");
            }
        }

        public override void Dispose()
        {
            _unreadMessageService.PropertyChanged -= OnUnreadMessageServicePropertyChanged;
            base.Dispose();
        }
    }
}

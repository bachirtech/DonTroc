using System;
using System.Collections.Generic;
using DonTroc.Models;
using DonTroc.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;

namespace DonTroc.ViewModels
{
    public class ChatViewModel : BaseViewModel
    {
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;
        private readonly NotificationService _notificationService;
        private readonly UnreadMessageService _unreadMessageService;
        private readonly GlobalNotificationService _globalNotificationService;
        private readonly GeolocationService _geolocationService;
        private readonly GamificationService _gamificationService;
        private readonly AudioService _audioService;
        private readonly PushNotificationService _pushNotificationService;
        
        private string _conversationId = string.Empty;
        private IDisposable? _messagesSubscription;
        private IDisposable? _typingSubscription;
        private readonly System.Timers.Timer _typingTimer;
        private System.Timers.Timer? _recordingTimer; // Timer pour l'enregistrement - nullable
        
        // Informations du destinataire pour les notifications push
        private string? _recipientUserId;
        private string? _recipientFcmToken;
        private string? _currentUserName;

        // Propriétés pour la lecture audio
        private bool _isPlayingVoice;

        public string ConversationId
        {
            get => _conversationId;
            set
            {
                _conversationId = value;
                LoadMessagesCommand.Execute(null);
                _ = Task.Run(async () => await MarkConversationAsReadAsync());
            }
        }

        public ObservableCollection<Message> Messages { get; } = new();
        
        // Commandes
        private ICommand LoadMessagesCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand SendLocationCommand { get; }
        public ICommand TakePhotoCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand OpenImageCommand { get; }
        public ICommand OpenLocationCommand { get; }
        public ICommand CopyMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand ShowMessageOptionsCommand { get; } // Nouvelle commande pour le menu contextuel

        // Nouvelles commandes pour les fonctionnalités vocales et UI
        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand PlayVoiceMessageCommand { get; }
        public ICommand ToggleMediaOptionsCommand { get; }
        public ICommand SendOrRecordCommand { get; }
        public ICommand HandleVoiceMessageTapCommand { get; }


        // Propriétés pour le message en cours
        private string _newMessageText = "";
        public string NewMessageText
        {
            get => _newMessageText;
            set 
            { 
                SetProperty(ref _newMessageText, value);
                OnUserTyping();
                (SendMessageCommand as Command)?.ChangeCanExecute();
                OnPropertyChanged(nameof(SendButtonText));
                OnPropertyChanged(nameof(SendButtonFontSize));
                OnPropertyChanged(nameof(SendButtonBackgroundColor));
            }
        }

        // Indicateurs de frappe
        private bool _isOtherUserTyping;
        public bool IsOtherUserTyping
        {
            get => _isOtherUserTyping;
            set => SetProperty(ref _isOtherUserTyping, value);
        }

        private string _typingIndicatorText = "";
        public string TypingIndicatorText
        {
            get => _typingIndicatorText;
            set => SetProperty(ref _typingIndicatorText, value);
        }

        // Propriétés pour l'envoi d'images
        private bool _isImagePickerVisible;
        public bool IsImagePickerVisible
        {
            get => _isImagePickerVisible;
            set => SetProperty(ref _isImagePickerVisible, value);
        }

        private bool _isSendingLocation;
        public bool IsSendingLocation
        {
            get => _isSendingLocation;
            set => SetProperty(ref _isSendingLocation, value);
        }

        // Propriétés pour les accusés de réception
        private int _unreadMessagesCount;
        public int UnreadMessagesCount
        {
            get => _unreadMessagesCount;
            set => SetProperty(ref _unreadMessagesCount, value);
        }

        private string _lastSeenStatus = "";
        public string LastSeenStatus
        {
            get => _lastSeenStatus;
            set => SetProperty(ref _lastSeenStatus, value);
        }

        // Propriétés pour l'interface utilisateur améliorée
        private bool _isMediaOptionsVisible;
        public bool IsMediaOptionsVisible
        {
            get => _isMediaOptionsVisible;
            set => SetProperty(ref _isMediaOptionsVisible, value);
        }

        // Propriétés pour le sélecteur d'émojis
        private bool _isEmojiPickerVisible;
        public bool IsEmojiPickerVisible
        {
            get => _isEmojiPickerVisible;
            set => SetProperty(ref _isEmojiPickerVisible, value);
        }

        // Commandes pour les émojis
        public ICommand ToggleEmojiPickerCommand { get; }
        public ICommand InsertEmojiCommand { get; }

        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            set 
            { 
                SetProperty(ref _isRecording, value);
                OnPropertyChanged(nameof(SendButtonText));
                OnPropertyChanged(nameof(SendButtonFontSize));
                OnPropertyChanged(nameof(SendButtonBackgroundColor));
                OnPropertyChanged(nameof(RecordingStatusText));
            }
        }

        // Nouvelles propriétés pour le feedback visuel d'enregistrement
        private string _recordingStatusText = "";
        public string RecordingStatusText
        {
            get => _recordingStatusText;
            set => SetProperty(ref _recordingStatusText, value);
        }

        private TimeSpan _recordingDuration;
        public TimeSpan RecordingDuration
        {
            get => _recordingDuration;
            set 
            { 
                SetProperty(ref _recordingDuration, value);
                RecordingStatusText = IsRecording ? $"🎤 Enregistrement... {value:mm\\:ss}" : "";
            }
        }

        // Propriétés pour la lecture de messages vocaux
        public bool IsPlayingVoice
        {
            get => _isPlayingVoice;
            set => SetProperty(ref _isPlayingVoice, value);
        }

        private string _currentPlayingMessageId = "";
        public string CurrentPlayingMessageId
        {
            get => _currentPlayingMessageId;
            set => SetProperty(ref _currentPlayingMessageId, value);
        }

        // Propriétés calculées pour l'interface dynamique
        public string SendButtonText => IsRecording ? "🛑" : (string.IsNullOrWhiteSpace(NewMessageText) ? "🎤" : "➤");
        public double SendButtonFontSize => IsRecording || string.IsNullOrWhiteSpace(NewMessageText) ? 20 : 16;
        public Color SendButtonBackgroundColor => IsRecording ? Colors.Red : Color.FromArgb("#D4A574");

        /// <summary>
        /// Sélectionne une image depuis la galerie
        /// </summary>
        private async Task ExecuteSelectImageCommand()
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Sélectionner une photo"
                });

                if (photo == null)
                    return;

                IsBusy = true;

                var currentUserId = _authService.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour envoyer une image.", "OK");
                    return;
                }

                // Upload de l'image vers Cloudinary
                using (var stream = await photo.OpenReadAsync())
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var imageData = memoryStream.ToArray();

                    try
                    {
                        // Upload vers Cloudinary de manière sécurisée
                        var imageUrls = await _firebaseService.UploadImagesAsync(new List<byte[]> { imageData }, currentUserId);
                        
                        if (!imageUrls.Any())
                        {
                            await Shell.Current.DisplayAlert("Erreur", "Impossible d'uploader l'image vers le serveur.", "OK");
                            return;
                        }

                        var imageUrl = imageUrls[0];

                        // Créer le message avec l'URL Cloudinary
                        var message = new Message
                        {
                            ConversationId = this.ConversationId,
                            SenderId = currentUserId,
                            ImageUrl = imageUrl,
                            Text = "📷 Image",
                            Timestamp = DateTime.UtcNow
                        };

                        await _firebaseService.SendMessageAsync(message);

                        // Envoyer une notification push au destinataire (en arrière-plan)
                        _ = Task.Run(async () => await SendPushNotificationToRecipientAsync("📷 Vous avez reçu une image"));

                        // Ajouter des points de gamification
                        try
                        {
                            await _gamificationService.AddPointsAsync(currentUserId, 5, "Image envoyée");
                        }
                        catch (Exception gamificationEx)
                        {
                            Debug.WriteLine($"Erreur gamification: {gamificationEx.Message}");
                        }
                    }
                    catch (Exception uploadEx)
                    {
                        // Message d'erreur plus spécifique selon le type d'exception
                        string errorMessage = uploadEx.Message switch
                        {
                            var msg when msg.Contains("non valide") || msg.Contains("dangereuse") => 
                                "Le format de l'image n'est pas supporté. Essayez avec une image JPEG ou PNG.",
                            var msg when msg.Contains("trop grande") || msg.Contains("MAX_FILE_SIZE") => 
                                "L'image est trop grande. La taille maximum est de 10MB.",
                            var msg when msg.Contains("Cloudinary") => 
                                "Erreur du serveur d'images. Veuillez réessayer dans quelques instants.",
                            var msg when msg.Contains("non autorisé") || msg.Contains("Unauthorized") => 
                                "Vous n'êtes pas autorisé à envoyer des images. Vérifiez votre connexion.",
                            _ => $"Erreur lors de l'envoi de l'image: {uploadEx.Message}"
                        };
                        
                        await Shell.Current.DisplayAlert("Erreur", errorMessage, "OK");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la sélection d'image: {ex.Message}");
                
                // Ne pas afficher le message d'erreur ici si on l'a déjà fait dans le catch interne
                if (!ex.Message.Contains("non valide") && !ex.Message.Contains("dangereuse") && 
                    !ex.Message.Contains("trop grande") && !ex.Message.Contains("Cloudinary") && 
                    !ex.Message.Contains("non autorisé"))
                {
                    await Shell.Current.DisplayAlert("Erreur", $"Impossible d'envoyer l'image: {ex.Message}", "OK");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Ajout d'une méthode manquante pour TakePhotoCommand
        private async Task ExecuteTakePhotoCommand()
        {
            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Prendre une photo"
                });

                if (photo == null)
                    return;

                IsBusy = true;

                var currentUserId = _authService.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour envoyer une photo.", "OK");
                    return;
                }

                // CORRECTION: Upload de l'image vers Cloudinary au lieu de l'utiliser localement
                using (var stream = await photo.OpenReadAsync())
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var imageData = memoryStream.ToArray();

                    // Upload vers Cloudinary de manière sécurisée
                    var imageUrls = await _firebaseService.UploadImagesAsync(new List<byte[]> { imageData }, currentUserId);
                    
                    if (!imageUrls.Any())
                    {
                        await Shell.Current.DisplayAlert("Erreur", "Impossible d'uploader la photo.", "OK");
                        return;
                    }

                    var imageUrl = imageUrls[0];

                    // Créer le message avec l'URL Cloudinary
                    var message = new Message
                    {
                        ConversationId = this.ConversationId,
                        SenderId = currentUserId,
                        ImageUrl = imageUrl, // ✅ CORRECTION: URL web au lieu de fichier local
                        Text = "📷 Photo prise", // Texte de fallback
                        Timestamp = DateTime.UtcNow
                    };

                    await _firebaseService.SendMessageAsync(message);

                    // Envoyer une notification push au destinataire (en arrière-plan)
                    _ = Task.Run(async () => await SendPushNotificationToRecipientAsync("📷 Vous avez reçu une photo"));

                    // Ajouter des points de gamification
                    await _gamificationService.AddPointsAsync(currentUserId, 5, "Photo envoyée");
                    
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la prise de photo: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", $"Impossible d'envoyer la photo: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                IsImagePickerVisible = false;
            }
        }

        // Constructeur avec tous les services nécessaires
        public ChatViewModel(
            FirebaseService firebaseService, 
            AuthService authService, 
            NotificationService notificationService, 
            UnreadMessageService unreadMessageService, 
            GlobalNotificationService globalNotificationService,
            GeolocationService geolocationService,
            GamificationService gamificationService,
            AudioService audioService,
            PushNotificationService pushNotificationService)
        {
            _firebaseService = firebaseService;
            _authService = authService;
            _notificationService = notificationService;
            _unreadMessageService = unreadMessageService;
            _globalNotificationService = globalNotificationService;
            _geolocationService = geolocationService;
            _gamificationService = gamificationService;
            _audioService = audioService;
            _pushNotificationService = pushNotificationService;

            // Initialisation des propriétés
            _conversationId = string.Empty;

            // Initialisation des commandes avec gestion d'erreur sécurisée
            LoadMessagesCommand = new Command(ExecuteLoadMessagesCommandSafe);
            SendMessageCommand = new Command(ExecuteSendMessageCommandSafe, () => !string.IsNullOrWhiteSpace(NewMessageText));
            SendImageCommand = new Command(ExecuteSendImageCommandSafe);
            SendLocationCommand = new Command(ExecuteSendLocationCommandSafe);
            TakePhotoCommand = new Command(ExecuteTakePhotoCommandSafe);
            SelectImageCommand = new Command(ExecuteSelectImageCommandSafe);
            OpenImageCommand = new Command<Message>(ExecuteOpenImageCommandSafe);
            OpenLocationCommand = new Command<Message>(ExecuteOpenLocationCommandSafe);
            CopyMessageCommand = new Command<Message>(ExecuteCopyMessageCommandSafe);
            DeleteMessageCommand = new Command<Message>(ExecuteDeleteMessageCommandSafe);
            ShowMessageOptionsCommand = new Command<Message>(ExecuteShowMessageOptionsCommandSafe); // Nouvelle commande

            // Nouvelles commandes
            StartRecordingCommand = new Command(ExecuteStartRecordingCommandSafe);
            StopRecordingCommand = new Command(ExecuteStopRecordingCommandSafe);
            PlayVoiceMessageCommand = new Command<Message>(ExecutePlayVoiceMessageCommandSafe);
            ToggleMediaOptionsCommand = new Command(() =>
            {
                IsMediaOptionsVisible = !IsMediaOptionsVisible;
                if (IsMediaOptionsVisible)
                    IsEmojiPickerVisible = false;
            });
            SendOrRecordCommand = new Command(ExecuteSendOrRecordCommandSafe);
            HandleVoiceMessageTapCommand = new Command<Message>(ExecuteHandleVoiceMessageTapCommand);

            // Commandes pour les émojis
            ToggleEmojiPickerCommand = new Command(ExecuteToggleEmojiPicker);
            InsertEmojiCommand = new Command<string>(ExecuteInsertEmoji);


            // Timer pour les indicateurs de frappe
            _typingTimer = new System.Timers.Timer(2000);
            _typingTimer.Elapsed += (_, _) => Task.Run(async () => await StopTyping());
            _typingTimer.AutoReset = false;
        }

        #region Safe Command Wrappers

        private async void ExecuteLoadMessagesCommandSafe()
        {
            try
            {
                await ExecuteLoadMessagesCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du chargement des messages: {ex.Message}");
            }
        }

        private async void ExecuteSendMessageCommandSafe()
        {
            try
            {
                await ExecuteSendMessageCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'envoi du message: {ex.Message}");
            }
        }

        private async void ExecuteSendImageCommandSafe()
        {
            try
            {
                await ExecuteSendImageCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'envoi d'image: {ex.Message}");
            }
        }

        private async void ExecuteSendLocationCommandSafe()
        {
            try
            {
                await ExecuteSendLocationCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du partage de localisation: {ex.Message}");
            }
        }

        private async void ExecuteTakePhotoCommandSafe()
        {
            try
            {
                await ExecuteTakePhotoCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la prise de photo: {ex.Message}");
            }
        }

        private async void ExecuteSelectImageCommandSafe() 
        {
            try
            {
                await ExecuteSelectImageCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la sélection d'image: {ex.Message}");
            }
        }

        private async void ExecuteOpenImageCommandSafe(Message message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.ImageUrl))
                    return;

                // Navigation vers la nouvelle vue de l'image
                await Shell.Current.GoToAsync($"{nameof(ImageViewerView)}?imageUrl={Uri.EscapeDataString(message.ImageUrl)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'ouverture d'image: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'afficher l'image.", "OK");
            }
        }

        private async void ExecuteOpenLocationCommandSafe(Message message)
        {
            try
            {
                await ExecuteOpenLocationCommand(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'ouverture de localisation: {ex.Message}");
            }
        }

        private async void ExecuteCopyMessageCommandSafe(Message message)
        {
            try
            {
                await ExecuteCopyMessageCommand(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la copie: {ex.Message}");
            }
        }

        private async void ExecuteDeleteMessageCommandSafe(Message message)
        {
            try
            {
                await ExecuteDeleteMessageCommand(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la suppression: {ex.Message}");
            }
        }

        private async void ExecuteShowMessageOptionsCommandSafe(Message message)
        {
            try
            {
                var currentUserId = _authService.GetUserId();
                var isMyMessage = message.SenderId == currentUserId;

                string? action;

                if (isMyMessage)
                {
                    // Options pour les messages envoyés par l'utilisateur
                    if (!string.IsNullOrEmpty(message.ImageUrl))
                    {
                        action = await Shell.Current.DisplayActionSheet("Options de l'image", "Annuler", "Supprimer", "Renvoyer");
                        if (action == "Supprimer")
                        {
                            await ExecuteDeleteMessageCommand(message);
                        }
                        else if (action == "Renvoyer")
                        {
                            await ExecuteResendImageCommand(message);
                        }
                    }
                    else // Message texte
                    {
                        action = await Shell.Current.DisplayActionSheet("Options du message", "Annuler", "Supprimer", "Copier le texte");
                        if (action == "Supprimer")
                        {
                            await ExecuteDeleteMessageCommand(message);
                        }
                        else if (action == "Copier le texte")
                        {
                            await ExecuteCopyMessageCommand(message);
                        }
                    }
                }
                else
                {
                    // Options pour les messages reçus
                    if (!string.IsNullOrEmpty(message.Text))
                    {
                        action = await Shell.Current.DisplayActionSheet("Options", "Annuler", null, "Copier le texte");
                        if (action == "Copier le texte")
                        {
                            await ExecuteCopyMessageCommand(message);
                        }
                    }
                    // Aucune action pour les images reçues pour le moment
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'affichage des options du message: {ex.Message}");
            }
        }

        /// <summary>
        /// EXPERT: Renvoie une image déjà envoyée.
        /// </summary>
        private async Task ExecuteResendImageCommand(Message message)
        {
            if (string.IsNullOrEmpty(message.ImageUrl)) return;

            try
            {
                IsBusy = true;
                var currentUserId = _authService.GetUserId();

                var newMessage = new Message
                {
                    ConversationId = this.ConversationId,
                    SenderId = currentUserId,
                    ImageUrl = message.ImageUrl, // Réutiliser la même URL d'image
                    Text = "📷 Image",
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseService.SendMessageAsync(newMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du renvoi de l'image: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de renvoyer l'image.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }


        /// <summary>
        /// Affiche ou masque le sélecteur d'émojis
        /// </summary>
        private void ExecuteToggleEmojiPicker()
        {
            IsEmojiPickerVisible = !IsEmojiPickerVisible;
            
            // Fermer les options multimédia si elles sont ouvertes
            if (IsEmojiPickerVisible)
            {
                IsMediaOptionsVisible = false;
            }
            
        }

        /// <summary>
        /// Insère un émoji dans le texte du message
        /// </summary>
        private void ExecuteInsertEmoji(string emoji)
        {
            if (string.IsNullOrEmpty(emoji))
                return;

            // Ajouter l'émoji à la position courante (à la fin du texte)
            NewMessageText = (NewMessageText ?? "") + emoji;
            
        }


        private async void ExecuteStartRecordingCommandSafe()
        {
            try
            {
                await ExecuteStartRecordingCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du démarrage de l'enregistrement: {ex.Message}");
            }
        }

        private async void ExecuteStopRecordingCommandSafe()
        {
            try
            {
                await ExecuteStopRecordingCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'arrêt de l'enregistrement: {ex.Message}");
            }
        }

        private async void ExecutePlayVoiceMessageCommandSafe(Message message)
        {
            try
            {
                await ExecutePlayVoiceMessageCommand(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la lecture du message vocal: {ex.Message}");
            }
        }

        private async void ExecuteSendOrRecordCommandSafe()
        {
            try
            {
                await ExecuteSendOrRecordCommand();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur dans SendOrRecordCommand: {ex.Message}");
            }
        }

        private async void ExecuteHandleVoiceMessageTapCommand(Message message)
        {
            if (string.IsNullOrEmpty(message.VoiceMessageUrl))
                return;

            try
            {
                string action = await Shell.Current.DisplayActionSheet(
                    "Options du message vocal", 
                    "Annuler", 
                    null, 
                    "Réécouter", "Supprimer");

                switch (action)
                {
                    case "Réécouter":
                        await ExecutePlayVoiceMessageCommand(message);
                        break;
                    case "Supprimer":
                        await ExecuteDeleteMessageCommand(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la gestion de l'appui sur le message vocal : {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Une erreur est survenue.", "OK");
            }
        }

        #endregion

        private async Task ExecuteLoadMessagesCommand()
        {
            var currentUserId = _authService.GetUserId();
            if (IsBusy || string.IsNullOrEmpty(ConversationId) || string.IsNullOrEmpty(currentUserId))
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour accéder au chat.", "OK");
                }
                return;
            }

            IsBusy = true;

            try
            {
                // Marquer cette conversation comme active pour éviter les notifications
                _globalNotificationService.SetActiveConversation(ConversationId);
                
                // Désabonner les anciens abonnements
                _messagesSubscription?.Dispose();
                _typingSubscription?.Dispose();

                // Charger les informations de la conversation pour obtenir le destinataire
                await LoadRecipientInfoAsync(currentUserId);

                // Charger les messages existants une fois
                Messages.Clear();
                var existingMessages = await _firebaseService.GetMessagesAsync(ConversationId);

                foreach (var message in existingMessages)
                {
                    message.IsSentByUser = message.SenderId == currentUserId;
                    Messages.Add(message);
                }

                // Marquer tous les messages comme lus maintenant que l'utilisateur les voit
                _ = Task.Run(async () => await MarkConversationAsReadAsync());

                // S'abonner aux nouveaux messages en temps réel avec gestion d'erreur
                _messagesSubscription = _firebaseService.SubscribeToMessages(
                    ConversationId,
                    OnNewMessageReceived,
                    ex =>
                    {
                        Debug.WriteLine($"Erreur streaming messages: {ex.Message}");
                        MainThread.BeginInvokeOnMainThread(async () => 
                        {
                            await Shell.Current.DisplayAlert("Erreur", "Impossible de charger les messages en temps réel (permissions ou connexion).", "OK");
                        });
                        _messagesSubscription?.Dispose();
                    });

                // S'abonner aux indicateurs d'écriture
                _typingSubscription = _firebaseService.SubscribeToTypingIndicators(ConversationId, currentUserId, OnTypingChanged);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du chargement des messages: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de charger les messages.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Charge les informations du destinataire pour les notifications push
        /// </summary>
        private async Task LoadRecipientInfoAsync(string currentUserId)
        {
            try
            {
                // Charger la conversation pour obtenir les participants
                var conversations = await _firebaseService.GetUserConversationsAsync(currentUserId);
                var conversation = conversations.FirstOrDefault(c => c.Id == ConversationId);
                
                if (conversation != null)
                {
                    // Déterminer qui est le destinataire
                    _recipientUserId = conversation.SellerId == currentUserId 
                        ? conversation.BuyerId 
                        : conversation.SellerId;
                    
                    // Charger le profil du destinataire pour obtenir le token FCM
                    if (!string.IsNullOrEmpty(_recipientUserId))
                    {
                        var recipientProfile = await _firebaseService.GetUserProfileAsync(_recipientUserId);
                        _recipientFcmToken = recipientProfile?.FcmToken;
                    }
                    
                    // Charger le nom de l'utilisateur actuel pour les notifications
                    var currentUserProfile = await _firebaseService.GetUserProfileAsync(currentUserId);
                    _currentUserName = currentUserProfile?.Name ?? "Un utilisateur";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatViewModel] Erreur lors du chargement des infos du destinataire: {ex.Message}");
            }
        }

        /// <summary>
        /// Envoie une notification push au destinataire
        /// </summary>
        private async Task SendPushNotificationToRecipientAsync(string messageText)
        {
            try
            {
                // Vérifier que nous avons les informations nécessaires
                if (string.IsNullOrEmpty(_recipientFcmToken))
                {
                    return;
                }

                if (string.IsNullOrEmpty(_currentUserName))
                {
                    _currentUserName = "Un utilisateur";
                }

                // Envoyer la notification push
                var success = await _pushNotificationService.SendMessageNotificationAsync(
                    _recipientFcmToken,
                    _currentUserName,
                    messageText,
                    ConversationId
                );

                if (success)
                {
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatViewModel] Erreur lors de l'envoi de la notification push: {ex.Message}");
            }
        }

        /// <summary>
        /// Appelée quand un nouveau message est reçu en temps réel
        /// </summary>
        private void OnNewMessageReceived(Message message)
        {
            var currentUserId = _authService.GetUserId();
            message.IsSentByUser = message.SenderId == currentUserId;

            // Ignorer les messages supprimés localement ("Supprimer pour moi")
            var deletedKey = $"deleted_msg_{ConversationId}_{message.Id}";
            if (Preferences.Get(deletedKey, false)) return;

            // Vérifier si ce message existe déjà (mise à jour de statut)
            var existingMessage = Messages.FirstOrDefault(m => m.Id == message.Id);
            if (existingMessage != null)
            {
                // Mettre à jour le statut du message existant
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    existingMessage.Status = message.Status;
                    existingMessage.IsRead = message.IsRead;
                    existingMessage.IsDelivered = message.IsDelivered;
                    existingMessage.ReadAt = message.ReadAt;
                    existingMessage.DeliveredAt = message.DeliveredAt;
                    
                    // Notifier le changement pour rafraîchir l'affichage
                    var index = Messages.IndexOf(existingMessage);
                    if (index >= 0)
                    {
                        Messages.RemoveAt(index);
                        Messages.Insert(index, message);
                    }
                });
                return;
            }

            // Ajouter le message à la liste sur le thread UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Messages.Add(message);

                // Si le message n'est pas de l'utilisateur actuel
                if (!message.IsSentByUser)
                {
                    // Marquer le message comme livré (en arrière-plan)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _firebaseService.MarkMessageAsDeliveredAsync(ConversationId, message.Id);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Erreur lors du marquage comme livré: {ex.Message}");
                        }
                    });

                    // Afficher une notification
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Récupérer le nom de l'expéditeur pour la notification
                            var senderProfile = await _firebaseService.GetUserProfileAsync(message.SenderId);
                            var senderName = senderProfile?.Name ?? "Utilisateur inconnu";

                            // Afficher la notification uniquement si l'app n'est pas au premier plan
                            // ou si l'utilisateur n'est pas sur cette conversation
                            if (!IsAppInForeground() || !IsCurrentConversationActive())
                            {
                                await _notificationService.ShowMessageNotificationAsync(
                                    senderName,
                                    message.Text,
                                    ConversationId
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Erreur lors de l'affichage de la notification: {ex.Message}");
                            // En cas d'erreur, afficher quand même une notification basique
                            try
                            {
                                await _notificationService.ShowMessageNotificationAsync(
                                    "Nouveau message",
                                    message.Text,
                                    ConversationId
                                );
                            }
                            catch
                            {
                                // Ignorer silencieusement si même la notification basique échoue
                            }
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Vérifie si l'application est au premier plan
        /// </summary>
        private bool IsAppInForeground()
        {
            try
            {
                // Vérifier l'état de l'application
                return Application.Current?.MainPage != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie si la conversation actuelle est active (l'utilisateur est sur cette page de chat)
        /// </summary>
        private bool IsCurrentConversationActive()
        {
            try
            {
                // Cette méthode peut être améliorée pour vérifier si l'utilisateur est vraiment sur cette page
                // Pour l'instant, on considère que si le ViewModel est actif, la conversation est active
                return !string.IsNullOrEmpty(ConversationId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Appelée quand l'état d'écriture d'un autre utilisateur change
        /// </summary>
        private void OnTypingChanged(bool isTyping)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsOtherUserTyping = isTyping;
                TypingIndicatorText = isTyping ? "L'autre utilisateur est en train d'écrire..." : string.Empty;
            });
        }

        /// <summary>
        /// Appelée quand l'utilisateur actuel tape un message
        /// </summary>
        private async void OnUserTyping()
        {
            if (string.IsNullOrEmpty(ConversationId))
                return;

            try
            {
                var currentUserId = _authService.GetUserId();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    // Indiquer qu'on est en train d'écrire
                    await _firebaseService.SetTypingIndicatorAsync(ConversationId, currentUserId, true);
                    
                    // Redémarrer le timer
                    _typingTimer.Stop();
                    _typingTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'indication d'écriture: {ex.Message}");
            }
        }

        /// <summary>
        /// Arrête l'indicateur d'écriture
        /// </summary>
        private async Task StopTyping()
        {
            if (string.IsNullOrEmpty(ConversationId))
                return;

            try
            {
                var currentUserId = _authService.GetUserId();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    await _firebaseService.SetTypingIndicatorAsync(ConversationId, currentUserId, false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'arrêt de l'indication d'écriture: {ex.Message}");
            }
        }

        private async Task ExecuteSendMessageCommand()
        {
            if (string.IsNullOrWhiteSpace(NewMessageText))
                return;

            try
            {
                var currentUserId = _authService.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour envoyer un message.", "OK");
                    return;
                }
                
                // Vérification du ConversationId
                if (string.IsNullOrEmpty(ConversationId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "La conversation n'est pas correctement initialisée. Veuillez réessayer.", "OK");
                    return;
                }

                // Arrêter l'indicateur d'écriture avant d'envoyer
                await StopTyping();

                // Sauvegarder le texte du message avant de le vider
                var messageText = this.NewMessageText;

                var message = new Message
                {
                    ConversationId = this.ConversationId,
                    SenderId = currentUserId,
                    Text = messageText,
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseService.SendMessageAsync(message);

                NewMessageText = string.Empty; // Vide le champ de saisie

                // Envoyer une notification push au destinataire (en arrière-plan)
                _ = Task.Run(async () => await SendPushNotificationToRecipientAsync(messageText));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'envoi du message: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'envoyer le message.", "OK");
            }
        }

        /// <summary>
        /// Envoie une image sélectionnée
        /// </summary>
        private async Task ExecuteSendImageCommand()
        {
            IsImagePickerVisible = false; // Fermer le picker
            await ExecuteSelectImageCommand();
        }

        /// <summary>
        /// Envoie la localisation actuelle de l'utilisateur
        /// </summary>
        private async Task ExecuteSendLocationCommand()
        {
            if (IsSendingLocation)
                return;

            try
            {
                IsSendingLocation = true;

                var currentUserId = _authService.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour partager votre localisation.", "OK");
                    return;
                }

                // Demander la permission de localisation
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permission requise", "L'accès à la localisation est nécessaire pour partager votre position.", "OK");
                    return;
                }

                // Obtenir la localisation actuelle
                var location = await _geolocationService.GetCurrentLocationAsync();
                if (location is null)
                {
                    await Shell.Current.DisplayAlert("Erreur", "Impossible d'obtenir votre localisation actuelle.", "OK");
                    return;
                }

                // Créer le message avec la localisation
                var message = new Message
                {
                    ConversationId = this.ConversationId,
                    SenderId = currentUserId,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Text = "📍 Localisation partagée",
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseService.SendMessageAsync(message);

                // Envoyer une notification push au destinataire (en arrière-plan)
                _ = Task.Run(async () => await SendPushNotificationToRecipientAsync("📍 Vous avez reçu une localisation"));

                // Ajouter des points de gamification
                await _gamificationService.AddPointsAsync(currentUserId, 3, "Localisation partagée");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du partage de localisation: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de partager votre localisation.", "OK");
            }
            finally
            {
                IsSendingLocation = false;
                IsMediaOptionsVisible = false; // Fermer le menu des options média
            }
        }

        /// <summary>
        /// Ouvre une image en plein écran
        /// </summary>
        private async Task ExecuteOpenImageCommand(Message message)
        {
            if (string.IsNullOrEmpty(message.ImageUrl))
                return;

            try
            {
                // Vérifier si l'URL est valide
                if (!Uri.IsWellFormedUriString(message.ImageUrl, UriKind.RelativeOrAbsolute))
                {
                    await Shell.Current.DisplayAlert("Erreur", "L'image ne peut pas être ouverte (URL invalide).", "OK");
                    return;
                }

                // Naviguer vers ImageViewerView avec l'URL directe
                await Shell.Current.GoToAsync($"ImageViewerView", new Dictionary<string, object>
                {
                    ["imageUrls"] = message.ImageUrl
                });
            }
            catch (Exception)
            {
                // Essayer une approche alternative en cas d'erreur
                try
                {
                    await Shell.Current.GoToAsync("ImageViewerView", new Dictionary<string, object>
                    {
                        ["ImageUrl"] = message.ImageUrl
                    });
                }
                catch (Exception)
                {
                    await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir l'image. Veuillez réessayer.", "OK");
                }
            }
        }

        /// <summary>
        /// Ouvre la localisation dans l'application de cartes
        /// </summary>
        private async Task ExecuteOpenLocationCommand(Message message)
        {
            if (message.Latitude == null || message.Longitude == null)
                return;

            try
            {
                var location = new Location(message.Latitude.Value, message.Longitude.Value);
                var options = new MapLaunchOptions { Name = "Localisation partagée" };
                
                await Map.Default.OpenAsync(location, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'ouverture de la carte: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir la carte.", "OK");
            }
        }

        /// <summary>
        /// Copie le texte d'un message dans le presse-papiers
        /// </summary>
        private async Task ExecuteCopyMessageCommand(Message message)
        {
            if (string.IsNullOrEmpty(message.Text))
                return;

            try
            {
                await Clipboard.Default.SetTextAsync(message.Text);
                // Optionnel : afficher un toast ou feedback visuel
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la copie: {ex.Message}");
            }
        }

        /// <summary>
        /// Supprime un message
        /// </summary>
        private async Task ExecuteDeleteMessageCommand(Message message)
        {
            try
            {
                var result = await Shell.Current.DisplayAlert(
                    "Supprimer le message", 
                    "Êtes-vous sûr de vouloir supprimer ce message ?", 
                    "Supprimer", 
                    "Annuler");

                if (!result)
                    return;

                await _firebaseService.DeleteMessageAsync(message.Id, ConversationId);
                
                // Retirer le message de la collection locale
                var messageToRemove = Messages.FirstOrDefault(m => m.Id == message.Id);
                if (messageToRemove != null)
                {
                    Messages.Remove(messageToRemove);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la suppression: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de supprimer le message.", "OK");
            }
        }

        /// <summary>
        /// Affiche le menu contextuel pour un message texte
        /// </summary>
        private async Task ExecuteShowMessageOptionsCommand(Message message)
        {
            if (string.IsNullOrEmpty(message.Text))
                return;

            try
            {
                // Seuls les messages envoyés par l'utilisateur peuvent être supprimés
                var currentUserId = _authService.GetUserId();
                var canDelete = message.SenderId == currentUserId;

                var actions = new List<string> { "Copier le texte" };
                if (canDelete)
                {
                    actions.Add("Supprimer");
                }

                string action = await Shell.Current.DisplayActionSheet(
                    "Options du message", 
                    "Annuler", 
                    null, 
                    actions.ToArray());

                switch (action)
                {
                    case "Copier le texte":
                        await ExecuteCopyMessageCommand(message);
                        // Optionnel : afficher un feedback visuel
                        await Shell.Current.DisplayAlert("Copié", "Le message a été copié dans le presse-papiers.", "OK");
                        break;
                    case "Supprimer":
                        await ExecuteDeleteMessageCommand(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'affichage des options du message : {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Une erreur est survenue.", "OK");
            }
        }

        /// <summary>
        /// Démarre l'enregistrement vocal
        /// </summary>
        private async Task ExecuteStartRecordingCommand()
        {
            if (IsRecording)
                return;

            try
            {
                var permissionResult = await Permissions.RequestAsync<Permissions.Microphone>();
                if (permissionResult != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permission requise", "L'accès au microphone est nécessaire pour enregistrer des messages vocaux.", "OK");
                    return;
                }

                IsRecording = true;
                RecordingDuration = TimeSpan.Zero;

                // Démarrer l'enregistrement
                await _audioService.StartRecordingAsync();

                // Démarrer le timer pour afficher la durée
                _recordingTimer = new System.Timers.Timer(1000);
                _recordingTimer.Elapsed += (_, _) =>
                {
                    RecordingDuration = RecordingDuration.Add(TimeSpan.FromSeconds(1));
                };
                _recordingTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du démarrage de l'enregistrement: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de démarrer l'enregistrement.", "OK");
                IsRecording = false;
            }
        }

        /// <summary>
        /// Arrête l'enregistrement vocal et envoie le message
        /// </summary>
        private async Task ExecuteStopRecordingCommand()
        {
            if (!IsRecording)
                return;

            try
            {
                IsRecording = false;
                _recordingTimer?.Stop();
                _recordingTimer?.Dispose();
                _recordingTimer = null;

                var audioFilePath = await _audioService.StopRecordingAsync();
                
                if (string.IsNullOrEmpty(audioFilePath))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Enregistrement échoué.", "OK");
                    return;
                }

                // Vérifier la durée minimale - CORRECTION: utilisation de TotalSeconds
                if (RecordingDuration.TotalSeconds < 1)
                {
                    await Shell.Current.DisplayAlert("Erreur", "L'enregistrement est trop court.", "OK");
                    return;
                }

                var currentUserId = _authService.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour envoyer un message vocal.", "OK");
                    return;
                }

                var message = new Message
                {
                    ConversationId = this.ConversationId,
                    SenderId = currentUserId,
                    VoiceMessageUrl = audioFilePath,
                    VoiceMessageDuration = (int)RecordingDuration.TotalSeconds, // CORRECTION: utilisation de TotalSeconds
                    Text = $"🎤 Message vocal ({RecordingDuration:mm\\:ss})",
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseService.SendMessageAsync(message);

                // Envoyer une notification push au destinataire (en arrière-plan)
                _ = Task.Run(async () => await SendPushNotificationToRecipientAsync("🎤 Vous avez reçu un message vocal"));

                // Ajouter des points de gamification
                await _gamificationService.AddPointsAsync(currentUserId, 8, "Message vocal envoyé");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'arrêt de l'enregistrement: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'envoyer le message vocal.", "OK");
            }
            finally
            {
                RecordingDuration = TimeSpan.Zero;
                RecordingStatusText = "";
            }
        }

        /// <summary>
        /// Lit ou arrête la lecture d'un message vocal
        /// </summary>
        private async Task ExecutePlayVoiceMessageCommand(Message message)
        {
            if (string.IsNullOrEmpty(message.VoiceMessageUrl))
                return;

            try
            {
                if (IsPlayingVoice && CurrentPlayingMessageId == message.Id)
                {
                    // Arrêter la lecture
                    await _audioService.StopPlaybackAsync();
                    IsPlayingVoice = false;
                    CurrentPlayingMessageId = "";
                }
                else
                {
                    // Arrêter toute lecture en cours
                    if (IsPlayingVoice)
                    {
                        await _audioService.StopPlaybackAsync();
                    }

                    // Démarrer la nouvelle lecture
                    IsPlayingVoice = true;
                    CurrentPlayingMessageId = message.Id;

                    // CORRECTION: Appel correct à PlayAsync au lieu de PlayAudioAsync
                    await _audioService.PlayAsync(message.VoiceMessageUrl, () =>
                    {
                        // Callback quand la lecture se termine
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IsPlayingVoice = false;
                            CurrentPlayingMessageId = "";
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la lecture du message vocal: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible de lire le message vocal.", "OK");
                IsPlayingVoice = false;
                CurrentPlayingMessageId = "";
            }
        }

        /// <summary>
        /// Envoie un message ou démarre/arrête l'enregistrement selon le contexte
        /// </summary>
        private async Task ExecuteSendOrRecordCommand()
        {
            if (IsRecording)
            {
                // Arrêter l'enregistrement
                await ExecuteStopRecordingCommand();
            }
            else if (!string.IsNullOrWhiteSpace(NewMessageText))
            {
                // Envoyer le message texte
                await ExecuteSendMessageCommand();
            }
            else
            {
                // Démarrer l'enregistrement
                await ExecuteStartRecordingCommand();
            }
        }

        /// <summary>
        /// Récupère le nom de l'expéditeur pour les notifications
        /// </summary>
        private async Task<string> GetSenderNameAsync(string senderId)
        {
            try
            {
                var userProfile = await _firebaseService.GetUserProfileAsync(senderId);
                return userProfile?.Name ?? "Utilisateur inconnu";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la récupération du nom de l'utilisateur: {ex.Message}");
                return "Utilisateur inconnu";
            }
        }

        /// <summary>
        /// NOUVEAU: Méthode publique pour obtenir l'ID utilisateur actuel (pour le code-behind)
        /// </summary>
        public string? GetCurrentUserId()
        {
            return _authService.GetUserId();
        }

        /// <summary>
        /// Marque la conversation actuelle comme lue
        /// </summary>
        private async Task MarkConversationAsReadAsync()
        {
            if (!string.IsNullOrEmpty(ConversationId))
            {
                try
                {
                    var currentUserId = _authService.GetUserId();
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        // Marquer les messages comme lus dans Firebase (avec statut ✓✓ bleu)
                        await _firebaseService.MarkMessagesAsReadAsync(ConversationId, currentUserId);
                    }
                    
                    // Mettre à jour le compteur local
                    await _unreadMessageService.MarkConversationAsReadAsync(ConversationId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur lors du marquage de la conversation comme lue: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Nettoyer les abonnements quand la page est fermée
        /// </summary>
        public override void Dispose()
        {
            // Réinitialiser la conversation active pour réactiver les notifications
            _globalNotificationService.SetActiveConversation(null);
            
            _messagesSubscription?.Dispose();
            _typingSubscription?.Dispose();
            _typingTimer.Dispose();
            _recordingTimer?.Stop();
            _recordingTimer?.Dispose();
            base.Dispose();
        }
    }
}

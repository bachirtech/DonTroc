using System;
using DonTroc.ViewModels;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls; // Assurez-vous que ce using est présent

namespace DonTroc.Views;

public partial class ChatView : ContentPage
{
    private readonly ChatViewModel _viewModel;
    private readonly AuthService _authService; // Ajout du service d'authentification
    private Message? _tappedMessage;
    private readonly System.Timers.Timer _longPressTimer;
    private bool _isLongPress;

    public ChatView(ChatViewModel viewModel, AuthService authService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService; // Injection de dépendance
        BindingContext = viewModel;

        // Timer pour détecter l'appui long
        _longPressTimer = new System.Timers.Timer(500); // 500ms pour un appui long
        _longPressTimer.Elapsed += OnLongPressTimerElapsed;
        _longPressTimer.AutoReset = false;
    }

    // Handlers pour ImageButton (tap et appui long)
    private void OnImagePressed(object? sender, EventArgs e)
    {
        try
        {
            if (sender is VisualElement ve && ve.BindingContext is Message message)
            {
                _tappedMessage = message;
                _isLongPress = false;
                _longPressTimer.Stop();
                _longPressTimer.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatView] OnImagePressed erreur: {ex.Message}");
        }
    }

    private void OnImageReleased(object? sender, EventArgs e)
    {
        try
        {
            _longPressTimer.Stop();

            if (!_isLongPress && _tappedMessage != null)
            {
                // Tap court -> ouvrir l'image en grand
                _viewModel.OpenImageCommand.Execute(_tappedMessage);
            }

            _tappedMessage = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatView] OnImageReleased erreur: {ex.Message}");
        }
    }

    private void OnImageClicked(object? sender, EventArgs e)
    {
        // Clicked peut se produire après Released ; on s'appuie sur la logique Pressed/Released
        try
        {
            if (sender is VisualElement ve && ve.BindingContext is Message message)
            {
                // Sécurité : si un long press a déjà été détecté, ignorer le click
                if (_isLongPress)
                    return;

                _viewModel.OpenImageCommand.Execute(message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatView] OnImageClicked erreur: {ex.Message}");
        }
    }

    /// <summary>
    /// Gère le début d'un contact sur un message (appui).
    /// </summary>
    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        if (sender is BindableObject bindable && bindable.BindingContext is Message message)
        {
            _tappedMessage = message;
            _isLongPress = false;
            _longPressTimer.Start(); // Démarrer le timer pour l'appui long
        }
    }

    /// <summary>
    /// Gère la fin d'un contact sur un message (relâchement).
    /// </summary>
    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
        _longPressTimer.Stop(); // Arrêter le timer

        if (!_isLongPress && _tappedMessage != null)
        {
            // Si ce n'est pas un appui long, c'est un appui court (tap)
            if (!string.IsNullOrEmpty(_tappedMessage.ImageUrl))
            {
                _viewModel.OpenImageCommand.Execute(_tappedMessage);
            }
            // Pour le texte, on pourrait ajouter une action future ici si nécessaire
        }
        _tappedMessage = null;
    }

    /// <summary>
    /// Se déclenche lorsque le timer d'appui long est écoulé.
    /// </summary>
    private void OnLongPressTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _longPressTimer.Stop();
        _isLongPress = true;

        if (_tappedMessage != null)
        {
            // Exécuter sur le thread UI car le timer s'exécute sur un thread de fond
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _viewModel.ShowMessageOptionsCommand.Execute(_tappedMessage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatView] Erreur ShowMessageOptionsCommand: {ex.Message}");
                }
            });
        }
    }

    // Appelée quand la page disparaît - pour nettoyer les abonnements
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Nettoyer les abonnements Firebase pour éviter les fuites mémoire
        try { (_viewModel as IDisposable)?.Dispose(); } catch { }
        _longPressTimer.Dispose(); // Nettoyer le timer
    }

    /// <summary>
    /// Gère le tap sur la CollectionView pour fermer le clavier ou les options.
    /// </summary>
    private void OnCollectionViewTapped(object sender, TappedEventArgs e)
    {
        MessageEditor.Unfocus();
        if (_viewModel.IsMediaOptionsVisible)
        {
            _viewModel.IsMediaOptionsVisible = false;
        }
    }

    /// <summary>
    /// Gère le balayage pour fermer le clavier.
    /// </summary>
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Running && e.TotalY > 20)
        {
            MessageEditor.Unfocus();
        }
    }
}

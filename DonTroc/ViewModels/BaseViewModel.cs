using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// Classe de base optimisée pour les ViewModels avec gestion des performances
/// </summary>
public class BaseViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _isBusy;
    private readonly object _lockObject = new();
    private readonly Dictionary<string, object?> _propertyCache = new();
    private bool _isDisposed;
    
    // Services injectés (optionnels)
    protected ILogger? Logger { get; private set; }
    protected CancellationTokenSource? CancellationTokenSource { get; private set; }

    public BaseViewModel(ILogger? logger = null)
    {
        Logger = logger;
        CancellationTokenSource = new CancellationTokenSource();
    }

    #region Propriétés de base optimisées

    /// <summary>
    /// Propriété pour indiquer si une opération est en cours
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    #endregion

    #region PropertyChanged optimisé

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Méthode optimisée pour définir la valeur d'une propriété avec cache
    /// </summary>
    protected bool SetProperty<T>(ref T backingStore, T value,
        [CallerMemberName] string propertyName = "",
        Action? onChanged = null)
    {
        // Vérification avec cache pour éviter les comparaisons coûteuses
        lock (_lockObject)
        {
            if (_propertyCache.TryGetValue(propertyName, out var cachedValue))
            {
                if (EqualityComparer<T>.Default.Equals((T?)cachedValue, value))
                    return false;
            }

            // Si la valeur est la même que la valeur actuelle, ne rien faire
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            // Met à jour la valeur et le cache
            backingStore = value;
            _propertyCache[propertyName] = value;
        }

        // Exécute l'action de changement si fournie
        onChanged?.Invoke();
        
        // Déclenche l'événement de changement de propriété sur le thread UI
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Méthode optimisée pour déclencher PropertyChanged
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (_isDisposed) return;

        try
        {
            // Assure que l'événement est déclenché sur le thread UI
            if (MainThread.IsMainThread)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_isDisposed)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, $"Erreur lors de la notification de changement de propriété: {propertyName}");
        }
    }

    #endregion

    #region Gestion des tâches optimisée

    /// <summary>
    /// Exécute une tâche avec gestion d'erreur et indicateur de chargement
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, 
        bool showBusy = true, 
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        try
        {
            if (showBusy) IsBusy = true;

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, 
                CancellationTokenSource?.Token ?? CancellationToken.None);

            await operation();
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation($"Opération annulée: {operationName ?? "unknown"}");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, $"Erreur dans l'opération: {operationName ?? "unknown"}");
            await HandleErrorAsync(ex, operationName);
        }
        finally
        {
            if (showBusy) IsBusy = false;
        }
    }

    /// <summary>
    /// Exécute une tâche avec valeur de retour
    /// </summary>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, 
        bool showBusy = true, 
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return default;

        try
        {
            if (showBusy) IsBusy = true;

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, 
                CancellationTokenSource?.Token ?? CancellationToken.None);

            return await operation();
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation($"Opération annulée: {operationName ?? "unknown"}");
            return default;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, $"Erreur dans l'opération: {operationName ?? "unknown"}");
            await HandleErrorAsync(ex, operationName);
            return default;
        }
        finally
        {
            if (showBusy) IsBusy = false;
        }
    }

    #endregion

    #region Gestion d'erreur

    /// <summary>
    /// Méthode virtuelle pour gérer les erreurs (peut être surchargée)
    /// </summary>
    protected virtual async Task HandleErrorAsync(Exception exception, string? operationName = null)
    {
        try
        {
            // Par défaut, affiche une alerte à l'utilisateur
            var message = exception switch
            {
                UnauthorizedAccessException => "Vous n'êtes pas autorisé à effectuer cette action.",
                TimeoutException => "L'opération a pris trop de temps. Veuillez réessayer.",
                HttpRequestException => "Problème de connexion réseau. Vérifiez votre connexion.",
                _ => "Une erreur inattendue s'est produite. Veuillez réessayer."
            };

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!_isDisposed && Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Erreur", 
                        message, 
                        "OK");
                }
            });
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Erreur lors de la gestion d'erreur");
        }
    }

    #endregion

    #region Optimisations mémoire

    /// <summary>
    /// Nettoie le cache des propriétés (à appeler périodiquement)
    /// </summary>
    protected void ClearPropertyCache()
    {
        lock (_lockObject)
        {
            _propertyCache.Clear();
        }
    }

    /// <summary>
    /// Annule toutes les opérations en cours
    /// </summary>
    protected void CancelOperations()
    {
        try
        {
            CancellationTokenSource?.Cancel();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Erreur lors de l'annulation des opérations");
        }
    }

    #endregion

    #region IDisposable

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            CancelOperations();
            CancellationTokenSource?.Dispose();
            ClearPropertyCache();
            
            _isDisposed = true;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Erreur lors de la libération des ressources du ViewModel");
        }
    }

    #endregion
}

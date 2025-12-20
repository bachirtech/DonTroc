using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.Views;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

[QueryProperty(nameof(TransactionId), "transactionId")]
public class TransactionDetailsViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly RatingService _ratingService;
    private readonly TransactionService _transactionService;
    private Transaction? _transaction;
    private string? _transactionId;

    public TransactionDetailsViewModel(TransactionService transactionService, RatingService ratingService)
    {
        _transactionService = transactionService;
        _ratingService = ratingService;

        MarquerTermineeCommand = new Command(async () => await MarquerTermineeAsync());
        LaisserAvisCommand = new Command(async () => await LaisserAvisAsync());
    }

    public Transaction? Transaction
    {
        get => _transaction;
        set
        {
            _transaction = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMessages));
            OnPropertyChanged(nameof(HasRendezVousInfo));
        }
    }

    public string? TransactionId
    {
        get => _transactionId;
        set
        {
            _transactionId = value;
            OnPropertyChanged();
            _ = LoadTransactionAsync();
        }
    }

    public bool HasMessages => Transaction != null &&
                               (!string.IsNullOrEmpty(Transaction.MessageDemandeur) ||
                                !string.IsNullOrEmpty(Transaction.MessageProprietaire));

    public bool HasRendezVousInfo => Transaction != null &&
                                     (!string.IsNullOrEmpty(Transaction.LieuRendezVous) ||
                                      Transaction.DateRendezVous.HasValue);

    public ICommand MarquerTermineeCommand { get; }
    public ICommand LaisserAvisCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("transactionId", out var transactionId))
        {
            TransactionId = transactionId?.ToString();
        }
    }

    private async Task LoadTransactionAsync()
    {
        if (string.IsNullOrEmpty(TransactionId))
            return;

        try
        {
            Transaction = await _transactionService.GetTransactionByIdAsync(TransactionId);
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Erreur",
                $"Impossible de charger la transaction: {ex.Message}", "OK");
        }
    }

    private async Task MarquerTermineeAsync()
    {
        if (Transaction == null) return;

        try
        {
            await _transactionService.MarquerCommeTermineeAsync(Transaction.Id);
            Transaction.Statut = StatutTransaction.Terminee;
            OnPropertyChanged(nameof(Transaction));

            await Application.Current?.MainPage?.DisplayAlert("Succès", "Transaction marquée comme terminée", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Erreur",
                $"Impossible de mettre à jour la transaction: {ex.Message}", "OK");
        }
    }

    private async Task LaisserAvisAsync()
    {
        if (Transaction == null) return;

        try
        {
            // Navigation vers la vue d'évaluation
            await Shell.Current.GoToAsync($"{nameof(RatingView)}?transactionId={Transaction.Id}");
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Erreur",
                $"Impossible d'ouvrir la page d'évaluation: {ex.Message}", "OK");
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
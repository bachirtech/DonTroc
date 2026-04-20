using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DonTroc.Models;
using DonTroc.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Views;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

public class TransactionsViewModel : BaseViewModel
{
    private readonly TransactionService _transactionService;
    private readonly FirebaseService _firebaseService;
    private readonly RatingService _ratingService;
    private readonly AuthService _authService;
    
    public ObservableCollection<Transaction> Transactions { get; } = new();
    public ObservableCollection<Transaction> TransactionsEnAttente { get; } = new();
    
    private Transaction? _selectedTransaction;
    public Transaction? SelectedTransaction
    {
        get => _selectedTransaction;
        set => SetProperty(ref _selectedTransaction, value);
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ConfirmerTransactionCommand { get; }
    public ICommand AnnulerTransactionCommand { get; }
    public ICommand FinaliserTransactionCommand { get; }
    public ICommand EvaluerTransactionCommand { get; }
    public ICommand VoirDetailsCommand { get; }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TransactionsViewModel))]
    public TransactionsViewModel(TransactionService transactionService, FirebaseService firebaseService, RatingService ratingService, AuthService authService)
    {
        _transactionService = transactionService;
        _firebaseService = firebaseService;
        _ratingService = ratingService;
        _authService = authService;

        RefreshCommand = new Command(async void () => await RefreshAsync());
        ConfirmerTransactionCommand = new Command<Transaction>(async (transaction) => await ConfirmerTransactionAsync(transaction));
        AnnulerTransactionCommand = new Command<Transaction>(async (transaction) => await AnnulerTransactionAsync(transaction));
        FinaliserTransactionCommand = new Command<Transaction>(async (transaction) => await FinaliserTransactionAsync(transaction));
        EvaluerTransactionCommand = new Command<Transaction>(async (transaction) => await EvaluerTransactionAsync(transaction));
        VoirDetailsCommand = new Command<Transaction>(async (transaction) => await VoirDetailsAsync(transaction));
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            // Charger l'historique complet
            var historique = await _transactionService.GetHistoriqueTransactionsAsync();
            
            Transactions.Clear();
            foreach (var transaction in historique)
            {
                // Calculer les propriétés d'évaluation
                await CalculerProprietesEvaluationAsync(transaction);
                Transactions.Add(transaction);
            }

            // Charger les transactions en attente
            var enAttente = await _transactionService.GetTransactionsEnAttenteAsync();
            
            TransactionsEnAttente.Clear();
            foreach (var transaction in enAttente)
            {
                // Calculer les propriétés d'évaluation
                await CalculerProprietesEvaluationAsync(transaction);
                TransactionsEnAttente.Add(transaction);
            }
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", 
                    $"Impossible de charger les transactions : {ex.Message}", "OK");
            }
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task ConfirmerTransactionAsync(Transaction? transaction) // Confirmer une transaction
    {
        if (transaction == null || !transaction.PeutEtreConfirmee)
            return;

        var reponse = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Confirmer la transaction",
            "Voulez-vous ajouter un message de réponse ?",
            "Confirmer", "Annuler",
            placeholder: "Message optionnel...");

        if (reponse == null) return; // Annulé

        try
        {
            IsBusy = true;
            
            var succes = await _transactionService.ConfirmerTransactionAsync(transaction.Id, reponse);
            
            if (succes)
            {
                await Application.Current.MainPage!.DisplayAlert("Succès", 
                    "Transaction confirmée avec succès !", "OK");
                await RefreshAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", 
                    "Impossible de confirmer la transaction.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", 
                $"Erreur lors de la confirmation : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AnnulerTransactionAsync(Transaction? transaction)
    {
        if (transaction == null || !transaction.PeutEtreAnnulee)
            return;

        var confirmer = await Application.Current!.MainPage!.DisplayAlert(
            "Annuler la transaction",
            "Êtes-vous sûr de vouloir annuler cette transaction ?",
            "Oui", "Non");

        if (!confirmer) return;

        var motif = await Application.Current.MainPage.DisplayPromptAsync(
            "Motif d'annulation",
            "Veuillez indiquer le motif d'annulation (optionnel) :",
            "Annuler", "Retour",
            placeholder: "Motif...");

        if (motif == null) return; // Retour

        try
        {
            IsBusy = true;
            
            var succes = await _transactionService.AnnulerTransactionAsync(transaction.Id, motif);
            
            if (succes)
            {
                await Application.Current.MainPage.DisplayAlert("Succès", 
                    "Transaction annulée.", "OK");
                await RefreshAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", 
                    "Impossible d'annuler la transaction.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", 
                $"Erreur lors de l'annulation : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task FinaliserTransactionAsync(Transaction? transaction)
    {
        if (transaction == null || !transaction.PeutEtreFinalisee)
            return;

        var confirmer = await Application.Current!.MainPage!.DisplayAlert(
            "Finaliser la transaction",
            "Confirmer que l'échange a bien eu lieu ?",
            "Oui", "Non");

        if (!confirmer) return;

        try
        {
            IsBusy = true;
            
            var succes = await _transactionService.FinaliserTransactionAsync(transaction.Id);
            
            if (succes)
            {
                await Application.Current.MainPage.DisplayAlert("Succès", 
                    "Transaction finalisée ! Vous pouvez maintenant l'évaluer.", "OK");
                await RefreshAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erreur", 
                    "Impossible de finaliser la transaction.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", 
                $"Erreur lors de la finalisation : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EvaluerTransactionAsync(Transaction? transaction)
    {
        if (transaction == null || transaction.Statut != StatutTransaction.Terminee)
            return;

        try
        {
            // Naviguer vers la page d'évaluation avec l'ID de la transaction
            var navigationParameter = new Dictionary<string, object>
            {
                { "TransactionId", transaction.Id }
            };

            await Shell.Current.GoToAsync($"{nameof(RatingView)}", navigationParameter);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Erreur", 
                $"Impossible d'ouvrir la page d'évaluation : {ex.Message}", "OK");
        }
    }

    private async Task VoirDetailsAsync(Transaction? transaction) // Voir les de trensaction
    {
        if (transaction == null) return;

        SelectedTransaction = transaction;
        await Shell.Current.GoToAsync($"{nameof(TransactionDetailsView)}?transactionId={transaction.Id}");
    }

    /// <summary>
    /// Calcule les propriétés d'évaluation pour une transaction donnée
    /// </summary>
    private async Task CalculerProprietesEvaluationAsync(Transaction transaction)
    {
        try
        {
            var currentUserId = _authService.GetUserId();
            if (currentUserId == null)
            {
                transaction.PeutEtreEvaluee = false;
                transaction.DejaEvaluee = false;
                return;
            }

            // Une transaction peut être évaluée seulement si :
            // 1. Elle est terminée
            // 2. L'utilisateur actuel est partie prenante (propriétaire OU demandeur)
            //    Chacun évalue l'AUTRE : A évalue B, B évalue A
            transaction.PeutEtreEvaluee = transaction.Statut == StatutTransaction.Terminee &&
                                         (transaction.ProprietaireId == currentUserId || 
                                          transaction.DemandeurId == currentUserId);

            // Vérifier si l'utilisateur a déjà évalué cette transaction
            if (transaction.PeutEtreEvaluee)
            {
                var evaluationExistante = await _ratingService.VerifierEvaluationExistanteAsync(
                    transaction.Id, currentUserId);
                transaction.DejaEvaluee = evaluationExistante != null;
                
                // Si déjà évaluée, on ne peut plus l'évaluer
                if (transaction.DejaEvaluee)
                {
                    transaction.PeutEtreEvaluee = false;
                }
            }
            else
            {
                transaction.DejaEvaluee = false;
            }
        }
        catch (Exception ex)
        {
            // En cas d'erreur, on désactive l'évaluation par sécurité
            transaction.PeutEtreEvaluee = false;
            transaction.DejaEvaluee = false;
            
            // Log l'erreur (optionnel)
            System.Diagnostics.Debug.WriteLine($"Erreur lors du calcul des propriétés d'évaluation: {ex.Message}");
        }
    }
}

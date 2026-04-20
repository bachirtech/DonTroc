using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;

namespace DonTroc.Services;

public class TransactionService
{
    private readonly FirebaseClient _firebaseClient;
    private readonly AuthService _authService;
    
    public TransactionService(AuthService authService) // méthode pour la transaction
    {
        _authService = authService;
        _firebaseClient = new FirebaseClient(
            ConfigurationService.FirebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await _authService.GetAuthTokenAsync() ?? ""
            });
    }

    /// <summary>
    /// Propose une nouvelle transaction (don ou troc)
    /// </summary>
    public async Task<Transaction> ProposerTransactionAsync(string annonceId, string proprietaireId, 
        string? annonceEchangeId = null, string? message = null)
    {
        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel == null)
            throw new InvalidOperationException("Utilisateur non connecté");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            Type = annonceEchangeId != null ? TypeTransaction.Troc : TypeTransaction.Don,
            AnnonceId = annonceId,
            ProprietaireId = proprietaireId,
            DemandeurId = utilisateurActuel.Uid,
            AnnonceEchangeId = annonceEchangeId,
            MessageDemandeur = message,
            DateCreation = DateTime.UtcNow,
            Statut = StatutTransaction.EnAttente
        };

        await _firebaseClient
            .Child("transactions")
            .Child(transaction.Id)
            .PutAsync(transaction);

        return transaction;
    }

    /// <summary>
    /// Confirme une transaction (accepte la proposition)
    /// </summary>
    public async Task<bool> ConfirmerTransactionAsync(string transactionId, string? messageReponse = null)
    {
        var transaction = await GetTransactionAsync(transactionId);
        if (transaction == null || !transaction.PeutEtreConfirmee)
            return false;

        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel?.Uid != transaction.ProprietaireId)
            return false;

        transaction.Statut = StatutTransaction.Confirmee;
        transaction.DateConfirmation = DateTime.UtcNow;
        transaction.MessageProprietaire = messageReponse;

        await _firebaseClient
            .Child("transactions")
            .Child(transactionId)
            .PutAsync(transaction);

        // Mettre à jour le statut de l'annonce
        await MettreAJourStatutAnnonceAsync(transaction.AnnonceId, StatutAnnonce.Reservee, 
            transaction.DemandeurId, transactionId);

        return true;
    }

    /// <summary>
    /// Annule une transaction
    /// </summary>
    public async Task<bool> AnnulerTransactionAsync(string transactionId, string motif = "")
    {
        var transaction = await GetTransactionAsync(transactionId);
        if (transaction == null || !transaction.PeutEtreAnnulee)
            return false;

        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel?.Uid != transaction.ProprietaireId && 
            utilisateurActuel?.Uid != transaction.DemandeurId)
            return false;

        // Sauvegarder l'ancien statut AVANT modification pour le check de remise en disponible
        var ancienStatut = transaction.Statut;
        
        transaction.Statut = StatutTransaction.Annulee;
        transaction.Notes = motif;

        await _firebaseClient
            .Child("transactions")
            .Child(transactionId)
            .PutAsync(transaction);

        // Remettre l'annonce en disponible si elle était réservée (confirmée)
        if (ancienStatut == StatutTransaction.Confirmee)
        {
            await MettreAJourStatutAnnonceAsync(transaction.AnnonceId, StatutAnnonce.Disponible);
        }

        return true;
    }

    /// <summary>
    /// Finalise une transaction (marque comme terminée)
    /// </summary>
    public async Task<bool> FinaliserTransactionAsync(string transactionId, 
        string? lieuRendezVous = null, DateTime? dateRendezVous = null)
    {
        var transaction = await GetTransactionAsync(transactionId);
        if (transaction == null)
            return false;

        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel?.Uid != transaction.ProprietaireId && 
            utilisateurActuel?.Uid != transaction.DemandeurId)
            return false;

        transaction.Statut = StatutTransaction.Terminee;
        transaction.DateFinalisation = DateTime.UtcNow;
        transaction.LieuRendezVous = lieuRendezVous;
        transaction.DateRendezVous = dateRendezVous;

        await _firebaseClient
            .Child("transactions")
            .Child(transactionId)
            .PutAsync(transaction);

        // Marquer l'annonce comme échangée
        await MettreAJourStatutAnnonceAsync(transaction.AnnonceId, StatutAnnonce.Echangee, 
            null, transactionId, DateTime.UtcNow);

        // Si c'est un troc, marquer aussi l'annonce d'échange
        if (transaction.Type == TypeTransaction.Troc && !string.IsNullOrEmpty(transaction.AnnonceEchangeId))
        {
            await MettreAJourStatutAnnonceAsync(transaction.AnnonceEchangeId, StatutAnnonce.Echangee, 
                null, transactionId, DateTime.UtcNow);
        }

        return true;
    }

    /// <summary>
    /// Évalue une transaction terminée
    /// </summary>
    public async Task<bool> EvaluerTransactionAsync(string transactionId, int note, string? commentaire = null)
    {
        var transaction = await GetTransactionAsync(transactionId);
        if (transaction == null || transaction.Statut != StatutTransaction.Terminee)
            return false;

        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel == null)
            return false;

        if (utilisateurActuel.Uid == transaction.ProprietaireId)
        {
            transaction.EvaluationProprietaire = note;
            transaction.CommentaireProprietaire = commentaire;
        }
        else if (utilisateurActuel.Uid == transaction.DemandeurId)
        {
            transaction.EvaluationDemandeur = note;
            transaction.CommentaireDemandeur = commentaire;
        }
        else
        {
            return false;
        }

        await _firebaseClient
            .Child("transactions")
            .Child(transactionId)
            .PutAsync(transaction);

        return true;
    }

    /// <summary>
    /// Récupère une transaction par son ID
    /// </summary>
    public async Task<Transaction?> GetTransactionAsync(string transactionId)
    {
        try
        {
            var transaction = await _firebaseClient
                .Child("transactions")
                .Child(transactionId)
                .OnceSingleAsync<Transaction>();

            return transaction;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Récupère l'historique des transactions pour l'utilisateur actuel
    /// </summary>
    public async Task<List<Transaction>> GetHistoriqueTransactionsAsync()
    {
        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel == null)
            return new List<Transaction>();

        try
        {
            var transactions = await _firebaseClient
                .Child("transactions")
                .OnceAsync<Transaction>();

            return transactions
                .Where(t => t.Object.ProprietaireId == utilisateurActuel.Uid || 
                           t.Object.DemandeurId == utilisateurActuel.Uid)
                .Select(t => t.Object)
                .OrderByDescending(t => t.DateCreation)
                .ToList();
        }
        catch
        {
            return new List<Transaction>();
        }
    }

    /// <summary>
    /// Récupère les transactions en attente pour l'utilisateur actuel
    /// </summary>
    public async Task<List<Transaction>> GetTransactionsEnAttenteAsync()
    {
        var utilisateurActuel = await _authService.GetCurrentUserAsync();
        if (utilisateurActuel == null)
            return new List<Transaction>();

        try
        {
            var transactions = await _firebaseClient
                .Child("transactions")
                .OnceAsync<Transaction>();

            return transactions
                .Where(t => (t.Object.ProprietaireId == utilisateurActuel.Uid || 
                            t.Object.DemandeurId == utilisateurActuel.Uid) &&
                           t.Object.Statut == StatutTransaction.EnAttente)
                .Select(t => t.Object)
                .OrderByDescending(t => t.DateCreation)
                .ToList();
        }
        catch
        {
            return new List<Transaction>();
        }
    }

    /// <summary>
    /// Met à jour le statut d'une annonce
    /// </summary>
    private async Task MettreAJourStatutAnnonceAsync(string annonceId, StatutAnnonce statut, 
        string? utilisateurReserveId = null, string? transactionId = null, DateTime? dateEchange = null)
    {
        var updates = new Dictionary<string, object>
        {
            ["Statut"] = statut.ToString()
        };

        if (utilisateurReserveId != null)
        {
            updates["UtilisateurReserveId"] = utilisateurReserveId;
            updates["DateReservation"] = DateTime.UtcNow;
        }

        if (transactionId != null)
        {
            updates["TransactionId"] = transactionId;
        }

        if (dateEchange != null)
        {
            updates["DateEchange"] = dateEchange;
        }

        await _firebaseClient
            .Child("Annonces")
            .Child(annonceId)
            .PatchAsync(updates);
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string transactionId) // methode pour recuperer une transaction par son id
    {
        return await GetTransactionAsync(transactionId);
    }

    public async Task MarquerCommeTermineeAsync(string transactionId) // methode pour marquer une transaction comme terminee
    {
        await FinaliserTransactionAsync(transactionId);
    }
}

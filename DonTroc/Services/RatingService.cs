using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;

namespace DonTroc.Services;

/// <summary>
/// Service pour gérer les évaluations et le système de notation
/// </summary>
public class RatingService
{
    private readonly FirebaseClient _firebaseClient;
    private readonly AuthService _authService;
    private readonly FirebaseService _firebaseService;

    public RatingService(AuthService authService, FirebaseService firebaseService)
    {
        _authService = authService;
        _firebaseService = firebaseService;
        
        // Créer le client Firebase avec authentification, comme dans FirebaseService
        var baseUrl = "https://dontroc-55570-default-rtdb.europe-west1.firebasedatabase.app/";
        _firebaseClient = new FirebaseClient(
            baseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => authService.GetAuthTokenAsync()
            });
    }

    /// <summary>
    /// Créer une nouvelle évaluation
    /// </summary>
    public async Task<bool> CreerEvaluationAsync(Rating evaluation)
    {
        try
        {
            // Vérifier que l'utilisateur connecté est bien l'évaluateur
            var currentUserId = _authService.GetUserId();
            if (currentUserId != evaluation.EvaluateurId)
                return false;

            // Vérifier qu'il n'y a pas déjà une évaluation pour cette transaction
            var evaluationExistante = await VerifierEvaluationExistanteAsync(
                evaluation.TransactionId, evaluation.EvaluateurId);
            
            if (evaluationExistante != null)
                return false; // Évaluation déjà donnée

            // Générer un ID unique
            evaluation.Id = Guid.NewGuid().ToString();
            evaluation.DateCreation = DateTime.UtcNow;

            // Sauvegarder l'évaluation
            await _firebaseClient
                .Child("Ratings")
                .Child(evaluation.Id)
                .PutAsync(evaluation);

            // Mettre à jour les statistiques du profil évalué
            await MettreAJourStatistiquesUtilisateurAsync(evaluation.EvalueId);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la création de l'évaluation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Modifier une évaluation existante
    /// </summary>
    public async Task<bool> ModifierEvaluationAsync(Rating evaluation)
    {
        try
        {
            var currentUserId = _authService.GetUserId();
            if (currentUserId != evaluation.EvaluateurId)
                return false;

            evaluation.EstModifiee = true;
            evaluation.DateModification = DateTime.UtcNow;

            await _firebaseClient
                .Child("Ratings")
                .Child(evaluation.Id)
                .PutAsync(evaluation);

            // Recalculer les statistiques
            await MettreAJourStatistiquesUtilisateurAsync(evaluation.EvalueId);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la modification de l'évaluation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Récupérer les évaluations d'un utilisateur
    /// </summary>
    public async Task<List<Rating>> GetEvaluationsUtilisateurAsync(string userId, int limite = 10)
    {
        try
        {
            var evaluations = await _firebaseClient
                .Child("Ratings")
                .OrderBy("EvalueId")
                .EqualTo(userId)
                .OnceAsync<Rating>();

            return evaluations
                .Select(item => item.Object)
                .OrderByDescending(r => r.DateCreation)
                .Take(limite)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des évaluations: {ex.Message}");
            return new List<Rating>();
        }
    }

    /// <summary>
    /// Vérifier si une évaluation existe déjà pour une transaction donnée
    /// </summary>
    public async Task<Rating?> VerifierEvaluationExistanteAsync(string transactionId, string evaluateurId)
    {
        try
        {
            var evaluations = await _firebaseClient
                .Child("Ratings")
                .OrderBy("TransactionId")
                .EqualTo(transactionId)
                .OnceAsync<Rating>();

            return evaluations
                .Select(item => item.Object)
                .FirstOrDefault(r => r.EvaluateurId == evaluateurId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la vérification d'évaluation: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Calculer les statistiques de notation d'un utilisateur
    /// </summary>
    public async Task<(double noteMoyenne, int nombreEvaluations)> CalculerStatistiquesAsync(string userId)
    {
        try
        {
            var evaluations = await GetEvaluationsUtilisateurAsync(userId, int.MaxValue);
            
            if (!evaluations.Any())
                return (0.0, 0);

            var noteMoyenne = Math.Round(evaluations.Average(r => r.Note), 1);
            return (noteMoyenne, evaluations.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du calcul des statistiques: {ex.Message}");
            return (0.0, 0);
        }
    }

    /// <summary>
    /// Mettre à jour les statistiques d'un utilisateur dans son profil
    /// </summary>
    private async Task MettreAJourStatistiquesUtilisateurAsync(string userId)
    {
        try
        {
            var (noteMoyenne, nombreEvaluations) = await CalculerStatistiquesAsync(userId);
            
            // Calculer le nombre d'échanges réussis (transactions terminées)
            var transactions = await _firebaseClient
                .Child("Transactions")
                .OrderBy("Statut")
                .EqualTo((int)StatutTransaction.Terminee)
                .OnceAsync<Transaction>();

            var nombreEchanges = transactions
                .Count(t => t.Object.ProprietaireId == userId || t.Object.DemandeurId == userId);

            // Utiliser l'instance injectée du FirebaseService pour mettre à jour les statistiques
            await _firebaseService.UpdateUserRatingStatsAsync(userId, noteMoyenne, nombreEvaluations, nombreEchanges);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la mise à jour des statistiques: {ex.Message}");
        }
    }

    /// <summary>
    /// Récupérer les évaluations pour une transaction spécifique
    /// </summary>
    public async Task<List<Rating>> GetEvaluationsTransactionAsync(string transactionId)
    {
        try
        {
            var evaluations = await _firebaseClient
                .Child("Ratings")
                .OrderBy("TransactionId")
                .EqualTo(transactionId)
                .OnceAsync<Rating>();

            return evaluations.Select(item => item.Object).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération des évaluations de transaction: {ex.Message}");
            return new List<Rating>();
        }
    }

    /// <summary>
    /// Vérifier si l'utilisateur peut évaluer cette transaction
    /// </summary>
    public async Task<bool> PeutEvaluerTransactionAsync(string transactionId, string userId)
    {
        try
        {
            // Vérifier que la transaction existe et est terminée
            var transaction = await _firebaseClient
                .Child("Transactions")
                .Child(transactionId)
                .OnceSingleAsync<Transaction>();

            if (transaction == null || transaction.Statut != StatutTransaction.Terminee)
                return false;

            // Vérifier que l'utilisateur est partie prenante de la transaction
            bool estPartiePrenante = transaction.ProprietaireId == userId || 
                                   transaction.DemandeurId == userId;

            if (!estPartiePrenante)
                return false;

            // Vérifier qu'il n'a pas déjà évalué
            var evaluationExistante = await VerifierEvaluationExistanteAsync(transactionId, userId);
            return evaluationExistante == null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la vérification des droits d'évaluation: {ex.Message}");
            return false;
        }
    }
}

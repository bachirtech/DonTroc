using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services;

/// <summary>
/// Service gérant les propositions de troc structurées entre utilisateurs.
/// Stockage Firebase : nœud racine "tradeProposals/{proposalId}".
/// </summary>
public class TradeProposalService
{
    private const string Node = "tradeProposals";
    private const int MaxRounds = 3;

    private readonly FirebaseClient _firebaseClient;
    private readonly AuthService _authService;
    private readonly FirebaseService _firebaseService;
    private readonly TransactionService _transactionService;
    private readonly PushNotificationService _pushNotificationService;
    private readonly NotificationService _notificationService;
    private readonly GamificationService _gamificationService;
    private readonly ILogger<TradeProposalService> _logger;

    public TradeProposalService(
        AuthService authService,
        FirebaseService firebaseService,
        TransactionService transactionService,
        PushNotificationService pushNotificationService,
        NotificationService notificationService,
        GamificationService gamificationService,
        ILogger<TradeProposalService> logger)
    {
        _authService = authService;
        _firebaseService = firebaseService;
        _transactionService = transactionService;
        _pushNotificationService = pushNotificationService;
        _notificationService = notificationService;
        _gamificationService = gamificationService;
        _logger = logger;

        _firebaseClient = new FirebaseClient(
            ConfigurationService.FirebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () => await _authService.GetAuthTokenAsync() ?? string.Empty
            });
    }

    // ═════════════════════════════════════════════════════════
    // CRÉATION
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Crée une nouvelle proposition de troc.
    /// </summary>
    public async Task<TradeProposal?> CreateProposalAsync(
        Annonce targetAnnonce,
        Annonce offeredAnnonce,
        string? message,
        int roundNumber = 1,
        string? parentProposalId = null)
    {
        try
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
                throw new InvalidOperationException("Utilisateur non connecté");

            if (string.Equals(currentUser.Uid, targetAnnonce.UtilisateurId, StringComparison.Ordinal))
                throw new InvalidOperationException("Vous ne pouvez pas proposer un troc sur votre propre annonce.");

            if (!string.Equals(currentUser.Uid, offeredAnnonce.UtilisateurId, StringComparison.Ordinal))
                throw new InvalidOperationException("L'annonce offerte doit vous appartenir.");

            if (offeredAnnonce.Statut != StatutAnnonce.Disponible)
                throw new InvalidOperationException("L'annonce offerte doit être disponible.");

            // Empêcher les doublons actifs
            var existing = await GetProposalsBetweenAsync(
                currentUser.Uid, targetAnnonce.Id, offeredAnnonce.Id);
            if (existing.Any(p => p.Statut == TradeProposalStatus.Pending))
                throw new InvalidOperationException("Une proposition est déjà en attente pour ce troc.");

            var proposerProfile = await _firebaseService.GetUserProfileAsync(currentUser.Uid);

            var proposal = new TradeProposal
            {
                Id = Guid.NewGuid().ToString(),
                AnnonceCibleId = targetAnnonce.Id,
                OfferedAnnonceId = offeredAnnonce.Id,
                ProposerId = currentUser.Uid,
                OwnerId = targetAnnonce.UtilisateurId,
                Message = message,
                Statut = TradeProposalStatus.Pending,
                DateCreationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                RoundNumber = Math.Clamp(roundNumber, 1, MaxRounds),
                ParentProposalId = parentProposalId,

                // Snapshot pour affichage rapide
                AnnonceCibleTitre = targetAnnonce.Titre,
                AnnonceCiblePhoto = targetAnnonce.FirstPhotoUrl,
                OfferedAnnonceTitre = offeredAnnonce.Titre,
                OfferedAnnoncePhoto = offeredAnnonce.FirstPhotoUrl,
                ProposerName = proposerProfile?.Name ?? "Un utilisateur",
                ProposerPhotoUrl = proposerProfile?.ProfilePictureUrl
            };

            await _firebaseClient.Child(Node).Child(proposal.Id).PutAsync(proposal);

            // Gamification + notifications (non bloquant)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gamificationService.OnUserActionAsync(currentUser.Uid, "propose_trade");
                    await NotifyOwnerOfNewProposalAsync(proposal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Post-création proposition : erreur non bloquante");
                }
            });

            return proposal;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de la proposition de troc");
            return null;
        }
    }

    // ═════════════════════════════════════════════════════════
    // ACTIONS
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Accepte une proposition : crée une Transaction Troc associée.
    /// </summary>
    public async Task<bool> AcceptAsync(string proposalId)
    {
        try
        {
            var proposal = await GetProposalAsync(proposalId);
            if (proposal == null || proposal.Statut != TradeProposalStatus.Pending)
                return false;

            var userId = _authService.GetUserId();
            if (userId != proposal.OwnerId) return false;

            // Créer la transaction de troc
            Transaction? transaction = null;
            try
            {
                transaction = await _transactionService.ProposerTransactionAsync(
                    proposal.AnnonceCibleId,
                    proposal.OwnerId,
                    proposal.OfferedAnnonceId,
                    proposal.Message);

                if (transaction != null)
                {
                    // Confirmer immédiatement (le owner ayant accepté)
                    await _transactionService.ConfirmerTransactionAsync(
                        transaction.Id, "Proposition de troc acceptée");
                }
            }
            catch (Exception txEx)
            {
                _logger.LogError(txEx, "Erreur création transaction depuis proposition {Id}", proposalId);
            }

            proposal.Statut = TradeProposalStatus.Accepted;
            proposal.DateReponseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            proposal.TransactionId = transaction?.Id;

            await _firebaseClient.Child(Node).Child(proposalId).PutAsync(proposal);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _gamificationService.OnUserActionAsync(proposal.OwnerId, "accept_trade");
                    await NotifyProposerOfResponseAsync(proposal, accepted: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Post-accept : erreur non bloquante");
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur AcceptAsync {Id}", proposalId);
            return false;
        }
    }

    /// <summary>
    /// Refuse une proposition.
    /// </summary>
    public async Task<bool> DeclineAsync(string proposalId, string? motif = null)
    {
        try
        {
            var proposal = await GetProposalAsync(proposalId);
            if (proposal == null || proposal.Statut != TradeProposalStatus.Pending)
                return false;

            var userId = _authService.GetUserId();
            if (userId != proposal.OwnerId) return false;

            proposal.Statut = TradeProposalStatus.Declined;
            proposal.MotifRefus = motif;
            proposal.DateReponseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await _firebaseClient.Child(Node).Child(proposalId).PutAsync(proposal);

            _ = Task.Run(async () =>
            {
                try
                {
                    await NotifyProposerOfResponseAsync(proposal, accepted: false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Post-decline : erreur non bloquante");
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur DeclineAsync {Id}", proposalId);
            return false;
        }
    }

    /// <summary>
    /// Annule une proposition (par le proposeur uniquement).
    /// </summary>
    public async Task<bool> CancelAsync(string proposalId)
    {
        try
        {
            var proposal = await GetProposalAsync(proposalId);
            if (proposal == null || proposal.Statut != TradeProposalStatus.Pending)
                return false;

            var userId = _authService.GetUserId();
            if (userId != proposal.ProposerId) return false;

            proposal.Statut = TradeProposalStatus.Cancelled;
            proposal.DateReponseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await _firebaseClient.Child(Node).Child(proposalId).PutAsync(proposal);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur CancelAsync {Id}", proposalId);
            return false;
        }
    }

    // ═════════════════════════════════════════════════════════
    // LECTURE
    // ═════════════════════════════════════════════════════════

    public async Task<TradeProposal?> GetProposalAsync(string proposalId)
    {
        try
        {
            return await _firebaseClient.Child(Node).Child(proposalId)
                .OnceSingleAsync<TradeProposal>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetProposalAsync {Id}", proposalId);
            return null;
        }
    }

    /// <summary>
    /// Propositions reçues par l'utilisateur connecté (ses annonces sont ciblées).
    /// </summary>
    public async Task<List<TradeProposal>> GetReceivedAsync()
    {
        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new List<TradeProposal>();

        try
        {
            var items = await _firebaseClient.Child(Node)
                .OrderBy("OwnerId").EqualTo(userId)
                .OnceAsync<TradeProposal>();

            var list = items.Select(i => { i.Object.Id = i.Key; return i.Object; })
                .OrderByDescending(p => p.DateCreationTimestamp)
                .ToList();

            await AutoExpireAsync(list);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetReceivedAsync échec");
            return new List<TradeProposal>();
        }
    }

    /// <summary>
    /// Propositions envoyées par l'utilisateur connecté.
    /// </summary>
    public async Task<List<TradeProposal>> GetSentAsync()
    {
        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId)) return new List<TradeProposal>();

        try
        {
            var items = await _firebaseClient.Child(Node)
                .OrderBy("ProposerId").EqualTo(userId)
                .OnceAsync<TradeProposal>();

            var list = items.Select(i => { i.Object.Id = i.Key; return i.Object; })
                .OrderByDescending(p => p.DateCreationTimestamp)
                .ToList();

            await AutoExpireAsync(list);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetSentAsync échec");
            return new List<TradeProposal>();
        }
    }

    /// <summary>
    /// Nombre de propositions en attente reçues (pour badge UI).
    /// </summary>
    public async Task<int> GetPendingReceivedCountAsync()
    {
        var list = await GetReceivedAsync();
        return list.Count(p => p.Statut == TradeProposalStatus.Pending);
    }

    private async Task<List<TradeProposal>> GetProposalsBetweenAsync(
        string proposerId, string targetAnnonceId, string offeredAnnonceId)
    {
        try
        {
            var items = await _firebaseClient.Child(Node)
                .OrderBy("ProposerId").EqualTo(proposerId)
                .OnceAsync<TradeProposal>();

            return items
                .Select(i => { i.Object.Id = i.Key; return i.Object; })
                .Where(p => p.AnnonceCibleId == targetAnnonceId &&
                            p.OfferedAnnonceId == offeredAnnonceId)
                .ToList();
        }
        catch
        {
            return new List<TradeProposal>();
        }
    }

    /// <summary>
    /// Expire automatiquement les propositions anciennes (> 7 jours sans réponse).
    /// </summary>
    private async Task AutoExpireAsync(IEnumerable<TradeProposal> proposals)
    {
        foreach (var p in proposals.Where(p => p.Statut == TradeProposalStatus.Pending && p.IsExpired()))
        {
            try
            {
                p.Statut = TradeProposalStatus.Expired;
                p.DateReponseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _firebaseClient.Child(Node).Child(p.Id).PutAsync(p);
            }
            catch
            {
                // Best-effort
            }
        }
    }

    // ═════════════════════════════════════════════════════════
    // NOTIFICATIONS
    // ═════════════════════════════════════════════════════════

    private async Task NotifyOwnerOfNewProposalAsync(TradeProposal proposal)
    {
        try
        {
            var owner = await _firebaseService.GetUserProfileAsync(proposal.OwnerId);
            var title = "🔄 Nouvelle proposition de troc";
            var body = $"{proposal.ProposerName ?? "Quelqu'un"} propose : {proposal.OfferedAnnonceTitre} ↔ {proposal.AnnonceCibleTitre}";

            // Push
            if (!string.IsNullOrWhiteSpace(owner?.FcmToken))
            {
                await _pushNotificationService.SendNotificationAsync(
                    owner.FcmToken, title, body,
                    new Dictionary<string, string>
                    {
                        ["type"] = "trade_proposal",
                        ["proposalId"] = proposal.Id
                    });
            }

            // Notif locale (au cas où l'app est ouverte)
            await _notificationService.ShowGamificationNotificationAsync(
                title, body, "trade_proposal", proposal.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotifyOwnerOfNewProposalAsync");
        }
    }

    private async Task NotifyProposerOfResponseAsync(TradeProposal proposal, bool accepted)
    {
        try
        {
            var proposer = await _firebaseService.GetUserProfileAsync(proposal.ProposerId);
            var title = accepted ? "✅ Troc accepté !" : "❌ Troc refusé";
            var body = accepted
                ? $"Votre proposition pour « {proposal.AnnonceCibleTitre} » a été acceptée !"
                : $"Votre proposition pour « {proposal.AnnonceCibleTitre} » a été refusée.";

            if (!string.IsNullOrWhiteSpace(proposer?.FcmToken))
            {
                await _pushNotificationService.SendNotificationAsync(
                    proposer.FcmToken, title, body,
                    new Dictionary<string, string>
                    {
                        ["type"] = "trade_proposal_response",
                        ["proposalId"] = proposal.Id,
                        ["accepted"] = accepted.ToString()
                    });
            }

            await _notificationService.ShowGamificationNotificationAsync(
                title, body, "trade_proposal_response", proposal.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotifyProposerOfResponseAsync");
        }
    }
}


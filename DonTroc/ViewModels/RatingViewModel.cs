using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour gérer les évaluations et le système de notation
/// </summary>
public class RatingViewModel : BaseViewModel
{
    private readonly RatingService _ratingService;
    private readonly AuthService _authService;
    private readonly TransactionService _transactionService;
    private readonly FirebaseService _firebaseService;
    
    private Transaction? _transaction;
    private UserProfile? _utilisateurAEvaluer;
    private Rating? _evaluationActuelle;
    private int _noteSelectionnee = 5;
    private string _commentaire = string.Empty;
    private bool _peutEvaluer = false;
    private bool _evaluationDejaFaite = false;
    private string _nomEvaluateur = string.Empty;

    public RatingViewModel(RatingService ratingService, AuthService authService, 
        TransactionService transactionService, FirebaseService firebaseService)
    {
        _ratingService = ratingService;
        _authService = authService;
        _transactionService = transactionService;
        _firebaseService = firebaseService;
        
        // Commandes
        SoumettreEvaluationCommand = new Command(async () => await SoumettreEvaluationAsync(), () => PeutEvaluer);
        ModifierEvaluationCommand = new Command(async () => await ModifierEvaluationAsync(), () => EvaluationDejaFaite);
        
        // Collection des évaluations
        EvaluationsUtilisateur = new ObservableCollection<Rating>();
    }

    // === PROPRIÉTÉS ===

    public Transaction? Transaction
    {
        get => _transaction;
        set => SetProperty(ref _transaction, value);
    }

    public UserProfile? UtilisateurAEvaluer
    {
        get => _utilisateurAEvaluer;
        set => SetProperty(ref _utilisateurAEvaluer, value);
    }

    public Rating? EvaluationActuelle
    {
        get => _evaluationActuelle;
        set
        {
            if (SetProperty(ref _evaluationActuelle, value))
            {
                OnPropertyChanged(nameof(DateCreation));
            }
        }
    }

    public int NoteSelectionnee
    {
        get => _noteSelectionnee;
        set
        {
            if (SetProperty(ref _noteSelectionnee, value))
            {
                OnPropertyChanged(nameof(Etoile1));
                OnPropertyChanged(nameof(Etoile2));
                OnPropertyChanged(nameof(Etoile3));
                OnPropertyChanged(nameof(Etoile4));
                OnPropertyChanged(nameof(Etoile5));
                ((Command)SoumettreEvaluationCommand).ChangeCanExecute();
            }
        }
    }

    public string Commentaire
    {
        get => _commentaire;
        set => SetProperty(ref _commentaire, value);
    }

    public bool PeutEvaluer
    {
        get => _peutEvaluer;
        set
        {
            if (SetProperty(ref _peutEvaluer, value))
                ((Command)SoumettreEvaluationCommand).ChangeCanExecute();
        }
    }

    public bool EvaluationDejaFaite
    {
        get => _evaluationDejaFaite;
        set
        {
            if (SetProperty(ref _evaluationDejaFaite, value))
                ((Command)ModifierEvaluationCommand).ChangeCanExecute();
        }
    }

    public string NomEvaluateur
    {
        get => _nomEvaluateur;
        set => SetProperty(ref _nomEvaluateur, value);
    }

    /// <summary>
    /// Propriété Note pour la compatibilité avec la vue (alias de NoteSelectionnee)
    /// </summary>
    public int Note
    {
        get => NoteSelectionnee;
        set => NoteSelectionnee = value;
    }

    /// <summary>
    /// Date de création de l'évaluation actuelle
    /// </summary>
    public DateTime? DateCreation => EvaluationActuelle?.DateCreation;

    public ObservableCollection<Rating> EvaluationsUtilisateur { get; }

    // === PROPRIÉTÉS POUR L'AFFICHAGE DES ÉTOILES ===

    public bool Etoile1 => NoteSelectionnee >= 1;
    public bool Etoile2 => NoteSelectionnee >= 2;
    public bool Etoile3 => NoteSelectionnee >= 3;
    public bool Etoile4 => NoteSelectionnee >= 4;
    public bool Etoile5 => NoteSelectionnee >= 5;

    // === COMMANDES ===

    public ICommand SoumettreEvaluationCommand { get; }
    public ICommand ModifierEvaluationCommand { get; }

    public ICommand SelectionnerNoteCommand => new Command<int>(note => NoteSelectionnee = note);

    // === MÉTHODES ===

    /// <summary>
    /// Initialiser l'évaluation pour une transaction donnée
    /// </summary>
    public async Task InitialiserEvaluationAsync(string transactionId)
    {
        try
        {
            IsBusy = true;
            
            // Récupérer la transaction depuis Firebase
            Transaction = await _transactionService.GetTransactionAsync(transactionId);
            
            var currentUserId = _authService.GetUserId();
            if (Transaction == null || currentUserId == null) return;

            // Déterminer qui évaluer
            var utilisateurAEvaluerId = Transaction.ProprietaireId == currentUserId 
                ? Transaction.DemandeurId 
                : Transaction.ProprietaireId;

            // Récupérer le profil de l'utilisateur à évaluer
            UtilisateurAEvaluer = await _firebaseService.GetUserProfileAsync(utilisateurAEvaluerId);

            // Vérifier si l'utilisateur peut évaluer
            PeutEvaluer = await _ratingService.PeutEvaluerTransactionAsync(transactionId, currentUserId);

            // Vérifier s'il y a déjà une évaluation
            EvaluationActuelle = await _ratingService.VerifierEvaluationExistanteAsync(transactionId, currentUserId);
            EvaluationDejaFaite = EvaluationActuelle != null;

            // Si évaluation existante, charger les données
            if (EvaluationActuelle != null)
            {
                NoteSelectionnee = EvaluationActuelle.Note;
                Commentaire = EvaluationActuelle.Commentaire ?? string.Empty;
            }

            // Charger les évaluations de l'utilisateur à évaluer
            await ChargerEvaluationsUtilisateurAsync(utilisateurAEvaluerId);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                $"Erreur lors de l'initialisation: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Charger les évaluations d'un utilisateur
    /// </summary>
    private async Task ChargerEvaluationsUtilisateurAsync(string userId)
    {
        try
        {
            var evaluations = await _ratingService.GetEvaluationsUtilisateurAsync(userId);
            
            EvaluationsUtilisateur.Clear();
            foreach (var evaluation in evaluations)
            {
                EvaluationsUtilisateur.Add(evaluation);
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                $"Erreur lors du chargement des évaluations: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Soumettre une nouvelle évaluation
    /// </summary>
    private async Task SoumettreEvaluationAsync()
    {
        try
        {
            if (Transaction == null || UtilisateurAEvaluer == null) return;

            IsBusy = true;

            var currentUserId = _authService.GetUserId();
            if (currentUserId == null) return;

            var nouvelleEvaluation = new Rating
            {
                TransactionId = Transaction.Id,
                EvaluateurId = currentUserId,
                EvalueId = UtilisateurAEvaluer.Id,
                Note = NoteSelectionnee,
                Commentaire = string.IsNullOrWhiteSpace(Commentaire) ? null : Commentaire.Trim()
            };

            var succes = await _ratingService.CreerEvaluationAsync(nouvelleEvaluation);

            if (succes)
            {
                await Application.Current!.MainPage!.DisplayAlert("Succès", 
                    "Votre évaluation a été enregistrée avec succès!", "OK");
                
                EvaluationDejaFaite = true;
                PeutEvaluer = false;
                
                // Recharger les évaluations
                await ChargerEvaluationsUtilisateurAsync(UtilisateurAEvaluer.Id);
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                    "Impossible d'enregistrer votre évaluation. Veuillez réessayer.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                $"Erreur lors de l'enregistrement: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Modifier une évaluation existante
    /// </summary>
    private async Task ModifierEvaluationAsync()
    {
        try
        {
            if (EvaluationActuelle == null) return;

            IsBusy = true;

            EvaluationActuelle.Note = NoteSelectionnee;
            EvaluationActuelle.Commentaire = string.IsNullOrWhiteSpace(Commentaire) ? null : Commentaire.Trim();

            var succes = await _ratingService.ModifierEvaluationAsync(EvaluationActuelle);

            if (succes)
            {
                await Application.Current!.MainPage!.DisplayAlert("Succès", 
                    "Votre évaluation a été modifiée avec succès!", "OK");
                
                // Recharger les évaluations
                if (UtilisateurAEvaluer != null)
                    await ChargerEvaluationsUtilisateurAsync(UtilisateurAEvaluer.Id);
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                    "Impossible de modifier votre évaluation. Veuillez réessayer.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                $"Erreur lors de la modification: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Obtenir la description textuelle de la note
    /// </summary>
    public string GetDescriptionNote(int note)
    {
        return note switch
        {
            1 => "Très insatisfaisant",
            2 => "Insatisfaisant", 
            3 => "Correct",
            4 => "Satisfaisant",
            5 => "Excellent",
            _ => ""
        };
    }
}

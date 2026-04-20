using System;
using System.Threading.Tasks;

namespace DonTroc.Services;

/// <summary>
/// Type de mise à jour Play In-App Update.
/// </summary>
public enum InAppUpdateMode
{
    /// <summary>Téléchargement en arrière-plan + bouton "Installer" (UX douce).</summary>
    Flexible = 0,
    /// <summary>Overlay plein écran bloquant Google Play (force update).</summary>
    Immediate = 1
}

/// <summary>
/// Résultat d'une tentative d'affichage du flow Play In-App Update.
/// </summary>
public enum InAppUpdateResult
{
    /// <summary>Service indisponible (iOS, OS Android &lt; 5, ou Play Store absent).</summary>
    NotSupported,
    /// <summary>Aucune mise à jour disponible sur le Play Store.</summary>
    NoUpdate,
    /// <summary>Mise à jour dispo mais le mode demandé n'est pas autorisé par Google.</summary>
    UpdateModeNotAllowed,
    /// <summary>Flow Play Store affiché à l'utilisateur (résultat final via OnActivityResult).</summary>
    FlowStarted,
    /// <summary>Erreur lors de la communication avec Play Core.</summary>
    Error
}

/// <summary>
/// Abstraction du Google Play In-App Update SDK.
/// Implémentation Android via Play Core, no-op sur iOS.
/// </summary>
public interface IInAppUpdateService
{
    /// <summary>True si le service est utilisable sur la plateforme courante (Android + Play Store).</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Tente de démarrer un flow Play In-App Update.
    /// Le résultat final (succès/échec d'install) arrive via <see cref="MainActivity.OnActivityResult"/>.
    /// </summary>
    Task<InAppUpdateResult> TryStartUpdateAsync(InAppUpdateMode mode);

    /// <summary>
    /// À appeler dans OnResume : si un update Immediate était en cours
    /// (ex : utilisateur a quitté pendant le téléchargement),
    /// relance automatiquement l'overlay Google Play.
    /// </summary>
    Task ResumeIfImmediateUpdatePendingAsync();

    /// <summary>
    /// Événement levé quand un update Flexible a fini de télécharger
    /// (l'utilisateur peut maintenant cliquer pour installer).
    /// </summary>
    event Action? FlexibleUpdateDownloaded;

    /// <summary>
    /// Termine et installe une MAJ Flexible déjà téléchargée
    /// (déclenche un redémarrage de l'app par Google Play).
    /// </summary>
    Task CompleteFlexibleUpdateAsync();
}


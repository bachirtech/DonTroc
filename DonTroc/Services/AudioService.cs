using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;

namespace DonTroc.Services
{
    /// <summary>
    /// Service audio optimisé pour les messages vocaux avec sécurité renforcée
    /// </summary>
    public class AudioService
    {
        private IAudioPlayer? _audioPlayer;
        private IAudioRecorder? _audioRecorder; // Ajout de l'enregistreur
        private string? _currentRecordingPath;
        private bool _isRecording;
        private bool _isPlaying;
        private CancellationTokenSource? _recordingCancellation;
        private DateTime _recordingStartTime;
        private readonly List<string> _allowedFormats = new() { ".wav", ".m4a", ".mp3" };
        private const int MAX_RECORDING_DURATION_SECONDS = 300; // 5 minutes max
        private const int MIN_RECORDING_DURATION_MS = 500; // 0.5 seconde min
        private readonly IAudioManager _audioManager;

        public event Action<TimeSpan>? RecordingDurationUpdated;
        public event Action<bool>? RecordingStateChanged;
        public event Action<string>? PlaybackStatusChanged;

        public bool IsRecording => _isRecording;
        public bool IsPlaying => _isPlaying;

        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        /// <summary>
        /// Démarre l'enregistrement d'un message vocal avec validation sécurisée
        /// </summary>
        public async Task<bool> StartRecordingAsync()
        {
            try
            {
                if (_isRecording)
                    return false;

                // Vérifier les permissions microphone
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    throw new UnauthorizedAccessException("Permission microphone refusée");
                }

                // Créer un nom de fichier sécurisé
                var fileName = $"voice_{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}.m4a";
                var documentsPath = FileSystem.CacheDirectory;
                _currentRecordingPath = Path.Combine(documentsPath, fileName);

                // Utiliser le vrai enregistreur
                _audioRecorder = _audioManager.CreateRecorder();
                if (_audioRecorder == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Impossible de créer l'enregistreur audio.");
                    return false;
                }

                _recordingCancellation = new CancellationTokenSource();
                _isRecording = true;
                _recordingStartTime = DateTime.Now;

                RecordingStateChanged?.Invoke(true);

                // Démarrer le monitoring de la durée
                _ = Task.Run(MonitorRecordingDuration);

                // Démarrer l'enregistrement
                await _audioRecorder.StartAsync(_currentRecordingPath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur démarrage enregistrement: {ex.Message}");
                RecordingStateChanged?.Invoke(false);
                return false;
            }
        }

        /// <summary>
        /// Arrête l'enregistrement avec validation de sécurité
        /// </summary>
        public async Task<string?> StopRecordingAsync()
        {
            try
            {
                if (!_isRecording || string.IsNullOrEmpty(_currentRecordingPath) || _audioRecorder == null)
                    return null;

                _recordingCancellation?.Cancel();
                _isRecording = false;
                RecordingStateChanged?.Invoke(false);

                // Arrêter l'enregistrement
                await _audioRecorder.StopAsync();
                _audioRecorder = null;

                var duration = DateTime.Now - _recordingStartTime;
                
                // Validation de la durée
                if (duration.TotalMilliseconds < MIN_RECORDING_DURATION_MS)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Enregistrement trop court");
                    // Supprimer le fichier si trop court
                    if (File.Exists(_currentRecordingPath))
                        File.Delete(_currentRecordingPath);
                    return null;
                }

                if (duration.TotalSeconds > MAX_RECORDING_DURATION_SECONDS)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Enregistrement trop long");
                    return null;
                }

                // Validation du fichier créé
                if (await ValidateAudioFileAsync(_currentRecordingPath))
                {
                    return _currentRecordingPath;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur arrêt enregistrement: {ex.Message}");
                _isRecording = false;
                RecordingStateChanged?.Invoke(false);
                return null;
            }
        }

        /// <summary>
        /// Lit un message vocal avec contrôles de sécurité
        /// </summary>
        public async Task<bool> PlayVoiceMessageAsync(string filePath)
        {
            try
            {
                if (_isPlaying || string.IsNullOrEmpty(filePath))
                    return false;

                // Validation sécurisée du fichier
                if (!await ValidateAudioFileAsync(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Fichier audio non valide ou potentiellement dangereux");
                    return false;
                }

                _isPlaying = true;
                PlaybackStatusChanged?.Invoke("▶️ Lecture en cours...");

                // Créer le lecteur audio
                _audioPlayer = _audioManager.CreatePlayer(filePath);
                if (_audioPlayer == null)
                {
                    _isPlaying = false;
                    PlaybackStatusChanged?.Invoke("❌ Erreur de lecture");
                    return false;
                }

                // Écouter la fin de lecture
                _audioPlayer.PlaybackEnded += OnPlaybackEnded;
                _audioPlayer.Play();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lecture audio: {ex.Message}");
                _isPlaying = false;
                PlaybackStatusChanged?.Invoke("❌ Erreur de lecture");
                return false;
            }
        }

        /// <summary>
        /// Arrête la lecture en cours
        /// </summary>
        public Task StopPlaybackAsync()
        {
            try
            {
                if (_isPlaying && _audioPlayer != null)
                {
                    _audioPlayer.Stop();
                    _audioPlayer.Dispose();
                    _audioPlayer = null;
                    _isPlaying = false;
                    PlaybackStatusChanged?.Invoke("⏹️ Arrêté");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur arrêt lecture: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Lit un fichier audio avec callback de fin - méthode utilisée par ChatViewModel
        /// </summary>
        public async Task PlayAsync(string filePath, Action? onComplete = null)
        {
            try
            {
                if (_isPlaying || string.IsNullOrEmpty(filePath))
                {
                    onComplete?.Invoke();
                    return;
                }

                // Validation sécurisée du fichier
                if (!await ValidateAudioFileAsync(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Fichier audio non valide");
                    onComplete?.Invoke();
                    return;
                }

                _isPlaying = true;
                PlaybackStatusChanged?.Invoke("▶️ Lecture en cours...");

                // Créer le lecteur audio selon le type de chemin
                if (Path.IsPathRooted(filePath))
                {
                    _audioPlayer = _audioManager.CreatePlayer(filePath);
                }
                else
                {
                    var stream = await FileSystem.OpenAppPackageFileAsync(filePath);
                    _audioPlayer = _audioManager.CreatePlayer(stream);
                }

                if (_audioPlayer == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Impossible de créer le lecteur audio.");
                    _isPlaying = false;
                    onComplete?.Invoke();
                    return;
                }

                // Gérer la fin de lecture
                _audioPlayer.PlaybackEnded += (_, _) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _isPlaying = false;
                        PlaybackStatusChanged?.Invoke("⏹️ Lecture terminée");
                        onComplete?.Invoke();
                        if (_audioPlayer != null)
                        {
                            _audioPlayer.Dispose();
                            _audioPlayer = null;
                        }
                    });
                };

                _audioPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lecture audio: {ex.Message}");
                _isPlaying = false;
                PlaybackStatusChanged?.Invoke("❌ Erreur de lecture");
                onComplete?.Invoke();
                _audioPlayer?.Dispose();
                _audioPlayer = null;
            }
        }

        #region Méthodes privées

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isPlaying = false;
                PlaybackStatusChanged?.Invoke("⏹️ Lecture terminée");
                _audioPlayer?.Dispose();
                _audioPlayer = null;
            });
        }

        /// <summary>
        /// Monitore la durée d'enregistrement
        /// </summary>
        private async Task MonitorRecordingDuration()
        {
            try
            {
                while (_isRecording && !(_recordingCancellation?.Token.IsCancellationRequested ?? false))
                {
                    var duration = DateTime.Now - _recordingStartTime;
                    RecordingDurationUpdated?.Invoke(duration);

                    // Arrêter automatiquement si trop long
                    if (duration.TotalSeconds >= MAX_RECORDING_DURATION_SECONDS)
                    {
                        await StopRecordingAsync();
                        break;
                    }

                    await Task.Delay(1000, _recordingCancellation?.Token ?? CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal quand l'enregistrement est annulé
            }
        }

        /// <summary>
        /// Valide un fichier audio pour la sécurité
        /// </summary>
        private async Task<bool> ValidateAudioFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return false;

                var fileInfo = new FileInfo(filePath);
                
                // Vérifier l'extension
                if (!_allowedFormats.Contains(fileInfo.Extension.ToLowerInvariant()))
                    return false;

                // Vérifier la taille (max 50MB)
                if (fileInfo.Length > 50 * 1024 * 1024)
                    return false;

                // Vérifications basiques du contenu
                var header = new byte[16];
                using var stream = File.OpenRead(filePath);
                var bytesRead = await stream.ReadAsync(header, 0, Math.Min(header.Length, (int)stream.Length));

                return bytesRead > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Nettoie les ressources
        /// </summary>
        public void Dispose()
        {
            _recordingCancellation?.Cancel();
            _recordingCancellation?.Dispose();
            _audioPlayer?.Dispose();
            _isRecording = false;
            _isPlaying = false;
        }
    }
}

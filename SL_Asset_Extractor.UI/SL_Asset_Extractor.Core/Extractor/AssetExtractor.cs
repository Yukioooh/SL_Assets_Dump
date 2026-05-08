// On appelle AssetStudioModCLI comme un programme externe.

using Microsoft.Extensions.Logging;
using SL_Asset_Extractor.Core.Scanner;
using SL_Asset_Extractor.Core.Models;
using System.Diagnostics;

namespace SLAssetExtractor.Core.Extractor
{
    public class AssetExtractor
    {
        private readonly Microsoft.Extensions.Logging.ILogger<AssetExtractor> _logger;
        private readonly string _cliPath;        // Chemin vers AssetStudioModCLI.exe
        private readonly string _outputBasePath; // Dossier de sortie des PNG
        private const string UnityVersion = "2021.3.53f1"; // Version fixe du jeu

        
        public event Action<string>? LogReceived;

        public AssetExtractor(
            Microsoft.Extensions.Logging.ILogger<AssetExtractor> logger,
            string cliPath,
            string outputBasePath)
        {
            _logger = logger;
            _cliPath = cliPath;
            _outputBasePath = outputBasePath;
        }
        public async Task<bool> ExtractFromFolderAsync(
            string bundlesFolderPath,
            CancellationToken cancellationToken = default)
        {
            
            if (!File.Exists(_cliPath))
            {
                _logger.LogError("AssetStudioModCLI introuvable : {Path}", _cliPath);
                return false;
            }

            var arguments = BuildArguments(bundlesFolderPath);

            _logger.LogInformation("Lancement de l'extraction...");
            _logger.LogDebug("Commande : {Cli} {Args}", _cliPath, arguments);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _cliPath,
                    Arguments = arguments,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogDebug("[CLI] {Message}", e.Data);
                        LogReceived?.Invoke(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogWarning("[CLI Error] {Message}", e.Data);
                        LogReceived?.Invoke($"⚠️ {e.Data}");
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();


                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Extraction terminée avec succès.");
                    return true;
                }
                else
                {
                    _logger.LogError("CLI terminé avec code d'erreur : {Code}",
                        process.ExitCode);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Extraction annulée.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction.");
                return false;
            }
        }

        private string BuildArguments(string bundlesFolderPath)
        {

            return string.Join(" ", new[]
            {
                $"\"{bundlesFolderPath}\"",          // Dossier source
                "-t tex2d,sprite",                    // Types à extraire
                $"-o \"{_outputBasePath}\"",          // Dossier de sortie
                $"--unity-version {UnityVersion}",    // Version Unity
                "-g type",                            // Grouper par type
                "--log-level Warning"                 // Niveau de log du CLI
            });
        }
    }
}
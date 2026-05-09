using System.Diagnostics;

namespace SL_Asset_Extractor.Core.Extractor
{
    public class AssetExtractor
    {
        private readonly string _cliPath;
        private readonly string _outputBasePath;
        private const string UnityVersion = "2021.3.53f1";

        public event Action<string>? LogReceived;

        public AssetExtractor(string cliPath, string outputBasePath)
        {
            _cliPath = cliPath;
            _outputBasePath = outputBasePath;
        }

        public async Task<bool> ExtractFromFolderAsync(
            string bundlePath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_cliPath))
            {
                LogReceived?.Invoke($"CLI introuvable : {_cliPath}");
                return false;
            }

            var arguments = BuildArguments(bundlePath);

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
                        LogReceived?.Invoke(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        LogReceived?.Invoke($"Erreur CLI : {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);

                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                LogReceived?.Invoke("Extraction annulée.");
                return false;
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Erreur : {ex.Message}");
                return false;
            }
        }

        private string BuildArguments(string bundlePath)
        {
            return string.Join(" ", new[]
            {
                $"\"{bundlePath}\"",
                "-t tex2d,sprite",
                $"-o \"{_outputBasePath}\"",
                $"--unity-version {UnityVersion}",
                "-g type",
                "--log-level Warning"
            });
        }
    }
}
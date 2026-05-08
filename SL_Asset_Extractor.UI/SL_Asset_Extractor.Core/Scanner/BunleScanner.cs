using SL_Asset_Extractor.Core.Models;
using SLAssetExtractor.Core.Scanner;

namespace SL_Asset_Extractor.Core.Scanner
{

    public class BundleScanner
    {

        private readonly ILogger<BundleScanner> _logger;

        public BundleScanner(ILogger<BundleScanner> logger)
        {
            _logger = logger;
        }

        public event Action<string, int, int>? BundleFound;

        public event Action<int>? ScanCompleted;

        public async Task<List<BundleInfo>> ScanDirectoryAsync(
            string directoryPath,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException(
                    $"Le dossier '{directoryPath}' n'existe pas.");
            }

            _logger.LogInformation("Démarrage du scan dans : {Path}", directoryPath);

            var bundleFiles = Directory
                .GetFiles(directoryPath, "*.bundle", SearchOption.AllDirectories);

            _logger.LogInformation("{Count} fichiers .bundle trouvés.", bundleFiles.Length);

            var results = new List<BundleInfo>();

            for (int i = 0; i < bundleFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = bundleFiles[i];
                var fileName = Path.GetFileName(filePath);

                BundleFound?.Invoke(fileName, i + 1, bundleFiles.Length);

                try
                {
                    var fileInfo = new FileInfo(filePath);

                    var hash = await HashCalculator.ComputeFileHashAsync(filePath);

                    var bundleInfo = new BundleInfo
                    {
                        FileName = fileName,
                        FullPath = filePath,
                        Hash = hash,
                        FileSize = fileInfo.Length,
                        LastScanned = DateTime.UtcNow 
                    };

                    results.Add(bundleInfo);

                    _logger.LogDebug("Bundle analysé : {Name} ({Size} Ko)",
                        fileName, fileInfo.Length / 1024);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erreur lors du scan du bundle : {FileName}", fileName);
                }
            }

            ScanCompleted?.Invoke(results.Count);
            _logger.LogInformation("Scan terminé : {Count} bundles analysés.", results.Count);

            return results;
        }
    }

    public interface ILogger<T>
    {
        void LogDebug(string v1, string fileName, long v2);
        void LogError(Exception ex, string v, string fileName);
        void LogInformation(string v, string directoryPath);
        void LogInformation(string v, int length);
    }
}